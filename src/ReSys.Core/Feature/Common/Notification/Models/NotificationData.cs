using System.Text.RegularExpressions;

using ErrorOr;

using ReSys.Core.Feature.Common.Notification.Constants;

namespace ReSys.Core.Feature.Common.Notification.Models;

/// <summary>
/// Represents the base model for sending notifications, regardless of delivery method (Email, SMS, Push, etc.).
/// Contains common metadata, content, and personalization values used in specific notification types.
/// </summary>
public partial class NotificationData
{
    public required NotificationConstants.UseCase UseCase { get; set; }
    public NotificationConstants.SendMethod SendMethodType { get; set; } = NotificationConstants.SendMethod.Email;
    public NotificationFormats.Enumerate TemplateFormatType { get; set; } = NotificationFormats.Enumerate.Default;
    public Dictionary<NotificationConstants.Parameter, string?> Values { get; set; } = new();
    public List<string> Receivers { get; set; } = [];
    public string? Title { get; set; }
    public string? Content { get; set; }
    public string? HtmlContent { get; set; }
    public string CreatedBy { get; set; } = "System";
    public DateTimeOffset? CreatedAt { get; set; }
    public List<string> Attachments { get; set; } = [];
    public NotificationConstants.PriorityLevel Priority { get; set; } = NotificationConstants.PriorityLevel.Normal;
    public string Language { get; set; } = "en-US";
    public string? Sender { get; set; }

    /// <summary>
    /// Validates the notification data and returns either the validated instance or a list of validation errors.
    /// </summary>
    public ErrorOr<NotificationData> Validate()
    {
        List<Error> errors = [];

        if (UseCase == NotificationConstants.UseCase.None)
            errors.Add(item: Errors.MissingUseCase);

        if (!Receivers.Any(predicate: r => !string.IsNullOrWhiteSpace(value: r)))
            errors.Add(item: Errors.MissingReceiver);

        if (string.IsNullOrWhiteSpace(value: CreatedBy))
            errors.Add(item: Errors.EmptyCreatedBy);

        if (SendMethodType == NotificationConstants.SendMethod.Email)
        {
            if (string.IsNullOrWhiteSpace(value: Title))
                errors.Add(item: Errors.MissingEmailTitle);

            if (string.IsNullOrWhiteSpace(value: Content) && string.IsNullOrWhiteSpace(value: HtmlContent))
                errors.Add(item: Errors.MissingEmailContent);

            if (!string.IsNullOrWhiteSpace(value: Sender) && !Regex.IsMatch(input: Sender,
                    pattern: @"^[^@\s]+@[^@\s]+\.[^@\s]+$"))
                errors.Add(item: Errors.InvalidEmailSender);
        }
        else if (SendMethodType == NotificationConstants.SendMethod.SMS)
        {
            if (string.IsNullOrWhiteSpace(value: Content))
                errors.Add(item: Errors.MissingSmsContent);
            else if (Content.Length > 160)
                errors.Add(item: Errors.SmsContentTooLong);

            if (!string.IsNullOrWhiteSpace(value: Sender) && !Regex.IsMatch(input: Sender,
                    pattern: @"^\+?[1-9]\d{7,14}$"))
                errors.Add(item: Errors.InvalidSmsSender);
        }

        if (errors.Any())
            return errors;

        return this;
    }
}