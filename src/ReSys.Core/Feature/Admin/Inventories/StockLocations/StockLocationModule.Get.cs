using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Inventories.StockTransfers;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
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
                    var transfer = await dbContext.Set<StockTransfer>()
                        .Include(navigationPropertyPath: st => st.SourceLocation)
                        .Include(navigationPropertyPath: st => st.DestinationLocation)
                        .Include(navigationPropertyPath: st => st.Movements)
                        .ThenInclude(navigationPropertyPath: m => m.StockItem)
                        .ThenInclude(navigationPropertyPath: si => si.Variant)
                        .ThenInclude(navigationPropertyPath: v => v.Product)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: st => st.Id == request.Id, cancellationToken: ct);

                    if (transfer == null)
                        return StockTransfer.Errors.NotFound(id: request.Id);

                    return transfer;
                }
            }
        }
    }
}