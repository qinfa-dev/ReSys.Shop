using System.Diagnostics;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Storage.Services;
using ReSys.Infrastructure.Storages.Options;
using ReSys.Infrastructure.Storages.Providers;
using Serilog;

namespace ReSys.Infrastructure.Storages;

public static class StorageConfiguration
{
    // ======================================================
    // Registration
    // ======================================================
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var sw = Stopwatch.StartNew();

        try
        {
            Log.Information("Configuring storage services");

            // --------------------------------------------------
            // Bind + validate options
            // --------------------------------------------------
            services
                .AddOptions<StorageOptions>()
                .Bind(configuration.GetSection(StorageOptions.Section))
                .ValidateOnStart();

            // --------------------------------------------------
            // Resolve provider at startup
            // --------------------------------------------------
            services.AddScoped<IStorageService>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<StorageOptions>>().Value;

                Log.Information(
                    "Storage provider selected: {Provider}",
                    options.Provider);

                return options.Provider switch
                {
                    StorageProvider.Local =>
                        ActivatorUtilities.CreateInstance<LocalStorageService>(sp),

                    StorageProvider.Azure =>
                        ActivatorUtilities.CreateInstance<AzureStorageService>(sp),

                    StorageProvider.GoogleCloud =>
                        ActivatorUtilities.CreateInstance<GoogleCloudStorageService>(sp),

                    _ => throw new InvalidOperationException(
                        $"Unsupported StorageProvider: {options.Provider}")
                };
            });

            sw.Stop();

            Log.Information(
                "Storage services configured in {Duration}ms",
                sw.Elapsed.TotalMilliseconds);

            return services;
        }
        catch (Exception ex)
        {
            sw.Stop();

            Log.Fatal(
                ex,
                LogTemplates.ComponentStartupFailed,
                "Storage",
                ex.Message);

            throw;
        }
    }
}
