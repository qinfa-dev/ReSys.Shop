using Ardalis.GuardClauses;

using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Caching.Interfaces;
using ReSys.Infrastructure.Persistence.Caching.Options;
using ReSys.Infrastructure.Persistence.Caching.Services;

namespace ReSys.Infrastructure.Persistence.Caching;

/// <summary>
/// Registers caching infrastructure (InMemory or Redis) dynamically based on environment.
/// Uses .NET 9+ HybridCache for local + distributed caching.
/// </summary>
public static class CacheServiceCollectionExtensions
{
    /// <summary>
    /// Adds hybrid caching infrastructure that automatically adapts based on environment and configuration.
    /// </summary>
    public static IServiceCollection AddCaching(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        // Bind and validate configuration
        services.AddOptions<CacheOptions>()
            .BindConfiguration(configSectionPath: CacheOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        var opts = configuration.GetSection(key: CacheOptions.SectionName).Get<CacheOptions>()
            ?? new CacheOptions();

        // Auto-select provider based on environment
        opts.Type = DetermineProviderType(environment: environment,
            configuredType: opts.Type);

        // Register appropriate distributed cache provider
        RegisterDistributedCache(services: services,
            opts: opts);

        // Configure HybridCache with defaults
        services.AddHybridCache(setupAction: hc =>
        {
            hc.DefaultEntryOptions = new HybridCacheEntryOptions
            {
                Expiration = TimeSpan.FromMinutes(minutes: opts.DefaultExpiryMinutes),
                LocalCacheExpiration = TimeSpan.FromSeconds(seconds: opts.LocalCacheExpirySeconds)
            };
        });

        // Register cache service abstraction
        services.AddSingleton<ICacheService, CacheService>();

        LogCacheConfiguration(environment: environment,
            providerType: opts.Type);

        return services;
    }

    private static string DetermineProviderType(IHostEnvironment environment, string? configuredType)
    {
        // Explicit configuration takes precedence
        if (!string.IsNullOrWhiteSpace(value: configuredType))
            return configuredType;

        // Auto-detect based on environment
        return environment.IsEnvironment(environmentName: "Test") || environment.IsDevelopment()
            ? "InMemory"
            : "Redis";
    }

    private static void RegisterDistributedCache(IServiceCollection services, CacheOptions opts)
    {
        if (opts.Type.Equals(value: "Redis",
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            var connection = GetRedisConnectionString(opts: opts);
            Guard.Against.NullOrWhiteSpace(input: connection,
                message: "Redis connection string is required when Type='Redis'");

            services.AddStackExchangeRedisCache(setupAction: o => o.Configuration = connection);
        }
        else
        {
            services.AddDistributedMemoryCache();
        }
    }

    private static string? GetRedisConnectionString(CacheOptions opts)
    {
        if (!string.IsNullOrWhiteSpace(value: opts.RedisConnection))
            return opts.RedisConnection;

        if (opts.RedisCacheOptions is not null)
        {
            return $"{opts.RedisCacheOptions.Host}:{opts.RedisCacheOptions.Port}," +
                   $"allowAdmin={opts.RedisCacheOptions.AllowAdmin}";
        }

        return null;
    }

    private static void LogCacheConfiguration(IHostEnvironment environment, string providerType)
    {
        var loggerFactory = LoggerFactory.Create(configure: builder =>
        {
            builder.AddConsole();
            if (environment.IsDevelopment())
                builder.SetMinimumLevel(level: LogLevel.Debug);
        });

        var logger = loggerFactory.CreateLogger(categoryName: "CacheInit");
        logger.LogInformation(
            message: "Hybrid cache initialized using {Provider} provider (Environment: {Environment})",
            args:
            [
                providerType,
                environment.EnvironmentName
            ]);
    }
}