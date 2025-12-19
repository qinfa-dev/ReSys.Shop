using ReSys.Core.Domain.Inventories.StockTransfers;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockTransfers;

public static partial class StockTransferModule
{
    // Delete
    public static class Delete
    {
        public sealed record Command(Guid Id) : ICommand<Deleted>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Deleted>
        {
            public async Task<ErrorOr<Deleted>> Handle(Command command, CancellationToken ct)
            {
                var transfer = await unitOfWork.Context.Set<StockTransfer>()
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id, cancellationToken: ct);

                if (transfer == null)
                    return StockTransfer.Errors.NotFound(id: command.Id);

                var deleteResult = transfer.Delete();
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);
                unitOfWork.Context.Set<StockTransfer>().Remove(entity: transfer);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Deleted;
            }
        }
    }
}