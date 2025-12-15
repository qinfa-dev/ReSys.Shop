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
        // Select List:

        // Paged List:
        public static class PagedList
        {
            public sealed class Request : QueryableParams;
            public sealed record Result : Models.ListItem;

            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper
            ) : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query command,
                    CancellationToken cancellationToken)
                {

                    var pagedResult = await dbContext.Set<OptionType>()
                        .Include(navigationPropertyPath: ot => ot.ProductOptionTypes)
                        .Include(navigationPropertyPath: ot => ot.OptionValues)
                        .AsQueryable()
                        .AsNoTracking()
                        .ApplySearch(searchParams: command.Request)
                        .ApplyFilters(filterParams: command.Request)
                        .ApplySort(sortParams: command.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListAsync(pagingParams: command.Request,
                            cancellationToken: cancellationToken);

                    return pagedResult;
                }
            }
        }
        // By Id:
        public static class ById
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(Guid Id) : IQuery<Result>;
            public sealed class QueryHandler(
                IApplicationDbContext dbContext,
                IMapper mapper
            ) : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
                {
                    var optionType = await dbContext.Set<OptionType>()
                        .Include(navigationPropertyPath: ot => ot.ProductOptionTypes)
                        .Include(navigationPropertyPath: ot => ot.OptionValues)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: p => p.Id == request.Id, cancellationToken: cancellationToken);

                    if (optionType == null)
                        return OptionType.Errors.NotFound(id: request.Id);

                    return optionType;
                }
            }
        }

    }
}