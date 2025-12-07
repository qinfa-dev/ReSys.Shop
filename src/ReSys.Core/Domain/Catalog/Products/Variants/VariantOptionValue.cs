using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.OptionTypes;

namespace ReSys.Core.Domain.Catalog.Products.Variants;

/// <summary>
/// Represents the explicit association of a <see cref="OptionValue"/> with a <see cref="Variant"/>.
/// This entity defines a specific characteristic (e.g., "Red" color, "Large" size) for a product variant.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Variant Definition:</strong>
/// <list type="bullet">
/// <item>
/// <term>Variant Differentiation</term>
/// <description>Links a <see cref="Variant"/> to the <see cref="OptionValue"/>s that define its unique configuration.</description>
/// </item>
/// <item>
/// <term>Many-to-Many Relationship</term>
/// <description>Facilitates a flexible relationship where a variant can have multiple option values, and an option value can be part of multiple variants.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>VariantId</term>
/// <description>The unique identifier of the associated <see cref="Variant"/>.</description>
/// </item>
/// <item>
/// <term>OptionValueId</term>
/// <description>The unique identifier of the associated <see cref="OptionValue"/>.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class VariantOptionValue : AuditableEntity<Guid>
{
    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="VariantOptionValue"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested <see cref="VariantOptionValue"/> could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="VariantOptionValue"/> that was not found.</param>
        public static Error NotFound(Guid id) => CommonInput.Errors.NotFound(prefix: nameof(VariantOptionValue), field: id.ToString());
    }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the unique identifier of the associated <see cref="Variant"/>.
    /// </summary>
    public Guid VariantId { get; set; }
    /// <summary>
    /// Gets or sets the unique identifier of the associated <see cref="OptionValue"/>.
    /// </summary>
    public Guid OptionValueId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Variant"/>.
    /// </summary>
    public Variant Variant { get; set; } = null!;
    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="OptionValue"/>.
    /// </summary>
    public OptionValue OptionValue { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private VariantOptionValue() { }
    #endregion

    #region Factory
    /// <summary>
    /// Factory method to create a new <see cref="VariantOptionValue"/> instance.
    /// This establishes a link between a product variant and an option value.
    /// </summary>
    /// <param name="variantId">The unique identifier of the <see cref="Variant"/>.</param>
    /// <param name="optionValueId">The unique identifier of the <see cref="OptionValue"/>.</param>
    /// <returns>
    /// An <see cref="ErrorOr{VariantOptionValue}"/> result.
    /// Returns the newly created <see cref="VariantOptionValue"/> instance on success.
    /// </returns>
    /// <remarks>
    /// Basic validation for <paramref name="variantId"/> and <paramref name="optionValueId"/> being empty
    /// is typically handled at a higher level (e.g., in <see cref="Variant.AddOptionValue(OptionValue)"/>).
    /// </remarks>
    public static ErrorOr<VariantOptionValue> Create(Guid variantId, Guid optionValueId)
    {
        // Basic validation for Guids is typically done by the caller (e.g., Variant.AddOptionValue)
        // Here, we assume valid Guids are passed.

        return new VariantOptionValue
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            OptionValueId = optionValueId,
            CreatedAt = DateTimeOffset.UtcNow // Inherited from AuditableEntity
        };
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Marks the <see cref="VariantOptionValue"/> for logical deletion.
    /// In this context, deletion typically means signaling that the association between the variant and the option value should be removed.
    /// The actual removal from the parent aggregate's collection is handled by the <see cref="Variant"/> aggregate.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Always returns <see cref="Result.Deleted"/>, as the removal from the parent collection
    /// is managed by the <see cref="Variant"/> aggregate.
    /// </returns>
    /// <remarks>
    /// This method signals that the variant option value should no longer be associated with its parent <see cref="Variant"/>.
    /// The <see cref="Variant.RemoveOptionValue(Guid)"/> method should be used to initiate the removal from the variant's collection.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}