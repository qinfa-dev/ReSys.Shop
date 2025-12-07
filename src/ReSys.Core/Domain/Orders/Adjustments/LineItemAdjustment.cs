//using ErrorOr;

//using ReSys.Core.Common.Constants;
//using ReSys.Core.Common.Domain.Entities;
//using ReSys.Core.Domain.Orders.LineItems;
//using ReSys.Core.Domain.Promotions.Promotions;

//namespace ReSys.Core.Domain.Orders.Adjustments;

///// <summary>
///// Represents a financial adjustment applied at the line-item level (individual product in an order).
///// </summary>
///// <remarks>
///// <para>
///// <strong>Role in Order Domain:</strong> Part of the two-level adjustment system that enables flexible pricing:
///// <list type="bullet">
///// <item>
///// <term>LineItemAdjustment</term>
///// <description>Item-specific adjustments (e.g., product-specific discount, quantity-based promotion)</description>
///// </item>
///// <item>
///// <term>OrderAdjustment</term>
///// <description>Order-wide adjustments (e.g., global promotion discount, sales tax, shipping surcharge)</description>
///// </item>
///// </list>
///// </para>
/////
///// <para>
///// <strong>Financial Semantics:</strong>
///// <list type="bullet">
///// <item>
///// <term>Negative amounts (-500 cents)</term>
///// <description>Represent discounts or credits applied to the line item (reduces final cost)</description>
///// </item>
///// <item>
///// <term>Positive amounts (+200 cents)</term>
///// <description>Represent fees or surcharges applied to the line item (increases final cost)</description>
///// </item>
///// </list>
///// </para>
/////
///// <para>
///// <strong>Two-Level System Integration:</strong>
///// <list type="bullet">
///// <item>
///// <term>LineItem.TotalCents</term>
///// <description>SubtotalCents (UnitPriceCents × Quantity) + SUM(LineItemAdjustment.AmountCents)</description>
///// </item>
///// <item>
///// <term>Order.AdjustmentTotalCents</term>
///// <description>SUM(OrderAdjustment.AmountCents) + SUM(LineItem.LineItemAdjustment.AmountCents) for all items</description>
///// </item>
///// <item>
///// <term>Order.TotalCents</term>
///// <description>SUM(LineItem.TotalCents) + Order.AdjustmentTotalCents</description>
///// </item>
///// </list>
///// </para>
/////
///// <para>
///// <strong>Promotion Tracking:</strong>
///// When PromotionId is set, the adjustment is marked as promotion-driven. This enables:
///// <list type="bullet">
///// <item>Clearing all promotion adjustments when user changes to a different promotion (via Order.ApplyPromotion)</item>
///// <item>Distinguishing between manual adjustments (PromotionId = null) and promotional adjustments (PromotionId = promotion.Id)</item>
///// <item>Auditing which promotion contributed to the final price</item>
///// </list>
///// </para>
/////
///// <para>
///// <strong>Common Usage Patterns:</strong>
///// <code>
///// // Example 1: Buy 2+ items of product X, get 10% discount per item
///// var item1 = order.LineItems.First(); // Qty: 3
///// // System creates 3 LineItemAdjustments, each -300 cents ($3.00)
///// 
///// // Example 2: Free shipping on items over $100 (applied as line-item adjustment)
///// var expensiveItem = order.LineItems.First(li =&gt; li.SubtotalCents &gt; 10000);
///// var result = expensiveItem.AddAdjustment(
/////     amountCents: -200,  // $2.00 line item discount
/////     description: "Free item qualifying promotion",
/////     promotionId: promo.Id);
///// 
///// // Example 3: Tax calculated at line level (e.g., different tax rates per item)
///// // Sales tax is typically added per-item rather than order-level
///// var taxAdjustment = LineItemAdjustment.Create(
/////     lineItemId: item.Id,
/////     amountCents: 580,  // $5.80 tax on this item
/////     description: "Sales tax (8.0%)");
///// </code>
///// </para>
/////
///// <para>
///// <strong>Clearing Adjustments:</strong>
///// When a promotion is changed via Order.ApplyPromotion(newPromo), all LineItemAdjustments with PromotionId set are removed
///// to avoid stacking promotions. Manual adjustments (PromotionId = null) are preserved.
///// </para>
///// </remarks>
//public sealed class LineItemAdjustment : AuditableEntity<Guid>
//{
//    #region Constraints
//    /// <summary>Validation boundaries for LineItemAdjustment operations.</summary>
//    public static class Constraints
//    {
//        /// <summary>Maximum length for adjustment description (e.g., "10% promotion discount").</summary>
//        public const int DescriptionMaxLength = CommonInput.Constraints.Text.LongTextMaxLength;
        
//        /// <summary>Minimum allowed amount in cents. No practical lower bound (supports very large negative discounts).</summary>
//        public const long AmountCentsMinValue = long.MinValue; // Adjustments can be negative for discounts
//    }
//    #endregion

//    #region Errors
//    /// <summary>Defines error scenarios for LineItemAdjustment operations.</summary>
//    public static class Errors
//    {
//        /// <summary>Triggered when amount validation fails.</summary>
//        public static Error InvalidAmountCents => Error.Validation(code: "LineItemAdjustment.InvalidAmountCents", description: $"Amount cents must be at least {Constraints.AmountCentsMinValue}.");
        
//        /// <summary>Triggered when description is missing or empty.</summary>
//        public static Error DescriptionRequired => CommonInput.Errors.Required(prefix: nameof(LineItemAdjustment), field: nameof(Description));
        
//        /// <summary>Triggered when description exceeds maximum length.</summary>
//        public static Error DescriptionTooLong => CommonInput.Errors.TooLong(prefix: nameof(LineItemAdjustment), field: nameof(Description), maxLength: Constraints.DescriptionMaxLength);
//    }
//    #endregion

//    #region Properties
//    /// <summary>Foreign key reference to the parent LineItem.</summary>
//    public Guid LineItemId { get; set; }
    
//    /// <summary>
//    /// Foreign key reference to the Promotion that created this adjustment (nullable).
//    /// Set only if this adjustment comes from a promotion; null for manual adjustments (tax, custom discount).
//    /// </summary>
//    public Guid? PromotionId { get; set; }
    
//    /// <summary>
//    /// Amount of adjustment in cents (negative for discount, positive for fee/tax).
//    /// Negative values reduce line item total; positive values increase it.
//    /// Example: -500 cents = $5.00 discount, +800 cents = $8.00 fee
//    /// </summary>
//    public long AmountCents { get; set; }
    
//    /// <summary>
//    /// Human-readable description of the adjustment.
//    /// Examples: "5% quantity discount", "Item-specific promotion", "Sales tax (8.5%)"
//    /// Used in order summaries, invoices, and customer communications.
//    /// </summary>
//    public string Description { get; set; } = string.Empty;
//    #endregion

//    #region Relationships
//    /// <summary>Reference to the parent LineItem aggregate within the Order.</summary>
//    public LineItem LineItem { get; set; } = null!;
    
//    /// <summary>Optional reference to the Promotion that created this adjustment.</summary>
//    public Promotion? Promotion { get; set; }
//    #endregion

//    #region Computed Properties
//    /// <summary>
//    /// Indicates whether this adjustment came from a promotion.
//    /// Used to identify which adjustments to clear when promotion changes.
//    /// </summary>
//    public bool IsPromotion => PromotionId.HasValue;
//    #endregion

//    #region Constructors
//    /// <summary>Private constructor; use Create() factory method for validation.</summary>
//    private LineItemAdjustment() { }
//    #endregion

//    #region Factory Methods
//    /// <summary>
//    /// Creates a new line-item level adjustment.
//    /// </summary>
//    /// <remarks>
//    /// Validates description is provided and within length limits.
//    /// Does not validate amount (no range restrictions other than type limits).
//    /// 
//    /// Typical usage:
//    /// <code>
//    /// // Create item-specific promotion adjustment
//    /// var adjustment = LineItemAdjustment.Create(
//    ///     lineItemId: lineItem.Id,
//    ///     amountCents: -500,  // $5.00 discount per item
//    ///     description: "Buy 2+ get 10% off this item",
//    ///     promotionId: promo.Id);
//    /// 
//    /// // Create tax adjustment (manually calculated at item level)
//    /// var tax = LineItemAdjustment.Create(
//    ///     lineItemId: lineItem.Id,
//    ///     amountCents: 425,  // $4.25 tax
//    ///     description: "Sales tax (8.5%)");
//    /// 
//    /// if (adjustment.IsError) return Problem(adjustment.FirstError);
//    /// </code>
//    /// </remarks>
//    public static ErrorOr<LineItemAdjustment> Create(Guid lineItemId, long amountCents, string description, Guid? promotionId = null)
//    {
//        if (string.IsNullOrWhiteSpace(value: description)) return Errors.DescriptionRequired;
//        if (description.Length > Constraints.DescriptionMaxLength) return Errors.DescriptionTooLong;

//        return new LineItemAdjustment
//        {
//            Id = Guid.NewGuid(),
//            LineItemId = lineItemId,
//            AmountCents = amountCents,
//            Description = description,
//            PromotionId = promotionId,
//            CreatedAt = DateTimeOffset.UtcNow
//        };
//    }
//    #endregion
//}