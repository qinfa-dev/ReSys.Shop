using MapsterMapper;

using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    public static class Update
    {
        public record Request : Models.Parameter;
        public record Result : Models.ListItem;
        public sealed record Command(Guid Id, Request Request) : ICommand<Result>;

        public sealed class CommandValidator : AbstractValidator<Command>
        {
            public CommandValidator()
            {
                RuleFor(expression: x => x.Id).NotEmpty();
                RuleFor(expression: x => x.Request).SetValidator(validator: new Models.ParameterValidator());
            }
        }

        public sealed class CommandHandler(
            IUnitOfWork unitOfWork,
            IMapper mapper)
            : ICommandHandler<Command, Result>
        {
            public async Task<ErrorOr<Result>> Handle(Command command, CancellationToken ct)
            {
                var request = command.Request;
                var stockItem = await unitOfWork.Context.Set<StockItem>()
                    .Include(navigationPropertyPath: si => si.StockMovements)
                    .FirstOrDefaultAsync(predicate: si => si.Id == command.Id, cancellationToken: ct);

                if (stockItem == null)
                    return StockItem.Errors.NotFound(id: command.Id);

                // Check for duplicate SKU if SKU changed
                if (stockItem.Sku != request.Sku)
                {
                    var duplicate = await unitOfWork.Context.Set<StockItem>()
                        .AnyAsync(predicate: si => si.Id != command.Id && si.Sku == request.Sku && si.StockLocationId == request.StockLocationId, cancellationToken: ct);
                    if (duplicate)
                        return StockItem.Errors.DuplicateSku(sku: request.Sku, stockLocationId: request.StockLocationId);
                }

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);

                var updateResult = stockItem.Update(
                    variantId: request.VariantId,
                    stockLocationId: request.StockLocationId,
                    sku: request.Sku,
                    backorderable: request.Backorderable,
                    quantityOnHand: request.QuantityOnHand,
                    quantityReserved: request.QuantityReserved,
                    publicMetadata: request.PublicMetadata,
                    privateMetadata: request.PrivateMetadata);

                if (updateResult.IsError) return updateResult.Errors;

                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return mapper.Map<Result>(source: updateResult.Value);
            }
        }
    }
}