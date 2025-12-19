using Mapster;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Mvc;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Stores.Products;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class Products
    {
        public static partial class Get
        {
            public static class PagedList
            {
                public sealed class Request : QueryableParams
                {
                    [FromQuery] public Guid?[]? StoreId { get; init; }
                    [FromQuery] public Guid?[]? ProductId { get; init; }
                }
                public record Result : Models.StoreProductListItem;

                public record Query(Request Request) : IRequest<ErrorOr<PagedList<Result>>>;

                public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
                    : IRequestHandler<Query, ErrorOr<PagedList<Result>>>
                {
                    private readonly IUnitOfWork _unitOfWork = unitOfWork;
                    private readonly IApplicationDbContext _dbContext = unitOfWork.Context;

                    public async Task<ErrorOr<PagedList<Result>>> Handle(Query query, CancellationToken ct)
                    {
                        var p = query.Request;
                        var pagedList = await _dbContext.Set<StoreProduct>()
                            .Include(sp => sp.Product)
                            .AsNoTracking()
                            .Where(sp => (p.StoreId == null || p.StoreId.Length == 0 || p.StoreId.Contains(sp.StoreId)) &&
                                         (p.ProductId == null || p.ProductId.Length == 0 || p.ProductId.Contains(sp.ProductId)))
                            .ApplySearch(query.Request)
                            .ApplyFilters(query.Request)
                            .ApplySort(query.Request)
                            .ProjectToType<Result>(mapper.Config)
                            .ToPagedListOrAllAsync(query.Request, ct);

                        return pagedList;
                    }
                }
            }
        }
    }
}