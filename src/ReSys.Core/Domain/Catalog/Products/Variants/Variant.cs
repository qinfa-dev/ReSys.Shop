using System.ComponentModel.DataAnnotations;

using Microsoft.AspNetCore.Http.HttpResults;

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
            CommonInput.Errors.Conflict(prefix: nameof(Variant), field: "Deletion", msg: "Cannot delete variant with completed orders.");
        /// <summary>
        /// Triggered when attempting to add option values to the master variant.
        /// Master variant must remain as default configuration without option values.
        /// </summary>
        public static Error MasterCannotHaveOptionValues =>
            CommonInput.Errors.InvalidOperation(prefix: nameof(Variant), field: "OptionValues", msg: "Master variant cannot have option values.");
        /// <summary>
        /// Triggered when attempting to delete the master variant.
        /// Every product must have exactly one master variant for fallback configuration.
        /// </summary>
        public static Error MasterCannotBeDeleted =>
            CommonInput.Errors.InvalidOperation(prefix: nameof(Variant), field: "Deletion", msg: "Master variant cannot be deleted.");
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
        public static Error InvalidWeightUnit => Error.Validation(
            code: "Price.InvalidWeightUnit",
            description: $"Weight unit must be one of: {string.Join(separator: ", ", value: Constraints.ValidWeightUnits)}.");

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

    /// <summary>

    /// Gets or sets the unique identifier of the parent <see cref="Product"/> this variant belongs to.

    /// </summary>

    public Guid ProductId { get; set; }

    /// <summary>

    /// Gets or sets a value indicating whether this variant is the master variant for its product.

    /// A master variant represents the default configuration and cannot have option values.

    /// </summary>

    public bool IsMaster { get; set; }

    /// <summary>

    /// Gets or sets the Stock Keeping Unit (SKU) for this variant.

    /// SKU is a unique identifier used for inventory tracking and ordering.

    /// </summary>

    public string? Sku { get; set; }

    /// <summary>

    /// Gets or sets the barcode (e.g., UPC, EAN) for this variant, used for scanning.

    /// </summary>

    public string? Barcode { get; set; }

    /// <summary>

    /// Gets or sets the weight of the variant, used for shipping calculations.

    /// </summary>

    public decimal? Weight { get; set; }

    /// <summary>

    /// Gets or sets the height of the variant, used for shipping calculations.

    /// </summary>

    public decimal? Height { get; set; }

    /// <summary>

    /// Gets or sets the width of the variant, used for shipping calculations.

    /// </summary>

    public decimal? Width { get; set; }

    /// <summary>

    /// Gets or sets the depth of the variant, used for shipping calculations.

    /// </summary>

    public decimal? Depth { get; set; }

    /// <summary>

    /// Gets or sets the unit of measurement for dimensions (e.g., "mm", "cm", "in").

    /// </summary>

    public string? DimensionsUnit { get; set; }

    /// <summary>

    /// Gets or sets the unit of measurement for weight (e.g., "g", "kg", "lb").

    /// </summary>

    public string? WeightUnit { get; set; }

    /// <summary>

    /// Gets or sets a value indicating whether inventory should be tracked for this variant.

    /// Typically true for physical products and false for digital products.

    /// </summary>

    public bool TrackInventory { get; set; } = true;

    /// <summary>

    /// Gets or sets the cost price of acquiring or producing this variant.

    /// </summary>

    public decimal? CostPrice { get; set; }

    /// <summary>

    /// Gets or sets the currency of the cost price (e.g., "USD").

    /// </summary>

    public string? CostCurrency { get; set; }

    /// <summary>

    /// Gets or sets the display order of this variant among others in the same product.

    /// Lower values typically appear first.

    /// </summary>

    public int Position { get; set; }

    /// <summary>

    /// Gets or sets the date and time when the variant was soft-deleted.

    /// </summary>

    public DateTimeOffset? DeletedAt { get; set; }

    /// <summary>

    /// Gets or sets the identifier of the user who soft-deleted the variant.

    /// </summary>

    public string? DeletedBy { get; set; }

    /// <summary>

    /// Gets or sets a value indicating whether the variant is soft-deleted.

    /// </summary>

    public bool IsDeleted { get; set; }

    /// <summary>

    /// Gets or sets the date and time when the variant should be discontinued and no longer available for purchase.

    /// </summary>

    public DateTimeOffset? DiscontinueOn { get; set; }

    /// <summary>

    /// Gets or sets public-facing metadata associated with the variant.

    /// This dictionary can store flexible key-value pairs visible to customers.

    /// </summary>

    public IDictionary<string, object?>? PublicMetadata { get; set; }

    /// <summary>

    /// Gets or sets private metadata associated with the variant.

    /// This dictionary can store internal-only key-value pairs not visible to customers.

    /// </summary>

    public IDictionary<string, object?>? PrivateMetadata { get; set; }



    /// <summary>

    /// Gets or sets the row version timestamp for optimistic concurrency control.

    /// </summary>

    [Timestamp]

    public byte[]? RowVersion { get; set; }

    #endregion

    #region Relationships
    public Product Product { get; set; } = null!;
    public ICollection<ProductImage> Images { get; set; } = new List<ProductImage>();
    public ICollection<Price> Prices { get; set; } = new List<Price>();
    public ICollection<StockItem> StockItems { get; set; } = new List<StockItem>();
    public ICollection<VariantOptionValue> VariantOptionValues { get; set; } = new List<VariantOptionValue>();
    public ICollection<OptionValue> OptionValues => VariantOptionValues.Select(selector: ovv => ovv.OptionValue).ToList();
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
    /// <summary>
    /// Initializes a new instance of the <see cref="Variant"/> class.
    /// This is a private constructor primarily used by Entity Framework Core for materialization
    /// and by the static factory method <see cref="Create"/>.
    /// </summary>
    private Variant() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new <see cref="Variant"/> instance.
    /// This factory method performs initial validation and sets up the variant's physical and inventory properties.
    /// </summary>
    /// <param name="productId">The unique identifier of the parent <see cref="Product"/> this variant belongs to.</param>
    /// <param name="isMaster">A flag indicating if this is the master variant for the product. Defaults to false.</param>
    /// <param name="sku">The Stock Keeping Unit (SKU) for this variant. Must be unique within the product.</param>
    /// <param name="barcode">The barcode (e.g., UPC, EAN) for this variant.</param>
    /// <param name="weight">The weight of the variant for shipping calculations.</param>
    /// <param name="height">The height of the variant for shipping calculations.</param>
    /// <param name="width">The width of the variant for shipping calculations.</param>
    /// <param name="depth">The depth of the variant for shipping calculations.</param>
    /// <param name="dimensionsUnit">The unit of measurement for dimensions (e.g., "cm", "in"). Defaults to <see cref="Constraints.DefaultDimensionUnit"/>.</param>
    /// <param name="weightUnit">The unit of measurement for weight (e.g., "kg", "lb"). Defaults to <see cref="Constraints.DefaultWeightUnit"/>.</param>
    /// <param name="costPrice">The cost price of acquiring or producing this variant.</param>
    /// <param name="costCurrency">The currency of the cost price. Defaults to <see cref="Price.Constraints.DefaultCurrency"/>.</param>
    /// <param name="trackInventory">A flag indicating if inventory should be tracked for this variant. True for physical products, false for digital products. Defaults to true.</param>
    /// <param name="position">The display order of this variant among others in the same product. Defaults to 0.</param>
    /// <param name="publicMetadata">Optional dictionary for public-facing metadata.</param>
    /// <param name="privateMetadata">Optional dictionary for internal-only metadata.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Variant}"/> result.
    /// Returns the newly created <see cref="Variant"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails (e.g., <see cref="Errors.ProductRequired"/>, <see cref="Errors.InvalidDimensionUnit"/>).
    /// </returns>
    /// <remarks>
    /// This factory method performs initial validation for mandatory fields and units.
    /// It initializes metadata dictionaries to prevent null references.
    /// <para>
    /// For non-master variants, it adds a <see cref="Product.Events.VariantAdded"/> domain event.
    /// If <paramref name="trackInventory"/> is true for a non-master variant, it also adds a <see cref="Events.SetMasterOutOfStock"/> event.
    /// A general <see cref="Events.Created"/> domain event is always added.
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// Guid productId = Guid.NewGuid(); // Assume this is an existing Product ID
    /// var variantResult = Variant.Create(
    ///     productId: productId,
    ///     isMaster: false,
    ///     sku: "TS-BLU-SM",
    ///     weight: 0.2m,
    ///     weightUnit: "kg",
    ///     costPrice: 10.50m,
    ///     costCurrency: "USD",
    ///     publicMetadata: new Dictionary&lt;string, object?&gt; { { "color", "blue" }, { "size", "small" } });
    /// 
    /// if (variantResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating variant: {variantResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newVariant = variantResult.Value;
    ///     Console.WriteLine($"Variant '{newVariant.Sku}' created for Product {productId}.");
    /// }
    /// </code>
    /// </remarks>
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
            CreatedAt = DateTimeOffset.UtcNow // Added missing CreatedAt initialization
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
    /// <summary>
    /// Updates the mutable properties of the variant.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="sku">The new Stock Keeping Unit (SKU) for this variant. If null, the existing SKU is retained.</param>
    /// <param name="barcode">The new barcode for this variant. If null, the existing barcode is retained.</param>
    /// <param name="weight">The new weight of the variant. If null, the existing weight is retained.</param>
    /// <param name="height">The new height of the variant. If null, the existing height is retained.</param>
    /// <param name="width">The new width of the variant. If null, the existing width is retained.</param>
    /// <param name="depth">The new depth of the variant. If null, the existing depth is retained.</param>
    /// <param name="dimensionsUnit">The new unit of measurement for dimensions. If null, the existing unit is retained.</param>
    /// <param name="weightUnit">The new unit of measurement for weight. If null, the existing unit is retained.</param>
    /// <param name="trackInventory">The new flag indicating whether inventory should be tracked. If null, the existing flag is retained.</param>
    /// <param name="costPrice">The new cost price of the variant. If null, the existing cost price is retained.</param>
    /// <param name="costCurrency">The new currency of the cost price. If null, the existing cost currency is retained.</param>
    /// <param name="position">The new display order of the variant. If null, the existing position is retained.</param>
    /// <param name="discontinueOn">The new date and time when the variant should be discontinued. If null, the existing date is retained.</param>
    /// <param name="publicMetadata">New public metadata associated with the variant. If null, the existing public metadata is retained.</param>
    /// <param name="privateMetadata">New private metadata associated with the variant. If null, the existing private metadata is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Variant}"/> result.
    /// Returns the updated <see cref="Variant"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails (e.g., <see cref="Errors.InvalidPrice"/>, <see cref="Errors.InvalidDimensionUnit"/>).
    /// </returns>
    /// <remarks>
    /// This method performs validation for all provided parameters and updates properties
    /// if their new values are different from the current ones.
    /// <para>
    /// The <c>UpdatedAt</c> timestamp is automatically updated if any changes occur via <c>MarkAsUpdated()</c>.
    /// A general <see cref="Events.Updated"/> domain event is added.
    /// If the variant is not the master variant, a <see cref="Product.Events.VariantUpdated"/> event is also added to the product.
    /// If <paramref name="trackInventory"/> changes from true to false, a <see cref="Events.ClearStockItems"/> event is emitted.
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId); // Assume existing variant
    /// var updateResult = variant.Update(
    ///     weight: 0.25m,
    ///     weightUnit: "lb",
    ///     costPrice: 12.00m,
    ///     discontinueOn: DateTimeOffset.UtcNow.AddYears(1));
    /// 
    /// if (updateResult.IsError)
    /// {
    ///     Console.WriteLine($"Error updating variant: {updateResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Variant '{variant.Sku}' updated successfully.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
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
        if (currency.Length > CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength) return Price.Errors.CurrencyTooLong;
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
    /// <summary>
    /// Sets the price for this variant in a specific currency.
    /// If a price for the given currency already exists, it updates the existing price; otherwise, it creates a new one.
    /// </summary>
    /// <param name="amount">The current selling price. Must be non-negative.</param>
    /// <param name="compareAtAmount">Optional original/list price for sale display.</param>
    /// <param name="currency">The ISO 4217 currency code (e.g., "USD"). Defaults to <see cref="Price.Constraints.DefaultCurrency"/>.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Price}"/> result.
    /// Returns the created or updated <see cref="Price"/> instance on success.
    /// Returns one of the <see cref="Errors"/> or <see cref="Price.Errors"/> if validation fails.
    /// </returns>
    /// <remarks>
    /// This method ensures that prices are valid and uses the <see cref="Price.Create"/> and <see cref="Price.Update"/>
    /// methods to manage price entities.
    /// <para>
    /// A general <see cref="Events.Updated"/> domain event is added for the variant.
    /// A specific <see cref="Events.VariantPriceChanged"/> event is also added to signal price changes.
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId);
    /// var priceResult = variant.SetPrice(amount: 15.99m, currency: "USD", compareAtAmount: 20.00m);
    /// 
    /// if (priceResult.IsError)
    /// {
    ///     Console.WriteLine($"Error setting price: {priceResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Price for variant '{variant.Sku}' in USD set to {priceResult.Value.Amount}.");
    /// }
    /// </code>
    /// </remarks>

    /// <summary>
    /// Associates an existing <see cref="OptionValue"/> with this variant.
    /// This defines a characteristic (e.g., "Red" color, "Large" size) of the variant.
    /// </summary>
    /// <param name="optionValue">The <see cref="OptionValue"/> instance to associate with this variant.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Variant}"/> result.
    /// Returns the updated <see cref="Variant"/> instance on success.
    /// Returns <see cref="Errors.MasterCannotHaveOptionValues"/> if called on a master variant.
    /// Returns <see cref="OptionValue.Errors.NotFound(Guid)"/> if <paramref name="optionValue"/> is null.
    /// Returns an <see cref="DbLoggerCategory.Model.Validation"/> if the option type is not associated with the product.
    /// </returns>
    /// <remarks>
    /// Master variants are not allowed to have option values.
    /// This method also ensures that the <see cref="OptionType"/> of the provided <paramref name="optionValue"/>
    /// is linked to the parent product.
    /// The variant's <c>UpdatedAt</c> timestamp is updated, and a <see cref="Events.Updated"/>
    /// and <see cref="Events.OptionAdded"/> domain events are added.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId); // Assume existing non-master variant
    /// var colorOptionValue = GetOptionValueByName("Red"); // Assume existing OptionValue
    /// var result = variant.AddOptionValue(colorOptionValue);
    /// 
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error adding option value: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Option value '{colorOptionValue.Presentation}' added to variant '{variant.DescriptiveName}'.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Variant> AddOptionValue(OptionValue? optionValue)
    {
        if (IsMaster)
            return Errors.MasterCannotHaveOptionValues;

        if (optionValue is null)
            return OptionValue.Errors.NotFound(id: Guid.Empty);

        if (Product.ProductOptionTypes.All(predicate: pot => pot.OptionTypeId != optionValue.OptionTypeId))
            return Error.Validation(code: "Variant.InvalidOptionValue", description: "Option type is not associated with the product.");

        if (VariantOptionValues.Any(predicate: ovv => ovv.OptionValueId == optionValue.Id))
            return this; // Already linked

        var ovvResult = VariantOptionValue.Create(variantId: Id, optionValueId: optionValue.Id);
        if (ovvResult.IsError)
            return ovvResult.FirstError;

        VariantOptionValues.Add(item: ovvResult.Value);

        AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
        AddDomainEvent(domainEvent: new Events.OptionAdded(VariantId: Id, OptionId: optionValue.Id));
        return this;
    }

    /// <summary>
    /// Removes an associated <see cref="OptionValue"/> from this variant.
    /// </summary>
    /// <param name="optionValueId">The unique identifier of the <see cref="OptionValue"/> to remove.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Variant}"/> result.
    /// Returns the updated <see cref="Variant"/> instance on success.
    /// Returns <see cref="Errors.MasterCannotHaveOptionValues"/> if called on a master variant.
    /// Returns <see cref="OptionValue.Errors.NotFound(Guid)"/> if the specified <see cref="OptionValue"/> is not found for this variant.
    /// </returns>
    /// <remarks>
    /// This method removes the <see cref="VariantOptionValue"/> entry that links the specified <see cref="OptionValue"/> to this variant.
    /// The variant's <c>UpdatedAt</c> timestamp is updated, and a <see cref="Events.Updated"/>
    /// and <see cref="Events.OptionRemoved"/> domain events are added.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId); // Assume existing non-master variant
    /// var optionValueIdToRemove = variant.OptionValues.First(ov => ov.Name == "Red").Id;
    /// var result = variant.RemoveOptionValue(optionValueIdToRemove);
    /// 
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error removing option value: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Option value removed from variant '{variant.DescriptiveName}'.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Variant> RemoveOptionValue(Guid optionValueId)
    {
        if (IsMaster)
            return Errors.MasterCannotHaveOptionValues;

        var ovv = VariantOptionValues.FirstOrDefault(predicate: ov => ov.OptionValueId == optionValueId);
        if (ovv is null)
            return OptionValue.Errors.NotFound(id: optionValueId);

        VariantOptionValues.Remove(item: ovv);

        AddDomainEvent(domainEvent: new Events.Updated(VariantId: Id));
        AddDomainEvent(domainEvent: new Events.OptionRemoved(VariantId: Id, OptionId: optionValueId));
        return this;
    }

    /// <summary>
    /// Adds a new image asset to the variant's collection of images.
    /// The asset's position is automatically determined to ensure proper ordering.
    /// </summary>
    /// <param name="asset">The <see cref="ProductImage"/> instance to be added to the variant. Must not be null.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Variant}"/> result.
    /// Returns the updated <see cref="Variant"/> instance on success.
    /// Returns an <see cref="DbLoggerCategory.Model.Validation"/> if the provided asset is null.
    /// Returns one of the <see cref="ProductImage.Errors"/> if the asset's parameters are invalid.
    /// Returns an <see cref="Conflict"/> if a duplicate image type exists.
    /// </returns>
    /// <remarks>
    /// This method performs checks for null assets and duplicate image types to maintain data integrity.
    /// It automatically assigns a position to the new asset based on existing images.
    /// A <see cref="Events.ImageAdded"/> domain event is added.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId);
    /// var newImage = ProductImage.Create(
    ///     url: "https://example.com/variant_side.jpg",
    ///     variantId: variant.Id,
    ///     alt: "Blue T-Shirt side view",
    ///     type: nameof(ProductImageType.Gallery),
    ///     contentType: "image/jpeg").Value;
    /// 
    /// var result = variant.AddAsset(newImage);
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error adding image to variant: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Image added to variant '{variant.DescriptiveName}'.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Removes an image asset from the variant's collection.
    /// </summary>
    /// <param name="assetId">The unique identifier of the <see cref="ProductImage"/> to remove.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Variant}"/> result.
    /// Returns the updated <see cref="Variant"/> instance on success.
    /// Returns <see cref="Errors.NotFound(Guid)"/> if the specified asset is not found within this variant's images.
    /// </returns>
    /// <remarks>
    /// This method removes the asset from the owned collection.
    /// A <see cref="Events.ImageRemoved"/> domain event is added.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId);
    /// var imageToRemoveId = variant.Images.First(img => img.Type == nameof(ProductImageType.Gallery)).Id;
    /// var result = variant.RemoveAsset(imageToRemoveId);
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error removing image: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Image removed from variant '{variant.DescriptiveName}'.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Variant> RemoveAsset(Guid assetId)
    {
        var asset = Images.FirstOrDefault(predicate: a => a.Id == assetId);
        if (asset == null)
            return Errors.NotFound(id: assetId);

        Images.Remove(item: asset);
        AddDomainEvent(domainEvent: new Events.ImageRemoved(VariantId: Id, AssetId: asset.Id));
        return this;
    }

    /// <summary>
    /// Soft-deletes the variant, marking it as deleted and triggering relevant domain events.
    /// This operation is not permitted for the master variant, or if the variant is part of completed orders.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful soft-deletion.
    /// Returns <see cref="Errors.MasterCannotBeDeleted"/> if an attempt is made to delete the master variant.
    /// Returns <see cref="Errors.CannotDeleteWithCompleteOrders"/> if the variant is linked to completed orders.
    /// </returns>
    /// <remarks>
    /// Soft-deletion is preferred over hard-deletion to preserve historical data, especially
    /// if the variant has been part of completed orders.
    /// <para>
    /// This method sets <c>DeletedAt</c>, <c>IsDeleted</c> flags via <c>MarkAsDeleted()</c> extension.
    /// It adds a <see cref="Product.Events.VariantRemoved"/> event to the parent product,
    /// an <see cref="Events.RemoveFromIncompleteOrders"/> event, and a general <see cref="Events.Deleted"/> event.
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId); // Assume existing non-master variant
    /// var result = variant.Delete(); // Soft delete
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error soft-deleting variant: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Variant '{variant.DescriptiveName}' has been soft-deleted.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Discontinues the variant, setting its <see cref="DiscontinueOn"/> date to the current time.
    /// A discontinued variant is no longer available for purchase.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Variant}"/> result.
    /// Returns the updated <see cref="Variant"/> instance on success.
    /// Returns the current variant instance if it is already discontinued (idempotent).
    /// </returns>
    /// <remarks>
    /// This method updates the <c>DiscontinueOn</c> property and the variant's <c>UpdatedAt</c> timestamp.
    /// A general <see cref="Events.Updated"/> domain event is added.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var variant = GetVariantById(variantId);
    /// var result = variant.Discontinue();
    /// if (result.IsError)
    /// {
    ///     Console.WriteLine($"Error discontinuing variant: {result.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"Variant '{variant.DescriptiveName}' has been discontinued.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
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
    /// <summary>
    /// Defines domain events related to the lifecycle and state changes of a <see cref="Variant"/>.
    /// These events are crucial for enabling a decoupled, event-driven architecture, allowing
    /// other services or bounded contexts to react to variant-related changes.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Event fired when a new <see cref="Variant"/> is successfully created.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the newly created variant.</param>
        /// <param name="ProductId">The unique identifier of the parent product.</param>
        public sealed record Created(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Event fired when a <see cref="Variant"/>'s properties (e.g., SKU, physical attributes) are updated.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the updated variant.</param>
        public sealed record Updated(Guid VariantId) : DomainEvent;

        /// <summary>
        /// Event fired when a <see cref="Variant"/> is soft-deleted.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the deleted variant.</param>
        /// <param name="ProductId">The unique identifier of the parent product.</param>
        public sealed record Deleted(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Event fired when a new image asset is added to a variant.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        /// <param name="AssetId">The unique identifier of the added image asset.</param>
        public sealed record ImageAdded(Guid VariantId, Guid AssetId) : DomainEvent;

        /// <summary>
        /// Event fired when an image asset is removed from a variant.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        /// <param name="AssetId">The unique identifier of the removed image asset.</param>
        public sealed record ImageRemoved(Guid VariantId, Guid AssetId) : DomainEvent;

        /// <summary>
        /// Event fired when an <see cref="OptionValue"/> is added to a variant.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        /// <param name="OptionId">The unique identifier of the added <see cref="OptionValue"/>.</param>
        public sealed record OptionAdded(Guid VariantId, Guid OptionId) : DomainEvent;

        /// <summary>
        /// Event fired when an <see cref="OptionValue"/> is removed from a variant.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        /// <param name="OptionId">The unique identifier of the removed <see cref="OptionValue"/>.</param>
        public sealed record OptionRemoved(Guid VariantId, Guid OptionId) : DomainEvent;

        /// <summary>
        /// Event fired when stock levels are set or updated for a specific <see cref="StockItem"/> of this variant.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        /// <param name="StockItemId">The unique identifier of the stock item.</param>
        /// <param name="QuantityOnHand">The new quantity on hand for the stock item.</param>
        /// <param name="StockLocationId">The unique identifier of the stock location.</param>
        public sealed record StockSet(Guid VariantId, Guid StockItemId, int QuantityOnHand, Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Event fired to signal that the master variant's stock should be set to out of stock.
        /// This is often triggered when a non-master variant is added and tracks inventory.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the master variant.</param>
        /// <param name="ProductId">The unique identifier of the parent product.</param>
        public sealed record SetMasterOutOfStock(Guid VariantId, Guid ProductId) : DomainEvent;

        /// <summary>
        /// Event fired to signal that all <see cref="StockItem"/>s for a variant should be cleared (e.g., when it no longer tracks inventory).
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        public sealed record ClearStockItems(Guid VariantId) : DomainEvent;

        /// <summary>
        /// Event fired to signal that this variant should be removed from any incomplete (e.g., pending) customer orders.
        /// This is typically triggered during variant deletion.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        public sealed record RemoveFromIncompleteOrders(Guid VariantId) : DomainEvent;

        /// <summary>
        /// Event fired when a variant's price for a specific currency has changed.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        /// <param name="ProductId">The unique identifier of the parent product.</param>
        /// <param name="Amount">The new amount of the price.</param>
        /// <param name="Currency">The currency of the price.</param>
        public sealed record VariantPriceChanged(Guid VariantId, Guid ProductId, decimal Amount, string Currency) : DomainEvent;

        /// <summary>
        /// Event fired when a variant's price for a specific currency has been removed.
        /// </summary>
        /// <param name="VariantId">The unique identifier of the variant.</param>
        /// <param name="PriceId">The unique identifier of the removed price entry.</param>
        /// <param name="Amount">The amount of the removed price (for historical context).</param>
        /// <param name="Currency">The currency of the removed price.</param>
        public sealed record VariantPriceRemoved(Guid VariantId, Guid PriceId, decimal Amount, string Currency) : DomainEvent;
    }
    #endregion
}