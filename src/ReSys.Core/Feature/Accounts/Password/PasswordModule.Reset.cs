using MediatR;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;

namespace ReSys.Core.Feature.Accounts.Password;

public static partial class PasswordModule
{
    public static class Reset
    {
        public sealed record Param(string Email, string ResetCode, string NewPassword);
        public sealed record Command(Param Param) : ICommand<Result>;
        public sealed record Result(string Message)
        {
            public static Result Default => new(Message: "Password has been successfully reset.");
        }

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.Email)
                    .MustBeValidEmail(prefix: nameof(User),
                        field: nameof(Param.Email));

                RuleFor(expression: x => x.ResetCode)
                    .MustBeValidInput(isRequired: true,
                        prefix: nameof(User),
                        field: nameof(Param.ResetCode));

                RuleFor(expression: x => x.NewPassword)
                    .MustBeValidInput(isRequired: true,
                        prefix: nameof(User),
                        field: nameof(Param.NewPassword));
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
           UserManager<User> userManager)
        : IRequestHandler<Command, ErrorOr<Result>>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                Param param = request.Param;
                // Check: user exists by email
                User? user = await userManager.FindByEmailAsync(email: param.Email);
                if (user == null)
                    return User.Errors.InvalidToken;

                // Decode: reset code
                ErrorOr<string> decodeResult = param.ResetCode.DecodeToken();
                if (decodeResult.IsError)
                    return decodeResult.Errors;

                // Reset: password
                IdentityResult result = await userManager.ResetPasswordAsync(user: user,
                    token: decodeResult.Value,
                    newPassword: param.NewPassword);
                if (!result.Succeeded)
                    return result.Errors.ToApplicationResult(prefix: "ResetPasswordFailed");

                return Result.Default;
            }
        }
    }
}
