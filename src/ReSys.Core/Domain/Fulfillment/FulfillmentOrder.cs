//using ErrorOr;

//using ReSys.Core.Common.Domain.Entities;
//using ReSys.Core.Common.Domain.Events;
//using ReSys.Core.Domain.Fulfillment;
//using ReSys.Core.Domain.Inventories.Locations;
//using ReSys.Core.Domain.Orders;

///// <summary>
///// Represents a fulfillment order, which is a directive to prepare and ship a set of items for a customer order.
///// This aggregate manages the lifecycle of the fulfillment process, from inventory allocation to delivery.
///// </summary>
///// <remarks>
///// <para>
///// <strong>Role in Fulfillment Domain:</strong>
///// The FulfillmentOrder is central to the logistics and shipping process:
///// <list type="bullet">
///// <item>
///// <term>Decouples Orders from Fulfillment</term>
///// <description>Separates the customer order concept from the physical process of shipping.</description>
///// </item>
///// <item>
///// <term>State Machine</term>
///// <description>Manages distinct states from Pending to Delivered, ensuring a controlled workflow.</description>
///// </item>
///// <item>
///// <term>Inventory Coordination</term>
///// <description>Integrates with inventory by reserving and deducting stock via domain events.</description>
///// </item>
///// <item>
///// <term>Tracking</term>
///// <description>Tracks package IDs and shipping tracking numbers.</description>
///// </item>
///// </list>
///// </para>
/////
///// <para>
///// <strong>Fulfillment Workflow:</strong>
///// Typically, an <see cref="Orders.Order"/> triggers the creation of one or more <see cref="FulfillmentOrder"/>s.
///// Each fulfillment order is then processed through a series of states:
///// <c>Pending -> Allocated -> Picking -> Picked -> Packing -> ReadyToShip -> Shipped -> Delivered</c>
///// </para>
///// </remarks>
//public sealed class FulfillmentOrder : Aggregate
//{
//    /// <summary>
//    /// Defines the various states a <see cref="FulfillmentOrder"/> can be in throughout its lifecycle.
//    /// This enumeration represents the finite state machine governing the fulfillment process.
//    /// </summary>
//    public enum FulfillmentState
//    {
//        /// <summary>Initially created, awaiting inventory allocation.</summary>
//        Pending,
//        /// <summary>Inventory reserved for this fulfillment.</summary>
//        Allocated,
//        /// <summary>Warehouse staff actively picking items.</summary>
//        Picking,
//        /// <summary>All items picked, ready for packing.</summary>
//        Picked,
//        /// <summary>Items currently being packed.</summary>
//        Packing,
//        /// <summary>Package ready for carrier pickup.</summary>
//        ReadyToShip,
//        /// <summary>Handed over to carrier for transport.</summary>
//        Shipped,
//        /// <summary>Customer has received the package.</summary>
//        Delivered,
//        /// <summary>Fulfillment order was canceled.</summary>
//        Canceled
//    }

//    #region Errors
//    /// <summary>
//    /// Defines domain error scenarios specific to <see cref="FulfillmentOrder"/> operations.
//    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling
//    /// during state transitions and business logic execution.
//    /// </summary>
//    public static class Errors
//    {
//        /// <summary>
//        /// Error indicating that a requested fulfillment order could not be found.
//        /// </summary>
//        public static Error NotFound(Guid id) => Error.NotFound(code: "FulfillmentOrder.NotFound", description: $"Fulfillment order with ID '{id}' was not found.");
//        /// <summary>
//        /// Error indicating an invalid state transition attempt.
//        /// </summary>
//        public static Error InvalidStateTransition(FulfillmentState from, FulfillmentState to) =>
//            Error.Validation(code: "FulfillmentOrder.InvalidStateTransition", description: $"Cannot transition from {from} to {to}.");
//        /// <summary>
//        /// Error indicating an action was attempted on an already canceled fulfillment order.
//        /// </summary>
//        public static Error AlreadyCanceled => Error.Validation(code: "FulfillmentOrder.AlreadyCanceled", description: "Cannot perform action on a canceled fulfillment order.");
//        /// <summary>
//        /// Error indicating the fulfillment order must be in the <see cref="FulfillmentState.Pending"/> state for the requested action.
//        /// </summary>
//        public static Error NotPending => Error.Validation(code: "FulfillmentOrder.NotPending", description: "Fulfillment order must be in Pending state.");
//        /// <summary>
//        /// Error indicating the fulfillment order must be in the <see cref="FulfillmentState.Allocated"/> state for the requested action.
//        /// </summary>
//        public static Error NotAllocated => Error.Validation(code: "FulfillmentOrder.NotAllocated", description: "Fulfillment order must be in Allocated state.");
//        /// <summary>
//        /// Error indicating the fulfillment order must be in the <see cref="FulfillmentState.Picking"/> state for the requested action.
//        /// </summary>
//        public static Error NotPicking => Error.Validation(code: "FulfillmentOrder.NotPicking", description: "Fulfillment order must be in Picking state.");
//        /// <summary>
//        /// Error indicating the fulfillment order must be in the <see cref="FulfillmentState.Picked"/> state for the requested action.
//        /// </summary>
//        public static Error NotPicked => Error.Validation(code: "FulfillmentOrder.NotPicked", description: "Fulfillment order must be in Picked state.");
//        /// <summary>
//        /// Error indicating the fulfillment order must be in the <see cref="FulfillmentState.Packing"/> state for the requested action.
//        /// </summary>
//        public static Error NotPacked => Error.Validation(code: "FulfillmentOrder.NotPacked", description: "Fulfillment order must be in Packing state.");
//        /// <summary>
//        /// Error indicating the fulfillment order must be in the <see cref="FulfillmentState.ReadyToShip"/> state for the requested action.
//        /// </summary>
//        public static Error NotReadyToShip => Error.Validation(code: "FulfillmentOrder.NotReadyToShip", description: "Fulfillment order must be in ReadyToShip state.");
//        /// <summary>
//        /// Error indicating the fulfillment order must be in the <see cref="FulfillmentState.Shipped"/> state for the requested action.
//        /// </summary>
//        public static Error NotShipped => Error.Validation(code: "FulfillmentOrder.NotShipped", description: "Fulfillment order must be in Shipped state.");
//        /// <summary>
//        /// Error indicating that a package ID is required for packing.
//        /// </summary>
//        public static Error PackageIdRequired => Error.Validation(code: "FulfillmentOrder.PackageIdRequired", description: "Package ID is required for packing.");
//        /// <summary>
//        /// Error indicating that a tracking number is required for shipping.
//        /// </summary>
//        public static Error TrackingNumberRequired => Error.Validation(code: "FulfillmentOrder.TrackingNumberRequired", description: "Tracking number is required for shipping.");
//        /// <summary>
//        /// Error indicating that the fulfillment order must contain at least one item.
//        /// </summary>
//        public static Error NoItems => Error.Validation(code: "FulfillmentOrder.NoItems", description: "Fulfillment order must have at least one item.");
//    }
//    #endregion

//    #region Properties
//    /// <summary>
//    /// Gets the unique identifier of the associated customer order.
//    /// </summary>
//    public Guid OrderId { get; set; }
//    /// <summary>
//    /// Gets the unique identifier of the stock location from which this fulfillment order will be processed.
//    /// </summary>
//    public Guid StockLocationId { get; set; }
//    /// <summary>
//    /// Gets or sets the current state of the fulfillment order.
//    /// The state progresses through a defined lifecycle (see <see cref="FulfillmentState"/>).
//    /// </summary>
//    public FulfillmentState State { get; set; } = FulfillmentState.Pending;
//    /// <summary>
//    /// Gets or sets an optional identifier for the physical package once packed.
//    /// </summary>
//    public string? PackageId { get; set; } // Identifier for the physical package
//    /// <summary>
//    /// Gets or sets the tracking number provided by the shipping carrier.
//    /// </summary>
//    public string? TrackingNumber { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when inventory was allocated for this fulfillment order.
//    /// </summary>
//    public DateTimeOffset? AllocatedAt { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when picking of items for this fulfillment order began.
//    /// </summary>
//    public DateTimeOffset? PickingStartedAt { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when all items for this fulfillment order were picked.
//    /// </summary>
//    public DateTimeOffset? PickedAt { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when items for this fulfillment order were packed.
//    /// </summary>
//    public DateTimeOffset? PackedAt { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when the package was marked as ready for shipment.
//    /// </summary>
//    public DateTimeOffset? ReadyToShipAt { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when the fulfillment order was handed over to the carrier.
//    /// </summary>
//    public DateTimeOffset? ShippedAt { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when the package was delivered to the customer.
//    /// </summary>
//    public DateTimeOffset? DeliveredAt { get; set; }
//    /// <summary>
//    /// Gets or sets the timestamp when the fulfillment order was canceled.
//    /// </summary>
//    public DateTimeOffset? CanceledAt { get; set; }
//    #endregion

//    #region Relationships
//    /// <summary>
//    /// Gets the associated customer order.
//    /// This is typically a navigation property for ORM.
//    /// </summary>
//    public Order Order { get; set; } = null!;
//    /// <summary>
//    /// Gets the stock location from which this fulfillment order is being processed.
//    /// This is typically a navigation property for ORM.
//    /// </summary>
//    public StockLocation StockLocation { get; set; } = null!;
//    /// <summary>
//    /// Gets the collection of line items that comprise this fulfillment order.
//    /// These are the individual product variants and quantities to be fulfilled.
//    /// </summary>
//    public ICollection<FulfillmentLineItem> Items { get; set; } = new List<FulfillmentLineItem>();
//    #endregion

//    #region Constructors
//    /// <summary>
//    /// Private constructor for ORM (Entity Framework Core) materialization.
//    /// </summary>
//    private FulfillmentOrder() { }
//    #endregion

//    #region Factory Methods
//    /// <summary>
//    /// Factory method to create a new <see cref="FulfillmentOrder"/> instance.
//    /// Initializes the order in the <see cref="FulfillmentState.Pending"/> state.
//    /// </summary>
//    /// <param name="orderId">The unique identifier of the associated customer order.</param>
//    /// <param name="stockLocationId">The unique identifier of the stock location for fulfillment.</param>
//    /// <param name="items">A collection of <see cref="FulfillmentLineItem"/>s to be fulfilled.</param>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the newly created <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Error.Validation"/> if required IDs are empty or no items are provided.
//    /// </returns>
//    public static ErrorOr<FulfillmentOrder> Create(Guid orderId, Guid stockLocationId, ICollection<FulfillmentLineItem> items)
//    {
//        if (orderId == Guid.Empty) return Error.Validation(code: "FulfillmentOrder.InvalidOrder", description: "Order ID is required.");
//        if (stockLocationId == Guid.Empty) return Error.Validation(code: "FulfillmentOrder.InvalidStockLocation", description: "Stock location ID is required.");
//        if (items == null || !items.Any()) return Errors.NoItems;

//        var fulfillmentOrder = new FulfillmentOrder
//        {
//            Id = Guid.NewGuid(),
//            OrderId = orderId,
//            StockLocationId = stockLocationId,
//            State = FulfillmentState.Pending,
//            Items = items,
//            CreatedAt = DateTimeOffset.UtcNow
//        };

//        fulfillmentOrder.AddDomainEvent(new Events.Created(fulfillmentOrder.Id, orderId, stockLocationId));
//        return fulfillmentOrder;
//    }
//    #endregion

//    #region Business Logic - State Transitions
//    /// <summary>
//    /// Transitions the fulfillment order state from <see cref="FulfillmentState.Pending"/> to <see cref="FulfillmentState.Allocated"/>.
//    /// This signifies that inventory has been reserved for the items in this fulfillment order.
//    /// </summary>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the current state is not <see cref="FulfillmentState.Pending"/>.
//    /// Returns <see cref="Errors.AlreadyCanceled"/> if the order is canceled.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> AllocateInventory()
//    {
//        if (State != FulfillmentState.Pending) return Errors.InvalidStateTransition(State, FulfillmentState.Allocated);
//        if (State == FulfillmentState.Canceled) return Errors.AlreadyCanceled;

//        State = FulfillmentState.Allocated;
//        AllocatedAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.InventoryAllocated(Id, OrderId, StockLocationId));
//        return this;
//    }

//    /// <summary>
//    /// Transitions the fulfillment order state from <see cref="FulfillmentState.Allocated"/> to <see cref="FulfillmentState.Picking"/>.
//    /// This indicates that warehouse staff have begun the process of picking items.
//    /// </summary>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the current state is not <see cref="FulfillmentState.Allocated"/>.
//    /// Returns <see cref="Errors.AlreadyCanceled"/> if the order is canceled.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> StartPicking()
//    {
//        if (State != FulfillmentState.Allocated) return Errors.InvalidStateTransition(State, FulfillmentState.Picking);
//        if (State == FulfillmentState.Canceled) return Errors.AlreadyCanceled;

//        State = FulfillmentState.Picking;
//        PickingStartedAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.PickingStarted(Id, OrderId, StockLocationId));
//        return this;
//    }

//    /// <summary>
//    /// Transitions the fulfillment order state from <see cref="FulfillmentState.Picking"/> to <see cref="FulfillmentState.Picked"/>.
//    /// This indicates that all items for the fulfillment order have been successfully picked.
//    /// </summary>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the current state is not <see cref="FulfillmentState.Picking"/>.
//    /// Returns <see cref="Errors.AlreadyCanceled"/> if the order is canceled.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> CompletePicking()
//    {
//        if (State != FulfillmentState.Picking) return Errors.InvalidStateTransition(State, FulfillmentState.Picked);
//        if (State == FulfillmentState.Canceled) return Errors.AlreadyCanceled;

//        State = FulfillmentState.Picked;
//        PickedAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.PickingCompleted(Id, OrderId, StockLocationId));
//        return this;
//    }

//    /// <summary>
//    /// Transitions the fulfillment order state from <see cref="FulfillmentState.Picked"/> to <see cref="FulfillmentState.Packing"/>.
//    /// This initiates the packing process and associates a package identifier with the fulfillment.
//    /// </summary>
//    /// <param name="packageId">A unique identifier for the physical package being created.</param>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the current state is not <see cref="FulfillmentState.Picked"/>.
//    /// Returns <see cref="Errors.AlreadyCanceled"/> if the order is canceled.
//    /// Returns <see cref="Errors.PackageIdRequired"/> if the provided package ID is null or whitespace.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> Pack(string packageId)
//    {
//        if (State != FulfillmentState.Picked) return Errors.InvalidStateTransition(State, FulfillmentState.Packing);
//        if (State == FulfillmentState.Canceled) return Errors.AlreadyCanceled;
//        if (string.IsNullOrWhiteSpace(packageId)) return Errors.PackageIdRequired;

//        State = FulfillmentState.Packing;
//        PackageId = packageId;
//        PackedAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.PackingCompleted(Id, OrderId, StockLocationId, packageId));
//        return this;
//    }

//    /// <summary>
//    /// Transitions the fulfillment order state from <see cref="FulfillmentState.Packing"/> to <see cref="FulfillmentState.ReadyToShip"/>.
//    /// This indicates that the package is sealed and ready for carrier pickup.
//    /// </summary>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the current state is not <see cref="FulfillmentState.Packing"/>.
//    /// Returns <see cref="Errors.AlreadyCanceled"/> if the order is canceled.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> MarkReadyToShip()
//    {
//        if (State != FulfillmentState.Packing) return Errors.InvalidStateTransition(State, FulfillmentState.ReadyToShip);
//        if (State == FulfillmentState.Canceled) return Errors.AlreadyCanceled;

//        State = FulfillmentState.ReadyToShip;
//        ReadyToShipAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.ReadyToShip(Id, OrderId, StockLocationId));
//        return this;
//    }

//    /// <summary>
//    /// Transitions the fulfillment order state from <see cref="FulfillmentState.ReadyToShip"/> to <see cref="FulfillmentState.Shipped"/>.
//    /// This indicates the package has been handed over to the shipping carrier.
//    /// </summary>
//    /// <param name="trackingNumber">The tracking number provided by the shipping carrier for this package.</param>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the current state is not <see cref="FulfillmentState.ReadyToShip"/>.
//    /// Returns <see cref="Errors.AlreadyCanceled"/> if the order is canceled.
//    /// Returns <see cref="Errors.TrackingNumberRequired"/> if the tracking number is null or whitespace.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> Ship(string trackingNumber)
//    {
//        if (State != FulfillmentState.ReadyToShip) return Errors.InvalidStateTransition(State, FulfillmentState.Shipped);
//        if (State == FulfillmentState.Canceled) return Errors.AlreadyCanceled;
//        if (string.IsNullOrWhiteSpace(trackingNumber)) return Errors.TrackingNumberRequired;

//        State = FulfillmentState.Shipped;
//        TrackingNumber = trackingNumber;
//        ShippedAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.Shipped(Id, OrderId, StockLocationId, trackingNumber));
//        return this;
//    }

//    /// <summary>
//    /// Transitions the fulfillment order state from <see cref="FulfillmentState.Shipped"/> to <see cref="FulfillmentState.Delivered"/>.
//    /// This indicates that the package has been successfully delivered to the customer.
//    /// </summary>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the current state is not <see cref="FulfillmentState.Shipped"/>.
//    /// Returns <see cref="Errors.AlreadyCanceled"/> if the order is canceled.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> Deliver()
//    {
//        if (State != FulfillmentState.Shipped) return Errors.InvalidStateTransition(State, FulfillmentState.Delivered);
//        if (State == FulfillmentState.Canceled) return Errors.AlreadyCanceled;

//        State = FulfillmentState.Delivered;
//        DeliveredAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.Delivered(Id, OrderId, StockLocationId));
//        return this;
//    }

//    /// <summary>
//    /// Transitions the fulfillment order state to <see cref="FulfillmentState.Canceled"/>.
//    /// A fulfillment order can be canceled from any state except <see cref="FulfillmentState.Shipped"/> or <see cref="FulfillmentState.Delivered"/>.
//    /// This operation is idempotent if the order is already canceled.
//    /// </summary>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentOrder}"/> result.
//    /// Returns the updated <see cref="FulfillmentOrder"/> instance on success.
//    /// Returns <see cref="Errors.InvalidStateTransition"/> if the order is already shipped or delivered.
//    /// </returns>
//    public ErrorOr<FulfillmentOrder> Cancel()
//    {
//        if (State == FulfillmentState.Delivered || State == FulfillmentState.Shipped) return Errors.InvalidStateTransition(State, FulfillmentState.Canceled);
//        if (State == FulfillmentState.Canceled) return this; // Idempotent

//        State = FulfillmentState.Canceled;
//        CanceledAt = DateTimeOffset.UtcNow;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        AddDomainEvent(new Events.Canceled(Id, OrderId, StockLocationId));
//        return this;
//    }
//    #endregion

//    #region Domain Events
//    /// <summary>
//    /// Defines domain events related to the lifecycle and state changes of a <see cref="FulfillmentOrder"/>.
//    /// These events facilitate a decoupled architecture, allowing other bounded contexts to react
//    /// to significant changes in the fulfillment process.
//    /// </summary>
//    public static class Events
//    {
//        /// <summary>
//        /// Event fired when a new <see cref="FulfillmentOrder"/> is created.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the newly created fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location for the fulfillment.</param>
//        public sealed record Created(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId) : DomainEvent;
//        /// <summary>
//        /// Event fired when inventory is allocated for a <see cref="FulfillmentOrder"/>.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        public sealed record InventoryAllocated(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId) : DomainEvent;
//        /// <summary>
//        /// Event fired when picking for a <see cref="FulfillmentOrder"/> has started.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        public sealed record PickingStarted(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId) : DomainEvent;
//        /// <summary>
//        /// Event fired when picking for a <see cref="FulfillmentOrder"/> has completed.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        public sealed record PickingCompleted(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId) : DomainEvent;
//        /// <summary>
//        /// Event fired when packing for a <see cref="FulfillmentOrder"/> has completed.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        /// <param name="PackageId">The unique identifier for the physical package.</param>
//        public sealed record PackingCompleted(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId, string PackageId) : DomainEvent;
//        /// <summary>
//        /// Event fired when a <see cref="FulfillmentOrder"/> is marked as ready to ship.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        public sealed record ReadyToShip(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId) : DomainEvent;
//        /// <summary>
//        /// Event fired when a <see cref="FulfillmentOrder"/> has been shipped.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        /// <param name="TrackingNumber">The tracking number provided by the carrier.</param>
//        public sealed record Shipped(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId, string TrackingNumber) : DomainEvent;
//        /// <summary>
//        /// Event fired when a <see cref="FulfillmentOrder"/> has been delivered.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        public sealed record Delivered(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId) : DomainEvent;
//        /// <summary>
//        /// Event fired when a <see cref="FulfillmentOrder"/> is canceled.
//        /// </summary>
//        /// <param name="FulfillmentOrderId">The ID of the fulfillment order.</param>
//        /// <param name="OrderId">The ID of the associated customer order.</param>
//        /// <param name="StockLocationId">The ID of the stock location.</param>
//        public sealed record Canceled(Guid FulfillmentOrderId, Guid OrderId, Guid StockLocationId) : DomainEvent;
//    }
//    #endregion
//}
