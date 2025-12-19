using MapsterMapper;

using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Create
    {
        public sealed record Request : Models.Parameter;
        public sealed record Result : Models.ListItem;
        public sealed record Command(Request Request) : ICommand<Result>;
        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Request).SetValidator(new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var param = command.Request;

                // Check for unique name
                var existingPromotion = await unitOfWork.Context.Set<Promotion>()
                    .CheckNameIsUniqueAsync<Promotion,Guid>(param.Name, cancellationToken:ct);
                if (existingPromotion.IsError)
                    return existingPromotion.Errors;

                // Check for unique code if provided
                if (!string.IsNullOrEmpty(param.PromotionCode))
                {
                    var existingCode = await unitOfWork.Context.Set<Promotion>()
                        .FirstOrDefaultAsync(p => p.PromotionCode == param.PromotionCode.ToUpperInvariant(), ct);
                    if (existingCode != null)
                        return Error.Conflict("Promotion.DuplicateCode", $"Promotion code '{param.PromotionCode}' already exists");
                }

                // Create promotion action
                var actionResult = Helpers.CreatePromotionAction(param.Action);
                if (actionResult.IsError) return actionResult.Errors;

                var createResult = Promotion.Create(
                    name: param.Name,
                    action: actionResult.Value,
                    code: param.PromotionCode,
                    description: param.Description,
                    minimumOrderAmount: param.MinimumOrderAmount,
                    maximumDiscountAmount: param.MaximumDiscountAmount,
                    startsAt: param.StartsAt,
                    expiresAt: param.ExpiresAt,
                    usageLimit: param.UsageLimit,
                    requiresCouponCode: param.RequiresCouponCode);

                if (createResult.IsError) return createResult.Errors;

                var promotion = createResult.Value;
                promotion.Active = param.Active;

                unitOfWork.Context.Set<Promotion>().Add(promotion);
                await unitOfWork.SaveChangesAsync(ct);

                return mapper.Map<Result>(promotion);
            }
        }
    }
}