using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    public static partial class Get
    {
        // By Id:
        public static class ById
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(string Id) : IQuery<Result>; // User ID is string

            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper
            ) : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
                {
                    var user = await dbContext.Set<User>()
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: cancellationToken);

                    if (user == null)
                        return User.Errors.NotFound(request.Id);

                    return user;
                }
            }
        }

    }
}