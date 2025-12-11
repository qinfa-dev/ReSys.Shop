using ReSys.Core.Feature.Common.Notification.Models.Notification;

namespace ReSys.Core.Feature.Common.Notification.Interfaces;

public interface INotificationService
{
    Task<ErrorOr<Success>> AddNotificationAsync(NotificationData notification, CancellationToken cancellationToken);
}
