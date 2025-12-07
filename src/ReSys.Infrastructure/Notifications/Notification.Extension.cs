using Ardalis.GuardClauses;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Notification.Interfaces;

namespace ReSys.Infrastructure.Notifications;

/// <summary>
/// Root notification configuration — orchestrates registration of all notification providers.
/// </summary>
public static class NotificationConfiguration
{
    /// <summary>
    /// Registers all notification-related services (Email + SMS).
    /// </summary>
    public static IServiceCollection AddNotificationServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        using var provider = services.BuildServiceProvider();
        var logger = provider.GetRequiredService<ILoggerFactory>()
            .CreateLogger(categoryName: nameof(NotificationConfiguration));

        logger.LogInformation(message: LogTemplates.ModuleRegistered,
            args:
            [
                nameof(NotificationConfiguration),
                3
            ]);

        // Load configuration safely
        var smtpOptions = configuration.GetSection(key: SmtpOptions.Section).Get<SmtpOptions>();
        var smsOptions = configuration.GetSection(key: SmsOptions.Section).Get<SmsOptions>();

        Guard.Against.Null(input: smtpOptions);
        Guard.Against.Null(input: smsOptions);

        // Delegate to specialized modules
        services.AddEmailNotification(options: smtpOptions,
            logger: logger);
        services.AddSmsNotification(options: smsOptions,
            logger: logger);

        // Register main facade
        services.AddScoped<INotificationService, NotificationService>();
        logger.LogDebug(message: LogTemplates.ServiceRegistered,
            args:
            [
                nameof(INotificationService),
                "Scoped"
            ]);

        return services;
    }
}