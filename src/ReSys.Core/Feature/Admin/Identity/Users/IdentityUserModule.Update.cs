using MapsterMapper;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    public static class Update
    {
        public record Request : Models.Parameter;
        public record Result : Models.ListItem;
        public sealed record Command(string Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                var userIdRequired = CommonInput.Errors.Required(
                    prefix: nameof(User),
                    field: nameof(Command.Id));
                RuleFor(expression: m => m.Id)
                    .NotEmpty()
                    .WithErrorCode(errorCode: userIdRequired.Code)
                    .WithMessage(errorMessage: userIdRequired.Description);

                RuleFor(expression: x => x.Request)
                    .SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<User> userManager
        ) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken cancellationToken)
            {
                var request = command.Request;

                User? user = await userManager.FindByIdAsync(command.Id);
                if (user == null)
                {
                    return User.Errors.NotFound(command.Id);
                }

                // Check: for email uniqueness if changed
                if (user.Email != request.Email && await userManager.FindByEmailAsync(request.Email) != null)
                {
                    return User.Errors.EmailAlreadyExists(request.Email);
                }

                // Check: for username uniqueness if changed
                if (user.UserName != request.UserName && await userManager.FindByNameAsync(request.UserName) != null)
                {
                    return User.Errors.UserNameAlreadyExists(request.UserName);
                }

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                var updateResult = user.Update(
                    email: request.Email,
                    userName: request.UserName,
                    firstName: request.FirstName,
                    lastName: request.LastName,
                    dateOfBirth: request.DateOfBirth,
                    phoneNumber: request.PhoneNumber,
                    profileImagePath: request.ProfileImagePath,
                    emailConfirmed: request.EmailConfirmed,
                    phoneNumberConfirmed: request.PhoneNumberConfirmed
                );

                if (updateResult.IsError) return updateResult.Errors;

                if (!string.IsNullOrEmpty(request.Password))
                {
                    var token = await userManager.GeneratePasswordResetTokenAsync(user);
                    var passwordChangeResult = await userManager.ResetPasswordAsync(user, token, request.Password);
                    if (!passwordChangeResult.Succeeded)
                    {
                        var errors = passwordChangeResult.Errors
                            .Select(e => Error.Validation(code: e.Code, description: e.Description)).ToList();
                        return errors;
                    }
                }

                if (user.EmailConfirmed != request.EmailConfirmed)
                {
                    if (request.EmailConfirmed)
                        user.ConfirmEmail();
                    else
                    {
                        // ASP.NET Identity doesn't directly unconfirm email, so we manually set it if needed
                        user.EmailConfirmed = false;
                        user.AddDomainEvent(new User.Events.UserUpdated(user.Id));
                    }
                }

                // Update phone number confirmed status if changed and not handled by Update method
                if (user.PhoneNumberConfirmed != request.PhoneNumberConfirmed)
                {
                    if (request.PhoneNumberConfirmed)
                        user.ConfirmPhoneNumber();
                    else
                    {
                        // ASP.NET Identity doesn't directly unconfirm phone, so we manually set it if needed
                        user.PhoneNumberConfirmed = false;
                        user.AddDomainEvent(new User.Events.UserUpdated(user.Id));
                    }
                }

                var identityUpdateResult = await userManager.UpdateAsync(user);
                if (!identityUpdateResult.Succeeded)
                {
                    var errors = identityUpdateResult.Errors
                        .Select(e => Error.Validation(code: e.Code, description: e.Description)).ToList();
                    return errors;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}