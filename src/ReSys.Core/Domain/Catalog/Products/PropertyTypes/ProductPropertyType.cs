using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.PropertyTypes;

namespace ReSys.Core.Domain.Catalog.Products.PropertyTypes;

/// <summary>
/// Represents the explicit association of a <see cref="Product"/> with a <see cref="Product"/>.
/// This entity stores the specific value that a generic property takes for a particular product,
/// along with its display position and a URL-friendly filter parameter.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Product Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>Product-Specific Values</term>
/// <description>Assigns concrete values (e.g., "Cotton") to generic properties (e.g., "Material") for a product.</description>
/// </item>
/// <item>
/// <term>Filterable Attributes</term>
/// <description>Enables filtering products based on property values by providing a <c>FilterParam</c>.</description>
/// </item>
/// <item>
/// <term>Display Order</term>
/// <description>Controls the sequence in which product properties are displayed for a product in the UI.</description>
/// </item>
/// <item>
/// <term>Many-to-Many Relationship</term>
/// <description>Facilitates a flexible relationship where products can have multiple properties, and properties can be used by multiple products.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>ProductId</term>
/// <description>The unique identifier of the associated <see cref="PropertyType"/>.</description>
/// </item>
/// <item>
/// <term>PropertyId</term>
/// <description>The unique identifier of the associated <see cref="PropertyType"/>.</description>
/// </item>
/// <item>
/// <term>Value</term>
/// <description>The specific value (e.g., "Red", "100g") for the property.</description>
/// </item>
/// <item>
/// <term>Position</term>
/// <description>The display order of the property for the product.</description>
/// </item>
/// <item>
/// <term>FilterParam</term>
/// <description>A URL-friendly string for filtering based on this property's value.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasPosition</strong> - For managing display order.</item>
/// <item><strong>IHasFilterParam</strong> - For generating URL-friendly filter keys.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProductPropertyType :
    AuditableEntity,
    IHasPosition
{
    #region Constraints

    /// <summary>
    /// Defines constraints and limits for <see cref="ProductPropertyType"/> operations and validation.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum allowed length for the <see cref="ProductPropertyType.PropertyTypeValue"/> of the product property.
        /// This constraint is aligned with <see cref="CommonInput.Constraints.Text.LongTextMaxLength"/>.
        /// </summary>
        public const int MaxValueLength = CommonInput.Constraints.Text.LongTextMaxLength;

        /// <summary>
        /// Maximum allowed value for the <c>Position</c> of the product property.
        /// </summary>
        public const int MaxPosition = int.MaxValue;
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="ErrorOr"/> operations.
    /// These errors are returned via the <see cref="ProductPropertyType"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested <see cref="id"/> could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="ProductPropertyType"/> that was not found.</param>
        public static Error NotFound(Guid id) => CommonInput.Errors.NotFound(prefix: nameof(ProductPropertyType), field: id.ToString());

        /// <summary>
        /// Occurs when an unexpected error happens during a <see cref="operation"/> operation.
        /// </summary>
        /// <param name="operation">A descriptive string indicating the operation during which the error occurred.</param>
        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: $"ProductProperty.Unexpected.{operation}",
            description: $"An unexpected error occurred during {operation} operation");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the unique identifier of the associated <see cref="Product"/>.
    /// </summary>
    public Guid ProductId { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier of the associated generic <see cref="PropertyType"/>.
    /// </summary>
    public Guid PropertyTypeId { get; set; }
    /// <summary>
    /// Gets or sets the display order of this property within the product's property list.
    /// Lower values typically appear first.
    /// </summary>
    public int Position { get; set; }

    /// <summary>
    /// Gets or sets the specific value assigned to this property for the product.
    /// The value is trimmed on set to remove leading/trailing whitespace.
    /// </summary>
    public string? PropertyTypeValue { get; set; }
    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Product"/>.
    /// </summary>
    public Product Product { get; set; } = null!;
    /// <summary>
    /// Gets or sets the navigation property to the associated generic <see cref="PropertyType"/>.
    /// </summary>
    public PropertyType PropertyType { get; set; } = null!;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Indicates whether the associated generic <see cref="Catalog.PropertyTypes.PropertyType.Filterable"/> is configured to be filterable.
    /// This property delegates to the <see cref="Catalog.PropertyTypes.PropertyType"/> property of the associated <see cref="PropertyType"/>.
    /// </summary>
    public bool IsFilterable => PropertyType.Filterable;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="productId"/> instance.
    /// Establishes the link between a product and a generic property, assigning its specific value.
    /// </summary>
    /// <param name="productId">The unique identifier of the <see cref="productId"/>.</param>
    /// <param name="propertyId">The unique identifier of the <see cref="propertyId"/>.</param>
    /// <param name="value">The unique identifier of the generic <see cref="position"/>.</param>
    /// <param name="position"></param>
    /// <param name="filterable">The specific value to assign to the property for this product.</param>
    /// <param name="value">A flag indicating if the parent <see cref="ErrorOr{TValue}"/> is filterable. Used for generating <c>FilterParam</c>.</param>
    /// <param name="value">An optional explicit URL-friendly filter key. If not provided and the property is filterable, it will be generated from <paramref name="filterParam"/>.</param>
    /// <returns>
    /// An <see cref="productId"/> result.
    /// Returns the newly created <see cref="propertyId"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures that <c>Position</c> is non-negative and within defined limits.
    /// It automatically generates the <c>FilterParam</c> from the <paramref name="value"/> if the property is filterable
    /// and no explicit <paramref name="filterParam"/> is provided.
    /// <para>
    /// Basic validation for <paramref name="productId"/>, <paramref name="propertyId"/>, and <paramref name="value"/>
    /// is typically handled at a higher level (e.g., in <see cref="ProductPropertyType"/>).
    /// </para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// Guid productId = Guid.NewGuid(); // Assume existing Product ID
    /// Guid materialPropertyId = Guid.NewGuid(); // Assume existing Property ID for "Material"
    /// var productPropertyResult = ProductProperty.Create(
    ///     productId: productId,
    ///     propertyId: materialPropertyId,
    ///     value: "Organic Cotton",
    ///     position: 1,
    ///     filterable: true); // Assuming the "Material" property is filterable
    /// 
    /// if (productPropertyResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating ProductProperty: {productPropertyResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newProductProperty = productPropertyResult.Value;
    ///     Console.WriteLine($"ProductProperty created for Product {productId} with Material: {newProductProperty.Value}.");
    /// }
    /// </code>
    /// </remarks>
    public static ErrorOr<ProductPropertyType> Create(
        Guid productId,
        Guid propertyId,
        string value,
        int position = 0)
    {
        var productProperty = new ProductPropertyType
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            PropertyTypeId = propertyId,
            PropertyTypeValue = value,
            Position = Math.Max(val1: 0, val2: Math.Min(val1: position, val2: Constraints.MaxPosition)),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return productProperty;
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Updates the mutable attributes of the <see cref="value"/>.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="value"></param>
    /// <param name="position">The new value to assign to this property for the product. If null, the existing value is retained.</param>
    /// <returns>
    /// An <see cref="PropertyTypeValue"/> result.
    /// Returns the updated <see cref="PropertyTypeValue"/> instance on success.
    /// Returns an error if validation fails (e.g., value too long, invalid filter param format).
    /// </returns>
    /// <remarks>
    /// If the <see cref="ProductPropertyType"/> is changed and the associated <see cref="ProductPropertyType"/> is filterable,
    /// The <c>UpdatedAt</c> timestamp is automatically updated if any changes occur (handled by base class).
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var productProperty = GetExistingProductProperty(); // Assume existing ProductProperty
    /// var updateResult = productProperty.Update(value: "Organic Cotton Blend", position: 2);
    /// 
    /// if (updateResult.IsError)
    /// {
    ///     Console.WriteLine($"Error updating ProductProperty: {updateResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"ProductProperty updated to value: {productProperty.Value}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<ProductPropertyType> Update(
        string? value = null,
        int? position = null)
    {
        bool changed = false;

        // Update value
        if (value is not null && value != PropertyTypeValue)
        {
            PropertyTypeValue = value;
            changed = true;
        }

        // Update position
        if (position.HasValue && position != Position)
        {
            Position = Math.Max(val1: 0, val2: Math.Min(val1: position.Value, val2: Constraints.MaxPosition));
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow; // Handled by base class/ORM
            // A specific domain event for ProductProperty updated could be added here if needed.
        }

        return this;
    }

    /// <summary>
    /// Marks the <see cref="Product"/> for logical deletion.
    /// In this context, deletion means signaling that the association between the product and the property should be removed.
    /// The actual removal from the parent aggregate's collection is handled by the <see cref="ErrorOr{TValue}"/> aggregate.
    /// </summary>
    /// <returns>
    /// An <see cref="Result.Deleted"/> result.
    /// Always returns <see cref="Result"/>, as the removal from the parent collection
    /// is managed by the <see cref="Product"/> aggregate.
    /// </returns>
    /// <remarks>
    /// This method signals that the product property should no longer be associated with its parent <see cref="Products.Product.RemoveProperty"/>.
    /// The <see cref="Products.Product"/> method should be used to initiate the removal from the product's collection.
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        // This method primarily acts as a placeholder or to fulfill an interface.
        // The actual removal from the Product's collection is handled by Product.RemoveProperty.
        return Result.Deleted;
    }
    #endregion
}