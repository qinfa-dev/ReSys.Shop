using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Catalog.Properties;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Properties;

public static partial class PropertyModule
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
                    var property = await dbContext.Set<Property>()
                        .Include(navigationPropertyPath: p => p.ProductProperties)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: cancellationToken);

                    if (property == null)
                        return Property.Errors.NotFound(id: request.Id);

                    return property;
                }
            }
        }
    }
}