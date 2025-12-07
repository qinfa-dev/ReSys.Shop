using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Orders.Shipments;
using ReSys.Core.Domain.Stores.ShippingMethods;

namespace ReSys.Core.Domain.ShippingMethods;

/// <summary>
/// Represents a shipping method available across the e-commerce platform.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Purpose:</strong>
/// This aggregate root defines and manages shipping methods for delivering products to customers.
/// It encapsulates shipping type classification, cost calculation, estimated delivery times, and
/// multi-store availability configuration. Shipping methods form the foundation for logistics
/// planning and customer delivery option presentation.
/// </para>
/// <para>
/// <strong>Key Characteristics:</strong>
/// <list type="bullet">
/// <item><description>Global Definition: Defines shipping methods once, then mapped to stores via StoreShippingMethod</description></item>
/// <item><description>Type Categorization: Classified as Standard, Express, Overnight, Pickup, or FreeShipping</description></item>
/// <item><description>Cost Calculation: Intelligent cost determination with weight-based surcharges and free shipping detection</description></item>
/// <item><description>Estimated Delivery: Min/max day estimates for customer expectations</description></item>
/// <item><description>Store Customization: Per-store availability and cost override via StoreShippingMethod entities</description></item>
/// <item><description>Metadata Support: Public (customer-visible) and private (internal) configuration data</description></item>
/// <item><description>Display Control: Channel-specific visibility (FrontEnd, BackEnd, Both, None)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Important Invariants:</strong>
/// <list type="bullet">
/// <item><description>Name must be unique across all shipping methods</description></item>
/// <item><description>Type must be one of: Standard, Express, Overnight, Pickup, FreeShipping</description></item>
/// <item><description>BaseCost must be non-negative (0 for free shipping)</description></item>
/// <item><description>EstimatedDaysMin must be ≤ EstimatedDaysMax when both specified</description></item>
/// <item><description>MaxWeight applies surcharge: 1.5x multiplier for orders exceeding limit</description></item>
/// <item><description>Position determines display order in UI (lower values = higher priority)</description></item>
/// <item><description>Cannot be deleted if associated with active Shipments or active StoreShippingMethod links</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><description><see cref="IHasUniqueName"/>: Name uniqueness enforced via unique index</description></item>
/// <item><description><see cref="IHasPosition"/>: Display ordering for customer-facing lists</description></item>
/// <item><description><see cref="IHasParameterizableName"/>: Display name (Presentation) separate from identifier</description></item>
/// <item><description><see cref="IHasMetadata"/>: PublicMetadata for configs visible to customers, PrivateMetadata for internal use</description></item>
/// <item><description><see cref="IHasDisplayOn"/>: Multi-channel visibility (FrontEnd, BackEnd, Both, None)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Related Aggregates:</strong>
/// <list type="bullet">
/// <item><description>Store: Referenced through StoreShippingMethod for per-store configuration</description></item>
/// <item><description>Shipment: Referenced for fulfillment tracking (Orders domain)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Domain Events Raised:</strong>
/// <list type="bullet">
/// <item><description>Events.Created - New shipping method added to system</description></item>
/// <item><description>Events.Updated - Shipping method configuration changed</description></item>
/// <item><description>Events.Deleted - Shipping method removed (with cascade to StoreShippingMethods)</description></item>
/// </list>
/// </para>
/// <para>
/// <strong>Usage Examples:</strong>
/// </para>
/// <example>
/// <para>Create a standard ground shipping method:</para>
/// <code>
/// var result = ShippingMethod.Create(
///     name: "Ground Shipping",
///     presentation: "Standard Ground (5-7 business days)",
///     type: ShippingMethod.ShippingType.Standard,
///     baseCost: 5.99m,
///     description: "Economical ground delivery",
///     estimatedDaysMin: 5,
///     estimatedDaysMax: 7,
///     position: 1);
/// 
/// if (result.IsSuccess)
/// {
///     var groundShipping = result.Value;
///     _dbContext.ShippingMethods.Add(groundShipping);
///     await _dbContext.SaveChangesAsync();
/// }
/// </code>
/// </example>
/// <example>
/// <para>Create free shipping with metadata for tracking:</para>
/// <code>
/// var metadata = new Dictionary&lt;string, object?&gt;
/// {
///     { "campaign_code", "SUMMER_FREE_SHIP" },
///     { "min_order_amount", 50m },
///     { "promo_end_date", "2025-08-31" }
/// };
/// 
/// var result = ShippingMethod.Create(
///     name: "Free Shipping - Summer Promo",
///     presentation: "FREE - Limited time!",
///     type: ShippingMethod.ShippingType.FreeShipping,
///     baseCost: 0,
///     estimatedDaysMin: 7,
///     estimatedDaysMax: 10,
///     position: 0,
///     publicMetadata: metadata);
/// </code>
/// </example>
/// <example>
/// <para>Calculate shipping cost with weight-based surcharge:</para>
/// <code>
/// var shippingMethod = await _dbContext.ShippingMethods.FindAsync(methodId);
/// 
/// // Order total weight: 25 lbs, method max weight: 20 lbs
/// // Cost = $9.99 * 1.5 = $14.99 (overweight surcharge applied)
/// var shippingCost = shippingMethod.CalculateCost(orderWeight: 25, orderTotal: 150);
/// // Result: $14.99m
/// </code>
/// </example>
/// <example>
/// <para>Update method and make available in specific stores:</para>
/// <code>
/// var method = await _dbContext.ShippingMethods.FindAsync(methodId);
/// 
/// // Update base cost and estimated time
/// await method.Update(
///     baseCost: 6.99m,
///     estimatedDaysMin: 4,
///     estimatedDaysMax: 6);
/// 
/// // Configure for US store with cost override
/// var storeMethod = StoreShippingMethod.Create(
///     storeId: usStoreId,
///     shippingMethodId: method.Id,
///     available: true,
///     storeBaseCost: 6.99m); // Use global cost
/// 
/// // Configure for EU store with Euro pricing
/// var euMethod = StoreShippingMethod.Create(
///     storeId: euStoreId,
///     shippingMethodId: method.Id,
///     available: true,
///     storeBaseCost: 6.50m); // Override for EU pricing
/// </code>
/// </example>
/// </remarks>
public sealed class ShippingMethod : Aggregate, IHasUniqueName, IHasPosition, IHasParameterizableName, IHasMetadata, IHasDisplayOn
{
    #region Constraints
    /// <summary>
    /// Business constraints and limits for shipping methods.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for shipping method names (e.g., "Express Next-Day Air").
        /// </summary>
        /// <remarks>
        /// 100 characters allows descriptive names while maintaining UI consistency.
        /// Example: "Free Shipping (Orders Over $50) - 7-10 Business Days"
        /// </remarks>
        public const int NameMaxLength = 100;

        /// <summary>
        /// Valid shipping method type identifiers.
        /// </summary>
        /// <remarks>
        /// Used to ensure type values are persisted as strings and match enum values:
        /// <list type="bullet">
        /// <item><description>"Standard" - Ground/economical shipping (5-10 days typical)</description></item>
        /// <item><description>"Express" - Faster shipping (2-3 days typical)</description></item>
        /// <item><description>"Overnight" - Premium overnight delivery (1 day)</description></item>
        /// <item><description>"Pickup" - Customer pickup from store/location</description></item>
        /// <item><description>"FreeShipping" - Complimentary delivery (promotional or threshold-based)</description></item>
        /// </list>
        /// </remarks>
        public static readonly string[] ValidTypes = ["Standard", "Express", "Overnight", "Pickup", "FreeShipping"];
    }

    /// <summary>
    /// Categorizes the fundamental nature and characteristics of a shipping method.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Enum Values:</strong>
    /// <list type="bullet">
    /// <item><description>Standard - Ground/economical shipping (lowest cost, longest delivery)</description></item>
    /// <item><description>Express - Faster than standard (2-3 days, moderate cost)</description></item>
    /// <item><description>Overnight - Premium next-day delivery (highest cost)</description></item>
    /// <item><description>Pickup - Customer pickup from physical location</description></item>
    /// <item><description>FreeShipping - Complimentary delivery (triggers IsFreeShipping flag)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public enum ShippingType { Standard, Express, Overnight, Pickup, FreeShipping }
    #endregion

    #region Errors
    /// <summary>
    /// Domain errors specific to shipping method operations.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Indicates a shipping method is required but not provided.
        /// </summary>
        /// <remarks>
        /// Typically occurs when selecting shipping at checkout and no method is available or selected.
        /// Recovery: Ensure at least one shipping method is active and available in the store.
        /// </remarks>
        public static Error Required => Error.Validation(
            code: "ShippingMethod.Required",
            description: "Shipping method is required.");

        /// <summary>
        /// Indicates a shipping method with the specified ID could not be found.
        /// </summary>
        /// <remarks>
        /// Possible causes: ID is incorrect, method was deleted, or method doesn't exist in requested store.
        /// Recovery: Verify the ID is correct, reload available methods from the database.
        /// </remarks>
        public static Error NotFound(Guid id) => Error.NotFound(
            code: "ShippingMethod.NotFound",
            description: $"Shipping method with ID '{id}' was not found.");

        /// <summary>
        /// Indicates a shipping method cannot be deleted because it's currently in use.
        /// </summary>
        /// <remarks>
        /// Prevents referential integrity violations. A method is "in use" if:
        /// <list type="bullet">
        /// <item><description>Active Shipments reference this method</description></item>
        /// <item><description>Active StoreShippingMethod links exist</description></item>
        /// </list>
        /// Recovery: Disable the method (Active=false) instead of deleting, or remove all active associations first.
        /// </remarks>
        public static Error InUse => Error.Conflict(
            code: "ShippingMethod.InUse",
            description: "Cannot delete shipping method that is in use.");

        public static Error NameRequired => CommonInput.Errors.Required(prefix: nameof(ShippingMethod), field: nameof(Name));
        public static Error NameTooLong => CommonInput.Errors.TooLong(prefix: nameof(ShippingMethod), field: nameof(Name), maxLength: Constraints.NameMaxLength);
        public static Error PresentationRequired => CommonInput.Errors.Required(prefix: nameof(ShippingMethod), field: nameof(Presentation));
        public static Error BaseCostNegative => CommonInput.Errors.InvalidRange(prefix: nameof(ShippingMethod), field: nameof(BaseCost), min: 0m);
        public static Error EstimatedDaysRangeInvalid => CommonInput.Errors.InvalidRange(prefix: nameof(ShippingMethod), field: "EstimatedDays");
        public static Error PositionNegative => CommonInput.Errors.InvalidRange(prefix: nameof(ShippingMethod), field: nameof(Position), min: 0);
        public static Error InvalidType => CommonInput.Errors.InvalidEnumValue<ShippingType>(prefix: nameof(ShippingMethod), field: nameof(Type));
    }
    #endregion

    #region Core Properties
    /// <summary>
    /// The unique, human-readable identifier for this shipping method (e.g., "Ground Shipping").
    /// Required by IHasUniqueName concern (enforced via unique index).
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The display name shown to customers (e.g., "Standard Ground (5-7 days)").
    /// Can differ from Name for enhanced UX. Required by IHasParameterizableName concern.
    /// </summary>
    public string Presentation { get; set; } = string.Empty;

    /// <summary>
    /// Optional detailed description displayed in shipping options (e.g., on checkout page).
    /// Example: "Economical ground delivery. Typical delivery 5-7 business days."
    /// </summary>
    public string? Description { get; set; }

    /// <summary>
    /// Categorization of this shipping method (Standard, Express, Overnight, Pickup, FreeShipping).
    /// Determines display priority, cost calculation, and customer selection options.
    /// </summary>
    public ShippingType Type { get; set; }

    /// <summary>
    /// Whether this shipping method is available for use in the system.
    /// When false, excluded from customer selection (but not deleted from database).
    /// Allows temporary disabling without data loss (soft deactivation).
    /// </summary>
    public bool Active { get; set; } = true;

    /// <summary>
    /// Display order for this shipping method in lists/dropdowns (0 = highest priority).
    /// Lower values appear first to customers. Managed by IHasPosition concern.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Base shipping cost in the configured currency.
    /// For methods with MaxWeight: applied to orders within the limit.
    /// For overweight orders: multiplied by 1.5 as surcharge (see CalculateCost).
    /// Example: $5.99 for ground shipping, $0 for free shipping.
    /// </summary>
    public decimal BaseCost { get; set; }

    /// <summary>
    /// ISO 4217 currency code for BaseCost (e.g., "USD", "EUR", "GBP").
    /// Default: "USD". Should match the store's primary currency where method is used.
    /// </summary>
    public string Currency { get; set; } = "USD";

    /// <summary>
    /// Minimum estimated delivery days for this method.
    /// Displayed to customers: "Delivery in 5-7 business days".
    /// Must be ≤ EstimatedDaysMax if both specified.
    /// </summary>
    public int? EstimatedDaysMin { get; set; }

    /// <summary>
    /// Maximum estimated delivery days for this method.
    /// Displayed to customers: "Delivery in 5-7 business days".
    /// Must be ≥ EstimatedDaysMin if both specified.
    /// </summary>
    public int? EstimatedDaysMax { get; set; }

    /// <summary>
    /// Maximum weight (in lbs/kg, see documentation) eligible for standard cost.
    /// Orders exceeding this weight incur 1.5x surcharge (e.g., $5.99 → $8.99).
    /// Null = no weight limit, no surcharge applies.
    /// </summary>
    public decimal? MaxWeight { get; set; }

    /// <summary>
    /// Customer-visible configuration data (JSON serializable).
    /// Example: campaign code, minimum order amount, regional restrictions.
    /// Managed by IHasMetadata concern.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Internal configuration data (not exposed to customers).
    /// Example: carrier API keys, webhook tokens, internal tracking codes.
    /// Managed by IHasMetadata concern.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();

    /// <summary>
    /// Controls where this shipping method appears: FrontEnd (customer-facing), BackEnd (internal),
    /// Both, or None (hidden). Managed by IHasDisplayOn concern.
    /// </summary>
    public DisplayOn DisplayOn { get; set; } = DisplayOn.Both;
    #endregion

    #region Relationships
    public ICollection<StoreShippingMethod> StoreShippingMethods { get; set; } = new List<StoreShippingMethod>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    #endregion

    #region Computed Properties
    public bool IsFreeShipping => Type == ShippingType.FreeShipping || BaseCost == 0;
    public bool IsExpressShipping => Type == ShippingType.Express || Type == ShippingType.Overnight;
    public string EstimatedDelivery => EstimatedDaysMin.HasValue && EstimatedDaysMax.HasValue
        ? $"{EstimatedDaysMin}-{EstimatedDaysMax} days"
        : "Standard delivery";
    #endregion

    #region Constructors
    private ShippingMethod() { }
    #endregion

    #region Factory
    /// <summary>
    /// Creates a new shipping method with validation and initialization.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Pre-Conditions:</strong>
    /// <list type="bullet">
    /// <item><description>name must not be null or whitespace</description></item>
    /// <item><description>presentation must not be null or whitespace</description></item>
    /// <item><description>type must be a valid ShippingType enum value</description></item>
    /// <item><description>baseCost must be non-negative (0 for free shipping)</description></item>
    /// <item><description>estimatedDaysMin ≤ estimatedDaysMax if both specified</description></item>
    /// <item><description>position must be non-negative (0 = highest priority)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Post-Conditions:</strong>
    /// <list type="bullet">
    /// <item><description>Returns new ShippingMethod instance with all properties initialized</description></item>
    /// <item><description>CreatedAt set to UTC now</description></item>
    /// <item><description>Active defaults to true (can be overridden)</description></item>
    /// <item><description>Name and Presentation are trimmed</description></item>
    /// <item><description>Domain event (Events.Created) added for publish after SaveChanges</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Raises Events.Created event (published after SaveChangesAsync in DbContext)</description></item>
    /// <item><description>Sets Id to new Guid.NewGuid()</description></item>
    /// <item><description>Initializes empty metadata dictionaries if not provided</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public static ErrorOr<ShippingMethod> Create(
        string name,
        string presentation,
        ShippingType type,
        decimal baseCost,
        string? description = null,
        bool active = true,
        int? estimatedDaysMin = null,
        int? estimatedDaysMax = null,
        int position = 0,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null,
        DisplayOn displayOn = DisplayOn.Both)
    {
        List<Error> errors = new();

        if (string.IsNullOrWhiteSpace(name))
        {
            errors.Add(Errors.NameRequired);
        }
        else if (name.Length > Constraints.NameMaxLength)
        {
            errors.Add(Errors.NameTooLong);
        }

        if (string.IsNullOrWhiteSpace(presentation))
        {
            errors.Add(Errors.PresentationRequired);
        }

        if (baseCost < 0)
        {
            errors.Add(Errors.BaseCostNegative);
        }

        if (estimatedDaysMin.HasValue && estimatedDaysMax.HasValue && estimatedDaysMin > estimatedDaysMax)
        {
            errors.Add(Errors.EstimatedDaysRangeInvalid);
        }
        else if (estimatedDaysMin < 0 || estimatedDaysMax < 0)
        {
            errors.Add(Errors.EstimatedDaysRangeInvalid); // Also for negative values
        }

        // Enum validation
        if (!Enum.IsDefined(typeof(ShippingType), type))
        {
            errors.Add(Errors.InvalidType);
        }

        if (position < 0)
        {
            errors.Add(Errors.PositionNegative);
        }

        if (errors.Any())
        {
            return errors;
        }

        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);
        var shippingMethod = new ShippingMethod
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Presentation = presentation.Trim(),
            Description = description?.Trim(),
            Type = type,
            BaseCost = baseCost,
            Active = active,
            EstimatedDaysMin = estimatedDaysMin,
            EstimatedDaysMax = estimatedDaysMax,
            Position = position,
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
            CreatedAt = DateTimeOffset.UtcNow,
            DisplayOn = displayOn
        };

        shippingMethod.AddDomainEvent(domainEvent: new Events.Created(ShippingMethodId: shippingMethod.Id, Name: shippingMethod.Name));
        return shippingMethod;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates shipping method configuration with partial changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Behavior:</strong>
    /// Only provided (non-null) parameters are updated. Null parameters are ignored, allowing
    /// selective updates. The method returns self for method chaining patterns.
    /// </para>
    /// <para>
    /// <strong>Parameters Updated (if provided):</strong>
    /// <list type="bullet">
    /// <item><description>name - Human-readable identifier (must be unique)</description></item>
    /// <item><description>presentation - Customer-facing display name</description></item>
    /// <item><description>description - Detailed description shown at checkout</description></item>
    /// <item><description>baseCost - Standard shipping cost (before surcharges)</description></item>
    /// <item><description>active - Enable/disable method availability</description></item>
    /// <item><description>estimatedDaysMin - Minimum delivery estimate</description></item>
    /// <item><description>estimatedDaysMax - Maximum delivery estimate</description></item>
    /// <item><description>maxWeight - Weight threshold for surcharge application</description></item>
    /// <item><description>position - Display order in UI lists (0 = highest priority)</description></item>
    /// <item><description>publicMetadata - Customer-visible configuration (merged, not replaced)</description></item>
    /// <item><description>privateMetadata - Internal configuration (merged, not replaced)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Sets UpdatedAt to UTC now if any property changed</description></item>
    /// <item><description>Raises Events.Updated event only if changes detected</description></item>
    /// <item><description>Returns self for fluent API method chaining</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Update base cost and delivery estimate:</para>
    /// <code>
    /// var method = await _dbContext.ShippingMethods.FindAsync(methodId);
    /// var result = await method.Update(
    ///     baseCost: 6.99m,
    ///     estimatedDaysMin: 4,
    ///     estimatedDaysMax: 6);
    /// 
    /// // Change is persisted after SaveChangesAsync
    /// await _dbContext.SaveChangesAsync();
    /// </code>
    /// </example>
    public ErrorOr<Updated> Update(
        string? name = null,
        string? presentation = null,
        string? description = null,
        decimal? baseCost = null,
        bool? active = null,
        int? estimatedDaysMin = null,
        int? estimatedDaysMax = null,
        decimal? maxWeight = null,
        int? position = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)

    {
        List<Error> errors = new();

        // Perform validation checks on raw input if provided
        if (name is not null)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                errors.Add(Errors.NameRequired);
            }
            else if (name.Length > Constraints.NameMaxLength)
            {
                errors.Add(Errors.NameTooLong);
            }
        }

        if (presentation is not null)
        {
            if (string.IsNullOrWhiteSpace(presentation))
            {
                errors.Add(Errors.PresentationRequired);
            }
        }

        if (baseCost.HasValue && baseCost < 0)
        {
            errors.Add(Errors.BaseCostNegative);
        }

        if (estimatedDaysMin.HasValue && estimatedDaysMax.HasValue && estimatedDaysMin > estimatedDaysMax)
        {
            errors.Add(Errors.EstimatedDaysRangeInvalid);
        }
        else if ((estimatedDaysMin.HasValue && estimatedDaysMin < 0) || (estimatedDaysMax.HasValue && estimatedDaysMax < 0))
        {
            errors.Add(Errors.EstimatedDaysRangeInvalid); // Also for negative values
        }

        if (maxWeight.HasValue && maxWeight < 0)
        {
            errors.Add(CommonInput.Errors.InvalidRange(prefix: nameof(ShippingMethod), field: nameof(MaxWeight), min: 0m));
        }

        if (position.HasValue && position < 0)
        {
            errors.Add(Errors.PositionNegative);
        }

        if (errors.Any())
        {
            return errors;
        }

        bool changed = false;

        string currentName = Name;
        string currentPresentation = Presentation;

        // Normalize name and presentation only if they are actually provided
        if (name is not null)
        {
            (name, _) = HasParameterizableName.NormalizeParams(name: name, presentation: string.Empty);
            if (!string.IsNullOrEmpty(name) && name != currentName) { Name = name.Trim(); changed = true; }
        }

        if (presentation is not null)
        {
            (_, presentation) = HasParameterizableName.NormalizeParams(name: string.Empty, presentation: presentation);
            if (!string.IsNullOrEmpty(presentation) && presentation != currentPresentation) { Presentation = presentation.Trim(); changed = true; }
        }

        if (description != null && description != Description) { Description = description.Trim(); changed = true; }
        if (baseCost.HasValue && baseCost != BaseCost) { BaseCost = baseCost.Value; changed = true; }
        if (active.HasValue && active != Active) { Active = active.Value; changed = true; }
        if (estimatedDaysMin.HasValue && estimatedDaysMin != EstimatedDaysMin) { EstimatedDaysMin = estimatedDaysMin; changed = true; }
        if (estimatedDaysMax.HasValue && estimatedDaysMax != EstimatedDaysMax) { EstimatedDaysMax = estimatedDaysMax; changed = true; }
        if (maxWeight.HasValue && maxWeight != MaxWeight) { MaxWeight = maxWeight.Value; changed = true; }
        if (position.HasValue && position != Position) { Position = position.Value; changed = true; }
        if (publicMetadata != null && !PublicMetadata.MetadataEquals(dict2: publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
            changed = true;
        }

        if (privateMetadata != null && !PrivateMetadata.MetadataEquals(dict2: privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.Updated(ShippingMethodId: Id));
        }

        return Result.Updated;
    }

    /// <summary>
    /// Calculates the actual shipping cost for an order based on weight and free shipping eligibility.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Pricing Logic:</strong>
    /// <list type="bullet">
    /// <item><description>Free Shipping: Returns 0 if Type is FreeShipping or BaseCost is 0</description></item>
    /// <item><description>Weight Surcharge: If MaxWeight defined and order exceeds limit, applies 1.5x multiplier</description></item>
    /// <item><description>Standard: Returns BaseCost otherwise</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Parameters:</strong>
    /// <list type="bullet">
    /// <item><description>orderWeight - Total weight of all items in the order (units depend on configuration)</description></item>
    /// <item><description>orderTotal - Total order value (used for future tier-based pricing extensions)</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Examples:</strong>
    /// <list type="bullet">
    /// <item><description>Free shipping: Returns 0m</description></item>
    /// <item><description>Standard ($5.99), weight 15 lbs, max 20 lbs: Returns $5.99</description></item>
    /// <item><description>Standard ($5.99), weight 25 lbs, max 20 lbs: Returns $8.99 (1.5x surcharge)</description></item>
    /// <item><description>Express ($12.99), no weight limit: Returns $12.99</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <para>Calculate cost for overweight package:</para>
    /// <code>
    /// var method = await _dbContext.ShippingMethods
    ///     .FirstAsync(m => m.Name == "Ground Shipping");
    /// 
    /// // Order: 25 lbs (exceeds 20 lb limit)
    /// var cost = method.CalculateCost(orderWeight: 25, orderTotal: 150);
    /// // Result: $5.99 * 1.5 = $8.99
    /// </code>
    /// </example>
    public decimal CalculateCost(decimal orderWeight, decimal orderTotal)
    {
        if (IsFreeShipping) return 0;
        if (MaxWeight.HasValue && orderWeight > MaxWeight.Value) return BaseCost * 1.5m; // Overweight surcharge
        return BaseCost;
    }

    /// <summary>
    /// Marks this shipping method as deleted and raises the deletion event.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Important:</strong>
    /// This method should only be called after verifying there are no active dependencies:
    /// <list type="bullet">
    /// <item><description>No active Shipments reference this method</description></item>
    /// <item><description>No active StoreShippingMethod links exist</description></item>
    /// </list>
    /// Pre-deletion checks should be performed in a handler or application service.
    /// </para>
    /// <para>
    /// <strong>Side Effects:</strong>
    /// <list type="bullet">
    /// <item><description>Raises Events.Deleted event (published after SaveChangesAsync)</description></item>
    /// <item><description>Event triggers cascade deletion of StoreShippingMethod entities</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(domainEvent: new Events.Deleted(ShippingMethodId: Id));
        return Result.Deleted;
    }
    #endregion

    #region Events
    /// <summary>
    /// Domain events raised during shipping method lifecycle.
    /// </summary>
    /// <remarks>
    /// Events signal important state changes and enable integration with other bounded contexts
    /// (Orders, Shipments, Stores) through asynchronous event handlers.
    /// </remarks>
    public static class Events
    {
        /// <summary>
        /// Raised when a new shipping method is created and added to the system.
        /// </summary>
        /// <remarks>
        /// <strong>Handlers typically:</strong>
        /// <list type="bullet">
        /// <item><description>Publish to event bus for external systems (payment processors, logistics APIs)</description></item>
        /// <item><description>Update search/cache indices for quick method lookup</description></item>
        /// <item><description>Audit log entry creation</description></item>
        /// </list>
        /// </remarks>
        public sealed record Created(Guid ShippingMethodId, string Name) : DomainEvent;

        /// <summary>
        /// Raised when an existing shipping method's properties are updated.
        /// </summary>
        /// <remarks>
        /// <strong>Handlers typically:</strong>
        /// <list type="bullet">
        /// <item><description>Update cached shipping options for stores</description></item>
        /// <item><description>Notify logistics providers of cost/timing changes</description></item>
        /// <item><description>Refresh customer-facing shipping options</description></item>
        /// <item><description>Update search indices for real-time pricing</description></item>
        /// </list>
        /// </remarks>
        public sealed record Updated(Guid ShippingMethodId) : DomainEvent;

        /// <summary>
        /// Raised when a shipping method is deleted from the system.
        /// </summary>
        /// <remarks>
        /// <strong>Handlers typically:</strong>
        /// <list type="bullet">
        /// <item><description>Remove from customer-facing shipping options</description></item>
        /// <item><description>Notify stores to disable this method (via cascade)</description></item>
        /// <item><description>Archive audit trail</description></item>
        /// <item><description>Inform fulfillment partners of discontinuation</description></item>
        /// </list>
        /// </remarks>
        public sealed record Deleted(Guid ShippingMethodId) : DomainEvent;
    }
    #endregion
}