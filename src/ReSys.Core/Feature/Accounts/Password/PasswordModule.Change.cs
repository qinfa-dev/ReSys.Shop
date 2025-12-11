using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Password;

public static partial class PasswordModule
{
    public static class Change
    {
        #region Records
        public sealed record Param(string CurrentPassword, string NewPassword, string NewPasswordConfirm);
        public sealed record Command(Param Param) : ICommand<Updated>;
        #endregion

        #region Validators
        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.CurrentPassword)
                    .NullableRequired(prefix: nameof(User),
                        field: nameof(Param.CurrentPassword));

                var samePasswordNotAllowed = Error.Validation(
                    code: $"{nameof(User)}.SamePasswordNotAllowed",
                    description: "New password cannot be the same as the current password.");

                RuleFor(expression: x => x.NewPassword)
                    .NullableRequired(prefix: nameof(User),
                        field: nameof(Param.NewPassword))
                    .NotEqual(expression: m => m.CurrentPassword)
                    .WithErrorCode(errorCode: samePasswordNotAllowed.Code)
                    .WithMessage(errorMessage: samePasswordNotAllowed.Description);

                var confirmPasswordMismatch = Error.Validation(
                    code: $"{nameof(User)}.PasswordsDoNotMatch",
                    description: "Password and confirm password do not match.");

                RuleFor(expression: x => x.NewPasswordConfirm)
                    .NullableRequired(prefix: nameof(User),
                        field: nameof(Param.NewPasswordConfirm))
                    .Equal(expression: x => x.NewPassword)
                    .WithErrorCode(errorCode: confirmPasswordMismatch.Code)
                    .WithMessage(errorMessage: confirmPasswordMismatch.Description);
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
        public sealed class CommandHandler(UserManager<User> userManager, IUserContext userContext)
            : ICommandHandler<Command, Updated>
        {
            public async Task<ErrorOr<Updated>> Handle(Command request, CancellationToken cancellationToken)
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

                // Check: current password is correct
                Param param = request.Param;
                IdentityResult result = await userManager.ChangePasswordAsync(user: user,
                    currentPassword: param.CurrentPassword,
                    newPassword: param.NewPassword);
                if (!result.Succeeded)
                {
                    return result.Errors.ToApplicationResult(prefix: "");
                }

                return Result.Updated;
            }
        }
        #endregion
    }
}
