using Mapster;

using MapsterMapper;

using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Catalog.Taxonomies.Images;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    public static partial class Images
    {
        public static class Get
        {
            public sealed class Request : QueryableParams;
            public sealed class Result : Models.UploadImageParameter;
            public sealed record Query(Guid TaxonId, Request Request) : IQuery<PagedList<Result>>;

            public sealed class QueryHandler(IUnitOfWork unitOfWork, IMapper mapper, ILogger<QueryHandler> logger)
                : IQueryHandler<Query, PagedList<Result>>
            {
                public async Task<ErrorOr<PagedList<Result>>> Handle(Query request, CancellationToken ct)
                {
                    try
                    {
                        PagedList<Result> rules = await unitOfWork.Context.Set<TaxonImage>()
                            .Where(predicate: r => r.TaxonId == request.TaxonId)
                            .OrderBy(keySelector: r => r.CreatedAt)
                            .AsQueryable()
                            .AsNoTracking()
                            .ApplySearch(searchParams: request.Request)
                            .ApplyFilters(filterParams: request.Request)
                            .ApplySort(sortParams: request.Request)
                            .ProjectToType<Result>(config: mapper.Config)
                            .ToPagedListOrAllAsync(pagingParams: request.Request, cancellationToken: ct);

                        return rules;
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(exception: ex, message: "Error retrieving rules for taxon {TaxonId}", args: request.TaxonId);
                        return Error.Failure(code: "TaxonImages.GetFailed", description: "Failed to retrieve taxon rules");
                    }
                }
            }
        }
    }
}