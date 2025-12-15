using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Promotions.Rules;

/// <summary>
/// Represents a junction entity linking a <see cref="PromotionRule"/> to a specific <see cref="Taxon"/>.
/// This is used for category-based promotion rules.
/// </summary>
/// <remarks>
/// This entity is an <see cref="AuditableEntity{Guid}"/> and tracks its creation and update timestamps.
/// It ensures that a promotion rule can target specific product categories.
/// </remarks>
public sealed class PromotionRuleTaxon : AuditableEntity<Guid>
{
    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="PromotionRuleTaxon"/> operations.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested promotion rule taxon could not be found.
        /// </summary>
        /// <param name="id">The ID of the promotion rule taxon that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "PromotionRuleTaxon.NotFound", description: $"Promotion rule taxon with ID '{id}' was not found.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="PromotionRule"/>.
    /// </summary>
    public Guid PromotionRuleId { get; set; }
    /// <summary>
    /// Gets or sets the foreign key to the associated <see cref="Taxon"/>.
    /// </summary>
    public Guid TaxonId { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the <see cref="PromotionRule"/> to which this taxon association belongs.
    /// </summary>
    public PromotionRule PromotionRule { get; set; } = null!;
    /// <summary>
    /// Gets or sets the <see cref="Taxon"/> associated with this rule.
    /// </summary>
    public Taxon Taxon { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private PromotionRuleTaxon() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new <see cref="PromotionRuleTaxon"/> instance.
    /// </summary>
    /// <param name="promotionRuleId">The ID of the <see cref="PromotionRule"/>.</param>
    /// <param name="taxonId">The ID of the <see cref="Taxon"/> to associate.</param>
    /// <returns>A new <see cref="PromotionRuleTaxon"/> instance.</returns>
    public static ErrorOr<PromotionRuleTaxon> Create(Guid promotionRuleId, Guid taxonId)
    {
        var promotionRuleTaxon = new PromotionRuleTaxon
        {
            Id = Guid.NewGuid(),
            PromotionRuleId = promotionRuleId,
            TaxonId = taxonId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return promotionRuleTaxon;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Marks the <see cref="PromotionRuleTaxon"/> for deletion.
    /// </summary>
    /// <returns><see cref="Result.Deleted"/> on success.</returns>
    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }
    #endregion
}
