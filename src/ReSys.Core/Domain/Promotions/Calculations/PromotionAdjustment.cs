namespace ReSys.Core.Domain.Promotions.Calculations;

/// <summary>
/// Represents a single adjustment (discount or reward) to be applied to an order or line item as a result of a promotion.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b>
/// Encapsulates the output of a promotion action, representing either a discount or an adjustment amount
/// that should be applied to the order. Multiple adjustments can be applied from a single promotion,
/// each potentially targeting a different line item or the order as a whole.
/// </para>
/// 
/// <para>
/// <b>Amount Representation:</b>
/// <list type="bullet">
/// <item><b>Negative Values:</b> Represent discounts (e.g., -5000 = -$50.00)</item>
/// <item><b>Positive Values:</b> Represent surcharges or additional fees (e.g., 1000 = $10.00)</item>
/// <item><b>Units:</b> Amounts are always in cents (1/100 of the currency unit)</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Target Scope:</b>
/// <list type="bullet">
/// <item><b>Order-level:</b> If LineItemId is null, adjustment applies to the entire order</item>
/// <item><b>Line-item level:</b> If LineItemId has a value, adjustment applies only to that specific line item</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create adjustments from promotion calculation
/// var result = PromotionCalculator.Calculate(promotion, order);
/// 
/// // Apply each adjustment
/// foreach (var adjustment in result.Value.Adjustments)
/// {
///     if (adjustment.LineItemId.HasValue)
///     {
///         // Apply to specific line item
///         var lineItem = order.LineItems.First(li => li.Id == adjustment.LineItemId);
///         lineItem.Adjustments.Add(adjustment);
///     }
///     else
///     {
///         // Apply to order
///         order.Adjustments.Add(adjustment);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <param name="Description">Human-readable description of the adjustment (e.g., "20% Discount: Summer Sale"). Used for display and audit purposes.</param>
/// <param name="Amount">The adjustment amount in cents. Negative for discounts, positive for surcharges. Examples: -5000 (= -$50.00), 1000 (= $10.00).</param>
/// <param name="LineItemId">Optional. If specified, adjustment applies only to this line item. If null, adjustment applies to the entire order.</param>
/// <summary>
/// Represents a single adjustment (discount or reward) to be applied to an order or line item as a result of a promotion.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b>
/// Encapsulates the output of a promotion action, representing either a discount or an adjustment amount
/// that should be applied to the order. Multiple adjustments can be applied from a single promotion,
/// each potentially targeting a different line item or the order as a whole.
/// </para>
///
/// <para>
/// <b>Amount Representation:</b>
/// <list type="bullet">
/// <item><b>Negative Values:</b> Represent discounts (e.g., -5000 = -$50.00)</item>
/// <item><b>Positive Values:</b> Represent surcharges or additional fees (e.g., 1000 = $10.00)</item>
/// <item><b>Units:</b> Amounts are always in cents (1/100 of the currency unit)</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Target Scope:</b>
/// <list type="bullet">
/// <item><b>Order-level:</b> If LineItemId is null, adjustment applies to the entire order</item>
/// <item><b>Line-item level:</b> If LineItemId has a value, adjustment applies only to that specific line item</item>
/// </list>
/// </para>
///
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// // Create adjustments from promotion calculation
/// var result = PromotionCalculator.Calculate(promotion, order);
///
/// // Apply each adjustment
/// foreach (var adjustment in result.Value.Adjustments)
/// {
///     if (adjustment.LineItemId.HasValue)
///     {
///         // Apply to specific line item
///         var lineItem = order.LineItems.First(li => li.Id == adjustment.LineItemId);
///         lineItem.Adjustments.Add(adjustment);
///     }
///     else
///     {
///         // Apply to order
///         order.Adjustments.Add(adjustment);
///     }
/// }
/// </code>
/// </para>
/// </remarks>
/// <param name="Description">Human-readable description of the adjustment (e.g., "20% Discount: Summer Sale"). Used for display and audit purposes.</param>
/// <param name="Amount">The adjustment amount in cents. Negative for discounts, positive for surcharges. Examples: -5000 (= -$50.00), 1000 (= $10.00).</param>
/// <param name="LineItemId">Optional. If specified, adjustment applies only to this line item. If null, adjustment applies to the entire order.</param>
public sealed record PromotionAdjustment(
    string Description,
    long Amount,
    Guid? LineItemId = null);

#region Properties
// Properties are implicitly defined in a record's primary constructor
#endregion