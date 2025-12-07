using Ardalis.GuardClauses;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Notification.Interfaces;

using Sinch;
using Sinch.SMS;

namespace ReSys.Infrastructure.Notifications;

/// <summary>
/// Registers SMS notification providers (e.g., Sinch).
/// </summary>
public static class SmsNotificationExtensions
{
    public static IServiceCollection AddSmsNotification(
        this IServiceCollection services,
        SmsOptions options,
        ILogger logger)
    {
        if (!options.EnableSmsNotifications)
        {
            logger.LogWarning(message: LogTemplates.FeatureDisabled,
                args: "SMS Notifications");
            services.AddSingleton<ISmsSenderService, EmptySmsSenderService>();
            return services;
        }

        Guard.Against.Null(input: options.SinchConfig,
            parameterName: nameof(options.SinchConfig));
        var cfg = options.SinchConfig;

        services.AddSingleton<ISinchClient>(implementationFactory: _ =>
        {
            logger.LogInformation(message: LogTemplates.ExternalCallStarted,
                args:
                [
                    "Sinch",
                    "INIT",
                    "SinchClient"
                ]);

            return new SinchClient(projectId: cfg.ProjectId,
                keyId: cfg.KeyId,
                keySecret: cfg.KeySecret,
                options: o =>
                {
                    if (!string.IsNullOrWhiteSpace(value: cfg.SmsRegion))
                    {
                        o.SmsRegion = cfg.SmsRegion.Trim()
                                .ToLower() switch
                        {
                            "us" => SmsRegion.Us,
                            "eu" => SmsRegion.Eu,
                            _ => o.SmsRegion
                        };
                    }
                });
        });

        services.AddSingleton<ISmsSenderService, SmsSinchSenderService>();
        logger.LogInformation(message: LogTemplates.ServiceRegistered,
            args:
            [
                nameof(ISmsSenderService),
                "Singleton"
            ]);


        logger.LogInformation(message: "SMS Notifications have been registered successfully.");

        return services;
    }
}