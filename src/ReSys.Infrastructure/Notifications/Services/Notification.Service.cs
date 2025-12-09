using ErrorOr;

using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Feature.Common.Notification.Constants;
using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Notification.Models.Notification;

namespace ReSys.Infrastructure.Notifications.Services;

internal sealed class NotificationService(
    IEmailSenderService emailSenderService,
    ISmsSenderService smsSenderService,
    IServiceScopeFactory serviceScopeFactory)
    : INotificationService
{
    public async Task<ErrorOr<Success>> AddNotificationAsync(
        NotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        if (notificationData.UseCase == NotificationConstants.UseCase.None)
            return Errors.InvalidUseCase;

        if (notificationData.Receivers.Any(predicate: r => !string.IsNullOrWhiteSpace(value: r)) != true)
            return Errors.EmptyReceivers;

        if (notificationData.SendMethodType is not (NotificationConstants.SendMethod.Email or NotificationConstants.SendMethod.SMS))
            return Errors.NotSupportedSendMethod;

        using IServiceScope scope = serviceScopeFactory.CreateScope();

        ErrorOr<NotificationData> validationResult = notificationData.Validate();
        if (validationResult.IsError)
            return validationResult.Errors;

        return await DispatchNotificationAsync(notificationData: notificationData,
            cancellationToken: cancellationToken);
    }

    private async Task<ErrorOr<Success>> DispatchNotificationAsync(
        NotificationData notificationData,
        CancellationToken cancellationToken)
    {
        return notificationData.SendMethodType switch
        {
            NotificationConstants.SendMethod.Email => await emailSenderService.AddEmailNotificationAsync(
                notificationData: notificationData.ToEmailNotificationData(),
                cancellationToken: cancellationToken
            ),

            NotificationConstants.SendMethod.SMS => await smsSenderService.AddSmsNotificationAsync(
                notificationData: notificationData.ToSmsNotificationData(),
                cancellationToken: cancellationToken
            ),

            _ => Errors.NotSupportedSendMethod
        };
    }

    public static class Errors
    {
        public static Error InvalidUseCase => Error.Validation(
            code: "NotificationService.InvalidUseCase",
            description: "Use case must be specified.");

        public static Error EmptyReceivers => Error.Validation(
            code: "NotificationService.EmptyReceivers",
            description: "At least one valid receiver required.");

        public static Error NotSupportedSendMethod => Error.Validation(
            code: "NotificationService.NotSupportedSendMethod",
            description: "The specified send method type is not supported.");

        public static Error ContactNotFound => Error.NotFound(
            code: "NotificationService.ContactNotFound",
            description: "No valid contacts found.");

        public static Error DatabaseError => Error.Unexpected(
            code: "NotificationService.DatabaseError",
            description: "Database operation failed.");
    }
}