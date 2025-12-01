namespace ReSys.Core.Domain.Promotions.Calculations;

/// <summary>
/// Represents the result of a promotion calculation, containing the promotion ID and resulting adjustments.
/// </summary>
/// <remarks>
/// <para>
/// <b>Purpose:</b>
/// Encapsulates the output of <see cref="PromotionCalculator.Calculate(Promotions.Promotion, Orders.Order)"/>,
/// providing the adjustments that should be applied to an order when a promotion is successfully applied.
/// </para>
/// 
/// <para>
/// <b>Usage Example:</b>
/// <code>
/// var result = PromotionCalculator.Calculate(promotion, order);
/// if (result.IsError)
/// {
///     return result.FirstError;
/// }
/// 
/// // Apply adjustments to the order
/// foreach (var adjustment in result.Value.Adjustments)
/// {
///     order.Adjustments.Add(adjustment);
/// }
/// </code>
/// </para>
/// 
/// <para>
/// <b>Properties:</b>
/// <list type="bullet">
/// <item><b>PromotionId:</b> The unique identifier of the applied promotion</item>
/// <item><b>Adjustments:</b> Read-only collection of calculated adjustments to apply to the order or line items</item>
/// </list>
/// </para>
/// </remarks>
/// <param name="PromotionId">The unique identifier of the promotion that was applied.</param>
/// <param name="Adjustments">The calculated adjustments resulting from the promotion. May contain multiple adjustments targeting different line items or the order as a whole.</param>
public sealed record PromotionCalculationResult(
    Guid PromotionId,
    IReadOnlyList<PromotionAdjustment> Adjustments);