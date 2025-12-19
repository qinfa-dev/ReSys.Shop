using MapsterMapper;

using ReSys.Core.Domain.Settings.ShippingMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
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
                    var shippingMethod = await dbContext.Set<ShippingMethod>()
                        .FirstOrDefaultAsync(predicate: pm => pm.Id == request.Id, cancellationToken: ct);

                    if (shippingMethod == null)
                        return ShippingMethod.Errors.NotFound(id: request.Id);

                    var result = mapper.Map<Result>(source: shippingMethod);
                    return result;
                }
            }
        }
    }
}