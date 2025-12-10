using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Orders.Shipments;
using ReSys.Core.Domain.Orders;

namespace ReSys.Core.Domain.Inventories.Stocks;

/// <summary>
/// Represents the inventory of a specific product variant at a particular stock location.
/// Manages quantity tracking, reservations, and movement history.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Tracks the quantity of a product variant in stock at a location, manages reservations for pending orders,
/// and maintains a complete history of all quantity changes through stock movements.
/// </para>
/// 
/// <para>
/// <b>Key Concepts:</b>
/// <list type="bullet">
/// <item><b>QuantityOnHand:</b> Physical count of items currently at this location</item>
/// <item><b>QuantityReserved:</b> Items allocated for pending orders (subset of QuantityOnHand)</item>
/// <item><b>CountAvailable:</b> Items available for new orders (QuantityOnHand - QuantityReserved, minimum 0)</item>
/// <item><b>Backorderable:</b> Can customers order when out of stock? Allows negative CountAvailable</item>
/// <item><b>StockMovements:</b> Complete audit trail of all quantity changes</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>State Transitions:</b>
/// <list type="bullet">
/// <item><b>Adjust:</b> Modify QuantityOnHand (restock, damage, shrinkage, etc.)</item>
/// <item><b>Reserve:</b> Allocate items for an order (increases QuantityReserved)</item>
/// <item><b>Release:</b> Free up reserved items (order cancellation)</item>
/// <item><b>ConfirmShipment:</b> Decrease both QuantityOnHand and QuantityReserved (order fulfillment)</item>
/// </list>
/// </para>
/// </remarks>
public sealed class StockItem : Aggregate, IHasMetadata
{
    #region Constraints
    public static class Constraints
    {
        public const int MinQuantity = 0;
        public const int SkuMaxLength = 255;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error DuplicateSku(string sku, Guid stockLocationId) =>
            Error.Conflict(
                code: "StockItem.DuplicateSku",
                description: $"Stock item with SKU '{sku}' already exists in stock location '{stockLocationId}'.");

        public static Error InsufficientStock(int available, int requested) =>
            Error.Validation(
                code: "StockItem.InsufficientStock",
                description: $"Insufficient stock. Available: {available}, Requested: {requested}");

        public static Error InvalidQuantity =>
            Error.Validation(
                code: "StockItem.InvalidQuantity",
                description: "Quantity must be non-negative.");

        public static Error InvalidRelease(int reserved, int releaseRequested) =>
            Error.Validation(
                code: "StockItem.InvalidRelease",
                description: $"Cannot release {releaseRequested} units. Only {reserved} units reserved.");

        public static Error InvalidShipment(int reserved, int shipRequested) =>
            Error.Validation(
                code: "StockItem.InvalidShipment",
                description: $"Cannot ship {shipRequested} units. Only {reserved} units reserved.");

        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "StockItem.NotFound",
                description: $"Stock item with ID '{id}' was not found.");

        public static Error NegativeReserved =>
            Error.Validation(
                code: "StockItem.NegativeReserved",
                description: "Reserved quantity cannot be negative.");

        public static Error ReservedExceedsOnHand =>
            Error.Validation(
                code: "StockItem.ReservedExceedsOnHand",
                description: "Reserved quantity cannot exceed quantity on hand.");

        public static Error DuplicateReservation(Guid orderId, int existingQuantity, int requestedQuantity) =>
            Error.Validation(
                code: "StockItem.DuplicateReservation",
                description: $"Order '{orderId}' already has {existingQuantity} units reserved. Cannot reserve {requestedQuantity}.");
    }
    #endregion

    #region Properties
    /// <summary>Gets the ID of the product variant being stocked.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Gets the ID of the stock location where this item is held.</summary>
    public Guid StockLocationId { get; set; }

    /// <summary>Gets the SKU (Stock Keeping Unit) of this item.</summary>
    public string Sku { get; set; } = string.Empty;

    /// <summary>Gets the current physical quantity on hand at this location.</summary>
    public int QuantityOnHand { get; private set; } // FIXED: private setter for encapsulation

    /// <summary>Gets the quantity currently reserved for pending orders.</summary>
    public int QuantityReserved { get; private set; } // FIXED: private setter

    /// <summary>
    /// Gets a value indicating whether this item can be backordered (ordered when out of stock).
    /// </summary>
    public bool Backorderable { get; set; } = true;

    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }

    private readonly Dictionary<Guid, int> _reservations = new();
    #endregion

    #region Relationships
    public StockLocation StockLocation { get; set; } = null!;
    public Variant Variant { get; set; } = null!;
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    #endregion

    #region Computed Properties

    /// <summary>
    /// Gets the quantity available for new reservations (QuantityOnHand - QuantityReserved, minimum 0).
    /// </summary>
    public int CountAvailable => Math.Max(val1: 0, val2: QuantityOnHand - QuantityReserved);

    /// <summary>
    /// Gets a value indicating whether this item is in stock (CountAvailable > 0 or Backorderable).
    /// </summary>
    public bool InStock => CountAvailable > 0 || Backorderable;

    #endregion

    #region Constructors
    private StockItem() { }
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new stock item for a variant at a specific location.
    /// </summary>
    /// <param name="variantId">The product variant ID.</param>
    /// <param name="stockLocationId">The stock location ID.</param>
    /// <param name="sku">The Stock Keeping Unit (SKU).</param>
    /// <param name="quantityOnHand">Initial quantity on hand (default: 0).</param>
    /// <param name="quantityReserved">Initial reserved quantity (default: 0). Should not exceed QuantityOnHand.</param>
    /// <param name="backorderable">Whether this item can be backordered (default: true).</param>
    /// <param name="publicMetadata">Optional public metadata.</param>
    /// <param name="privateMetadata">Optional private metadata.</param>
    /// <returns>
    /// On success: A new StockItem instance.
    /// On failure: Validation error if quantity is negative.
    /// </returns>
    public static ErrorOr<StockItem> Create(
        Guid variantId,
        Guid stockLocationId,
        string sku,
        int quantityOnHand = 0,
        int quantityReserved = 0,
        bool backorderable = true,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        if (quantityOnHand < Constraints.MinQuantity)
            return Errors.InvalidQuantity;

        var item = new StockItem
        {
            Id = Guid.NewGuid(),
            VariantId = variantId,
            StockLocationId = stockLocationId,
            Sku = sku,
            QuantityOnHand = quantityOnHand,
            QuantityReserved = quantityReserved,
            Backorderable = backorderable,
            PublicMetadata = publicMetadata,
            PrivateMetadata = privateMetadata,
            CreatedAt = DateTimeOffset.UtcNow
        };

        item.AddDomainEvent(
            domainEvent: new Events.StockItemCreated(
                StockItemId: item.Id,
                VariantId: item.VariantId,
                StockLocationId: item.StockLocationId));

        return item;
    }

    #endregion

    #region Queries

    /// <summary>
    /// Gets backordered inventory units for this stock item at this location.
    /// Uses a query method (no stored collection) to maintain clean aggregate boundaries.
    /// Spree-aligned pattern: InventoryUnit.backordered_for_stock_item(self)
    /// </summary>
    /// <param name="unitsQuery">The base InventoryUnit query to filter from.</param>
    /// <returns>Backordered inventory units ordered by creation date.</returns>
    public IEnumerable<InventoryUnit> GetBackorderedInventoryUnits(IQueryable<InventoryUnit> unitsQuery)
    {
        return unitsQuery
            .Where(iu => iu.StockLocationId == StockLocationId &&
                         iu.State == InventoryUnit.InventoryUnitState.Backordered)
            .OrderBy(iu => iu.CreatedAt);
    }

    #endregion

    #region Business Logic: Updates

    /// <summary>
    /// Updates the stock item's properties including quantities and metadata.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Update Behavior:</b>
    /// <list type="bullet">
    /// <item>Only non-null parameters are updated</item>
    /// <item>Quantity changes are recorded as adjustments with full movement history</item>
    /// <item>Metadata updates are only applied if provided (not null)</item>
    /// <item>UpdatedAt is set only if changes are made</item>
    /// </list>
    /// </para>
    /// </remarks>
    public ErrorOr<StockItem> Update(
        Guid variantId,
        Guid stockLocationId,
        string sku,
        bool backorderable,
        int? quantityOnHand = null,
        int? quantityReserved = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        bool changed = false;

        if (VariantId != variantId)
        {
            VariantId = variantId;
            changed = true;
        }

        if (StockLocationId != stockLocationId)
        {
            StockLocationId = stockLocationId;
            changed = true;
        }

        if (Sku != sku)
        {
            Sku = sku;
            changed = true;
        }

        if (Backorderable != backorderable)
        {
            Backorderable = backorderable;
            changed = true;
        }

        // Handle QuantityOnHand changes
        if (quantityOnHand.HasValue && quantityOnHand.Value != QuantityOnHand)
        {
            var difference = quantityOnHand.Value - QuantityOnHand;
            var adjustResult = Adjust(
                quantity: difference,
                originator: StockMovement.MovementOriginator.Adjustment,
                reason: "Stock level updated via StockItem.Update");
            if (adjustResult.IsError)
                return adjustResult.Errors;
            changed = true;
        }

        // Handle QuantityReserved changes
        if (quantityReserved.HasValue && quantityReserved.Value != QuantityReserved)
        {
            var difference = quantityReserved.Value - QuantityReserved;
            if (difference > 0) // Reserve more
            {
                var reserveResult = Reserve(
                    quantity: difference,
                    orderId: Guid.Empty);
                if (reserveResult.IsError)
                    return reserveResult.Errors;
            }
            else if (difference < 0) // Release some
            {
                var releaseResult = Release(
                    quantity: -difference,
                    orderId: Guid.Empty);
                if (releaseResult.IsError)
                    return releaseResult.Errors;
            }
            changed = true;
        }

        // Handle metadata updates
        if (publicMetadata != null && PublicMetadata != null && !PublicMetadata.MetadataEquals(dict2: publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
            changed = true;
        }
        else if (publicMetadata == null && PublicMetadata != null)
        {
            PublicMetadata = null;
            changed = true;
        }
        else if (publicMetadata != null && PublicMetadata == null)
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
            changed = true;
        }

        if (privateMetadata != null && PrivateMetadata != null && !PrivateMetadata.MetadataEquals(dict2: privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
            changed = true;
        }
        else if (privateMetadata == null && PrivateMetadata != null)
        {
            PrivateMetadata = null;
            changed = true;
        }
        else if (privateMetadata != null && PrivateMetadata == null)
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(domainEvent: new Events.StockItemUpdated(StockItemId: Id));
        }

        return this;
    }

    #endregion

    #region Business Logic: Stock Adjustments

    /// <summary>
    /// Adjusts the quantity on hand due to restock, damage, loss, shrinkage, or other reasons.
    /// Creates a movement record for audit trail.
    /// </summary>
    /// <param name="quantity">The quantity change (positive for restock, negative for damage/loss).</param>
    /// <param name="originator">The originator of this adjustment (Adjustment, StockTransfer, etc.).</param>
    /// <param name="reason">Optional description of why this adjustment occurred.</param>
    /// <param name="originatorId">Optional reference to a stock transfer if this is part of a transfer.</param>
    /// <returns>
    /// On success: This stock item (for method chaining).
    /// On failure: Error if resulting quantity would be negative.
    /// </returns>
    /// <remarks>
    /// Adjustments do not affect reserved quantities. Use Reserve/Release for order-related changes.
    /// </remarks>
    public ErrorOr<StockItem> Adjust(
        int quantity,
        StockMovement.MovementOriginator originator,
        string? reason = null,
        Guid? originatorId = null)
    {
        var newCount = QuantityOnHand + quantity;
        if (newCount < Constraints.MinQuantity)
            return Errors.InsufficientStock(available: QuantityOnHand, requested: -quantity); // Updated to use parameterized error

        QuantityOnHand = newCount;
        UpdatedAt = DateTimeOffset.UtcNow;

        var movementResult = StockMovement.Create(
            stockItemId: Id,
            quantity: quantity,
            originator: originator,
            action: StockMovement.MovementAction.Adjustment,
            reason: reason,
            originatorId: originatorId);

        if (movementResult.IsError)
            return movementResult.FirstError;

        StockMovements.Add(item: movementResult.Value);
        AddDomainEvent(
            domainEvent: new Events.StockAdjusted(
                StockItemId: Id,
                VariantId: VariantId,
                StockLocationId: StockLocationId,
                Quantity: quantity,
                NewCount: QuantityOnHand));

        // Process backorders if this is a restock (positive quantity)
        // Note: Backorder processing now requires application service to fetch and pass units
        // This maintains clean domain boundaries (domain doesn't query across aggregates)
        if (quantity > 0 && Backorderable)
        {
            ProcessBackorders(quantityAvailable: quantity, backorderedUnits: null);
        }

        return this;
    }

    #endregion

    #region Business Logic: Backorder Processing

    /// <summary>
    /// Processes backordered inventory units when stock becomes available.
    /// Fills backorders sequentially up to the available quantity.
    /// </summary>
    /// <param name="quantityAvailable">The quantity of stock that became available.</param>
    /// <param name="backorderedUnits"></param>
    /// <remarks>
    /// This is called automatically when stock is restocked (positive adjustment).
    /// Backordered units are filled in order of creation (oldest first).
    /// </remarks>
    private void ProcessBackorders(int quantityAvailable, IEnumerable<InventoryUnit>? backorderedUnits = null)
    {
        // If no units provided, cannot process backorders
        // (Units must be queried by application service and passed here)
        if (backorderedUnits == null || !backorderedUnits.Any())
            return;

        int remaining = quantityAvailable;

        foreach (var unit in backorderedUnits)
        {
            if (remaining <= 0)
                break;

            var fillResult = unit.FillBackorder();
            if (fillResult.IsError)
                continue;

            remaining--;

            AddDomainEvent(
                domainEvent: new Events.BackorderProcessed(
                    StockItemId: Id,
                    InventoryUnitId: unit.Id,
                    FilledQuantity: 1));  // Always 1 per unit
        }
    }

    #endregion

    #region Business Logic: Reservations

    /// <summary>
    /// Reserves stock for a pending order, allocating it from available inventory.
    /// </summary>
    /// <param name="quantity">The quantity to reserve (must be positive).</param>
    /// <param name="orderId">Optional ID of the order this reservation is for.</param>
    /// <returns>
    /// On success: This stock item (for method chaining).
    /// On failure: Error if insufficient stock and not backorderable, or invalid quantity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Backorderable Behavior:</b>
    /// <list type="bullet">
    /// <item><b>If Backorderable:</b> Reserve always succeeds, even if CountAvailable becomes negative</item>
    /// <item><b>If Not Backorderable:</b> Reserve fails if quantity > CountAvailable</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// Reserved items reduce CountAvailable but remain in QuantityOnHand until shipment confirmation.
    /// </para>
    /// </remarks>
    public ErrorOr<StockItem> Reserve(int quantity, Guid? orderId = null)
    {
        if (quantity <= Constraints.MinQuantity)
            return Errors.InvalidQuantity;

        // Idempotency check: If orderId is provided, check existing reservations
        if (orderId.HasValue)
        {
            if (_reservations.TryGetValue(orderId.Value, out var existingReservedQuantity))
            {
                if (existingReservedQuantity == quantity)
                {
                    // Already reserved this exact quantity for this order, idempotent success
                    return this;
                }
                else
                {
                    // Attempting to reserve a different quantity for an already reserved orderId
                    return Errors.DuplicateReservation(orderId.Value, existingReservedQuantity, quantity);
                }
            }
        }

        var newReserved = QuantityReserved + quantity;

        // NEW: Prevent reserving more than on-hand (unless backorderable)
        if (!Backorderable && newReserved > QuantityOnHand)
            return Errors.InsufficientStock(available: QuantityOnHand, requested: quantity);

        QuantityReserved = newReserved;
        UpdatedAt = DateTimeOffset.UtcNow;

        if (orderId.HasValue)
        {
            _reservations[orderId.Value] = quantity; // Track reservation for this order
        }

        var movementResult = StockMovement.Create(
            stockItemId: Id,
            quantity: -quantity,
            originator: StockMovement.MovementOriginator.Order,
            action: StockMovement.MovementAction.Reserved,
            reason: $"Order {orderId}",
            originatorId: orderId);

        if (movementResult.IsError)
            return movementResult.FirstError;

        StockMovements.Add(item: movementResult.Value);
        AddDomainEvent(
            domainEvent: new Events.StockReserved(
                StockItemId: Id,
                VariantId: VariantId,
                StockLocationId: StockLocationId,
                Quantity: quantity,
                OrderId: orderId));

        return this;
    }

    /// <summary>
    /// Releases previously reserved stock (e.g., due to order cancellation).
    /// </summary>
    /// <param name="quantity">The quantity to release (must be positive and &lt;= QuantityReserved).</param>
    /// <param name="orderId">The ID of the order this release is for.</param>
    /// <returns>
    /// On success: This stock item (for method chaining).
    /// On failure: Error if quantity invalid or exceeds reserved amount.
    /// </returns>
    /// <remarks>
    /// Releasing stock increases CountAvailable but does not change QuantityOnHand.
    /// </remarks>
    public ErrorOr<StockItem> Release(int quantity, Guid orderId)
    {
        if (quantity <= Constraints.MinQuantity)
            return Errors.InvalidQuantity;

        if (QuantityReserved < quantity)
            return Errors.InvalidRelease(reserved: QuantityReserved, releaseRequested: quantity);

        QuantityReserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Update the internal reservations dictionary
        if (_reservations.ContainsKey(orderId))
        {
            _reservations[orderId] -= quantity;
            if (_reservations[orderId] <= 0)
            {
                _reservations.Remove(orderId);
            }
        }

        var movementResult = StockMovement.Create(
            stockItemId: Id,
            quantity: quantity,
            originator: StockMovement.MovementOriginator.Order,
            action: StockMovement.MovementAction.Released,
            reason: $"Order {orderId} canceled",
            originatorId: orderId);

        if (movementResult.IsError)
            return movementResult.FirstError;

        StockMovements.Add(item: movementResult.Value);
        AddDomainEvent(
            domainEvent: new Events.StockReleased(
                StockItemId: Id,
                VariantId: VariantId,
                StockLocationId: StockLocationId,
                Quantity: quantity,
                OrderId: orderId));

        return this;
    }

    #endregion

    #region Business Logic: Shipment

    /// <summary>
    /// Confirms shipment of reserved stock, decreasing both QuantityOnHand and QuantityReserved.
    /// </summary>
    /// <param name="quantity">The quantity shipped (must be positive and &lt;= QuantityReserved).</param>
    /// <param name="shipmentId">The ID of the shipment.</param>
    /// <param name="orderId"></param>
    /// <returns>
    /// On success: Result.Deleted (follows ErrorOr pattern for result type).
    /// On failure: Error if quantity invalid or exceeds reserved amount.
    /// </returns>
    /// <remarks>
    /// This operation finalizes a shipment by removing items from both physical inventory and reservations.
    /// </remarks>
    public ErrorOr<Deleted> ConfirmShipment(int quantity, Guid shipmentId, Guid orderId)
    {
        if (quantity <= Constraints.MinQuantity)
            return Errors.InvalidQuantity;

        if (QuantityReserved < quantity)
            return Errors.InvalidShipment(reserved: QuantityReserved, shipRequested: quantity);

        QuantityOnHand -= quantity;
        QuantityReserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        // Update the internal reservations dictionary
        if (_reservations.ContainsKey(orderId))
        {
            _reservations[orderId] -= quantity;
            if (_reservations[orderId] <= 0)
            {
                _reservations.Remove(orderId);
            }
        }

        var movementResult = StockMovement.Create(
            stockItemId: Id,
            quantity: -quantity,
            originator: StockMovement.MovementOriginator.Shipment,
            action: StockMovement.MovementAction.Sold,
            reason: $"Shipment {shipmentId}",
            originatorId: shipmentId);

        if (movementResult.IsError)
            return movementResult.FirstError;

        StockMovements.Add(item: movementResult.Value);
        AddDomainEvent(
            domainEvent: new Events.StockShipped(
                StockItemId: Id,
                VariantId: VariantId,
                StockLocationId: StockLocationId,
                Quantity: quantity,
                ShipmentId: shipmentId));

        return Result.Deleted;
    }

    #endregion

    #region Business Logic: Lifecycle

    /// <summary>
    /// Deletes this stock item and publishes a deletion event.
    /// </summary>
    public ErrorOr<Deleted> Delete()
    {
        AddDomainEvent(domainEvent: new Events.StockItemDeleted(StockItemId: Id));
        return Result.Deleted;
    }

    #endregion

    #region Business Logic: Inventory Queries

    /// <summary>
    /// Gets the quantity reserved for a specific order.
    /// </summary>
    /// <param name="orderId">The order ID to query.</param>
    /// <returns>The quantity reserved for this order, or 0 if no reservation exists.</returns>
    public int GetReservedQuantityForOrder(Guid orderId) =>
        _reservations.TryGetValue(orderId, out var quantity) ? quantity : 0;

    /// <summary>
    /// Gets all orders with reservations for this stock item.
    /// </summary>
    /// <returns>Collection of order IDs that have reservations.</returns>
    public IReadOnlyCollection<Guid> GetReservedOrders() =>
        _reservations.Keys.ToList().AsReadOnly();

    /// <summary>
    /// Gets the total count of distinct orders with reservations.
    /// </summary>
    public int ReservationCount => _reservations.Count;

    /// <summary>
    /// Determines if a specific order has a reservation.
    /// </summary>
    /// <param name="orderId">The order ID to check.</param>
    /// <returns>True if the order has a reservation; otherwise false.</returns>
    public bool HasReservation(Guid orderId) =>
        _reservations.ContainsKey(orderId);

    #endregion

    #region Business Logic: Invariants

    // NEW: Add method to validate stock item invariants
    public ErrorOr<Success> ValidateInvariants()
    {
        // Check quantity on hand vs reserved consistency
        if (QuantityReserved < 0)
            return Errors.NegativeReserved;
        
        if (QuantityReserved > QuantityOnHand && !Backorderable)
            return Errors.ReservedExceedsOnHand;

        return Result.Success;
    }

    #endregion

    #region Domain Events

    public static class Events
    {
        /// <summary>
        /// Raised when a new stock item is created.
        /// </summary>
        public sealed record StockItemCreated(Guid StockItemId, Guid VariantId, Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Raised when a stock item's properties are updated.
        /// </summary>
        public sealed record StockItemUpdated(Guid StockItemId) : DomainEvent;

        /// <summary>
        /// Raised when a stock item is deleted.
        /// </summary>
        public sealed record StockItemDeleted(Guid StockItemId) : DomainEvent;

        /// <summary>
        /// Raised when stock is adjusted (restock, damage, loss, etc.).
        /// </summary>
        public sealed record StockAdjusted(
            Guid StockItemId,
            Guid VariantId,
            Guid StockLocationId,
            int Quantity,
            int NewCount) : DomainEvent;

        /// <summary>
        /// Raised when stock is reserved for a pending order.
        /// </summary>
        public sealed record StockReserved(
            Guid StockItemId,
            Guid VariantId,
            Guid StockLocationId,
            int Quantity,
            Guid? OrderId) : DomainEvent;

        /// <summary>
        /// Raised when reserved stock is released (order cancellation).
        /// </summary>
        public sealed record StockReleased(
            Guid StockItemId,
            Guid VariantId,
            Guid StockLocationId,
            int Quantity,
            Guid? OrderId) : DomainEvent;

        /// <summary>
        /// Raised when stock is shipped (reserved items removed from inventory).
        /// </summary>
        public sealed record StockShipped(
            Guid StockItemId,
            Guid VariantId,
            Guid StockLocationId,
            int Quantity,
            Guid ShipmentId) : DomainEvent;

        /// <summary>
        /// Raised when a backordered inventory unit is filled from newly available stock.
        /// </summary>
        public sealed record BackorderProcessed(
            Guid StockItemId,
            Guid InventoryUnitId,
            int FilledQuantity) : DomainEvent;
    }

    #endregion
}