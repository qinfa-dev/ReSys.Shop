using System.Diagnostics;

using Ardalis.GuardClauses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Infrastructure.Persistence.Caching;
using ReSys.Infrastructure.Persistence.Contexts;
using ReSys.Infrastructure.Persistence.Interceptors;
using ReSys.Infrastructure.Persistence.Options;
using ReSys.Infrastructure.Seeders;

using Serilog;

namespace ReSys.Infrastructure.Persistence;

/// <summary>
/// Extension methods for configuring database and persistence services.
/// </summary>
internal static class DatabaseServiceCollectionExtensions
{
    #region Public Methods

    /// <summary>
    /// Registers all persistence-related services including EF Core,
    /// interceptors, Unit of Work, and data seeding components.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="environment">The hosting environment.</param>
    /// <returns>The same <see cref="IServiceCollection"/> instance for chaining.</returns>
    public static IServiceCollection AddPersistence(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var stopwatch = Stopwatch.StartNew();
        Log.Information(LogTemplates.ModuleRegistered,
            "Persistence",
            0);

        try
        {
            var count = 0;

            // Register EF Core interceptors
            services.AddEfCoreInterceptors();
            count++;

            // Register database context
            services.AddDatabase(configuration, environment);
            count++;

            // Register caching services
            services.AddCaching(configuration, environment);
            count++;

            // Register Unit of Work pattern
            services.AddUnitOfWork();
            count++;

            // Register data seeders
            services.AddDataSeeders();
            count++;

            stopwatch.Stop();
            Log.Information(LogTemplates.ModuleRegistered, "Persistence", count);
            Log.Debug("Persistence configured in {Duration:0.0000}ms", stopwatch.Elapsed.TotalMilliseconds);

            return services;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            Log.Fatal(ex, LogTemplates.ComponentStartupFailed, "Persistence", ex.Message);
            throw;
        }
    }

    #endregion

    #region EF Core Interceptors

    /// <summary>
    /// Registers EF Core save-change interceptors for audit tracking and domain event dispatching.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    private static void AddEfCoreInterceptors(this IServiceCollection services)
    {
        // Action tracking interceptor
        services.AddScoped<ISaveChangesInterceptor, PersistenceInterceptors.ActionTracking>();
        Log.Debug(LogTemplates.ServiceRegistered, nameof(PersistenceInterceptors.ActionTracking), "Scoped");

        // Domain event dispatcher interceptor
        services.AddScoped<ISaveChangesInterceptor, PersistenceInterceptors.DispatchDomainEvent>();
        Log.Debug(LogTemplates.ServiceRegistered, nameof(PersistenceInterceptors.DispatchDomainEvent), "Scoped");

        // Auditing log interceptor
        services.AddScoped<ISaveChangesInterceptor, PersistenceInterceptors.AuditingLog>();
        Log.Debug(LogTemplates.ServiceRegistered, nameof(PersistenceInterceptors.AuditingLog), "Scoped");

        Log.Information("EF Core interceptors registered (3 total)");
    }

    #endregion

    #region Database Configuration

    /// <summary>
    /// Registers the application's database context with an appropriate EF Core provider
    /// based on the current hosting environment.
    /// </summary>
    /// <param name="services">The service collection to extend.</param>
    /// <param name="configuration">The configuration provider.</param>
    /// <param name="environment">The hosting environment.</param>
    private static void AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var connectionString = configuration.GetConnectionString(DbConnectionOptions.DefaultConnectionString);
            Guard.Against.NullOrWhiteSpace(connectionString);

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                // Add all registered interceptors
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                // Configure based on environment
                if (environment.IsEnvironment(DbConnectionOptions.TestEnvironmentName))
                {
                    ConfigureInMemoryDatabase(options, environment);
                }
                else if (environment.IsDevelopment())
                {
                    ConfigurePostgreSqlDatabase(options, connectionString, environment, isDevelopment: true);
                }
                else
                {
                    ConfigurePostgreSqlDatabase(options, connectionString, environment, isDevelopment: false);
                }
            });

            // Register IApplicationDbContext interface
            services.AddScoped<IApplicationDbContext>(sp => sp.GetRequiredService<ApplicationDbContext>());
            Log.Debug(LogTemplates.ServiceRegistered, nameof(ApplicationDbContext), "Scoped");

            sw.Stop();
            LogDatabaseConfiguration(environment, sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            Log.Error(ex, LogTemplates.OperationFailed, nameof(AddDatabase), ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Configures the in-memory database for testing environments.
    /// </summary>
    private static void ConfigureInMemoryDatabase(
        DbContextOptionsBuilder options,
        IHostEnvironment environment)
    {
        options.UseInMemoryDatabase(DbConnectionOptions.TestDatabaseName)
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging();

        Log.Information(
            LogTemplates.DbConnected,
            environment.EnvironmentName,
            $"InMemory-{DbConnectionOptions.TestDatabaseName}");
    }

    /// <summary>
    /// Configures PostgreSQL database with environment-specific settings.
    /// </summary>
    private static void ConfigurePostgreSqlDatabase(
        DbContextOptionsBuilder options,
        string connectionString,
        IHostEnvironment environment,
        bool isDevelopment)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            ConfigureNpgsqlOptions(npgsqlOptions);
        })
            .EnableDetailedErrors()
            .EnableSensitiveDataLogging()
            .UseSnakeCaseNamingConvention();

        Log.Information(LogTemplates.DbConnected, environment.EnvironmentName, DbConnectionOptions.Postgres);
    }

    /// <summary>
    /// Configures Npgsql-specific options including vector support, migrations, and enum mappings.
    /// </summary>
    private static void ConfigureNpgsqlOptions(
        Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder npgsqlOptions)
    {
        // Enable vector support for pgvector extension
        npgsqlOptions.UseVector();

        // Configure migrations history table
        npgsqlOptions.MigrationsHistoryTable(
            DbConnectionOptions.MigrationsHistoryTable,
            DbConnectionOptions.MigrationsSchema);

        // Configure data source with dynamic JSON support
        npgsqlOptions.ConfigureDataSource(dataSourceBuilder =>
        {
            dataSourceBuilder.EnableDynamicJson();
        });
    }

    /// <summary>
    /// Logs the final database configuration details.
    /// </summary>
    private static void LogDatabaseConfiguration(IHostEnvironment environment, double durationMs)
    {
        var provider = environment.IsProduction() || environment.IsDevelopment()
            ? DbConnectionOptions.Postgres
            : DbConnectionOptions.InMemory;

        Log.Information(
            LogTemplates.ConfigLoaded,
            "Database",
            new
            {
                Environment = environment.EnvironmentName,
                Provider = provider,
                Interceptors = true,
                SnakeCaseNaming = provider == DbConnectionOptions.Postgres,
                VectorSupport = provider == DbConnectionOptions.Postgres
            });

        Log.Debug("Database configured in {Duration:0.0000}ms", durationMs);
    }

    #endregion

    #region Unit of Work

    /// <summary>
    /// Registers the Unit of Work pattern implementation.
    /// </summary>
    private static void AddUnitOfWork(this IServiceCollection services)
    {
        services.AddScoped<IUnitOfWork, UnitOfWork>();
        Log.Debug(LogTemplates.ServiceRegistered, nameof(IUnitOfWork), "Scoped");
    }

    #endregion
}