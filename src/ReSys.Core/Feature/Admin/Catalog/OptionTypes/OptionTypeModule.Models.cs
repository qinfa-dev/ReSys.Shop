using Mapster;

using ReSys.Core.Domain.Catalog.OptionTypes;

namespace ReSys.Core.Feature.Admin.Catalog.OptionTypes;

public static partial class OptionTypeModule
{
    public static class Models
    {
        // Request:
        public record Parameter : IHasParameterizableName,
            IHasUniqueName,
            IHasPosition,
            IHasMetadata
        {
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public bool Filterable { get; set; }
            public int Position { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        // Validator:
        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(OptionType);
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
            public int Position { get; set; }
            public bool Filterable { get; set; }

            // Statistics:
            public int OptionValueCount { get; set; }
            public int ProductCount { get; set; }

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
                // OptionType -> SelectItem
                config.NewConfig<OptionType, SelectItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation);

                // ListItem
                config.NewConfig<OptionType, ListItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Position, source: src => src.Position)
                    .Map(member: dest => dest.Filterable, source: src => src.Filterable)
                    .Map(member: dest => dest.ProductCount, source: src => src.ProductOptionTypes.Count)
                    .Map(member: dest => dest.OptionValueCount, source: src => src.OptionValues.Count)
                    .Map(member: dest => dest.CreatedAt, source: src => src.CreatedAt)
                    .Map(member: dest => dest.CreatedAt, source: src => src.UpdatedAt);

                // OptionType -> Detail
                config.NewConfig<OptionType, Detail>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Position, source: src => src.Position)
                    .Map(member: dest => dest.Filterable, source: src => src.Filterable)
                    .Map(member: dest => dest.PublicMetadata, source: src => src.PublicMetadata)
                    .Map(member: dest => dest.PrivateMetadata, source: src => src.PrivateMetadata);
            }
        }
    }
}