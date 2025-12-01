using ErrorOr;

namespace ReSys.Core.Feature.Common.Notification.Models.Sms;

public partial class SmsNotificationData
{
    /// <summary>
    /// Standardized reusable validation errors for SmsNotificationData.
    /// </summary>
    public static class Errors
    {
        public static Error MissingUseCase => Error.Validation(
            code: "SmsNotification.UseCase.Missing",
            description: "Notification use case must be specified.");

        public static Error InvalidSendMethod => Error.Validation(
            code: "SmsNotification.SendMethod.Invalid",
            description: "SendMethod must be SMS for SmsNotificationData.");

        public static Error MissingSenderNumber => Error.Validation(
            code: "SmsNotification.SenderNumber.Missing",
            description: "Sender number is required for SMS notifications.");

        public static Error MissingReceivers => Error.Validation(
            code: "SmsNotification.Receivers.Missing",
            description: "At least one valid phone number is required.");

        public static Error MissingContent => Error.Validation(
            code: "SmsNotification.Content.Missing",
            description: "Content is required for SMS notifications.");

        public static Error ContentTooLong => Error.Validation(
            code: "SmsNotification.Content.TooLong",
            description: "SMS content exceeds 160 characters, which may be truncated by some carriers.");
    }
}