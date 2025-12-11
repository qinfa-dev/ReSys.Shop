using Mapster;

using MapsterMapper;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    public static class Roles
    {
        // Get Roles:
        public static class GetList
        {
            public sealed class Parameter
            {
                public string? UserId { get; set; }
            }
            public sealed record Result : Models.RoleItem;
            public sealed record Query(Parameter Parameter) : IQuery<List<Result>>;

            public sealed class QueryHandler(
                UserManager<User> userManager,
                RoleManager<Role> roleManager,
                IMapper mapper,
                IUserContext userContext
            ) : IQueryHandler<Query, List<Result>>
            {
                public async Task<ErrorOr<List<Result>>> Handle(Query query, CancellationToken cancellationToken)
                {
                    var userId = query.Parameter.UserId ?? userContext.UserId ?? Guid.Empty.ToString();
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                        return User.Errors.NotFound(userId);

                    var userRoleNames = await userManager.GetRolesAsync(user);
                    var roles = await roleManager.Roles
                        .Where(r => userRoleNames.Contains(r.Name!))
                        .AsQueryable()
                        .AsNoTracking()
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToListAsync(cancellationToken: cancellationToken);

                    return roles;
                }
            }
        }

        // Assign Role:
        public static class Assign
        {
            public sealed record Request(string RoleName);
            public sealed record Command(string? UserId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.Request.RoleName).NotEmpty().WithMessage("Role name is required.");
                }
            }

            public sealed class CommandHandler(
                UserManager<User> userManager,
                RoleManager<Role> roleManager,
                IUnitOfWork unitOfWork,
                IUserContext userContext
            ) : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken cancellationToken)
                {
                    var userId = command.UserId ?? userContext.UserId ?? Guid.Empty.ToString();
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                        return User.Errors.NotFound(userId);

                    var roleExists = await roleManager.RoleExistsAsync(command.Request.RoleName);
                    if (!roleExists)
                    {
                        return Error.NotFound(code: "Role.NotFound", description: $"Role '{command.Request.RoleName}' not found.");
                    }

                    var isInRole = await userManager.IsInRoleAsync(user, command.Request.RoleName);
                    if (isInRole)
                    {
                        return Error.Conflict(code: "UserRole.AlreadyAssigned", description: $"User is already in role '{command.Request.RoleName}'.");
                    }

                    await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                    var result = await userManager.AddToRoleAsync(user, command.Request.RoleName);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
                        return errors;
                    }

                    await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                    await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                    return Result.Success;
                }
            }
        }

        // Unassign Role:
        public static class Unassign
        {
            public sealed record Request(string RoleName);
            public sealed record Command(string? UserId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.Request.RoleName).NotEmpty().WithMessage("Role name is required.");
                }
            }

            public sealed class CommandHandler(
                UserManager<User> userManager,
                RoleManager<Core.Domain.Identity.Roles.Role> roleManager,
                IUnitOfWork unitOfWork,
                IUserContext userContext
            ) : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken cancellationToken)
                {
                    var userId = command.UserId ?? userContext.UserId ?? Guid.Empty.ToString();
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                        return User.Errors.NotFound(userId);

                    var roleExists = await roleManager.RoleExistsAsync(command.Request.RoleName);
                    if (!roleExists)
                    {
                        return Error.NotFound(code: "Role.NotFound", description: $"Role '{command.Request.RoleName}' not found.");
                    }

                    var isInRole = await userManager.IsInRoleAsync(user, command.Request.RoleName);
                    if (!isInRole)
                    {
                        return Error.NotFound(code: "UserRole.NotAssigned", description: $"User is not in role '{command.Request.RoleName}'.");
                    }

                    await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                    var result = await userManager.RemoveFromRoleAsync(user, command.Request.RoleName);
                    if (!result.Succeeded)
                    {
                        var errors = result.Errors.Select(e => Error.Validation(e.Code, e.Description)).ToList();
                        return errors;
                    }

                    await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                    await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                    return Result.Success;
                }
            }
        }
    }
}