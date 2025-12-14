using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Constants;
using ReSys.Infrastructure.Seeders.Contexts;
using ReSys.Infrastructure.Seeders.Orchestrators;

using Serilog;

namespace ReSys.Infrastructure.Seeders;

internal static class SeedersServiceCollectionExtensions
{
    /// <summary>
    /// Registers data seeding services and orchestrators for initial data population.
    /// </summary>
    internal static void AddDataSeeders(this IServiceCollection services)
    {
        services.AddTransient<IDataSeeder, IdentityDataSeeder>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(IdentityDataSeeder),
            propertyValue1: "Transient");

        services.AddTransient<IDataSeeder, LocationDataSeeder>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(LocationDataSeeder),
            propertyValue1: "Transient");

        services.AddTransient<IDataSeeder, CatalogOptionTypeDataSeeder>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(CatalogOptionTypeDataSeeder),
            propertyValue1: "Transient");

        services.AddTransient<IDataSeeder, CatalogPropertyDataSeeder>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(CatalogPropertyDataSeeder),
            propertyValue1: "Transient");

        services.AddTransient<IDataSeeder, CatalogTaxonomyDataSeeder>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(CatalogTaxonomyDataSeeder),
            propertyValue1: "Transient");

        services.AddHostedService<SeederOrchestrator>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(SeederOrchestrator),
            propertyValue1: "Singleton");

        Log.Information(messageTemplate: "Data seeders registered successfully.");
    }
}