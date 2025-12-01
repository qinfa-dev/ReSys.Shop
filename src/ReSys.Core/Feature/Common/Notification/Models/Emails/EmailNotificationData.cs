using ErrorOr;

using ReSys.Core.Feature.Common.Notification.Constants;

namespace ReSys.Core.Feature.Common.Notification.Models.Emails;

/// <summary>
/// Represents the data required to send an email notification.
/// Includes recipients, content, attachments, and metadata for context.
/// </summary>
public partial class EmailNotificationData
{
    public required NotificationConstants.UseCase UseCase { get; set; }
    public List<string> Receivers { get; set; } = [];
    public List<string> Cc { get; set; } = [];
    public List<string> Bcc { get; set; } = [];
    public string Title { get; set; } = string.Empty;
    public string? Content { get; set; }
    public string? HtmlContent { get; set; }
    public string CreatedBy { get; set; } = "System";
    public List<string> Attachments { get; set; } = [];
    public DateTimeOffset? CreatedAt { get; set; }
    public NotificationConstants.PriorityLevel Priority { get; set; } = NotificationConstants.PriorityLevel.Normal;
    public NotificationConstants.SendMethod SendMethod { get; set; } = NotificationConstants.SendMethod.Email;
    public string Language { get; set; } = "en-US";

    /// <summary>
    /// Validates the <see cref="EmailNotificationData"/> instance and returns either the validated instance or a list of validation errors.
    /// </summary>
    public ErrorOr<EmailNotificationData> Validate()
    {
        List<Error> errors = [];

        if (UseCase == NotificationConstants.UseCase.None)
            errors.Add(item: Errors.MissingUseCase);

        if (SendMethod != NotificationConstants.SendMethod.Email)
            errors.Add(item: Errors.InvalidSendMethod);

        if (Receivers.All(predicate: r => string.IsNullOrWhiteSpace(value: r)))
            errors.Add(item: Errors.MissingReceivers);

        if (string.IsNullOrWhiteSpace(value: Title))
            errors.Add(item: Errors.MissingTitle);

        if (string.IsNullOrWhiteSpace(value: Content) && string.IsNullOrWhiteSpace(value: HtmlContent))
            errors.Add(item: Errors.MissingContent);

        if (errors.Any())
            return errors;

        return this;
    }
}