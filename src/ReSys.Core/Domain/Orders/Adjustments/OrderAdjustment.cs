using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Orders.Adjustments;

/// <summary>
/// Represents a financial adjustment at the order level (not line-item specific).
/// Typically used for order-wide discounts, taxes, or fees.
/// </summary>
/// <remarks>
/// KEY PATTERN:
/// OrderAdjustment is an owned entity of Order; accessed only through Order.Adjustments collection.
/// 
/// PURPOSE:
/// Tracks financial adjustments applied to the entire order rather than specific items.
/// Examples:
/// • Order-level promotion discount (e.g., "$10 off orders over $100")
/// • Shipping discount
/// • Gift card credit
/// • Service fee
/// • Tax calculations
/// 
/// ADJUSTMENT VALUES:
/// • Negative values: Discounts, credits (reduce total)
/// • Positive values: Taxes, fees, surcharges (increase total)
/// • Amount stored in cents (long) for precision
/// 
/// PROMOTION TRACKING:
/// • PromotionId: Links adjustment to promotion that created it
/// • IsPromotion: Computed property to identify promotion-related adjustments
/// • Used to distinguish promotion adjustments from tax/fee adjustments
/// • Promotions cleared when new promotion applied; non-promotion adjustments preserved
/// 
/// CALCULATION IMPACT:
/// Order.AdjustmentTotalCents = SUM(OrderAdjustment.AmountCents) + SUM(LineItem.Adjustments.Sum(a => a.AmountCents))
/// Order.TotalCents = ItemTotalCents + ShipmentTotalCents + AdjustmentTotalCents
/// </remarks>
public sealed class OrderAdjustment : AuditableEntity<Guid>
{
    #region Constraints
    /// <summary>Defines validation limits for OrderAdjustment.</summary>
    public static class Constraints
    {
        /// <summary>Maximum length for adjustment description.</summary>
        public const int DescriptionMaxLength = CommonInput.Constraints.Text.LongTextMaxLength;
        
        /// <summary>Minimum amount in cents (allows negative for discounts/credits).</summary>
        public const long AmountCentsMinValue = long.MinValue; // Adjustments can be negative for discounts
    }
    #endregion

    #region Errors
    /// <summary>Defines error scenarios for OrderAdjustment operations.</summary>
    public static class Errors
    {
        /// <summary>Triggered when amount validation fails.</summary>
        public static Error InvalidAmountCents => Error.Validation(code: "OrderAdjustment.InvalidAmountCents", description: $"Amount cents must be at least {Constraints.AmountCentsMinValue}.");
        
        /// <summary>Triggered when description is missing or empty.</summary>
        public static Error DescriptionRequired => CommonInput.Errors.Required(prefix: nameof(OrderAdjustment), field: nameof(Description));
        
        /// <summary>Triggered when description exceeds maximum length.</summary>
        public static Error DescriptionTooLong => CommonInput.Errors.TooLong(prefix: nameof(OrderAdjustment), field: nameof(Description), maxLength: Constraints.DescriptionMaxLength);
    }
    #endregion

    #region Properties
    /// <summary>Foreign key reference to the parent Order.</summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Foreign key reference to the Promotion that created this adjustment (nullable).
    /// Set only if this adjustment comes from a promotion; null for manual adjustments (tax, fee).
    /// </summary>
    public Guid? PromotionId { get; set; }
    
    /// <summary>
    /// Amount of adjustment in cents (negative for discount, positive for fee/tax).
    /// Negative values reduce order total; positive values increase it.
    /// Example: -500 cents = $5.00 discount, +800 cents = $8.00 fee
    /// </summary>
    public long AmountCents { get; set; }
    
    /// <summary>
    /// Human-readable description of the adjustment.
    /// Examples: "10% promotion discount", "Sales tax (8.5%)", "Shipping surcharge"
    /// Used in order summaries and customer communications.
    /// </summary>
    public string Description { get; set; } = string.Empty;
    #endregion

    #region Relationships
    /// <summary>Reference to the parent Order aggregate.</summary>
    public Order Order { get; set; } = null!;
    
    /// <summary>Optional reference to the Promotion that created this adjustment.</summary>
    public Promotion? Promotion { get; set; }
    #endregion

    #region Computed Properties
    /// <summary>
    /// Indicates whether this adjustment came from a promotion.
    /// Used to identify which adjustments to clear when promotion changes.
    /// </summary>
    public bool IsPromotion => PromotionId.HasValue;
    #endregion

    #region Constructors
    /// <summary>Private constructor; use Create() factory method for validation.</summary>
    private OrderAdjustment() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new order-level adjustment.
    /// </summary>
    /// <remarks>
    /// Validates description is provided and within length limits.
    /// Does not validate amount (no range restrictions other than type limits).
    /// 
    /// Typical usage:
    /// <code>
    /// // Create promotion adjustment
    /// var adjustment = OrderAdjustment.Create(
    ///     orderId: order.Id,
    ///     amountCents: -1000,  // $10.00 discount
    ///     description: "10% promotion discount",
    ///     promotionId: promo.Id);
    /// 
    /// // Create tax adjustment
    /// var tax = OrderAdjustment.Create(
    ///     orderId: order.Id,
    ///     amountCents: 800,  // $8.00 tax
    ///     description: "Sales tax (8.0%)");
    /// 
    /// if (adjustment.IsError) return Problem(adjustment.FirstError);
    /// </code>
    /// </remarks>
    public static ErrorOr<OrderAdjustment> Create(Guid orderId, long amountCents, string description, Guid? promotionId = null)
    {
        if (string.IsNullOrWhiteSpace(value: description)) return Errors.DescriptionRequired;
        if (description.Length > Constraints.DescriptionMaxLength) return Errors.DescriptionTooLong;

        return new OrderAdjustment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AmountCents = amountCents,
            Description = description,
            PromotionId = promotionId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion
}