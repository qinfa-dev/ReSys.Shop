using ErrorOr;

using ReSys.Core.Feature.Common.Notification.Models;

namespace ReSys.Core.Feature.Common.Notification.Services;

public interface INotificationService
{
    Task<ErrorOr<Success>> AddNotificationAsync(NotificationData notification, CancellationToken cancellationToken);
}
