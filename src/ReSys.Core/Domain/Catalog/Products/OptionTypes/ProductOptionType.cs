using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.OptionTypes;

namespace ReSys.Core.Domain.Catalog.Products.OptionTypes;

public sealed class ProductOptionType : AuditableEntity, IHasPosition
{
    #region Errors
    public static class Errors
    {
                public static Error Required => CommonInput.Errors.Required(prefix: nameof(ProductOptionType));
        public static Error AlreadyLinked => Error.Conflict(code: "ProductOptionType.AlreadyLinked", description: "OptionType is already linked to this product.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "ProductOptionType.NotFound", description: $"ProductOptionType with ID '{id}' was not found.");
    }
    #endregion

    #region Core Properties
    public int Position { get; set; }
    #endregion

    #region Relationships
    public Guid ProductId { get; set; }
    public Guid OptionTypeId { get; set; }
    public Product Product { get; set; } = null!;
    public OptionType OptionType { get; set; } = null!;
    #endregion

    #region Constructors
    private ProductOptionType() { }
    #endregion

    #region Factory
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
    public ErrorOr<ProductOptionType> UpdatePosition(int position)
    {
        Position = position;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}
