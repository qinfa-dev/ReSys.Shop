using Mapster;

using ReSys.Core.Domain.Catalog.OptionTypes;

namespace ReSys.Core.Feature.Admin.Catalog.OptionValues;

public static partial class OptionValueModule
{
    public static class Models
    {
        // Request:
        public record Parameter : IHasParameterizableName,
            IHasUniqueName,
            IHasPosition,
            IHasMetadata
        {
            public Guid OptionTypeId { get; set; }
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
                const string prefix = nameof(OptionValue);
                RuleFor(expression: x => x.OptionTypeId)
                    .NotEmpty()
                    .WithErrorCode(OptionType.Errors.Required.Code)
                    .WithMessage(OptionType.Errors.Required.Description);
                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);
                this.AddPositionRules(prefix: prefix);
            }
        }

        // Result:
        public record SelectItem : IHasParameterizableName, IHasIdentity<Guid>
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public Guid? OptionTypeId { get; set; }
            public string? OptionTypeName { get; set; }
            public string? OptionTypePresentation{ get; set; }
        }

        public record ListItem
        {
            // Properties:
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string OptionTypeName { get; set; } = string.Empty;
            public int Position { get; set; }

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
                // OptionValue -> SelectItem
                config.NewConfig<OptionValue, SelectItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.OptionTypeId, source: src => src.OptionType.Id)
                    .Map(member: dest => dest.OptionTypeName, source: src => src.OptionType.Name)
                    .Map(member: dest => dest.OptionTypePresentation, source: src => src.OptionType.Presentation);

                // OptionValue -> ListItem
                config.NewConfig<OptionValue, ListItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Position, source: src => src.Position)
                    .Map(member: dest => dest.OptionTypeName, source: src => src.OptionType.Name)
                    .Map(member: dest => dest.CreatedAt, source: src => src.CreatedAt)
                    .Map(member: dest => dest.UpdatedAt, source: src => src.UpdatedAt);

                // OptionValue -> Detail
                config.NewConfig<OptionValue, Detail>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Position, source: src => src.Position)
                    .Map(member: dest => dest.OptionTypeId, source: src => src.OptionTypeId)
                    .Map(member: dest => dest.PublicMetadata, source: src => src.PublicMetadata)
                    .Map(member: dest => dest.PrivateMetadata, source: src => src.PrivateMetadata);
            }
        }
    }
}