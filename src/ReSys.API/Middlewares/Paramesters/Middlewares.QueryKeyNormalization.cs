using System.Buffers;
using System.Collections.Concurrent;
using System.Runtime.CompilerServices;

using Microsoft.Extensions.Primitives;

namespace ReSys.API.Middlewares.Paramesters;

/// <summary>
/// ASP.NET Core middleware that normalizes query string parameter names to support both 
/// snake_case and camelCase conventions simultaneously. This allows APIs to accept 
/// query parameters in either format while maintaining compatibility with model binding.
/// </summary>
/// <remarks>
/// <para>
/// The middleware creates additional query parameter entries with normalized names:
/// - snake_case parameters get camelCase equivalents added
/// - camelCase parameters get snake_case equivalents added
/// </para>
/// <para>
/// Performance characteristics:
/// - Uses a two-tier caching strategy: lock-free read snapshots + concurrent write cache
/// - Employs stack allocation for small strings and ArrayPool for larger ones
/// - Early exit optimization when no normalization is needed
/// - Zero allocation for requests that don't require processing
/// </para>
/// <para>
/// Memory usage: Cache grows with unique parameter names but provides O(1) lookup.
/// Thread safety: Fully thread-safe with lock-free read paths for optimal performance.
/// </para>
/// </remarks>
/// <example>
/// <code>
/// // Register in Program.cs or Startup.cs
/// app.UseMiddleware&lt;QueryKeyNormalizationMiddleware&gt;();
/// 
/// // Example transformations:
/// // Input:  ?page_size=10&amp;sort_order=desc
/// // Result: ?page_size=10&amp;pageSize=10&amp;sort_order=desc&amp;sortOrder=desc
/// 
/// // Input:  ?pageSize=10&amp;sortOrder=desc  
/// // Result: ?pageSize=10&amp;page_size=10&amp;sortOrder=desc&amp;sort_order=desc
/// </code>
/// </example>
/// <remarks>
/// Initializes a new instance of the QueryKeyNormalizationMiddleware.
/// </remarks>
/// <param name="next">The next middleware delegate in the pipeline.</param>
public sealed class QueryKeyNormalizationMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    #region Caching Infrastructure

    /// <summary>
    /// Thread-safe cache for snake_case → camelCase conversions.
    /// Optimized for concurrent writes during cache misses.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> _camelCaseCache = new(comparer: StringComparer.Ordinal);

    /// <summary>
    /// Thread-safe cache for camelCase → snake_case conversions.
    /// Optimized for concurrent writes during cache misses.
    /// </summary>
    private static readonly ConcurrentDictionary<string, string> _snakeCaseCache = new(comparer: StringComparer.Ordinal);

    /// <summary>
    /// Lock-free, read-optimized snapshot of camelCase cache.
    /// Rebuilt periodically to maintain fast read performance.
    /// </summary>
    private static volatile IReadOnlyDictionary<string, string>? _camelCaseSnapshot;

    /// <summary>
    /// Lock-free, read-optimized snapshot of snake_case cache.
    /// Rebuilt periodically to maintain fast read performance.
    /// </summary>
    private static volatile IReadOnlyDictionary<string, string>? _snakeCaseSnapshot;

    /// <summary>
    /// Number of new entries added to camelCase cache since last snapshot rebuild.
    /// Used to trigger periodic snapshot refreshes.
    /// </summary>
    private static int _camelEntriesSinceSnapshot;

    /// <summary>
    /// Number of new entries added to snake_case cache since last snapshot rebuild.
    /// Used to trigger periodic snapshot refreshes.
    /// </summary>
    private static int _snakeEntriesSinceSnapshot;

    #endregion

    #region Configuration Constants

    /// <summary>
    /// Threshold for rebuilding read-optimized snapshots.
    /// When new cache entries exceed this number, snapshots are recreated.
    /// </summary>
    /// <remarks>
    /// Higher values = less frequent rebuilds but potentially staler snapshots.
    /// Lower values = more frequent rebuilds but better read performance.
    /// </remarks>
    private const int SnapshotRebuildThreshold = 64;

    /// <summary>
    /// Maximum string length for stack allocation optimization.
    /// Strings shorter than this use stackalloc, longer strings use ArrayPool.
    /// </summary>
    /// <remarks>
    /// Typical query parameter names are 5-20 characters, so 256 provides ample headroom
    /// while staying well within stack allocation safety limits.
    /// </remarks>
    private const int StackAllocThreshold = 256;

    #endregion

    /// <summary>
    /// Processes the HTTP request, normalizing query parameter names if needed.
    /// </summary>
    /// <param name="context">The HTTP context for the current request.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    /// <remarks>
    /// Performance optimizations applied:
    /// 1. Early exit if no query parameters exist
    /// 2. Early exit if no normalization is needed
    /// 3. Only replaces query collection if changes were made
    /// 4. Uses OrdinalIgnoreCase comparison for model binding compatibility
    /// </remarks>
    public async Task InvokeAsync(HttpContext context)
    {
        IQueryCollection originalQuery = context.Request.Query;

        // Fast path: no query parameters
        if (originalQuery.Count == 0)
        {
            await _next(context: context);
            return;
        }

        // Fast path: no normalization needed
        if (!RequiresNormalization(query: originalQuery))
        {
            await _next(context: context);
            return;
        }

        // Build normalized query collection
        Dictionary<string, StringValues> normalizedDict = BuildNormalizedQuery(originalQuery: originalQuery);

        // Only replace if we actually added normalized keys
        if (normalizedDict.Count > originalQuery.Count)
        {
            context.Request.Query = new QueryCollection(store: normalizedDict);
        }

        await _next(context: context);
    }

    #region Core Logic

    /// <summary>
    /// Determines if any query parameters require normalization.
    /// </summary>
    /// <param name="query">The query collection to examine.</param>
    /// <returns>True if normalization is needed, false otherwise.</returns>
    /// <remarks>
    /// Optimized for early exit: checks cheap conditions first (underscore lookup)
    /// then more expensive character-by-character uppercase detection.
    /// </remarks>
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
    private static bool RequiresNormalization(IQueryCollection query)
    {
        foreach (string key in query.Keys)
        {
            ReadOnlySpan<char> keySpan = key.AsSpan();

            // Check for underscores first (cheap IndexOf operation)
            if (keySpan.IndexOf(value: '_') >= 0)
                return true;

            // Check for uppercase letters (requires character iteration)
            for (int i = 0; i < keySpan.Length; i++)
            {
                char ch = keySpan[index: i];
                if (ch is >= 'A' and <= 'Z')
                    return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Builds a new query dictionary with original parameters plus normalized equivalents.
    /// </summary>
    /// <param name="originalQuery">The original query collection.</param>
    /// <returns>A dictionary containing both original and normalized parameter names.</returns>
    private static Dictionary<string, StringValues> BuildNormalizedQuery(IQueryCollection originalQuery)
    {
        // Pre-size to avoid resizing; use OrdinalIgnoreCase for model binder compatibility
        Dictionary<string, StringValues> dict = new(
            capacity: originalQuery.Count * 2,
            comparer: StringComparer.OrdinalIgnoreCase);

        // Add all original parameters first
        foreach (KeyValuePair<string, StringValues> kvp in originalQuery)
        {
            dict[key: kvp.Key] = kvp.Value;
        }

        // Add normalized variants
        foreach (KeyValuePair<string, StringValues> kvp in originalQuery)
        {
            ProcessKeyForNormalization(key: kvp.Key,
                value: kvp.Value,
                targetDict: dict);
        }

        return dict;
    }

    /// <summary>
    /// Processes a single query parameter key, adding its normalized equivalent if applicable.
    /// </summary>
    /// <param name="key">The parameter key to process.</param>
    /// <param name="value">The parameter value(s).</param>
    /// <param name="targetDict">The dictionary to add normalized keys to.</param>
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
    private static void ProcessKeyForNormalization(string key, StringValues value, Dictionary<string, StringValues> targetDict)
    {
        ReadOnlySpan<char> keySpan = key.AsSpan();

        if (keySpan.IndexOf(value: '_') >= 0)
        {
            // snake_case → camelCase
            string camelCaseKey = GetCamelCase(snakeCase: key);
            targetDict.TryAdd(key: camelCaseKey,
                value: value);
        }
        else
        {
            // Only convert to snake_case if uppercase letters are present
            if (HasUppercaseLetters(span: keySpan))
            {
                string snakeCaseKey = GetSnakeCase(camelCase: key);
                targetDict.TryAdd(key: snakeCaseKey,
                    value: value);
            }
        }
    }

    /// <summary>
    /// Efficiently checks if a string contains uppercase letters.
    /// </summary>
    /// <param name="span">The string span to check.</param>
    /// <returns>True if uppercase letters are found, false otherwise.</returns>
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
    private static bool HasUppercaseLetters(ReadOnlySpan<char> span)
    {
        for (int i = 0; i < span.Length; i++)
        {
            char ch = span[index: i];
            if (ch is >= 'A' and <= 'Z')
                return true;
        }
        return false;
    }

    #endregion

    #region High-Performance Cache Access

    /// <summary>
    /// Retrieves or computes the camelCase equivalent of a snake_case string.
    /// Uses a two-tier caching strategy for optimal read performance.
    /// </summary>
    /// <param name="snakeCase">The snake_case input string.</param>
    /// <returns>The camelCase equivalent string.</returns>
    /// <remarks>
    /// Cache strategy:
    /// 1. Try lock-free snapshot first (fastest)
    /// 2. Fall back to concurrent dictionary with computation
    /// 3. Periodically rebuild snapshot for optimal read performance
    /// </remarks>
    private static string GetCamelCase(string snakeCase)
    {
        // Try fast lock-free read from snapshot
        IReadOnlyDictionary<string, string>? snapshot = _camelCaseSnapshot;
        if (snapshot?.TryGetValue(key: snakeCase,
                value: out string? cachedValue) == true)
            return cachedValue;

        // Cache miss: compute and store in concurrent dictionary
        string computedValue = _camelCaseCache.GetOrAdd(key: snakeCase,
            valueFactory: static key => ComputeCamelCase(snakeCase: key));

        // Periodically rebuild snapshot to maintain fast reads
        if (Interlocked.Increment(location: ref _camelEntriesSinceSnapshot) >= SnapshotRebuildThreshold)
        {
            Interlocked.Exchange(location1: ref _camelEntriesSinceSnapshot,
                value: 0);
            _camelCaseSnapshot = CreateReadOptimizedSnapshot(source: _camelCaseCache);
        }

        return computedValue;
    }

    /// <summary>
    /// Retrieves or computes the snake_case equivalent of a camelCase string.
    /// Uses a two-tier caching strategy for optimal read performance.
    /// </summary>
    /// <param name="camelCase">The camelCase input string.</param>
    /// <returns>The snake_case equivalent string.</returns>
    private static string GetSnakeCase(string camelCase)
    {
        // Try fast lock-free read from snapshot
        IReadOnlyDictionary<string, string>? snapshot = _snakeCaseSnapshot;
        if (snapshot?.TryGetValue(key: camelCase,
                value: out string? cachedValue) == true)
            return cachedValue;

        // Cache miss: compute and store in concurrent dictionary
        string computedValue = _snakeCaseCache.GetOrAdd(key: camelCase,
            valueFactory: static key => ComputeSnakeCase(camelCase: key));

        // Periodically rebuild snapshot to maintain fast reads
        if (Interlocked.Increment(location: ref _snakeEntriesSinceSnapshot) >= SnapshotRebuildThreshold)
        {
            Interlocked.Exchange(location1: ref _snakeEntriesSinceSnapshot,
                value: 0);
            _snakeCaseSnapshot = CreateReadOptimizedSnapshot(source: _snakeCaseCache);
        }

        return computedValue;
    }

    /// <summary>
    /// Creates a read-optimized snapshot from a concurrent dictionary.
    /// </summary>
    /// <param name="source">The source concurrent dictionary.</param>
    /// <returns>A read-only dictionary optimized for fast lookups.</returns>
    /// <remarks>
    /// Plain Dictionary instances offer better read performance than ConcurrentDictionary
    /// since they don't have concurrency overhead during lookups.
    /// </remarks>
    private static IReadOnlyDictionary<string, string> CreateReadOptimizedSnapshot(ConcurrentDictionary<string, string> source)
    {
        Dictionary<string, string> snapshot = new(capacity: source.Count,
            comparer: StringComparer.Ordinal);
        foreach (KeyValuePair<string, string> kvp in source)
        {
            snapshot[key: kvp.Key] = kvp.Value;
        }
        return snapshot;
    }

    #endregion

    #region String Transformation Algorithms

    /// <summary>
    /// Converts a snake_case string to camelCase format.
    /// Uses optimized memory allocation strategies for different string sizes.
    /// </summary>
    /// <param name="snakeCase">The snake_case input string (e.g., "page_size").</param>
    /// <returns>The camelCase equivalent (e.g., "pageSize").</returns>
    /// <remarks>
    /// Performance optimizations:
    /// - Stack allocation for strings ≤ 256 chars (zero heap allocation)
    /// - ArrayPool for larger strings (reduced GC pressure)
    /// - Manual case conversion using bit manipulation (faster than char methods)
    /// - Single-pass algorithm with minimal branching
    /// </remarks>
    /// <example>
    /// ComputeCamelCase("page_size") → "pageSize"
    /// ComputeCamelCase("sort_order_by") → "sortOrderBy"
    /// ComputeCamelCase("no_underscores") → "nounderscores"
    /// </example>
    private static string ComputeCamelCase(string snakeCase)
    {
        if (string.IsNullOrEmpty(value: snakeCase))
            return snakeCase;

        ReadOnlySpan<char> inputSpan = snakeCase.AsSpan();
        int inputLength = inputSpan.Length;

        // Fast path: no underscores found
        if (inputSpan.IndexOf(value: '_') == -1)
            return snakeCase.ToLowerInvariant();

        // Choose allocation strategy based on string size
        if (inputLength <= StackAllocThreshold)
        {
            return ComputeCamelCaseWithStackAlloc(input: inputSpan);
        }

        return ComputeCamelCaseWithArrayPool(input: inputSpan,
            inputLength: inputLength);
    }

    /// <summary>
    /// Computes camelCase using stack allocation for small strings.
    /// </summary>
    private static string ComputeCamelCaseWithStackAlloc(ReadOnlySpan<char> input)
    {
        Span<char> buffer = stackalloc char[StackAllocThreshold];
        int writeIndex = 0;
        bool shouldCapitalizeNext = false;

        foreach (char ch in input)
        {
            if (ch == '_')
            {
                // Only set capitalize flag if we've written at least one character
                if (writeIndex > 0)
                    shouldCapitalizeNext = true;
            }
            else
            {
                if (shouldCapitalizeNext)
                {
                    // Convert to uppercase using bit manipulation (faster than char.ToUpper)
                    buffer[index: writeIndex++] = ch is >= 'a' and <= 'z' ? (char)(ch - 32) : ch;
                    shouldCapitalizeNext = false;
                }
                else
                {
                    // Convert to lowercase using bit manipulation
                    buffer[index: writeIndex++] = ch is >= 'A' and <= 'Z' ? (char)(ch + 32) : ch;
                }
            }
        }

        return new string(value: buffer[..writeIndex]);
    }

    /// <summary>
    /// Computes camelCase using ArrayPool for large strings.
    /// </summary>
    private static string ComputeCamelCaseWithArrayPool(ReadOnlySpan<char> input, int inputLength)
    {
        char[] rentedBuffer = ArrayPool<char>.Shared.Rent(minimumLength: inputLength);
        try
        {
            Span<char> buffer = rentedBuffer.AsSpan(start: 0,
                length: inputLength);
            int writeIndex = 0;
            bool shouldCapitalizeNext = false;

            foreach (char ch in input)
            {
                if (ch == '_')
                {
                    if (writeIndex > 0)
                        shouldCapitalizeNext = true;
                }
                else
                {
                    if (shouldCapitalizeNext)
                    {
                        buffer[index: writeIndex++] = ch is >= 'a' and <= 'z' ? (char)(ch - 32) : ch;
                        shouldCapitalizeNext = false;
                    }
                    else
                    {
                        buffer[index: writeIndex++] = ch is >= 'A' and <= 'Z' ? (char)(ch + 32) : ch;
                    }
                }
            }

            return new string(value: buffer[..writeIndex]);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(array: rentedBuffer);
        }
    }

    /// <summary>
    /// Converts a camelCase string to snake_case format.
    /// Uses optimized memory allocation strategies for different string sizes.
    /// </summary>
    /// <param name="camelCase">The camelCase input string (e.g., "pageSize").</param>
    /// <returns>The snake_case equivalent (e.g., "page_size").</returns>
    /// <remarks>
    /// Performance optimizations:
    /// - Pre-scans for uppercase count to avoid unnecessary processing
    /// - Estimates output buffer size to minimize allocations
    /// - Uses stack allocation for small strings, ArrayPool for large ones
    /// - Manual case conversion using bit manipulation
    /// </remarks>
    /// <example>
    /// ComputeSnakeCase("pageSize") → "page_size"
    /// ComputeSnakeCase("sortOrderBy") → "sort_order_by"
    /// ComputeSnakeCase("nouppercase") → "nouppercase"
    /// </example>
    private static string ComputeSnakeCase(string camelCase)
    {
        if (string.IsNullOrEmpty(value: camelCase))
            return camelCase;

        ReadOnlySpan<char> inputSpan = camelCase.AsSpan();
        int inputLength = inputSpan.Length;

        // Pre-scan for uppercase letters
        int uppercaseCount = CountUppercaseLetters(span: inputSpan);

        // Fast path: no uppercase letters
        if (uppercaseCount == 0)
            return camelCase;

        // Estimate output length: original + underscores for each uppercase
        int estimatedOutputLength = inputLength + uppercaseCount;

        // Choose allocation strategy
        if (estimatedOutputLength <= StackAllocThreshold)
        {
            return ComputeSnakeCaseWithStackAlloc(input: inputSpan);
        }

        return ComputeSnakeCaseWithArrayPool(input: inputSpan,
            estimatedLength: estimatedOutputLength);
    }

    /// <summary>
    /// Counts uppercase letters in a string span.
    /// </summary>
    [MethodImpl(methodImplOptions: MethodImplOptions.AggressiveInlining)]
    private static int CountUppercaseLetters(ReadOnlySpan<char> span)
    {
        int count = 0;
        foreach (char ch in span)
        {
            if (ch is >= 'A' and <= 'Z')
                count++;
        }
        return count;
    }

    /// <summary>
    /// Computes snake_case using stack allocation for small strings.
    /// </summary>
    private static string ComputeSnakeCaseWithStackAlloc(ReadOnlySpan<char> input)
    {
        Span<char> buffer = stackalloc char[StackAllocThreshold];
        int writeIndex = 0;

        for (int i = 0; i < input.Length; i++)
        {
            char ch = input[index: i];
            if (ch is >= 'A' and <= 'Z')
            {
                // Add underscore before uppercase letters (except at start)
                if (i > 0)
                    buffer[index: writeIndex++] = '_';

                // Convert to lowercase using bit manipulation
                buffer[index: writeIndex++] = (char)(ch + 32);
            }
            else
            {
                buffer[index: writeIndex++] = ch;
            }
        }

        return new string(value: buffer[..writeIndex]);
    }

    /// <summary>
    /// Computes snake_case using ArrayPool for large strings.
    /// </summary>
    private static string ComputeSnakeCaseWithArrayPool(ReadOnlySpan<char> input, int estimatedLength)
    {
        char[] rentedBuffer = ArrayPool<char>.Shared.Rent(minimumLength: estimatedLength);
        try
        {
            Span<char> buffer = rentedBuffer.AsSpan();
            int writeIndex = 0;

            for (int i = 0; i < input.Length; i++)
            {
                char ch = input[index: i];
                if (ch is >= 'A' and <= 'Z')
                {
                    if (i > 0)
                        buffer[index: writeIndex++] = '_';
                    buffer[index: writeIndex++] = (char)(ch + 32);
                }
                else
                {
                    buffer[index: writeIndex++] = ch;
                }
            }

            return new string(value: buffer[..writeIndex]);
        }
        finally
        {
            ArrayPool<char>.Shared.Return(array: rentedBuffer);
        }
    }

    #endregion
}