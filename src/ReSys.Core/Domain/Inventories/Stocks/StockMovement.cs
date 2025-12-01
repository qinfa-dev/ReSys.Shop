using ErrorOr;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Inventories.Stocks;

public sealed class StockMovement : AuditableEntity<Guid>
{
    #region Enums
    public enum MovementOriginator
    {
        StockTransfer = 0,
        Order = 1,
        Return = 2,
        Damage = 3,
        Loss = 4,
        Found = 5,
        Promotion = 6,
        Adjustment = 7,
        Recount = 8,
        Shipment = 9,
        Supplier = 10,
        Customer = 11
    }

    public enum MovementAction
    {
        Received = 0,
        Sold = 1,
        Returned = 2,
        Damaged = 3,
        Lost = 4,
        Adjustment = 5,
        Reserved = 6,
        Released = 7
    }

    public enum AdjustmentType
    {
        Increase = 0,
        Decrease = 1
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error InvalidQuantity => Error.Validation(code: "StockMovement.InvalidQuantity", description: "Quantity cannot be zero.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "StockMovement.NotFound", description: $"Stock movement with ID '{id}' was not found.");
    }
    #endregion

    #region Properties
    public Guid StockItemId { get; set; }
    public int Quantity { get; set; }
    public MovementOriginator Originator { get; set; }
    public MovementAction Action { get; set; }
    public string? Reason { get; set; }
    public Guid? StockTransferId { get; set; }
    #endregion

    #region Relationships
    public StockItem StockItem { get; set; } = null!;
    public StockTransfer? StockTransfer { get; set; }
    #endregion

    #region Computed Properties
    public bool IsIncrease => Quantity > 0;
    public bool IsDecrease => Quantity < 0;
    #endregion

    #region Constructors
    private StockMovement() { }
    #endregion

    #region Factory Methods
    public static ErrorOr<StockMovement> Create(StockItem stockItem, int quantity, MovementOriginator originator, MovementAction action, string? reason = null, Guid? stockTransferId = null)
    {
        if (quantity == 0) return Errors.InvalidQuantity;

        return new StockMovement
        {
            Id = Guid.NewGuid(),
            StockItemId = stockItem.Id,
            Quantity = quantity,
            Originator = originator,
            Action = action,
            Reason = reason,
            StockTransferId = stockTransferId,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion
}

