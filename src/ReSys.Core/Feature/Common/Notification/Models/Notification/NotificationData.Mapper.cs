using ReSys.Core.Feature.Common.Notification.Constants;
using ReSys.Core.Feature.Common.Notification.Models.Emails;
using ReSys.Core.Feature.Common.Notification.Models.Sms;

namespace ReSys.Core.Feature.Common.Notification.Models.Notification;

public static class NotificationDataMapper
{
    /// <summary>
    /// Maps a NotificationData instance to an SmsNotificationData instance.
    /// </summary>
    /// <param name="notificationData">The NotificationData instance to map.</param>
    /// <returns>An SmsNotificationData instance with mapped properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown if notificationData is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if required fields are missing or invalid.</exception>
    public static SmsNotificationData ToSmsNotificationData(this NotificationData notificationData)
    {
        if (notificationData == null)
            throw new ArgumentNullException(paramName: nameof(notificationData));

        // Fetch template for default content if needed
        NotificationConstants.Template template = NotificationConstants.Templates[key: notificationData.UseCase];
        string content = notificationData.Content ?? template.TemplateContent ?? string.Empty;

        // Replace placeholders with parameter values
        if (!string.IsNullOrWhiteSpace(value: content))
        {
            foreach (KeyValuePair<NotificationConstants.Parameter, string?> param in notificationData.Values)
            {
                string placeholder = $"{{{param.Key}}}";
                content = content.Replace(oldValue: placeholder,
                    newValue: param.Value ?? string.Empty);
            }
        }

        SmsNotificationData smsData = new()
        {
            UseCase = notificationData.UseCase,
            Receivers = notificationData.Receivers.Where(predicate: r => !string.IsNullOrWhiteSpace(value: r)).Distinct().ToList(),
            Content = content,
            CreatedBy = notificationData.CreatedBy,
            CreatedAt = notificationData.CreatedAt,
            Priority = notificationData.Priority,
            Language = notificationData.Language
        };

        // Validate the resulting SMS data
        smsData.Validate();
        return smsData;
    }

    /// <summary>
    /// Maps a NotificationData instance to an EmailNotificationData instance.
    /// </summary>
    /// <param name="notificationData">The NotificationData instance to map.</param>
    /// <returns>An EmailNotificationData instance with mapped properties.</returns>
    /// <exception cref="ArgumentNullException">Thrown if notificationData is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if required fields are missing or invalid.</exception>
    public static EmailNotificationData ToEmailNotificationData(this NotificationData notificationData)
    {
        if (notificationData == null)
            throw new ArgumentNullException(paramName: nameof(notificationData));

        // Fetch template for default content if needed
        NotificationConstants.Template template = NotificationConstants.Templates[key: notificationData.UseCase];
        string title = notificationData.Title ?? template.Name;
        string content = notificationData.Content ?? template.TemplateContent ?? string.Empty;
        string? htmlContent = notificationData.HtmlContent ?? template.HtmlTemplateContent ?? string.Empty;

        // Replace placeholders with parameter values
        foreach (KeyValuePair<NotificationConstants.Parameter, string?> param in notificationData.Values)
        {
            string placeholder = $"{{{param.Key}}}";
            title = title.Replace(oldValue: placeholder,
                newValue: param.Value ?? string.Empty);
            content = content.Replace(oldValue: placeholder,
                newValue: param.Value ?? string.Empty);
            htmlContent = htmlContent?.Replace(oldValue: placeholder,
                newValue: param.Value ?? string.Empty);
        }

        EmailNotificationData emailData = new()
        {
            UseCase = notificationData.UseCase,
            Receivers = notificationData.Receivers.Where(predicate: r => !string.IsNullOrWhiteSpace(value: r)).Distinct().ToList(),
            Title = title,
            Content = content,
            HtmlContent = string.IsNullOrWhiteSpace(value: htmlContent) ? null : htmlContent,
            CreatedBy = notificationData.CreatedBy,
            CreatedAt = notificationData.CreatedAt,
            Attachments = notificationData.Attachments.Where(predicate: a => !string.IsNullOrWhiteSpace(value: a)).Distinct().ToList(),
            Priority = notificationData.Priority,
            Language = notificationData.Language
        };

        // Validate the resulting Email data
        emailData.Validate();
        return emailData;
    }
}
