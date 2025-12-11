using MapsterMapper;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Feature.Accounts.Common;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public static class Create
    {
        public sealed record Request : Models.Parameter; // Alias

        public sealed record Result : Models.ListItem; // Alias

        public sealed record Command(Request Request) : ICommand<Result>;

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
            ILogger<CommandHandler> logger) : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command request, CancellationToken cancellationToken)
            {
                Request param = request.Request; // Renamed to Request
                try
                {
                    // Check: if role already exists
                    Role? existingRole = await roleManager.FindByNameAsync(roleName: param.Name);
                    if (existingRole != null)
                        return Role.Errors.RoleAlreadyExists(roleName: param.Name);

                    // Create: new role
                    var createResult = Role.Create(
                        name: param.Name,
                        displayName: param.DisplayName,
                        description: param.Description,
                        priority: param.Priority,
                        isSystemRole: param.IsSystemRole);

                    if (createResult.IsError) return createResult.Errors;
                    var role = createResult.Value;

                    IdentityResult result = await roleManager.CreateAsync(role: role);
                    if (!result.Succeeded)
                    {
                        string errors = string.Join(separator: "; ",
                            values: result.Errors.Select(selector: e => e.Description));
                        logger.LogError(message: "Failed to create role {RoleName}: {Errors}",
                            args: [param.Name, errors]);
                        return result.Errors.ToApplicationResult(
                            prefix: "Role",
                            fallbackCode: "CreationFailed");
                    }

                    logger.LogInformation(message: "Successfully created role {RoleId} with name {RoleName}",
                        args: [role.Id, param.Name]);

                    return mapper.Map<Result>(source: role);
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex,
                        message: "Unexpected error creating role with name {RoleName}",
                        args: param.Name);
                    return Role.Errors.UnexpectedError(operation: "creating");
                }
            }
        }
    }
}