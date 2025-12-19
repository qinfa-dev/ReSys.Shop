using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    public static class Reserve
    {
        public sealed record Request
        {
            public int Quantity { get; init; }
            public Guid? OrderId { get; init; }
        }

        public sealed class RequestValidator : AbstractValidator<Request>
        {
            public RequestValidator()
            {
                RuleFor(expression: x => x.Quantity)
                    .GreaterThan(valueToCompare: 0)
                    .WithMessage("Quantity must be positive.");
            }
        }

        public sealed record Command(Guid Id, Request Request) : ICommand<Success>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request).SetValidator(validator: new RequestValidator());
            }
        }

        public sealed class CommandHandler(IUnitOfWork unitOfWork)
            : ICommandHandler<Command, Success>
        {
            public async Task<ErrorOr<Success>> Handle(Command command, CancellationToken ct)
            {
                var stockItem = await unitOfWork.Context.Set<StockItem>()
                    .Include(navigationPropertyPath: si => si.StockMovements)
                    .FirstOrDefaultAsync(predicate: si => si.Id == command.Id, cancellationToken: ct);

                if (stockItem == null)
                    return StockItem.Errors.NotFound(id: command.Id);

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var reserveResult = stockItem.Reserve(
                    quantity: command.Request.Quantity,
                    orderId: command.Request.OrderId);

                if (reserveResult.IsError) return reserveResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}