using ErrorOr;

namespace ReSys.Core.Feature.Common.Notification.Models.Notification;

public partial class NotificationData
{
    /// <summary>
    /// Standardized reusable validation errors for NotificationData.
    /// </summary>
    public static class Errors
    {
        public static Error NullData => Error.Validation(
            code: "Notification.NullData",
            description: "NotificationData cannot be null.");

        public static Error MissingUseCase => Error.Validation(
            code: "Notification.UseCase.Missing",
            description: "Notification use case must be specified.");

        public static Error MissingReceiver => Error.Validation(
            code: "Notification.Receivers.Missing",
            description: "At least one valid receiver is required.");

        public static Error EmptyCreatedBy => Error.Validation(
            code: "Notification.CreatedBy.Missing",
            description: "CreatedBy cannot be empty or whitespace.");

        public static Error MissingEmailTitle => Error.Validation(
            code: "Notification.Email.Title.Missing",
            description: "Title is required for Email notifications.");

        public static Error MissingEmailContent => Error.Validation(
            code: "Notification.Email.Content.Missing",
            description: "At least one of Content or HtmlContent is required for Email notifications.");

        public static Error MissingSmsContent => Error.Validation(
            code: "Notification.SMS.Content.Missing",
            description: "Content is required for SMS notifications.");

        public static Error SmsContentTooLong => Error.Validation(
            code: "Notification.SMS.Content.TooLong",
            description: "SMS content exceeds 160 characters and may be truncated.");

        public static Error InvalidEmailSender => Error.Validation(
            code: "Notification.Email.Sender.Invalid",
            description: "Sender must be a valid email address.");

        public static Error InvalidSmsSender => Error.Validation(
            code: "Notification.SMS.Sender.Invalid",
            description: "Sender must be a valid phone number.");

        public static Error NullParameters => Error.Validation(
            code: "Notification.Parameters.Null",
            description: "Parameters dictionary cannot be null.");

        public static Error InvalidReceivers => Error.Validation(
            code: "Notification.Receivers.Invalid",
            description: "At least one valid receiver is required.");

        public static Error InvalidTitle => Error.Validation(
            code: "Notification.Title.Invalid",
            description: "Title cannot be empty or whitespace.");

        public static Error InvalidContent => Error.Validation(
            code: "Notification.Content.Invalid",
            description: "Content cannot be empty or whitespace.");

        public static Error InvalidHtmlContent => Error.Validation(
            code: "Notification.HtmlContent.Invalid",
            description: "HTML content cannot be empty or whitespace.");

        public static Error InvalidCreatedBy => Error.Validation(
            code: "Notification.CreatedBy.Invalid",
            description: "CreatedBy cannot be empty or whitespace.");

        public static Error InvalidLanguage => Error.Validation(
            code: "Notification.Language.Invalid",
            description: "Language cannot be empty or whitespace.");
    }
}