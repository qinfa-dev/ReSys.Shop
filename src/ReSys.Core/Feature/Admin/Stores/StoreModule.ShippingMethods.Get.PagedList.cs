using Mapster;

using MapsterMapper;

using Microsoft.AspNetCore.Mvc;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Stores.ShippingMethods;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class ShippingMethods
    {
        public static partial class Get
        {
            public static class PagedList
            {
                public sealed class Request : QueryableParams
                {
                    [FromQuery] public Guid?[]? StoreId { get; set; }
                    [FromQuery] public Guid?[]? ShippingMethodId { get; set; }
                }

                public record Result : Models.StoreShippingMethodListItem;
                public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

                public sealed class QueryHandler(IApplicationDbContext db, IMapper mapper)
                    : IQueryHandler<Query, PagedList<Result>>
                {
                    public async Task<ErrorOr<PagedList<Result>>> Handle(Query query, CancellationToken ct)
                    {
                        var p = query.Request;
                        return await db.Set<StoreShippingMethod>()
                            .Include(spm => spm.ShippingMethod)
                            .Include(spm => spm.Store)
                            .AsNoTracking()
                            .Where(c =>
                                (p.StoreId == null || p.StoreId.Length == 0 || p.StoreId.Contains(c.StoreId)) &&
                                (p.ShippingMethodId == null || p.ShippingMethodId.Length == 0 || p.ShippingMethodId.Contains(c.ShippingMethodId)))
                            .ApplySearch(query.Request)
                            .ApplyFilters(query.Request)
                            .ApplySort(query.Request)
                            .ProjectToType<Result>(mapper.Config)
                            .ToPagedListOrAllAsync(query.Request, ct);
                    }
                }
            }

        }
    }
}