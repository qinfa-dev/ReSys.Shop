using Mapster;

using ReSys.Core.Domain.Catalog.Products.PropertyTypes;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class PropertyType
    {
        public static class Models
        {
            public record ProductPropertyParameter : IHasPosition
            {
                public string PropertyValue { get; init; } = string.Empty;
                public int Position { get; set; }
            }
            public sealed class ParameterValidator : AbstractValidator<ProductPropertyParameter>
            {
                public ParameterValidator()
                {
                    RuleFor(expression: x => x.PropertyValue)
                        .MustBeValidInputRequired(
                            prefix: nameof(ProductPropertyType),
                            field: nameof(ProductPropertyType.PropertyTypeValue),
                            maxLength: ProductPropertyType.Constraints.MaxValueLength);
                    this.AddPositionRules(prefix: nameof(ProductPropertyType));
                }
            }

            public record ProductPropertyResult : ProductPropertyParameter, IHasIdentity<Guid>
            {
                public Guid Id { get; set; }
                public Guid? PropertyTypeId { get; set; }
                public string? PropertyTypeName { get; set; }
                public string? PropertyTypePresentation { get; set; }
            }

            public sealed class Mapping : IRegister
            {
                public void Register(TypeAdapterConfig config)
                {
                    config.NewConfig<ProductPropertyType, ProductPropertyResult>()
                        .Map(member: dest => dest.Id, source: src => src.Id)
                        .Map(member: dest => dest.PropertyValue, source: src => src.PropertyTypeValue)
                        .Map(member: dest => dest.Position, source: src => src.Position)
                        .Map(member: dest => dest.PropertyTypeId, source: src => src.PropertyType.Id)
                        .Map(member: dest => dest.PropertyTypeName, source: src => src.PropertyType.Name)
                        .Map(member: dest => dest.PropertyTypeName, source: src => src.PropertyType.Presentation);
                }
            }
        }
    }
}