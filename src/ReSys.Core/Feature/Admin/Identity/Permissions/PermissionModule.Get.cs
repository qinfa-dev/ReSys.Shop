using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Identity.Permissions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Identity.Permissions;

public static partial class PermissionModule
{
    public static partial class Get
    {
        public static class ById
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(Guid Id) : IQuery<Result>;
            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper
            ) : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
                {
                    var permission = await dbContext.Set<AccessPermission>()
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: cancellationToken);

                    if (permission == null)
                        return AccessPermission.Errors.NotFound;

                    return permission;
                }
            }
        }

        public static class ByName
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(string Name) : IQuery<Result>;
            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper
            ) : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
                {
                    var permission = await dbContext.Set<AccessPermission>()
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Name == request.Name, cancellationToken: cancellationToken);

                    if (permission == null)
                        return AccessPermission.Errors.NotFound;

                    return permission;
                }
            }
        }
    }
}