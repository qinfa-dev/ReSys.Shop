using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Phone;

public static partial class PhoneModule
{
    public static class Confirm
    {
        public sealed record Param(string NewPhone, string Code);
        public sealed record Command(Param Param) : ICommand<Updated>;

        public sealed class ParamValidator : AbstractValidator<Param>
        {
            public ParamValidator()
            {
                RuleFor(expression: x => x.NewPhone)
                    .NotEmpty()
                    .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.NewPhone)).Code)
                    .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.NewPhone)).Description)
                    .MustBeValidPhone(prefix: nameof(User),
                        field: nameof(Param.NewPhone));

                RuleFor(expression: x => x.Code)
                    .NotEmpty()
                    .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.Code)).Code)
                    .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(User),
                        field: nameof(Param.Code)).Description);
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
            IUserContext userContext)
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

                Param param = request.Param;

                // Check: new phone is not already in use by another user
                IQueryable<User> existingUserQuery = userManager.Users.Where(predicate: u => u.PhoneNumber == param.NewPhone && u.Id != user.Id);
                User? existingUser = await existingUserQuery.FirstOrDefaultAsync(cancellationToken: cancellationToken);
                if (existingUser != null)
                    return User.Errors.PhoneNumberAlreadyExists(phoneNumber: param.NewPhone);

                // Verify: confirmation code and change phone number
                IdentityResult changeResult = await userManager.ChangePhoneNumberAsync(user: user,
                    phoneNumber: param.NewPhone,
                    token: param.Code);
                if (!changeResult.Succeeded)
                {
                    Serilog.Log.Information(
                        messageTemplate: "Failed to change phone number for user {UserId} to {NewPhone}: {Errors}",
                        propertyValue0: userId,
                        propertyValue1: param.NewPhone,
                        propertyValue2: string.Join(separator: ", ",
                            values: changeResult.Errors.Select(selector: e => e.Description)));
                    return changeResult.Errors.ToApplicationResult(prefix: "ChangePhoneNumber.Failed");
                }

                Serilog.Log.Information(
                    messageTemplate: "Phone number successfully changed for user {UserId} from {OldPhone} to {NewPhone}",
                    propertyValue0: userId,
                    propertyValue1: user.PhoneNumber,
                    propertyValue2: param.NewPhone);

                return Result.Updated;
            }
        }
    }
}
