using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.Products.Prices;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Variants;

public static partial class VariantModule
{
    public static partial class Prices
    {
        public static class Get
        {
            public sealed class Request : QueryableParams;
            public sealed record Result : Models.PriceItem;
            public sealed record Query(Guid VariantId, Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken ct)
                {
                    var prices = await unitOfWork.Context.Set<Price>()
                        .Where(predicate: p => p.VariantId == request.VariantId)
                        .AsQueryable()
                        .AsNoTracking()
                        .ApplySearch(searchParams: request.Request)
                        .ApplyFilters(filterParams: request.Request)
                        .ApplySort(sortParams: request.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListOrAllAsync(pagingParams: request.Request, cancellationToken: ct);

                    return prices;
                }
            }
        }
    }
}