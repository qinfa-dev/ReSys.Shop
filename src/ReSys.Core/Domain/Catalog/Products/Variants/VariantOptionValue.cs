using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.OptionTypes;

namespace ReSys.Core.Domain.Catalog.Products.Variants;

public sealed class VariantOptionValue : AuditableEntity<Guid>
{
    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => CommonInput.Errors.NotFound(prefix: nameof(VariantOptionValue), field: id.ToString());
    }
    #endregion

    #region Relationships
    public Guid VariantId { get; set; }
    public Guid OptionValueId { get; set; }
    public Variant Variant { get; set; } = null!;
    public OptionValue OptionValue { get; set; } = null!;
    #endregion

    #region Constructors
    private VariantOptionValue() { }
    #endregion

    #region Factory
    public static ErrorOr<VariantOptionValue> Create(Guid variantId, Guid optionValueId)
    {
        return new VariantOptionValue
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            OptionValueId = optionValueId
        };
    }
    #endregion

    #region Business Logic
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}