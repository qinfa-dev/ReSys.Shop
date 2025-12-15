using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
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
                    var product = await dbContext.Set<Product>()
                        .Include(navigationPropertyPath: p => p.Variants)
                        .Include(navigationPropertyPath: p => p.Images)
                        .Include(navigationPropertyPath: p => p.ProductOptionTypes)
                        .ThenInclude(navigationPropertyPath: p => p.OptionType)
                        .Include(navigationPropertyPath: p => p.Classifications)
                        .ThenInclude(navigationPropertyPath: p => p.Taxon)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: ct);

                    if (product == null)
                        return Product.Errors.NotFound(id: request.Id);

                    return product;
                }
            }
        }
    }
}