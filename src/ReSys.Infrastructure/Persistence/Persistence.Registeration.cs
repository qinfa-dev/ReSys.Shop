using System.Diagnostics;

using Ardalis.GuardClauses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Auditing;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Domain.Catalog.Products.Reviews;
using ReSys.Core.Domain.Catalog.Properties;
using ReSys.Core.Domain.Configurations;
using ReSys.Core.Domain.Identity.Permissions;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.StorePickups;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.Payments;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Infrastructure.Persistence.Caching;
using ReSys.Infrastructure.Persistence.Contexts;
using ReSys.Infrastructure.Persistence.Interceptors;
using ReSys.Infrastructure.Persistence.Options;
using ReSys.Infrastructure.Seeders;

using Serilog;

using static ReSys.Core.Domain.Orders.Adjustments.OrderAdjustment;

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
            Guard.Against.NullOrWhiteSpace(connectionString, nameof(connectionString));

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                options.AddInterceptors(sp.GetServices<ISaveChangesInterceptor>());

                if (environment.IsEnvironment(DbConnectionOptions.TestEnvironmentName))
                {
                    ConfigureInMemoryDatabase(options);
                    Log.Information(LogTemplates.DbConnected, "Test", "InMemory-TestDb");
                }
                else if (environment.IsDevelopment())
                {
                    ConfigurePostgreSqlDatabase(options, connectionString, enableSensitiveLogging: true);
                    Log.Information(LogTemplates.DbConnected, "Development", "PostgreSQL");
                }
                else
                {
                    ConfigurePostgreSqlDatabase(options, connectionString, enableSensitiveLogging: false);
                    Log.Information(LogTemplates.DbConnected, environment.EnvironmentName, "PostgreSQL");
                }
            });

            services.AddScoped<IApplicationDbContext>(sp =>
                sp.GetRequiredService<ApplicationDbContext>());

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
    /// Configures the in-memory database provider for testing.
    /// </summary>
    private static void ConfigureInMemoryDatabase(DbContextOptionsBuilder options)
    {
        options.UseInMemoryDatabase(DbConnectionOptions.TestDatabaseName)
               .EnableDetailedErrors()
               .EnableSensitiveDataLogging();
    }

    /// <summary>
    /// Configures the PostgreSQL database provider with optimized settings.
    /// </summary>
    private static void ConfigurePostgreSqlDatabase(
        DbContextOptionsBuilder options,
        string connectionString,
        bool enableSensitiveLogging)
    {
        options.UseNpgsql(connectionString, npgsqlOptions =>
        {
            // Enable pgvector extension support
            npgsqlOptions.UseVector();

            // Configure migrations
            npgsqlOptions.MigrationsHistoryTable(
                DbConnectionOptions.MigrationsHistoryTable,
                DbConnectionOptions.MigrationsSchema);

            // Enable dynamic JSON support
            npgsqlOptions.ConfigureDataSource(dataSourceBuilder =>
            {
                dataSourceBuilder.EnableDynamicJson();
                dataSourceBuilder.MapEnum<DisplayOn>();
                dataSourceBuilder.MapEnum<AuditSeverity>();
                dataSourceBuilder.MapEnum<ProductImage.ProductImageType>();
                dataSourceBuilder.MapEnum<Product.ProductStatus>();
                dataSourceBuilder.MapEnum<Review.ReviewStatus>();
                dataSourceBuilder.MapEnum<Property.PropertyKind>();
                dataSourceBuilder.MapEnum<ConfigurationValueType>();
                dataSourceBuilder.MapEnum<AccessPermission.PermissionCategory>();
                dataSourceBuilder.MapEnum<AddressType>();
                dataSourceBuilder.MapEnum<LocationType>();
                dataSourceBuilder.MapEnum<StorePickup.PickupState>();
                dataSourceBuilder.MapEnum<AdjustmentScope>();
                dataSourceBuilder.MapEnum<Order.OrderState>();
                dataSourceBuilder.MapEnum<Payment.PaymentState>();
            });

            // Connection resilience
            // TODO: Enable if needed
            //npgsqlOptions.EnableRetryOnFailure(
            //    maxRetryCount: 3,
            //    maxRetryDelay: TimeSpan.FromSeconds(5),
            //    errorCodesToAdd: null);

            // Command timeout
            npgsqlOptions.CommandTimeout(30);
        })
            .EnableDetailedErrors()
            .UseSnakeCaseNamingConvention();

        if (enableSensitiveLogging)
        {
            options.EnableSensitiveDataLogging();
        }

        // Performance optimization: disable query splitting warnings
        options.ConfigureWarnings(warnings =>
        {
            warnings.Ignore(RelationalEventId.MultipleCollectionIncludeWarning);
        });
    }

    /// <summary>
    /// Logs the database configuration details.
    /// </summary>
    private static void LogDatabaseConfiguration(IHostEnvironment environment, double durationMs)
    {
        var provider = environment.IsEnvironment(DbConnectionOptions.TestEnvironmentName)
            ? DbConnectionOptions.InMemory
            : DbConnectionOptions.Postgres;

        Log.Information(LogTemplates.ConfigLoaded, "Database", new
        {
            Environment = environment.EnvironmentName,
            Provider = provider,
            Interceptors = true,
            SnakeCaseNaming = provider == DbConnectionOptions.Postgres,
            VectorSupport = provider == DbConnectionOptions.Postgres,
            RetryOnFailure = provider == DbConnectionOptions.Postgres
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

    #region Migration Utilities

    /// <summary>
    /// Ensures the database is created and migrations are applied.
    /// Use with caution in production environments.
    /// </summary>
    public static async Task EnsureDatabaseAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var sw = Stopwatch.StartNew();
        Log.Information("Ensuring database exists and migrations are applied...");

        try
        {
            await context.Database.MigrateAsync(cancellationToken);
            sw.Stop();

            Log.Information("Database ensured successfully in {Duration:0.0000}ms", sw.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Error(ex, "Failed to ensure database: {Error}", ex.Message);
            throw;
        }
    }

    /// <summary>
    /// Gets pending migrations that haven't been applied to the database.
    /// </summary>
    public static async Task<IEnumerable<string>> GetPendingMigrationsAsync(
        this IServiceProvider serviceProvider,
        CancellationToken cancellationToken = default)
    {
        using var scope = serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        return await context.Database.GetPendingMigrationsAsync(cancellationToken);
    }

    #endregion
}