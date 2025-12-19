using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Inventories.Movements;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    public static class Movements
    {
        public static class Get
        {
            public sealed class Request : QueryableParams;
            public sealed record Result : Models.MovementItem;
            public sealed record Query(Guid StockItemId, Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query query, CancellationToken ct)
                {
                    var movements = await unitOfWork.Context.Set<StockMovement>()
                        .Where(predicate: sm => sm.StockItemId == query.StockItemId)
                        .OrderByDescending(keySelector: sm => sm.CreatedAt)
                        .AsNoTracking()
                        .ApplySearch(searchParams: query.Request)
                        .ApplyFilters(filterParams: query.Request)
                        .ApplySort(sortParams: query.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListAsync(pagingParams: query.Request, cancellationToken: ct);

                    return movements;
                }
            }
        }
    }
}