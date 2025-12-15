using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Taxons;

public static partial class TaxonModule
{
    public static partial class Get
    {
        public static class ById
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(Guid Id) : IQuery<Result>;

            public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken ct)
                {
                    var taxon = await dbContext.Set<Taxon>()
                        .Include(navigationPropertyPath: t => t.Taxonomy)
                        .Include(navigationPropertyPath: t => t.Parent)
                        .Include(navigationPropertyPath: t => t.Children)
                        .Include(navigationPropertyPath: t => t.TaxonImages)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: ct);

                    if (taxon == null)
                        return Taxon.Errors.NotFound(id: request.Id);

                    return taxon;
                }
            }
        }
    }
}