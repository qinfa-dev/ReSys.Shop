using Mapster;

using ReSys.Core.Domain.Catalog.PropertyTypes;

namespace ReSys.Core.Feature.Admin.Catalog.PropertyTypes;

public static partial class PropertyTypeModule
{
    public static class Models
    {
        // Request:
        public record Parameter : IHasParameterizableName,
            IHasUniqueName,
            IHasPosition,
            IHasMetadata,
            IHasDisplayOn
        {
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public PropertyType.PropertyKind Kind { get; set; }
            public bool Filterable { get; set; }
            public string? FilterParam { get; set; }
            public DisplayOn DisplayOn { get; set; }
            public int Position { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        // Validator:
        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(PropertyType);
                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);
                this.AddPositionRules(prefix: prefix);
                this.AddDisplayOnRules(prefix: prefix);
                RuleFor(expression: x => x.Kind)
                    .MustBeValidEnum(prefix: prefix, field: nameof(Parameter.Kind));

                RuleFor(expression: x => x.DisplayOn)
                    .MustBeValidEnum(prefix: prefix, field: nameof(DisplayOn));

                RuleFor(expression: x => x.FilterParam)
                    .MustBeValidSlug(prefix: prefix, field: nameof(Parameter.FilterParam))
                    .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.FilterParam));
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
            public PropertyType.PropertyKind Kind { get; set; }
            public string? FilterParam { get; set; }
            public DisplayOn DisplayOn { get; set; }
            public int Position { get; set; }
            public bool Filterable { get; set; }

            // Statistics:
            public int ProductPropertyCount { get; set; }

            // Audit time:
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        // Mapping:
        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                // Property -> SelectItem
                config.NewConfig<PropertyType, SelectItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation);

                // ListItem
                config.NewConfig<PropertyType, ListItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Kind, source: src => src.Kind)
                    .Map(member: dest => dest.FilterParam, source: src => src.FilterParam)
                    .Map(member: dest => dest.DisplayOn, source: src => src.DisplayOn)
                    .Map(member: dest => dest.Position, source: src => src.Position)
                    .Map(member: dest => dest.Filterable, source: src => src.Filterable)
                    .Map(member: dest => dest.ProductPropertyCount, source: src => src.ProductPropertyTypes.Count)
                    .Map(member: dest => dest.CreatedAt, source: src => src.CreatedAt)
                    .Map(member: dest => dest.UpdatedAt, source: src => src.UpdatedAt);

                // Property -> Detail
                config.NewConfig<PropertyType, Detail>()
                    .Inherits<PropertyType, ListItem>()
                    .Map(member: dest => dest.PublicMetadata, source: src => src.PublicMetadata)
                    .Map(member: dest => dest.PrivateMetadata, source: src => src.PrivateMetadata);
            }
        }
    }
}