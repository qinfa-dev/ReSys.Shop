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
    public static async Task<ErrorOr<Success>> GenerateAndSendPasswordResetCodeAsync(
      this UserManager<User> userManager,
      INotificationService notificationService,
      IConfiguration configuration,
      User user,
      string? clientUri = null,
      CancellationToken cancellationToken = default)
    {
        string resetCode = await userManager.GeneratePasswordResetTokenAsync(user: user);

        // Encode reset code for URL
        string encodedResetCode = WebEncoders.Base64UrlEncode(input: Encoding.UTF8.GetBytes(s: resetCode));
        string userId = await userManager.GetUserIdAsync(user: user);

        // Prepare route values
        List<KeyValuePair<string, string?>> routeValues =
        [
            new(key: "userId",
                value: userId),
            new(key: "code",
                value: encodedResetCode)
        ];

        // Check: front-end client URI fallback
        StorefrontOption? storefrontOption = configuration.GetSection(key: StorefrontOption.Section).Get<StorefrontOption>();
        if (storefrontOption == null)
            return Error.Validation(code: "Auth.StorefrontOptionNotFound",
                description: "Storefront options not found in configuration.");
        string baseUrl = clientUri ?? storefrontOption.BaseUrl;

        // Generate reset password URL
        string resetPasswordUrl = $"{baseUrl}/reset-password?{QueryString.Create(parameters: routeValues)}";

        // Determine: target email
        string? email = user.Email;

        if (string.IsNullOrWhiteSpace(value: email))
            return CommonInput.Errors.Required(prefix: nameof(NotificationConstants.UseCase.SystemActiveEmail),
                field: "Email cannot be null or empty.");

        // Prepare: notification
        ErrorOr<NotificationData> notificationDataResult = NotificationDataBuilder
            .WithUseCase(useCase: NotificationConstants.UseCase.SystemResetPassword)
            .AddParam(parameter: NotificationConstants.Parameter.SystemName,
                value: storefrontOption.SystemName)
            .AddParam(parameter: NotificationConstants.Parameter.SupportEmail,
                value: storefrontOption.SupportEmail)
            .AddParam(parameter: NotificationConstants.Parameter.ActiveUrl,
                value: HtmlEncoder.Default.Encode(value: resetPasswordUrl))
            .AddParam(parameter: NotificationConstants.Parameter.UserName,
                value: user.UserName)
            .WithReceivers(receivers: [email])
            .Build();

        if (notificationDataResult.IsError)
            return notificationDataResult.Errors;

        // Send: notification
        return await notificationService.AddNotificationAsync(notification: notificationDataResult.Value,
            cancellationToken: cancellationToken);
    }
}
