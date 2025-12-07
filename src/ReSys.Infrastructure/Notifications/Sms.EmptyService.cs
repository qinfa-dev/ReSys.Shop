using ErrorOr;

using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Notification.Models.Sms;

using Serilog;

namespace ReSys.Infrastructure.Notifications;

public sealed class EmptySmsSenderService : ISmsSenderService
{
    public async Task<ErrorOr<Success>> AddSmsNotificationAsync(
        SmsNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        Log.Warning(messageTemplate: "SMS sending is unavailable or disabled. Notification not sent. UseCase: {UseCase}, Receivers: {Receivers}",
            propertyValue0: notificationData.UseCase,
            propertyValue1: notificationData.Receivers);
        await Task.CompletedTask;
        return Error.Unexpected(
            code: "SmsNotification.Unavailable",
            description: "SMS sending is currently unavailable or disabled.");
    }
}