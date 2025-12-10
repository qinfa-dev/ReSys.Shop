using System.Diagnostics;

using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Systems.Options;

using Serilog;

namespace ReSys.Core.Feature.Common.Systems;

/// <summary>
/// Configures system-wide options including admin panel and storefront settings.
/// These options control application behavior and UI configuration.
/// </summary>
public static class SystemConfiguration
{
    #region Service Registration

    /// <summary>
    /// Registers system configuration options including admin panel and storefront settings.
    /// Options are bound from configuration and available via the IOptions pattern.
    /// </summary>
    public static IServiceCollection AddSystems(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Information(messageTemplate: "Configuring system options...");
            int optionsCount = 0;

            // --- Admin Panel Options ---
            IConfigurationSection adminPanelSection = configuration.GetSection(key: AdminPanelOption.Section);
            if (adminPanelSection.Exists())
            {
                Log.Debug(messageTemplate: LogTemplates.ConfigLoaded,
                         propertyValue0: nameof(AdminPanelOption),
                         propertyValue1: new { Section = AdminPanelOption.Section });

                services
                    .AddOptions<AdminPanelOption>()
                    .Bind(config: adminPanelSection)
                    .ValidateOnStart()
                    .Services.AddSingleton<IValidateOptions<AdminPanelOption>, SystemOptionValidator<AdminPanelOption>>();

                optionsCount++;
            }
            else
            {
                Log.Warning(messageTemplate: "Configuration section '{Section}' not found.",
                           propertyValue: AdminPanelOption.Section);
            }

            // --- Storefront Options ---
            IConfigurationSection storefrontSection = configuration.GetSection(key: StorefrontOption.Section);
            if (storefrontSection.Exists())
            {
                Log.Debug(messageTemplate: LogTemplates.ConfigLoaded,
                         propertyValue0: nameof(StorefrontOption),
                         propertyValue1: new { Section = StorefrontOption.Section });

                services
                    .AddOptions<StorefrontOption>()
                    .Bind(config: storefrontSection)
                    .ValidateOnStart()
                    .Services.AddSingleton<IValidateOptions<StorefrontOption>, SystemOptionValidator<StorefrontOption>>();

                optionsCount++;
            }
            else
            {
                Log.Warning(messageTemplate: "Configuration section '{Section}' not found.",
                           propertyValue: StorefrontOption.Section);
            }

            stopwatch.Stop();

            Log.Information(
                messageTemplate: "System options configured ({OptionCount} option groups) in {Duration:0.0000}ms",
                propertyValue0: optionsCount,
                propertyValue1: stopwatch.Elapsed.TotalMilliseconds);

            Log.Information(
                messageTemplate: LogTemplates.ConfigLoaded,
                propertyValue0: "SystemOptions",
                propertyValue1: new
                {
                    Options = new[]
                    {
                        AdminPanelOption.Section,
                        StorefrontOption.Section
                    }
                });

            return services;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Fatal(
                exception: ex,
                messageTemplate: LogTemplates.ComponentStartupFailed,
                propertyValue0: "Systems",
                propertyValue1: ex.Message);

            throw;
        }
    }

    #endregion
}