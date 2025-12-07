using System.Net;
using System.Net.Mail;

using Ardalis.GuardClauses;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Notification.Interfaces;

namespace ReSys.Infrastructure.Notifications.Emails;

/// <summary>
/// Registers email notification providers and sender services.
/// </summary>
public static class EmailNotificationExtensions
{
    public static IServiceCollection AddEmailNotification(
        this IServiceCollection services,
        SmtpOptions options,
        ILogger logger)
    {
        if (!options.EnableEmailNotifications)
        {
            logger.LogWarning(message: LogTemplates.FeatureDisabled,
                args: "Email Notifications");
            services.AddSingleton<IEmailSenderService, EmptyEmailSenderService>();
            return services;
        }

        switch (options.Provider.ToLowerInvariant())
        {
            case "papercut":
            case "smtp":
                RegisterSmtp(services: services,
                    options: options,
                    logger: logger);
                break;

            case "sendgrid":
                RegisterSendGrid(services: services,
                    options: options,
                    logger: logger);
                break;

            default:
                logger.LogWarning(message: LogTemplates.UnknownProvider,
                    args:
                    [
                        "Email",
                        options.Provider
                    ]);
                services.AddSingleton<IEmailSenderService, EmptyEmailSenderService>();
                return services;
        }

        services.AddSingleton<IEmailSenderService, EmailSenderService>();
        logger.LogInformation(message: LogTemplates.ServiceRegistered,
            args:
            [
                nameof(IEmailSenderService),
                "Singleton"
            ]);

        return services;
    }

    private static void RegisterSmtp(IServiceCollection services, SmtpOptions options, ILogger logger)
    {
        Guard.Against.Null(input: options.SmtpConfig,
            parameterName: nameof(options.SmtpConfig));
        var cfg = options.SmtpConfig!;

        var smtpClient = new SmtpClient(host: cfg.Host,
            port: cfg.Port)
        {
            EnableSsl = cfg.EnableSsl,
            UseDefaultCredentials = cfg.UseDefaultCredentials,
            Credentials = cfg.UseDefaultCredentials
                ? CredentialCache.DefaultNetworkCredentials
                : new NetworkCredential(userName: cfg.Username,
                    password: cfg.Password)
        };

        services.AddFluentEmail(defaultFromEmail: options.FromEmail,
                defaultFromName: options.FromName)
                .AddSmtpSender(smtpClient: smtpClient);

        logger.LogInformation(message: LogTemplates.ExternalCallStarted,
            args:
            [
                "SMTP",
                "CONNECT",
                $"{cfg.Host}:{cfg.Port}"
            ]);
    }

    private static void RegisterSendGrid(IServiceCollection services, SmtpOptions options, ILogger logger)
    {
        Guard.Against.Null(input: options.SendGridConfig,
            parameterName: nameof(options.SendGridConfig));

        services.AddFluentEmail(defaultFromEmail: options.FromEmail,
                defaultFromName: options.FromName)
                .AddSendGridSender(apiKey: options.SendGridConfig.ApiKey);

        logger.LogInformation(message: LogTemplates.ExternalCallStarted,
            args:
            [
                "SendGrid",
                "API",
                "sendgrid.com"
            ]);
    }
}
