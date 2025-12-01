using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;

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

        public static Error InsufficientStock =>
            Error.Validation(
                code: "StockItem.InsufficientStock",
                description: "Insufficient stock available for this operation.");

        public static Error InvalidQuantity =>
            Error.Validation(
                code: "StockItem.InvalidQuantity",
                description: "Quantity must be non-negative.");

        public static Error InvalidRelease =>
            Error.Validation(
                code: "StockItem.InvalidRelease",
                description: "Cannot release more quantity than is currently reserved.");

        public static Error InvalidShipment =>
            Error.Validation(
                code: "StockItem.InvalidShipment",
                description: "Cannot ship more quantity than is currently reserved.");

        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "StockItem.NotFound",
                description: $"Stock item with ID '{id}' was not found.");
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
    public int QuantityOnHand { get; set; }

    /// <summary>Gets the quantity currently reserved for pending orders.</summary>
    public int QuantityReserved { get; set; }

    /// <summary>
    /// Gets a value indicating whether this item can be backordered (ordered when out of stock).
    /// </summary>
    public bool Backorderable { get; set; } = true;

    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }
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
    /// <param name="stockTransferId">Optional reference to a stock transfer if this is part of a transfer.</param>
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
        Guid? stockTransferId = null)
    {
        var newCount = QuantityOnHand + quantity;
        if (newCount < Constraints.MinQuantity)
            return Errors.InsufficientStock;

        QuantityOnHand = newCount;
        UpdatedAt = DateTimeOffset.UtcNow;

        var movementResult = StockMovement.Create(
            stockItem: this,
            quantity: quantity,
            originator: originator,
            action: StockMovement.MovementAction.Adjustment,
            reason: reason,
            stockTransferId: stockTransferId);

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

        return this;
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

        if (!Backorderable && CountAvailable < quantity)
            return Errors.InsufficientStock;

        QuantityReserved += quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        var movementResult = StockMovement.Create(
            stockItem: this,
            quantity: -quantity,
            originator: StockMovement.MovementOriginator.Order,
            action: StockMovement.MovementAction.Reserved,
            reason: $"Order {orderId}");

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
    /// <param name="quantity">The quantity to release (must be positive and <= QuantityReserved).</param>
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
            return Errors.InvalidRelease;

        QuantityReserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        var movementResult = StockMovement.Create(
            stockItem: this,
            quantity: quantity,
            originator: StockMovement.MovementOriginator.Order,
            action: StockMovement.MovementAction.Released,
            reason: $"Order {orderId} canceled");

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
    /// <param name="quantity">The quantity shipped (must be positive and <= QuantityReserved).</param>
    /// <param name="shipmentId">The ID of the shipment.</param>
    /// <returns>
    /// On success: Result.Deleted (follows ErrorOr pattern for result type).
    /// On failure: Error if quantity invalid or exceeds reserved amount.
    /// </returns>
    /// <remarks>
    /// This operation finalizes a shipment by removing items from both physical inventory and reservations.
    /// </remarks>
    public ErrorOr<Deleted> ConfirmShipment(int quantity, Guid shipmentId)
    {
        if (quantity <= Constraints.MinQuantity)
            return Errors.InvalidQuantity;

        if (QuantityReserved < quantity)
            return Errors.InvalidShipment;

        QuantityOnHand -= quantity;
        QuantityReserved -= quantity;
        UpdatedAt = DateTimeOffset.UtcNow;

        var movementResult = StockMovement.Create(
            stockItem: this,
            quantity: -quantity,
            originator: StockMovement.MovementOriginator.Shipment,
            action: StockMovement.MovementAction.Sold,
            reason: $"Shipment {shipmentId}");

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
    }

    #endregion
}