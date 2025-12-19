using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    public static class Release
    {
        public sealed record Request
        {
            public int Quantity { get; init; }
            public Guid OrderId { get; init; }
        }

        public sealed class RequestValidator : AbstractValidator<Request>
        {
            public RequestValidator()
            {
                RuleFor(expression: x => x.Quantity)
                    .GreaterThan(valueToCompare: 0)
                    .WithErrorCode(StockItem.Errors.InvalidQuantity.Code)
                    .WithErrorCode(StockItem.Errors.InvalidQuantity.Description);

                var idRequired = CommonInput.Errors.Required(nameof(StockItem), nameof(Request.OrderId));
                RuleFor(expression: x => x.OrderId)
                    .NotEmpty()
                    .WithErrorCode(idRequired.Code)
                    .WithMessage(idRequired.Description);
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

                var releaseResult = stockItem.Release(
                    quantity: command.Request.Quantity,
                    orderId: command.Request.OrderId);

                if (releaseResult.IsError) return releaseResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Success;
            }
        }
    }
}