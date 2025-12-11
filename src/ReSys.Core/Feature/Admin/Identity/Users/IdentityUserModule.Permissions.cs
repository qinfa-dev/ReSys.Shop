using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    public static class Permissions
    {
        // Get Permissions:
        public static class GetList
        {
            public sealed class Parameter
            {
                public string? UserId { get; set; }
            }
            public sealed record Result : Models.PermissionItem;

            public sealed record Query(Parameter Parameter) : IQuery<List<Models.PermissionItem>>;

            public sealed class QueryHandler(
                UserManager<User> userManager,
                IUserContext userContext
            ) : IQueryHandler<Query, List<Models.PermissionItem>>
            {
                public async Task<ErrorOr<List<Models.PermissionItem>>> Handle(Query query, CancellationToken cancellationToken)
                {
                    var userId = query.Parameter.UserId ?? userContext.UserId ?? Guid.Empty.ToString();
                    var user = await userManager.FindByIdAsync(userId);
                    if (user == null)
                        return User.Errors.NotFound(userId);

                    var claims = await userManager.GetClaimsAsync(user);
                    var permissions = claims.Select(c => new Models.PermissionItem
                    {
                        Name = c.Type,
                        DisplayName = c.Type,
                        Description = c.Value
                    }).ToList();

                    return permissions;
                }
            }
        }

        // Assign Permission:
        public static class Assign
        {
            public sealed record Request(string ClaimType, string ClaimValue);
            public sealed record Command(string UserId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
                    RuleFor(x => x.Request.ClaimType).NotEmpty().WithMessage("Claim type is required.");
                    RuleFor(x => x.Request.ClaimValue).NotEmpty().WithMessage("Claim value is required.");
                }
            }

            public sealed class CommandHandler(
                UserManager<User> userManager,
                IUnitOfWork unitOfWork
            ) : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken cancellationToken)
                {
                    var user = await userManager.FindByIdAsync(command.UserId);
                    if (user == null)
                    {
                        return User.Errors.NotFound(command.UserId);
                    }

                    // Check if claim already exists for the user
                    var existingClaims = await userManager.GetClaimsAsync(user);
                    if (existingClaims.Any(c => c.Type == command.Request.ClaimType && c.Value == command.Request.ClaimValue))
                    {
                        return Error.Conflict(code: "UserPermission.AlreadyAssigned", description: $"Permission '{command.Request.ClaimType}:{command.Request.ClaimValue}' is already assigned to the user.");
                    }

                    await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                    var claim = new System.Security.Claims.Claim(command.Request.ClaimType, command.Request.ClaimValue);
                    var result = await userManager.AddClaimAsync(user, claim);
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

        // Unassign Permission:
        public static class Unassign
        {
            public sealed record Request(string ClaimType, string ClaimValue);
            public sealed record Command(string UserId, Request Request) : ICommand<Success>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.UserId).NotEmpty().WithMessage("User ID is required.");
                    RuleFor(x => x.Request.ClaimType).NotEmpty().WithMessage("Claim type is required.");
                    RuleFor(x => x.Request.ClaimValue).NotEmpty().WithMessage("Claim value is required.");
                }
            }

            public sealed class CommandHandler(
                UserManager<User> userManager,
                IUnitOfWork unitOfWork
            ) : ICommandHandler<Command, Success>
            {
                public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken cancellationToken)
                {
                    var user = await userManager.FindByIdAsync(command.UserId);
                    if (user == null)
                    {
                        return User.Errors.NotFound(command.UserId);
                    }

                    var claimToRemove = new System.Security.Claims.Claim(command.Request.ClaimType, command.Request.ClaimValue);

                    var existingClaims = await userManager.GetClaimsAsync(user);
                    var claim = existingClaims.FirstOrDefault(c => c.Type == claimToRemove.Type && c.Value == claimToRemove.Value);

                    if (claim == null)
                        return Error.NotFound(
                            code: "UserPermission.NotAssigned",
                            description: $"Permission '{command.Request.ClaimType}:{command.Request.ClaimValue}' is not assigned to the user.");

                    await unitOfWork.BeginTransactionAsync(cancellationToken: cancellationToken);

                    var result = await userManager.RemoveClaimAsync(user, claim);
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