using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Get
    {
        public static class SelectList
        {
            public sealed class Request : QueryableParams
            {
                public bool? ActiveOnly { get; init; }
            }
            public sealed record Result : Models.SelectItem;
            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query command, CancellationToken ct)
                {
                    var query = dbContext.Set<Promotion>().AsNoTracking();

                    if (command.Request.ActiveOnly == true)
                        query = query.Where(p => p.Active);

                    var pagedResult = await query
                        .ApplySearch(command.Request)
                        .ApplyFilters(command.Request)
                        .ApplySort(command.Request)
                        .ProjectToType<Result>(mapper.Config)
                        .ToPagedListOrAllAsync(command.Request, ct);

                    return pagedResult;
                }
            }
        }

        public static class PagedList
        {
            public sealed class Request : QueryableParams
            {
                public bool? ActiveOnly { get; init; }
                public bool? IsExpired { get; init; }
            }
            public sealed record Result : Models.ListItem;
            public sealed record Query(Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IApplicationDbContext dbContext, IMapper mapper)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query command, CancellationToken ct)
                {
                    var query = dbContext.Set<Promotion>()
                        .Include(p => p.PromotionRules)
                        .AsNoTracking();

                    if (command.Request.ActiveOnly == true)
                        query = query.Where(p => p.Active);

                    if (command.Request.IsExpired == true)
                        query = query.Where(p => p.ExpiresAt.HasValue && p.ExpiresAt < DateTimeOffset.UtcNow);
                    else if (command.Request.IsExpired == false)
                        query = query.Where(p => !p.ExpiresAt.HasValue || p.ExpiresAt >= DateTimeOffset.UtcNow);

                    var pagedResult = await query
                        .ApplySearch(command.Request)
                        .ApplyFilters(command.Request)
                        .ApplySort(command.Request)
                        .ProjectToType<Result>(mapper.Config)
                        .ToPagedListAsync(command.Request, ct);

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
                    var promotion = await dbContext.Set<Promotion>()
                        .Include(p => p.PromotionRules)
                        .ProjectToType<Result>(mapper.Config)
                        .FirstOrDefaultAsync(p => p.Id == request.Id, ct);

                    if (promotion == null)
                        return Promotion.Errors.NotFound(request.Id);

                    return promotion;
                }
            }
        }
    }
}