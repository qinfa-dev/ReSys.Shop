using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public static class Delete
    {
        public sealed record Command(string Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id)
                    .NotEmpty()
                    .WithMessage(errorMessage: "Role ID is required.");
            }
        }

        public sealed class CommandHandler(
            RoleManager<Role> roleManager,
            UserManager<User> userManager,
            ILogger<CommandHandler> logger
        ) : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command request, CancellationToken cancellationToken)
            {
                try
                {
                    // Check: role existence
                    Role? role = await roleManager.FindByIdAsync(roleId: request.Id);
                    if (role is null)
                        return Role.Errors.RoleNotFound;

                    // Check: prevent deletion if the role is assigned to users
                    IList<User> usersInRole = await userManager.GetUsersInRoleAsync(roleName: role.Name!);
                    if (usersInRole.Count > 0)
                        return Role.Errors.RoleInUse(roleName: role.Name!);

                    // Store role info before deletion
                    string roleName = role.Name!;
                    string roleId = role.Id;

                    // Attempt deletion
                    IdentityResult deleteResult = await roleManager.DeleteAsync(role: role);
                    if (!deleteResult.Succeeded)
                    {
                        string errors = string.Join(separator: "; ",
                            values: deleteResult.Errors.Select(selector: e => e.Description));
                        logger.LogError(message: "Failed to delete role {RoleId}: {Errors}", args: [roleId, errors]);

                        return Error.Failure(
                            code: "Role.DeletionFailed",
                            description: $"Failed to delete role: {roleName}'. Errors: {errors}"
                        );
                    }

                    logger.LogInformation(message: "Successfully deleted role {RoleId} ({RoleName})",
                        args: [roleId, roleName]);

                    // Return consistent Deleted result
                    return Result.Deleted;
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex,
                        message: "Unexpected error deleting role {RoleId}",
                        args: request.Id);
                    return Role.Errors.UnexpectedError(operation: "deleting");
                }
            }
        }
    }
}