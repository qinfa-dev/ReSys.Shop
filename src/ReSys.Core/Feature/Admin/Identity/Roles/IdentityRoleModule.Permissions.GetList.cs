using Mapster;

using MapsterMapper;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Roles.Claims;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Constants;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public static partial class Permissions
    {
        public static class GetList
        {
            public sealed record Result : Models.PermissionItem;

            public sealed record Query(string RoleId) : IQuery<List<Result>>;

            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                RoleManager<Role> roleManager,
                IMapper mapper,
                ILogger<QueryHandler> logger
            ) : IQueryHandler<Query, List<Result>>
            {
                public async Task<ErrorOr<List<Result>>> Handle(Query query, CancellationToken cancellationToken)
                {
                    try
                    {
                        // Check if role exists
                        Role? role = await roleManager.FindByIdAsync(query.RoleId);
                        if (role == null)
                        {
                            return Role.Errors.RoleNotFound;
                        }

                        // Get permissions for this role
                        List<Result> permissions = await dbContext.Set<RoleClaim>()
                            .Where(rc => rc.RoleId == query.RoleId && rc.ClaimType == CustomClaim.Permission)
                            .AsNoTracking()
                            .ProjectToType<Result>(config: mapper.Config)
                            .ToListAsync(cancellationToken: cancellationToken);

                        logger.LogDebug(
                            message: "Retrieved {Count} permissions for role {RoleId}",
                            args: [permissions.Count, query.RoleId]
                        );

                        return permissions;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex,
                            message: "Error retrieving permissions for role {RoleId}",
                            args: query.RoleId);
                        return Error.Failure(code: "RolePermissions.RetrievalFailed",
                            description: "Failed to retrieve permissions for role");
                    }
                }
            }
        }
    }
}