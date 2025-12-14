using Mapster;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ReSys.Core.Domain.Catalog.Products.Images;
using ReSys.Core.Domain.Catalog.Products.Prices;
using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Feature.Catalog.Variants;

public static partial class VariantModule
{
    public static class Models
    {
        public record Parameter : IHasPosition, IHasMetadata
        {
            public Guid ProductId { get; set; }
            public string? Sku { get; set; }
            public string? Barcode { get; set; }
            public decimal? Weight { get; set; }
            public decimal? Height { get; set; }
            public decimal? Width { get; set; }
            public decimal? Depth { get; set; }
            public string? DimensionsUnit { get; set; }
            public string? WeightUnit { get; set; }
            public decimal? CostPrice { get; set; }
            public string? CostCurrency { get; set; }
            public bool TrackInventory { get; set; } = true;
            public int Position { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(Variant);

                RuleFor(expression: x => x.ProductId)
                    .NotEmpty()
                    .WithErrorCode(errorCode: "Variant.ProductIdRequired")
                    .WithMessage(errorMessage: "Product ID is required for variant.");

                RuleFor(expression: x => x.Sku)
                    .MaximumLength(maximumLength: Variant.Constraints.SkuMaxLength)
                    .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.Sku));

                RuleFor(expression: x => x.Barcode)
                    .MaximumLength(maximumLength: Variant.Constraints.BarcodeMaxLength)
                    .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.Barcode));

                RuleFor(expression: x => x.DimensionsUnit)
                    .Must(predicate: x => Variant.Constraints.ValidDimensionUnits.Contains(value: x))
                    .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.DimensionsUnit))
                    .WithErrorCode(Variant.Errors.InvalidDimensionUnit.Code)
                    .WithMessage(Variant.Errors.InvalidDimensionUnit.Description);

                RuleFor(expression: x => x.WeightUnit)
                    .Must(predicate: x => Variant.Constraints.ValidWeightUnits.Contains(value: x))
                    .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.WeightUnit))
                    .WithErrorCode(Variant.Errors.InvalidWeightUnit.Code)
                    .WithMessage(Variant.Errors.InvalidWeightUnit.Description);

                RuleFor(expression: x => x.CostCurrency)
                    .Must(predicate: x => Price.Constraints.ValidCurrencies.Contains(value: x))
                    .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.CostCurrency))
                    .WithErrorCode(Price.Errors.InvalidCurrency.Code)
                    .WithMessage(Price.Errors.InvalidCurrency.Description);

                this.AddMetadataSupportRules(prefix: prefix);
                this.AddPositionRules(prefix: prefix);
            }
        }

 
        public record SelectItem
        {
            public Guid Id { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public string? Sku { get; set; }
            public string OptionsText { get; set; } = string.Empty;
            public bool IsMaster { get; set; }
        }

        public record ListItem
        {
            public Guid Id { get; set; }
            public Guid ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public bool IsMaster { get; set; }
            public string? Sku { get; set; }
            public string? Barcode { get; set; }
            public string OptionsText { get; set; } = string.Empty;
            public decimal? Weight { get; set; }
            public string? WeightUnit { get; set; }
            public bool TrackInventory { get; set; }
            public decimal? CostPrice { get; set; }
            public string? CostCurrency { get; set; }
            public int Position { get; set; }
            public bool InStock { get; set; }
            public bool Purchasable { get; set; }
            public bool Available { get; set; }
            public double TotalOnHand { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public decimal? Height { get; set; }
            public decimal? Width { get; set; }
            public decimal? Depth { get; set; }
            public string? DimensionsUnit { get; set; }
            public DateTimeOffset? DiscontinueOn { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
            public List<string> OptionValueNames { get; set; } = new();
        }

        public record PriceItem
        {
            public decimal? Amount { get; set; }
            public Guid Id { get; set; }
            public decimal? CompareAtAmount { get; set; }
            public string Currency { get; set; } = string.Empty;
            public bool Discounted { get; set; }
        }

        public record StockItem
        {
            public Guid Id { get; set; }
            public Guid StockLocationId { get; set; }
            public string LocationName { get; set; } = string.Empty;
            public int QuantityOnHand { get; set; }
            public bool Backorderable { get; set; }
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<Variant, SelectItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.ProductName, source: src => src.Product.Name)
                    .Map(member: dest => dest.Sku, source: src => src.Sku)
                    .Map(member: dest => dest.IsMaster, source: src => src.IsMaster);

                config.NewConfig<Variant, ListItem>()
                    .Map(member: dest => dest.ProductName, source: src => src.Product.Name);

                config.NewConfig<Variant, Detail>()
                    .Inherits<Variant, ListItem>()
                    .Map(member: dest => dest.OptionValueNames, source: src => src.OptionValues.Select(ov => ov.Name).ToList());

                config.NewConfig<Price, PriceItem>();
                config.NewConfig<Core.Domain.Inventories.Stocks.StockItem, StockItem>()
                    .Map(member: dest => dest.LocationName, source: src => src.StockLocation.Name);
            }
        }
    }
}