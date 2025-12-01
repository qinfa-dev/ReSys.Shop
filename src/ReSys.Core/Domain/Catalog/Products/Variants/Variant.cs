using System.ComponentModel.DataAnnotations;

using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Domain.Catalog.Products.Prices;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.LineItems;

namespace ReSys.Core.Domain.Catalog.Products.Variants;

/// <summary>
/// Represents a specific product variant that can be sold independently with its own pricing, inventory, and option values.
/// Each product must have a master variant; additional variants represent different configurations (colors, sizes, models).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// Variants are the actual sellable units in the system:
/// <list type="bullet">
/// <item>
/// <term>Master Variant</term>
/// <description>One per product; cannot have option values; represents default configuration</description>
/// </item>
/// <item>
/// <term>Non-Master Variants</term>
/// <description>0+ per product; have option values (color, size); represent specific configurations</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Pricing Strategy:</strong>
/// <list type="bullet">
/// <item>
/// <term>Multi-Currency</term>
/// <description>Each variant can have prices in different currencies (USD, EUR, GBP, etc.)</description>
/// </item>
/// <item>
/// <term>Price Capture</term>
/// <description>Prices captured at order time (frozen in LineItem), not retrieved dynamically</description>
/// </item>
/// <item>
/// <term>Cost Tracking</term>
/// <description>CostPrice and CostCurrency tracked for margin calculations</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Inventory Management:</strong>
/// <list type="bullet">
/// <item>
/// <term>Multi-Location Tracking</term>
/// <description>Inventory tracked across multiple warehouse/store locations via StockItem</description>
/// </item>
/// <item>
/// <term>Physical vs Digital</term>
/// <description>TrackInventory flag: true for physical (stock limited), false for digital (infinite)</description>
/// </item>
/// <item>
/// <term>Backorderable</term>
/// <description>When true, customers can pre-order out-of-stock items</description>
/// </item>
/// <item>
/// <term>TotalOnHand</term>
/// <description>Computed: Sum of all StockItem quantities across all locations</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Option Values &amp; Combinations:</strong>
/// <list type="bullet">
/// <item>
/// <term>Master Variant</term>
/// <description>CANNOT have option values (represents default/fallback configuration)</description>
/// </item>
/// <item>
/// <term>Non-Master Variants</term>
/// <description>CAN have option values (e.g., Blue + Large = specific SKU)</description>
/// </item>
/// <item>
/// <term>Option Combinations</term>
/// <description>Each variant can combine multiple option values from different option types</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Physical Specifications:</strong>
/// Variants track physical properties for shipping and fulfillment:
/// <list type="bullet">
/// <item>SKU (Stock Keeping Unit) - Unique identifier for ordering/inventory</item>
/// <item>Barcode - Point-of-sale scanning</item>
/// <item>Dimensions (Height × Width × Depth + Unit) - Packaging/shipping calculations</item>
/// <item>Weight + Unit - Shipping cost calculations</item>
/// <item>Dimension/Weight Units - mm, cm, in, ft for dimensions; g, kg, lb, oz for weight</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Invariants:</strong>
/// <list type="bullet">
/// <item>Master variant (IsMaster = true) CANNOT have option values</item>
/// <item>Master variant CANNOT be deleted (must exist for every product)</item>
/// <item>SKU must be unique within product (if provided)</item>
/// <item>If TrackInventory = true, stock items must be managed for each location</item>
/// <item>If TrackInventory = false (digital), no inventory tracking needed</item>
/// <item>Discontinued variants cannot be purchased (DiscontinueOn + time limit)</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Domain Events Raised:</strong>
/// <list type="bullet">
/// <item><strong>PriceAdded</strong> - When price added for currency</item>
/// <item><strong>StockItemAdded</strong> - When inventory added at location</item>
/// <item><strong>OptionValueAdded</strong> - When option value linked (non-master only)</item>
/// <item><strong>Discontinued</strong> - When variant marked for discontinuation</item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Typical Usage:</strong>
/// <code>
/// // Master variant created automatically with product
/// var product = Product.Create(name: "T-Shirt", slug: "t-shirt");
/// var master = product.GetMaster().Value;
/// 
/// // Add prices for master
/// master.AddPrice("USD", 2999);
/// master.AddPrice("EUR", 2899);
/// 
/// // Add inventory at warehouse
/// master.AddOrUpdateStockItem(warehouse.Id, quantity: 500);
/// 
/// // Create non-master variant (color/size)
/// var blueSmall = product.AddVariant(sku: "TS-BLU-SM").Value;
/// blueSmall.AddOptionValue(blueOption);
/// blueSmall.AddOptionValue(smallOption);
/// 
/// // Now blueSmall is a distinct sellable unit with:
/// // - Own SKU, prices, and inventory
/// // - Combination of Blue color + Small size
/// // - Can be ordered independently
/// </code>
/// </para>
/// </remarks>
public sealed class Variant :
    Aggregate<Guid>,
    IHasPosition,
    ISoftDeletable,
    IHasMetadata
{
    #region Constraints
    /// <summary>
    /// Defines validation boundaries and valid values for variant properties.
    /// </summary>
    /// <remarks>
    /// These constants ensure data consistency across all variants.
    /// Dimension and weight units support multiple measurement systems for international support.
    /// </remarks>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for SKU (Stock Keeping Unit) identifier.
        /// SKU must be concise but unique, typically 10-50 characters.
        /// </summary>
        public const int SkuMaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        
        /// <summary>
        /// Maximum length for barcode (e.g., UPC, EAN).
        /// Barcodes are relatively short identifiers (12-14 digits typically).
        /// </summary>
        public const int BarcodeMaxLength = CommonInput.Constraints.Text.TinyTextMaxLength;
        
        /// <summary>
        /// Default unit for measuring weight when not specified.
        /// Variants default to grams for consistency.
        /// </summary>
        public const string DefaultWeightUnit = "g";
        
        /// <summary>
        /// Default unit for measuring dimensions when not specified.
        /// Variants default to millimeters for precision.
        /// </summary>
        public const string DefaultDimensionUnit = "mm";
        
        /// <summary>
        /// Valid dimension units: millimeters, centimeters, inches, feet.
        /// Enables flexible input based on regional preferences.
        /// </summary>
        public static readonly string[] ValidDimensionUnits = ["mm", "cm", "in", "ft"];
        
        /// <summary>
        /// Valid weight units: grams, kilograms, pounds, ounces.
        /// Enables flexible input based on regional preferences.
        /// </summary>
        public static readonly string[] ValidWeightUnits = ["g", "kg", "lb", "oz"];
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines error scenarios specific to Variant operations.
    /// </summary>
    /// <remarks>
    /// These errors represent validation failures, state conflicts, and invariant violations.
    /// All follow ErrorOr pattern for explicit, type-safe error handling.
    /// </remarks>
    public static class Errors
    {
        /// <summary>
        /// Triggered when variant cannot be found by ID.
        /// </summary>
        public static Error NotFound(Guid id) => 
            CommonInput.Errors.NotFound(prefix: nameof(Variant), field: id.ToString());
        
        /// <summary>
        /// Triggered when attempting to delete a variant that has completed orders.
        /// Variants with order history must be soft-deleted to preserve order data.
        /// </summary>
        public static Error CannotDeleteWithCompleteOrders => 
            CommonInput.Errors.Conflict(prefix: nameof(Variant), field: "orders");
        
        /// <summary>
        /// Triggered when attempting to add option values to the master variant.
        /// Master variant must remain as default configuration without option values.
        /// </summary>
        public static Error MasterCannotHaveOptionValues => 
            CommonInput.Errors.InvalidOperation(prefix: nameof(Variant), field: "Master variant cannot have option values.");
        
        /// <summary>
        /// Triggered when attempting to delete the master variant.
        /// Every product must have exactly one master variant for fallback configuration.
        /// </summary>
        public static Error MasterCannotBeDeleted => 
            CommonInput.Errors.InvalidOperation(prefix: nameof(Variant), field: "Master variant cannot be deleted.");
        
        /// <summary>
        /// Triggered when price is invalid or negative.
        /// Prices must be non-negative (zero allowed for free items, though unusual).
        /// </summary>
        public static Error InvalidPrice => 
            CommonInput.Errors.TooFewItems(prefix: nameof(Variant), field: "Price", min: 0);
        
        /// <summary>
        /// Triggered when product reference is missing.
        /// Every variant must belong to exactly one product.
        /// </summary>
        public static Error ProductRequired => 
            CommonInput.Errors.Required(prefix: nameof(Variant), field: "Product");
        
        /// <summary>
        /// Triggered when dimension unit is not in valid list (mm, cm, in, ft).
        /// </summary>
        public static Error InvalidDimensionUnit => 
            CommonInput.Errors.InvalidValue(prefix: nameof(Variant), field: "Dimension unit");
        
        /// <summary>
        /// Triggered when weight unit is not in valid list (g, kg, lb, oz).
        /// </summary>
        public static Error InvalidWeightUnit => 
            CommonInput.Errors.InvalidValue(prefix: nameof(Variant), field: "Weight unit");
        
        /// <summary>
        /// Triggered when StockItem reference is null or invalid.
        /// StockItems required for inventory tracking.
        /// </summary>
        public static Error InvalidStockItem => 
            CommonInput.Errors.Null(prefix: nameof(Variant), field: "StockItem");
        
        /// <summary>
        /// Triggered when StockItem belongs to different variant than expected.
        /// Stock items can only be managed by their owning variant.
        /// </summary>
        public static Error MismatchedStockItem => 
            CommonInput.Errors.InvalidValue(prefix: nameof(Variant), field: "Mismatched StockItem");
        
        /// <summary>
        /// Triggered when attempting to add inventory for same location twice.
        /// Each location can only have one StockItem per variant.
        /// </summary>
        public static Error DuplicateStockLocation => 
            CommonInput.Errors.Conflict(prefix: nameof(Variant), field: "Stock location");
        
        /// <summary>
        /// Triggered when attempting to add an option value that doesn't exist or is invalid.
        /// </summary>
        public static Error InvalidOptionValue => 
            CommonInput.Errors.InvalidValue(prefix: nameof(Variant), field: "Option value");
        
        /// <summary>
        /// Triggered when image/asset reference is null.
        /// Images must have valid asset references.
        /// </summary>
        public static Error InvalidAsset => 
            CommonInput.Errors.Null(prefix: nameof(Variant), field: "Asset");
        
        /// <summary>
        /// Triggered when attempting to add image with same type twice (e.g., two "primary" images).
        /// Each image type can only appear once per variant.
        /// </summary>
        public static Error DuplicateAssetType => 
            CommonInput.Errors.Conflict(prefix: nameof(Variant), field: "Asset type");
    }
    #endregion

    #region Properties
    public Guid ProductId { get; set; }
    public bool IsMaster { get; set; }
    public string? Sku { get; set; }
    public string? Barcode { get; set; }
    public decimal? Weight { get; set; }
    public decimal? Height { get; set; }
    public decimal? Width { get; set; }
    public decimal? Depth { get; set; }
    public string? DimensionsUnit { get; set; }
    public string? WeightUnit { get; set; }
    public bool TrackInventory { get; set; } = true;
    public decimal? CostPrice { get; set; }
    public string? CostCurrency { get; set; }
    public int Position { get; set; }
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
    public DateTimeOffset? DiscontinueOn { get; set; }
    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }

    [Timestamp]
    public byte[]? RowVersion { get; set; }
    #endregion

    #region Relationships
    public Product Product { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Price> Prices { get; set; } = new List<Price>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<VariantOptionValue> OptionValueVariants { get; set; } = new List<VariantOptionValue>();
    public ICollection<OptionValue> OptionValues => OptionValueVariants.Select(selector: ovv => ovv.OptionValue).ToList();
    public ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
    public ICollection<Order> Orders => LineItems.Select(selector: li => li.Order).ToList();

    #endregion

    #region Computed Properties
    public bool Deleted => DeletedAt.HasValue;
    public bool Discontinued => DiscontinueOn.HasValue && DiscontinueOn <= DateTimeOffset.UtcNow;
    public bool Available => !Discontinued && Product.Available;
    public bool Purchasable => (InStock || Backorderable) && HasPrice;
    public bool InStock => !ShouldTrackInventory || StockItems.Any(predicate: si => si.QuantityOnHand > 0);
    public bool Backorderable => StockItems.Any(predicate: si => si.Backorderable);
    public bool Backordered => !InStock && Backorderable;
    public bool CanSupply => InStock || Backorderable;
    public bool ShouldTrackInventory => TrackInventory;
    public bool HasPrice => Prices.Any(predicate: p => p.Amount.HasValue);
    public double TotalOnHand => ShouldTrackInventory ? StockItems.Sum(selector: si => si.QuantityOnHand) : double.PositiveInfinity;
    public string OptionsText => OptionValues
        .OrderBy(keySelector: ov => Product.ProductOptionTypes.FirstOrDefault(predicate: pot => pot.OptionTypeId == ov.OptionTypeId)?.Position ?? 0)
        .Select(selector: ov => ov.Presentation)
        .JoinToSentence();
    public string DescriptiveName => IsMaster ? $"{Product.Name} - Master" : $"{Product.Name} - {OptionsText}";
    public ProductImage? DefaultImage => Images.OrderBy(keySelector: a => a.Position).FirstOrDefault() ?? Product.DefaultImage;
    public ProductImage? SecondaryImage => Images.OrderBy(keySelector: a => a.Position).Skip(count: 1).FirstOrDefault() ?? Product.SecondaryImage;
    public decimal Volume => (Width ?? 0) * (Height ?? 0) * (Depth ?? 0);
    public decimal Dimension => (Width ?? 0) + (Height ?? 0) + (Depth ?? 0);

    public decimal? PriceIn(string? currency = null)
    {
        currency ??= Price.Constraints.DefaultCurrency;
        return Prices.FirstOrDefault(predicate: p => p.Currency == currency)?.Amount;
    }

    public decimal? CompareAtPriceIn(string? currency = null) => Prices
        .FirstOrDefault(predicate: p =>
            currency == Price.Constraints.DefaultCurrency ||
            (!string.IsNullOrEmpty(value: currency) && p.Currency == currency))
        ?.CompareAtAmount;

    public bool OnSaleIn(string? currency = null) => Prices
        .Any(predicate: p => (currency == Price.Constraints.DefaultCurrency ||
                              (!string.IsNullOrEmpty(value: currency) && p.Currency == currency)) && p.Discounted);
    #endregion

    #region Constructors
    private Variant() { }
    #endregion

    #region Factory Methods
    public static ErrorOr<Variant> Create(
        Guid productId,
        bool isMaster = false,
        string? sku = null,
        string? barcode = null,
        decimal? weight = null,
        decimal? height = null,
        decimal? width = null,
        decimal? depth = null,
        string? dimensionsUnit = null,
        string? weightUnit = null,
        decimal? costPrice = null,
        string? costCurrency = null,
        bool trackInventory = true,
        int position = 0,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        if (productId == Guid.Empty)
            return Errors.ProductRequired;

        dimensionsUnit = dimensionsUnit?.Trim() ?? Constraints.DefaultDimensionUnit;
        weightUnit = weightUnit?.Trim() ?? Constraints.DefaultWeightUnit;
        costCurrency = costCurrency?.Trim() ?? Price.Constraints.DefaultCurrency;

        if (!string.IsNullOrWhiteSpace(value: dimensionsUnit) && !Constraints.ValidDimensionUnits.Contains(value: dimensionsUnit))
            return Errors.InvalidDimensionUnit;

        if (!string.IsNullOrWhiteSpace(value: weightUnit) && !Constraints.ValidWeightUnits.Contains(value: weightUnit))
            return Errors.InvalidWeightUnit;

        if (!string.IsNullOrWhiteSpace(value: costCurrency) && !Price.Constraints.ValidCurrencies.Contains(value: costCurrency))
            return Price.Errors.InvalidCurrency;

        var variant = new Variant
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            IsMaster = isMaster,
            Sku = sku?.Trim(),
            Barcode = barcode?.Trim(),
            Weight = weight,
            Height = height,
            Width = width,
            Depth = depth,
            DimensionsUnit = dimensionsUnit,
            WeightUnit = weightUnit,
            TrackInventory = trackInventory,
            CostPrice = costPrice,
            CostCurrency = costCurrency,
            Position = position,
            PublicMetadata = publicMetadata != null ? new Dictionary<string, object?>(dictionary: publicMetadata) : new Dictionary<string, object?>(),
            PrivateMetadata = privateMetadata != null ? new Dictionary<string, object?>(dictionary: privateMetadata) : new Dictionary<string, object?>(),
        };

        if (!isMaster)
        {
            variant.AddDomainEvent(domainEvent: new Product.Events.VariantAdded(VariantId: variant.Id, ProductId: productId));
            if (variant.ShouldTrackInventory)
                variant.AddDomainEvent(domainEvent: new Events.SetMasterOutOfStock(VariantId: variant.Id, ProductId: productId));
        }

        variant.AddDomainEvent(domainEvent: new Events.Created(VariantId: variant.Id, ProductId: productId));
        return variant;
    }
    #endregion

    #region Business Logic
    public ErrorOr<Variant> Update(
        string? sku = null,
        string? barcode = null,
        decimal? weight = null,
        decimal? height = null,
        decimal? width = null,
        decimal? depth = null,
        string? dimensionsUnit = null,
        string? weightUnit = null,
        bool? trackInventory = null,
        decimal? costPrice = null,
        string? costCurrency = null,
        int? position = null,
        DateTimeOffset? discontinueOn = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        if (costPrice.HasValue && costPrice < 0)
            return Errors.InvalidPrice;

        dimensionsUnit = dimensionsUnit?.Trim() ?? DimensionsUnit;
        weightUnit = weightUnit?.Trim() ?? WeightUnit;
        costCurrency = costCurrency?.Trim() ?? Price.Constraints.DefaultCurrency;

        if (!string.IsNullOrWhiteSpace(value: dimensionsUnit) && !Constraints.ValidDimensionUnits.Contains(value: dimensionsUnit))
            return Errors.InvalidDimensionUnit;

        if (!string.IsNullOrWhiteSpace(value: weightUnit) && !Constraints.ValidWeightUnits.Contains(value: weightUnit))
            return Errors.InvalidWeightUnit;

        if (!string.IsNullOrWhiteSpace(value: costCurrency) && !Price.Constraints.ValidCurrencies.Contains(value: costCurrency))
            return Price.Errors.InvalidCurrency;

        bool changed = false;

        if (sku != null && sku != Sku)
        {
            Sku = sku.Trim();
            changed = true;
        }

        if (barcode != null && barcode != Barcode)
        {
            Barcode = barcode.Trim();
            changed = true;
        }

        if (weight.HasValue && weight != Weight)
        {
            Weight = weight;
            changed = true;
        }

        if (height.HasValue && height != Height)
        {
            Height = height;
            changed = true;
        }

        if (width.HasValue && width != Width)
        {
            Width = width;
            changed = true;
        }

        if (depth.HasValue && depth != Depth)
        {
            Depth = depth;
            changed = true;
        }

        if (dimensionsUnit != DimensionsUnit)
        {
            DimensionsUnit = dimensionsUnit;
            changed = true;
        }

        if (weightUnit != WeightUnit)
        {
            WeightUnit = weightUnit;
            changed = true;
        }

        if (trackInventory.HasValue && trackInventory != TrackInventory)
        {
            TrackInventory = trackInventory.Value;
            changed = true;
            if (!TrackInventory)
                AddDomainEvent(domainEvent: new Events.ClearStockItems(VariantId: Id));
        }

        if (costPrice.HasValue && costPrice != CostPrice)
        {
            CostPrice = costPrice;
            changed = true;
        }

        if (costCurrency != CostCurrency)
        {
            CostCurrency = costCurrency;
            changed = true;
        }

        if (position.HasValue && position != Position)
        {
            Position = position.Value;
            changed = true;
        }

        if (discontinueOn != null && discontinueOn != DiscontinueOn)
        {
            DiscontinueOn = discontinueOn;
            changed = true;
        }

        if (publicMetadata != null && !PublicMetadata.MetadataEquals(dict2: publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
        }

        if (privateMetadata != null && !PrivateMetadata.MetadataEquals(dict2: privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
        }

        if (changed)
        {
            this.MarkAsUpdated();
            AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
            if (!IsMaster)
                AddDomainEvent(domainEvent: new Product.Events.VariantUpdated(VariantId: Id, ProductId: ProductId));
        }

        return this;
    }

    public ErrorOr<Price> SetPrice(decimal? amount, decimal? compareAtAmount = null, string currency = Price.Constraints.DefaultCurrency)
    {
        if (amount < 0) return Errors.InvalidPrice;
        if (string.IsNullOrWhiteSpace(value: currency)) return Price.Errors.CurrencyRequired;
        if (currency.Length >CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength) return Price.Errors.CurrencyTooLong;
        if (!Price.Constraints.ValidCurrencies.Contains(value: currency)) return Price.Errors.InvalidCurrency;

        var price = Prices.FirstOrDefault(predicate: m => m.Currency == currency);

        if (price is null)
        {
            var priceResult = Price.Create(variantId: Id, amount: amount, currency: currency, compareAtAmount: compareAtAmount);
            if (priceResult.IsError)
            {
                return priceResult.Errors;
            }
            Prices.Add(item: priceResult.Value);
            price = priceResult.Value; // Assign the newly created price
        }
        else
        {
            var updateResult = price.Update(amount: amount, compareAtAmount: compareAtAmount);
            if (updateResult.IsError)
            {
                return updateResult.Errors;
            }
        }

        AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
        AddDomainEvent(domainEvent: new Events.VariantPriceChanged(VariantId: Id, ProductId: ProductId, Amount: amount ?? 0, Currency: currency));
        return price; // Return the price object
    }

    public ErrorOr<Variant> AttachStockItem(StockItem? stockItem)
    {
        if (stockItem is null)
        {
            return Error.Validation(code: "Variant.InvalidStockItem", description: "StockItem cannot be null.");
        }

        if (stockItem.Variant.Id != Id)
        {
            return Error.Validation(code: "Variant.MismatchedStockItem", description: "The provided StockItem does not belong to this Variant.");
        }

        if (StockItems.Any(predicate: si => si.StockLocationId == stockItem.StockLocationId))
        {
            return Error.Conflict(code: "Variant.DuplicateStockLocation", description: "A stock record for this location is already attached.");
        }

        StockItems.Add(item: stockItem);
        AddDomainEvent(domainEvent: new Events.StockSet(VariantId: Id, StockItemId: stockItem.Id, QuantityOnHand: stockItem.QuantityOnHand, StockLocationId: stockItem.StockLocationId));
        AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));

        return this;
    }

    public ErrorOr<Variant> UpdateStockLevel(Guid stockLocationId, int newQuantityOnHand, bool? backorderable = null)
    {
        var stockItem = StockItems.FirstOrDefault(predicate: si => si.StockLocationId == stockLocationId);

        if (stockItem is null)
        {
            return StockItem.Errors.NotFound(id: stockLocationId);
        }
        var oldQuantity = stockItem.QuantityOnHand;
        var oldBackorderable = stockItem.Backorderable;

        var difference = newQuantityOnHand - stockItem.QuantityOnHand;
        if (difference != 0)
        {
            var adjustResult = stockItem.Adjust(quantity: difference, originator: StockMovement.MovementOriginator.Adjustment, reason: "Stock level adjustment");
            if (adjustResult.IsError)
            {
                return adjustResult.FirstError;
            }
        }

        if (backorderable.HasValue && backorderable.Value != stockItem.Backorderable)
        {
            stockItem.Backorderable = backorderable.Value;
        }

        if (oldQuantity != stockItem.QuantityOnHand || oldBackorderable != stockItem.Backorderable)
        {
            AddDomainEvent(domainEvent: new Events.StockSet(VariantId: Id, StockItemId: stockItem.Id, QuantityOnHand: stockItem.QuantityOnHand, StockLocationId: stockItem.StockLocationId));
            AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
        }

        return this;
    }

    public ErrorOr<Variant> AddOptionValue(OptionValue? optionValue)
    {
        if (IsMaster)
            return Errors.MasterCannotHaveOptionValues;

        if (optionValue is null)
            return OptionValue.Errors.NotFound(id: Guid.Empty);

        if (Product.ProductOptionTypes.All(predicate: pot => pot.OptionTypeId != optionValue.OptionTypeId))
            return Error.Validation(code: "Variant.InvalidOptionValue", description: "Option type is not associated with the product.");

        if (OptionValueVariants.Any(predicate: ovv => ovv.OptionValueId == optionValue.Id))
            return this; // Already linked

        var ovvResult = VariantOptionValue.Create(variantId: Id, optionValueId: optionValue.Id);
        if (ovvResult.IsError)
            return ovvResult.FirstError;

        OptionValueVariants.Add(item: ovvResult.Value);

        AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
        AddDomainEvent(domainEvent: new Events.OptionAdded(VariantId: Id, OptionId: optionValue.Id));
        return this;
    }

    public ErrorOr<Variant> RemoveOptionValue(Guid optionValueId)
    {
        if (IsMaster)
            return Errors.MasterCannotHaveOptionValues;

        var ovv = OptionValueVariants.FirstOrDefault(predicate: ov => ov.OptionValueId == optionValueId);
        if (ovv is null)
            return OptionValue.Errors.NotFound(id: optionValueId);

        OptionValueVariants.Remove(item: ovv);

        AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
        AddDomainEvent(domainEvent: new Events.OptionRemoved(VariantId: Id, OptionId: optionValueId));
        return this;
    }

    public ErrorOr<Variant> AddAsset(ProductImage? asset)
    {
        if (asset == null)
            return Error.Validation(code: "Variant.InvalidAsset", description: "Asset cannot be null.");

        var validationErrors = asset.ValidateParams(prefix: nameof(ProductImage));
        if (validationErrors.Any())
            return validationErrors.First();

        if (Images.Any(predicate: a => a.Type == asset.Type))
            return Error.Conflict(code: "Variant.DuplicateAssetType", description: $"An asset of type '{asset.Type}' already exists.");

        var maxPosition = Images.Any() ? Images.Max(selector: a => a.Position) : 0;
        asset.SetPosition(position: maxPosition + 1);
        Images.Add(item: asset);

        AddDomainEvent(domainEvent: new Events.ImageAdded(VariantId: Id, AssetId: asset.Id));
        return this;
    }

    public ErrorOr<Variant> RemoveAsset(Guid assetId)
    {
        var asset = Images.FirstOrDefault(predicate: a => a.Id == assetId);
        if (asset == null)
            return Errors.NotFound(id: assetId);

        Images.Remove(item: asset);
        AddDomainEvent(domainEvent: new Events.ImageRemoved(VariantId: Id, AssetId: asset.Id));
        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        if (IsMaster)
            return Errors.MasterCannotBeDeleted;

        if (Orders.Any(predicate: o => o.CompletedAt.HasValue))
            return Errors.CannotDeleteWithCompleteOrders;

        this.MarkAsDeleted();
        AddDomainEvent(domainEvent: new Product.Events.VariantRemoved(VariantId: Id, ProductId: ProductId));
        AddDomainEvent(domainEvent: new Events.RemoveFromIncompleteOrders(VariantId: Id));
        AddDomainEvent(domainEvent: new Events.Deleted(VariantId: Id, ProductId: ProductId));
        return Result.Deleted;
    }

    public ErrorOr<Variant> Discontinue()
    {
        if (Discontinued)
            return this;

        DiscontinueOn = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
        return this;
    }

    #endregion

    #region Events
    public static class Events
    {
        /// <summary>
        /// Purpose: Notify that a variant has been created
        /// </summary>
        public sealed record Created(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a variant has been updated
        /// </summary>
        public sealed record Updated(Guid VariantId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that a variant has been deleted
        /// </summary>
        public sealed record Deleted(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that an asset has been added to a variant
        /// </summary>
        public sealed record ImageAdded(Guid VariantId, Guid AssetId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that an asset has been removed from a variant
        /// </summary>
        public sealed record ImageRemoved(Guid VariantId, Guid AssetId) : DomainEvent;

        public sealed record OptionAdded(Guid VariantId, Guid OptionId) : DomainEvent;

        public sealed record OptionRemoved(Guid VariantId, Guid OptionId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that stock has been set for a variant
        /// </summary>
        public sealed record StockSet(Guid VariantId, Guid StockItemId, int QuantityOnHand, Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that master variant stock should be set to out of stock
        /// </summary>
        public sealed record SetMasterOutOfStock(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that stock items should be cleared
        /// </summary>
        public sealed record ClearStockItems(Guid VariantId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that variant should be removed from incomplete orders
        /// </summary>
        public sealed record RemoveFromIncompleteOrders(Guid VariantId) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that variant price has changed
        /// </summary>
        public sealed record VariantPriceChanged(Guid VariantId, Guid ProductId, decimal Amount, string Currency) : DomainEvent;

        /// <summary>
        /// Purpose: Notify that variant price has been removed
        /// </summary>
        public sealed record VariantPriceRemoved(Guid VariantId, Guid PriceId, decimal Amount, string Currency) : DomainEvent;
    }
    #endregion


}