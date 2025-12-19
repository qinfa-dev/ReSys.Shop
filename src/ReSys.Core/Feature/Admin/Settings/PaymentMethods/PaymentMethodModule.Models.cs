using Mapster;

using ReSys.Core.Domain.Settings.PaymentMethods;

namespace ReSys.Core.Feature.Admin.Settings.PaymentMethods;

public static partial class PaymentMethodModule
{
    public static class Models
    {
        public record Parameter : IHasParameterizableName, IHasMetadata
        {
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string? Description { get; set; }
            public PaymentMethod.PaymentType Type { get; set; }
            public bool Active { get; set; } = true;
            public int Position { get; set; }
            public bool AutoCapture { get; set; }
            public DisplayOn DisplayOn { get; set; } = DisplayOn.Both;
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(PaymentMethod);

                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);

                RuleFor(expression: x => x.Name)
                    .NotEmpty()
                    .MaximumLength(PaymentMethod.Constraints.NameMaxLength);
            }
        }

        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public PaymentMethod.PaymentType Type { get; set; }
        }

        public record ListItem : SelectItem
        {
            public string? Description { get; set; }
            public bool Active { get; set; }
            public int Position { get; set; }
            public bool AutoCapture { get; set; }
            public DisplayOn DisplayOn { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
            public DateTimeOffset? DeletedAt { get; set; }
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
                config.NewConfig<PaymentMethod, SelectItem>();
                config.NewConfig<PaymentMethod, ListItem>();
                config.NewConfig<PaymentMethod, Detail>();
            }
        }
    }
}