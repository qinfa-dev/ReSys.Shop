using Mapster;

using ReSys.Core.Domain.Catalog.Products.OptionTypes;
using ReSys.Core.Domain.Catalog.Products.PropertyTypes;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class OptionTypes
    {
        public static class Models
        {
            public record ProductOptionTypeParameter : IHasPosition
            {
                public Guid OptionTypeId { get; set; }
                public int Position { get; set; }
            }

            public sealed class ProductOptionTypeParameterValidator : AbstractValidator<ProductOptionTypeParameter>
            {
                public ProductOptionTypeParameterValidator()
                {
                    this.AddPositionRules(prefix: nameof(ProductPropertyType));
                }
            }

            public record ProductOptionTypeResult : ProductOptionTypeParameter, IHasIdentity<Guid>
            {
                public Guid Id { get; set; }
                public string? OptionTypeName { get; set; }
                public string? OptionTypePresentation { get; set; }
            }

            public sealed class Mapping : IRegister
            {
                public void Register(TypeAdapterConfig config)
                {
                    config.NewConfig<ProductOptionType, ProductOptionTypeResult>()
                        .Map(member: dest => dest.Id, source: src => src.Id)
                        .Map(member: dest => dest.Position, source: src => src.Position)
                        .Map(member: dest => dest.OptionTypeId, source: src => src.OptionType.Id)
                        .Map(member: dest => dest.OptionTypeName, source: src => src.OptionType.Name)
                        .Map(member: dest => dest.OptionTypePresentation, source: src => src.OptionType.Presentation);
                }
            }
        }
    }
}