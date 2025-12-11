using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Products.OptionTypes;

namespace ReSys.Core.Domain.Catalog.OptionTypes;

/// <summary>
/// Represents a product characteristic type (e.g., Color, Size, Material) that differentiates product variants.
/// Option types define which characteristics apply to products and can be filtered on the storefront.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// Option types are the definitions of characteristics that variants can have:
/// <list type="bullet">
/// <item>
/// <term>Option Type Definition</term>
/// <description>Defines a characteristic like "Color" or "Size" that variants can use</description>
/// </item>
/// <item>
/// <term>Option Values</term>
/// <description>Specific choices for an option type (e.g., Red, Blue, Green for Color)</description>
/// </item>
/// <item>
/// <term>Product Association</term>
/// <description>Specifies which option types apply to a product (e.g., T-Shirt uses Color and Size)</description>
/// </item>
/// <item>
/// <term>Variant Combination</term>
/// <description>Variants combine option values to define configurations (Blue + Large)</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Name vs Presentation:</strong>
/// <list type="bullet">
/// <item>
/// <term>Name</term>
/// <description>Internal identifier (e.g., "color", "shirt-size") - normalized and unique</description>
/// </item>
/// <item>
/// <term>Presentation</term>
/// <description>Display name for customers (e.g., "Color", "Shirt Size") - formatted for UI</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Filterable Options:</strong>
/// When Filterable = true, the option type appears as a facet/filter on the storefront:
/// <code>
/// // Storefront filter example:
/// Color (Filterable = true)
///   ☐ Red (50 products)
///   ☐ Blue (35 products)
///   ☐ Green (20 products)
/// 
/// Style (Filterable = false)
///   [not shown as filter]
/// </code>
/// </para>
///
/// <para>
/// <strong>Option Type Reusability:</strong>
/// Option types are shared across products in the catalog:
/// <list type="bullet">
/// <item>
/// <term>Single Option Type</term>
/// <description>Multiple products can use the same option type (e.g., all apparel uses Color)</description>
/// </item>
/// <item>
/// <term>Shared Option Values</term>
/// <description>All products using an option type share the same option values (Red, Blue, etc.)</description>
/// </item>
/// <item>
/// <term>Consistency</term>
/// <description>Ensures consistent sizing/coloring across product catalog</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Position &amp; Ordering:</strong>
/// Position determines the order of options displayed in UI:
/// <code>
/// Product Options (in UI order):
/// 1. Size (Position: 0)
/// 2. Color (Position: 1)
/// 3. Style (Position: 2)
/// </code>
/// </para>
///
/// <para>
/// <strong>Typical Usage:</strong>
/// <code>
/// // 1. Create option type
/// var colorOption = OptionType.Create(
///     name: "color",
///     presentation: "Color",
///     position: 0,
///     filterable: true);
/// 
/// // 2. Add option values
/// colorOption.AddValue(OptionValue.Create(name: "red", presentation: "Red"));
/// colorOption.AddValue(OptionValue.Create(name: "blue", presentation: "Blue"));
/// 
/// // 3. Associate with product
/// product.AddOptionType(colorOption);
/// 
/// // 4. Create variants with option combinations
/// var blueVariant = product.AddVariant(sku: "TS-BLU");
/// blueVariant.AddOptionValue(colorOption.OptionValues.First(v => v.Name == "blue"));
/// </code>
/// </para>
/// </remarks>
public sealed class OptionType : AuditableEntity,
    IHasParameterizableName,
    IHasUniqueName,
    IHasPosition,
    IHasMetadata
{
    #region Errors
    /// <summary>
    /// Defines error scenarios specific to OptionType operations.
    /// </summary>
    /// <remarks>
    /// These errors represent validation failures and state conflicts when managing option types.
    /// </remarks>
    public static class Errors
    {
        /// <summary>
        /// Triggered when option type name is missing or empty.
        /// Name is required for identifying the option type in the system.
        /// </summary>
        public static Error Required => 
            CommonInput.Errors.Required(prefix: nameof(OptionType));
        
        /// <summary>
        /// Triggered when option type cannot be found by ID.
        /// </summary>
        public static Error NotFound(Guid id) => 
            CommonInput.Errors.NotFound(prefix: nameof(OptionType));
        
        /// <summary>
        /// Triggered when attempting to delete an option type that has existing option values.
        /// Option values must be deleted first, or option type should be archived instead of deleted.
        /// </summary>
        public static Error HasValues =>
            Error.Conflict(code: "OptionType.HasValues", description: "Cannot delete option type with existing values.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Internal name for the option type (e.g., "color", "shirt-size").
    /// Used for system identification and API references.
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Display name for customers (e.g., "Color", "Shirt Size").
    /// Used in storefront UI and customer communications.
    /// </summary>
    public string Presentation { get; set; } = string.Empty;
    
    /// <summary>
    /// Ordering position for this option type when displayed with other options.
    /// Lower values appear first in UI listings.
    /// </summary>
    public int Position { get; set; }
    
    /// <summary>
    /// Whether this option type should appear as a faceted filter on the storefront.
    /// When true, customers can filter products by this option's values.
    /// </summary>
    public bool Filterable { get; set; }
    
    /// <summary>
    /// Public metadata associated with the option type (visible to customers).
    /// Can store custom attributes like color hex codes, size conversion tables, etc.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; }
    
    /// <summary>
    /// Private metadata associated with the option type (internal only).
    /// Can store system or business-specific data not shown to customers.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Collection of option values that belong to this option type.
    /// Example: Red, Blue, Green values for Color option type.
    /// </summary>
    public ICollection<OptionValue> OptionValues { get; set; } = new List<OptionValue>();

    /// <summary>
    /// Collection of product-option type associations indicating which products use this option.
    /// </summary>
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();
    #endregion

    #region Constructors

    private OptionType() { }

    #endregion

    #region Factory

    /// <summary>
    /// Factory method to create a new <see cref="OptionType"/> instance.
    /// Initializes the option type with basic properties and sets up metadata dictionaries.
    /// </summary>
    /// <param name="name">The internal, unique name for the option type (e.g., "color", "size"). Will be normalized.</param>
    /// <param name="presentation">The human-readable display name for customers (e.g., "Color", "Size"). Defaults to <paramref name="name"/> if null.</param>
    /// <param name="position">The ordering position of this option type when displayed. Defaults to 0. Must be non-negative.</param>
    /// <param name="filterable">A flag indicating if this option type should be available as a filter on the storefront. Defaults to false.</param>
    /// <param name="publicMetadata">Optional dictionary for public-facing metadata. Defaults to an empty dictionary if null.</param>
    /// <param name="privateMetadata">Optional dictionary for internal-only metadata. Defaults to an empty dictionary if null.</param>
    /// <returns>
    /// An <see cref="ErrorOr{OptionType}"/> result.
    /// Returns the newly created <see cref="OptionType"/> instance on success.
    /// No explicit error checks are performed here beyond internal normalization, as basic string validation is handled by <see cref="HasParameterizableName.NormalizeParams(string, string?)"/>.
    /// </returns>
    /// <remarks>
    /// This method ensures that the <paramref name="name"/> and <paramref name="presentation"/> are normalized
    /// and that <paramref name="position"/> is non-negative. It also initializes metadata dictionaries
    /// to avoid null reference exceptions.
    /// <para>
    /// A domain event for <see cref="ReSys.Core.Common.Domain.Events.Created"/> (inherited from <see cref="AuditableEntity"/>) is implicitly
    /// handled by the aggregate base class, signifying the creation of this new <see cref="OptionType"/>.
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// // Create an option type for product sizes, making it filterable on the storefront
    /// var sizeOptionTypeResult = OptionType.Create(
    ///     name: "size",
    ///     presentation: "Size",
    ///     position: 0,
    ///     filterable: true,
    ///     publicMetadata: new Dictionary&lt;string, object?&gt; { { "displayType", "dropdown" } });
    /// 
    /// if (sizeOptionTypeResult.IsError)
    /// {
    ///     // Handle validation or creation errors
    ///     Console.WriteLine($"Error creating OptionType: {sizeOptionTypeResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var sizeOptionType = sizeOptionTypeResult.Value;
    ///     Console.WriteLine($"Created OptionType: {sizeOptionType.Name} (Filterable: {sizeOptionType.Filterable})");
    /// }
    /// </code>
    /// </remarks>
    public static ErrorOr<OptionType> Create(
        string name,
        string? presentation = null,
        int position = 0,
        bool filterable = false,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

        var optionType = new OptionType
        {
            Id = Guid.NewGuid(),
            Name = name,
            Presentation = presentation,
            Position = Math.Max(val1: 0, val2: position),
            Filterable = filterable,
            CreatedAt = DateTimeOffset.UtcNow,
            PrivateMetadata = privateMetadata ?? new Dictionary<string, object?>(),
            PublicMetadata = publicMetadata ?? new Dictionary<string, object?>(),
        };

        return optionType;
    }

    #endregion

    #region Business Logic

    public ErrorOr<OptionType> Update(
        string? name = null,
        string? presentation = null,
        int? position = null,
        bool? filterable = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false;

        if (name != null || presentation != null)
        {
            (name, presentation) = HasParameterizableName.NormalizeParams(name: name ?? Name, presentation: presentation ?? Presentation);
            if (!string.IsNullOrEmpty(value: name) && name != Name)
            {
                Name = name;
                changed = true;
            }

            if (presentation != Presentation)
            {
                Presentation = presentation;
                changed = true;
            }
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

        if (position.HasValue && position != Position)
        {
            Position = Math.Max(val1: 0, val2: position.Value);
            changed = true;
        }

        if (filterable.HasValue && filterable != Filterable)
        {
            Filterable = filterable.Value;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            // Potentially add a specific domain event for OptionType updated,
            // or rely on the aggregate's base updated timestamp.
        }

        return this;
    }

    /// <summary>
    /// Deletes the <see cref="OptionType"/> from the system.
    /// This operation is only permitted if there are no associated <see cref="OptionValue"/>s.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful deletion.
    /// Returns <see cref="Errors.HasValues"/> if the option type still has associated values.
    /// </returns>
    /// <remarks>
    /// Before calling this method, ensure all <see cref="OptionValue"/>s belonging to this
    /// option type have been removed. Alternatively, consider deactivating or archiving the
    /// option type instead of permanently deleting it if historical data is important.
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        if (OptionValues.Any()) return Errors.HasValues;
        return Result.Deleted;
    }

    /// <summary>
    /// Adds an existing <see cref="OptionValue"/> to this <see cref="OptionType"/>.
    /// </summary>
    /// <param name="optionValue">The <see cref="OptionValue"/> instance to add.</param>
    /// <returns>
    /// An <see cref="ErrorOr{OptionValue}"/> result.
    /// Returns the added <see cref="OptionValue"/> on success.
    /// Potential errors (e.g., duplicate value name) are typically handled in the <see cref="OptionValue"/>
    /// creation or through unique constraints enforced by the persistence layer.
    /// </returns>
    /// <remarks>
    /// This method manages the collection of <see cref="OptionValue"/>s directly owned by this aggregate.
    /// It's important that the <see cref="OptionValue"/>'s <c>OptionTypeId</c> matches this <see cref="OptionType"/>'s <c>Id</c>.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var colorOptionType = OptionType.Create(...).Value;
    /// var redValue = OptionValue.Create(colorOptionType.Id, "red", "Red").Value;
    /// colorOptionType.AddOptionValue(redValue);
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<OptionValue> AddOptionValue(OptionValue optionValue)
    {
        OptionValues.Add(item: optionValue);
        return optionValue;
    }

    /// <summary>
    /// Removes an <see cref="OptionValue"/> from this <see cref="OptionType"/> by its ID.
    /// </summary>
    /// <param name="optionValueId">The unique identifier of the <see cref="OptionValue"/> to remove.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful removal.
    /// Returns <see cref="OptionValue.Errors.NotFound(Guid)"/> if the specified <see cref="OptionValue"/> is not found within this <see cref="OptionType"/>'s collection.
    /// </returns>
    /// <remarks>
    /// This method ensures that the <see cref="OptionValue"/> is removed from the aggregate's owned collection.
    /// It's a key operation for managing the available choices for an <see cref="OptionType"/>.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var colorOptionType = GetExistingOptionType(); // Assume this retrieves an OptionType
    /// var blueValueId = colorOptionType.OptionValues.First(v => v.Name == "blue").Id;
    /// colorOptionType.RemoveOptionValue(blueValueId);
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<Deleted> RemoveOptionValue(Guid optionValueId)
    {
        var optionValue = OptionValues.FirstOrDefault(predicate: x => x.Id == optionValueId);
        if (optionValue is null) return OptionValue.Errors.NotFound(id: optionValueId);

        OptionValues.Remove(item: optionValue);
        return Result.Deleted;
    }

    #endregion
}
