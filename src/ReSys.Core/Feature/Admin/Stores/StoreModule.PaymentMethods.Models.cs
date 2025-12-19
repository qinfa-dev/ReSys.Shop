using Mapster;

using ReSys.Core.Domain.Stores.PaymentMethods;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public static partial class PaymentMethods
    {
        public static class Models
        {
            public record StorePaymentMethodParameter
            {
                public Guid PaymentMethodId { get; set; }
                public bool Available { get; set; } = true;
            }

            public sealed class StorePaymentMethodParameterValidator : AbstractValidator<StorePaymentMethodParameter>
            {
                public StorePaymentMethodParameterValidator()
                {
                    var paymentMethodIdRequired = CommonInput.Errors.KeyRequired(nameof(StorePaymentMethod));
                    RuleFor(x => x.PaymentMethodId).NotEmpty()
                        .WithMessage(paymentMethodIdRequired.Description)
                        .WithErrorCode(paymentMethodIdRequired.Code);
                }
            }

            public record StorePaymentMethodSelectItem : StorePaymentMethodParameter
            {
                public Guid Id { get; set; }
                public string PaymentMethodName { get; set; } = string.Empty;
                public Guid StoreId { get; set; }
                public string StoreName { get; set; } = string.Empty;
                public DateTimeOffset CreatedAt { get; set; }
                public DateTimeOffset? UpdatedAt { get; set; }
            }

            public sealed class Mapping : IRegister
            {
                public void Register(TypeAdapterConfig config)
                {
                    config.NewConfig<StorePaymentMethod, StorePaymentMethodSelectItem>()
                        .Map(d => d.PaymentMethodName, s => s.PaymentMethod!.Name)
                        .Map(d => d.StoreId, s => s.StoreId)
                        .Map(d => d.StoreName, s => s.Store!.Name);
                }
            }
        }
    }
}