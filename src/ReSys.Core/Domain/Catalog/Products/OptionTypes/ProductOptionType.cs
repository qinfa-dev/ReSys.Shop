using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.OptionTypes;

namespace ReSys.Core.Domain.Catalog.Products.OptionTypes;

/// <summary>
/// Represents the explicit association of an <see cref="OptionType"/> with a <see cref="Product"/>.
/// This entity defines which product characteristics are applicable to a specific product
/// and manages their display order.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Product Configuration:</strong>
/// <list type="bullet">
/// <item>
/// <term>Product Customization</term>
/// <description>Links a product to available option types (e.g., "Color" and "Size" for a T-shirt).</description>
/// </item>
/// <item>
/// <term>Display Order</term>
/// <description>Determines the sequence in which option types are presented for a product in the UI.</description>
/// </item>
/// <item>
/// <term>Many-to-Many Relationship</term>
/// <description>Facilitates a flexible relationship where products can have multiple option types, and option types can be used by multiple products.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>ProductId</term>
/// <description>The unique identifier of the associated <see cref="Product"/>.</description>
/// </item>
/// <item>
/// <term>OptionTypeId</term>
/// <description>The unique identifier of the associated <see cref="OptionType"/>.</description>
/// </item>
/// <item>
/// <term>Position</term>
/// <description>The display order of the option type for the product.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasPosition</strong> - For managing display order.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProductOptionType : AuditableEntity, IHasPosition
{
    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="ProductOptionType"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a <see cref="ProductOptionType"/> requires a valid ProductId and OptionTypeId.
        /// </summary>
        public static Error Required => CommonInput.Errors.Required(prefix: nameof(ProductOptionType));
        /// <summary>
        /// Error indicating that the <see cref="OptionType"/> is already linked to the specified product.
        /// </summary>
        public static Error AlreadyLinked => Error.Conflict(code: "ProductOptionType.AlreadyLinked", description: "OptionType is already linked to this product.");
        /// <summary>
        /// Error indicating that a requested <see cref="ProductOptionType"/> could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="ProductOptionType"/> that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "ProductOptionType.NotFound", description: $"ProductOptionType with ID '{id}' was not found.");
    }
    #endregion

    #region Core Properties
    /// <summary>
    /// Gets or sets the display order of this <see cref="OptionType"/> when presented for the associated product.
    /// Lower values typically appear first.
    /// </summary>
    public int Position { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the unique identifier of the associated <see cref="Product"/>.
    /// </summary>
    public Guid ProductId { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier of the associated <see cref="OptionType"/>.
    /// </summary>
    public Guid OptionTypeId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Product"/>.
    /// </summary>
    public Product Product { get; set; } = null!;
    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="OptionType"/>.
    /// </summary>
    public OptionType OptionType { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private ProductOptionType() { }
    #endregion

    #region Factory
    /// <summary>
    /// Factory method to create a new <see cref="ProductOptionType"/> instance.
    /// This establishes a link between a product and an option type.
    /// </summary>
    /// <param name="productId">The unique identifier of the <see cref="Product"/>.</param>
    /// <param name="optionTypeId">The unique identifier of the <see cref="OptionType"/>.</param>
    /// <param name="position">The display order for this option type within the product's options. Defaults to 0.</param>
    /// <returns>
    /// An <see cref="ErrorOr{ProductOptionType}"/> result.
    /// Returns the newly created <see cref="ProductOptionType"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures that the <paramref name="position"/> is non-negative.
    /// Basic validation for <paramref name="productId"/> and <paramref name="optionTypeId"/> being empty
    /// is typically handled at a higher level (e.g., in <see cref="Product.AddOptionType(ProductOptionType)"/>).
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// Guid productId = Guid.NewGuid(); // Assume this is an existing Product ID
    /// Guid optionTypeId = Guid.NewGuid(); // Assume this is an existing OptionType ID
    /// var productOptionTypeResult = ProductOptionType.Create(
    ///     productId: productId,
    ///     optionTypeId: optionTypeId,
    ///     position: 1);
    /// 
    /// if (productOptionTypeResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating ProductOptionType: {productOptionTypeResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newProductOptionType = productOptionTypeResult.Value;
    ///     Console.WriteLine($"ProductOptionType created linking Product {productId} to OptionType {optionTypeId}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static ErrorOr<ProductOptionType> Create(Guid productId, Guid optionTypeId, int position = 0)
    {
        return new ProductOptionType
        {
            Id = Guid.NewGuid(),
            ProductId = productId,
            OptionTypeId = optionTypeId,
            Position = Math.Max(val1: 0, val2: position),
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the display position of this <see cref="ProductOptionType"/>.
    /// </summary>
    /// <param name="position">The new non-negative display order for this option type within the product's options.</param>
    /// <returns>
    /// An <see cref="ErrorOr{ProductOptionType}"/> result.
    /// Returns the updated <see cref="ProductOptionType"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method updates the <c>Position</c> and the <c>UpdatedAt</c> timestamp.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var productOptionType = GetExistingProductOptionType(); // Assume this retrieves an instance
    /// var updateResult = productOptionType.UpdatePosition(position: 2);
    /// if (updateResult.IsError)
    /// {
    ///     Console.WriteLine($"Error updating position: {updateResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"ProductOptionType position updated to {productOptionType.Position}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<ProductOptionType> UpdatePosition(int position)
    {
        Position = position;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Marks the <see cref="ProductOptionType"/> for logical deletion.
    /// In this context, deletion typically means signaling that the association between the product and the option type should be removed.
    /// The actual removal from the parent aggregate's collection is handled by the <see cref="Product"/> aggregate.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Always returns <see cref="Result.Deleted"/>, as the removal from the parent collection
    /// is managed by the <see cref="Product"/> aggregate.
    /// </returns>
    /// <remarks>
    /// This method signals that the product option type should no longer be associated with its parent <see cref="Product"/>.
    /// The <see cref="Product.RemoveOptionType(Guid)"/> method should be used to initiate the removal from the product's collection.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}
