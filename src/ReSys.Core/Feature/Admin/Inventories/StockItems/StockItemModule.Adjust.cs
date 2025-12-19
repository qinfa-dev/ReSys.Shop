using ReSys.Core.Domain.Inventories.Movements;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    public static class Adjust
    {
        public sealed record Request
        {
            public int Quantity { get; init; }
            public string? Reason { get; init; }
        }

        public sealed class RequestValidator : AbstractValidator<Request>
        {
            public RequestValidator()
            {
                RuleFor(expression: x => x.Quantity)
                    .NotEqual(0)
                    .WithMessage("Quantity cannot be zero.");
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

                var adjustResult = stockItem.Adjust(
                    quantity: command.Request.Quantity,
                    originator: StockMovement.MovementOriginator.Adjustment,
                    reason: command.Request.Reason ?? "Manual adjustment");

                if (adjustResult.IsError) return adjustResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}