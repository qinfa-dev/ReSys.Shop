using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Variants;

public static partial class VariantModule
{
    public static class Get
    {
        public static class SelectList
        {
            public sealed class Request : QueryableParams;
            public sealed record Result : Models.SelectItem;
            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query command, CancellationToken ct)
                {
                    var pagedResult = await dbContext.Set<Variant>()
                        .Include(navigationPropertyPath: v => v.Product)
                        .AsNoTracking()
                        .ApplySearch(searchParams: command.Request)
                        .ApplyFilters(filterParams: command.Request)
                        .ApplySort(sortParams: command.Request)
                        .ProjectToType<Result>(config: mapper.Config)
                        .ToPagedListOrAllAsync(pagingParams: command.Request, cancellationToken: ct);

                    return pagedResult;
                }
            }
        }

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
                    var pagedResult = await dbContext.Set<Variant>()
                        .Include(navigationPropertyPath: v => v.Product)
                        .Include(navigationPropertyPath: v => v.StockItems)
                        .Include(navigationPropertyPath: v => v.Prices)
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

        public static class ById
        {
            public sealed record Result : Models.Detail;
            public sealed record Query(Guid Id) : IQuery<Result>;

            public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                : IQueryHandler<Query, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken ct)
                {
                    var variant = await dbContext.Set<Variant>()
                        .Include(navigationPropertyPath: v => v.Product)
                        .Include(navigationPropertyPath: v => v.StockItems)
                        .Include(navigationPropertyPath: v => v.Prices)
                        .Include(navigationPropertyPath: v => v.OptionValues)
                        .Include(navigationPropertyPath: v => v.Images)
                        .ProjectToType<Result>(config: mapper.Config)
                        .FirstOrDefaultAsync(predicate: v => v.Id == request.Id, cancellationToken: ct);

                    if (variant == null)
                        return Variant.Errors.NotFound(id: request.Id);

                    return variant;
                }
            }
        }
    }
}