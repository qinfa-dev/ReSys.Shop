using Mapster;
using ReSys.Core.Domain.Stores.ShippingMethods;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class ShippingMethods
    {
        public static class Models
        {
            public record StoreShippingMethodParameter
            {
                public Guid ShippingMethodId { get; set; }
                public bool Available { get; set; } = true;
                public decimal? StoreBaseCost { get; set; }
            }

            public sealed class StorePaymentMethodParameterValidator : AbstractValidator<StoreShippingMethodParameter>
            {
                public StorePaymentMethodParameterValidator()
                {
                    var paymentMethodIdRequired = CommonInput.Errors.KeyRequired(nameof(StoreShippingMethod));
                    RuleFor(x => x.ShippingMethodId).NotEmpty()
                        .WithMessage(paymentMethodIdRequired.Description)
                        .WithErrorCode(paymentMethodIdRequired.Code);
                }
            }

            public record StoreShippingMethodListItem : StoreShippingMethodParameter
            {
                public Guid Id { get; init; }
                public string ShippingMethodName { get; init; } = string.Empty;
                public Guid? StoreId { get; init; }
                public string? StoreName { get; init; }
                public DateTimeOffset CreatedAt { get; init; }
                public DateTimeOffset? UpdatedAt { get; init; }


                public sealed class Mapping : IRegister
                {
                    public void Register(TypeAdapterConfig config)
                    {
                        config.NewConfig<StoreShippingMethod, StoreShippingMethodListItem>()
                            .Map(d => d.ShippingMethodName, s => s.ShippingMethod!.Name)
                            .Map(d => d.StoreId, s => s.StoreId)
                            .Map(d => d.StoreName, s => s.Store!.Name);
                    }
                }
            }
        }
    }
}
