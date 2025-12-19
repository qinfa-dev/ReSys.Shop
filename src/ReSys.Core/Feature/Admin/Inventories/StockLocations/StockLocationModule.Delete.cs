using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Domain.Orders.Shipments;
using ReSys.Core.Domain.Inventories.StockTransfers; // For Shipment and ShipmentStatus
// No need to add ReSys.Core.Domain.Inventories.Locations again
// No need to add ReSys.Core.Domain.Inventories.StockLocations again
// No need for ReSys.Core.Domain.Location.States

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
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
                var location = await unitOfWork.Context.Set<StockLocation>()
                    .Include(navigationPropertyPath: l => l.StockItems)
                    .FirstOrDefaultAsync(predicate: m => m.Id == command.Id,
                        cancellationToken: ct);

                if (location == null)
                    return StockLocation.Errors.NotFound(id: command.Id);

                // Query for hasPendingShipments
                var hasPendingShipments = await unitOfWork.Context.Set<Shipment>()
                    .AnyAsync(s => s.StockLocationId == location.Id && s.State == Shipment.ShipmentState.Pending,
                        ct);

                // Query for hasActiveStockTransfers
                var hasActiveStockTransfers = await unitOfWork.Context.Set<StockTransfer>()
                    .AnyAsync(st => (st.SourceLocationId == location.Id || st.DestinationLocationId == location.Id) &&
                                    (st.State == StockTransfer.StockTransferState.Pending),
                        cancellationToken: ct);

                // Determine hasBackorderedInventoryUnits explicitly from StockItems
                var hasBackorderedInventoryUnits = location.StockItems.Any(si => si.CurrentBackorderQuantity > 0);

                var deleteResult = location.Delete(
                    hasPendingShipments,
                    hasActiveStockTransfers,
                    hasBackorderedInventoryUnits);
                if (deleteResult.IsError) return deleteResult.Errors;

                await unitOfWork.BeginTransactionAsync(cancellationToken: ct);
                await unitOfWork.SaveChangesAsync(cancellationToken: ct);
                await unitOfWork.CommitTransactionAsync(cancellationToken: ct);

                return Result.Deleted;
            }
        }
    }
}
