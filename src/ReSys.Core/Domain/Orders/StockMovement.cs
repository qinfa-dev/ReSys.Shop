using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Inventories.Stocks; // For StockItem

namespace ReSys.Core.Domain.Orders;

/// <summary>
/// Represents a movement of stock for a specific stock item, serving as an audit trail.
/// This entity combines the richness of the original enum-based movement tracking with the
/// flexibility of the Spree-aligned polymorphic originator pattern.
/// </summary>
public sealed class StockMovement : AuditableEntity<Guid>
{
    #region Enums
    /// <summary>
    /// Specifies the type of entity that initiated or is associated with the stock movement.
    /// </summary>
    public enum MovementOriginator
    {
        Undefined = 0, // Default or unknown
        StockTransfer = 1,
        Order = 2,
        Return = 3,
        Damage = 4,
        Loss = 5,
        Found = 6,
        Promotion = 7,
        Adjustment = 8,
        Recount = 9,
        Shipment = 10, // Added to align with new Shipment role
        Supplier = 11,
        Customer = 12
    }

    /// <summary>
    /// Specifies the action or purpose of the stock movement.
    /// </summary>
    public enum MovementAction
    {
        Undefined = 0, // Default or unknown
        Received = 1,
        Sold = 2,
        Returned = 3,
        Damaged = 4,
        Lost = 5,
        Adjustment = 6,
        Reserved = 7,
        Released = 8,
        Allocated = 9, // Added to align with new Shipment workflow
        Picked = 10,
        Packed = 11,
        Shipped = 12 // Simplified from Sold for shipment context
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error InvalidQuantity => Error.Validation(code: "StockMovement.InvalidQuantity", description: "Quantity cannot be zero.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "StockMovement.NotFound", description: $"Stock movement with ID '{id}' was not found.");
        public static Error StockItemRequired => Error.Validation(code: "StockMovement.StockItemRequired", description: "A stock item is required to create a stock movement.");
    }
    #endregion

    #region Properties
    public Guid StockItemId { get; private set; }
    public int Quantity { get; private set; }
    public MovementOriginator Originator { get; private set; }
    public MovementAction Action { get; private set; }
    public string? Reason { get; private set; }

    /// <summary>
    /// Gets the ID of the originating entity (e.g., ShipmentId, ReturnId) if applicable.
    /// Combined with Originator, this forms a polymorphic association.
    /// </summary>
    public Guid? OriginatorId { get; private set; }
    #endregion

    #region Relationships
    public StockItem StockItem { get; set; } = null!;
    #endregion

    #region Computed Properties
    public bool IsIncrease => Quantity > 0;
    public bool IsDecrease => Quantity < 0;
    #endregion

    #region Constructors
    private StockMovement() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new stock movement record.
    /// </summary>
    /// <param name="stockItemId">The ID of the stock item affected.</param>
    /// <param name="quantity">The quantity changed (positive for increase, negative for decrease).</param>
    /// <param name="originator">The type of entity initiating the movement.</param>
    /// <param name="action">The specific action of the movement (e.g., Received, Sold, Adjusted).</param>
    /// <param name="reason">Optional reason for the movement.</param>
    /// <param name="originatorId">Optional ID of the specific entity instance (e.g., ShipmentId).</param>
    public static ErrorOr<StockMovement> Create(
        Guid stockItemId,
        int quantity,
        MovementOriginator originator,
        MovementAction action,
        string? reason = null,
        Guid? originatorId = null)
    {
        if (stockItemId == Guid.Empty)
            return Errors.StockItemRequired;

        if (quantity == 0) return Errors.InvalidQuantity;

        var stockMovement = new StockMovement
        {
            Id = Guid.NewGuid(),
            StockItemId = stockItemId,
            Quantity = quantity,
            Originator = originator,
            Action = action,
            Reason = reason,
            OriginatorId = originatorId,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return stockMovement;
    }
    #endregion
}