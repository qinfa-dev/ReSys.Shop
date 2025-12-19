using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
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
                    var stockItem = await dbContext.Set<StockItem>()
                        .Include(navigationPropertyPath: si => si.Variant)
                        .ThenInclude(navigationPropertyPath: v => v.Product)
                        .Include(navigationPropertyPath: si => si.StockLocation)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: si => si.Id == request.Id, cancellationToken: ct);

                    if (stockItem == null)
                        return StockItem.Errors.NotFound(id: request.Id);

                    return stockItem;
                }
            }
        }
    }
}