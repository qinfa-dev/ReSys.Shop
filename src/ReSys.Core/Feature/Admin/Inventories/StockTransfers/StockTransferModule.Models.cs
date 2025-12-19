using Mapster;

using ReSys.Core.Domain.Inventories.StockTransfers;

namespace ReSys.Core.Feature.Admin.Inventories.StockTransfers;

public static partial class StockTransferModule
{
    public static class Models
    {
        public record Parameter
        {
            public Guid? SourceLocationId { get; set; }
            public Guid DestinationLocationId { get; set; }
            public string? Reference { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                RuleFor(expression: x => x.DestinationLocationId)
                    .NotEmpty()
                    .WithMessage("Destination location is required.");

                RuleFor(expression: x => x.SourceLocationId)
                    .NotEqual(x => x.DestinationLocationId)
                    .When(predicate: x => x.SourceLocationId.HasValue)
                    .WithMessage("Source and destination locations cannot be the same.");

                RuleFor(expression: x => x.Reference)
                    .MaximumLength(maximumLength: StockTransfer.Constraints.ReferenceMaxLength)
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.Reference));
            }
        }

        public record VariantQuantity
        {
            public Guid VariantId { get; init; }
            public int Quantity { get; init; }
        }

        public sealed class VariantQuantityValidator : AbstractValidator<VariantQuantity>
        {
            public VariantQuantityValidator()
            {
                RuleFor(expression: x => x.VariantId).NotEmpty();
                RuleFor(expression: x => x.Quantity)
                    .GreaterThan(valueToCompare: 0)
                    .WithMessage("Quantity must be positive.");
            }
        }

        public record ListItem
        {
            public Guid Id { get; set; }
            public string Number { get; set; } = string.Empty;
            public Guid? SourceLocationId { get; set; }
            public string? SourceLocationName { get; set; }
            public Guid DestinationLocationId { get; set; }
            public string DestinationLocationName { get; set; } = string.Empty;
            public string? Reference { get; set; }
            public int MovementCount { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public List<MovementItem> Movements { get; set; } = new();
        }

        public record MovementItem
        {
            public Guid Id { get; set; }
            public Guid VariantId { get; set; }
            public string VariantSku { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public int Quantity { get; set; }
            public string Action { get; set; } = string.Empty;
            public string Originator { get; set; } = string.Empty;
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<StockTransfer, ListItem>()
                    .Map(member: dest => dest.SourceLocationName, source: src => src.SourceLocation!.Name)
                    .Map(member: dest => dest.DestinationLocationName, source: src => src.DestinationLocation.Name)
                    .Map(member: dest => dest.MovementCount, source: src => src.Movements.Count);

                config.NewConfig<StockTransfer, Detail>()
                    .Inherits<StockTransfer, ListItem>();
            }
        }
    }
}