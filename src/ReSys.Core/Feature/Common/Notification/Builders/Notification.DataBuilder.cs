using ErrorOr;

using ReSys.Core.Feature.Common.Notification.Constants;
using ReSys.Core.Feature.Common.Notification.Models;
using ReSys.Core.Feature.Common.Notification.Models.Emails;
using ReSys.Core.Feature.Common.Notification.Models.Notification;
using ReSys.Core.Feature.Common.Notification.Models.Sms;

namespace ReSys.Core.Feature.Common.Notification.Builders;

public static class NotificationDataBuilder
{
    public static ErrorOr<NotificationData> WithUseCase(NotificationConstants.UseCase useCase = NotificationConstants.UseCase.None)
    {
        NotificationConstants.Template? template = NotificationConstants.Templates.GetValueOrDefault(key: useCase);
        NotificationData notificationData = new()
        {
            UseCase = useCase,
            SendMethodType = GetDefaultSendMethod(useCase: useCase),
            TemplateFormatType = template?.TemplateFormatType ?? NotificationFormats.Enumerate.Default,
            Content = template?.TemplateContent,
            HtmlContent = template?.HtmlTemplateContent,
            Title = template?.Name,
            Values = new Dictionary<NotificationConstants.Parameter, string?>(),
            Receivers = [],
            Attachments = []
        };

        return notificationData;
    }

    public static ErrorOr<NotificationData> WithUseCase(this ErrorOr<NotificationData> result, NotificationConstants.UseCase useCase = NotificationConstants.UseCase.None)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        NotificationConstants.Template? template = NotificationConstants.Templates.GetValueOrDefault(key: useCase);
        notificationData.UseCase = useCase;
        notificationData.SendMethodType = GetDefaultSendMethod(useCase: useCase);
        notificationData.TemplateFormatType = template?.TemplateFormatType ?? NotificationFormats.Enumerate.Default;
        notificationData.Content = template?.TemplateContent ?? notificationData.Content;
        notificationData.HtmlContent = template?.HtmlTemplateContent ?? notificationData.HtmlContent;
        notificationData.Title = template?.Name ?? notificationData.Title;

        return notificationData;
    }

    public static ErrorOr<NotificationData> WithSendMethodType(this ErrorOr<NotificationData> result, NotificationConstants.SendMethod sendMethodType)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        notificationData.SendMethodType = sendMethodType;

        return notificationData;
    }

    public static ErrorOr<NotificationData> AddParam(this ErrorOr<NotificationData> result, NotificationConstants.Parameter parameter, string? value)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        notificationData.Values[key: parameter] = value;

        return notificationData;
    }

    public static ErrorOr<NotificationData> AddParams(this ErrorOr<NotificationData> result, Dictionary<NotificationConstants.Parameter, string?>? values)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (values == null)
            return NotificationData.Errors.NullParameters;

        foreach (KeyValuePair<NotificationConstants.Parameter, string?> item in values)
        {
            notificationData.Values[key: item.Key] = item.Value;
        }
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithReceivers(this ErrorOr<NotificationData> result, List<string>? receivers)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (receivers == null || receivers.All(predicate: r => string.IsNullOrWhiteSpace(value: r)))
            return notificationData;

        List<string> uniqueReceivers = receivers.Where(predicate: r => !string.IsNullOrWhiteSpace(value: r) && !notificationData.Receivers.Contains(item: r)).ToList();
        notificationData.Receivers.AddRange(collection: uniqueReceivers);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithReceiver(this ErrorOr<NotificationData> result, string? receiver)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(value: receiver))
            return notificationData;

        if (!notificationData.Receivers.Contains(item: receiver))
            notificationData.Receivers.Add(item: receiver);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithTitle(this ErrorOr<NotificationData> result, string? title)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(value: title))
            return NotificationData.Errors.InvalidTitle;

        notificationData.Title = title;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithContent(this ErrorOr<NotificationData> result, string? content)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(value: content))
            return NotificationData.Errors.InvalidContent;

        notificationData.Content = content;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithHtmlContent(this ErrorOr<NotificationData> result, string? htmlContent)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(value: htmlContent))
            return NotificationData.Errors.InvalidHtmlContent;

        notificationData.HtmlContent = htmlContent;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithCreatedBy(this ErrorOr<NotificationData> result, string? createdBy)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(value: createdBy))
            return NotificationData.Errors.InvalidCreatedBy;

        notificationData.CreatedBy = createdBy;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithAttachments(this ErrorOr<NotificationData> result, List<string>? attachments)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (attachments == null || attachments.All(predicate: a => string.IsNullOrWhiteSpace(value: a)))
            return notificationData;

        List<string> uniqueAttachments = attachments.Where(predicate: a => !string.IsNullOrWhiteSpace(value: a) && !notificationData.Attachments.Contains(item: a)).ToList();
        notificationData.Attachments.AddRange(collection: uniqueAttachments);
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithPriority(this ErrorOr<NotificationData> result, NotificationConstants.PriorityLevel priority)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        notificationData.Priority = priority;
        return notificationData;
    }

    public static ErrorOr<NotificationData> WithLanguage(this ErrorOr<NotificationData> result, string? language)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(value: language))
            return NotificationData.Errors.InvalidLanguage;

        notificationData.Language = language;
        return notificationData;
    }

    public static ErrorOr<NotificationData> SetCreatedBy(this ErrorOr<NotificationData> result, string? createdBy, DateTimeOffset? createAt = null)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        if (string.IsNullOrWhiteSpace(value: createdBy))
            return NotificationData.Errors.InvalidCreatedBy;

        notificationData.CreatedBy = createdBy;
        notificationData.CreatedAt = createAt;
        return notificationData;
    }

    public static ErrorOr<NotificationData> Build(this ErrorOr<NotificationData> result)
    {
        if (result.IsError)
            return result.Errors;
        NotificationData notificationData = result.Value;

        return notificationData.Validate();
    }

    public static ErrorOr<SmsNotificationData> CreateSmsNotificationData(
        NotificationConstants.UseCase useCase,
        List<string>? receivers,
        Dictionary<NotificationConstants.Parameter, string?>? parameters,
        string senderNumber = "")
    {
        if (receivers == null || receivers.All(predicate: r => string.IsNullOrWhiteSpace(value: r)))
            return SmsNotificationData.Errors.MissingReceivers;
        if (parameters == null)
            return NotificationData.Errors.NullParameters;

        NotificationConstants.Template? template = NotificationConstants.Templates.GetValueOrDefault(key: useCase);
        string content = template?.TemplateContent ?? string.Empty;

        SmsNotificationData smsData = new()
        {
            UseCase = useCase,
            Receivers = receivers.Where(predicate: r => !string.IsNullOrWhiteSpace(value: r)).Distinct().ToList(),
            Content = content,
            SenderNumber = senderNumber,
            Priority = useCase == NotificationConstants.UseCase.System2FaOtp
                ? NotificationConstants.PriorityLevel.High
                : NotificationConstants.PriorityLevel.Normal
        };

        if (!string.IsNullOrWhiteSpace(value: content))
        {
            foreach (KeyValuePair<NotificationConstants.Parameter, string?> param in parameters)
            {
                string placeholder = $"{{{param.Key}}}";
                smsData.Content = smsData.Content.Replace(oldValue: placeholder,
                    newValue: param.Value ?? string.Empty);
            }
        }

        return smsData.Validate();
    }

    public static ErrorOr<EmailNotificationData> CreateEmailNotificationData(
        NotificationConstants.UseCase useCase,
        List<string>? receivers,
        Dictionary<NotificationConstants.Parameter, string?>? parameters)
    {
        if (receivers == null || receivers.All(predicate: r => string.IsNullOrWhiteSpace(value: r)))
            return EmailNotificationData.Errors.MissingReceivers;
        if (parameters == null)
            return NotificationData.Errors.NullParameters;

        NotificationConstants.Template? template = NotificationConstants.Templates.GetValueOrDefault(key: useCase);
        string title = template?.Name ?? useCase.ToString();
        string content = template?.TemplateContent ?? string.Empty;
        string htmlContent = template?.HtmlTemplateContent ?? string.Empty;

        EmailNotificationData emailData = new()
        {
            UseCase = useCase,
            Receivers = receivers.Where(predicate: r => !string.IsNullOrWhiteSpace(value: r)).Distinct().ToList(),
            Title = title,
            Content = content,
            HtmlContent = htmlContent,
            Attachments = [],
            Priority = useCase == NotificationConstants.UseCase.SystemResetPassword ? NotificationConstants.PriorityLevel.High : NotificationConstants.PriorityLevel.Normal
        };

        if (!string.IsNullOrWhiteSpace(value: content))
        {
            foreach (KeyValuePair<NotificationConstants.Parameter, string?> param in parameters)
            {
                string placeholder = $"{{{param.Key}}}";
                emailData.Content = emailData.Content.Replace(oldValue: placeholder,
                    newValue: param.Value ?? string.Empty);
                emailData.HtmlContent = emailData.HtmlContent?.Replace(oldValue: placeholder,
                    newValue: param.Value ?? string.Empty);
                emailData.Title = emailData.Title.Replace(oldValue: placeholder,
                    newValue: param.Value ?? string.Empty);
            }
        }

        return emailData.Validate();
    }

    public static ErrorOr<NotificationData> CreateNotificationData(
        NotificationConstants.UseCase useCase,
        List<string>? receivers,
        Dictionary<NotificationConstants.Parameter, string?>? parameters)
    {
        if (receivers == null || receivers.All(predicate: r => string.IsNullOrWhiteSpace(value: r)))
            return NotificationData.Errors.InvalidReceivers;
        if (parameters == null)
            return NotificationData.Errors.NullParameters;

        NotificationConstants.Template? template = NotificationConstants.Templates.GetValueOrDefault(key: useCase);
        NotificationData notificationData = new()
        {
            UseCase = useCase,
            SendMethodType = template?.SendMethodType ?? GetDefaultSendMethod(useCase: useCase),
            TemplateFormatType = template?.TemplateFormatType ?? NotificationFormats.Enumerate.Default,
            Content = template?.TemplateContent,
            HtmlContent = template?.HtmlTemplateContent,
            Title = template?.Name ?? useCase.ToString(),
            Receivers = receivers.Where(predicate: r => !string.IsNullOrWhiteSpace(value: r)).Distinct().ToList(),
            Values = new Dictionary<NotificationConstants.Parameter, string?>(dictionary: parameters),
            Attachments = [],
            Priority = GetDefaultPriority(useCase: useCase)
        };

        if (template?.ParamValues != null)
        {
            foreach (NotificationConstants.Parameter requiredParam in template.ParamValues)
            {
                notificationData.Values.TryAdd(key: requiredParam,
                    value: null);
            }
        }

        return notificationData.Validate();
    }

    private static NotificationConstants.SendMethod GetDefaultSendMethod(NotificationConstants.UseCase useCase)
    {
        return useCase switch
        {
            NotificationConstants.UseCase.System2FaOtp => NotificationConstants.SendMethod.SMS,
            NotificationConstants.UseCase.FlashSaleNotification => NotificationConstants.SendMethod.PushNotification,
            NotificationConstants.UseCase.BackInStockNotification => NotificationConstants.SendMethod.PushNotification,
            _ => NotificationConstants.SendMethod.Email
        };
    }

    private static NotificationConstants.PriorityLevel GetDefaultPriority(NotificationConstants.UseCase useCase)
    {
        return useCase switch
        {
            NotificationConstants.UseCase.System2FaOtp => NotificationConstants.PriorityLevel.High,
            NotificationConstants.UseCase.SystemResetPassword => NotificationConstants.PriorityLevel.High,
            NotificationConstants.UseCase.FlashSaleNotification => NotificationConstants.PriorityLevel.High,
            _ => NotificationConstants.PriorityLevel.Normal
        };
    }
}