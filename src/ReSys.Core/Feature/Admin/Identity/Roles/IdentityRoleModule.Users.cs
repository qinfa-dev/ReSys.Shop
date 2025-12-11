using Mapster;

using MapsterMapper;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Domain.Identity.Users.Roles;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public static class Users
    {
        // Get Role Users
        public static class GetList
        {
            public sealed class Request : QueryableParams;

            public sealed record Result : Models.UserItem;

            public sealed record Query(string RoleId, Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(
                RoleManager<Role> roleManager,
                IApplicationDbContext dbContext,
                IMapper mapper,
                ILogger<QueryHandler> logger
            ) : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query query, CancellationToken cancellationToken)
                {
                    var param = query.Request;
                    try
                    {
                        // Check if role exists
                        Role? role = await roleManager.FindByIdAsync(query.RoleId);
                        if (role == null)
                            return Role.Errors.RoleNotFound;

                        // Get UserIds for this role
                        var userIdsInRole = dbContext.Set<UserRole>()
                            .Where(ur => ur.RoleId == query.RoleId)
                            .Select(ur => ur.UserId)
                            .Distinct();

                        // Query users based on these IDs
                        IQueryable<User> usersQuery = dbContext.Set<User>()
                            .Where(u => userIdsInRole.Contains(u.Id))
                            .AsNoTracking()
                            .AsQueryable();

                        // Apply search, filter, sort
                        usersQuery = usersQuery
                            .ApplySearch(searchParams: param)
                            .ApplyFilters(filterParams: param)
                            .ApplySort(sortParams: param);

                        // Project and paginate
                        var pagedList = await usersQuery
                            .ProjectToType<Result>(config: mapper.Config)
                            .ToPagedListAsync(pagingParams: param, cancellationToken: cancellationToken);

                        logger.LogDebug(
                            message:
                            "Retrieved {Count} users for role {RoleId} (page {PageIndex}, size {PageSize}) with filters {@Filters}",
                            args: [pagedList.Items.Count, query.RoleId, param.PageIndex, param.PageSize, param]
                        );

                        return pagedList;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex,
                            message: "Error retrieving users for role {RoleId} with params {@Params}",
                            args: [query.RoleId, param]);
                        return Error.Failure(code: "RoleUsers.RetrievalFailed",
                            description: "Failed to retrieve users for role");
                    }
                }
            }
        }

        // Assign User to Role
        public static class Assign
        {
            public sealed record Request(string UserId);

            public sealed record Command(string RoleId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.RoleId)
                        .NotEmpty()
                        .WithMessage(errorMessage: "Role ID is required.");
                    RuleFor(expression: x => x.Request.UserId)
                        .NotEmpty()
                        .WithMessage(errorMessage: "User ID is required.");
                }
            }

            public sealed class CommandHandler(
                UserManager<User> userManager,
                RoleManager<Role> roleManager,
                IUnitOfWork unitOfWork,
                ILogger<CommandHandler> logger
            ) : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken cancellationToken)
                {
                    try
                    {
                        // Check if role exists
                        Role? role = await roleManager.FindByIdAsync(command.RoleId);
                        if (role == null)
                        {
                            return Role.Errors.RoleNotFound;
                        }

                        // Check if user exists
                        User? user = await userManager.FindByIdAsync(command.Request.UserId);
                        if (user == null)
                        {
                            return User.Errors.NotFound(command.Request.UserId);
                        }

                        // Check if user is already in role
                        if (await userManager.IsInRoleAsync(user: user, role: role.Name!))
                        {
                            return Error.Conflict(code: "Role.UserAlreadyAssigned",
                                description: $"User '{user.UserName}' is already assigned to role '{role.Name}'.");
                        }

                        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                        // Add user to role
                        IdentityResult result = await userManager.AddToRoleAsync(user: user, role: role.Name!);
                        if (!result.Succeeded)
                        {
                            string errors = string.Join(separator: "; ",
                                values: result.Errors.Select(selector: e => e.Description));
                            logger.LogError(
                                message: "Failed to assign user {UserId} to role {RoleId}: {Errors}",
                                args: [command.Request.UserId, command.RoleId, errors]);

                            return Error.Failure(code: "Role.UserAssignmentFailed",
                                description: $"Failed to assign user to role: {errors}");
                        }

                        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                        await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                        logger.LogInformation(
                            message: "Successfully assigned user {UserId} to role {RoleId}",
                            args: [command.Request.UserId, command.RoleId]);

                        return Result.Success;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex,
                            message: "Unexpected error assigning user {UserId} to role {RoleId}",
                            args: [command.Request.UserId, command.RoleId]);
                        return Role.Errors.UnexpectedError(operation: "assigning user to role");
                    }
                }
            }
        }

        // Unassign User from Role
        public static class Unassign
        {
            public sealed record Request(string UserId);

            public sealed record Command(string RoleId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(expression: x => x.RoleId)
                        .NotEmpty()
                        .WithMessage(errorMessage: "Role ID is required.");
                    RuleFor(expression: x => x.Request.UserId)
                        .NotEmpty()
                        .WithMessage(errorMessage: "User ID is required.");
                }
            }

            public sealed class CommandHandler(
                UserManager<User> userManager,
                RoleManager<Role> roleManager,
                IUnitOfWork unitOfWork,
                ILogger<CommandHandler> logger
            ) : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken cancellationToken)
                {
                    try
                    {
                        // Check if role exists
                        Role? role = await roleManager.FindByIdAsync(command.RoleId);
                        if (role == null)
                        {
                            return Role.Errors.RoleNotFound;
                        }

                        // Check if user exists
                        User? user = await userManager.FindByIdAsync(command.Request.UserId);
                        if (user == null)
                        {
                            return User.Errors.NotFound(command.Request.UserId);
                        }

                        // Check if user is in role
                        if (!await userManager.IsInRoleAsync(user: user, role: role.Name!))
                        {
                            return Error.NotFound(code: "Role.UserNotAssigned",
                                description: $"User '{user.UserName}' is not assigned to role '{role.Name}'.");
                        }

                        await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                        // Remove user from role
                        IdentityResult result = await userManager.RemoveFromRoleAsync(user: user, role: role.Name!);
                        if (!result.Succeeded)
                        {
                            string errors = string.Join(separator: "; ",
                                values: result.Errors.Select(selector: e => e.Description));
                            logger.LogError(
                                message: "Failed to unassign user {UserId} from role {RoleId}: {Errors}",
                                args: [command.Request.UserId, command.RoleId, errors]);

                            return Error.Failure(code: "Role.UserUnassignmentFailed",
                                description: $"Failed to unassign user from role: {errors}");
                        }

                        await unitOfWork.SaveChangesAsync(cancellationToken: cancellationToken);
                        await unitOfWork.CommitTransactionAsync(cancellationToken: cancellationToken);

                        logger.LogInformation(
                            message: "Successfully unassigned user {UserId} from role {RoleId}",
                            args: [command.Request.UserId, command.RoleId]);

                        return Result.Success;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex,
                            message: "Unexpected error unassigning user {UserId} from role {RoleId}",
                            args: [command.Request.UserId, command.RoleId]);
                        return Role.Errors.UnexpectedError(operation: "unassigning user from role");
                    }
                }
            }
        }
    }
}