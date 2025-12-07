using Microsoft.Extensions.Options;

namespace ReSys.Infrastructure.Caching.Options;

/// <summary>
/// Represents caching configuration for hybrid caching (local + distributed).
/// Supports environment-based Redis or in-memory caching.
/// </summary>
public sealed class CacheOptions : IValidateOptions<CacheOptions>
{
    public const string SectionName = "Cache";

    /// <summary>
    /// Cache provider type (InMemory or Redis).
    /// </summary>
    public string Type { get; set; } = "InMemory";

    /// <summary>
    /// Default distributed cache expiration in minutes.
    /// </summary>
    public int DefaultExpiryMinutes { get; set; } = 30;

    /// <summary>
    /// Local in-memory cache expiration in seconds.
    /// </summary>
    public int LocalCacheExpirySeconds { get; set; } = 300;

    /// <summary>
    /// Default prefix used for all cache keys.
    /// </summary>
    public string DefaultCachePrefix { get; set; } = "Ch_";

    /// <summary>
    /// Redis connection string used when <see cref="Type"/> is Redis.
    /// </summary>
    public string? RedisConnection { get; set; }

    /// <summary>
    /// Nested Redis options (optional fine-tuning for production).
    /// </summary>
    public RedisDistributedCacheOptions? RedisCacheOptions { get; set; } = new();

    public ValidateOptionsResult Validate(string? name, CacheOptions options)
    {
        if (string.IsNullOrWhiteSpace(value: options.Type))
            return ValidateOptionsResult.Fail(failureMessage: "Cache:Type must be specified.");

        if (options.Type != "InMemory" && options.Type != "Redis")
            return ValidateOptionsResult.Fail(failureMessage: "Cache:Type must be 'InMemory' or 'Redis'.");

        if (options.Type == "Redis" && string.IsNullOrWhiteSpace(value: options.RedisConnection))
            return ValidateOptionsResult.Fail(failureMessage: "Cache:RedisConnection is required when using Redis.");

        if (options.DefaultExpiryMinutes <= 0)
            return ValidateOptionsResult.Fail(failureMessage: "Cache:DefaultExpiryMinutes must be greater than 0.");

        if (options.LocalCacheExpirySeconds <= 0)
            return ValidateOptionsResult.Fail(failureMessage: "Cache:LocalCacheExpirySeconds must be greater than 0.");

        return ValidateOptionsResult.Success;
    }
}

/// <summary>
/// Represents configuration for Redis cache.
/// </summary>
public sealed class RedisDistributedCacheOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6379;
    public bool AllowAdmin { get; set; } = false;
}
