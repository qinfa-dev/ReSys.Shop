using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Notification.Interfaces;

namespace ReSys.Core.Feature.Accounts.Password;

public static partial class PasswordModule
{
    public static class Forgot
    {
        #region Records
        public sealed record Param(string Email);
        public sealed record Command(Param Param) : ICommand<Result>;
        public sealed record Result(string Message)
        {
            public static Result Default => new(Message: "If an account with the provided email exists, a password reset link has been sent to that email address.");
        }
        #endregion

        #region Validators
        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.Email)
                    .Required(prefix: nameof(User),
                        field: nameof(Param.Email))
                    .MustBeValidEmail(prefix: nameof(User),
                        field: nameof(Param.Email));
            }
        }
        public sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(expression: x => x.Param).SetValidator(validator: new ParamValidator());
            }
        }
        #endregion

        #region CommandHandler
        public sealed class CommandHandler(
           UserManager<User> userManager,
           INotificationService notificationService,
           IConfiguration configuration) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                // Check: User existence by email
                Param param = request.Param;
                User? user = await userManager.FindByEmailAsync(email: param.Email);
                // If user does not exist, return the default message
                if (user is null)
                    return Result.Default;

                // Generate: password reset token
                ErrorOr<Success> generatedTokenResult = await userManager.GenerateAndSendPasswordResetCodeAsync(
                    notificationService: notificationService,
                    configuration: configuration,
                    user: user,
                    cancellationToken: cancellationToken);

                // Check: if token generation was successful
                if (generatedTokenResult.IsError)
                {
                    // Log the error and return the default message
                    Serilog.Log.Error(messageTemplate: "Failed to send password reset code for user {Email}: {Errors}",
                        propertyValue0: param.Email,
                        propertyValue1: generatedTokenResult.Errors);
                }

                return Result.Default;
            }
        }
        #endregion
    }
}
