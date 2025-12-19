using Mapster;

using ReSys.Core.Domain.Inventories.Movements;
using ReSys.Core.Domain.Inventories.Stocks;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    public static class Models
    {
        public record Parameter : IHasMetadata
        {
            public Guid VariantId { get; set; }
            public Guid StockLocationId { get; set; }
            public string Sku { get; set; } = string.Empty;
            public int QuantityOnHand { get; set; }
            public int QuantityReserved { get; set; }
            public bool Backorderable { get; set; } = true;
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(StockItem);

                var variantIdRequired = CommonInput.Errors.Required(prefix, nameof(StockItem.VariantId));
                RuleFor(expression: x => x.VariantId)
                    .NotEmpty()
                    .WithErrorCode(variantIdRequired.Code)
                    .WithMessage(variantIdRequired.Description);

                var stockLocationIdRequired = CommonInput.Errors.Required(prefix, nameof(StockItem.StockLocationId));
                RuleFor(expression: x => x.StockLocationId)
                    .NotEmpty()
                    .WithErrorCode(stockLocationIdRequired.Code)
                    .WithMessage(stockLocationIdRequired.Description);

                RuleFor(expression: x => x.Sku)
                    .NotEmpty()
                    .MaximumLength(maximumLength: StockItem.Constraints.SkuMaxLength);

                RuleFor(expression: x => x.QuantityOnHand)
                    .GreaterThanOrEqualTo(valueToCompare: 0);

                RuleFor(expression: x => x.QuantityReserved)
                    .GreaterThanOrEqualTo(valueToCompare: 0);

                this.AddMetadataSupportRules(prefix: prefix);
            }
        }

        public record ListItem
        {
            public Guid Id { get; set; }
            public Guid VariantId { get; set; }
            public string VariantSku { get; set; } = string.Empty;
            public string ProductName { get; set; } = string.Empty;
            public Guid StockLocationId { get; set; }
            public string LocationName { get; set; } = string.Empty;
            public string Sku { get; set; } = string.Empty;
            public int QuantityOnHand { get; set; }
            public int QuantityReserved { get; set; }
            public int CountAvailable { get; set; }
            public bool Backorderable { get; set; }
            public bool InStock { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public record MovementItem
        {
            public Guid Id { get; set; }
            public int Quantity { get; set; }
            public string Originator { get; set; } = string.Empty;
            public string Action { get; set; } = string.Empty;
            public string? Reason { get; set; }
            public bool IsIncrease { get; set; }
            public bool IsDecrease { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<StockItem, ListItem>()
                    .Map(member: dest => dest.VariantSku, source: src => src.Variant.Sku)
                    .Map(member: dest => dest.ProductName, source: src => src.Variant.Product.Name)
                    .Map(member: dest => dest.LocationName, source: src => src.StockLocation.Name);

                config.NewConfig<StockItem, Detail>()
                    .Inherits<StockItem, ListItem>();

                config.NewConfig<StockMovement, MovementItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Quantity, source: src => src.Quantity)
                    .Map(member: dest => dest.Originator, source: src => src.Originator.ToString())
                    .Map(member: dest => dest.Action, source: src => src.Action.ToString())
                    .Map(member: dest => dest.Reason, source: src => src.Reason)
                    .Map(member: dest => dest.IsIncrease, source: src => src.IsIncrease)
                    .Map(member: dest => dest.IsDecrease, source: src => src.IsDecrease);

            }
        }
    }
}