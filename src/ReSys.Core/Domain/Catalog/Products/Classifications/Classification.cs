using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Catalog.Products.Classifications;

/// <summary>
/// Represents a product's explicit association with a specific <see cref="Taxon"/> (category) within a <see cref="Taxonomy"/>.
/// This entity acts as a junction table, defining the position of the product within that taxon's product list.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Categorization</term>
/// <description>Links a product to one or more categories, enabling structured browsing.</description>
/// </item>
/// <item>
/// <term>Ordering</term>
/// <description>Defines the display order of the product when listed under a specific taxon.</description>
/// </item>
/// <item>
/// <term>Many-to-Many Relationship</term>
/// <description>Facilitates a flexible relationship where products can belong to multiple taxons, and taxons can contain multiple products.</description>
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
/// <term>TaxonId</term>
/// <description>The unique identifier of the associated <see cref="Taxon"/>.</description>
/// </item>
/// <item>
/// <term>Position</term>
/// <description>The display order of the product within the taxon.</description>
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
public sealed class Classification :
    AuditableEntity,
    IHasPosition
{
    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="Classification"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a <see cref="Classification"/> requires both a <see cref="ProductId"/> and a <see cref="TaxonId"/>.
        /// </summary>
        public static Error Required => Error.Validation(code: "Classification.Required", description: "Classification requires both ProductId and TaxonId.");
        /// <summary>
        /// Error indicating that the product is already linked to the specified taxon.
        /// </summary>
        /// <param name="productId">The unique identifier of the <see cref="Product"/>.</param>
        /// <param name="taxonId">The unique identifier of the <see cref="Taxon"/>.</param>
        public static Error AlreadyLinked(Guid productId, Guid taxonId) => Error.Conflict(code: "Classification.AlreadyLinked", description: $"Product '{productId}' is already linked to taxon '{taxonId}'.");
        /// <summary>
        /// Error indicating that a requested <see cref="Classification"/> could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="Classification"/> that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "Classification.NotFound", description: $"Classification with ID '{id}' was not found.");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the display order of the product within the taxon's product list.
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
    /// Gets or sets the navigation property to the associated <see cref="Product"/>.
    /// </summary>
    public Product Product { get; set; } = null!;
    /// <summary>
    /// Gets or sets the unique identifier of the associated <see cref="Taxon"/>.
    /// </summary>
    public Guid TaxonId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Taxon"/>.
    /// </summary>
    public Taxon Taxon { get; set; } = null!;

    #endregion

    private Classification() { }

    #region Factory

    /// <summary>
    /// Factory method to create a new <see cref="Classification"/> instance.
    /// </summary>
    /// <param name="productId">The unique identifier of the <see cref="Product"/> to be classified.</param>
    /// <param name="taxonId">The unique identifier of the <see cref="Taxon"/> to which the product is being assigned.</param>
    /// <param name="position">The display order of the product within this taxon. Defaults to 0 and must be non-negative.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Classification}"/> result.
    /// Returns the newly created <see cref="Classification"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures that the <paramref name="position"/> is non-negative.
    /// Basic validation for <paramref name="productId"/> and <paramref name="taxonId"/> being empty is
    /// typically handled at a higher level (e.g., in <see cref="Product.AddClassification(Classification)"/>).
    /// </remarks>
    public static ErrorOr<Classification> Create(
        Guid productId,
        Guid taxonId,
        int position = 0)
    {
        var classification = new Classification
        {
            ProductId = productId,
            TaxonId = taxonId,
            Position = Math.Max(val1: 0, val2: position)
        };

        return classification;
    }

    #endregion

    #region Business Logic
    /// <summary>
    /// Marks the <see cref="Classification"/> for logical deletion.
    /// In this context, deletion means signaling that the association between the product and the taxon should be removed.
    /// The actual removal from the parent aggregate's collection is handled by the <see cref="Product"/> aggregate.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Always returns <see cref="Result.Deleted"/>, as the removal from the parent collection
    /// is managed by the <see cref="Product"/> aggregate.
    /// </returns>
    /// <remarks>
    /// This method signals that the classification should no longer be associated with its parent <see cref="Product"/>.
    /// The <see cref="Product.RemoveClassification(Guid)"/> method should be used to initiate the removal.
    /// </remarks>
    public ErrorOr<Deleted> Delete()
    {
        // This method primarily acts as a placeholder or to fulfill an interface.
        // The actual removal from the Product's collection is handled by Product.RemoveClassification.
        return Result.Deleted;
    }

    #endregion
}