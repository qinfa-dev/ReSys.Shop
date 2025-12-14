using Mapster;

using ReSys.Core.Domain.Catalog.Products.Classifications;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Classifications
    {
        public static class Models
        {
            public record ProductClassificationParameter : IHasPosition
            {
                public Guid TaxonId { get; set; }
                public int Position { get; set; }
            }

            public sealed class ProductClassificationParameterValidator
                : AbstractValidator<ProductClassificationParameter>
            {
                public ProductClassificationParameterValidator()
                {
                    this.AddPositionRules(prefix: nameof(Classification));
                    RuleFor(x => x.TaxonId).NotEmpty();
                }
            }

            public record ProductClassificationResult
                : ProductClassificationParameter,
                    IHasIdentity<Guid>
            {
                public Guid Id { get; set; }
                public string? TaxonName { get; set; }
                public string? TaxonPrettyName { get; set; }
            }

            public sealed class Mapping : IRegister
            {
                public void Register(TypeAdapterConfig config)
                {
                    config.NewConfig<Classification, ProductClassificationResult>()
                        .Map(d => d.Id, s => s.Id)
                        .Map(d => d.Position, s => s.Position)
                        .Map(d => d.TaxonId, s => s.TaxonId)
                        .Map(d => d.TaxonName, s => s.Taxon.Name)
                        .Map(d => d.TaxonPrettyName, s => s.Taxon.PrettyName);
                }
            }
        }
    }
}