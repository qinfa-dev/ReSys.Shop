using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Orders.Adjustments;

/// <summary>
/// Represents a financial adjustment applied to an order. The adjustment can be scoped to the entire order,
/// a specific line item, or a shipment. This entity consolidates all adjustments into a single model.
/// </summary>
/// <remarks>
/// KEY PATTERN:
/// OrderAdjustment is an owned entity of the Order aggregate and is accessed only through the Order.Adjustments collection.
/// This consolidation simplifies promotion application, tax calculation, and totals computation.
/// 
/// ADJUSTMENT SCOPES:
/// • Order: An adjustment applied to the entire order (e.g., "$10 off orders over $100").
/// • LineItem: An adjustment applied to a specific line item (e.g., "20% off this product"). `LineItemId` must be set.
/// • Shipping: An adjustment applied to the shipping cost (e.g., "Free shipping promotion").
///
/// FINANCIAL & PROMOTION IMPACT:
/// The financial and promotion tracking semantics remain the same, but are now unified. All calculations
/// are performed on the single `Order.Adjustments` collection, filtering by `Scope` where necessary.
/// </remarks>
public sealed class OrderAdjustment : AuditableEntity<Guid>
{
    /// <summary>
    /// Defines the scope of the adjustment to determine if it applies to the
    /// entire order, a specific line item, or shipping.
    /// </summary>
    public enum AdjustmentScope { Order, LineItem, Shipping }

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

        /// <summary>Triggered when a line item-scoped adjustment is missing a LineItemId.</summary>
        public static Error LineItemIdRequiredForLineItemScope => Error.Validation(code: "OrderAdjustment.LineItemIdRequired", description: "A LineItemId is required for adjustments with a 'LineItem' scope.");
    }
    #endregion

    #region Properties
    /// <summary>Foreign key reference to the parent Order.</summary>
    public Guid OrderId { get; set; }
    
    /// <summary>
    /// Foreign key reference to the LineItem this adjustment applies to.
    /// This is only set when the `Scope` is `LineItem`.
    /// </summary>
    public Guid? LineItemId { get; set; }

    /// <summary>
    /// The scope of this adjustment (Order, LineItem, or Shipping).
    /// </summary>
    public AdjustmentScope Scope { get; set; }

    /// <summary>
    /// Whether this adjustment is currently eligible and should be applied to totals.
    /// Adjustments that become ineligible are preserved on the order but do not
    /// contribute to totals until reinstated.
    /// </summary>
    public bool Eligible { get; set; } = true;

    /// <summary>
    /// If true, this adjustment is mandatory and should be persisted even when
    /// the computed amount is zero. Useful for explicitly marking taxes or shipping
    /// charges that are intentionally zero.
    /// </summary>
    public bool Mandatory { get; set; }

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
    public static ErrorOr<OrderAdjustment> Create(
        Guid orderId, 
        long amountCents, 
        string description, 
        AdjustmentScope scope,
        Guid? lineItemId = null,
        Guid? promotionId = null,
        bool eligible = true,
        bool mandatory = false)
    {
        if (string.IsNullOrWhiteSpace(value: description)) return Errors.DescriptionRequired;
        if (description.Length > Constraints.DescriptionMaxLength) return Errors.DescriptionTooLong;
        if (scope == AdjustmentScope.LineItem && !lineItemId.HasValue) return Errors.LineItemIdRequiredForLineItemScope;

        return new OrderAdjustment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            AmountCents = amountCents,
            Description = description,
            Scope = scope,
            LineItemId = lineItemId,
            PromotionId = promotionId,
            Eligible = eligible,
            Mandatory = mandatory,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion
}