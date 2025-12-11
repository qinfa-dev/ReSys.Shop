using ReSys.Core.Feature.Common.Notification.Constants;

namespace ReSys.Core.Feature.Common.Notification.Models.Sms;

/// <summary>
/// Represents the data required to send an SMS notification.
/// Includes recipients, content, metadata, and parameters for template replacement.
/// </summary>
public partial class SmsNotificationData
{
    public required NotificationConstants.UseCase UseCase { get; set; }
    public List<string> Receivers { get; set; } = [];
    public string Content { get; set; } = string.Empty;
    public string SenderNumber { get; set; } = string.Empty;
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset? CreatedAt { get; set; }
    public NotificationConstants.PriorityLevel Priority { get; set; } = NotificationConstants.PriorityLevel.Normal;
    public NotificationConstants.SendMethod SendMethod { get; set; } = NotificationConstants.SendMethod.SMS;
    public string Language { get; set; } = "en-US";
    public bool IsUnicode { get; set; }
    public string? TrackingId { get; set; }

    /// <summary>
    /// Validates the <see cref="SmsNotificationData"/> instance and returns either the validated instance or a list of validation errors.
    /// </summary>
    public ErrorOr<SmsNotificationData> Validate()
    {
        List<Error> errors = [];

        if (UseCase == NotificationConstants.UseCase.None)
            errors.Add(item: Errors.MissingUseCase);

        if (SendMethod != NotificationConstants.SendMethod.SMS)
            errors.Add(item: Errors.InvalidSendMethod);

        if (string.IsNullOrWhiteSpace(value: SenderNumber))
            errors.Add(item: Errors.MissingSenderNumber);

        if (!Receivers.Any(predicate: r => !string.IsNullOrWhiteSpace(value: r)))
            errors.Add(item: Errors.MissingReceivers);

        if (string.IsNullOrWhiteSpace(value: Content))
            errors.Add(item: Errors.MissingContent);
        else if (Content.Length > 160 && !IsUnicode)
            errors.Add(item: Errors.ContentTooLong);

        if (errors.Any())
            return errors;

        return this;
    }
}