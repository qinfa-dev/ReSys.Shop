using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Shipping;

namespace ReSys.Core.Domain.Orders.Shipments;

/// <summary>
/// Represents a shipment - an order's fulfillment package.
/// Tracks the lifecycle from pending through delivery.
/// Associates with a specific warehouse (StockLocation) for multi-warehouse fulfillment.
/// </summary>
public sealed class Shipment : Aggregate
{
    public enum ShipmentState { Pending = 0, Ready = 1, Shipped = 2, Delivered = 3, Canceled = 4 }

    #region Constraints
    public static class Constraints
    {
        public const int NumberMaxLength = 50;
        public const int TrackingNumberMaxLength = 100;
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
    public Guid ShippingMethodId { get; set; }
    public Guid? StockLocationId { get; set; }
    public string Number { get; set; } = string.Empty;
    public ShipmentState State { get; set; } = ShipmentState.Pending;
    public string? TrackingNumber { get; set; }
    public DateTimeOffset? ShippedAt { get; set; }
    public DateTimeOffset? DeliveredAt { get; set; }
    #endregion

    #region Relationships
    public Order Order { get; set; } = null!;
    public ShippingMethod? ShippingMethod { get; set; }
    public StockLocation? StockLocation { get; set; }
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
    /// Creates a new shipment for an order.
    /// Optionally associates with a warehouse for fulfillment.
    /// </summary>
    public static ErrorOr<Shipment> Create(Guid orderId, Guid shippingMethodId, Guid? stockLocationId = null)
    {
        if (orderId == Guid.Empty)
            return Error.Validation(code: "Shipment.InvalidOrder", description: "Order reference is required.");

        if (shippingMethodId == Guid.Empty)
            return Error.Validation(code: "Shipment.InvalidShippingMethod", description: "Shipping method is required.");

        var number = GenerateShipmentNumber();
        if (number.Length > Constraints.NumberMaxLength) 
            return Errors.NumberTooLong;

        var shipment = new Shipment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            ShippingMethodId = shippingMethodId,
            StockLocationId = stockLocationId,
            Number = number,
            State = ShipmentState.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        shipment.AddDomainEvent(domainEvent: new Events.ShipmentCreated(
            ShipmentId: shipment.Id,
            OrderId: orderId,
            StockLocationId: stockLocationId));

        return shipment;
    }
    #endregion

    #region Business Logic - Warehouse Assignment

    /// <summary>
    /// Assigns a warehouse (stock location) to this shipment.
    /// Must be done before marking as ready or shipping.
    /// </summary>
    public ErrorOr<Shipment> AssignStockLocation(Guid stockLocationId)
    {
        if (stockLocationId == Guid.Empty)
            return Errors.InvalidStockLocation;

        if (State != ShipmentState.Pending)
            return Errors.CannotAssignLocationAfterReady;

        StockLocationId = stockLocationId;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(domainEvent: new Events.ShipmentStockLocationAssigned(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: stockLocationId));

        return this;
    }

    #endregion

    #region Business Logic - State Transitions

    /// <summary>
    /// Marks the shipment as ready for pickup/handoff.
    /// Warehouse location must be assigned before this.
    /// </summary>
    public ErrorOr<Shipment> Ready()
    {
        if (State != ShipmentState.Pending)
            return Error.Validation(code: "Shipment.NotPending", description: "Shipment must be pending to mark as ready.");

        if (!StockLocationId.HasValue || StockLocationId == Guid.Empty)
            return Errors.StockLocationRequired;

        State = ShipmentState.Ready;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(domainEvent: new Events.ShipmentReady(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId.Value));

        return this;
    }

    /// <summary>
    /// Ships the shipment and records the tracking number.
    /// Warehouse location must be assigned before shipping.
    /// </summary>
    public ErrorOr<Shipment> Ship(string? trackingNumber = null)
    {
        if (State == ShipmentState.Shipped) 
            return this; // Idempotent

        if (State == ShipmentState.Canceled)
            return Error.Validation(code: "Shipment.AlreadyCanceled", description: "Cannot ship canceled shipment.");

        if (!StockLocationId.HasValue || StockLocationId == Guid.Empty)
            return Errors.StockLocationRequired;

        if (trackingNumber != null && trackingNumber.Length > Constraints.TrackingNumberMaxLength)
            return Errors.TrackingNumberTooLong;

        State = ShipmentState.Shipped;
        ShippedAt = DateTimeOffset.UtcNow;
        TrackingNumber = trackingNumber;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(domainEvent: new Events.ShipmentShipped(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId.Value,
            TrackingNumber: trackingNumber));

        return this;
    }

    /// <summary>
    /// Marks the shipment as delivered to customer.
    /// Must be shipped first.
    /// </summary>
    public ErrorOr<Shipment> Deliver()
    {
        if (State != ShipmentState.Shipped)
            return Error.Validation(code: "Shipment.NotShipped", description: "Shipment must be shipped before delivery.");

        if (State == ShipmentState.Delivered)
            return this; // Idempotent

        State = ShipmentState.Delivered;
        DeliveredAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(domainEvent: new Events.ShipmentDelivered(
            ShipmentId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId));

        return this;
    }

    /// <summary>
    /// Cancels the shipment.
    /// Cannot cancel if already shipped.
    /// </summary>
    public ErrorOr<Shipment> Cancel()
    {
        if (State == ShipmentState.Shipped)
            return Errors.CannotCancelShipped;

        if (State == ShipmentState.Canceled)
            return this; // Idempotent

        State = ShipmentState.Canceled;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(domainEvent: new Events.ShipmentCanceled(
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
        if (string.IsNullOrWhiteSpace(value: trackingNumber))
            return Error.Validation(code: "Shipment.TrackingNumberRequired",
                description: "Tracking number is required.");

        if (trackingNumber.Length > Constraints.TrackingNumberMaxLength)
            return Errors.TrackingNumberTooLong;

        TrackingNumber = trackingNumber;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(domainEvent: new Events.ShipmentTrackingUpdated(
            ShipmentId: Id,
            OrderId: OrderId,
            TrackingNumber: trackingNumber,
            StockLocationId: StockLocationId));

        return this;
    }

    #endregion

    #region Helpers
    private static string GenerateShipmentNumber() => $"S{DateTimeOffset.UtcNow:yyyyMMdd}{Random.Shared.Next(minValue: 1000, maxValue: 9999)}";
    #endregion

    #region Events
    public static class Events
    {
        public sealed record ShipmentCreated(Guid ShipmentId, Guid OrderId, Guid? StockLocationId) : DomainEvent;
        public sealed record ShipmentReady(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        public sealed record ShipmentStockLocationAssigned(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        public sealed record ShipmentShipped(Guid ShipmentId, Guid OrderId, Guid StockLocationId, string? TrackingNumber) : DomainEvent;
        public sealed record ShipmentDelivered(Guid ShipmentId, Guid OrderId, Guid? StockLocationId) : DomainEvent;
        public sealed record ShipmentCanceled(Guid ShipmentId, Guid OrderId, Guid? StockLocationId) : DomainEvent;
        public sealed record ShipmentTrackingUpdated(Guid ShipmentId, Guid OrderId, string TrackingNumber, Guid? StockLocationId) : DomainEvent;
    }
    #endregion
}