namespace ReSys.Core.Common.Models.Pagination;

/// <summary>
/// Extension methods for applying pagination to IQueryable sources.
/// </summary>
public static class PagingExtensions
{
    private const int DefaultPageSize = 10;
    private const int MaxPageSize = 100;
    private const int MaxAllItemsLimit = 1000;

    /// <summary>
    /// Applies pagination to a queryable using default page size if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the queryable.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters (optional).</param>
    /// <param name="fallbackDefaultPageSize">The default page size to use when no paging params provided.</param>
    /// <returns>A queryable with pagination applied.</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static IQueryable<T> ApplyPagingOrDefault<T>(
        this IQueryable<T> query,
        IPagingParam? pagingParams,
        int fallbackDefaultPageSize = DefaultPageSize)
    {
        ArgumentNullException.ThrowIfNull(argument: query);

        if (pagingParams?.HasPagingValues() != true)
        {
            // No paging parameters provided, use default pagination
            int pageSize = NormalizePageSize(pageSize: fallbackDefaultPageSize);
            return query.Take(count: pageSize);
        }

        // Apply specified pagination
        int pageIndex = Math.Max(val1: pagingParams.EffectivePageIndex(),
            val2: 0);
        int effectivePageSize = NormalizePageSize(pageSize: pagingParams.PageSize ?? DefaultPageSize);

        return query
            .Skip(count: pageIndex * effectivePageSize)
            .Take(count: effectivePageSize);
    }

    /// <summary>
    /// Applies pagination to a queryable or returns all items (capped at max limit) if no paging parameters are provided.
    /// </summary>
    /// <typeparam name="T">The type of items in the queryable.</typeparam>
    /// <param name="query">The source queryable.</param>
    /// <param name="pagingParams">The paging parameters (optional).</param>
    /// <param name="maxAllItemsLimit">The maximum number of items to return when no pagination is applied.</param>
    /// <returns>A queryable with pagination applied or all items (capped).</returns>
    /// <exception cref="ArgumentNullException">Thrown when query is null.</exception>
    public static IQueryable<T> ApplyPagingOrAll<T>(
        this IQueryable<T> query,
        IPagingParam? pagingParams,
        int maxAllItemsLimit = MaxAllItemsLimit)
    {
        ArgumentNullException.ThrowIfNull(argument: query);

        if (pagingParams?.HasPagingValues() != true)
        {
            // No paging parameters provided, return all items but cap at max limit
            int effectiveLimit = Math.Min(val1: Math.Max(val1: maxAllItemsLimit,
                    val2: 1),
                val2: MaxAllItemsLimit);
            return query.Take(count: effectiveLimit);
        }

        // Apply specified pagination
        int pageIndex = Math.Max(val1: pagingParams.EffectivePageIndex(),
            val2: 0);
        int effectivePageSize = NormalizePageSize(pageSize: pagingParams.PageSize ?? DefaultPageSize);

        return query
            .Skip(count: pageIndex * effectivePageSize)
            .Take(count: effectivePageSize);
    }

    /// <summary>
    /// Gets the effective page index (0-based) from paging parameters.
    /// </summary>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <returns>The effective page index (0-based).</returns>
    public static int EffectivePageIndex(this IPagingParam pagingParams)
    {
        ArgumentNullException.ThrowIfNull(argument: pagingParams);
        return Math.Max(val1: pagingParams.PageIndex ?? 0,
            val2: 0);
    }

    /// <summary>
    /// Gets the effective page number (1-based) from paging parameters.
    /// </summary>
    /// <param name="pagingParams">The paging parameters.</param>
    /// <returns>The effective page number (1-based).</returns>
    public static int EffectivePageNumber(this IPagingParam pagingParams)
    {
        ArgumentNullException.ThrowIfNull(argument: pagingParams);
        return Math.Max(val1: pagingParams.PageIndex ?? 0,
            val2: 0) + 1;
    }

    /// <summary>
    /// Checks if paging parameters contain valid paging values.
    /// </summary>
    /// <param name="pagingParams">The paging parameters to check.</param>
    /// <returns>True if paging parameters contain valid paging values; otherwise, false.</returns>
    public static bool HasPagingValues(this IPagingParam? pagingParams)
    {
        return pagingParams?.PageSize.HasValue == true || pagingParams?.PageIndex.HasValue == true;
    }

    /// <summary>
    /// Normalizes the page size to ensure it's within acceptable bounds.
    /// </summary>
    /// <param name="pageSize">The requested page size.</param>
    /// <returns>A normalized page size between 1 and MaxPageSize.</returns>
    private static int NormalizePageSize(int pageSize) =>
        pageSize <= 0 ? DefaultPageSize : Math.Min(val1: pageSize,
            val2: MaxPageSize);
}