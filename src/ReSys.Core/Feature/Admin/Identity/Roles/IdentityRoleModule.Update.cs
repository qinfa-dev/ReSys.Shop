using MapsterMapper;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Roles;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public static class Update
    {
        public sealed record Request : Models.Parameter;

        public sealed record Result : Models.ListItem;

        public sealed record Command(string Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Request)
                    .SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(
            RoleManager<Role> roleManager,
            IMapper mapper,
            ILogger<CommandHandler> logger
        ) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                Request param = request.Request;

                try
                {
                    Role? role = await roleManager.FindByIdAsync(roleId: request.Id);
                    if (role == null)
                        return Role.Errors.RoleNotFound;

                    // Check: if trying to modify a default role inappropriately
                    if (role.IsDefault)
                        return Role.Errors.CannotModifyDefaultRole(roleName: role.Name!);

                    // Update: role properties
                    var updateResult = role.Update(
                        name: param.Name,
                        displayName: param.DisplayName,
                        priority: param.Priority,
                        description: param.Description,
                        isSystemRole: param.IsSystemRole);

                    if (updateResult.IsError) return updateResult.Errors;
                    role = updateResult.Value;

                    // Save: role
                    IdentityResult identityResult = await roleManager.UpdateAsync(role: role);
                    if (!identityResult.Succeeded)
                    {
                        string errors = string.Join(separator: "; ",
                            values: identityResult.Errors.Select(selector: e => e.Description));
                        logger.LogError(message: "Failed to update role {RoleId}: {Errors}",
                            args: [request.Id, errors]);
                        return Error.Failure(code: "Role.UpdateFailed",
                            description: $"Failed to update role: {errors}");
                    }

                    logger.LogInformation(message: "Successfully updated role {RoleId}",
                        args: role.Id);
                    var result = mapper.Map<Result>(source: role);
                    return result;
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex,
                        message: "Unexpected error updating role {RoleId}",
                        args: request.Id);
                    return Role.Errors.UnexpectedError(operation: "updating");
                }
            }
        }
    }
}