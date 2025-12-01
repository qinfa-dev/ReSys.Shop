using System.Text.RegularExpressions;

using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.Products.Properties;

namespace ReSys.Core.Domain.Catalog.Properties;

/// <summary>
/// Represents a reusable product property or attribute (e.g., Color, Size, Material, Brand, Weight).
/// Properties are shared attributes that can be assigned to multiple products via ProductProperty join entity.
/// Each property has metadata, filtering capability, and multi-channel display control.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// Properties enable flexible product attribute management:
/// <list type="bullet">
/// <item>
/// <term>Reusable Attributes</term>
/// <description>Single Property (e.g., "Color") used across many products</description>
/// </item>
/// <item>
/// <term>Flexible Values</term>
/// <description>Each product assigns its own values to the property ("Red", "Blue", etc.)</description>
/// </item>
/// <item>
/// <term>Storefront Filtering</term>
/// <description>Filterable properties appear in storefront filter sidebar</description>
/// </item>
/// <item>
/// <term>Multi-Channel Display</term>
/// <description>Control visibility on frontend, backend, both, or neither</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Property Kinds (Data Types):</strong>
/// <list type="table">
/// <item>
/// <term>ShortText</term>
/// <description>Single-line text (max 255 chars) - "Brand", "Model"</description>
/// </item>
/// <item>
/// <term>Medium</term>
/// <description>Multi-line text (max 500 chars) - "Description", "Notes"</description>
/// </item>
/// <item>
/// <term>LongText</term>
/// <description>Large text (max 1000 chars) - "Specifications", "Usage Instructions"</description>
/// </item>
/// <item>
/// <term>Number</term>
/// <description>Integer values - "Weight (g)", "Capacity (ml)", "Age Recommendation"</description>
/// </item>
/// <item>
/// <term>RichText</term>
/// <description>Formatted HTML/Markdown - "Care Instructions", "Return Policy"</description>
/// </item>
/// <item>
/// <term>Boolean</term>
/// <description>Yes/No flag - "Eco-Friendly", "Waterproof", "Handmade"</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Difference from OptionType:</strong>
/// <list type="bullet">
/// <item>
/// <term>Property</term>
/// <description>Non-affecting attributes (don't differentiate variants). Optional per product.</description>
/// </item>
/// <item>
/// <term>OptionType</term>
/// <description>Variant-defining characteristics (affect variant creation). Required for non-master variants.</description>
/// </item>
/// </list>
/// Example: "Color" can be both a Property (for display) and an OptionType (for variant differentiation).
/// </para>
///
/// <para>
/// <strong>Filtering Strategy:</strong>
/// When Filterable=true, property becomes available in storefront sidebar:
/// <code>
/// Property: "Size"
/// Filterable: true
/// FilterParam: "size"  (auto-generated from name)
/// 
/// Storefront Filter Display:
/// [x] Small   (product count: 45)
/// [x] Medium  (product count: 87)
/// [x] Large   (product count: 62)
/// </code>
/// </para>
///
/// <para>
/// <strong>Multi-Channel Display:</strong>
/// <code>
/// DisplayOn.Both:     Show on frontend AND admin panel
/// DisplayOn.FrontEnd: Show only on storefront (customers see it)
/// DisplayOn.BackEnd:  Show only in admin panel (internal use)
/// DisplayOn.None:     Hidden everywhere (archived/inactive)
/// </code>
/// </para>
///
/// <para>
/// <strong>Typical Usage Example:</strong>
/// <code>
/// // 1. Create property
/// var color = Property.Create(
///     name: "color",
///     presentation: "Color",
///     kind: PropertyKind.ShortText,
///     filterable: true,
///     displayOn: DisplayOn.Both,
///     position: 10);
/// 
/// // 2. Assign to product
/// product.AddProperty(
///     propertyId: color.Id,
///     value: "Red",
///     position: 1);
/// 
/// // 3. Storefront filter includes this property
/// // Filter sidebar now shows "Color" with available values
/// </code>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasParameterizableName</strong> - Name + Presentation flexibility</item>
/// <item><strong>IHasPosition</strong> - Ordering in product details display</item>
/// <item><strong>IHasMetadata</strong> - PublicMetadata and PrivateMetadata</item>
/// <item><strong>IHasDisplayOn</strong> - Multi-channel visibility control</item>
/// <item><strong>IHasFilterParam</strong> - URL-friendly filter key</item>
/// <item><strong>IHasUniqueName</strong> - Unique property names</item>
/// </list>
/// </para>
/// </remarks>
public sealed class Property :
    Aggregate,
    IHasParameterizableName,
    IHasPosition,
    IHasMetadata,
    IHasDisplayOn,
    IHasFilterParam,
    IHasUniqueName
{
    #region Constraints
    /// <summary>
    /// Defines constraints and limits for property operations and validation.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for FilterParam (URL-friendly filter key).
        /// Matches slug constraints for consistency with URL encoding.
        /// </summary>
        public const int FilterParamMaxLength = CommonInput.Constraints.SlugsAndVersions.SlugMaxLength;
        
        /// <summary>
        /// Regex pattern for FilterParam validation (lowercase, hyphens, numbers only).
        /// Ensures safe URL generation without special characters.
        /// </summary>
        public const string FilterParamRegexPattern = CommonInput.Constraints.SlugsAndVersions.SlugPattern;
        
        /// <summary>
        /// Maximum length for RichText property values (1000 characters).
        /// Accommodates formatted content like HTML or Markdown.
        /// </summary>
        public const int RichTextMaxLength = CommonInput.Constraints.Text.LongTextMaxLength;
    }

    /// <summary>
    /// Property data type enumeration. Determines storage, validation, and UI rendering.
    /// </summary>
    public enum PropertyKind
    {
        /// <summary>Single-line text, max 255 characters. Used for: Brand, Model, SKU</summary>
        ShortText,
        
        /// <summary>Multi-line text, max 500 characters. Used for: Description, Notes, Comments</summary>
        Medium,
        
        /// <summary>Large text, max 1000 characters. Used for: Specifications, Instructions, Details</summary>
        LongText,
        
        /// <summary>Integer values. Used for: Weight, Dimensions, Capacity, Age, Quantity</summary>
        Number,
        
        /// <summary>Rich formatted text (HTML/Markdown), max 1000 characters. Used for: Care Instructions, Warnings</summary>
        RichText,
        
        /// <summary>Boolean yes/no flag. Used for: Eco-Friendly, Waterproof, Handmade, Certified</summary>
        Boolean
    }

    #endregion

    #region Errors
    /// <summary>
    /// Domain error definitions for property operations.
    /// Returned via ErrorOr pattern for railway-oriented error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Occurs when referenced property cannot be found in database.
        /// Typical causes: ID doesn't exist, property was deleted, query error.
        /// </summary>
        public static Error NotFound(Guid id) => Error.NotFound(
            code: "Property.NotFound",
            description: $"Property with ID '{id}' was not found.");

        /// <summary>
        /// Occurs when attempting to delete a property that has associated ProductProperties.
        /// Prevention: Cannot orphan product-property assignments.
        /// Resolution: Remove all ProductProperty associations first, then delete property.
        /// </summary>
        public static Error HasProductProperties => Error.Validation(
            code: "Property.HasProductProperties",
            description: "Cannot delete property with associated product properties. Remove product properties first.");

        /// <summary>
        /// Occurs when an unexpected error happens during property operation.
        /// Used for logging and debugging framework-level issues (not domain validation).
        /// </summary>
        public static Error UnexpectedError(string operation, Exception? ex = null) => Error.Unexpected(
            code: $"Property.Unexpected.{operation}",
            description: ex?.Message ?? "An unexpected error occurred during property operation.");
    }

    #endregion

    #region Core Properties
    /// <summary>
    /// Internal system name. Normalized slug-like format (lowercase, hyphens).
    /// Used for identification and code reference. Must be unique within store.
    /// Examples: "color", "size", "material", "eco-friendly", "weight-in-grams"
    /// </summary>
    public string Name { get; set; } = null!;

    /// <summary>
    /// Human-readable display name shown to customers and admins.
    /// Can differ from Name for better UX and localization.
    /// Example: Name="eco-friendly", Presentation="Eco-Friendly ✓"
    /// </summary>
    public string Presentation { get; set; } = null!;

    /// <summary>
    /// URL-friendly filter key for storefront filtering system.
    /// Auto-generated from Name when Filterable=true.
    /// Used in filter URLs: /products?filters=size-value&amp;color-value
    /// Example: name="color" → FilterParam="color"
    /// </summary>
    public string? FilterParam { get; set; }
    
    /// <summary>
    /// Data type for this property (ShortText, LongText, Number, Boolean, RichText, etc.).
    /// Determines storage limits, validation rules, and UI input field type.
    /// Default: ShortText
    /// </summary>
    public PropertyKind Kind { get; set; } = PropertyKind.ShortText;
    
    /// <summary>
    /// When true, property appears in storefront filter sidebar for product filtering.
    /// Only filterable properties generate FilterParam and participate in filter faceting.
    /// Example: Size=true (appears as filter), Brand=false (not filterable)
    /// </summary>
    public bool Filterable { get; set; }
    
    /// <summary>
    /// Multi-channel visibility control.
    /// DisplayOn.Both: Show everywhere (frontend + admin)
    /// DisplayOn.FrontEnd: Show only on storefront
    /// DisplayOn.BackEnd: Show only in admin panel
    /// DisplayOn.None: Hide everywhere (archived/inactive)
    /// </summary>
    public DisplayOn DisplayOn { get; set; } = DisplayOn.None;
    
    /// <summary>
    /// Display order within product details. Lower values appear first.
    /// Updated by editors to control property appearance order.
    /// Typical range: 0-999 (increments by 10 for easy reordering).
    /// </summary>
    public int Position { get; set; }

    #endregion

    #region Relationships
    /// <summary>
    /// Collection of ProductProperty join entities linking this property to products.
    /// Each ProductProperty represents the property assignment to a specific product.
    /// Example: Property "Color" has ProductProperties for "Red Shirt", "Blue Jeans", "Green Hat".
    /// </summary>
    public ICollection<ProductProperty> ProductProperties { get; set; } = new List<ProductProperty>();
    //public IEnumerable<Product> Products => ProductProperties.Select(selector: pp => pp.Product);

    #endregion

    #region Metadata
    /// <summary>
    /// Public metadata: Custom attributes visible/editable in admin UI and exposed via APIs.
    /// Use for: marketing flags, campaign tags, UI hints, custom categorization.
    /// Example: { "tooltip": "Choose the primary color", "appearance_order": "1" }
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Private metadata: Custom attributes visible only to admins and backend systems.
    /// Use for: migration data, integration markers, internal notes, business rules.
    /// Example: { "legacy_id": "prop-456", "import_source": "shopify", "sync_status": "complete" }
    /// Never exposed via public APIs.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();

    #endregion

    #region Constructors
    private Property() { }

    #endregion

    #region Factory
    /// <summary>
    /// Factory method for creating a new Property.
    /// Initializes all constraints and auto-generates FilterParam if filterable.
    /// Raises Created domain event.
    /// </summary>
    /// <remarks>
    /// <strong>Usage Examples:</strong>
    /// <code>
    /// // Create a non-filterable text property
    /// var brand = Property.Create(
    ///     name: "brand",
    ///     presentation: "Brand",
    ///     kind: PropertyKind.ShortText,
    ///     filterable: false,
    ///     displayOn: DisplayOn.Both,
    ///     position: 10);
    /// 
    /// // Create a filterable size property (generates FilterParam automatically)
    /// var size = Property.Create(
    ///     name: "size",
    ///     presentation: "Size",
    ///     kind: PropertyKind.ShortText,
    ///     filterable: true,
    ///     displayOn: DisplayOn.FrontEnd,
    ///     position: 5);
    /// // Result: FilterParam="size" (auto-generated from name)
    /// 
    /// // Create a number property for dimensions
    /// var weight = Property.Create(
    ///     name: "weight-grams",
    ///     presentation: "Weight (g)",
    ///     kind: PropertyKind.Number,
    ///     filterable: false,
    ///     position: 20);
    /// 
    /// // Create a boolean eco-friendly flag
    /// var ecoFriendly = Property.Create(
    ///     name: "eco-friendly",
    ///     presentation: "Eco-Friendly ♻",
    ///     kind: PropertyKind.Boolean,
    ///     displayOn: DisplayOn.Both,
    ///     position: 15);
    /// </code>
    /// </remarks>
    public static ErrorOr<Property> Create(
        string name,
        string presentation,
        PropertyKind? kind = null,
        bool filterable = false,
        DisplayOn? displayOn = null,
        int position = 0,
        string? filterParam = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        var actualKind = kind ?? PropertyKind.ShortText;
        var actualDisplayOn = displayOn ?? DisplayOn.Both;
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var property = new Property
        {
            Name = name,
            Presentation = presentation,
            Kind = actualKind,
            DisplayOn = actualDisplayOn,
            Position = Math.Max(val1: 0, val2: position),
            Filterable = filterable,
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
        };

        if (filterable)
        {
            if (!string.IsNullOrWhiteSpace(value: filterParam))
            {
                property.FilterParam = filterParam.ToSlug();
            }
            else
            {
                property.SetFilterParam(propertyExpression: m => m.Name);
            }
        }

        property.PublicMetadata = publicMetadata ?? new Dictionary<string, object?>();
        property.PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>();

        property.AddDomainEvent(domainEvent: new Events.Created(PropertyId: property.Id));
        return property;
    }

    #endregion

    #region Update
    /// <summary>
    /// Update property attributes. Tracks dependency changes that affect related data.
    /// When name/presentation/kind changes, ProductProperties may need cascade updates.
    /// Emits appropriate domain events for downstream handlers.
    /// </summary>
    /// <remarks>
    /// <strong>Change Tracking Strategy:</strong>
    /// <list type="bullet">
    /// <item>
    /// <term>dependencyChanged (name, presentation, kind, filterable, displayOn, position)</term>
    /// <description>Changes that affect product display or filtering. Triggers TouchAllProducts event.</description>
    /// </item>
    /// <item>
    /// <term>Changed (filterParam, metadata)</term>
    /// <description>Changes that don't require product touching. No cascade event.</description>
    /// </item>
    /// </list>
    /// 
    /// <strong>Domain Events Emitted:</strong>
    /// <list type="bullet">
    /// <item>Updated - Basic property update notification</item>
    /// <item>TouchAllProducts - When dependencyChanged=true (invalidate product caches)</item>
    /// <item>EnsureProductPropertiesHaveFilterParams - When Filterable becomes true</item>
    /// </list>
    /// </remarks>
    public ErrorOr<Property> Update(
        string? name = null,
        string? presentation = null,
        PropertyKind? kind = null,
        bool? filterable = null,
        DisplayOn? displayOn = null,
        int? position = null,
        string? filterParam = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name ?? Name, presentation: presentation ?? Presentation);

        bool changed = false;
        bool dependencyChanged = false;

        if (!string.IsNullOrWhiteSpace(value: name) && name != Name)
        {
            Name = name;
            changed = dependencyChanged = true;
        }

        if (!string.IsNullOrWhiteSpace(value: presentation) && presentation != Presentation)
        {
            Presentation = presentation.Trim();
            changed = dependencyChanged = true;
        }

        if (kind.HasValue && kind.Value != Kind)
        {
            Kind = kind.Value;
            changed = dependencyChanged = true;
        }

        if (filterable.HasValue && filterable.Value != Filterable)
        {
            Filterable = filterable.Value;
            changed = dependencyChanged = true;
        }

        if (displayOn.HasValue && displayOn.Value != DisplayOn)
        {
            DisplayOn = displayOn.Value;
            changed = dependencyChanged = true;
        }

        if (position.HasValue && position.Value != Position)
        {
            Position = Math.Max(val1: 0, val2: position.Value);
            changed = dependencyChanged = true;
        }

        if (filterParam != null && filterParam != FilterParam)
        {
            FilterParam = filterParam.ToSlug();
            changed = true;
        }

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
            AddDomainEvent(domainEvent: new Events.Updated(PropertyId: Id, DependencyChanged: dependencyChanged));

            if (dependencyChanged)
                AddDomainEvent(domainEvent: new Events.TouchAllProducts(PropertyId: Id));

            if (Filterable)
                AddDomainEvent(domainEvent: new Events.EnsureProductPropertiesHaveFilterParams(PropertyId: Id));
        }

        return this;
    }

    #endregion

    #region Delete
    /// <summary>
    /// Delete property. Only succeeds if no ProductProperties are assigned.
    /// Raises Deleted domain event.
    /// </summary>
    /// <returns>Deleted result on success; HasProductProperties error if property is in use.</returns>
    public ErrorOr<Deleted> Delete()
    {
        if (ProductProperties.Any())
            return Errors.HasProductProperties;

        AddDomainEvent(domainEvent: new Events.Deleted(PropertyId: Id));
        return Result.Deleted;
    }

    #endregion

    #region Helpers
    /// <summary>
    /// Returns validation constraints (min/max length, regex) for a specific PropertyKind.
    /// Used by ProductProperty validation to enforce type-specific rules.
    /// </summary>
    /// <param name="kind">The property kind to get constraints for.</param>
    /// <returns>Tuple of (minLength, maxLength, validationRegex). Regex null if no pattern validation.</returns>
    /// <remarks>
    /// Example usage in ProductProperty validator:
    /// <code>
    /// var (minLen, maxLen, regex) = Property.GetValidationConditionForKind(propertyKind);
    /// RuleFor(x => x.Value)
    ///     .Length(minLen, maxLen)
    ///     .Matches(regex)  // if regex != null
    /// </code>
    /// </remarks>
    public static (int minLength, int maxLength, Regex? validationRegex) GetValidationConditionForKind(PropertyKind kind) => kind switch
    {
        PropertyKind.ShortText => (CommonInput.Constraints.Text.MinLength, CommonInput.Constraints.Text.ShortTextMaxLength, null),
        PropertyKind.Medium => (CommonInput.Constraints.Text.MinLength, CommonInput.Constraints.Text.MediumTextMaxLength, null),
        PropertyKind.LongText => (CommonInput.Constraints.Text.MinLength, CommonInput.Constraints.Text.LongTextMaxLength, null),
        PropertyKind.Number => (CommonInput.Constraints.Text.MinLength, CommonInput.Constraints.Text.MaxLength, CommonInput.Constraints.NumericPatterns.IntegerRegex),
        PropertyKind.RichText => (CommonInput.Constraints.Text.MinLength, CommonInput.Constraints.Text.LongTextMaxLength, null),
        PropertyKind.Boolean => (CommonInput.Constraints.Text.MinLength, CommonInput.Constraints.Text.MaxLength, CommonInput.Constraints.Boolean.Regex),
        _ => (CommonInput.Constraints.Text.MinLength, CommonInput.Constraints.Text.MaxLength, null) // Default for unknown or unhandled kinds
    };

    /// <summary>
    /// Returns HTML5 input field type for a specific PropertyKind.
    /// Used by admin UI and API forms to render appropriate input controls.
    /// </summary>
    /// <param name="kind">The property kind to get field type for.</param>
    /// <returns>HTML input element string.</returns>
    /// <remarks>
    /// Example outputs:
    /// ShortText → "&lt;input type=\"text\"&gt;"
    /// Number → "&lt;input type=\"number\"&gt;"
    /// RichText → "&lt;rich-text-editor&gt;"
    /// Boolean → "&lt;input type=\"checkbox\"&gt;"
    /// </remarks>
    public string GetMetaFieldType() => Kind switch
    {
        PropertyKind.ShortText => "<input type=\"text\">",
        PropertyKind.LongText => "<textarea>",
        PropertyKind.Number => "<input type=\"number\">",
        PropertyKind.RichText => "<rich-text-editor>",
        PropertyKind.Boolean => "<input type=\"checkbox\">",
        _ => "<input type=\"text\">"
    };

    #endregion


    #region Events
    /// <summary>
    /// Domain events for property lifecycle and change notifications.
    /// Enables event-driven architecture for cross-domain communication.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Fired when a new property is created.
        /// Purpose: Notify consumers that property is now available for assignment.
        /// </summary>
        public record Created(Guid PropertyId) : DomainEvent;
        
        /// <summary>
        /// Fired when property details are updated.
        /// Purpose: Notify consumers of changes; if DependencyChanged=true, trigger product invalidation.
        /// Parameter: DependencyChanged indicates if name/kind/display changed (affects downstream data).
        /// </summary>
        public record Updated(Guid PropertyId, bool DependencyChanged) : DomainEvent;
        
        /// <summary>
        /// Fired when a property is deleted.
        /// Purpose: Clean up property references, remove from searches/filters.
        /// </summary>
        public record Deleted(Guid PropertyId) : DomainEvent;
        
        /// <summary>
        /// Fired when property changes require re-indexing all related products.
        /// Purpose: Invalidate product caches, re-index search, update product listings.
        /// Scenario: When property Kind or DisplayOn changes.
        /// </summary>
        public record TouchAllProducts(Guid PropertyId) : DomainEvent;
        
        /// <summary>
        /// Fired when a property becomes filterable and existing ProductProperties need FilterParams.
        /// Purpose: Ensure all ProductProperty assignments have proper filter parameter values.
        /// Scenario: When Filterable transitions from false to true.
        /// </summary>
        public record EnsureProductPropertiesHaveFilterParams(Guid PropertyId) : DomainEvent;
    }

    #endregion
}