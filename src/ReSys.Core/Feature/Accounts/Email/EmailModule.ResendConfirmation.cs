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
    public static class ResendConfirmation
    {
        public sealed record Param(string? Email = null);
        public sealed record Command(Param Param) : ICommand<Result>;

        public sealed record Result(
            string ConfirmMessage)
        {
            public static Result NewEmailConfirmation => new(
                ConfirmMessage: "If your email address is registered and not yet confirmed, a confirmation email has been sent.");
        }

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.Email)
                    .MustBeValidEmail(prefix: nameof(User),
                        field: nameof(Param.Email))
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.Email));
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
           INotificationService notificationService,
           IConfiguration configuration,
           IUserContext userContext)
               : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(
                Command request, CancellationToken cancellationToken)
            {
                Param param = request.Param;
                bool isAuthenticated = userContext.IsAuthenticated;

                if (isAuthenticated)
                    return await HandleAuthenticatedUserAsync(param: param,
                        cancellationToken: cancellationToken);
                if (!isAuthenticated && !string.IsNullOrEmpty(value: param.Email))
                    return await HandleAnonymousUserAsync(param: param,
                        cancellationToken: cancellationToken);

                Log.Warning(messageTemplate: "ResendConfirmEmail: Anonymous request without email.");
                return Error.Validation(code: "ResendConfirmEmail.MissingEmail",
                    description: "Email is required for anonymous requests.");
            }

            private async Task<ErrorOr<Result>> HandleAuthenticatedUserAsync(Param param, CancellationToken cancellationToken)
            {
                string? userId = userContext.UserId;
                if (userId == null)
                    return User.Errors.Unauthorized;

                User? user = await userManager.FindByIdAsync(userId: userId);
                if (user is null)
                    return User.Errors.NotFound(credential: userId);

                // Ensure email belongs to current user
                bool isCurrentEmail = string.Equals(a: user.Email,
                    b: param.Email,
                    comparisonType: StringComparison.OrdinalIgnoreCase);
                if (!isCurrentEmail)
                {
                    Log.Warning(
                        messageTemplate: "ResendConfirmEmail: Authenticated user {UserId} attempted to resend confirmation for unauthorized email {Email}",
                        propertyValue0: userId,
                        propertyValue1: param.Email);
                    return Error.Validation(code: "ResendConfirmEmail.UnauthorizedEmail",
                        description: "You can only resend confirmation emails for your current email address.");
                }

                if (await userManager.IsEmailConfirmedAsync(user: user))
                {
                    Log.Information(
                        messageTemplate: "ResendConfirmEmail: Authenticated user {UserId} with email {Email} already confirmed.",
                        propertyValue0: userId,
                        propertyValue1: param.Email);
                    return Result.NewEmailConfirmation;
                }

                Log.Information(messageTemplate: "Resending confirmation email for authenticated user {UserId} to {Email}",
                    propertyValue0: userId,
                    propertyValue1: user.Email);

                ErrorOr<Success> sendResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    cancellationToken: cancellationToken);

                if (sendResult.IsError)
                {
                    Log.Error(
                        messageTemplate: "ResendConfirmEmail: Failed to send confirmation email to {Email} for user {UserId}. Errors: {Errors}",
                        propertyValue0: user.Email,
                        propertyValue1: userId,
                        propertyValue2: sendResult.Errors);
                    return sendResult.Errors;
                }

                return Result.NewEmailConfirmation;
            }

            private async Task<ErrorOr<Result>> HandleAnonymousUserAsync(Param param, CancellationToken cancellationToken)
            {
                User? user = await userManager.FindByEmailAsync(email: param.Email ?? string.Empty);

                if (user is null)
                {
                    Log.Information(
                        messageTemplate: "ResendConfirmEmail: Anonymous request for non-existent email {Email}.",
                        propertyValue: param.Email);
                    return Result.NewEmailConfirmation;
                }

                if (await userManager.IsEmailConfirmedAsync(user: user))
                {
                    Log.Information(
                        messageTemplate: "ResendConfirmEmail: Anonymous request for already confirmed email {Email}.",
                        propertyValue: param.Email);
                    return Result.NewEmailConfirmation;
                }

                Log.Information(messageTemplate: "Resending confirmation email for anonymous request to {Email}",
                    propertyValue: user.Email);

                ErrorOr<Success> sendResult = await userManager.GenerateAndSendConfirmationEmailAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    cancellationToken: cancellationToken);

                if (sendResult.IsError)
                {
                    Log.Error(
                        messageTemplate: "ResendConfirmEmail: Failed to send confirmation email to {Email}. Errors: {Errors}",
                        propertyValue0: user.Email,
                        propertyValue1: sendResult.Errors);
                    return sendResult.Errors;
                }

                return Result.NewEmailConfirmation;
            }
        }
    }
}