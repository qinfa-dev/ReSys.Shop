using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Catalog.Products.Prices;

/// <summary>
/// Represents pricing information for a product variant in a specific currency.
/// Each variant can have multiple prices for different currencies (multi-currency support).
/// Prices include list price (Amount) and optional compare-at price (CompareAtAmount) for sale indicators.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Multi-Currency Pricing:</strong>
/// A single variant typically has one Price per currency:
/// <code>
/// Product: T-Shirt
/// └─ Variant: Red-Medium
///    ├─ Price (USD): $19.99
///    ├─ Price (EUR): €18.99
///    └─ Price (GBP): £17.99
/// </code>
/// </para>
///
/// <para>
/// <strong>Sale Pricing:</strong>
/// Prices support compare-at prices for sale displays:
/// <code>
/// Amount: $14.99 (current sale price)
/// CompareAtAmount: $24.99 (original price)
/// OnSale: true
/// DiscountPercent: 40%
/// </code>
/// </para>
///
/// <para>
/// <strong>Price Capture (Critical Pattern):</strong>
/// When an order is created, the current variant price is captured into the LineItem.
/// This snapshot persists even if variant price changes later, ensuring order total consistency:
/// <code>
/// // Day 1: Product has Price=$100
/// var order = new Order();
/// order.AddLineItem(variant: redShirt); // Captures price=$100
///
/// // Day 2: Product price changes to $80
/// redShirt.UpdatePrice(80);
///
/// // But the order still shows $100 for that line item
/// // Price in LineItem ≠ Price in Variant
/// </code>
/// </para>
///
/// <para>
/// <strong>Valid Currencies:</strong>
/// Supported: USD (default), EUR, GBP, VND
/// </para>
/// </remarks>
public sealed class Price : Aggregate
{
    #region Constraints
    /// <summary>
    /// Pricing constraints and valid currency definitions for price operations.
    /// Defines acceptable values and limits for multi-currency pricing.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Default currency for new prices: USD (United States Dollar).
        /// Used when currency not explicitly specified during price creation.
        /// Ensures consistency in multi-currency systems.
        /// </summary>
        public const string DefaultCurrency = "USD";
        
        /// <summary>
        /// Supported currencies for variant pricing across the platform.
        /// Current: USD (US Dollar), EUR (Euro), GBP (British Pound), VND (Vietnamese Dong).
        /// All prices MUST use one of these currencies for consistency in reporting and calculations.
        /// To add new currency: add to this array, update exchange rate provider, test extensively.
        /// </summary>
        public static readonly string[] ValidCurrencies = ["USD", "EUR", "GBP", "VND"];
        
        /// <summary>
        /// Maximum length for ISO 4217 currency codes (standard is 3 characters).
        /// Example valid values: "USD" (3), "EUR" (3), "GBP" (3)
        /// Used for validation to catch input errors early.
        /// </summary>
        public const int CurrencyCodeMaxLength = 3;
        
        /// <summary>
        /// Minimum decimal places for price precision (2 decimal places for cents).
        /// Ensures consistent rounding: $19.99 (2 decimals), €18.50 (2 decimals)
        /// Database constraint: decimal(18, 2) - supports up to 9.99 quadrillion with 2 decimal places.
        /// </summary>
        public const int PriceDecimalPlaces = 2;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Domain error definitions for price operations and validation.
    /// Returned via ErrorOr pattern for railway-oriented error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Occurs when attempting to create price without associated variant.
        /// Prevention: Every price must belong to a variant. Orphaned prices are invalid.
        /// Resolution: Ensure variant is created first, then add prices to it.
        /// </summary>
        public static Error VariantRequired => Error.Validation(
            code: "Price.VariantRequired",
            description: "Variant is required. Price must be associated with a variant.");
        
        /// <summary>
        /// Occurs when referenced price ID cannot be found in database.
        /// Typical causes: ID doesn't exist, price was deleted, querying wrong variant.
        /// Resolution: Verify price ID, check variant's prices collection, ensure not soft-deleted.
        /// </summary>
        public static Error NotFound(Guid id) => Error.NotFound(
            code: "Price.NotFound",
            description: $"Price with ID '{id}' was not found.");
        
        /// <summary>
        /// Occurs when currency code is not specified (null or empty string).
        /// Prevention: Currency is mandatory for price calculation, display, and reporting.
        /// Resolution: Always provide valid ISO 4217 currency code (3 characters).
        /// </summary>
        public static Error CurrencyRequired => CommonInput.Errors.Required(
            prefix: nameof(Price),
            field: nameof(Currency));
        
        /// <summary>
        /// Occurs when currency code exceeds maximum length (3 characters).
        /// Prevention: ISO 4217 currency codes must be exactly 3 characters (e.g., "USD", "EUR").
        /// Common mistake: passing "US Dollar" instead of "USD"
        /// </summary>
        public static Error CurrencyTooLong => CommonInput.Errors.TooLong(
            prefix: nameof(Price),
            field: nameof(Currency),
            maxLength: CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength);
        
        /// <summary>
        /// Occurs when currency is not in the ValidCurrencies approved list.
        /// Prevention: Only supported currencies enable proper exchange rate handling and reporting.
        /// Resolution: Use one of the valid currencies. To add new currency, update ValidCurrencies array.
        /// Valid options: USD, EUR, GBP, VND
        /// </summary>
        public static Error InvalidCurrency => Error.Validation(
            code: "Price.InvalidCurrency",
            description: $"Currency must be one of: {string.Join(separator: ", ", value: Constraints.ValidCurrencies)}.");
        
        /// <summary>
        /// Occurs when price amount is invalid (negative, NaN, etc.).
        /// Prevention: Prices must be non-negative for valid commerce operations.
        /// Resolution: Use Amount >= 0. Nullable amounts allowed (unset price).
        /// </summary>
        public static Error InvalidAmount => Error.Validation(
            code: "Price.InvalidAmount",
            description: "Price amount must be non-negative.");
        
        /// <summary>
        /// Occurs when CompareAtAmount is less than or equal to Amount (sale price >= list price).
        /// Prevention: Sale pricing requires list price > sale price to show meaningful discount.
        /// Resolution: Ensure CompareAtAmount > Amount when offering sale prices.
        /// </summary>
        public static Error InvalidSalePrice => Error.Validation(
            code: "Price.InvalidSalePrice",
            description: "Compare-at price must be greater than sale price for valid discount display.");
    }
    #endregion

    #region Core Properties
    /// <summary>
    /// Unique identifier for the variant this price applies to.
    /// Foreign key reference to Variant aggregate root.
    /// Every price belongs to exactly one variant; variant cannot be null.
    /// </summary>
    public Guid VariantId { get; set; }
    
    /// <summary>
    /// Current selling price in the specified currency.
    /// Nullable decimal to allow prices to be unset initially (admin editing before publication).
    /// Stored with 2 decimal places precision for currency calculations (cents/smaller units).
    /// Example values: 19.99, 0.50, 1000.00
    /// Validation: Must be non-negative if set (see InvalidAmount error).
    /// </summary>
    public decimal? Amount { get; set; }
    
    /// <summary>
    /// ISO 4217 currency code for this price (exactly 3 uppercase characters).
    /// Examples: "USD" (US Dollar), "EUR" (Euro), "GBP" (British Pound), "VND" (Vietnamese Dong)
    /// Defaults to USD if not specified during creation.
    /// CRITICAL: Must be one of ValidCurrencies for platform consistency.
    /// Multi-currency example: Same variant might have Price(USD, 19.99), Price(EUR, 18.99), Price(GBP, 17.99)
    /// </summary>
    public string Currency { get; set; } = Constraints.DefaultCurrency;
    
    /// <summary>
    /// Optional original/list price for compare-at sale display (strikethrough price).
    /// Used to show savings when the variant is on sale.
    /// Only meaningful when CompareAtAmount > Amount (discount exists).
    /// Typically higher than Amount to justify the "Sale" badge on storefront.
    /// Set to null when variant is not on sale or using "hidden" sale mode.
    /// 
    /// Example:
    /// Amount: 14.99 (sale price - what customer pays)
    /// CompareAtAmount: 24.99 (list price - original MSRP)
    /// Shows as: ~~$24.99~~ NOW $14.99 (40% off!)
    /// </summary>
    public decimal? CompareAtAmount { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Navigation property: the variant aggregate this price belongs to.
    /// Provides access to variant details, inventory, options, and other prices.
    /// Lazy-loaded on access via EF Core navigation property.
    /// Must never be null after loading (enforced by EF configuration).
    /// </summary>
    public Variant Variant { get; set; } = null!;
    #endregion

    #region Computed Properties
    /// <summary>
    /// True if this price is on sale (special price promotion active).
    /// Computed as: CompareAtAmount.HasValue AND Amount.HasValue AND CompareAtAmount > Amount
    /// Used by storefront to display "Sale!" badges, strikethrough pricing, discount percentages.
    /// Example: CompareAtAmount=100, Amount=70 → OnSale=true (30% off)
    /// Example: CompareAtAmount=null, Amount=50 → OnSale=false (regular price)
    /// </summary>
    public bool OnSale => CompareAtAmount.HasValue && Amount.HasValue && CompareAtAmount > Amount;
    
    /// <summary>
    /// Alias for OnSale property for semantic clarity in different contexts.
    /// Indicates the price is discounted from the compare-at (list) value.
    /// Used interchangeably with OnSale for code readability:
    /// if (price.Discounted) { ShowSaleIndicator(); }
    /// </summary>
    public bool Discounted => OnSale;
    
    /// <summary>
    /// Absolute discount amount in currency units (CompareAtAmount - Amount).
    /// Returns null if not on sale (OnSale = false).
    /// Used to display "Save $X" messaging on storefront.
    /// Example: List=$100, Sale=$80 → DiscountAmount=$20
    /// Example: List=$24.99, Sale=$19.99 → DiscountAmount=$5.00
    /// </summary>
    public decimal? DiscountAmount => OnSale ? CompareAtAmount - Amount : null;
    
    /// <summary>
    /// Discount percentage as a decimal from 0-100%.
    /// Returns null if not on sale (OnSale = false).
    /// Calculated as: ((CompareAtAmount - Amount) / CompareAtAmount) * 100
    /// Used for "Save X%" displays on storefront and marketing.
    /// Example: List=$100, Sale=$80 → DiscountPercent=20 ("Save 20%")
    /// Example: List=$19.99, Sale=$14.99 → DiscountPercent=24.96 ("Save ~25%")
    /// Rounding: Often displayed as integer (20%, 25%) but calculated with decimals for accuracy.
    /// </summary>
    public decimal? DiscountPercent => OnSale && CompareAtAmount > 0 ? ((CompareAtAmount - Amount) / CompareAtAmount * 100) : null;
    #endregion

    #region Constructors
    private Price() { }
    #endregion

    #region Factory
    /// <summary>
    /// Factory method for creating a new Price for a variant.
    /// Validates currency and initializes pricing information.
    /// Raises Created domain event for downstream processing.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Currency Validation:</strong>
    /// Currency is validated against ValidCurrencies during creation.
    /// All prices in system must use supported currencies for consistency.
    /// </para>
    ///
    /// <para>
    /// <strong>Typical Usage:</strong>
    /// <code>
    /// // Create price in USD
    /// var priceResult = Price.Create(
    ///     variantId: variant.Id,
    ///     amount: 19.99m,
    ///     currency: "USD");
    /// 
    /// if (priceResult.IsError)
    ///     return Problem(priceResult.FirstError.Description);
    /// 
    /// var price = priceResult.Value;
    /// variant.AddPrice(price);
    /// 
    /// // Create sale price with compare-at
    /// var salePriceResult = Price.Create(
    ///     variantId: variant.Id,
    ///     amount: 14.99m,
    ///     currency: "USD",
    ///     compareAtAmount: 24.99m);  // Shows "Save 40%"
    /// </code>
    /// </para>
    ///
    /// <para>
    /// <strong>Parameters:</strong>
    /// - variantId: Required. The variant this price belongs to.
    /// - amount: Optional. Sale/current price. Can be set later via Update().
    /// - currency: Defaults to USD. Must be one of ValidCurrencies.
    /// - compareAtAmount: Optional. Original list price for sale display.
    /// </para>
    /// </remarks>
    public static ErrorOr<Price> Create(Guid variantId, decimal? amount = null, string currency = Constraints.DefaultCurrency, decimal? compareAtAmount = null)
    {
        if (string.IsNullOrWhiteSpace(value: currency)) return Errors.CurrencyRequired;
        if (currency.Length >CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength) return Errors.CurrencyTooLong;
        if (!Constraints.ValidCurrencies.Contains(value: currency)) return Errors.InvalidCurrency;

        var price = new Price
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            Amount = amount,
            Currency = currency,
            CompareAtAmount = compareAtAmount
        };

        price.AddDomainEvent(domainEvent: new Events.Created(PriceId: price.Id, VariantId: variantId, Currency: currency));
        return price;
    }
    #endregion

    #region Business Logic - Update & Delete
    /// <summary>
    /// Updates the amounts for this price instance.
    /// Both <paramref name="amount"/> and <paramref name="compareAtAmount"/> are optional,
    /// allowing for partial updates.
    /// </summary>
    /// <param name="amount">The new current selling price. If null, the existing amount is retained.</param>
    /// <param name="compareAtAmount">The new optional original/list price for sale display. If null, the existing compare-at amount is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Price}"/> result.
    /// Returns the updated <see cref="Price"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method updates the <c>Amount</c> and <c>CompareAtAmount</c> properties.
    /// If any changes occur, the <c>UpdatedAt</c> timestamp is updated, and an <see cref="Events.Updated"/>
    /// domain event is raised.
    /// <para>
    /// Validation for <paramref name="amount"/> and <paramref name="compareAtAmount"/> (e.g., non-negativity, compare-at > amount)
    /// is implicitly handled by the <see cref="Price"/>'s computed properties or by validation logic in the application layer
    /// or during object construction.
    /// </para>
    /// <strong>Update Strategy:</strong>
    /// Only amount-related fields can be updated. Pass null to leave unchanged.
    /// <strong>Usage Example:</strong>
    /// <code>
    /// // Update sale price mid-campaign
    /// var price = GetPriceById(priceId);
    /// var updateResult = price.Update(
    ///     amount: 12.99m,  // Lower sale price
    ///     compareAtAmount: 24.99m);
    /// 
    /// // Change from regular to sale pricing
    /// var saleResult = price.Update(
    ///     amount: 9.99m,
    ///     compareAtAmount: 19.99m);  // Now OnSale = true
    /// 
    /// // Remove sale pricing (back to regular)
    /// var regularResult = price.Update(
    ///     amount: 19.99m,
    ///     compareAtAmount: null);  // Now OnSale = false
    /// </code>
    /// </remarks>
    public ErrorOr<Price> Update(decimal? amount = null, decimal? compareAtAmount = null)
    {
        bool changed = false;
        if (amount.HasValue && amount != Amount)
        {
            Amount = amount;
            changed = true;
        }
        if (compareAtAmount.HasValue && compareAtAmount != CompareAtAmount)
        {
            CompareAtAmount = compareAtAmount;
            changed = true;
        }
        if (changed)
        {
            AddDomainEvent(domainEvent: new Events.Updated(PriceId: Id, VariantId: VariantId, Amount: Amount, CompareAtAmount: CompareAtAmount));
        }
        return this;
    }

    /// <summary>
    /// Delete this price from the variant.
    /// Raises Deleted domain event for cascade operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Deletion Impact:</strong>
    /// Removes pricing for this currency from the variant.
    /// If this is the only price, variant becomes "unpriced" until new price added.
    /// </para>
    ///
    /// <para>
    /// <strong>Typical Usage:</strong>
    /// <code>
    /// // Remove pricing for specific currency
    /// var deleteResult = price.Delete();
    /// if (deleteResult.IsError)
    ///     return Problem(deleteResult.FirstError.Description);
    /// 
    /// // Price is now deleted
    /// variant.RemovePrice(price.Id);
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(domainEvent: new Events.Deleted(PriceId: Id, VariantId: VariantId));
        return Result.Deleted;
    }
    #endregion

    #region Events
    /// <summary>
    /// Domain events for price lifecycle tracking and cross-domain communication.
    /// Events enable async processing (inventory updates, search index, analytics, etc.).
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Raised when a new price is created for a variant.
        /// Handlers: Update product search index, notify external pricing systems, etc.
        /// </summary>
        public sealed record Created(Guid PriceId, Guid VariantId, string Currency) : DomainEvent;
        
        /// <summary>
        /// Raised when price amounts change (sale price updates, promotions, etc.).
        /// Handlers: Update product search index, notify customers of price changes, analytics tracking.
        /// </summary>
        public sealed record Updated(Guid PriceId, Guid VariantId, decimal? Amount, decimal? CompareAtAmount) : DomainEvent;
        
        /// <summary>
        /// Raised when a price is deleted from a variant (currency no longer offered).
        /// Handlers: Update product search index, remove from currency pricing list, analytics.
        /// </summary>
        public sealed record Deleted(Guid PriceId, Guid VariantId) : DomainEvent;
    }
    #endregion
}
