using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Promotions.Rules;

public sealed class PromotionRuleTaxon : AuditableEntity<Guid>
{
    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "PromotionRuleTaxon.NotFound", description: $"Promotion rule taxon with ID '{id}' was not found.");
    }
    #endregion

    #region Properties
    public Guid PromotionRuleId { get; set; }
    public Guid TaxonId { get; set; }
    #endregion

    #region Relationships
    public PromotionRule PromotionRule { get; set; } = null!;
    public Taxon Taxon { get; set; } = null!;
    #endregion

    #region Constructors
    private PromotionRuleTaxon() { }
    #endregion

    #region Factory Methods
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
    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }
    #endregion
}
