using Mapster;

using ReSys.Core.Domain.Settings.ShippingMethods;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
{
    public static class Models
    {
        public record Parameter : IHasParameterizableName, IHasMetadata, IHasDisplayOn
        {
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string? Description { get; set; }
            public ShippingMethod.ShippingType Type { get; set; }
            public bool Active { get; set; } = true;
            public int Position { get; set; }
            public decimal BaseCost { get; set; }
            public int? EstimatedDaysMin { get; set; }
            public int? EstimatedDaysMax { get; set; }

            public decimal? MaxWeight { get; set; }
            public DisplayOn DisplayOn { get; set; } = DisplayOn.Both;
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(ShippingMethod);

                this.AddParameterizableNameRules(prefix);
                this.AddMetadataSupportRules(prefix);
                this.AddDisplayOnRules(prefix);

                RuleFor(expression: x => x.Name)
                    .NotEmpty()
                    .MaximumLength(ShippingMethod.Constraints.NameMaxLength);

                RuleFor(expression: x => x.Type)
                    .IsInEnum();

                RuleFor(expression: x => x.BaseCost)
                    .GreaterThanOrEqualTo(0);
            }
        }

        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public ShippingMethod.ShippingType Type { get; set; }
            public bool Active { get; set; }
            public decimal BaseCost { get; set; }
            public string EstimatedDelivery { get; set; } = string.Empty;
        }

        public record ListItem : SelectItem
        {
            public string? Description { get; set; }
            public int Position { get; set; }
            public int? EstimatedDaysMin { get; set; }
            public int? EstimatedDaysMax { get; set; }
            public decimal? MaxWeight { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<ShippingMethod, SelectItem>();
                config.NewConfig<ShippingMethod, ListItem>();
                config.NewConfig<ShippingMethod, Detail>();
            }
        }
    }
}
