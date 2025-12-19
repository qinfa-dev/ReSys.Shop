using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Orders.LineItems;

namespace ReSys.Core.Domain.Orders.Shipments;

/// <summary>
/// Represents a single unit of inventory allocated to fulfill part of an order line item.
/// Each InventoryUnit tracks one physical item through the fulfillment lifecycle.
/// </summary>
/// <remarks>
/// <para>
/// <b>CORE PRINCIPLE - One Unit = One Physical Item:</b>
/// Following Solidus pattern, each InventoryUnit represents exactly ONE physical unit.
/// For a line item with quantity=5, create 5 separate InventoryUnit instances.
/// This granular tracking enables:
/// • Partial shipments from multiple warehouses
/// • Individual item returns and exchanges
/// • Per-unit state tracking (one backordered, four shipped, etc.)
/// • Split fulfillment scenarios
/// </para>
/// 
/// <para>
/// <b>State Machine:</b>
/// <list type="bullet">
/// <item><b>OnHand:</b> Unit is in stock at warehouse, ready to ship</item>
/// <item><b>Backordered:</b> Unit is not in stock, awaiting replenishment</item>
/// <item><b>Shipped:</b> Unit has been sent to customer</item>
/// <item><b>Returned:</b> Unit has been returned by customer</item>
/// <item><b>Canceled:</b> Unit was canceled before shipment</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Pending vs Finalized:</b>
/// Units start as "pending" when shipments are created. While pending:
/// • Inventory is NOT decremented from stock
/// • Units can be freely modified or removed
/// • Order can still be edited
/// 
/// When order completes, units are "finalized":
/// • Inventory IS decremented from stock
/// • Units become immutable
/// • Changes require return/exchange process
/// </para>
/// 
/// <para>
/// <b>Key Relationships:</b>
/// • Variant: The product SKU this unit represents
/// • LineItem: The order line this unit fulfills
/// • Shipment: The shipment containing this unit (provides Order context)
/// • ReturnItems: Return/exchange records for this unit
/// </para>
/// 
/// <para>
/// <b>Example Usage:</b>
/// <code>
/// // Creating units for a line item with quantity=3
/// for (int i = 0; i &lt; 3; i++)
/// {
///     var unitResult = InventoryUnit.Create(
///         variantId: variant.Id,
///         lineItemId: lineItem.Id,
///         shipmentId: shipment.Id,
///         initialState: InventoryUnitState.OnHand);
///     
///     if (unitResult.IsError) return unitResult.FirstError;
///     shipment.InventoryUnits.Add(unitResult.Value);
/// }
/// 
/// // Shipping units
/// foreach (var unit in shipment.InventoryUnits)
/// {
///     var shipResult = unit.Ship();
///     if (shipResult.IsError) return shipResult.FirstError;
/// }
/// 
/// // Returning a unit
/// var returnResult = unit.Return();
/// if (returnResult.IsError) return returnResult.FirstError;
/// </code>
/// </para>
/// </remarks>
public sealed class InventoryUnit : Aggregate
{
    #region Constants

    /// <summary>States before shipment - can be easily modified/canceled.</summary>
    public static readonly InventoryUnitState[] PreShipmentStates =
    {
        InventoryUnitState.OnHand,
        InventoryUnitState.Backordered
    };

    /// <summary>States after shipment - require return process to modify.</summary>
    public static readonly InventoryUnitState[] PostShipmentStates =
    {
        //InventoryUnitState.Returned
    };

    /// <summary>States from which cancellation is allowed.</summary>
    public static readonly InventoryUnitState[] CancelableStates =
    {
        InventoryUnitState.OnHand,
        InventoryUnitState.Backordered,
        InventoryUnitState.Shipped
    };

    #endregion

    #region State Enum

    /// <summary>
    /// Defines the lifecycle states of an inventory unit.
    /// </summary>
    public enum InventoryUnitState
    {
        /// <summary>Unit is in stock at warehouse, ready to ship.</summary>
        OnHand = 0,

        /// <summary>Unit is not in stock, awaiting replenishment.</summary>
        Backordered = 1,

        /// <summary>Unit has been shipped to customer.</summary>
        Shipped = 2,

        ///// <summary>Unit has been returned by customer.</summary>
        //Returned = 3,

        /// <summary>Unit was canceled before shipment.</summary>
        Canceled = 4
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "InventoryUnit.NotFound",
                description: $"Inventory unit with ID '{id}' was not found.");

        public static Error InvalidStateTransition(InventoryUnitState from, InventoryUnitState to) =>
            Error.Validation(
                code: "InventoryUnit.InvalidStateTransition",
                description: $"Cannot transition from {from} to {to}.");

        //public static Error CannotReturnNonShipped =>
        //    Error.Validation(
        //        code: "InventoryUnit.CannotReturnNonShipped",
        //        description: "Can only return inventory units that have been shipped.");

        //public static Error AlreadyReturned =>
        //    Error.Conflict(
        //        code: "InventoryUnit.AlreadyReturned",
        //        description: "This inventory unit has already been returned.");

        public static Error AlreadyCanceled =>
            Error.Conflict(
                code: "InventoryUnit.AlreadyCanceled",
                description: "This inventory unit has already been canceled.");

        public static Error CannotCancelInState(InventoryUnitState state) =>
            Error.Validation(
                code: "InventoryUnit.CannotCancelInState",
                description: $"Cannot cancel inventory unit in {state} state.");

        public static Error CannotDestroyInState(InventoryUnitState state) =>
            Error.Validation(
                code: "InventoryUnit.CannotDestroyInState",
                description: $"Cannot destroy inventory unit in {state} state. Only backordered or on_hand units can be destroyed.");

        public static Error ShipmentRequired =>
            Error.Validation(
                code: "InventoryUnit.ShipmentRequired",
                description: "Shipment is required for inventory unit.");

        public static Error LineItemRequired =>
            Error.Validation(
                code: "InventoryUnit.LineItemRequired",
                description: "Line item is required for inventory unit.");

        public static Error VariantRequired =>
            Error.Validation(
                code: "InventoryUnit.VariantRequired",
                description: "Variant is required for inventory unit.");
    }

    #endregion

    #region Properties - Core Identity

    /// <summary>
    /// Foreign key to the product variant this unit represents.
    /// The actual product SKU being fulfilled.
    /// </summary>
    public Guid VariantId { get; set;}

    /// <summary>
    /// Foreign key to the line item this unit fulfills.
    /// Links this unit back to the customer's order line.
    /// </summary>
    public Guid LineItemId { get; set;}

    /// <summary>
    /// Foreign key to the shipment containing this unit.
    /// CRITICAL: This provides the Order context (unit.Shipment.Order).
    /// Matches Solidus pattern where Order is not a direct FK.
    /// </summary>
    public Guid? ShipmentId { get; set;}

    #endregion

    #region Properties - Fulfillment Tracking

    /// <summary>
    /// Current state of this unit in the fulfillment lifecycle.
    /// Drives business logic and state transitions.
    /// </summary>
    public InventoryUnitState State { get; set;} = InventoryUnitState.OnHand;

    /// <summary>
    /// Indicates whether this unit is pending finalization.
    /// TRUE = inventory not yet decremented, unit can be freely modified
    /// FALSE = inventory decremented, unit is committed
    /// Set to false when shipment finalizes (on order completion).
    /// </summary>
    public bool Pending { get; set;} = true;

    /// <summary>
    /// Timestamp of the last state change.
    /// Used for auditing and tracking fulfillment timeline.
    /// </summary>
    public DateTimeOffset? StateChangedAt { get; set;}

    #endregion

    #region Properties - Exchange Tracking

    /// <summary>
    /// Foreign key to the return item that created this unit as an exchange.
    /// Null for original purchase units.
    /// Set for exchange units to track: original unit → return → new exchange unit.
    /// </summary>
    public Guid? OriginalReturnItemId { get; set;}

    #endregion

    #region Relationships

    /// <summary>The product variant being fulfilled (may be soft-deleted).</summary>
    public Variant? Variant { get; set; }

    /// <summary>The line item this unit fulfills.</summary>
    public LineItem? LineItem { get; set; }

    /// <summary>The shipment containing/delivering this unit.</summary>
    public Shipment? Shipment { get; set; }

    ///// <summary>Return items created from this unit (for returns/exchanges).</summary>
    //public ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();

    ///// <summary>The return item that created this as an exchange unit.</summary>
    //public ReturnItem? OriginalReturnItem { get; set; }

    #endregion

    #region Computed Properties - Convenience Accessors

    /// <summary>Gets the order this unit belongs to (through shipment navigation).</summary>
    public Order? Order => Shipment?.Order;

    /// <summary>Gets the order ID (through shipment).</summary>
    public Guid? OrderId => Shipment?.OrderId;

    /// <summary>Gets the stock location fulfilling this unit (through shipment).</summary>
    public Guid? StockLocationId => Shipment?.StockLocationId;

    /// <summary>Gets the stock location entity (through shipment).</summary>
    public StockLocation? StockLocation => Shipment?.StockLocation;

    #endregion

    #region Computed Properties - State Queries

    /// <summary>Is this unit in a pre-shipment state (can be easily modified)?</summary>
    public bool IsPreShipment => PreShipmentStates.Contains(State);

    /// <summary>Is this unit in a post-shipment state (requires return process)?</summary>
    public bool IsPostShipment => PostShipmentStates.Contains(State);

    /// <summary>Can this unit be canceled?</summary>
    public bool IsCancelable => CancelableStates.Contains(State) && !Pending;

    /// <summary>Is this unit ready to ship?</summary>
    public bool IsShippable => State == InventoryUnitState.OnHand;

    /// <summary>Is this unit backordered?</summary>
    public bool IsBackordered => State == InventoryUnitState.Backordered;

    /// <summary>Has this unit been shipped?</summary>
    public bool IsShipped => State == InventoryUnitState.Shipped;

    ///// <summary>Has this unit been returned?</summary>
    //public bool IsReturned => State == InventoryUnitState.Returned;

    /// <summary>Is this unit canceled?</summary>
    public bool IsCanceled => State == InventoryUnitState.Canceled;

    /// <summary>Can this unit be shipped (business rule check)?</summary>
    public bool AllowShip => State == InventoryUnitState.OnHand;

    #endregion

    #region Computed Properties - Return/Exchange Queries

    ///// <summary>
    ///// Does any non-expired return item for this unit request an exchange?
    ///// Used to determine if unit needs replacement fulfillment.
    ///// </summary>
    //public bool HasExchangeRequested =>
    //    ReturnItems.Any(ri => ri.IsExchangeRequested && !ri.IsExpired);

    ///// <summary>
    ///// Gets the active (non-cancelled) return item for this unit.
    ///// Returns null if no active return exists.
    ///// </summary>
    //public ReturnItem? CurrentReturnItem =>
    //    ReturnItems.FirstOrDefault(ri => !ri.IsCancelled);

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor to enforce factory method usage.
    /// </summary>
    private InventoryUnit() { }

    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new inventory unit for a line item.
    /// </summary>
    /// <param name="variantId">Product variant being fulfilled</param>
    /// <param name="lineItemId">Line item this unit fulfills</param>
    /// <param name="shipmentId">Shipment containing this unit</param>
    /// <param name="initialState">Starting state (OnHand or Backordered typically)</param>
    /// <param name="pending">Whether unit is pending finalization (default true)</param>
    /// <returns>ErrorOr containing the created unit</returns>
    /// <remarks>
    /// IMPORTANT: Create one unit per physical item.
    /// For quantity=5, call this method 5 times.
    /// 
    /// Example:
    /// <code>
    /// // For a line item with quantity 3
    /// for (int i = 0; i &lt; lineItem.Quantity; i++)
    /// {
    ///     var result = InventoryUnit.Create(
    ///         variantId: lineItem.VariantId,
    ///         lineItemId: lineItem.Id,
    ///         shipmentId: shipment.Id);
    ///     
    ///     if (result.IsError) return result.FirstError;
    ///     shipment.InventoryUnits.Add(result.Value);
    /// }
    /// </code>
    /// </remarks>
    public static ErrorOr<InventoryUnit> Create(
        Guid variantId,
        Guid lineItemId,
        Guid shipmentId,
        InventoryUnitState initialState = InventoryUnitState.OnHand,
        bool pending = true)
    {
        var unit = new InventoryUnit
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            LineItemId = lineItemId,
            ShipmentId = shipmentId,
            State = initialState,
            Pending = pending,
            CreatedAt = DateTimeOffset.UtcNow,
            UpdatedAt = DateTimeOffset.UtcNow,
            StateChangedAt = DateTimeOffset.UtcNow
        };

        unit.AddDomainEvent(new Events.Created(
            InventoryUnitId: unit.Id,
            VariantId: variantId,
            LineItemId: lineItemId,
            ShipmentId: shipmentId,
            State: initialState,
            Pending: pending));

        return unit;
    }

    /// <summary>
    /// Creates an exchange inventory unit from a return item.
    /// Used when customer exchanges returned product for a different item.
    /// </summary>
    /// <param name="variantId">New variant being sent as exchange</param>
    /// <param name="lineItemId">Line item for the exchange</param>
    /// <param name="shipmentId">Shipment for the exchange</param>
    /// <param name="originalReturnItemId">Return item that triggered this exchange</param>
    /// <param name="initialState">Starting state (typically OnHand)</param>
    /// <returns>ErrorOr containing the exchange unit</returns>
    /// <remarks>
    /// Exchange flow:
    /// 1. Customer returns item → ReturnItem created
    /// 2. Customer selects exchange variant
    /// 3. This method creates new InventoryUnit for exchange
    /// 4. New unit links back to original return via OriginalReturnItemId
    /// </remarks>
    public static ErrorOr<InventoryUnit> CreateForExchange(
        Guid variantId,
        Guid lineItemId,
        Guid shipmentId,
        Guid originalReturnItemId,
        InventoryUnitState initialState = InventoryUnitState.OnHand)
    {
        var result = Create(variantId, lineItemId, shipmentId, initialState, pending: true);
        if (result.IsError) return result;

        var unit = result.Value;
        unit.OriginalReturnItemId = originalReturnItemId;

        unit.AddDomainEvent(new Events.ExchangeUnitCreated(
            InventoryUnitId: unit.Id,
            OriginalReturnItemId: originalReturnItemId,
            VariantId: variantId));

        return unit;
    }

    #endregion

    #region Business Logic - State Transitions

    /// <summary>
    /// Fills a backordered unit when stock becomes available.
    /// Transitions: Backordered → OnHand
    /// Idempotent: Returns success if already OnHand.
    /// </summary>
    /// <returns>ErrorOr containing this unit or validation error</returns>
    /// <remarks>
    /// Called by inventory management service when:
    /// • New stock arrives at warehouse
    /// • Stock becomes available from another order cancellation
    /// • Inventory reallocation occurs
    /// 
    /// This does NOT decrement inventory - that happens on Finalize().
    /// </remarks>
    public ErrorOr<InventoryUnit> FillBackorder()
    {
        if (State == InventoryUnitState.OnHand)
            return this; // Already filled, idempotent

        if (State != InventoryUnitState.Backordered)
            return Errors.InvalidStateTransition(State, InventoryUnitState.OnHand);

        State = InventoryUnitState.OnHand;
        UpdatedAt = DateTimeOffset.UtcNow;
        StateChangedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.BackorderFilled(
            InventoryUnitId: Id,
            VariantId: VariantId,
            ShipmentId: ShipmentId));

        return this;
    }

    /// <summary>
    /// Transitions the unit to Backordered state.
    /// </summary>
    /// <returns>ErrorOr containing this unit or validation error</returns>
    public ErrorOr<InventoryUnit> ToBackordered()
    {
        if (State == InventoryUnitState.Backordered)
            return this; // Already backordered, idempotent

        if (State != InventoryUnitState.OnHand)
            return Errors.InvalidStateTransition(State, InventoryUnitState.Backordered);

        State = InventoryUnitState.Backordered;
        UpdatedAt = DateTimeOffset.UtcNow;
        StateChangedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.MovedToBackordered(
            InventoryUnitId: Id,
            VariantId: VariantId,
            ShipmentId: ShipmentId));

        return this;
    }

    /// <summary>
    /// Ships the unit (marks as sent to customer).
    /// Transitions: OnHand/Backordered → Shipped
    /// Automatically finalizes the unit (sets Pending = false).
    /// Called by OrderShipping service when unit is packaged and shipped.
    /// </summary>
    /// <returns>ErrorOr containing this unit or validation error</returns>
    /// <remarks>
    /// Shipping triggers:
    /// • Pending → false (inventory decremented)
    /// • State → Shipped
    /// • Carrier tracking begins
    /// 
    /// Can ship from Backordered state for drop-ship scenarios where
    /// item ships directly from supplier without entering warehouse.
    /// </remarks>
    public ErrorOr<InventoryUnit> Ship()
    {
        if (State == InventoryUnitState.Shipped)
            return this; // Already shipped, idempotent

        if (State == InventoryUnitState.Canceled)
            return Errors.InvalidStateTransition(State, InventoryUnitState.Shipped);

        // Can ship from OnHand or Backordered (for drop-ship scenarios)
        if (State != InventoryUnitState.OnHand && State != InventoryUnitState.Backordered)
            return Errors.InvalidStateTransition(State, InventoryUnitState.Shipped);

        State = InventoryUnitState.Shipped;
        Pending = false; // Shipping finalizes the unit
        UpdatedAt = DateTimeOffset.UtcNow;
        StateChangedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Shipped(
            InventoryUnitId: Id,
            VariantId: VariantId,
            ShipmentId: ShipmentId));

        return this;
    }

    ///// <summary>
    ///// Returns the unit (customer sent it back).
    ///// Transitions: Shipped → Returned
    ///// Only shipped units can be returned.
    ///// </summary>
    ///// <returns>ErrorOr containing this unit or validation error</returns>
    ///// <remarks>
    ///// Return process:
    ///// 1. Customer initiates return → ReturnItem created
    ///// 2. Item received at warehouse → ReturnItem.Receive()
    ///// 3. Item accepted → ReturnItem.Accept()
    ///// 4. This method called → Unit marked as Returned
    ///// 5. If resellable → Inventory restored (via domain event)
    ///// 
    ///// Only shipped units can be returned. Cannot return:
    ///// • OnHand units (never left warehouse)
    ///// • Backordered units (never fulfilled)
    ///// • Canceled units (never processed)
    ///// </remarks>
    //public ErrorOr<InventoryUnit> Return()
    //{
    //    if (State == InventoryUnitState.Returned)
    //        return Errors.AlreadyReturned;
    //
    //    if (State != InventoryUnitState.Shipped)
    //        return Errors.CannotReturnNonShipped;
    //
    //    State = InventoryUnitState.Returned;
    //    UpdatedAt = DateTimeOffset.UtcNow;
    //    StateChangedAt = DateTimeOffset.UtcNow;
    //
    //    AddDomainEvent(new Events.Returned(
    //        InventoryUnitId: Id,
    //        VariantId: VariantId,
    //        ShipmentId: ShipmentId));
    //
    //    return this;
    //}

    /// <summary>
    /// Cancels the unit before it ships.
    /// Transitions: OnHand/Backordered/Shipped → Canceled
    /// Cannot cancel if already Returned or Canceled.
    /// Automatically finalizes (sets Pending = false).
    /// </summary>
    /// <returns>ErrorOr containing this unit or validation error</returns>
    /// <remarks>
    /// Cancellation scenarios:
    /// • Customer cancels order before shipment
    /// • Item damaged in warehouse
    /// • Stock discrepancy discovered
    /// • Order fraudulent/declined payment
    /// 
    /// Can cancel Shipped units if caught before carrier pickup.
    /// Once in carrier's hands, must process return instead.
    /// 
    /// Cancellation releases inventory reservation (via domain event).
    /// </remarks>
    public ErrorOr<InventoryUnit> Cancel()
    {
        if (State == InventoryUnitState.Canceled)
            return Errors.AlreadyCanceled;

        if (!IsCancelable)
            return Errors.CannotCancelInState(State);

        State = InventoryUnitState.Canceled;
        Pending = false; // Cancellation finalizes the unit
        UpdatedAt = DateTimeOffset.UtcNow;
        StateChangedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Canceled(
            InventoryUnitId: Id,
            VariantId: VariantId,
            ShipmentId: ShipmentId));

        return this;
    }

    /// <summary>
    /// Changes the shipment for this inventory unit.
    /// </summary>
    /// <param name="newShipmentId">The ID of the new shipment.</param>
    /// <returns>ErrorOr containing this unit or validation error</returns>
    public ErrorOr<InventoryUnit> ChangeShipment(Guid newShipmentId)
    {
        if (newShipmentId == Guid.Empty)
        {
            return Error.Validation("InventoryUnit.NewShipmentIdRequired", "New shipment ID is required.");
        }

        if (newShipmentId == ShipmentId)
        {
            return this; // Idempotent
        }

        var oldShipmentId = ShipmentId;
        ShipmentId = newShipmentId;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.ShipmentChanged(
            InventoryUnitId: Id,
            OldShipmentId: oldShipmentId,
            NewShipmentId: newShipmentId));

        return this;
    }

    #endregion

    #region Business Logic - Finalization

    /// <summary>
    /// Finalizes the unit (commits inventory decrement).
    /// Sets Pending = false, causing inventory to be decremented from stock.
    /// Called when shipment finalizes on order completion.
    /// Idempotent: Returns success if already finalized.
    /// </summary>
    /// <returns>ErrorOr containing this unit or validation error</returns>
    /// <remarks>
    /// Finalization happens when:
    /// • Order completes (all payments processed)
    /// • Shipment is finalized
    /// • Unit is shipped (auto-finalizes)
    /// • Unit is canceled (auto-finalizes)
    /// 
    /// Before finalization:
    /// • Inventory is reserved but not decremented
    /// • Changes are reversible
    /// • Can be freely removed from shipment
    /// 
    /// After finalization:
    /// • Inventory is decremented from stock
    /// • Changes require formal process (returns/exchanges)
    /// • Permanent record created
    /// </remarks>
    public ErrorOr<InventoryUnit> FinalizeUnit()
    {
        if (!Pending)
            return this; // Already finalized, idempotent

        Pending = false;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Finalized(
            InventoryUnitId: Id,
            VariantId: VariantId,
            ShipmentId: ShipmentId));

        return this;
    }

    #endregion

    #region Business Logic - Validation

    /// <summary>
    /// Validates whether this unit can be safely destroyed.
    /// Can only destroy units that are:
    /// - In Backordered or OnHand state
    /// - Not yet finalized (Pending = true)
    /// 
    /// This prevents deleting units that have already affected inventory
    /// or are part of completed fulfillment.
    /// </summary>
    /// <returns>ErrorOr Success or validation error</returns>
    /// <remarks>
    /// Destruction scenarios:
    /// • Removing items from cart before checkout
    /// • Canceling entire shipment before finalization
    /// • Rebuilding shipments due to inventory changes
    /// 
    /// Cannot destroy:
    /// • Finalized units (inventory already decremented)
    /// • Shipped units (already sent to customer)
    /// • Returned units (part of return record)
    /// • Canceled units (audit trail required)
    /// 
    /// Validation of shipment state should be done by application service.
    /// </remarks>
    public ErrorOr<Success> ValidateCanDestroy()
    {
        // Can only destroy backordered or on_hand units
        if (State != InventoryUnitState.Backordered && State != InventoryUnitState.OnHand)
            return Errors.CannotDestroyInState(State);

        // Cannot destroy finalized units
        if (!Pending)
        {
            return Error.Validation(
                code: "InventoryUnit.CannotDestroyFinalized",
                description: "Cannot destroy finalized inventory units.");
        }

        return Result.Success;
    }

    #endregion

    #region Domain Events

    /// <summary>
    /// Domain events published by InventoryUnit aggregate.
    /// These events enable decoupled communication with other bounded contexts:
    /// • Inventory context: Stock management, reservations, decrements
    /// • Notification context: Customer updates, warehouse alerts
    /// • Analytics context: Fulfillment metrics, backorder tracking
    /// </summary>
    public static class Events
    {
        /// <summary>Published when a new inventory unit is created.</summary>
        public sealed record Created(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid LineItemId,
            Guid? ShipmentId,
            InventoryUnitState State,
            bool Pending) : DomainEvent;

        /// <summary>Published when an exchange unit is created from a return.</summary>
        public sealed record ExchangeUnitCreated(
            Guid InventoryUnitId,
            Guid? OriginalReturnItemId,
            Guid VariantId) : DomainEvent;

        /// <summary>Published when a backordered unit is filled with stock.</summary>
        public sealed record BackorderFilled(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid? ShipmentId) : DomainEvent;

        /// <summary>Published when a unit is moved to backordered state.</summary>
        public sealed record MovedToBackordered(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid? ShipmentId) : DomainEvent;

        /// <summary>Published when a unit is shipped to customer.</summary>
        public sealed record Shipped(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid? ShipmentId) : DomainEvent;

        ///// <summary>Published when a unit is returned by customer.</summary>
        //public sealed record Returned(
        //    Guid InventoryUnitId,
        //    Guid VariantId,
        //    Guid ShipmentId) : DomainEvent;

        /// <summary>Published when a unit is canceled before shipment.</summary>
        public sealed record Canceled(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid? ShipmentId) : DomainEvent;

        /// <summary>
        /// Published when a unit is finalized (inventory decremented).
        /// Handled by Inventory bounded context to decrement stock.
        /// </summary>
        public sealed record Finalized(
            Guid InventoryUnitId,
            Guid VariantId,
            Guid? ShipmentId) : DomainEvent;

        /// <summary>Published when an inventory unit is moved to a new shipment.</summary>
        public sealed record ShipmentChanged(
            Guid InventoryUnitId,
            Guid? OldShipmentId,
            Guid? NewShipmentId) : DomainEvent;
    }

    #endregion
}