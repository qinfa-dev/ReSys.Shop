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

        services.AddHostedService<SeederOrchestrator>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: nameof(SeederOrchestrator),
            propertyValue1: "Singleton");

        Log.Information(messageTemplate: "Data seeders registered successfully.");
    }
}