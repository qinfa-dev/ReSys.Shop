using Mapster;

using ReSys.Core.Domain.Stores.Products;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class Products
    {
        public static class Models
        {
            public record StoreProductParameter
            {
                public Guid ProductId { get; set; }
                public bool Visible { get; set; } = true;
                public bool Featured { get; set; } = true;
            }

            public sealed class StorePaymentMethodParameterValidator : AbstractValidator<StoreProductParameter>
            {
                public StorePaymentMethodParameterValidator()
                {
                    var idRequired = CommonInput.Errors.KeyRequired(nameof(StoreProduct));
                    RuleFor(x => x.ProductId).NotEmpty()
                        .WithMessage(idRequired.Description)
                        .WithErrorCode(idRequired.Code);
                }
            }
            public record StoreProductListItem : StoreProductParameter
            {
                public Guid? Id { get; set; }
                public Guid StoreId { get; set; }
                public string ProductName { get; set; } = string.Empty;
                public string ProductSku { get; set; } = string.Empty;
                public DateTimeOffset CreatedAt { get; set; }
                public DateTimeOffset? UpdatedAt { get; set; }
            }
            public sealed class Mapping : IRegister
            {
                public void Register(TypeAdapterConfig config)
                {
                    config.NewConfig<StoreProduct, StoreProductListItem>()
                        .Map(dest => dest.ProductName, src => src.Product!.Name)
                        .Map(dest => dest.ProductSku, src => src.Product.GetMaster().Value.Sku);
                }
            }

        }
    }

}
