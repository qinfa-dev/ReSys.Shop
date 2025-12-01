using ErrorOr;

namespace ReSys.Core.Feature.Common.Notification.Models.Emails;

public partial class EmailNotificationData
{
    /// <summary>
    /// Standardized reusable validation errors for EmailNotificationData.
    /// </summary>
    public static class Errors
    {
        public static Error MissingUseCase => Error.Validation(
            code: "EmailNotification.UseCase.Missing",
            description: "Notification use case must be specified.");

        public static Error InvalidSendMethod => Error.Validation(
            code: "EmailNotification.SendMethod.Invalid",
            description: "SendMethod must be Email for EmailNotificationData.");

        public static Error MissingReceivers => Error.Validation(
            code: "EmailNotification.Receivers.Missing",
            description: "At least one valid email address is required.");

        public static Error MissingTitle => Error.Validation(
            code: "EmailNotification.Title.Missing",
            description: "Title is required for email notifications.");

        public static Error MissingContent => Error.Validation(
            code: "EmailNotification.Content.Missing",
            description: "At least one of Content or HtmlContent is required for email notifications.");
    }
}