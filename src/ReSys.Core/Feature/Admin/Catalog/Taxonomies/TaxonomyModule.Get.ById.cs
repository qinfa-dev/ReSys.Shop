using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Catalog.Taxonomies;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Taxonomies;

public static partial class TaxonomyModule
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
                    var taxonomy = await dbContext.Set<Taxonomy>()
                        .Include(navigationPropertyPath: t => t.Store)
                        .Include(navigationPropertyPath: t => t.Taxons)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: cancellationToken);

                    if (taxonomy == null)
                        return Taxonomy.Errors.NotFound(id: request.Id);

                    return taxonomy;
                }
            }
        }
    }
}