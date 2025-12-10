# Quick Reference: Code Snippets & Templates

Use these templates to accelerate implementation. Customize based on your specific needs.

---

## 1. StockLocation Enhancements

### Location Type Enum
```csharp
/// <summary>
/// Enumeration of stock location types.
/// </summary>
public enum LocationType
{
    /// <summary>Warehouse for bulk shipping only.</summary>
    Warehouse,

    /// <summary>Retail store for walk-in customers and pickup only.</summary>
    RetailStore,

    /// <summary>Hybrid location that supports both shipping and pickup.</summary>
    Both
}
```

### Add to StockLocation Class
```csharp
// ========== ADD THESE PROPERTIES ==========

/// <summary>
/// Gets or sets the type of this stock location.
/// </summary>
public LocationType Type { get; set; } = LocationType.Warehouse;

/// <summary>
/// Gets or sets whether this location can fulfill shipping orders.
/// </summary>
public bool ShipEnabled { get; set; } = true;

/// <summary>
/// Gets or sets whether this location supports in-store pickup.
/// </summary>
public bool PickupEnabled { get; set; } = false;

/// <summary>
/// Gets or sets the latitude for geographic calculations.
/// </summary>
public decimal? Latitude { get; set; }

/// <summary>
/// Gets or sets the longitude for geographic calculations.
/// </summary>
public decimal? Longitude { get; set; }

/// <summary>
/// Gets or sets contact phone number for the location.
/// </summary>
public string? Phone { get; set; }

/// <summary>
/// Gets or sets contact email for the location.
/// </summary>
public string? Email { get; set; }

/// <summary>
/// Gets or sets operating hours in JSON format.
/// Example: { "Monday": "9:00-17:00", "Tuesday": "9:00-17:00" }
/// </summary>
public IDictionary<string, object?>? OperatingHours { get; set; }

// ========== ADD THESE QUERY METHODS ==========

/// <summary>Gets whether this location can process shipments.</summary>
public bool CanShip => ShipEnabled && !IsDeleted;

/// <summary>Gets whether this location supports customer pickups.</summary>
public bool CanPickup => PickupEnabled && !IsDeleted;

/// <summary>Gets whether this is a warehouse location.</summary>
public bool IsWarehouse => Type == LocationType.Warehouse;

/// <summary>Gets whether this is a retail store location.</summary>
public bool IsRetailStore => Type == LocationType.RetailStore;

/// <summary>Gets whether this is a hybrid location.</summary>
public bool IsHybrid => Type == LocationType.Both;

/// <summary>Gets whether this location has geographic coordinates.</summary>
public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
```

### Factory Method Template
```csharp
/// <summary>
/// Creates a new stock location with comprehensive validation.
/// </summary>
public static ErrorOr<StockLocation> Create(
    string name,
    string presentation,
    LocationType type,
    bool shipEnabled = true,
    bool pickupEnabled = false,
    decimal? latitude = null,
    decimal? longitude = null,
    string? phone = null,
    string? email = null,
    IDictionary<string, object?>? operatingHours = null)
{
    // Normalize names
    (name, presentation) = HasParameterizableName.NormalizeParams(name: name, presentation: presentation);

    // Validate
    var errors = new List<Error>();
    if (string.IsNullOrWhiteSpace(value: name))
        errors.Add(item: Errors.NameRequired);

    if (errors.Any()) return errors;

    // Create
    var location = new StockLocation
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        Presentation = presentation.Trim(),
        Type = type,
        ShipEnabled = shipEnabled,
        PickupEnabled = pickupEnabled,
        Latitude = latitude,
        Longitude = longitude,
        Phone = phone?.Trim(),
        Email = email?.Trim(),
        OperatingHours = operatingHours ?? new Dictionary<string, object?>(),
        CreatedAt = DateTimeOffset.UtcNow
    };

    location.AddDomainEvent(new Events.Created(
        LocationId: location.Id,
        Name: location.Name,
        Type: location.Type));

    return location;
}
```

### Domain Events for StockLocation
```csharp
public static class Events
{
    /// <summary>Published when a stock location is created.</summary>
    public sealed record Created(Guid LocationId, string Name, LocationType Type) : DomainEvent;

    /// <summary>Published when a stock location is updated.</summary>
    public sealed record Updated(Guid LocationId) : DomainEvent;

    /// <summary>Published when a stock location is soft deleted.</summary>
    public sealed record Deleted(Guid LocationId) : DomainEvent;

    /// <summary>Published when a stock location is restored from deletion.</summary>
    public sealed record Restored(Guid LocationId) : DomainEvent;

    /// <summary>Published when a stock location enables shipping capability.</summary>
    public sealed record ShippingEnabled(Guid LocationId) : DomainEvent;

    /// <summary>Published when a stock location enables pickup capability.</summary>
    public sealed record PickupEnabled(Guid LocationId) : DomainEvent;
}
```

---

## 2. StockItem Reservation System

### Reservation Methods
```csharp
// ========== ADD TO StockItem CLASS ==========

/// <summary>
/// Reserves stock for a pending order (cart, checkout).
/// Increases the reserved_count without changing quantity_on_hand.
/// </summary>
public ErrorOr<Success> Reserve(Guid orderId, int quantity)
{
    // Validation
    if (quantity <= 0)
        return Errors.InvalidQuantity;

    // Check existing reservation
    if (_reservations.TryGetValue(orderId, out var existing))
    {
        if (existing == quantity)
            return Result.Success; // Already reserved
        
        // Increasing reservation
        int diff = quantity - existing;
        if (diff > 0 && CountAvailable < diff)
            return Errors.InsufficientStock(CountAvailable, diff);
    }
    else
    {
        // New reservation
        if (CountAvailable < quantity)
            return Errors.InsufficientStock(CountAvailable, quantity);
    }

    // Update reservation
    _reservations[orderId] = quantity;
    QuantityReserved = _reservations.Values.Sum();
    
    AddDomainEvent(new Events.Reserved(
        StockItemId: Id,
        OrderId: orderId,
        Quantity: quantity));

    return Result.Success;
}

/// <summary>
/// Releases reserved stock (cart abandoned, payment failed).
/// Decreases the reserved_count.
/// </summary>
public ErrorOr<Success> Release(Guid orderId, int? quantity = null)
{
    // Check if order has reservation
    if (!_reservations.TryGetValue(orderId, out var reserved))
        return Result.Success; // Nothing to release

    // Determine release quantity
    int releaseQty = quantity ?? reserved;
    if (releaseQty > reserved)
        return Errors.InvalidRelease(reserved, releaseQty);

    // Update reservation
    _reservations[orderId] -= releaseQty;
    if (_reservations[orderId] <= 0)
        _reservations.Remove(orderId);

    QuantityReserved = _reservations.Values.Sum();

    AddDomainEvent(new Events.Released(
        StockItemId: Id,
        OrderId: orderId,
        Quantity: releaseQty));

    return Result.Success;
}

/// <summary>
/// Confirms shipment and deducts actual inventory.
/// Decreases both quantity_on_hand and reserved_count.
/// </summary>
public ErrorOr<Success> ConfirmShipment(Guid orderId, int quantity)
{
    // Validate reservation exists
    if (!_reservations.TryGetValue(orderId, out var reserved))
        return Errors.InvalidShipment(0, quantity);

    if (reserved < quantity)
        return Errors.InvalidShipment(reserved, quantity);

    // Validate physical stock
    if (QuantityOnHand < quantity)
        return Errors.InsufficientStock(QuantityOnHand, quantity);

    // Deduct from both
    QuantityOnHand -= quantity;
    _reservations[orderId] -= quantity;
    if (_reservations[orderId] <= 0)
        _reservations.Remove(orderId);

    QuantityReserved = _reservations.Values.Sum();

    AddDomainEvent(new Events.Shipped(
        StockItemId: Id,
        OrderId: orderId,
        Quantity: quantity));

    return Result.Success;
}

/// <summary>
/// Adjusts inventory for restock, damage, shrinkage, or returns.
/// </summary>
public ErrorOr<Success> Adjust(int deltaQuantity, string reason)
{
    int newOnHand = QuantityOnHand + deltaQuantity;

    // Validate
    if (newOnHand < 0)
        return Errors.InvalidQuantity;

    if (newOnHand < QuantityReserved)
        return Errors.ReservedExceedsOnHand;

    // Update
    QuantityOnHand = newOnHand;

    AddDomainEvent(new Events.Adjusted(
        StockItemId: Id,
        DeltaQuantity: deltaQuantity,
        Reason: reason,
        NewQuantity: QuantityOnHand));

    return Result.Success;
}
```

### Domain Events for StockItem
```csharp
public static class Events
{
    public sealed record Reserved(Guid StockItemId, Guid OrderId, int Quantity) : DomainEvent;
    public sealed record Released(Guid StockItemId, Guid OrderId, int Quantity) : DomainEvent;
    public sealed record Shipped(Guid StockItemId, Guid OrderId, int Quantity) : DomainEvent;
    public sealed record Adjusted(Guid StockItemId, int DeltaQuantity, string Reason, int NewQuantity) : DomainEvent;
}
```

---

## 3. Fulfillment Strategy Pattern

### IFulfillmentStrategy Interface
```csharp
namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

/// <summary>
/// Defines the contract for fulfillment allocation strategies.
/// </summary>
public interface IFulfillmentStrategy
{
    /// <summary>
    /// Allocates inventory from available stock locations based on strategy rules.
    /// </summary>
    /// <param name="variant">Product variant to fulfill</param>
    /// <param name="quantity">Required quantity</param>
    /// <param name="availableLocations">Locations with sufficient stock</param>
    /// <param name="customerLocation">Customer's shipping address coordinates</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>List of allocations (location, quantity) in fulfillment order</returns>
    Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null,
        CancellationToken ct = default);
}

/// <summary>
/// Result of a fulfillment allocation decision.
/// </summary>
public sealed record FulfillmentAllocation(
    Guid LocationId,
    string LocationName,
    int AllocatedQuantity,
    decimal? ShippingCost = null,
    int? EstimatedDays = null);

/// <summary>
/// Customer location for distance-based calculations.
/// </summary>
public sealed record CustomerLocation(
    decimal Latitude,
    decimal Longitude,
    string? City = null,
    string? State = null);
```

### Nearest Location Strategy
```csharp
/// <summary>
/// Fulfillment strategy that ships from the closest location to the customer.
/// </summary>
public sealed class NearestLocationStrategy : IFulfillmentStrategy
{
    public async Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null,
        CancellationToken ct = default)
    {
        if (customerLocation == null)
            throw new InvalidOperationException("Nearest strategy requires customer location");

        // Sort locations by distance
        var sortedLocations = availableLocations
            .Where(l => l.HasLocation)
            .OrderBy(l => CalculateDistance(
                l.Latitude!.Value,
                l.Longitude!.Value,
                customerLocation.Latitude,
                customerLocation.Longitude))
            .ToList();

        var allocations = new List<FulfillmentAllocation>();
        int remaining = quantity;

        // Allocate from nearest first
        foreach (var location in sortedLocations)
        {
            if (remaining <= 0) break;

            var stockItem = location.StockItems
                .FirstOrDefault(si => si.VariantId == variant.Id);
            
            if (stockItem == null) continue;

            int allocate = Math.Min(stockItem.CountAvailable, remaining);
            
            allocations.Add(new(
                LocationId: location.Id,
                LocationName: location.Name,
                AllocatedQuantity: allocate
            ));

            remaining -= allocate;
        }

        return allocations;
    }

    /// <summary>
    /// Calculates distance between two coordinates using Haversine formula.
    /// </summary>
    private static decimal CalculateDistance(
        decimal fromLat,
        decimal fromLng,
        decimal toLat,
        decimal toLng)
    {
        const decimal earthRadiusKm = 6371;

        var fromLatRad = (double)(fromLat * Math.PI / 180);
        var toLatRad = (double)(toLat * Math.PI / 180);
        var deltaLat = (double)((toLat - fromLat) * Math.PI / 180);
        var deltaLng = (double)((toLng - fromLng) * Math.PI / 180);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(fromLatRad) * Math.Cos(toLatRad) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));
        return (decimal)(earthRadiusKm * c);
    }
}
```

### Highest Stock Strategy
```csharp
/// <summary>
/// Fulfillment strategy that ships from the location with the most inventory.
/// </summary>
public sealed class HighestStockStrategy : IFulfillmentStrategy
{
    public async Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null,
        CancellationToken ct = default)
    {
        // Sort by available quantity descending
        var sortedLocations = availableLocations
            .Select(l => new
            {
                Location = l,
                AvailableQty = l.StockItems
                    .FirstOrDefault(si => si.VariantId == variant.Id)?
                    .CountAvailable ?? 0
            })
            .OrderByDescending(x => x.AvailableQty)
            .ToList();

        var allocations = new List<FulfillmentAllocation>();
        int remaining = quantity;

        // Allocate from highest stock first
        foreach (var item in sortedLocations)
        {
            if (remaining <= 0) break;

            int allocate = Math.Min(item.AvailableQty, remaining);
            
            allocations.Add(new(
                LocationId: item.Location.Id,
                LocationName: item.Location.Name,
                AllocatedQuantity: allocate
            ));

            remaining -= allocate;
        }

        return allocations;
    }
}
```

---

## 4. Store Pickup Aggregate

### Complete StorePickup Class
```csharp
namespace ReSys.Core.Domain.Inventories.Locations;

public sealed class StorePickup : Aggregate, IHasAuditable
{
    #region Enums
    public enum PickupState
    {
        Pending = 0,      // Order placed, items being prepared
        Ready = 1,        // Items ready for pickup
        PickedUp = 2,     // Customer collected
        Cancelled = 3     // Pickup cancelled
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) =>
            Error.NotFound("StorePickup.NotFound", $"Store pickup '{id}' not found");

        public static Error InvalidStateTransition(PickupState current, PickupState requested) =>
            Error.Validation("StorePickup.InvalidStateTransition",
                $"Cannot transition from {current} to {requested}");

        public static Error AlreadyPickedUp =>
            Error.Conflict("StorePickup.AlreadyPickedUp",
                "Pickup has already been completed");

        public static Error NotReady =>
            Error.Conflict("StorePickup.NotReady",
                "Items are not ready for pickup yet");

        public static Error InvalidCode =>
            Error.Validation("StorePickup.InvalidCode",
                "Pickup code is incorrect");
    }
    #endregion

    #region Properties
    public Guid OrderId { get; set; }
    public Guid StockLocationId { get; set; }
    public PickupState State { get; private set; } = PickupState.Pending;
    public string PickupCode { get; private set; } = string.Empty;
    public string CustomerName { get; set; } = string.Empty;
    public string? CustomerPhone { get; set; }
    public string? CustomerEmail { get; set; }
    public DateTimeOffset? ReadyAt { get; private set; }
    public DateTimeOffset? PickedUpAt { get; private set; }
    public DateTimeOffset? CancelledAt { get; private set; }
    #endregion

    #region Relationships
    public Order Order { get; set; } = null!;
    public StockLocation StockLocation { get; set; } = null!;
    #endregion

    #region Factory
    public static ErrorOr<StorePickup> Create(
        Guid orderId,
        Guid stockLocationId,
        string customerName,
        string? customerPhone = null,
        string? customerEmail = null)
    {
        if (orderId == Guid.Empty || stockLocationId == Guid.Empty)
            return Error.Validation("StorePickup.InvalidInput", "Order and location IDs are required");

        if (string.IsNullOrWhiteSpace(customerName))
            return Error.Validation("StorePickup.NameRequired", "Customer name is required");

        var pickup = new StorePickup
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            StockLocationId = stockLocationId,
            CustomerName = customerName.Trim(),
            CustomerPhone = customerPhone?.Trim(),
            CustomerEmail = customerEmail?.Trim(),
            State = PickupState.Pending,
            PickupCode = GeneratePickupCode(),
            CreatedAt = DateTimeOffset.UtcNow
        };

        pickup.AddDomainEvent(new Events.Created(
            PickupId: pickup.Id,
            OrderId: orderId,
            PickupCode: pickup.PickupCode));

        return pickup;
    }

    private static string GeneratePickupCode()
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var random = new Random();
        return new string(Enumerable.Range(0, 6)
            .Select(_ => chars[random.Next(chars.Length)])
            .ToArray());
    }
    #endregion

    #region Business Logic
    public ErrorOr<Success> MarkReady()
    {
        if (State != PickupState.Pending)
            return Errors.InvalidStateTransition(State, PickupState.Ready);

        State = PickupState.Ready;
        ReadyAt = DateTimeOffset.UtcNow;
        
        AddDomainEvent(new Events.Ready(
            PickupId: Id,
            OrderId: OrderId));

        return Result.Success;
    }

    public ErrorOr<Success> CompletePickup(string verificationCode)
    {
        if (State != PickupState.Ready)
            return Errors.NotReady;

        if (verificationCode != PickupCode)
            return Errors.InvalidCode;

        State = PickupState.PickedUp;
        PickedUpAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.PickedUp(
            PickupId: Id,
            OrderId: OrderId));

        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string? reason = null)
    {
        if (State == PickupState.PickedUp)
            return Errors.AlreadyPickedUp;

        State = PickupState.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Cancelled(
            PickupId: Id,
            OrderId: OrderId,
            Reason: reason));

        return Result.Success;
    }
    #endregion

    #region Events
    public static class Events
    {
        public sealed record Created(Guid PickupId, Guid OrderId, string PickupCode) : DomainEvent;
        public sealed record Ready(Guid PickupId, Guid OrderId) : DomainEvent;
        public sealed record PickedUp(Guid PickupId, Guid OrderId) : DomainEvent;
        public sealed record Cancelled(Guid PickupId, Guid OrderId, string? Reason) : DomainEvent;
    }
    #endregion
}
```

---

## 5. EF Core Configuration Templates

### StockLocation Configuration
```csharp
public sealed class StockLocationConfiguration : IEntityTypeConfiguration<StockLocation>
{
    public void Configure(EntityTypeBuilder<StockLocation> builder)
    {
        builder.ToTable("stock_locations");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Name)
            .HasMaxLength(StockLocation.Constraints.NameMaxLength)
            .IsRequired();

        builder.Property(x => x.Presentation)
            .HasMaxLength(StockLocation.Constraints.PresentationMaxLength)
            .IsRequired();

        builder.Property(x => x.Type)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.ShipEnabled).IsRequired();
        builder.Property(x => x.PickupEnabled).IsRequired();
        builder.Property(x => x.Latitude);
        builder.Property(x => x.Longitude);

        builder.Property(x => x.Phone)
            .HasMaxLength(StockLocation.Constraints.PhoneMaxLength);

        builder.Property(x => x.Email)
            .HasMaxLength(256);

        builder.Property(x => x.OperatingHours)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<string, object?>>(v)!);

        // Metadata
        builder.Property(x => x.PublicMetadata)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<string, object?>>(v)!);

        builder.Property(x => x.PrivateMetadata)
            .HasConversion(
                v => JsonConvert.SerializeObject(v),
                v => JsonConvert.DeserializeObject<Dictionary<string, object?>>(v)!);

        // Soft Delete
        builder.Property(x => x.DeletedAt);

        // Indexes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => new { x.ShipEnabled, x.IsDeleted });
        builder.HasIndex(x => new { x.PickupEnabled, x.IsDeleted });
        builder.HasIndex(x => new { x.Latitude, x.Longitude });

        // Query Filter
        builder.HasQueryFilter(x => !x.IsDeleted);

        // Relationships
        builder.HasMany(x => x.StockItems)
            .WithOne(x => x.StockLocation)
            .HasForeignKey(x => x.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
```

### StorePickup Configuration
```csharp
public sealed class StorePickupConfiguration : IEntityTypeConfiguration<StorePickup>
{
    public void Configure(EntityTypeBuilder<StorePickup> builder)
    {
        builder.ToTable("store_pickups");
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.StockLocationId).IsRequired();

        builder.Property(x => x.State)
            .HasConversion<string>()
            .IsRequired();

        builder.Property(x => x.PickupCode)
            .HasMaxLength(10)
            .IsRequired();

        builder.Property(x => x.CustomerName)
            .HasMaxLength(255)
            .IsRequired();

        builder.Property(x => x.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(x => x.CustomerEmail)
            .HasMaxLength(255);

        // Timestamps
        builder.Property(x => x.ReadyAt);
        builder.Property(x => x.PickedUpAt);
        builder.Property(x => x.CancelledAt);

        // Relationships
        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockLocation)
            .WithMany()
            .HasForeignKey(x => x.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        // Indexes
        builder.HasIndex(x => x.PickupCode).IsUnique();
        builder.HasIndex(x => new { x.OrderId, x.State });
        builder.HasIndex(x => new { x.StockLocationId, x.State });
    }
}
```

---

## 6. Command Handler Templates

### FulfillOrderCommand & Handler
```csharp
public sealed record FulfillOrderCommand(
    Guid OrderId,
    FulfillmentStrategyType Strategy = FulfillmentStrategyType.Nearest
) : ICommand<FulfillOrderResponse>;

public enum FulfillmentStrategyType { Nearest, HighestStock, CostOptimized, Preferred }

public sealed class FulfillOrderHandler : ICommandHandler<FulfillOrderCommand, FulfillOrderResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IFulfillmentStrategyFactory _strategyFactory;
    private readonly ISender _sender;

    public FulfillOrderHandler(
        IApplicationDbContext db,
        IFulfillmentStrategyFactory strategyFactory,
        ISender sender)
    {
        _db = db;
        _strategyFactory = strategyFactory;
        _sender = sender;
    }

    public async Task<ErrorOr<FulfillOrderResponse>> Handle(
        FulfillOrderCommand command,
        CancellationToken ct)
    {
        // Get order with line items
        var order = await _db.Orders
            .Include(o => o.LineItems)
            .ThenInclude(li => li.Variant)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

        if (order == null)
            return Error.NotFound("Order.NotFound", $"Order '{command.OrderId}' not found");

        // Separate by fulfillment method
        var shipItems = order.LineItems
            .Where(li => li.FulfillmentMethod == FulfillmentMethod.Ship)
            .ToList();

        var pickupItems = order.LineItems
            .Where(li => li.FulfillmentMethod == FulfillmentMethod.Pickup)
            .ToList();

        // Process shipping items
        if (shipItems.Any())
        {
            var shipResult = await ProcessShippingAsync(
                order,
                shipItems,
                command.Strategy,
                ct);

            if (shipResult.IsError)
                return shipResult;
        }

        // Process pickup items
        if (pickupItems.Any())
        {
            var pickupResult = await ProcessPickupAsync(order, pickupItems, ct);
            if (pickupResult.IsError)
                return pickupResult;
        }

        // Save
        await _db.SaveChangesAsync(ct);

        return new FulfillOrderResponse(
            OrderId: order.Id,
            Message: "Order fulfilled successfully");
    }

    private async Task<ErrorOr<Success>> ProcessShippingAsync(
        Order order,
        List<LineItem> items,
        FulfillmentStrategyType strategyType,
        CancellationToken ct)
    {
        var strategy = _strategyFactory.Create(strategyType);

        // Group by variant
        foreach (var lineItemGroup in items.GroupBy(li => li.VariantId))
        {
            var variant = lineItemGroup.First().Variant;
            int totalQty = lineItemGroup.Sum(li => li.Quantity);

            // Get available locations
            var availableLocations = await _db.StockLocations
                .Where(l => l.CanShip &&
                       l.StockItems.Any(si =>
                           si.VariantId == variant.Id &&
                           si.CountAvailable >= totalQty))
                .Include(l => l.StockItems)
                .ToListAsync(ct);

            if (!availableLocations.Any())
                return Error.Conflict("StockItem.InsufficientStock",
                    $"Insufficient stock for variant '{variant.Id}'");

            // Allocate
            var customerLocation = order.ShippingAddress != null
                ? new CustomerLocation(
                    Latitude: (decimal)order.ShippingAddress.Latitude,
                    Longitude: (decimal)order.ShippingAddress.Longitude,
                    City: order.ShippingAddress.City)
                : null;

            var allocations = await strategy.AllocateAsync(
                variant,
                totalQty,
                availableLocations,
                customerLocation,
                ct);

            // Create shipments
            foreach (var allocation in allocations)
            {
                var location = availableLocations.First(l => l.Id == allocation.LocationId);
                var result = await CreateShipmentAsync(
                    order,
                    location,
                    variant,
                    allocation.AllocatedQuantity,
                    ct);

                if (result.IsError)
                    return result;
            }
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> ProcessPickupAsync(
        Order order,
        List<LineItem> items,
        CancellationToken ct)
    {
        var pickupLocation = await _db.StockLocations
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == order.PreferredPickupLocationId, ct);

        if (pickupLocation == null)
            return Error.NotFound("StockLocation.NotFound", "Pickup location not found");

        // Verify all items available
        foreach (var item in items)
        {
            var stock = await _db.StockItems
                .FirstOrDefaultAsync(
                    si => si.VariantId == item.VariantId &&
                          si.StockLocationId == pickupLocation.Id,
                    ct);

            if (stock == null || stock.CountAvailable < item.Quantity)
                return Error.Conflict("StockItem.InsufficientStock",
                    $"Item '{item.Variant.Sku}' unavailable at pickup location");
        }

        // Deduct stock for each item
        foreach (var item in items)
        {
            var stock = await _db.StockItems
                .FirstAsync(
                    si => si.VariantId == item.VariantId &&
                          si.StockLocationId == pickupLocation.Id,
                    ct);

            var result = stock.ConfirmShipment(order.Id, item.Quantity);
            if (result.IsError)
                return result;
        }

        // Create pickup record
        var pickup = StorePickup.Create(
            order.Id,
            pickupLocation.Id,
            order.User.FullName,
            order.User.PhoneNumber,
            order.User.Email);

        if (pickup.IsError)
            return pickup;

        _db.StorePickups.Add(pickup.Value);
        return Result.Success;
    }

    private async Task<ErrorOr<Success>> CreateShipmentAsync(
        Order order,
        StockLocation location,
        Variant variant,
        int quantity,
        CancellationToken ct)
    {
        // Create shipment
        var shipment = Shipment.Create(order.Id, location.Id, quantity);
        if (shipment.IsError)
            return shipment;

        // Deduct stock
        var stock = await _db.StockItems
            .FirstAsync(
                si => si.VariantId == variant.Id &&
                      si.StockLocationId == location.Id,
                ct);

        var result = stock.ConfirmShipment(order.Id, quantity);
        if (result.IsError)
            return result;

        _db.Shipments.Add(shipment.Value);
        return Result.Success;
    }
}

public sealed record FulfillOrderResponse(
    Guid OrderId,
    string Message);
```

---

## 7. Validator Templates

### Fulfillment Validator
```csharp
public sealed class FulfillOrderValidator : AbstractValidator<FulfillOrderCommand>
{
    public FulfillOrderValidator()
    {
        RuleFor(x => x.OrderId)
            .NotEmpty()
            .WithMessage("Order ID is required");

        RuleFor(x => x.Strategy)
            .IsInEnum()
            .WithMessage("Fulfillment strategy must be a valid value");
    }
}
```

---

## 8. Test Template

### Unit Test Example
```csharp
[TestFixture]
public class StockItemReservationTests
{
    [Test]
    public void Reserve_WithSufficientStock_Succeeds()
    {
        // Arrange
        var variantId = Guid.NewGuid();
        var locationId = Guid.NewGuid();
        var stockItem = StockItem.Create(variantId, locationId, "TEST-SKU");
        stockItem.QuantityOnHand = 100;

        var orderId = Guid.NewGuid();

        // Act
        var result = stockItem.Reserve(orderId, 50);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(stockItem.QuantityReserved, Is.EqualTo(50));
        Assert.That(stockItem.CountAvailable, Is.EqualTo(50));
    }

    [Test]
    public void Reserve_WithInsufficientStock_ReturnsFail()
    {
        // Arrange
        var stockItem = StockItem.Create(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU");
        stockItem.QuantityOnHand = 30;

        var orderId = Guid.NewGuid();

        // Act
        var result = stockItem.Reserve(orderId, 50);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError.Code, Is.EqualTo("StockItem.InsufficientStock"));
    }

    [Test]
    public void ConfirmShipment_DeductsFromBothCountsAndRemovesReservation()
    {
        // Arrange
        var stockItem = StockItem.Create(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU");
        stockItem.QuantityOnHand = 100;
        var orderId = Guid.NewGuid();

        stockItem.Reserve(orderId, 50);
        Assert.That(stockItem.QuantityReserved, Is.EqualTo(50));

        // Act
        var result = stockItem.ConfirmShipment(orderId, 50);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(stockItem.QuantityOnHand, Is.EqualTo(50));
        Assert.That(stockItem.QuantityReserved, Is.EqualTo(0));
    }

    [Test]
    public void Release_DecreasesReservation()
    {
        // Arrange
        var stockItem = StockItem.Create(Guid.NewGuid(), Guid.NewGuid(), "TEST-SKU");
        stockItem.QuantityOnHand = 100;
        var orderId = Guid.NewGuid();

        stockItem.Reserve(orderId, 50);
        Assert.That(stockItem.QuantityReserved, Is.EqualTo(50));

        // Act
        var result = stockItem.Release(orderId, 30);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(stockItem.QuantityReserved, Is.EqualTo(20));
    }
}
```

---

## 🚀 Quick Start Checklist

Use this when implementing each phase:

```
[ ] Read the phase description in MULTI_LOCATION_FULFILLMENT_PLAN.md
[ ] Create domain model first (never services/queries before domain)
[ ] Write factory method with validation
[ ] Add domain events
[ ] Write unit tests (TDD)
[ ] Create EF Configuration
[ ] Create validators (FluentValidation)
[ ] Create command/query handlers (MediatR)
[ ] Create mappers (Mapster)
[ ] Create API endpoints
[ ] Write integration tests
[ ] Update README.md with examples
[ ] Create migration (dotnet ef migrations add)
[ ] Test with database
```

---

**Next Steps:**
1. Start with Phase 1: StockLocation & StockItem enhancements
2. Use snippets from this document to accelerate implementation
3. Reference `PaymentMethod.cs` for complete pattern examples
4. Follow ReSys.Shop architectural guidelines in `.github/copilot-instructions.md`
