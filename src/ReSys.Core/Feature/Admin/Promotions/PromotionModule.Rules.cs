using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static partial class Rules
    {
        public static class Delete
        {
            public sealed record Command(Guid PromotionId, Guid RuleId) : ICommand<Deleted>;

            public sealed class CommandValidator : AbstractValidator<Command>
            {
                public CommandValidator()
                {
                    RuleFor(x => x.PromotionId)
                        .NotEmpty().WithMessage("Promotion ID is required.")
                        .WithErrorCode("Promotion.Id.Required");
                    RuleFor(x => x.RuleId)
                        .NotEmpty().WithMessage("Rule ID is required.")
                        .WithErrorCode("PromotionRule.Id.Required");
                }
            }

            public sealed class CommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Deleted>
            {
                public async Task<ErrorOr<Deleted>> Handle(Command command, CancellationToken ct)
                {
                    var promotion = await unitOfWork.Context.Set<Promotion>()
                        .Include(p => p.PromotionRules)
                        .FirstOrDefaultAsync(p => p.Id == command.PromotionId, ct);

                    if (promotion == null)
                        return Promotion.Errors.NotFound(command.PromotionId);

                    var removeResult = promotion.RemoveRule(command.RuleId);
                    if (removeResult.IsError) return removeResult.Errors;

                    await unitOfWork.SaveChangesAsync(ct);

                    return Result.Deleted;
                }
            }
        }
    }
}