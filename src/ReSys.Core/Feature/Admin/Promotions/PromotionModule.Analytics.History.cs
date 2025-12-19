using Microsoft.Extensions.Logging;

using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Common.Models.Search;
using ReSys.Core.Common.Models.Sort;
using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Queryable;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.Promotions.Usages;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Analytics
    {
        public static class History
        {
            public static class Get
            {
                public sealed class Request : QueryableParams
                {
                    public string? Action { get; init; }
                    public DateTimeOffset? From { get; init; }
                    public DateTimeOffset? To { get; init; }
                }

                public sealed record Result : Models.HistoryItem;
                public sealed record Query(Guid PromotionId, Request Request) : IQuery<PagedList<Result>>;

                public sealed class QueryValidator : AbstractValidator<Query>
                {
                    public QueryValidator()
                    {
                        RuleFor(x => x.PromotionId)
                            .NotEmpty().WithMessage("Promotion ID is required for history.")
                            .WithErrorCode("Promotion.Id.Required");
                    }
                }

                public sealed class QueryHandler(
                    IApplicationDbContext dbContext,
                    ILogger<QueryHandler> logger)
                    : IQueryHandler<Query, PagedList<Result>>
                {
                    public async Task<ErrorOr<PagedList<Result>>> Handle(Query query, CancellationToken ct)
                    {
                        // Verify promotion exists
                        var promotionExists = await dbContext.Set<Promotion>()
                            .AnyAsync(p => p.Id == query.PromotionId, ct);

                        if (!promotionExists)
                            return Promotion.Errors.NotFound(query.PromotionId);

                        // Query audit logs
                        var auditQuery = dbContext.Set<PromotionUsage>()
                            .Where(al => al.PromotionId == query.PromotionId)
                            .AsNoTracking();

                        // Apply filters
                        if (!string.IsNullOrEmpty(query.Request.Action))
                        {
                            auditQuery = auditQuery.Where(al => al.Action == query.Request.Action);
                        }

                        if (query.Request.From.HasValue)
                        {
                            auditQuery = auditQuery.Where(al => al.CreatedAt >= query.Request.From.Value);
                        }

                        if (query.Request.To.HasValue)
                        {
                            auditQuery = auditQuery.Where(al => al.CreatedAt <= query.Request.To.Value);
                        }

                        // Order by timestamp descending
                        auditQuery = auditQuery.OrderByDescending(al => al.CreatedAt);

                        // Apply search and additional filters
                        auditQuery = auditQuery
                            .ApplySearch(query.Request)
                            .ApplyFilters(query.Request)
                            .ApplySort(query.Request);

                        // Map to result
                        var pagedResult = await auditQuery
                            .Select(al => new Result
                            {
                                Id = al.Id,
                                Action = al.Action,
                                Description = al.Description,
                                PerformedBy = al.UserId,
                                PerformedByName = al.UserEmail ?? "System",
                                ChangesBefore = al.ChangesBefore,
                                ChangesAfter = al.ChangesAfter,
                                Timestamp = al.CreatedAt
                            })
                            .ToPagedListAsync(query.Request, ct);

                        logger.LogInformation("Retrieved {Count} audit log entries for promotion {PromotionId}",
                            pagedResult.Items.Count, query.PromotionId);

                        return pagedResult;
                    }
                }
            }
        }
    }
}