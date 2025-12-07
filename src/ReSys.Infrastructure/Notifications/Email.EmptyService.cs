using ErrorOr;

using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Notification.Models.Emails;

namespace ReSys.Infrastructure.Notifications;

public sealed class EmptyEmailSenderService : IEmailSenderService
{
    public Task<ErrorOr<Success>> AddEmailNotificationAsync(
        EmailNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        // Return error indicating email sender is disabled/unavailable
        return Task.FromResult<ErrorOr<Success>>(
            result: Error.Unexpected(
                code: "EmailSender.Disabled",
                description: "Email sender is not available. Email sending is disabled in this environment."));
    }
}