using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Stores.StockLocations;

/// <summary>
/// Represents the relationship between a Store and a StockLocation.
/// Manages fulfillment priorities for multi-warehouse operations.
/// </summary>
public sealed class StoreStockLocation : AuditableEntity
{
    #region Constraints
    public static class Constraints
    {
        public const int MinPriority = 1;
        public const int MaxPriority = CommonInput.Constraints.Numeric.MaxValue;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error AlreadyLinked => Error.Conflict(code: "StockLocationStore.AlreadyLinked", description: "Storefront is already linked to this stock location.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "StockLocationStore.NotFound", description: $"StoreStockLocation with ID '{id}' was not found.");
        public static Error InvalidPriority => Error.Validation(code: "StockLocationStore.InvalidPriority", description: $"Priority must be between {Constraints.MinPriority} and {Constraints.MaxPriority}.");
    }
    #endregion

    #region Properties
    public Guid StockLocationId { get; set; }
    public Guid StoreId { get; set; }
    public int Priority { get; set; } = Constraints.MinPriority;
    public bool CanFulfillOrders { get; set; } = true;
    #endregion

    #region Relationships
    public StockLocation StockLocation { get; set; } = null!;
    public Store Store { get; set; } = null!;
    #endregion

    #region Computed Properties
    /// <summary>
    /// Indicates if this location is currently capable of fulfilling orders.
    /// Combines both CanFulfillOrders flag and warehouse active status.
    /// </summary>
    public bool IsAvailableForFulfillment => CanFulfillOrders && StockLocation.Active;
    #endregion

    #region Constructors
    private StoreStockLocation() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new store-to-warehouse mapping with fulfillment configuration.
    /// </summary>
    public static ErrorOr<StoreStockLocation> Create(
        Guid stockLocationId,
        Guid storeId,
        int priority = Constraints.MinPriority,
        bool canFulfillOrders = true)
    {
        if (stockLocationId == Guid.Empty)
            return Error.Validation(code: "StoreStockLocation.InvalidStockLocation", description: "Stock location is required.");

        if (storeId == Guid.Empty)
            return Error.Validation(code: "StoreStockLocation.InvalidStore", description: "Store is required.");

        if (priority < Constraints.MinPriority || priority > Constraints.MaxPriority)
            return Errors.InvalidPriority;

        return new StoreStockLocation
        {
            Id = Guid.NewGuid(),
            StockLocationId = stockLocationId,
            StoreId = storeId,
            Priority = priority,
            CanFulfillOrders = canFulfillOrders,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion

    #region Business Logic

    /// <summary>
    /// Updates the fulfillment priority for this warehouse in the store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>Lower priority numbers are checked first (Priority 1 = primary warehouse)</description></item>
    /// <item><description>No change is made if new priority equals current priority</description></item>
    /// <item><description>Priority must be between MinPriority (1) and MaxPriority</description></item>
    /// <item><description>UpdatedAt only changed when actual change occurs</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// Priority 1 = check first (primary warehouse) → Priority 2 = check second (backup) → etc.
    /// </para>
    /// </remarks>
    /// <param name="priority">New priority (1 = checked first, higher = checked later)</param>
    /// <returns>ErrorOr result: updated entity or validation error</returns>
    public ErrorOr<StoreStockLocation> UpdatePriority(int priority)
    {
        if (priority < Constraints.MinPriority || priority > Constraints.MaxPriority)
            return Errors.InvalidPriority;

        if (Priority == priority)
            return this; // No change needed

        Priority = priority;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    /// <summary>
    /// Enables or disables order fulfillment from this warehouse in the store.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Behavior:</strong>
    /// <list type="bullet">
    /// <item><description>Controls whether orders can be fulfilled from this warehouse</description></item>
    /// <item><description>Useful for temporary shutdowns, maintenance, or stock issues</description></item>
    /// <item><description>IsAvailableForFulfillment computed property considers both this flag and warehouse active status</description></item>
    /// <item><description>No change is made if new value equals current value</description></item>
    /// </list>
    /// </para>
    /// <para>
    /// <strong>Use Cases:</strong>
    /// Disable fulfillment temporarily (maintenance, stock depletion). System automatically prefers other warehouses when disabled.
    /// </para>
    /// </remarks>
    /// <param name="enabled">Whether this warehouse can fulfill orders from this store</param>
    /// <returns>ErrorOr result: updated entity for chaining</returns>
    public ErrorOr<StoreStockLocation> SetFulfillmentEnabled(bool enabled)
    {
        if (CanFulfillOrders == enabled)
            return this; // No change needed

        CanFulfillOrders = enabled;
        UpdatedAt = DateTimeOffset.UtcNow;
        return this;
    }

    #endregion

    #region Deletion
    /// <summary>
    /// Marks this store-stock-location mapping for deletion.
    /// </summary>
    /// <remarks>
    /// This should be called through Store.RemoveStockLocation() rather than directly.
    /// Always use the aggregate root to maintain consistency.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}
