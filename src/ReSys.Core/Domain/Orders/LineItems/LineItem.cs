using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Orders.Adjustments;
using ReSys.Core.Domain.Orders.Shipments;

namespace ReSys.Core.Domain.Orders.LineItems;

/// <summary>
/// Represents a single product variant and quantity within an Order.
/// This is an owned entity of the Order aggregate; it cannot exist independently.
/// </summary>
/// <remarks>
/// KEY PATTERNS:
/// • Owned Entity: LineItem is only accessible through Order aggregate root
/// • Price Capture: Prices captured at creation time (snapshot) for historical accuracy
/// • Adjustments: Line-item level discounts/adjustments tracked separately
/// • Currency Snapshot: Inherits currency from parent Order
/// 
/// PRICING STRATEGY:
/// • PriceCents: Variant price in order currency (captured at creation)
/// • SubtotalCents: PriceCents × Quantity (merchandise subtotal)
/// • TotalCents: SubtotalCents + sum of line-item adjustments
/// • All calculations in cents (long) to preserve precision
/// 
/// ADJUSTMENT HANDLING:
/// • LineItemAdjustment collection holds order-specific adjustments
/// • Adjustments typically from promotion discounts
/// • Can be at item or line-item level depending on promotion rules
/// • Adjustments can be negative (discount) or positive (tax, fee)
/// 
/// CAPTURED PROPERTIES:
/// • CapturedName: Product descriptive name at order time (for history if product deleted)
/// • CapturedSku: Product SKU at order time (for inventory tracking)
/// • IsPromotional: Flags if this is a promotional item (free/bonus item)
/// 
/// Example calculation:
/// • Variant: "Red T-Shirt" at $25.00 USD
/// • Quantity: 2
/// • ItemTotalCents: 2 × 2500 = 5000 (cents)
/// • With 10% discount: adjustment = -500 (cents)
/// • Total: 5000 + (-500) = 4500 cents = $45.00
/// </remarks>
public class LineItem : AuditableEntity<Guid>
{
    #region Constraints
    /// <summary>
    /// Defines validation constraints for LineItem domain objects.
    /// </summary>
    /// <remarks>
    /// QUANTITY CONSTRAINTS:
    /// • QuantityMinValue: At least 1 unit must be ordered per item
    /// • Cannot have zero or negative quantities
    /// 
    /// PRICE CONSTRAINTS:
    /// • PriceCentsMinValue: Captures variant price as of order time
    /// • Price stored in cents (long) to maintain precision
    /// 
    /// STRING CONSTRAINTS:
    /// • CurrencyMaxLength: ISO 4217 code is exactly 3 characters
    /// • CapturedNameMaxLength: Descriptive name limit
    /// • CapturedSkuMaxLength: SKU maximum length
    /// </remarks>
    public static class Constraints
    {
        /// <summary>Minimum quantity allowed per line item (must be at least 1).</summary>
        public const int QuantityMinValue = 1;
        
        /// <summary>Minimum price in cents (non-negative).</summary>
        public const long PriceCentsMinValue = 0;
        
        /// <summary>Length of ISO 4217 currency code (always 3 characters).</summary>
        public const int CurrencyMaxLength = CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength;
        
        /// <summary>Maximum length of captured product name.</summary>
        public const int CapturedNameMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
        
        /// <summary>Maximum length of captured product SKU.</summary>
        public const int CapturedSkuMaxLength = CommonInput.Constraints.SlugsAndVersions.SlugMaxLength;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines all error scenarios specific to LineItem operations.
    /// </summary>
    /// <remarks>
    /// These errors guide developers on why LineItem operations failed
    /// and suggest corrective actions.
    /// </remarks>
    public static class Errors
    {
        /// <summary>Triggered when line item with specified ID is not found.</summary>
        public static Error NotFound(Guid id) => Error.NotFound(code: "LineItem.NotFound", description: $"LineItem with ID '{id}' was not found.");
        
        /// <summary>Triggered when variant reference is null.</summary>
        public static Error VariantRequired => Error.Validation(code: "LineItem.VariantRequired", description: "Variant cannot be null.");
        
        /// <summary>Triggered when quantity is less than minimum (< 1).</summary>
        public static Error InvalidQuantity => Error.Validation(code: "LineItem.InvalidQuantity", description: $"Quantity must be at least {Constraints.QuantityMinValue}.");
        
        /// <summary>Triggered when price is less than minimum (< 0).</summary>
        public static Error InvalidPriceCents => Error.Validation(code: "LineItem.InvalidPriceCents", description: $"Price cents must be at least {Constraints.PriceCentsMinValue}.");
        
        /// <summary>Triggered when currency code is missing.</summary>
        public static Error CurrencyRequired => CommonInput.Errors.Required(prefix: nameof(LineItem), field: nameof(Currency));
        
        /// <summary>Triggered when currency code exceeds maximum length.</summary>
        public static Error CurrencyTooLong => CommonInput.Errors.TooLong(prefix: nameof(LineItem), field: nameof(Currency), maxLength: Constraints.CurrencyMaxLength);
        
        /// <summary>Triggered when captured product name is missing.</summary>
        public static Error CapturedNameRequired => CommonInput.Errors.Required(prefix: nameof(LineItem), field: nameof(CapturedName));
        
        /// <summary>Triggered when captured product name exceeds maximum length.</summary>
        public static Error CapturedNameTooLong => CommonInput.Errors.TooLong(prefix: nameof(LineItem), field: nameof(CapturedName), maxLength: Constraints.CapturedNameMaxLength);
        
        /// <summary>Triggered when captured SKU exceeds maximum length.</summary>
        public static Error CapturedSkuTooLong => CommonInput.Errors.TooLong(prefix: nameof(LineItem), field: nameof(CapturedSku), maxLength: Constraints.CapturedSkuMaxLength);
        /// <summary>Triggered when a variant is not priced in the requested currency.</summary>                   
        public static Error VariantNotPriced(string variantName, Guid variantId, string currency) =>
            Error.Validation(code: "LineItem.VariantNotPriced", description: $"Variant '{variantName}' (ID: {variantId}) is  not priced for currency '{currency}'.");
    }
    #endregion

    #region Properties
    /// <summary>Foreign key reference to the parent Order.</summary>
    public Guid OrderId { get; set; }
    
    /// <summary>Foreign key reference to the Variant (product variant) ordered.</summary>
    public virtual Guid VariantId { get; set; }
    
    /// <summary>
    /// Quantity of this variant ordered.
    /// Must be ≥ 1; cannot order zero or negative quantities.
    /// </summary>
    public virtual int Quantity { get; set; }
    
    /// <summary>
    /// Unit price of variant in order currency (cents).
    /// Captured at order creation time as snapshot for historical accuracy.
    /// This preserves the price paid even if variant price changes later.
    /// </summary>
    public long PriceCents { get; set; }
    
    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EUR").
    /// Inherited from parent Order to ensure consistency.
    /// </summary>
    public string Currency { get; set; } = string.Empty;
    
    /// <summary>
    /// Descriptive name of product captured at order time.
    /// Example: "Red T-Shirt, Size M"
    /// Preserved even if product is deleted or name changes.
    /// </summary>
    public string CapturedName { get; set; } = string.Empty;
    
    /// <summary>
    /// Product SKU (stock keeping unit) captured at order time.
    /// Example: "TSHIRT-RED-M-2024"
    /// Nullable; used for inventory tracking and reorder purposes.
    /// </summary>
    public string? CapturedSku { get; set; }

    /// <summary>
    /// Flags this as a promotional/bonus item.
    /// When true, this item may be free or part of a buy-x-get-y promotion.
    /// Affects how the item is tracked in fulfillment.
    /// </summary>
    public bool IsPromotional { get; set; }
    #endregion

    #region Relationships
    public Order Order { get; set; } = null!;
    public Variant Variant { get; set; } = null!;
    public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
    public ICollection<LineItemAdjustment> Adjustments { get; set; } = new List<LineItemAdjustment>();
    #endregion

    #region Computed Properties
    /// <summary>
    /// Subtotal for this line item (quantity × unit price) in cents.
    /// This is the merchandise cost before adjustments.
    /// Formula: PriceCents × Quantity
    /// </summary>
    public long SubtotalCents => PriceCents * Quantity;
    
    /// <summary>
    /// Subtotal converted to decimal currency value (SubtotalCents ÷ 100).
    /// </summary>
    public decimal Subtotal => SubtotalCents / 100m;
    
    /// <summary>
    /// Unit price converted to decimal currency value (PriceCents ÷ 100).
    /// This is the price per single unit.
    /// </summary>
    public decimal UnitPrice => PriceCents / 100m;
    
    /// <summary>
    /// Total cost of the line item before any order-level adjustments.
    /// In the new model, this is the same as the subtotal.
    /// </summary>
    public long TotalCents => SubtotalCents;
    
    /// <summary>
    /// Total cost converted to decimal currency value (TotalCents ÷ 100).
    /// Final amount for this line item before order-level adjustments.
    /// </summary>
    public decimal Total => TotalCents / 100m;
    #endregion

    #region Constructors
    /// <summary>Private constructor; use Create() factory method for validation.</summary>
    private LineItem() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new LineItem for the specified variant and quantity.
    /// </summary>
    /// <remarks>
    /// This factory method:
    /// 1. Validates all inputs (variant, quantity, currency)
    /// 2. Captures the variant price at order time (snapshot)
    /// 3. Captures descriptive product name from variant
    /// 4. Captures SKU from variant
    /// 5. Creates lineitem with CreatedAt timestamp
    /// 
    /// Price is captured immediately to preserve the price paid
    /// even if the variant price changes later in the system.
    /// 
    /// Example:
    /// <code>
    /// var variant = await dbContext.Variants.FindAsync(variantId);
    /// var result = LineItem.Create(
    ///     orderId: order.Id,
    ///     variant: variant,
    ///     quantity: 2,
    ///     currency: "USD");
    /// 
    /// if (result.IsError) return BadRequest(result.FirstError);
    /// var lineItem = result.Value;
    /// order.LineItems.Add(lineItem);
    /// </code>
    /// </remarks>
    public static ErrorOr<LineItem> Create(Guid orderId, Variant? variant, int quantity, string currency)
    {
        if (variant == null) return Errors.VariantRequired;
        if (quantity < Constraints.QuantityMinValue) return Errors.InvalidQuantity;
        if (string.IsNullOrWhiteSpace(value: currency)) return Errors.CurrencyRequired;
        if (currency.Length > Constraints.CurrencyMaxLength) return Errors.CurrencyTooLong;

        var priceInCurrency = variant.PriceIn(currency: currency);
        if (priceInCurrency is null)
        {
            return Errors.VariantNotPriced(variantName: variant.DescriptiveName, variantId: variant.Id, currency: currency);
        }
        var priceCents = (long)(priceInCurrency.Value * 100);

        if (priceCents < Constraints.PriceCentsMinValue) return Errors.InvalidPriceCents;

        var capturedName = variant.DescriptiveName;
        if (string.IsNullOrWhiteSpace(value: capturedName)) return Errors.CapturedNameRequired;
        if (capturedName.Length > Constraints.CapturedNameMaxLength) return Errors.CapturedNameTooLong;

        var capturedSku = variant.Sku;
        if (capturedSku is { Length: > Constraints.CapturedSkuMaxLength }) return Errors.CapturedSkuTooLong;

        var lineItem = new LineItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            VariantId = variant.Id,
            Quantity = quantity,
            PriceCents = priceCents,
            CapturedName = capturedName,
            CapturedSku = capturedSku,
            CreatedAt = DateTimeOffset.UtcNow,
            Currency = currency
        };

        return lineItem;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the quantity of this line item.
    /// </summary>
    /// <remarks>
    /// Quantity must remain ≥ 1.
    /// Sets UpdatedAt timestamp to track modification time.
    /// Use Order.RemoveLineItem() instead to remove items completely.
    /// </remarks>
    public ErrorOr<LineItem> UpdateQuantity(int quantity)
    {
        if (quantity < Constraints.QuantityMinValue) return Errors.InvalidQuantity;
        Quantity = quantity;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks this line item for deletion (soft delete support).
    /// </summary>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}