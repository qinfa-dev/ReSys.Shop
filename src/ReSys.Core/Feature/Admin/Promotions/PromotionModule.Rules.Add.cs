using MapsterMapper;

using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.Promotions.Rules;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Rules
    {
        public static class Add
        {
            public sealed record Request : Models.RuleParameter;
            public sealed record Result : Models.RuleItem;
            public sealed record Command(Guid PromotionId, Request Request) : ICommand<Result>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.PromotionId)
                        .NotEmpty().WithMessage("Promotion ID is required.")
                        .WithErrorCode("Promotion.Id.Required");
                    RuleFor(x => x.Request).SetValidator(new Models.RuleParameterValidator());
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork, IMapper mapper)
                : ICommandHandler<Command, Result>
            {
                public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
                {
                    var promotion = await unitOfWork.Context.Set<Promotion>()
                        .Include(p => p.PromotionRules)
                        .FirstOrDefaultAsync(p => p.Id == command.PromotionId, ct);

                    if (promotion == null)
                        return Promotion.Errors.NotFound(command.PromotionId);

                    var ruleResult = PromotionRule.Create(
                        promotionId: command.PromotionId,
                        type: command.Request.Type,
                        value: command.Request.Value);

                    if (ruleResult.IsError) return ruleResult.Errors;

                    var addResult = promotion.AddRule(ruleResult.Value);
                    if (addResult.IsError) return addResult.Errors;
                    
                    unitOfWork.Context.Set<PromotionRule>().Add(ruleResult.Value);
                    await unitOfWork.SaveChangesAsync(ct);

                    return mapper.Map<Result>(ruleResult.Value);
                }
            }
        }
    }
}