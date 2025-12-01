using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.LineItems;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Promotions.Calculations;

/// <summary>
/// Provides the context required for a promotion action to calculate adjustments.
/// Encapsulates the promotion, order, and eligible line items needed for discount/adjustment computation.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b>
/// Serves as a data transfer object between <see cref="PromotionCalculator"/> and promotion action implementations.
/// Provides all necessary information for an action (e.g., percentage discount, fixed amount, etc.) to calculate appropriate adjustments.
/// </para>
/// 
/// <para>
/// <b>Workflow:</b>
/// <list type="number">
/// <item><see cref="PromotionCalculator"/> validates the promotion and filters eligible items</item>
/// <item>Creates a <see cref="PromotionCalculationContext"/> with the promotion, order, and eligible items</item>
/// <item>Calls the promotion's action: <c>promotion.Action.Calculate(context)</c></item>
/// <item>The action returns adjustments based on this context</item>
/// <item><see cref="PromotionCalculator"/> caps the discount if needed and returns the result</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
/// <item><b>Promotion:</b> The promotion being applied, including action type and discount configuration</item>
/// <item><b>Order:</b> The complete order context (totals, user, addresses, etc.)</item>
/// <item><b>EligibleItems:</b> Pre-filtered line items that are eligible for discount based on promotion rules</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Context is typically created by PromotionCalculator and passed to the action
/// var context = new PromotionCalculationContext(
///     Promotion: myPromotion,
///     Order: myOrder,
///     EligibleItems: filteredLineItems);
/// 
/// // The promotion's action uses the context to calculate adjustments
/// var adjustments = context.Promotion.Action.Calculate(context);
/// 
/// // Example action implementation:
/// // - Use context.Promotion.DiscountPercent to calculate percentage-based discount
/// // - Use context.Order to access order totals for cap calculations
/// // - Use context.EligibleItems to determine which items get discounted
/// </code>
/// </para>
/// 
/// <para>
/// <b>Key Design Notes:</b>
/// <list type="bullet">
/// <item><b>Immutable:</b> Context is a record and should not be modified after creation</item>
/// <item><b>Pre-filtered Items:</b> EligibleItems are already filtered by include/exclude rules</item>
/// <item><b>Action Responsibility:</b> The promotion action is responsible for interpreting this context and generating adjustments</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="Promotion">The promotion being applied. Contains the action and configuration needed for calculation.</param>
/// <param name="Order">The complete order context including user, totals, and line items.</param>
/// <param name="EligibleItems">Pre-filtered line items that satisfy the promotion's include/exclude rules and are eligible for discount.</param>
public sealed record PromotionCalculationContext(
    Promotion Promotion,
    Order Order,
    IReadOnlyList<LineItem> EligibleItems);

#region Properties
// Properties are implicitly defined in a record's primary constructor
#endregion
