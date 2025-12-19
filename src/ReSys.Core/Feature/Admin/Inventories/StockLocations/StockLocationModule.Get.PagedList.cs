using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
{
    public static partial class Get
    {

        public static class PagedList
        {
            public sealed class Request : QueryableParams;

            public sealed record Result : Models.ListItem;

            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query command, CancellationToken ct)
                {
                    var pagedResult = await dbContext.Set<StockLocation>()
                        .Include(navigationPropertyPath: l => l.Country)
                        .Include(navigationPropertyPath: l => l.State)
                        .Include(navigationPropertyPath: l => l.StockItems)
                        .AsNoTracking()
                        .ApplySearch(searchParams: command.Request)
                        .ApplyFilters(filterParams: command.Request)
                        .ApplySort(sortParams: command.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListAsync(pagingParams: command.Request, cancellationToken: ct);

                    return pagedResult;
                }
            }
        }
    }
}
