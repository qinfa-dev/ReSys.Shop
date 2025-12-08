using System.Diagnostics;

using Ardalis.GuardClauses;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

using Serilog;

namespace ReSys.Infrastructure.Persistence;

internal static class DatabaseServiceCollectionExtensions
{
    private const string TestDbName = "TestDb";
    private const string MigrationsHistoryTable = "__EFMigrationsHistory";
    private const string MigrationsSchema = "eshopdb";

    /// <summary>
    /// Registers the application's database context with an appropriate EF Core provider
    /// based on the current hosting environment.
    /// </summary>
    internal static IServiceCollection AddDatabase(
        this IServiceCollection services,
        IConfiguration configuration,
        IHostEnvironment environment)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            var connectionString = GetConnectionString(configuration, environment);

            services.AddDbContext<ApplicationDbContext>((sp, options) =>
            {
                ConfigureInterceptors(options, sp);
                ConfigureProvider(options, environment, connectionString);
            });

            RegisterDbContextInterface(services);

            LogDatabaseConfiguration(environment, sw);

            return services;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: LogTemplates.OperationFailed,
                propertyValue0: nameof(AddDatabase),
                propertyValue1: ex.Message);
            throw;
        }
    }

    private static string GetConnectionString(IConfiguration configuration, IHostEnvironment environment)
    {
        // Test environment can work without a connection string
        if (environment.IsEnvironment(environmentName: "Test"))
        {
            return string.Empty;
        }

        var connectionString = configuration.GetConnectionString(name: "DefaultConnection");
        Guard.Against.NullOrWhiteSpace(input: connectionString,
            message: "Connection string 'DefaultConnection' is required for non-test environments");

        return connectionString;
    }

    private static void ConfigureInterceptors(DbContextOptionsBuilder options, IServiceProvider sp)
    {
        var interceptors = sp.GetServices<ISaveChangesInterceptor>();
        options.AddInterceptors(interceptors: interceptors);
    }

    private static void ConfigureProvider(
        DbContextOptionsBuilder options,
        IHostEnvironment environment,
        string connectionString)
    {
        if (environment.IsEnvironment(environmentName: "Test"))
        {
            ConfigureInMemoryDatabase(options);
        }
        else
        {
            ConfigurePostgreSql(options, connectionString, environment);
        }
    }

    private static void ConfigureInMemoryDatabase(DbContextOptionsBuilder options)
    {
        options.UseInMemoryDatabase(databaseName: TestDbName)
               .EnableDetailedErrors()
               .EnableSensitiveDataLogging();

        Log.Information(messageTemplate: LogTemplates.DbConnected,
            propertyValue0: "Test",
            propertyValue1: $"InMemory-{TestDbName}");
    }

    private static void ConfigurePostgreSql(
        DbContextOptionsBuilder options,
        string connectionString,
        IHostEnvironment environment)
    {
        options.UseNpgsql(
                connectionString: connectionString,
                npgsqlOptionsAction: npgsqlOptions =>
                {
                    ConfigureNpgsqlOptions(npgsqlOptions, environment);
                })
            .UseSnakeCaseNamingConvention();

        // Only enable detailed logging in non-production environments
        if (!environment.IsProduction())
        {
            options.EnableDetailedErrors()
                   .EnableSensitiveDataLogging();
        }

        Log.Information(messageTemplate: LogTemplates.DbConnected,
            propertyValue0: environment.EnvironmentName,
            propertyValue1: "PostgreSQL");
    }

    private static void ConfigureNpgsqlOptions(
        Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure.NpgsqlDbContextOptionsBuilder npgsqlOptions,
        IHostEnvironment environment)
    {
        // Configure vector support
        npgsqlOptions.UseVector();

        // Configure migrations history table
        npgsqlOptions.MigrationsHistoryTable(
            tableName: MigrationsHistoryTable,
            schema: MigrationsSchema);

        // Configure data source
        npgsqlOptions.ConfigureDataSource(dataSourceBuilderAction: dataSourceBuilder =>
        {
            dataSourceBuilder.EnableDynamicJson();
        });
    }

    private static void RegisterDbContextInterface(IServiceCollection services)
    {
        services.AddScoped<IApplicationDbContext>(implementationFactory: sp =>
            sp.GetRequiredService<ApplicationDbContext>());

        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(IApplicationDbContext),
            propertyValue1: "Scoped");
    }

    private static void LogDatabaseConfiguration(IHostEnvironment environment, Stopwatch sw)
    {
        sw.Stop();

        var providerName = environment.IsEnvironment("Test")
            ? "InMemory"
            : "PostgreSQL";

        Log.Information(messageTemplate: LogTemplates.ConfigLoaded,
            propertyValue0: "Database",
            propertyValue1: new
            {
                Environment = environment.EnvironmentName,
                Provider = providerName,
                Interceptors = true,
                DetailedErrors = !environment.IsProduction(),
                SensitiveDataLogging = !environment.IsProduction()
            });

        Log.Debug(messageTemplate: "Database configured in {Duration:0.0000}ms",
            propertyValue: sw.Elapsed.TotalMilliseconds);
    }
}