using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;

using Serilog;

namespace ReSys.Core.Feature.Accounts.Email;

public static partial class EmailModule
{
    public static class Confirm
    {

        public record Param(string UserId, string Code, string? ChangedEmail);
        public sealed record Command(Param Param) : ICommand<Result>;
        public sealed record Result(string ConfirmMessage);

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.UserId)
                    .NotEmpty()
                    .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.UserId)).Code)
                    .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.UserId)).Description);

                RuleFor(expression: x => x.Code)
                    .NotEmpty()
                    .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.Code)).Code)
                    .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.Code)).Description);

                RuleFor(expression: x => x.ChangedEmail)
                    .MustBeValidInput(isRequired: false,
                        prefix: nameof(User),
                        field: nameof(Param.ChangedEmail))!
                    .MustBeValidEmail(prefix: nameof(User),
                        field: nameof(Param.ChangedEmail))
                    .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.ChangedEmail));
            }
        }
        public sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(expression: x => x.Param).SetValidator(validator: new ParamValidator());
            }
        }
        public sealed class CommandHandler(UserManager<User> userManager) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                Param param = request.Param;

                // Validate: User existence
                User? user = await userManager.FindByIdAsync(userId: param.UserId);
                if (user == null)
                {
                    Log.Warning(messageTemplate: "ConfirmEmail: User {UserId} not found",
                        propertyValue: param.UserId);
                    return User.Errors.NotFound(credential: param.UserId);
                }

                // Decode: token with enhanced error handling
                ErrorOr<string> decodedTokenResult = param.Code.DecodeToken();
                if (decodedTokenResult.IsError)
                {
                    Log.Warning(messageTemplate: "ConfirmEmail: Token decoding failed for user {UserId}",
                        propertyValue: param.UserId);
                    return decodedTokenResult.Errors;
                }

                string decodedToken = decodedTokenResult.Value;

                // Determine: confirmation scenario
                return string.IsNullOrWhiteSpace(value: param.ChangedEmail)
                    ? await HandleInitialEmailConfirmationAsync(user: user,
                        decodedToken: decodedToken,
                        userId: param.UserId)
                    : await HandleEmailChangeConfirmationAsync(user: user,
                        decodedToken: decodedToken,
                        changedEmail: param.ChangedEmail,
                        userId: param.UserId);
            }

            /// <summary>
            /// Handles initial email confirmation after registration
            /// </summary>
            private async Task<ErrorOr<Result>> HandleInitialEmailConfirmationAsync(
                User user,
                string decodedToken,
                string userId)
            {
                // Check: Email already confirmed
                if (await userManager.IsEmailConfirmedAsync(user: user))
                {
                    Log.Information(messageTemplate: "ConfirmEmail: User {UserId} email already confirmed",
                        propertyValue: userId);
                    return new Result(ConfirmMessage: "Your email address is already confirmed.");
                }

                // Confirm: email address
                IdentityResult result = await userManager.ConfirmEmailAsync(user: user,
                    token: decodedToken);
                if (!result.Succeeded)
                {
                    Log.Warning(messageTemplate: "ConfirmEmail: Initial confirmation failed for user {UserId}: {Errors}",
                        propertyValue0: userId,
                        propertyValue1: string.Join(separator: ", ",
                            values: result.Errors.Select(selector: e => e.Description)));
                    return result.Errors.ToApplicationResult(prefix: nameof(Confirm),
                        fallbackCode: "ConfirmEmailFailed");
                }

                Log.Information(messageTemplate: "ConfirmEmail: Initial email confirmed for user {UserId}",
                    propertyValue: userId);
                return new Result(ConfirmMessage: "Thank you for confirming your email address. Your account is now active.");
            }

            /// <summary>
            /// Handles email change confirmation
            /// </summary>
            private async Task<ErrorOr<Result>> HandleEmailChangeConfirmationAsync(
                User user,
                string decodedToken,
                string changedEmail,
                string userId)
            {
                // Additional Security: Check if target email is already in use
                User? existingUser = await userManager.FindByEmailAsync(email: changedEmail);
                if (existingUser != null && existingUser.Id != user.Id)
                {
                    Log.Warning(
                        messageTemplate: "ConfirmEmail: Email change blocked for user {UserId} - email {Email} already in use by user {ExistingUserId}",
                        propertyValue0: userId,
                        propertyValue1: changedEmail,
                        propertyValue2: existingUser.Id);

                    return User.Errors.EmailAlreadyExists(email: changedEmail);
                }

                // Storefront original email for logging
                string? originalEmail = user.Email;

                // Execute: email change
                IdentityResult changeResult = await userManager.ChangeEmailAsync(user: user,
                    newEmail: changedEmail,
                    token: decodedToken);
                if (!changeResult.Succeeded)
                {
                    Log.Warning(messageTemplate: "ConfirmEmail: Email change failed for user {UserId}: {Errors}",
                        propertyValue0: userId,
                        propertyValue1: string.Join(separator: ", ",
                            values: changeResult.Errors.Select(selector: e => e.Description)));
                    return changeResult.Errors.ToApplicationResult(prefix: nameof(Confirm),
                        fallbackCode: "ChangeEmailFailed");
                }

                // Update: username to match new email if it was previously the same
                await UpdateUsernameIfNeededAsync(user: user,
                    originalEmail: originalEmail,
                    newEmail: changedEmail,
                    userId: userId);

                Log.Information(
                    messageTemplate: "ConfirmEmail: Email successfully changed for user {UserId} from {OldEmail} to {NewEmail}",
                    propertyValue0: userId,
                    propertyValue1: originalEmail,
                    propertyValue2: changedEmail);

                return new Result(ConfirmMessage: "Your email address has been successfully changed. Please use your new email address for future logins.");
            }

            /// <summary>
            /// Updates username to match new email if it was previously the same as the old email
            /// </summary>
            private async Task UpdateUsernameIfNeededAsync(User user, string? originalEmail, string newEmail, string userId)
            {
                if (originalEmail != null && string.Equals(a: user.UserName,
                        b: originalEmail,
                        comparisonType: StringComparison.OrdinalIgnoreCase))
                {
                    IdentityResult setUserNameResult = await userManager.SetUserNameAsync(user: user,
                        userName: newEmail);
                    if (!setUserNameResult.Succeeded)
                    {
                        Log.Warning(messageTemplate: "ConfirmEmail: Failed to update username for user {UserId}: {Errors}",
                            propertyValue0: userId,
                            propertyValue1: string.Join(separator: ", ",
                                values: setUserNameResult.Errors.Select(selector: e => e.Description)));
                    }
                    else
                    {
                        Log.Information(
                            messageTemplate: "ConfirmEmail: Username updated to match new email for user {UserId}",
                            propertyValue: userId);
                    }
                }
            }
        }
    }
}