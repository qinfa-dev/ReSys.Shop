using MapsterMapper;

using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Update
    {
        public record Request : Models.Parameter;
        public record Result : Models.ListItem;
        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("Promotion ID is required.")
                    .WithErrorCode("Promotion.Id.Required");
                RuleFor(x => x.Request).SetValidator(new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var param = command.Request;
                var promotion = await unitOfWork.Context.Set<Promotion>()
                    .FirstOrDefaultAsync(p => p.Id == command.Id,
                        ct);

                if (promotion == null)
                    return Promotion.Errors.NotFound(command.Id);

                var uniqueNameCheck = await unitOfWork.Context.Set<Promotion>()
                    .CheckNameIsUniqueAsync<Promotion, Guid>(name: param.Name,
                        prefix: nameof(Promotion),
                        cancellationToken: ct,
                        [
                            promotion.Id
                        ]);
                if (uniqueNameCheck.IsError)
                    return uniqueNameCheck.Errors;

                // Check for unique code
                if (!string.IsNullOrEmpty(param.PromotionCode))
                {
                    var duplicate = await unitOfWork.Context.Set<Promotion>()
                        .FirstOrDefaultAsync(p =>
                                p.PromotionCode == param.PromotionCode.ToUpperInvariant() && p.Id != command.Id,
                            ct);
                    if (duplicate != null)
                        return Error.Conflict("Promotion.DuplicateCode",
                            $"Promotion code '{param.PromotionCode}' already exists");
                }

                // Create new promotion action
                var actionResult = Helpers.CreatePromotionAction(param.Action);
                if (actionResult.IsError) return actionResult.Errors;

                var updateResult = promotion.Update(
                    name: param.Name,
                    code: param.PromotionCode,
                    description: param.Description,
                    action: actionResult.Value,
                    minimumOrderAmount: param.MinimumOrderAmount,
                    maximumDiscountAmount: param.MaximumDiscountAmount,
                    startsAt: param.StartsAt,
                    expiresAt: param.ExpiresAt,
                    usageLimit: param.UsageLimit,
                    active: param.Active,
                    requiresCouponCode: param.RequiresCouponCode);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(ct);
                return mapper.Map<Result>(promotion);
            }
        }
    }
}