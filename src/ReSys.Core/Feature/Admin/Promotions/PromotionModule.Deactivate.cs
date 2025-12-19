using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public static class Deactivate
    {
        public sealed record Command(Guid Id) : ICommand<Success>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(x => x.Id)
                    .NotEmpty().WithMessage("Promotion ID is required.")
                    .WithErrorCode("Promotion.Id.Required");
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork) : ICommandHandler<Command, Success>
        {
            public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
            {
                var promotion = await unitOfWork.Context.Set<Promotion>()
                    .FirstOrDefaultAsync(p => p.Id == command.Id, ct);

                if (promotion == null)
                    return Promotion.Errors.NotFound(command.Id);

                var deactivateResult = promotion.Deactivate();
                if (deactivateResult.IsError) return deactivateResult.Errors;

                await unitOfWork.SaveChangesAsync(ct);
                return Result.Success;
            }
        }
    }
}