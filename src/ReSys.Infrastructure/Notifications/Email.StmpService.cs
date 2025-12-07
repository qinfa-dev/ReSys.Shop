using System.Net.Mail;

using ErrorOr;

using FluentEmail.Core;
using FluentEmail.Core.Models;

using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.Options;

using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Notification.Models.Emails;

using Serilog;

using Attachment = FluentEmail.Core.Models.Attachment;

namespace ReSys.Infrastructure.Notifications;

public sealed class EmailSenderService(
    IOptions<SmtpOptions> emailSettings,
    IFluentEmail fluentEmail)
    : IEmailSenderService
{
    private readonly SmtpOptions _emailOption = emailSettings.Value;

    public async Task<ErrorOr<Success>> AddEmailNotificationAsync(EmailNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        try
        {
            ErrorOr<EmailNotificationData> validationResult = notificationData.Validate();
            if (validationResult.IsError)
                return validationResult.Errors;

            foreach (string recipient in notificationData.Receivers)
            {
                if (!IsValidEmail(email: recipient))
                    return Errors.InvalidEmail(email: recipient);
            }

            if (notificationData.Attachments.Count != 0)
            {
                int maxSizeInBytes = _emailOption.MaxAttachmentSize ?? 25 * 1024 * 1024; // Default to 25MB
                List<string> missingAttachments = notificationData.Attachments.Where(predicate: a => !File.Exists(path: a)).ToList();
                if (missingAttachments.Any())
                    return Errors.InvalidAttachments(missingAttachments: missingAttachments);

                foreach (string attachment in notificationData.Attachments)
                {
                    FileInfo fileInfo = new(fileName: attachment);
                    if (fileInfo.Length > maxSizeInBytes)
                        return Errors.AttachmentSize(attachment: attachment,
                            maxSizeInBytes: maxSizeInBytes);
                }
            }

            IFluentEmail? email = fluentEmail
                .SetFrom(emailAddress: _emailOption.FromEmail,
                    name: _emailOption.FromName)
                .To(mailAddresses: notificationData.Receivers.Select(selector: m => new Address(emailAddress: m)))
                .Subject(subject: notificationData.Title)
                .PlaintextAlternativeBody(body: notificationData.Content)
                .Body(body: notificationData.HtmlContent,
                    isHtml: true);

            if (notificationData.Attachments.Count != 0)
            {
                FileExtensionContentTypeProvider contentTypeProvider = new();
                foreach (string attachmentPath in notificationData.Attachments)
                {
                    byte[] attachmentBytes = await File.ReadAllBytesAsync(path: attachmentPath,
                        cancellationToken: cancellationToken);
                    email.Attach(attachment: new Attachment
                    {
                        Filename = Path.GetFileName(path: attachmentPath),
                        Data = new MemoryStream(buffer: attachmentBytes),
                        ContentType = contentTypeProvider.TryGetContentType(subpath: attachmentPath,
                            contentType: out string? contentType)
                            ? contentType
                            : "application/octet-stream"
                    });
                }
            }

            Log.Information(
                messageTemplate: "Sending email notification with UseCase: {UseCase}, Priority: {Priority}, Language: {Language} to {Receivers}",
                propertyValues:
                [
                    notificationData.UseCase,
                    notificationData.Priority,
                    notificationData.Language,
                    notificationData.Receivers
                ]);

            SendResponse? sendResult = await email.SendAsync(token: cancellationToken);
            if (!sendResult.Successful)
            {
                Log.Error(messageTemplate: "Failed to send email notification. Errors: {Errors}",
                    propertyValue: sendResult.ErrorMessages);
                return Errors.SendFailed(errorMessages: sendResult.ErrorMessages);
            }

            Log.Information(messageTemplate: "Email notification sent successfully to {Receivers}",
                propertyValue: notificationData.Receivers);
            return Result.Success;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Exception occurred while sending email notification.");
            return Errors.ExceptionOccurred(ex: ex);
        }
    }

    private static bool IsValidEmail(string email)
    {
        try
        {
            MailAddress addr = new(address: email);
            return addr.Address == email;
        }
        catch
        {
            return false;
        }
    }

    public static class Errors
    {
        public static Error InvalidEmail(string email) => Error.Validation(
            code: "EmailNotification.InvalidEmail",
            description: $"Invalid email address: {email}");

        public static Error InvalidAttachments(List<string> missingAttachments) => Error.Validation(
            code: "EmailNotification.InvalidAttachments",
            description: $"The following attachments were not found: {string.Join(separator: ", ", values: missingAttachments)}");

        public static Error AttachmentSize(string attachment, long maxSizeInBytes) => Error.Validation(
            code: "EmailNotification.AttachmentSize",
            description: $"Attachment {attachment} exceeds the maximum size of {maxSizeInBytes / 1024 / 1024}MB.");

        public static Error SendFailed(IList<string> errorMessages) => Error.Unexpected(
            code: "EmailNotification.SendFailed",
            description: $"Failed to send email: {string.Join(separator: ", ", values: errorMessages)}");

        public static Error ExceptionOccurred(Exception ex) => Error.Unexpected(
            code: "EmailNotification.ExceptionOccurred",
            description: $"An exception occurred while sending email: {ex.Message}");
    }
}