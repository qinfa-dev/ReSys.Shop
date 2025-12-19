using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
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
                var stockItem = await unitOfWork.Context.Set<StockItem>()
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id, cancellationToken: ct);

                if (stockItem == null)
                    return StockItem.Errors.NotFound(id: command.Id);

                var deleteResult = stockItem.Delete();
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);
                unitOfWork.Context.Set<StockItem>().Remove(entity: stockItem);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Deleted;
            }
        }
    }
}