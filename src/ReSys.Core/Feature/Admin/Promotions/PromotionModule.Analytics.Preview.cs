using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Promotions.Calculations;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Analytics
    {
        public static class Preview
        {
            public sealed record Request
            {
                public Guid OrderId { get; set; }
            }

            public sealed record Result : Models.PreviewResult;
            public sealed record Command(Guid PromotionId, Request Request) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.PromotionId)
                        .NotEmpty().WithMessage("Promotion ID is required for preview.")
                        .WithErrorCode("Promotion.Id.Required");
                    RuleFor(x => x.Request.OrderId)
                        .NotEmpty().WithMessage("Order ID is required for preview.")
                        .WithErrorCode("Order.Id.Required");
                }
            }

            public sealed class CommandHandler(
                IApplicationDbContext dbContext,
                ILogger<CommandHandler> logger)
                : ICommandHandler<Command, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
                {
                    var promotion = await dbContext.Set<Promotion>()
                        .Include(p => p.PromotionRules)
                        .ThenInclude(pr => pr.PromotionRuleTaxons)
                        .Include(p => p.PromotionRules)
                        .ThenInclude(pr => pr.PromotionRuleUsers)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == command.PromotionId, ct);

                    if (promotion == null)
                        return Promotion.Errors.NotFound(command.PromotionId);

                    var order = await dbContext.Set<Order>()
                        .Include(o => o.LineItems)
                        .ThenInclude(li => li.Variant)
                        .ThenInclude(v => v.Product)
                        .ThenInclude(p => p.Taxons)
                        .Include(o => o.User)
                        .AsNoTracking()
                        .FirstOrDefaultAsync(o => o.Id == command.Request.OrderId, ct);

                    if (order == null)
                        return Error.NotFound("Order.NotFound", $"Order with ID '{command.Request.OrderId}' was not found");

                    var result = new Result
                    {
                        IsApplicable = true,
                        OriginalTotal = order.Total,
                        RuleEvaluations = new List<string>()
                    };

                    // Evaluate each rule
                    foreach (var rule in promotion.PromotionRules)
                    {
                        var ruleResult = rule.Evaluate(order);
                        var ruleDescription = $"{rule.Type}: {(ruleResult ? "✓ PASSED" : "✗ FAILED")} - {rule.Value}";
                        result.RuleEvaluations.Add(ruleDescription);

                        if (!ruleResult)
                        {
                            result.IsApplicable = false;
                            result.ReasonNotApplicable = $"Rule '{rule.Type}' not met: {rule.Value}";
                        }
                    }

                    // Check promotion validity
                    if (promotion.StartsAt.HasValue && promotion.StartsAt > DateTimeOffset.UtcNow)
                    {
                        result.IsApplicable = false;
                        result.ReasonNotApplicable = "Promotion has not started yet";
                        result.RuleEvaluations.Add("✗ Start Date: Not yet started");
                    }
                    else
                    {
                        result.RuleEvaluations.Add("✓ Start Date: Valid");
                    }

                    if (!promotion.IsActive)
                    {
                        result.IsApplicable = false;
                        result.ReasonNotApplicable = "Promotion is not active";
                        result.RuleEvaluations.Add("✗ Status: Inactive");
                    }
                    else
                    {
                        result.RuleEvaluations.Add("✓ Status: Active");
                    }

                    if (promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit)
                    {
                        result.IsApplicable = false;
                        result.ReasonNotApplicable = "Promotion usage limit reached";
                        result.RuleEvaluations.Add($"✗ Usage Limit: {promotion.UsageCount}/{promotion.UsageLimit}");
                    }
                    else if (promotion.UsageLimit.HasValue)
                    {
                        result.RuleEvaluations.Add($"✓ Usage Limit: {promotion.UsageCount}/{promotion.UsageLimit}");
                    }
                    else
                    {
                        result.RuleEvaluations.Add("✓ Usage Limit: Unlimited");
                    }

                    if (promotion.MinimumOrderAmount.HasValue && order.ItemTotal < promotion.MinimumOrderAmount)
                    {
                        result.IsApplicable = false;
                        result.ReasonNotApplicable = $"Order total ({order.ItemTotal:C}) does not meet minimum ({promotion.MinimumOrderAmount:C})";
                        result.RuleEvaluations.Add($"✗ Minimum Order: {order.ItemTotal:C} < {promotion.MinimumOrderAmount:C}");
                    }
                    else if (promotion.MinimumOrderAmount.HasValue)
                    {
                        result.RuleEvaluations.Add($"✓ Minimum Order: {order.ItemTotal:C} >= {promotion.MinimumOrderAmount:C}");
                    }

                    // Calculate discount if applicable
                    if (result.IsApplicable)
                    {
                        try
                        {
                            var calculationResult = PromotionCalculator.Calculate(promotion, order);

                            if (calculationResult.IsError)
                            {
                                result.IsApplicable = false;
                                result.ReasonNotApplicable = calculationResult.FirstError.Description;
                            }
                            else
                            {
                                var calcResult = calculationResult.Value;

                                foreach (var adjustment in calcResult.Adjustments)
                                {
                                    var lineItemName = adjustment.LineItemId.HasValue
                                        ? order.LineItems.FirstOrDefault(li => li.Id == adjustment.LineItemId.Value)?.Variant?.Product?.Name
                                        : null;

                                    result.Adjustments.Add(new Models.PreviewAdjustment
                                    {
                                        Description = adjustment.Description,
                                        Amount = adjustment.Amount / 100m,
                                        LineItemId = adjustment.LineItemId,
                                        LineItemName = lineItemName
                                    });
                                }

                                result.TotalDiscount = result.Adjustments.Sum(a => Math.Abs(a.Amount));
                                result.FinalTotal = result.OriginalTotal - result.TotalDiscount;

                                if (promotion.MaximumDiscountAmount.HasValue && result.TotalDiscount > promotion.MaximumDiscountAmount)
                                {
                                    result.RuleEvaluations.Add($"⚠ Discount Capped: {result.TotalDiscount:C} → {promotion.MaximumDiscountAmount:C}");
                                    result.TotalDiscount = promotion.MaximumDiscountAmount.Value;
                                    result.FinalTotal = result.OriginalTotal - result.TotalDiscount;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            logger.LogError(ex, "Error calculating promotion preview for {PromotionId} on order {OrderId}",
                                command.PromotionId, command.Request.OrderId);
                            result.IsApplicable = false;
                            result.ReasonNotApplicable = "Error calculating discount";
                        }
                    }
                    else
                    {
                        result.FinalTotal = result.OriginalTotal;
                    }

                    logger.LogInformation("Preview calculated for promotion {PromotionId} on order {OrderId}: Applicable={Applicable}, Discount={Discount}",
                        command.PromotionId, command.Request.OrderId, result.IsApplicable, result.TotalDiscount);

                    return result;
                }
            }
        }
    }
}