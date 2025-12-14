using ReSys.Core.Domain.Catalog.Products.PropertyTypes;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Properties
    {
        public static class Models
        {
            public record PropertyParameter : IHasPosition
            {
                public string Value { get; init; } = string.Empty;
                public int Position { get; set; }
            }
            public sealed class ParameterValidator : AbstractValidator<PropertyParameter>
            {
                public ParameterValidator()
                {
                    RuleFor(expression: x => x.Value)
                        .MustBeValidInputRequired(
                            prefix: nameof(ProductPropertyType),
                            field: nameof(ProductPropertyType.Value),
                            maxLength: ProductPropertyType.Constraints.MaxValueLength);
                    this.AddPositionRules(prefix: nameof(ProductPropertyType));
                }
            }
        }
    }
}