using Mapster;

using MapsterMapper;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Promotions.Rules;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Rules
    {
        public static class Get
        {
            public sealed class Request : QueryableParams;
            public sealed record Query(Guid PromotionId, Request Request) : IQuery<PagedList<Models.RuleItem>>;

            public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper)
                : IQueryHandler<Query, PagedList<Models.RuleItem>>
            {
                public async Task<ErrorOr<PagedList<Models.RuleItem>>> Handle(Query request, CancellationToken ct)
                {
                    var rules = await unitOfWork.Context.Set<PromotionRule>()
                        .Where(r => r.PromotionId == request.PromotionId)
                        .Include(r => r.PromotionRuleTaxons)
                        .Include(r => r.PromotionRuleUsers)
                        .OrderBy(r => r.CreatedAt)
                        .AsNoTracking()
                        .ApplySearch(request.Request)
                        .ApplyFilters(request.Request)
                        .ApplySort(request.Request)
                        .ProjectToType<Models.RuleItem>(mapper.Config)
                        .ToPagedListOrAllAsync(request.Request, ct);

                    return rules;
                }
            }
        }
    }
}