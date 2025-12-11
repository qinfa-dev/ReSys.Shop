using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.LineItems;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.Promotions.Rules;

namespace ReSys.Core.Domain.Promotions.Calculations;

/// <summary>
/// Calculates promotion discounts and adjustments based on promotion rules and configuration.
/// 
/// <para>
/// <b>Responsibility:</b>
/// This calculator validates promotion eligibility, determines which line items are eligible for discounts,
/// applies the promotion action (discount/adjustment), and enforces discount caps.
/// </para>
/// 
/// <para>
/// <b>Process Flow:</b>
/// <list type="number">
/// <item>Validate promotion is eligible (started, active, within usage limits)</item>
/// <item>Evaluate all promotion rules against the order</item>
/// <item>Check minimum order amount requirement</item>
/// <item>Filter eligible line items based on include/exclude rules</item>
/// <item>Calculate adjustments using the promotion action</item>
/// <item>Cap discount if maximum discount limit is configured</item>
/// <item>Return calculation result with adjustments</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Key Concepts:</b>
/// <list type="bullet">
/// <item><b>Eligibility Checks:</b> Ensures promotion can be applied (availability window, active status, usage limits)</item>
/// <item><b>Rule Evaluation:</b> Order-level rules (FirstOrder, MinimumQuantity, UserRole) must ALL be satisfied</item>
/// <item><b>Item Filtering:</b> Line-item rules (ProductInclude/Exclude, CategoryInclude/Exclude) determine which items get discounted</item>
/// <item><b>Promotional Items:</b> Items already marked as promotional are excluded from discount consideration</item>
/// <item><b>Discount Capping:</b> Total discount cannot exceed MaximumDiscountAmount if configured</item>
/// </list>
/// </para>
/// </summary>
public static class PromotionCalculator
{
    #region Public API

    /// <summary>
    /// Calculates promotion adjustments for the given order based on promotion rules and configuration.
    /// </summary>
    /// <param name="promotion">The promotion to apply. Must not be null.</param>
    /// <param name="order">The order to apply the promotion to. Must not be null.</param>
    /// <returns>
    /// On success: <see cref="PromotionCalculationResult"/> containing the promotion ID and list of adjustments.
    /// On failure: <see cref="Error"/> indicating why the promotion cannot be applied.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Validation Order (fails fast on first error):</b>
    /// <list type="number">
    /// <item>Promotion start date must have passed (StartsAt)</item>
    /// <item>Promotion must be marked as active (IsActive)</item>
    /// <item>Usage must not exceed limit (UsageCount < UsageLimit)</item>
    /// <item>ALL rules must be satisfied (PromotionRules.All())</item>
    /// <item>Order total must meet minimum (MinimumOrderAmount)</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <b>Examples:</b>
    /// </para>
    /// <example>
    /// <code>
    /// // Apply a 20% off promotion to an order
    /// var result = PromotionCalculator.Calculate(promotion, order);
    /// if (result.IsError)
    /// {
    ///     // Handle error - e.g., "Promotion not yet started"
    ///     return result.FirstError;
    /// }
    /// 
    /// // Apply adjustments to order
    /// foreach (var adj in result.Value.Adjustments)
    /// {
    ///     order.Adjustments.Add(adj);
    /// }
    /// </code>
    /// </example>
    /// </remarks>
    /// <exception cref="InvalidOperationException">Thrown if promotion.Action is not configured.</exception>
    public static ErrorOr<PromotionCalculationResult> Calculate(Promotion promotion, Order order)
    {
        // Step 1: Validate promotion eligibility
        var eligibilityError = ValidatePromotionEligibility(promotion);
        if (eligibilityError is not null)
            return (ErrorOr<PromotionCalculationResult>)eligibilityError;

        // Step 2: Evaluate order-level rules
        if (!promotion.PromotionRules.All(predicate: r => r.Evaluate(order: order)))
            return Error.Validation(
                code: "Promotion.RulesNotMet",
                description: "One or more promotion rules are not satisfied.");

        // Step 3: Check minimum order amount
        if (promotion.MinimumOrderAmount.HasValue && order.ItemTotal < promotion.MinimumOrderAmount)
            return Promotion.Errors.MinimumOrderNotMet(minimum: promotion.MinimumOrderAmount.Value);

        // Step 4: Determine eligible line items
        var eligibleItems = GetEligibleLineItems(promotion: promotion, order: order);

        // Step 5: Create calculation context and compute adjustments
        var context = new PromotionCalculationContext(
            Promotion: promotion,
            Order: order,
            EligibleItems: eligibleItems);

        if (promotion.Action is null)
        {
            throw new InvalidOperationException(
                "Promotion action must be configured before calculating adjustments.");
        }

        var adjustments = promotion.Action.Calculate(context: context);

        // Step 6: Apply discount cap if configured
        var cappedAdjustments = ApplyDiscountCap(adjustments: adjustments, promotion: promotion);

        return new PromotionCalculationResult(
            PromotionId: promotion.Id,
            Adjustments: cappedAdjustments);
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// Validates that the promotion is eligible to be applied based on timing, status, and usage limits.
    /// </summary>
    /// <param name="promotion">The promotion to validate.</param>
    /// <returns>An error if validation fails; null if validation succeeds.</returns>
    /// <remarks>
    /// Checks are performed in this order:
    /// <list type="number">
    /// <item>StartsAt: Promotion must have started (if configured)</item>
    /// <item>IsActive: Promotion must be marked as active</item>
    /// <item>UsageLimit: Usage count must not exceed configured limit</item>
    /// </list>
    /// </remarks>
    private static Error? ValidatePromotionEligibility(Promotion promotion)
    {
        // Check if promotion has not yet started
        if (promotion.StartsAt.HasValue && promotion.StartsAt > DateTimeOffset.UtcNow)
            return Promotion.Errors.NoStarted;

        // Check if promotion is not active
        if (!promotion.IsActive)
            return Promotion.Errors.NotActive;

        // Check if promotion has reached usage limit
        if (promotion.UsageLimit.HasValue && promotion.UsageCount >= promotion.UsageLimit)
            return Promotion.Errors.UsageLimitReached;

        return null;
    }

    /// <summary>
    /// Determines which line items are eligible for discount based on promotion rules.
    /// Applies include/exclude filters for products and categories.
    /// </summary>
    /// <param name="promotion">The promotion containing filter rules.</param>
    /// <param name="order">The order containing line items to filter.</param>
    /// <returns>A read-only list of eligible line items. Excludes already promotional items.</returns>
    /// <remarks>
    /// <para>
    /// <b>Item Filtering Logic:</b>
    /// <list type="bullet">
    /// <item>Starts with all non-promotional items (IsPromotional == false)</item>
    /// <item>ProductInclude: Keep only items with specified product</item>
    /// <item>ProductExclude: Remove items with specified product</item>
    /// <item>CategoryInclude: Keep only items with specified category/taxon</item>
    /// <item>CategoryExclude: Remove items with specified category/taxon</item>
    /// <item>MinimumQuantity/UserRole: Order-level rules, skipped here</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <b>Filter Application:</b>
    /// Filters are applied sequentially (chained). If a promotion has both include and exclude rules,
    /// items must satisfy all conditions to be eligible.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<LineItem> GetEligibleLineItems(Promotion promotion, Order order)
    {
        // Start with non-promotional items only
        var eligibleLineItems = order.LineItems
            .Where(predicate: li => !li.IsPromotional)
            .AsEnumerable();

        // Apply each rule's filtering logic
        foreach (var rule in promotion.PromotionRules)
        {
            eligibleLineItems = ApplyRuleFilter(rule: rule, items: eligibleLineItems);
        }

        return eligibleLineItems.ToList();
    }

    /// <summary>
    /// Applies a single promotion rule's filtering logic to a set of line items.
    /// </summary>
    /// <param name="rule">The rule to apply.</param>
    /// <param name="items">The line items to filter.</param>
    /// <returns>Filtered line items based on the rule type.</returns>
    /// <remarks>
    /// <para>
    /// <b>Rule Type Handling:</b>
    /// <list type="bullet">
    /// <item><b>ProductInclude:</b> Keep only items with Product ID matching rule.Value (parsed as Guid)</item>
    /// <item><b>ProductExclude:</b> Remove items with Product ID matching rule.Value</item>
    /// <item><b>CategoryInclude:</b> Keep only items with products in specified taxons/categories</item>
    /// <item><b>CategoryExclude:</b> Remove items with products in specified taxons/categories</item>
    /// <item><b>MinimumQuantity/UserRole:</b> Order-level rules, not applicable at item level</item>
    /// </list>
    /// </para>
    /// </remarks>
    private static IEnumerable<LineItem> ApplyRuleFilter(PromotionRule rule, IEnumerable<LineItem> items)
    {
        return rule.Type switch
        {
            PromotionRule.RuleType.ProductInclude => ApplyProductIncludeFilter(rule: rule, items: items),
            PromotionRule.RuleType.ProductExclude => ApplyProductExcludeFilter(rule: rule, items: items),
            PromotionRule.RuleType.CategoryInclude => ApplyCategoryIncludeFilter(rule: rule, items: items),
            PromotionRule.RuleType.CategoryExclude => ApplyCategoryExcludeFilter(rule: rule, items: items),
            // Order-level rules don't affect item filtering
            PromotionRule.RuleType.MinimumQuantity => items,
            PromotionRule.RuleType.UserRole => items,
            PromotionRule.RuleType.FirstOrder => items,
            _ => items
        };
    }

    /// <summary>
    /// Filters line items to only include those with the specified product ID.
    /// </summary>
    /// <param name="rule">The product include rule containing the product ID.</param>
    /// <param name="items">The line items to filter.</param>
    /// <returns>Only items with the specified product.</returns>
    private static IEnumerable<LineItem> ApplyProductIncludeFilter(PromotionRule rule, IEnumerable<LineItem> items)
    {
        if (!Guid.TryParse(input: rule.Value, result: out Guid productId))
            return items; // Invalid product ID, include all items

        return items.Where(predicate: li => li.Variant.ProductId == productId);
    }

    /// <summary>
    /// Filters line items to exclude those with the specified product ID.
    /// </summary>
    /// <param name="rule">The product exclude rule containing the product ID.</param>
    /// <param name="items">The line items to filter.</param>
    /// <returns>Items excluding those with the specified product.</returns>
    private static IEnumerable<LineItem> ApplyProductExcludeFilter(PromotionRule rule, IEnumerable<LineItem> items)
    {
        if (!Guid.TryParse(input: rule.Value, result: out Guid productId))
            return items; // Invalid product ID, exclude nothing

        return items.Where(predicate: li => li.Variant.ProductId != productId);
    }

    /// <summary>
    /// Filters line items to only include those with products in the specified categories/taxons.
    /// </summary>
    /// <param name="rule">The category include rule containing taxon references.</param>
    /// <param name="items">The line items to filter.</param>
    /// <returns>Only items with products in the specified categories.</returns>
    private static IEnumerable<LineItem> ApplyCategoryIncludeFilter(PromotionRule rule, IEnumerable<LineItem> items)
    {
        return items.Where(predicate: li =>
            rule.PromotionRuleTaxons.Any(predicate: prt =>
                li.Variant.Product.Taxons.Any(predicate: t => t.Id == prt.TaxonId)));
    }

    /// <summary>
    /// Filters line items to exclude those with products in the specified categories/taxons.
    /// </summary>
    /// <param name="rule">The category exclude rule containing taxon references.</param>
    /// <param name="items">The line items to filter.</param>
    /// <returns>Items excluding those with products in the specified categories.</returns>
    private static IEnumerable<LineItem> ApplyCategoryExcludeFilter(PromotionRule rule, IEnumerable<LineItem> items)
    {
        return items.Where(predicate: li =>
            !rule.PromotionRuleTaxons.Any(predicate: prt =>
                li.Variant.Product.Taxons.Any(predicate: t => t.Id == prt.TaxonId)));
    }

    /// <summary>
    /// Applies the discount cap to adjustments if a maximum discount amount is configured.
    /// </summary>
    /// <param name="adjustments">The calculated adjustments.</param>
    /// <param name="promotion">The promotion containing the maximum discount configuration.</param>
    /// <returns>Adjustments that respect the maximum discount limit.</returns>
    /// <remarks>
    /// <para>
    /// If the total discount exceeds MaximumDiscountAmount:
    /// <list type="bullet">
    /// <item>Replaces all adjustments with a single capped adjustment</item>
    /// <item>Total discount = min(calculated discount, MaximumDiscountAmount)</item>
    /// <item>Adjustment description indicates the discount was capped</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <b>Example:</b>
    /// If calculated discount is $50 but MaximumDiscountAmount is $25,
    /// returns a single adjustment for -$25.
    /// </para>
    /// </remarks>
    private static IReadOnlyList<PromotionAdjustment> ApplyDiscountCap(
        IReadOnlyList<PromotionAdjustment> adjustments,
        Promotion promotion)
    {
        // No cap configured, return adjustments as-is
        if (!promotion.MaximumDiscountAmount.HasValue)
            return adjustments;

        // Calculate total discount (adjustments are negative)
        var totalDiscount = adjustments.Sum(selector: a => a.Amount) / -100m;

        // Cap is not exceeded, return adjustments as-is
        if (totalDiscount <= promotion.MaximumDiscountAmount.Value)
            return adjustments;

        // Cap is exceeded, return single capped adjustment
        var cappedDiscountAmount = promotion.MaximumDiscountAmount.Value;
        return
        [
            new(
                Description: $"Discount: {promotion.Name} (Capped)",
                Amount: -(long)(cappedDiscountAmount * 100))
        ];
    }

    #endregion
}