using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Images
    {
        public static class GetList
        {
            public sealed class Request : QueryableParams;
            public sealed class Result : Models.ImageResult;
            public sealed record Query(Guid ProductId, Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken ct)
                {
                    var images = await unitOfWork.Context.Set<ProductImage>()
                        .Where(predicate: i => i.ProductId == request.ProductId && i.VariantId == null)
                        .OrderBy(keySelector: i => i.Position)
                        .AsQueryable()
                        .AsNoTracking()
                        .ApplySearch(searchParams: request.Request)
                        .ApplyFilters(filterParams: request.Request)
                        .ApplySort(sortParams: request.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListOrAllAsync(pagingParams: request.Request, cancellationToken: ct);

                    return images;
                }
            }
        }
    }
}