using ReSys.Core.Domain.Promotions.Actions;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Helpers
    {
        // Helper method to create PromotionAction from DTO
        public static ErrorOr<PromotionAction> CreatePromotionAction(Models.PromotionActionTransfer transfer)
        {
            return transfer.Type switch
            {
                Promotion.PromotionType.OrderDiscount => CreateOrderDiscountAction(transfer),
                Promotion.PromotionType.ItemDiscount => CreateItemDiscountAction(transfer),
                Promotion.PromotionType.FreeShipping => PromotionAction.CreateFreeShipping(),
                Promotion.PromotionType.BuyXGetY => CreateBuyXGetYAction(transfer),
                _ => Error.Validation("PromotionAction.InvalidType", $"Invalid action type: {transfer.Type}")
            };
        }

        public static ErrorOr<PromotionAction> CreateOrderDiscountAction(Models.PromotionActionTransfer transfer)
        {
            if (!transfer.DiscountType.HasValue)
                return Error.Validation("PromotionAction.DiscountTypeRequired", "DiscountType is required for OrderDiscount");

            if (!transfer.Value.HasValue)
                return Error.Validation("PromotionAction.ValueRequired", "Value is required for OrderDiscount");

            return PromotionAction.CreateOrderDiscount(transfer.DiscountType.Value, transfer.Value.Value);
        }

        public static ErrorOr<PromotionAction> CreateItemDiscountAction(Models.PromotionActionTransfer transfer)
        {
            if (!transfer.DiscountType.HasValue)
                return Error.Validation("PromotionAction.DiscountTypeRequired", "DiscountType is required for ItemDiscount");

            if (!transfer.Value.HasValue)
                return Error.Validation("PromotionAction.ValueRequired", "Value is required for ItemDiscount");

            return PromotionAction.CreateItemDiscount(transfer.DiscountType.Value, transfer.Value.Value);
        }

        public static ErrorOr<PromotionAction> CreateBuyXGetYAction(Models.PromotionActionTransfer transfer)
        {
            if (!transfer.BuyVariantId.HasValue || !transfer.BuyQuantity.HasValue ||
                !transfer.GetVariantId.HasValue || !transfer.GetQuantity.HasValue)
                return Error.Validation("PromotionAction.MissingBuyXGetYParams",
                    "BuyXGetY requires BuyVariantId, BuyQuantity, GetVariantId, and GetQuantity");

            return PromotionAction.CreateBuyXGetY(
                transfer.BuyVariantId.Value,
                transfer.BuyQuantity.Value,
                transfer.GetVariantId.Value,
                transfer.GetQuantity.Value);
        }
    }
}