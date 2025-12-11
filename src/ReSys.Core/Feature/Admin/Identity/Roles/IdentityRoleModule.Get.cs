using Mapster;

using MapsterMapper;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Constants;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public static class Get
    {
        public static class ById
        {
            public sealed record Result : Models.Detail;

            public sealed record Query(string Id) : IQuery<Result>;

            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper,
                ILogger<QueryHandler> logger
            ) : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
                {
                    try
                    {
                        // Check: Role existence in the database
                        Result? role = await dbContext.Set<Role>()
                            .AsNoTracking()
                            .Include(navigationPropertyPath: m => m.UserRoles)
                            .Include(navigationPropertyPath: m =>
                                m.RoleClaims.Where(rc => rc.ClaimType == CustomClaim.Permission))
                            .Where(predicate: m => m.Id == request.Id)
                            .ProjectToType<Result>(config: mapper.Config)
                            .FirstOrDefaultAsync(cancellationToken: cancellationToken);

                        if (role is null)
                            return Role.Errors.RoleNotFound;

                        // Log: Successful retrieval details
                        logger.LogDebug(
                            message:
                            "Retrieved role {RoleId} ({RoleName}) with {UserCount} users and {PermissionCount} permissions",
                            args: [role.Id, role.Name, role.UserCount, role.PermissionCount]
                        );

                        return role;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex,
                            message: "Error retrieving role {RoleId}",
                            args: request.Id);

                        // Recover: Return a standardized error object
                        return Error.Failure(code: "Role.RetrievalFailed",
                            description: "Failed to retrieve role details");
                    }
                }
            }
        }

        // Paged List:
        public static class PagedList
        {
            public class Request : QueryableParams
            {
                public bool? IsSystemRole { get; set; }
                public bool? IsDefault { get; set; }
                [FromQuery] public string[]? Permission { get; set; }
                [FromQuery] public string[]? UserId { get; set; }
            }

            public sealed record Result : Models.ListItem;

            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(
                IApplicationDbContext context,
                IMapper mapper,
                ILogger<QueryHandler> logger
            ) : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
                {
                    try
                    {
                        var param = request.Request; // Renamed from request.Param to request.Request

                        // Start base query
                        IQueryable<Role> query = context.Set<Role>()
                            .AsNoTracking()
                            .AsQueryable();

                        bool includeUserRoles = param.UserId is { Length: > 0 };
                        bool includeClaims = param.Permission is { Length: > 0 };

                        // Conditional includes
                        if (includeUserRoles)
                            query = query.Include(navigationPropertyPath: r => r.UserRoles);

                        if (includeClaims)
                            query = query.Include(navigationPropertyPath: r => r.RoleClaims);

                        // Filter by role attributes
                        if (param.IsSystemRole.HasValue)
                            query = query.Where(predicate: r => r.IsSystemRole == param.IsSystemRole.Value);

                        if (param.IsDefault.HasValue)
                            query = query.Where(predicate: r => r.IsDefault == param.IsDefault.Value);

                        // Filter by specific users
                        if (includeUserRoles && param.UserId is not null)
                        {
                            var userIds = param.UserId.ToHashSet();
                            query = query.Where(predicate: r => r.UserRoles.Any(ur => userIds.Contains(ur.UserId)));
                        }

                        // Filter by permissions (RoleClaims)
                        if (includeClaims && param.Permission is not null)
                        {
                            var permissions = param.Permission.ToHashSet();
                            query = query.Where(predicate: r => r.RoleClaims
                                .Any(rc => !string.IsNullOrWhiteSpace(rc.ClaimValue) &&
                                           rc.ClaimType == CustomClaim.Permission &&
                                           permissions.Contains(rc.ClaimValue)));
                        }

                        // Apply additional filters and sorting from SharedKernel
                        query = query
                            .ApplySearch(searchParams: param)
                            .ApplyFilters(filterParams: param)
                            .ApplySort(sortParams: param);

                        // Project & paginate
                        var paginatedList = await query
                            .ProjectToType<Result>(config: mapper.Config)
                            .ToPagedListAsync(pagingParams: param,
                                cancellationToken: cancellationToken);

                        logger.LogDebug(
                            message:
                            "Retrieved {Count} roles (page {PageIndex}, size {PageSize}) with filters {@Filters}",
                            args: [paginatedList.Items.Count, param.PageIndex, param.PageSize, param]
                        );

                        return paginatedList;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex,
                            message: "Error retrieving roles list with params {@Params}",
                            args: request.Request); // Renamed from request.Param to request.Request
                        return Error.Failure(code: "Roles.RetrievalFailed",
                            description: "Failed to retrieve roles list");
                    }
                }
            }
        }

        // Select List:
        public static class SelectList
        {
            public class Request : QueryableParams
            {
                public bool? IsSystemRole { get; set; }
                public bool? IsDefault { get; set; }
                [FromQuery] public string[]? Permission { get; set; }
                [FromQuery] public string[]? UserId { get; set; }
            }

            public sealed record Result : Models.ListItem;

            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(
                IApplicationDbContext context,
                IMapper mapper,
                ILogger<QueryHandler> logger
            ) : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken cancellationToken)
                {
                    try
                    {
                        var param = request.Request;

                        // Start base query
                        IQueryable<Role> query = context.Set<Role>()
                            .AsNoTracking()
                            .AsQueryable();

                        bool includeUserRoles = param.UserId is { Length: > 0 };
                        bool includeClaims = param.Permission is { Length: > 0 };

                        // Conditional includes
                        if (includeUserRoles)
                            query = query.Include(navigationPropertyPath: r => r.UserRoles);

                        if (includeClaims)
                            query = query.Include(navigationPropertyPath: r => r.RoleClaims);

                        // Filter by role attributes
                        if (param.IsSystemRole.HasValue)
                            query = query.Where(predicate: r => r.IsSystemRole == param.IsSystemRole.Value);

                        if (param.IsDefault.HasValue)
                            query = query.Where(predicate: r => r.IsDefault == param.IsDefault.Value);

                        // Filter by specific users
                        if (includeUserRoles && param.UserId is not null)
                        {
                            var userIds = param.UserId.ToHashSet();
                            query = query.Where(predicate: r => r.UserRoles.Any(ur => userIds.Contains(ur.UserId)));
                        }

                        // Filter by permissions (RoleClaims)
                        if (includeClaims && param.Permission is not null)
                        {
                            var permissions = param.Permission.ToHashSet();
                            query = query.Where(predicate: r => r.RoleClaims
                                .Any(rc => !string.IsNullOrWhiteSpace(rc.ClaimValue) &&
                                           rc.ClaimType == CustomClaim.Permission &&
                                           permissions.Contains(rc.ClaimValue)));
                        }

                        // Apply additional filters and sorting from SharedKernel
                        query = query
                            .ApplySearch(searchParams: param)
                            .ApplyFilters(filterParams: param)
                            .ApplySort(sortParams: param);

                        // Project & paginate
                        var paginatedList = await query
                            .ProjectToType<Result>(config: mapper.Config)
                            .ToPagedListAsync(pagingParams: param,
                                cancellationToken: cancellationToken);

                        logger.LogDebug(
                            message:
                            "Retrieved {Count} roles (page {PageIndex}, size {PageSize}) with filters {@Filters}",
                            args: [paginatedList.Items.Count, param.PageIndex, param.PageSize, param]
                        );

                        return paginatedList;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex,
                            message: "Error retrieving roles list with params {@Params}",
                            args: request.Request);
                        return Error.Failure(code: "Roles.RetrievalFailed",
                            description: "Failed to retrieve roles list");
                    }
                }
            }
        }

    }
}