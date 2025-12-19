using Mapster;

using MapsterMapper;

using ReSys.Core.Domain.Stores;
using ReSys.Core.Domain.Stores.Products;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class Products
    {
        public static partial class Get
        {
            public static class ById
            {
                public sealed record Result : Models.StoreProductListItem;
                public sealed record Query(Guid StoreId, Guid ProductId) : IQuery<Result>;
                public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
                    : IQueryHandler<Query, Result>
                {
                    private readonly IApplicationDbContext _dbContext = unitOfWork.Context;

                    public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken ct)
                    {
                        var storeProduct = await _dbContext.Set<StoreProduct>()
                            .Include(sp => sp.Product)
                            .Where(sp => sp.StoreId == request.StoreId && sp.ProductId == request.ProductId)
                            .ProjectToType<Result>(mapper.Config)
                            .FirstOrDefaultAsync(ct);

                        if (storeProduct == null)
                        {
                            return Store.Errors.ProductNotInStore;
                        }

                        return storeProduct;
                    }
                }
            }
        }
    }
}