using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

using ReSys.Core.Feature.Common.Caching.Interfaces;
using ReSys.Infrastructure.Persistence.Caching.Options;

namespace ReSys.Infrastructure.Persistence.Caching.Services;

/// <summary>
/// Provides a high-performance caching service built on top of .NET 9 <see cref="HybridCache"/>.
/// Supports local + distributed caching with configurable expiration and tagging.
/// </summary>
public sealed class CacheService(HybridCache hybridCache, IOptions<CacheOptions> cacheOptions)
    : ICacheService
{
    private readonly CacheOptions _cacheOptions = cacheOptions.Value;

    /// <inheritdoc />
    public async Task<T?> GetAsync<T>(string key, CancellationToken cancellationToken = default)
    {
        try
        {
            // Try to get without creating - returns default if not found
            return await hybridCache.GetOrCreateAsync(
                key: key,
                factory: static _ => ValueTask.FromResult<T?>(result: default),
                cancellationToken: cancellationToken);
        }
        catch
        {
            // If any error occurs, treat as cache miss
            return default;
        }
    }

    /// <inheritdoc />
    public async Task<T> GetOrCreateAsync<T>(
        string key,
        Func<CancellationToken, ValueTask<T>> factory,
        IEnumerable<string>? tags = null,
        TimeSpan? localExpiration = null,
        TimeSpan? distributedExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = BuildCacheOptions(localExpiration: localExpiration,
            distributedExpiration: distributedExpiration);

        return await hybridCache.GetOrCreateAsync(
            key: key,
            factory: factory,
            options: options,
            tags: tags,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task SetAsync<T>(
        string key,
        T value,
        IEnumerable<string>? tags = null,
        TimeSpan? localExpiration = null,
        TimeSpan? distributedExpiration = null,
        CancellationToken cancellationToken = default)
    {
        var options = BuildCacheOptions(localExpiration: localExpiration,
            distributedExpiration: distributedExpiration);

        await hybridCache.SetAsync(
            key: key,
            value: value,
            options: options,
            tags: tags,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveAsync(string key, CancellationToken cancellationToken = default)
    {
        await hybridCache.RemoveAsync(key: key,
            cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveManyAsync(IEnumerable<string> keys, CancellationToken cancellationToken = default)
    {
        foreach (var key in keys)
            await hybridCache.RemoveAsync(key: key,
                cancellationToken: cancellationToken);
    }

    /// <inheritdoc />
    public async Task RemoveByTagsAsync(IEnumerable<string> tags, CancellationToken cancellationToken = default)
    {
        await hybridCache.RemoveByTagAsync(tags: tags,
            cancellationToken: cancellationToken);
    }

    private HybridCacheEntryOptions BuildCacheOptions(TimeSpan? localExpiration, TimeSpan? distributedExpiration)
    {
        return new HybridCacheEntryOptions
        {
            LocalCacheExpiration = localExpiration
                ?? TimeSpan.FromSeconds(seconds: _cacheOptions.LocalCacheExpirySeconds),
            Expiration = distributedExpiration
                ?? TimeSpan.FromMinutes(minutes: _cacheOptions.DefaultExpiryMinutes)
        };
    }
}