using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.OptionTypes;

public static partial class OptionTypeModule
{
    public static partial class Get
    {
        public static class SelectList
        {
            public sealed class Request : QueryableParams;
            public sealed record Result : Models.SelectItem;

            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper
            ) : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query command, CancellationToken cancellationToken)
                {
                    PagedList<Result> pagedResult = await dbContext.Set<OptionType>().AsQueryable()
                        .AsNoTracking()
                        .ApplySearch(searchParams: command.Request)
                        .ApplyFilters(filterParams: command.Request)
                        .ApplySort(sortParams: command.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListOrAllAsync(pagingParams: command.Request,
                            cancellationToken: cancellationToken);

                    return pagedResult;
                }
            }
        }
    }
}

public static partial class OptionTypeModule
{
    public static partial class Get
    {
    }
}