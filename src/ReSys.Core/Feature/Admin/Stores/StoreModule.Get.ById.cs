using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
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
                    var store = await dbContext.Set<Store>()
                        .Include(s => s.Country)
                        .Include(s => s.State)
                        .Include(s => s.Orders) // For ActiveOrderCount etc.
                        //.Include(s => s.StoreProducts) // For VisibleProductCount etc.
                        .Include(s => s.StoreShippingMethods) // For IsConfigured etc.
                        .Include(s => s.StorePaymentMethods) // For IsConfigured etc.
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: s => s.Id == request.Id, cancellationToken: ct);

                    if (store == null)
                        return Store.Errors.NotFound(id: request.Id);

                    return store;
                }
            }
        }
    }
}