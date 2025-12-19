using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Delete
    {
        public sealed record Command(Guid Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("Promotion ID is required.")
                    .WithErrorCode("Promotion.Id.Required");
            }
        }
        public sealed class CommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command command, CancellationToken ct)
            {
                var promotion = await unitOfWork.Context.Set<Promotion>()
                    .Include(p => p.PromotionRules)
                    .FirstOrDefaultAsync(p => p.Id == command.Id, ct);

                if (promotion == null)
                    return Promotion.Errors.NotFound(command.Id);

                // Check if promotion has been used
                if (promotion.UsageCount > 0)
                    return Error.Validation("Promotion.CannotDeleteUsed",
                        "Cannot delete a promotion that has been used. Consider deactivating it instead.");

                unitOfWork.Context.Set<Promotion>().Remove(promotion);
                await unitOfWork.SaveChangesAsync(ct);

                return Result.Deleted;
            }
        }
    }
}