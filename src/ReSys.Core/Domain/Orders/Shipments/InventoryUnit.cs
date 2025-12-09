using ErrorOr;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Orders.LineItems;
using ReSys.Core.Domain.Orders.Returns;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Orders.Shipments;

/// <summary>
/// Represents a unit of inventory associated with an order line item.
/// Tracks the state of each physical unit as it progresses through fulfillment (on-hand → shipped → returned).
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Each InventoryUnit represents a trackable unit or a block of units for a variant. For bulk items, one unit can represent a quantity > 1. For individually tracked items, N units are created with quantity = 1.
/// </para>
/// 
/// <para>
/// <b>State Machine:</b>
/// An InventoryUnit progresses through these states:
/// <list type="bullet">
/// <item><b>OnHand:</b> Item is in stock at fulfillment location, not yet shipped</item>
/// <item><b>Backordered:</b> Item is not available, customer may be waiting for restock</item>
/// <item><b>Shipped:</b> Item has been fulfilled and sent to customer</item>
/// <item><b>Returned:</b> Item has been returned by customer</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Relationships:</b>
/// <list type="bullet">
/// <item><b>Variant:</b> The product variant being inventoried (may be deleted)</item>
/// <item><b>Order:</b> The order this unit belongs to</item>
/// <item><b>LineItem:</b> The specific line item this unit fulfills</item>
/// <item><b>Shipment:</b> Optional - the shipment that will/has delivered this unit</item>
/// <item><b>StockLocation:</b> Optional - the location holding this inventory for fulfillment</item>
/// <item><b>ReturnItems:</b> Return items created from this unit (for exchange/refund tracking)</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Tax Handling:</b>
/// Removed in this port. Tax calculations are handled at LineItem level, not at unit level.
/// </para>
/// </remarks>
public sealed class InventoryUnit : Aggregate
{
    #region State Enum
    public enum InventoryUnitState
    {
        OnHand = 0,
        Backordered = 1,
        Shipped = 2,
        Returned = 3,
    }
    #endregion

    #region Errors
    public static class Errors
    {


        public static Error CannotReturnFromNonShipped =>
            Error.Validation(
                code: "InventoryUnit.CannotReturnFromNonShipped",
                description: "Can only return inventory units that have been shipped.");

        public static Error AlreadyReturned =>
            Error.Conflict(
                code: "InventoryUnit.AlreadyReturned",
                description: "This inventory unit has already been returned.");

        public static Error InvalidStateTransition(InventoryUnitState from, InventoryUnitState to) =>
            Error.Validation(
                code: "InventoryUnit.InvalidStateTransition",
                description: $"Cannot transition from {from} to {to}.");

        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "InventoryUnit.NotFound",
                description: $"Inventory unit with ID '{id}' was not found.");

        public static Error InvalidQuantity =>
            Error.Validation(
                code: "InventoryUnit.InvalidQuantity",
                description: "Quantity must be positive.");

        public static Error CannotSplitInTerminalState =>
            Error.Validation(
                code: "InventoryUnit.CannotSplitInTerminalState",
                description: "Cannot split a unit that has been shipped or returned.");

        public static Error InvalidSplitQuantity =>
            Error.Validation(
                code: "InventoryUnit.InvalidSplitQuantity",
                description: "Split quantity must be positive and less than the unit's total quantity.");
    }
    #endregion

    #region Properties
    /// <summary>Gets the ID of the product variant for this unit.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets the quantity of items in this unit.</summary>
    public int Quantity { get; set; }

    /// <summary>Gets the optional serial number for individually tracked items.</summary>
    public string? SerialNumber { get; set; }

    /// <summary>Gets a value indicating whether this unit requires individual tracking (e.g., for serialized items).</summary>
    public bool RequiresIndividualTracking { get; set; }

    /// <summary>Gets the ID of the order this unit belongs to.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Gets the ID of the line item this unit fulfills.</summary>
    public Guid LineItemId { get; set; }

    /// <summary>Gets the optional ID of the shipment delivering this unit.</summary>
    public Guid ShipmentId { get; set; }

    /// <summary>Gets the optional ID of the stock location holding this unit.</summary>
    public Guid? StockLocationId { get; set; }

    /// <summary>Gets the current state of this inventory unit in its lifecycle.</summary>
    public InventoryUnitState State { get; set; } = InventoryUnitState.OnHand;

    /// <summary>Gets the timestamp of the last state change.</summary>
    public DateTimeOffset StateChangedAt { get; set; }

    /// <summary>Gets the optional ID of the original return item if this is an exchange unit.</summary>
    public Guid? OriginalReturnItemId { get; set; }
    #endregion

    #region Relationships
    public Variant? Variant { get; set; }
    public Order? Order { get; set; }
    public LineItem? LineItem { get; set; }
    public Shipment Shipment { get; set; } = null!;
    public StockLocation? StockLocation { get; set; }

    /// <summary>Gets the return items created for this unit (for exchange/refund processing).</summary>
    public ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();

    /// <summary>Gets the inventory units that were created as exchanges for returns of this unit.</summary>
    public ICollection<InventoryUnit> ExchangeUnits { get; set; } = new List<InventoryUnit>();

    /// <summary>Gets the return item that triggered this unit's creation (if this is an exchange).</summary>
    public ReturnItem? OriginalReturnItem { get; set; }
    #endregion

    #region Constructors
    private InventoryUnit() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates an inventory unit for a line item.
    /// This now creates a single unit with a quantity, rather than multiple instances.
    /// </summary>
    public static ErrorOr<InventoryUnit> Create(
        Guid variantId,
        Guid orderId,
        Guid lineItemId,
        Guid shipmentId,
        int quantity)
    {
        if (quantity <= 0)
        {
            return Errors.InvalidQuantity;
        }

        var unit = new InventoryUnit
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            OrderId = orderId,
            LineItemId = lineItemId,
            ShipmentId = shipmentId,
            Quantity = quantity,
            State = InventoryUnitState.OnHand,
            CreatedAt = DateTimeOffset.UtcNow,
            StateChangedAt = DateTimeOffset.UtcNow
        };

        unit.AddDomainEvent(
            domainEvent: new Events.Created(
                InventoryUnitId: unit.Id,
                VariantId: variantId,
                OrderId: orderId,
                LineItemId: lineItemId,
                State: unit.State,
                Quantity: quantity));

        return unit;
    }
    #endregion

    #region Business Logic: State Transitions

    public ErrorOr<InventoryUnit> FillBackorder()
    {
        if (State != InventoryUnitState.Backordered)
        {
            if (State == InventoryUnitState.OnHand)
                return this;

            return Errors.InvalidStateTransition(from: State, to: InventoryUnitState.OnHand);
        }

        State = InventoryUnitState.OnHand;
        UpdatedAt = DateTimeOffset.UtcNow;
        StateChangedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.BackorderFilled(
                InventoryUnitId: Id,
                VariantId: VariantId,
                OrderId: OrderId));

        return this;
    }

    public ErrorOr<InventoryUnit> TransitionToShipped()
    {
        if (State == InventoryUnitState.Shipped)
            return this; // Idempotent

        if (State != InventoryUnitState.OnHand)
            return Errors.InvalidStateTransition(State, InventoryUnitState.Shipped);
        
        State = InventoryUnitState.Shipped;
        UpdatedAt = DateTimeOffset.UtcNow;
        StateChangedAt = DateTimeOffset.UtcNow;
        
        AddDomainEvent(
            domainEvent: new Events.Shipped(
                InventoryUnitId: Id,
                VariantId: VariantId,
                OrderId: OrderId,
                ShipmentId: ShipmentId));

        return this;
    }

    public ErrorOr<InventoryUnit> Return()
    {
        if (State == InventoryUnitState.Returned)
            return Errors.AlreadyReturned;

        if (State != InventoryUnitState.Shipped)
            return Errors.CannotReturnFromNonShipped;

        State = InventoryUnitState.Returned;
        UpdatedAt = DateTimeOffset.UtcNow;
        StateChangedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.Returned(
                InventoryUnitId: Id,
                VariantId: VariantId,
                OrderId: OrderId));

        return this;
    }
    #endregion

    #region Business Logic: Inventory Splitting
    public ErrorOr<InventoryUnit> Split(int extractQuantity)
    {
        if (IsInTerminalState)
            return Errors.CannotSplitInTerminalState;

        if (extractQuantity <= 0 || extractQuantity >= Quantity)
            return Errors.InvalidSplitQuantity;

        var newUnit = new InventoryUnit
        {
            Id = Guid.NewGuid(),
            VariantId = VariantId,
            OrderId = OrderId,
            LineItemId = LineItemId,
            Quantity = extractQuantity,
            ShipmentId = ShipmentId,
            StockLocationId = StockLocationId,
            State = State,
            SerialNumber = null, 
            RequiresIndividualTracking = RequiresIndividualTracking,
            OriginalReturnItemId = OriginalReturnItemId,
            CreatedAt = DateTimeOffset.UtcNow,
            StateChangedAt = DateTimeOffset.UtcNow
        };

        var remainingQuantity = Quantity - extractQuantity;
        Quantity = remainingQuantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.Split(
                OriginalUnitId: Id,
                NewUnitId: newUnit.Id,
                OriginalQuantity: Quantity + extractQuantity,
                ExtractedQuantity: extractQuantity,
                RemainingQuantity: remainingQuantity));

        return newUnit;
    }
    #endregion

    #region Business Logic: Stock Location Management
    public ErrorOr<InventoryUnit> SetStockLocation(StockLocation stockLocation)
    {
        if (StockLocationId == stockLocation.Id)
            return this; 

        StockLocationId = stockLocation.Id;
        StockLocation = stockLocation;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.StockLocationAssigned(
                InventoryUnitId: Id,
                StockLocationId: stockLocation.Id));

        return this;
    }
    #endregion

    #region Queries
    public ReturnItem? GetCurrentReturnItem()
    {
        return ReturnItems.FirstOrDefault(
            predicate: ri => ri.ReceptionStatus != ReturnItem.ReturnReceptionStatus.Cancelled);
    }
    
    public bool IsInTerminalState => State == InventoryUnitState.Returned;
    #endregion

    #region Domain Events
    public static class Events
    {
        public sealed record Created(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid OrderId,
            Guid LineItemId,
            InventoryUnitState State,
            int Quantity) : DomainEvent;

        public sealed record BackorderFilled(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid OrderId) : DomainEvent;

        public sealed record Shipped(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid OrderId,
            Guid ShipmentId) : DomainEvent;

        public sealed record Returned(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid OrderId) : DomainEvent;

        public sealed record Split(
            Guid OriginalUnitId,
            Guid NewUnitId,
            int OriginalQuantity,
            int ExtractedQuantity,
            int RemainingQuantity) : DomainEvent;

        public sealed record StockLocationAssigned(
            Guid InventoryUnitId,
            Guid StockLocationId) : DomainEvent;
    }



    #endregion

}