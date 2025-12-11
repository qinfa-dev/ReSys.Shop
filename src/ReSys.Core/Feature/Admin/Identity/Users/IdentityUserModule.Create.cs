using MapsterMapper;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    public static class Create
    {
        public sealed record Request : Models.Parameter;
        public sealed record Result : Models.ListItem;

        public sealed record Command(Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Request)
                    .SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper,
            UserManager<User> userManager
        ) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken cancellationToken)
            {
                var request = command.Request;

                // Check: if email already exists
                if (await userManager.FindByEmailAsync(request.Email) != null)
                {
                    return User.Errors.EmailAlreadyExists(request.Email);
                }

                // Check: if username already exists
                if (await userManager.FindByNameAsync(request.UserName) != null)
                {
                    return User.Errors.UserNameAlreadyExists(request.UserName);
                }

                var userCreationResult = User.Create(
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

                if (userCreationResult.IsError) return userCreationResult.Errors;
                var user = userCreationResult.Value;

                await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);
                IdentityResult identityResult;
                if (!string.IsNullOrEmpty(request.Password))
                {
                    identityResult = await userManager.CreateAsync(user, request.Password);
                }
                else
                {
                    identityResult = await userManager.CreateAsync(user);
                }

                if (!identityResult.Succeeded)
                {
                    var errors = identityResult.Errors.ToApplicationResult();
                    return errors;
                }

                await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                return mapper.Map<Result>(source: user);
            }
        }
    }
}