using System.Text.RegularExpressions;

using ErrorOr;

using Microsoft.Extensions.Options;

using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Notification.Models.Sms;

using Serilog;

using Sinch;
using Sinch.SMS;
using Sinch.SMS.Batches;
using Sinch.SMS.Batches.Send;

namespace ReSys.Infrastructure.Notifications.SMS;

public sealed class SmsSinchSenderService(IOptions<SmsOptions> smsOption, ISinchClient sinchClient) : ISmsSenderService
{
    private readonly SmsOptions _smsOption = smsOption.Value;

    public async Task<ErrorOr<Success>> AddSmsNotificationAsync(
        SmsNotificationData notificationData,
        CancellationToken cancellationToken = default)
    {
        ErrorOr<SmsNotificationData> validationResult = notificationData.Validate();
        if (validationResult.IsError)
            return validationResult.Errors;

        foreach (string recipient in notificationData.Receivers)
        {
            if (!IsValidPhoneNumber(phoneNumber: recipient))
                return Errors.InvalidPhoneNumber(phoneNumber: recipient);
        }

        try
        {
            Log.Information(messageTemplate: "Sending SMS via Sinch to {Receivers} with UseCase {UseCase}",
                propertyValue0: notificationData.Receivers,
                propertyValue1: notificationData.UseCase);

            ISinchSms smsApi = sinchClient.Sms;

            IBatch response = await smsApi.Batches.Send(request: new SendTextBatchRequest
            {
                From = string.IsNullOrWhiteSpace(value: notificationData.SenderNumber)
                        ? _smsOption.SinchConfig.SenderPhoneNumber
                        : notificationData.SenderNumber,
                To = notificationData.Receivers,
                Body = notificationData.Content
            },
                cancellationToken: cancellationToken);

            Log.Information(messageTemplate: "Sinch SMS batch sent. BatchId: {BatchId}",
                propertyValue: response.Id);

            return Result.Success;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to send SMS via Sinch");
            return Error.Failure(code: "SmsNotification.Failed",
                description: $"Failed to send SMS: {ex.Message}");
        }
    }

    private static bool IsValidPhoneNumber(string? phoneNumber)
    {
        return !string.IsNullOrWhiteSpace(value: phoneNumber)
               && PhoneFormat.IsMatch(input: phoneNumber);
    }

    public static class Errors
    {
        public static Error InvalidPhoneNumber(string phoneNumber) => Error.Validation(
            code: "SmsNotification.InvalidPhoneNumber",
            description: $"Invalid phone number: {phoneNumber}");
    }

    private static readonly Regex PhoneFormat = new(pattern: @"^\+?\d{10,15}$",
        options: RegexOptions.Compiled);
}
