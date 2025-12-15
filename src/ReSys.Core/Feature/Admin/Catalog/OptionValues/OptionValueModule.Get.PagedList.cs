using Mapster;

using MapsterMapper;

using Microsoft.AspNetCore.Mvc;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.OptionValues;

public static partial class OptionValueModule
{
    public static partial class Get
    {
        public static class PagedList
        {
            public sealed class Request : QueryableParams
            {
                [FromQuery] public Guid[]? OptionTypeId { get; set; }
                [FromQuery] public Guid[]? VariantId { get; set; }
            }
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
                    var param = command.Request;
                    var pagedResult = await dbContext.Set<OptionValue>()
                        .Include(navigationPropertyPath: ov => ov.OptionType)
                        .Include(m => m.VariantOptionValues.Where(m =>
                            param.VariantId == null ||
                            param.VariantId.Length == 0 ||
                            param.VariantId.ToList().Contains(m.VariantId)))
                        .AsNoTracking()
                        .Where(m => param.OptionTypeId == null ||
                            param.OptionTypeId.Length == 0 || 
                            param.OptionTypeId.ToList().Contains(m.OptionTypeId))
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
    }
}