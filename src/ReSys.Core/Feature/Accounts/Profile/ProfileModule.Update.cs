using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Accounts.Common;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Profile;

public static partial class ProfileModule
{
    public static class Update
    {
        public sealed record Param : Model.Param;

        public sealed record Command(string? UserId, Param Param) : ICommand<Updated>;

        public sealed class Validator : AbstractValidator<Command>
        {
            public Validator()
            {
                RuleFor(expression: x => x.Param)
                    .SetValidator(validator: new Model.Validator());
            }
        }

        public sealed class CommandHandler(
            IUserContext userContext,
            UserManager<User> userManager,
            IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Updated>
        {
            public async Task<ErrorOr<Updated>> Handle(Command command, CancellationToken cancellationToken)
            {
                try
                {
                    // Load: user context
                    string? userId = userContext.UserId;
                    // Validate: user is authenticated
                    if (!userContext.IsAuthenticated || string.IsNullOrEmpty(value: userId))
                        return User.Errors.Unauthorized;

                    // Ensure the user can only access their own profile
                    if (command.UserId != userId)
                        return User.Errors.Unauthorized;

                    // Get: user
                    User? user = await userManager.FindByIdAsync(userId: userId);
                    if (user is null)
                        return User.Errors.NotFound(credential: userId);
                    Param param = command.Param;

                    // Check: username uniqueness
                    if (!string.IsNullOrWhiteSpace(value: param.Username) && param.Username != user.UserName)
                    {
                        User? existingUser = await userManager.FindByNameAsync(userName: param.Username);
                        if (existingUser is not null && existingUser.Id != user.Id)
                        {
                            //await unitOfWork.RollbackTransactionAsync(cancellationToken: cancellationToken);
                            return User.Errors.UserNameAlreadyExists(userName: param.Username);
                        }
                    }

                    // Update: profile fields
                    ErrorOr<User> updateResult = user.UpdateProfile(
                        firstName: param.FirstName,
                        lastName: param.LastName,
                        dateOfBirth: param.DateOfBirth,
                        profileImagePath: param.ProfileImagePath);

                    if (updateResult.IsError) return updateResult.Errors;

                    IdentityResult result = await userManager.UpdateAsync(user: user);
                    if (!result.Succeeded)
                    {
                        return result.Errors.ToApplicationResult();
                    }

                    await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);

                    return new Updated();
                }
                catch (Exception)
                {
                    return Error.Unexpected(
                        code: "UpdateProfile.Unexpected",
                        description: "An unexpected error occurred while updating the user profile."
                    );
                }
            }
        }
    }
}