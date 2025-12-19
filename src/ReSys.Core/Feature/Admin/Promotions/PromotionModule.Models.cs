using Mapster;

using ReSys.Core.Domain.Promotions.Actions;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.Promotions.Rules;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Models
    {
        #region Requests

        // Promotion:
        public record Parameter
        {
            public string Name { get; set; } = string.Empty;
            public string? PromotionCode { get; set; }
            public string? Description { get; set; }
            public decimal? MinimumOrderAmount { get; set; }
            public decimal? MaximumDiscountAmount { get; set; }
            public DateTimeOffset? StartsAt { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
            public int? UsageLimit { get; set; }
            public bool Active { get; set; } = true;
            public bool RequiresCouponCode { get; set; }
            public PromotionActionTransfer Action { get; set; } = null!;
        }
        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                RuleFor(x => x.Name)
                    .NotEmpty().WithMessage("Promotion name is required.")
                    .WithErrorCode("Promotion.Name.Required")
                    .Length(Promotion.Constraints.MinNameLength, Promotion.Constraints.NameMaxLength)
                    .WithMessage($"Promotion name must be between {Promotion.Constraints.MinNameLength} and {Promotion.Constraints.NameMaxLength} characters.")
                    .WithErrorCode("Promotion.Name.InvalidLength");

                RuleFor(x => x.PromotionCode)
                    .Length(Promotion.Constraints.MinCodeLength, Promotion.Constraints.CodeMaxLength)
                    .WithMessage($"Promotion code must be between {Promotion.Constraints.MinCodeLength} and {Promotion.Constraints.CodeMaxLength} characters.")
                    .WithErrorCode("Promotion.Code.InvalidLength")
                    .When(x => !string.IsNullOrEmpty(x.PromotionCode));

                RuleFor(x => x.MinimumOrderAmount)
                    .GreaterThanOrEqualTo(Promotion.Constraints.MinOrderAmount)
                    .WithMessage($"Minimum order amount must be greater than or equal to {Promotion.Constraints.MinOrderAmount}.")
                    .WithErrorCode("Promotion.MinimumOrderAmount.Invalid")
                    .When(x => x.MinimumOrderAmount.HasValue);

                RuleFor(x => x.MaximumDiscountAmount)
                    .GreaterThanOrEqualTo(Promotion.Constraints.MinDiscountValue)
                    .WithMessage($"Maximum discount amount must be greater than or equal to {Promotion.Constraints.MinDiscountValue}.")
                    .WithErrorCode("Promotion.MaximumDiscountAmount.Invalid")
                    .When(x => x.MaximumDiscountAmount.HasValue);

                RuleFor(x => x.UsageLimit)
                    .GreaterThanOrEqualTo(Promotion.Constraints.MinUsageLimit)
                    .WithMessage($"Usage limit must be greater than or equal to {Promotion.Constraints.MinUsageLimit}.")
                    .WithErrorCode("Promotion.UsageLimit.Invalid")
                    .When(x => x.UsageLimit.HasValue);

                RuleFor(x => x.Action)
                    .NotNull().WithMessage("Promotion action is required.")
                    .WithErrorCode("Promotion.Action.Required")
                    .SetValidator(new PromotionActionTransferValidator());

                RuleFor(x => x)
                    .Must(x => !x.RequiresCouponCode || !string.IsNullOrWhiteSpace(x.PromotionCode))
                    .WithMessage("Promotion code is required when RequiresCouponCode is true")
                    .WithErrorCode("Promotion.Code.RequiredWhenCoupon")
                    .WithName("PromotionCode");

                RuleFor(x => x)
                    .Must(x => !x.StartsAt.HasValue || !x.ExpiresAt.HasValue || x.StartsAt < x.ExpiresAt)
                    .WithMessage("Start date must be before expiry date")
                    .WithErrorCode("Promotion.DateRange.Invalid")
                    .WithName("DateRange");
            }
        }

        // PromotionAction:
        public record PromotionActionTransfer
        {
            public Promotion.PromotionType Type { get; set; }
            public Promotion.DiscountType? DiscountType { get; set; }
            public decimal? Value { get; set; } // Discount value (for OrderDiscount/ItemDiscount)
            public Guid? BuyVariantId { get; set; } // For BuyXGetY
            public int? BuyQuantity { get; set; } // For BuyXGetY
            public Guid? GetVariantId { get; set; } // For BuyXGetY
            public int? GetQuantity { get; set; } // For BuyXGetY
        }
        public sealed class PromotionActionTransferValidator : AbstractValidator<PromotionActionTransfer>
        {
            public PromotionActionTransferValidator()
            {
                RuleFor(x => x.Type)
                    .IsInEnum().WithMessage("Invalid promotion action type.")
                    .WithErrorCode("PromotionAction.Type.Invalid");

                // OrderDiscount/ItemDiscount validation
                When(x => x.Type == Promotion.PromotionType.OrderDiscount || x.Type == Promotion.PromotionType.ItemDiscount, () =>
                {
                    RuleFor(x => x.DiscountType)
                        .NotNull().WithMessage("Discount type is required for this promotion action.")
                        .WithErrorCode("PromotionAction.DiscountType.Required")
                        .IsInEnum().WithMessage("Invalid discount type.")
                        .WithErrorCode("PromotionAction.DiscountType.Invalid");

                    RuleFor(x => x.Value)
                        .NotNull().WithMessage("Discount value is required for this promotion action.")
                        .WithErrorCode("PromotionAction.Value.Required")
                        .GreaterThanOrEqualTo(0m).WithMessage("Discount value must be greater than or equal to 0.")
                        .WithErrorCode("PromotionAction.Value.Invalid")
                        .LessThanOrEqualTo(1000000m).WithMessage("Discount value cannot exceed 1,000,000.")
                        .WithErrorCode("PromotionAction.Value.TooHigh");
                });

                // BuyXGetY validation
                When(x => x.Type == Promotion.PromotionType.BuyXGetY, () =>
                {
                    RuleFor(x => x.BuyVariantId)
                        .NotEmpty().WithMessage("Buy variant ID is required for BuyXGetY promotion.")
                        .WithErrorCode("PromotionAction.BuyXGetY.BuyVariantId.Required");
                    RuleFor(x => x.BuyQuantity)
                        .NotNull().WithMessage("Buy quantity is required for BuyXGetY promotion.")
                        .WithErrorCode("PromotionAction.BuyXGetY.BuyQuantity.Required")
                        .GreaterThanOrEqualTo(1).WithMessage("Buy quantity must be greater than or equal to 1.")
                        .WithErrorCode("PromotionAction.BuyXGetY.BuyQuantity.Invalid");
                    RuleFor(x => x.GetVariantId)
                        .NotEmpty().WithMessage("Get variant ID is required for BuyXGetY promotion.")
                        .WithErrorCode("PromotionAction.BuyXGetY.GetVariantId.Required");
                    RuleFor(x => x.GetQuantity)
                        .NotNull().WithMessage("Get quantity is required for BuyXGetY promotion.")
                        .WithErrorCode("PromotionAction.BuyXGetY.GetQuantity.Required")
                        .GreaterThanOrEqualTo(1).WithMessage("Get quantity must be greater than or equal to 1.")
                        .WithErrorCode("PromotionAction.BuyXGetY.GetQuantity.Invalid");
                });
            }
        }

        // PromotionRule:
        public record RuleParameter
        {
            public PromotionRule.RuleType Type { get; set; }
            public string Value { get; set; } = string.Empty;
        }
        public sealed class RuleParameterValidator : AbstractValidator<RuleParameter>
        {
            public RuleParameterValidator()
            {
                RuleFor(x => x.Type)
                    .IsInEnum().WithMessage("Invalid promotion rule type.")
                    .WithErrorCode("PromotionRule.Type.Invalid");

                RuleFor(x => x.Value)
                    .NotEmpty().WithMessage("Rule value is required.")
                    .WithErrorCode("PromotionRule.Value.Required")
                    .MaximumLength(PromotionRule.Constraints.ValueMaxLength).WithMessage($"Rule value cannot exceed {PromotionRule.Constraints.ValueMaxLength} characters.")
                    .WithErrorCode("PromotionRule.Value.TooLong");
            }
        }

        #endregion

        #region Responses

        // Promotion:

        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? Description { get; set; }
            public bool Active { get; set; }
        }
        public record ListItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string? PromotionCode { get; set; }
            public string? Description { get; set; }
            public string Type { get; set; } = string.Empty;
            public decimal? MinimumOrderAmount { get; set; }
            public decimal? MaximumDiscountAmount { get; set; }
            public DateTimeOffset? StartsAt { get; set; }
            public DateTimeOffset? ExpiresAt { get; set; }
            public int? UsageLimit { get; set; }
            public int UsageCount { get; set; }
            public bool Active { get; set; }
            public bool IsActive { get; set; }
            public bool IsExpired { get; set; }
            public int RemainingUsage { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : ListItem
        {
            public bool RequiresCouponCode { get; set; }
            public PromotionActionTransfer? Action { get; set; }
            public int RuleCount { get; set; }
        }

        // PromotionRule:
        public record RuleItem : RuleParameter
        {
            public Guid Id { get; set; }
            public int TaxonCount { get; set; }
            public int UserCount { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        // PromotionRuleTaxon:
        public record PromotionTaxonRuleParameter
        {
            public Guid? Id { get; set; }
            public Guid TaxonId { get; set; }

        }

        public record PromotionTaxonRuleItem : PromotionTaxonRuleParameter
        {
            public string TaxonName { get; set; } = string.Empty;
            public DateTimeOffset CreatedAt { get; set; }
        }

        // PromotionRuleUsers:
        public record PromotionUsersRuleParameter
        {
            public Guid? Id { get; set; }
            public Guid UserId { get; set; }

        }

        public record PromotionUsersRuleItem : PromotionUsersRuleParameter
        {
            public string? UserName { get; init; }
            // Contact:
            public string? UserFullName { get; set; }
            public string? UserEmail { get; set; }
            public string? UserPhone { get; set; }
        }

        #endregion

        public record StatsResult
        {
            public Guid PromotionId { get; set; }
            public string Name { get; set; } = string.Empty;
            public int TotalUsageCount { get; set; }
            public int RemainingUsage { get; set; }
            public decimal TotalDiscountGiven { get; set; }
            public decimal AverageDiscountPerOrder { get; set; }
            public int AffectedOrdersCount { get; set; }
            public decimal TotalRevenueImpact { get; set; }
            public DateTimeOffset? FirstUsedAt { get; set; }
            public DateTimeOffset? LastUsedAt { get; set; }
            public Dictionary<string, int> UsageByDay { get; set; } = new();
            public List<TopProductItem> TopAffectedProducts { get; set; } = new();
            public PerformanceMetrics Performance { get; set; } = new();
        }

        public record TopProductItem
        {
            public Guid ProductId { get; set; }
            public string ProductName { get; set; } = string.Empty;
            public int TimesDiscounted { get; set; }
            public decimal TotalDiscount { get; set; }
        }

        public record PerformanceMetrics
        {
            public double ConversionRate { get; set; } // Orders with promo / Total orders during period
            public decimal RevenuePerUse { get; set; }
            public decimal CostPerAcquisition { get; set; }
            public double ReturnOnInvestment { get; set; }
        }

        public record PreviewResult
        {
            public bool IsApplicable { get; set; }
            public string? ReasonNotApplicable { get; set; }
            public List<PreviewAdjustment> Adjustments { get; set; } = new();
            public decimal TotalDiscount { get; set; }
            public decimal OriginalTotal { get; set; }
            public decimal FinalTotal { get; set; }
            public List<string> RuleEvaluations { get; set; } = new();
        }

        public record PreviewAdjustment
        {
            public string Description { get; set; } = string.Empty;
            public decimal Amount { get; set; }
            public Guid? LineItemId { get; set; }
            public string? LineItemName { get; set; }
        }

        public record HistoryItem
        {
            public Guid Id { get; set; }
            public string Action { get; set; } = string.Empty;
            public string Description { get; set; } = string.Empty;
            public string? PerformedBy { get; set; }
            public string? PerformedByName { get; set; }
            public IDictionary<string, object?>? ChangesBefore { get; set; }
            public IDictionary<string, object?>? ChangesAfter { get; set; }
            public DateTimeOffset Timestamp { get; set; }
        }


        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<Promotion, SelectItem>();

                config.NewConfig<Promotion, ListItem>()
                    .Map(dest => dest.Type, src => src.Type.ToString())
                    .Map(dest => dest.RemainingUsage, src => src.RemainingUsage);

                config.NewConfig<Promotion, Detail>()
                    .Inherits<Promotion, ListItem>()
                    .Map(dest => dest.Action, src => MapPromotionAction(src.Action))
                    .Map(dest => dest.RuleCount, src => src.PromotionRules.Count);

                config.NewConfig<PromotionRule, RuleItem>()
                    .Map(dest => dest.Type, src => src.Type.ToString())
                    .Map(dest => dest.TaxonCount, src => src.PromotionRuleTaxons.Count)
                    .Map(dest => dest.UserCount, src => src.PromotionRuleUsers.Count);
            }

            private static PromotionActionTransfer? MapPromotionAction(PromotionAction? action)
            {
                if (action == null) return null;

                var dto = new PromotionActionTransfer { Type = action.Type };

                switch (action.Type)
                {
                    case Promotion.PromotionType.OrderDiscount:
                    case Promotion.PromotionType.ItemDiscount:
                        var discountTypeString = action.GetPrivate<string>("discountType");
                        if (Enum.TryParse<Promotion.DiscountType>(discountTypeString, out var discountType))
                        {
                            dto.DiscountType = discountType;
                        }
                        dto.Value = action.GetPrivate<decimal?>("value");
                        break;
                    case Promotion.PromotionType.BuyXGetY:
                        dto.BuyVariantId = action.GetPrivate<Guid?>("buyVariantId");
                        dto.BuyQuantity = action.GetPrivate<int?>("buyQuantity");
                        dto.GetVariantId = action.GetPrivate<Guid?>("getVariantId");
                        dto.GetQuantity = action.GetPrivate<int?>("getQuantity");
                        break;
                }

                return dto;
            }
        }
    }
}