using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Notification.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

using Serilog;

namespace ReSys.Core.Feature.Accounts.Email;

public static partial class EmailModule
{
    public static class Change
    {
        public sealed record Param(
            string CurrentEmail,
            string NewEmail,
            string Password);

        public sealed record Command(Param Param) : ICommand<Result>;
        public sealed record Result(string ConfirmMessage);

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                var sameEmailNotAllowed = Error.Validation(
                    code: $"{nameof(User)}.SameEmailNotAllowed",
                    description: "New email cannot be the same as the current email.");

                RuleFor(expression: x => x.CurrentEmail)
                    .MustBeValidInput(isRequired: true,
                        prefix: nameof(User),
                        field: nameof(Param.CurrentEmail))!
                    .MustBeValidEmail(prefix: nameof(User),
                        field: nameof(Param.CurrentEmail));

                RuleFor(expression: x => x.NewEmail)
                    .MustBeValidInput(isRequired: true,
                        prefix: nameof(User),
                        field: nameof(Param.NewEmail))!
                    .NotEqual(expression: x => x.CurrentEmail)
                    .WithErrorCode(errorCode: sameEmailNotAllowed.Code)
                    .WithMessage(errorMessage: sameEmailNotAllowed.Description);

                RuleFor(expression: x => x.Password)
                    .MustBeValidInput(isRequired: true,
                        prefix: nameof(User),
                        field: nameof(Param.Password));
            }
        }
        public sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(expression: x => x.Param).SetValidator(validator: new ParamValidator());
            }
        }

        public sealed class CommandHandler(
            UserManager<User> userManager,
            IUserContext userContext,
            INotificationService notificationService,
            IConfiguration configuration)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Load: user context
                string? userId = userContext.UserId;
                bool isAuthenticated = userContext.IsAuthenticated;

                // Check: user is authenticated
                if (userId is null || !isAuthenticated)
                    return User.Errors.Unauthorized;

                // Check: user exists
                User? user = await userManager.FindByIdAsync(userId: userId);
                if (user is null)
                    return User.Errors.NotFound(credential: userId);

                Param param = request.Param;

                // Check: current password is correct (security verification)
                bool isCurrentPasswordValid = await userManager.CheckPasswordAsync(user: user,
                    password: param.Password);
                if (!isCurrentPasswordValid)
                    return User.Errors.InvalidCredentials;

                // Check: new email is not already in use by another user
                User? existingUser = await userManager.FindByEmailAsync(email: param.NewEmail);
                if (existingUser != null && existingUser.Id != user.Id)
                    return User.Errors.EmailAlreadyExists(email: param.NewEmail);

                // Send: email change confirmation to new email address
                ErrorOr<Success> sendEmailResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    newEmail: param.NewEmail,
                    cancellationToken: cancellationToken);

                if (sendEmailResult.IsError)
                {
                    // Rollback: clear pending email if email sending fails
                    await userManager.UpdateAsync(user: user);
                    Log.Warning(
                        messageTemplate: "Failed to send email change confirmation to {NewEmail} for user {UserId}: {Errors}",
                        propertyValue0: param.NewEmail,
                        propertyValue1: userId,
                        propertyValue2: string.Join(separator: ", ",
                            values: sendEmailResult.Errors.Select(selector: e => e.Description)));
                    return sendEmailResult.Errors;
                }

                Log.Information(
                    messageTemplate: "Email change initiated for user {UserId} from {CurrentEmail} to {NewEmail}",
                    propertyValue0: userId,
                    propertyValue1: user.Email,
                    propertyValue2: param.NewEmail);

                return new Result(ConfirmMessage: $"A confirmation email has been sent to {param.NewEmail}. Please check your inbox and click the confirmation link to complete the email address change.");
            }
        }
    }
}