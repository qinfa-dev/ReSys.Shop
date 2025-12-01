using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Catalog.Products.Classifications;

/// <summary>
/// Links a Product to a Taxon, defining its position within that taxon's product list.
/// </summary>
public sealed class Classification :
    AuditableEntity,
    IHasPosition
{
    #region Errors

    public static class Errors
    {
        public static Error Required => Error.Validation(code: "Classification.Required", description: "Classification requires both ProductId and TaxonId.");
        public static Error AlreadyLinked(Guid productId, Guid taxonId) => Error.Conflict(code: "Classification.AlreadyLinked", description: $"Product '{productId}' is already linked to taxon '{taxonId}'.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "Classification.NotFound", description: $"Classification with ID '{id}' was not found.");
    }

    #endregion

    #region Properties

    public int Position { get; set; }

    #endregion

    #region Relationships
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public Guid TaxonId { get; set; }
    public Taxon Taxon { get; set; } = null!;

    #endregion

    private Classification() { }

    #region Factory

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
    public ErrorOr<Deleted> Delete()
    {
        return Result.Deleted;
    }

    #endregion
}