using Mapster;

using ReSys.Core.Domain.Catalog.Taxonomies;

namespace ReSys.Core.Feature.Admin.Catalog.Taxonomies;

public static partial class TaxonomyModule
{
    public static class Models
    {
        // Request:
        public record Parameter :
            IHasParameterizableName,
            IHasUniqueName,
            IHasPosition,
            IHasMetadata
        {
            public Guid? StoreId { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public int Position { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        // Validator:
        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(Taxonomy);
                var idRequired = CommonInput.Errors.Required(prefix, nameof(Taxonomy.StoreId));

                //RuleFor(expression: x => x.StoreId)
                //    .NotEmpty()
                //    .WithErrorCode(idRequired.Code)
                //    .WithMessage(idRequired.Description);

                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);
                this.AddPositionRules(prefix: prefix);
            }
        }

        // Result:
        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
        }

        public record ListItem
        {
            // Properties:
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string StoreName { get; set; } = string.Empty;
            public int Position { get; set; }
            public int TaxonCount { get; set; }

            // Audit time:
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : Parameter, IHasIdentity<Guid>
        {
            public Guid Id { get; set; }
        }

        // Mapping:
        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                // Taxonomy -> SelectItem
                config.NewConfig<Taxonomy, SelectItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation);

                // ListItem
                config.NewConfig<Taxonomy, ListItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.StoreName, source: src => src.Store.Name)
                    .Map(member: dest => dest.Position, source: src => src.Position)
                    .Map(member: dest => dest.TaxonCount, source: src => src.Taxons.Count)
                    .Map(member: dest => dest.CreatedAt, source: src => src.CreatedAt)
                    .Map(member: dest => dest.UpdatedAt, source: src => src.UpdatedAt);

                // Taxonomy -> Detail
                config.NewConfig<Taxonomy, Detail>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Position, source: src => src.Position)
                    .Map(member: dest => dest.PublicMetadata, source: src => src.PublicMetadata)
                    .Map(member: dest => dest.PrivateMetadata, source: src => src.PrivateMetadata);
            }
        }
    }
}