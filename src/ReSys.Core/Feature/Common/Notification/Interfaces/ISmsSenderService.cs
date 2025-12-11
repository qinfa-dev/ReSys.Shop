using ReSys.Core.Feature.Common.Notification.Models.Sms;

namespace ReSys.Core.Feature.Common.Notification.Interfaces;

public interface ISmsSenderService
{
    public Task<ErrorOr<Success>> AddSmsNotificationAsync(
        SmsNotificationData notificationData, 
        CancellationToken cancellationToken = default);
}
