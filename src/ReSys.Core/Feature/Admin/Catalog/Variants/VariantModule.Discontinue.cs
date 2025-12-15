using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Catalog.Variants;

public static partial class VariantModule
{
    public static class Discontinue
    {
        public sealed record Command(Guid Id) : ICommand<Success>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Success>
        {
            public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
            {
                var variant = await unitOfWork.Context.Set<Variant>()
                    .FindAsync(keyValues: [command.Id], cancellationToken: ct);

                if (variant == null)
                    return Variant.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var discontinueResult = variant.Discontinue();
                if (discontinueResult.IsError) return discontinueResult.Errors;

                unitOfWork.Context.Set<Variant>().Update(variant);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}