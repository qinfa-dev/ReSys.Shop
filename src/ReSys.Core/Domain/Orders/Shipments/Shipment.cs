using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Orders.Shipments;

/// <summary>
/// Represents a shipment, now encompassing the entire fulfillment lifecycle from warehouse operations to customer delivery.
/// This aggregate is aligned with the Spree Commerce model, serving as a single source of truth for shipment state.
/// </summary>
public sealed class Shipment : Aggregate
{
    public enum ShipmentState
    {
        Pending,      // Waiting to process (backorder, unpaid, or just created)
        Ready,        // Can be shipped (stock available, order paid) - ← Replaces "Allocated"
        Picked,       // Items picked from shelves
        Packed,       // Items in box
        ReadyToShip,  // On dock, ready for carrier
        Shipped,      // With carrier
        Delivered,    // Customer received
        Canceled      // Order or shipment canceled
    }

    #region Constraints
    public static class Constraints
    {
        public const int NumberMaxLength = 50;
        public const int TrackingNumberMaxLength = 100;
        public const int PackageIdMaxLength = 255;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error CannotCancelShipped => Error.Validation(code: "Shipment.CannotCancelShipped", description: "Cannot cancel shipped shipment.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "Shipment.NotFound", description: $"Shipment with ID '{id}' was not found.");
        public static Error NumberTooLong => CommonInput.Errors.TooLong(prefix: nameof(Shipment), field: nameof(Number), maxLength: Constraints.NumberMaxLength);
        public static Error TrackingNumberTooLong => CommonInput.Errors.TooLong(prefix: nameof(Shipment), field: nameof(TrackingNumber), maxLength: Constraints.TrackingNumberMaxLength);
        public static Error StockLocationRequired => Error.Validation(code: "Shipment.StockLocationRequired", description: "Stock location must be assigned before shipping.");
        public static Error InvalidStockLocation => Error.Validation(code: "Shipment.InvalidStockLocation", description: "Stock location is required for shipment fulfillment.");
        public static Error CannotAssignLocationAfterReady => Error.Validation(code: "Shipment.CannotAssignLocationAfterReady", description: "Stock location must be assigned before shipment is marked ready.");
    }
    #endregion

    #region Properties
    public Guid OrderId { get; set; }
    public Guid StockLocationId { get; set; }
    public string Number { get; set; } = string.Empty;
    public ShipmentState State { get; set; } = ShipmentState.Pending;
    public string? TrackingNumber { get; set; }

    // Warehouse Workflow Timestamps
    public DateTimeOffset? AllocatedAt { get; set; }
    public DateTimeOffset? PickingStartedAt { get; set; }
    public DateTimeOffset? PickedAt { get; set; }
    public DateTimeOffset? PackedAt { get; set; }
    public DateTimeOffset? ReadyToShipAt { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    public string? PackageId { get; set; }
    #endregion

    #region Relationships
    public Order Order { get; set; } = null!;
    public StockLocation StockLocation { get; set; } = null!;
    public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    #endregion

    #region Computed Properties
    public bool IsShipped => State == ShipmentState.Shipped || State == ShipmentState.Delivered;
    public bool IsDelivered => State == ShipmentState.Delivered;
    public bool IsCanceled => State == ShipmentState.Canceled;
    public bool IsPending => State == ShipmentState.Pending;
    public bool IsReady => State == ShipmentState.Ready;
    #endregion

    #region Constructors
    private Shipment() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new shipment for an order and associates it with a warehouse for fulfillment.
    /// </summary>
    public static ErrorOr<Shipment> Create(Guid orderId, Guid stockLocationId)
    {
        if (orderId == Guid.Empty)
            return Error.Validation(code: "Shipment.InvalidOrder", description: "Order reference is required.");
        
        if (stockLocationId == Guid.Empty)
            return Errors.InvalidStockLocation;

        var number = GenerateShipmentNumber();
        if (number.Length > Constraints.NumberMaxLength)
            return Errors.NumberTooLong;

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            StockLocationId = stockLocationId,
            Number = number,
            State = ShipmentState.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        shipment.AddDomainEvent(new Events.Created(
            ShipmentId: shipment.Id,
            OrderId: orderId,
            StockLocationId: stockLocationId));

        return shipment;
    }
    #endregion

    #region Business Logic - State Transitions

    /// <summary>
    /// Allocates inventory for this shipment.
    /// Verifies all inventory units are on-hand (not backordered) before transitioning to Ready state.
    /// </summary>
    public ErrorOr<Shipment> AllocateInventory()
    {
        if (State != ShipmentState.Pending)
            return Error.Validation(code: "Shipment.InvalidStateForAllocation", description: "Shipment must be in Pending state.");

        if (!InventoryUnits.Any())
            return Error.Validation(code: "Shipment.NoInventoryUnits", description: "Shipment has no inventory units.");

        // Allow partial allocation: as long as at least one unit is OnHand, mark Ready
        var onHandCount = InventoryUnits.Count(u => u.State == InventoryUnit.InventoryUnitState.OnHand);

        if (onHandCount == 0)
        {
            var backordered = InventoryUnits.Count(u => u.State == InventoryUnit.InventoryUnitState.Backordered);
            if (backordered > 0)
            {
                return Error.Validation(
                    code: "Shipment.BackorderedOnly",
                    description: $"Shipment contains only backordered items ({backordered}). No items available to allocate.");
            }

            return Error.Validation(code: "Shipment.NoInventoryUnits", description: "Shipment has no available on-hand items to allocate.");
        }

        State = ShipmentState.Ready;
        AllocatedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Ready(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId));

        return this;
    }

    /// <summary>
    /// Marks the shipment as ready for pickup/handoff.
    /// </summary>
    public ErrorOr<Shipment> Ready()
    {
        if (State != ShipmentState.Pending)
            return Error.Validation(code: "Shipment.NotPending", description: "Shipment must be pending to mark as ready.");

        State = ShipmentState.Ready;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Ready(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId));

        return this;
    }

    /// <summary>
    /// Ships the shipment and records the tracking number.
    /// Updates all inventory units to Shipped state.
    /// </summary>
    public ErrorOr<Shipment> Ship(string? trackingNumber = null)
    {
        if (State == ShipmentState.Shipped)
            return this; // Idempotent

        if (State == ShipmentState.Canceled)
            return Error.Validation(code: "Shipment.AlreadyCanceled", description: "Cannot ship canceled shipment.");
        
        if (trackingNumber != null && trackingNumber.Length > Constraints.TrackingNumberMaxLength)
            return Errors.TrackingNumberTooLong;

        // Transition all inventory units to Shipped state
        foreach (var unit in InventoryUnits)
        {
            var result = unit.TransitionToShipped();
            if (result.IsError) return result.FirstError;
        }

        State = ShipmentState.Shipped;
        ShippedAt = DateTimeOffset.UtcNow;
        TrackingNumber = trackingNumber;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Shipped(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId,
            TrackingNumber: trackingNumber));

        return this;
    }

    /// <summary>
    /// Marks the shipment as delivered to customer.
    /// </summary>
    public ErrorOr<Shipment> Deliver()
    {
        if (State == ShipmentState.Delivered)
            return this; // Idempotent

        if (State != ShipmentState.Shipped)
            return Error.Validation(code: "Shipment.NotShipped", description: "Shipment must be shipped before delivery.");

        State = ShipmentState.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Delivered(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId));

        return this;
    }

    /// <summary>
    /// Cancels the shipment.
    /// </summary>
    public ErrorOr<Shipment> Cancel()
    {
        if (State == ShipmentState.Shipped)
            return Errors.CannotCancelShipped;

        if (State == ShipmentState.Canceled)
            return this; // Idempotent

        State = ShipmentState.Canceled;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Canceled(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId));

        return this;
    }

    #endregion

    #region Business Logic - Tracking

    /// <summary>
    /// Updates the tracking number for this shipment.
    /// </summary>
    public ErrorOr<Shipment> UpdateTrackingNumber(string trackingNumber)
    {
        if (string.IsNullOrWhiteSpace(trackingNumber))
            return Error.Validation(code: "Shipment.TrackingNumberRequired",
                description: "Tracking number is required.");

        if (trackingNumber.Length > Constraints.TrackingNumberMaxLength)
            return Errors.TrackingNumberTooLong;

        TrackingNumber = trackingNumber;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.TrackingUpdated(
            ShipmentId: Id,
            OrderId: OrderId,
            TrackingNumber: trackingNumber,
            StockLocationId: StockLocationId));

        return this;
    }

    #endregion

    #region Helpers
    private static string GenerateShipmentNumber() => $"S{DateTimeOffset.UtcNow:yyyyMMdd}{Random.Shared.Next(1000, 9999)}";
    #endregion

    #region Events
    public static class Events
    {
        public sealed record Created(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        public sealed record Ready(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        public sealed record Shipped(Guid ShipmentId, Guid OrderId, Guid StockLocationId, string? TrackingNumber) : DomainEvent;
        public sealed record Delivered(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        public sealed record Canceled(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        public sealed record TrackingUpdated(Guid ShipmentId, Guid OrderId, string TrackingNumber, Guid StockLocationId) : DomainEvent;
    }
    #endregion
}