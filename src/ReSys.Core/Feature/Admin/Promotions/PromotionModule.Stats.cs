using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Analytics
    {
        public static class Stats
        {
            public static class Get
            {
                public sealed record Result : Models.StatsResult;
                public sealed record Query(Guid PromotionId) : IQuery<Result>;

                public sealed class QueryValidator : AbstractValidator<Query>
                {
                    public QueryValidator()
                    {
                        RuleFor(x => x.PromotionId)
                            .NotEmpty().WithMessage("Promotion ID is required for statistics.")
                            .WithErrorCode("Promotion.Id.Required");
                    }
                }

                public sealed class QueryHandler(
                    IApplicationDbContext dbContext,
                    ILogger<QueryHandler> logger)
                    : IQueryHandler<Query, Result>
                {
                    public async Task<ErrorOr<Result>> Handle(Query query, CancellationToken ct)
                    {
                        var promotion = await dbContext.Set<Promotion>()
                            .Include(p => p.PromotionOrderAdjustments)
                            .ThenInclude(oa => oa.Order)
                            .Include(p => p.LineItemAdjustments)
                            .ThenInclude(lia => lia.LineItem)
                            .ThenInclude(li => li.Variant)
                            .ThenInclude(v => v.Product)
                            .AsNoTracking()
                            .FirstOrDefaultAsync(p => p.Id == query.PromotionId, ct);

                        if (promotion == null)
                            return Promotion.Errors.NotFound(query.PromotionId);

                        // Calculate total discount given (from adjustments)
                        var orderDiscounts = promotion.PromotionOrderAdjustments.Sum(oa => Math.Abs(oa.AmountCents));
                        var lineItemDiscounts = promotion.LineItemAdjustments.Sum(lia => Math.Abs(lia.AmountCents));
                        var totalDiscountCents = orderDiscounts + lineItemDiscounts;
                        var totalDiscountGiven = totalDiscountCents / 100m;

                        // Get affected orders
                        var affectedOrders = promotion.PromotionOrderAdjustments
                            .Select(oa => oa.Order)
                            .Distinct()
                            .ToList();

                        var affectedOrdersCount = affectedOrders.Count;
                        var averageDiscountPerOrder = affectedOrdersCount > 0
                            ? totalDiscountGiven / affectedOrdersCount
                            : 0m;

                        // Calculate total revenue impact (sum of all affected order totals)
                        var totalRevenueImpact = affectedOrders.Sum(o => o.Total);

                        // First and last usage
                        var allAdjustments = promotion.PromotionOrderAdjustments
                            .Cast<object>()
                            .Concat(promotion.LineItemAdjustments)
                            .ToList();

                        DateTimeOffset? firstUsedAt = null;
                        DateTimeOffset? lastUsedAt = null;

                        if (promotion.PromotionOrderAdjustments.Any())
                        {
                            firstUsedAt = promotion.PromotionOrderAdjustments.Min(oa => oa.CreatedAt);
                            lastUsedAt = promotion.PromotionOrderAdjustments.Max(oa => oa.CreatedAt);
                        }

                        // Usage by day (last 30 days)
                        var usageByDay = new Dictionary<string, int>();
                        var thirtyDaysAgo = DateTimeOffset.UtcNow.AddDays(-30);

                        var recentOrders = affectedOrders
                            .Where(o => o.CreatedAt >= thirtyDaysAgo)
                            .GroupBy(o => o.CreatedAt.Date)
                            .ToDictionary(
                                g => g.Key.ToString("yyyy-MM-dd"),
                                g => g.Count()
                            );

                        usageByDay = recentOrders;

                        // Top affected products
                        var topProducts = promotion.LineItemAdjustments
                            .Where(lia => lia.LineItem?.Variant?.Product != null)
                            .GroupBy(lia => new
                            {
                                ProductId = lia.LineItem.Variant.ProductId,
                                ProductName = lia.LineItem.Variant.Product.Name
                            })
                            .Select(g => new Models.TopProductItem
                            {
                                ProductId = g.Key.ProductId,
                                ProductName = g.Key.ProductName,
                                TimesDiscounted = g.Count(),
                                TotalDiscount = g.Sum(lia => Math.Abs(lia.AmountCents)) / 100m
                            })
                            .OrderByDescending(p => p.TotalDiscount)
                            .Take(10)
                            .ToList();

                        // Performance metrics
                        var totalOrdersInPeriod = firstUsedAt.HasValue
                            ? await dbContext.Set<Order>()
                                .CountAsync(o => o.CreatedAt >= firstUsedAt.Value, ct)
                            : 0;

                        var conversionRate = totalOrdersInPeriod > 0
                            ? (double)affectedOrdersCount / totalOrdersInPeriod
                            : 0;

                        var revenuePerUse = promotion.UsageCount > 0
                            ? totalRevenueImpact / promotion.UsageCount
                            : 0m;

                        var costPerAcquisition = affectedOrdersCount > 0
                            ? totalDiscountGiven / affectedOrdersCount
                            : 0m;

                        var roi = totalDiscountGiven > 0
                            ? (double)((totalRevenueImpact - totalDiscountGiven) / totalDiscountGiven)
                            : 0;

                        var result = new Result
                        {
                            PromotionId = promotion.Id,
                            Name = promotion.Name,
                            TotalUsageCount = promotion.UsageCount,
                            RemainingUsage = promotion.RemainingUsage,
                            TotalDiscountGiven = totalDiscountGiven,
                            AverageDiscountPerOrder = averageDiscountPerOrder,
                            AffectedOrdersCount = affectedOrdersCount,
                            TotalRevenueImpact = totalRevenueImpact,
                            FirstUsedAt = firstUsedAt,
                            LastUsedAt = lastUsedAt,
                            UsageByDay = usageByDay,
                            TopAffectedProducts = topProducts,
                            Performance = new Models.PerformanceMetrics
                            {
                                ConversionRate = conversionRate,
                                RevenuePerUse = revenuePerUse,
                                CostPerAcquisition = costPerAcquisition,
                                ReturnOnInvestment = roi
                            }
                        };

                        logger.LogInformation("Retrieved statistics for promotion {PromotionId}: {UsageCount} uses, {TotalDiscount} total discount",
                            query.PromotionId, promotion.UsageCount, totalDiscountGiven);

                        return result;
                    }
                }
            }
        }
    }

}