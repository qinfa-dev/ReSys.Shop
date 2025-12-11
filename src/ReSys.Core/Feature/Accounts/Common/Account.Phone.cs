using System.Text;
using System.Text.Encodings.Web;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Configuration;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Notification.Builders;
using ReSys.Core.Feature.Common.Notification.Constants;
using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Notification.Models.Notification;
using ReSys.Core.Feature.Common.Systems.Options;

namespace ReSys.Core.Feature.Accounts.Common;

public static partial class Account
{
    public static async Task<ErrorOr<Success>> GenerateAndSendConfirmationSmsAsync(
      this UserManager<User> userManager,
      INotificationService notificationService,
      IConfiguration configuration,
      User user,
      string? newPhoneNumber = null,
      string? clientUri = null,
      CancellationToken cancellationToken = default)
    {
        // Generate confirmation token
        string code = !string.IsNullOrWhiteSpace(value: newPhoneNumber)
            ? await userManager.GenerateChangePhoneNumberTokenAsync(user: user,
                phoneNumber: newPhoneNumber)
            : await userManager.GenerateUserTokenAsync(user: user,
                tokenProvider: TokenOptions.DefaultPhoneProvider,
                purpose: "phone-confirmation");

        // Encode token for URL
        code = WebEncoders.Base64UrlEncode(input: Encoding.UTF8.GetBytes(s: code));
        string userId = await userManager.GetUserIdAsync(user: user);

        // Prepare route values
        List<KeyValuePair<string, string?>> routeValues =
        [
            new(key: "userId",
                value: userId),
            new(key: "code",
                value: code)
        ];

        if (!string.IsNullOrWhiteSpace(value: newPhoneNumber))
        {
            routeValues.Add(item: new(key: "changedPhoneNumber",
                value: newPhoneNumber));
        }

        // Check: front-end client URI fallback
        StorefrontOption? storefrontOption = configuration.GetSection(key: StorefrontOption.Section).Get<StorefrontOption>();
        if (storefrontOption == null)
            return Error.Validation(code: "Auth.StorefrontOptionNotFound",
                description: "Storefront options not found in configuration.");
        string baseUrl = clientUri ?? storefrontOption.BaseUrl;

        // Generate: confirmation URL
        string confirmPhoneUrl = $"{baseUrl}/confirm-phone?{QueryString.Create(parameters: routeValues)}";

        // Determine: target phone number
        string? phoneNumber = newPhoneNumber ?? user.PhoneNumber;

        if (string.IsNullOrWhiteSpace(value: phoneNumber))
            return Error.Validation(code: "Account.PhoneNumberNotFound",
                description: "User does not have a phone number to confirm.");

        // Prepare notification
        ErrorOr<NotificationData> notificationDataResult = NotificationDataBuilder
            .WithUseCase(useCase: NotificationConstants.UseCase.SystemActivePhone)
            .AddParam(parameter: NotificationConstants.Parameter.SystemName,
                value: storefrontOption.SystemName)
            .AddParam(parameter: NotificationConstants.Parameter.SupportEmail,
                value: storefrontOption.SupportEmail)
            .AddParam(parameter: NotificationConstants.Parameter.ActiveUrl,
                value: HtmlEncoder.Default.Encode(value: confirmPhoneUrl))
            .AddParam(parameter: NotificationConstants.Parameter.UserName,
                value: user.UserName)
            .WithReceivers(receivers: [phoneNumber])
            .Build();


        if (notificationDataResult.IsError)
            return notificationDataResult.Errors;

        // Send notification
        return await notificationService.AddNotificationAsync(notification: notificationDataResult.Value,
            cancellationToken: cancellationToken);
    }
}
