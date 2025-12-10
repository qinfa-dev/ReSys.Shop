# Multi-Location Retail Fulfillment System - Implementation Plan

**ReSys.Shop** | One Brand → Multiple Retail Stores + Warehouses → One Website

---

## 📋 Executive Summary

This document outlines the comprehensive implementation of a **multi-location fulfillment system** for ReSys.Shop. The system enables:

- ✅ **One website** serving customers nationwide
- ✅ **Multiple locations** (warehouses for shipping, retail stores for pickup, hybrid locations)
- ✅ **Smart fulfillment** (nearest location, cost-optimized, inventory-balanced)
- ✅ **Store pickup** with real-time availability and pickup codes
- ✅ **Stock transfers** between locations for inventory balancing
- ✅ **Split shipments** when single location can't fulfill entire order

---

## 🏗️ Architecture Overview

### Current Foundation (Already Exists)

```
ReSys.Core.Domain.Inventories/
├── Locations/
│   ├── StockLocation.cs       ✅ Aggregate Root (needs enhancement)
│   └── StockLocationConfiguration.cs
├── Stocks/
│   ├── StockItem.cs           ✅ Owned Entity (needs reserve/release methods)
│   └── StockItemConfiguration.cs
└── README.md
```

**Existing Capabilities:**
- StockLocation: Address, Name, Presentation, Metadata, Soft-delete
- StockItem: QuantityOnHand, QuantityReserved, CountAvailable, Backorderable, StockMovements

**What's Missing:**
- Location type classification (warehouse vs retail_store vs both)
- Fulfillment capabilities (ship_enabled, pickup_enabled)
- Geographic data for distance calculations
- Geographic queries for nearest location
- Fulfillment strategy pattern
- Store pickup management
- Stock transfer management
- Fulfillment orchestration

---

## 🎯 Implementation Phases

### PHASE 1: Domain Model Enhancement (Weeks 1-2)

#### 1.1 Extend StockLocation Aggregate

**File:** `src/ReSys.Core/Domain/Inventories/Locations/StockLocation.cs`

**Add Properties:**
```csharp
// Location Type Classification
public enum LocationType { Warehouse, RetailStore, Both }
public LocationType Type { get; set; } // warehouse | retail_store | both

// Fulfillment Capabilities
public bool ShipEnabled { get; set; } = true;      // Can ship orders
public bool PickupEnabled { get; set; } = false;   // In-store pickup available

// Geographic Data (for distance calculations)
public decimal? Latitude { get; set; }
public decimal? Longitude { get; set; }

// Contact Information
public string? Phone { get; set; }
public string? Email { get; set; }

// Operating Hours (JSON)
public IDictionary<string, object?>? OperatingHours { get; set; }
```

**Add Factory Method:**
```csharp
public static ErrorOr<StockLocation> Create(
    string name,
    string presentation,
    LocationType type,
    bool shipEnabled = true,
    bool pickupEnabled = false,
    decimal? latitude = null,
    decimal? longitude = null,
    // ... other params
)
```

**Add Query Methods:**
```csharp
public bool CanShip => ShipEnabled && !IsDeleted;
public bool CanPickup => PickupEnabled && !IsDeleted;
public bool IsWarehouse => Type == LocationType.Warehouse;
public bool IsRetailStore => Type == LocationType.RetailStore;
public bool IsHybrid => Type == LocationType.Both;
public bool HasLocation => Latitude.HasValue && Longitude.HasValue;
```

**Add Domain Events:**
```csharp
public static class Events
{
    public sealed record Created(Guid LocationId, string Name, LocationType Type) : DomainEvent;
    public sealed record Updated(Guid LocationId) : DomainEvent;
    public sealed record Deleted(Guid LocationId) : DomainEvent;
}
```

---

#### 1.2 Enhance StockItem with Reservation Methods

**File:** `src/ReSys.Core/Domain/Inventories/Stocks/StockItem.cs`

**Add Methods:**

```csharp
// Reserve stock for pending order (cart/checkout)
public ErrorOr<Success> Reserve(Guid orderId, int quantity)
{
    if (quantity <= 0) return Errors.InvalidQuantity;
    if (!_reservations.ContainsKey(orderId) && CountAvailable < quantity)
        return Errors.InsufficientStock(CountAvailable, quantity);
    
    if (_reservations.TryGetValue(orderId, out var existing))
    {
        if (existing == quantity) return Result.Success;
        
        int diff = quantity - existing;
        if (diff > 0 && CountAvailable < diff)
            return Errors.InsufficientStock(CountAvailable, diff);
    }
    
    _reservations[orderId] = quantity;
    QuantityReserved = _reservations.Values.Sum();
    
    AddDomainEvent(new Events.Reserved(StockItemId: Id, OrderId: orderId, Quantity: quantity));
    return Result.Success;
}

// Release reserved stock (cart abandoned, payment failed)
public ErrorOr<Success> Release(Guid orderId, int? quantity = null)
{
    if (!_reservations.TryGetValue(orderId, out var reserved))
        return Result.Success; // Already released
    
    int releaseQty = quantity ?? reserved;
    if (releaseQty > reserved)
        return Errors.InvalidRelease(reserved, releaseQty);
    
    _reservations[orderId] -= releaseQty;
    if (_reservations[orderId] <= 0)
        _reservations.Remove(orderId);
    
    QuantityReserved = _reservations.Values.Sum();
    
    AddDomainEvent(new Events.Released(StockItemId: Id, OrderId: orderId, Quantity: releaseQty));
    return Result.Success;
}

// Confirm shipment (actually deduct inventory)
public ErrorOr<Success> ConfirmShipment(Guid orderId, int quantity)
{
    if (!_reservations.TryGetValue(orderId, out var reserved))
        return Errors.InvalidShipment(0, quantity);
    
    if (reserved < quantity)
        return Errors.InvalidShipment(reserved, quantity);
    
    if (QuantityOnHand < quantity)
        return Errors.InsufficientStock(QuantityOnHand, quantity);
    
    QuantityOnHand -= quantity;
    _reservations[orderId] -= quantity;
    if (_reservations[orderId] <= 0)
        _reservations.Remove(orderId);
    
    QuantityReserved = _reservations.Values.Sum();
    
    AddDomainEvent(new Events.Shipped(StockItemId: Id, OrderId: orderId, Quantity: quantity));
    return Result.Success;
}

// Adjust stock (restock, damage, shrinkage)
public ErrorOr<Success> Adjust(int deltaQuantity, string reason)
{
    int newOnHand = QuantityOnHand + deltaQuantity;
    if (newOnHand < 0) return Errors.InvalidQuantity;
    if (newOnHand < QuantityReserved) return Errors.ReservedExceedsOnHand;
    
    QuantityOnHand = newOnHand;
    
    AddDomainEvent(new Events.Adjusted(
        StockItemId: Id,
        DeltaQuantity: deltaQuantity,
        Reason: reason,
        NewQuantity: QuantityOnHand
    ));
    
    return Result.Success;
}
```

**Add Domain Events:**
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

### PHASE 2: Fulfillment Strategy Pattern (Week 2)

#### 2.1 Create Fulfillment Strategy Interface

**New File:** `src/ReSys.Core/Domain/Inventories/Locations/FulfillmentStrategies/IFulfillmentStrategy.cs`

```csharp
namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

public interface IFulfillmentStrategy
{
    /// <summary>
    /// Allocates inventory from available stock locations based on strategy rules.
    /// </summary>
    /// <param name="variant">Product variant to fulfill</param>
    /// <param name="quantity">Required quantity</param>
    /// <param name="availableLocations">Locations with sufficient stock</param>
    /// <param name="customerLocation">Customer's shipping address (lat/lng)</param>
    /// <returns>List of allocations (location, quantity) in order of fulfillment</returns>
    Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null);
}

public sealed record FulfillmentAllocation(
    Guid LocationId,
    string LocationName,
    int AllocatedQuantity,
    decimal? ShippingCost = null,
    int? EstimatedDays = null);

public sealed record CustomerLocation(
    decimal Latitude,
    decimal Longitude,
    string? City = null,
    string? State = null);
```

---

#### 2.2 Implement Concrete Strategies

**New File:** `src/ReSys.Core/Domain/Inventories/Locations/FulfillmentStrategies/NearestLocationStrategy.cs`

```csharp
public sealed class NearestLocationStrategy : IFulfillmentStrategy
{
    public async Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null)
    {
        if (customerLocation == null)
            throw new InvalidOperationException("Nearest strategy requires customer location");

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

        foreach (var location in sortedLocations)
        {
            if (remaining <= 0) break;

            var stockItem = location.StockItems.FirstOrDefault(si => si.VariantId == variant.Id);
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

    private static decimal CalculateDistance(
        decimal fromLat,
        decimal fromLng,
        decimal toLat,
        decimal toLng)
    {
        const decimal earthRadiusKm = 6371;

        var fromLatRad = (double)(fromLat * Math.PI / 180);
        var toLtRad = (double)(toLat * Math.PI / 180);
        var deltaLat = (double)((toLat - fromLat) * Math.PI / 180);
        var deltaLng = (double)((toLng - fromLng) * Math.PI / 180);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(fromLatRad) * Math.Cos(toLtRad) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));
        return (decimal)(earthRadiusKm * c);
    }
}
```

**New File:** `src/ReSys.Core/Domain/Inventories/Locations/FulfillmentStrategies/HighestStockStrategy.cs`

```csharp
public sealed class HighestStockStrategy : IFulfillmentStrategy
{
    public async Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null)
    {
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

**New File:** `src/ReSys.Core/Domain/Inventories/Locations/FulfillmentStrategies/CostOptimizedStrategy.cs`

```csharp
public sealed class CostOptimizedStrategy : IFulfillmentStrategy
{
    private readonly IShippingCalculatorService _shippingCalculator;

    public CostOptimizedStrategy(IShippingCalculatorService shippingCalculator)
    {
        _shippingCalculator = shippingCalculator;
    }

    public async Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null)
    {
        if (customerLocation == null)
            throw new InvalidOperationException("Cost optimized strategy requires customer location");

        var locationsWithCost = new List<(StockLocation Location, decimal Cost, int Available)>();

        foreach (var location in availableLocations)
        {
            var stockItem = location.StockItems.FirstOrDefault(si => si.VariantId == variant.Id);
            if (stockItem == null) continue;

            var cost = await _shippingCalculator.CalculateAsync(new()
            {
                FromLatitude = location.Latitude ?? 0,
                FromLongitude = location.Longitude ?? 0,
                ToLatitude = customerLocation.Latitude,
                ToLongitude = customerLocation.Longitude,
                Weight = variant.Weight,
                Quantity = quantity
            });

            locationsWithCost.Add((location, cost, stockItem.CountAvailable));
        }

        var allocations = new List<FulfillmentAllocation>();
        int remaining = quantity;

        foreach (var (location, cost, available) in locationsWithCost.OrderBy(x => x.Cost))
        {
            if (remaining <= 0) break;

            int allocate = Math.Min(available, remaining);
            allocations.Add(new(
                LocationId: location.Id,
                LocationName: location.Name,
                AllocatedQuantity: allocate,
                ShippingCost: cost / allocate // Per unit cost
            ));

            remaining -= allocate;
        }

        return allocations;
    }
}
```

---

### PHASE 3: Store Pickup Management (Week 2-3)

#### 3.1 Create StorePickup Aggregate

**New File:** `src/ReSys.Core/Domain/Inventories/Locations/StorePickup.cs`

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
                "Items are not ready for pickup");
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

        pickup.AddDomainEvent(new Events.Created(pickup.Id, orderId));
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
        AddDomainEvent(new Events.Ready(Id, OrderId));
        return Result.Success;
    }

    public ErrorOr<Success> CompletePickup(string verificationCode)
    {
        if (State != PickupState.Ready)
            return Errors.NotReady;

        if (verificationCode != PickupCode)
            return Error.Validation("StorePickup.InvalidCode", "Pickup code is incorrect");

        State = PickupState.PickedUp;
        PickedUpAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.PickedUp(Id, OrderId));
        return Result.Success;
    }

    public ErrorOr<Success> Cancel(string? reason = null)
    {
        if (State == PickupState.PickedUp)
            return Errors.AlreadyPickedUp;

        State = PickupState.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Cancelled(Id, OrderId, reason));
        return Result.Success;
    }
    #endregion

    #region Events
    public static class Events
    {
        public sealed record Created(Guid PickupId, Guid OrderId) : DomainEvent;
        public sealed record Ready(Guid PickupId, Guid OrderId) : DomainEvent;
        public sealed record PickedUp(Guid PickupId, Guid OrderId) : DomainEvent;
        public sealed record Cancelled(Guid PickupId, Guid OrderId, string? Reason) : DomainEvent;
    }
    #endregion
}
```

---

#### 3.2 Create StorePickup Configuration

**New File:** `src/ReSys.Infrastructure/Persistence/Configurations/StorePickupConfiguration.cs`

```csharp
public sealed class StorePickupConfiguration : IEntityTypeConfiguration<StorePickup>
{
    public void Configure(EntityTypeBuilder<StorePickup> builder)
    {
        builder.ToTable("store_pickups");
        builder.HasKey(x => x.Id);

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

        builder.HasOne(x => x.Order)
            .WithMany()
            .HasForeignKey(x => x.OrderId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(x => x.StockLocation)
            .WithMany()
            .HasForeignKey(x => x.StockLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => x.PickupCode).IsUnique();
        builder.HasIndex(x => new { x.OrderId, x.State });
    }
}
```

---

### PHASE 4: Stock Transfer Management (Week 3)

#### 4.1 Extend StockTransfer Aggregate

**File:** `src/ReSys.Core/Domain/Inventories/Locations/StockTransfer.cs` (already exists, needs enhancement)

**Key additions:**

```csharp
public sealed class StockTransfer : Aggregate, IHasAuditable
{
    public enum TransferState { Pending, InTransit, Received, Cancelled }

    // Add these methods:

    public ErrorOr<Success> Initiate()
    {
        if (State != TransferState.Pending)
            return Error.Validation("...", "...");

        // Deduct from source location
        foreach (var item in Items)
        {
            var stockItem = SourceLocation.StockItems
                .First(si => si.VariantId == item.VariantId);
            
            var result = stockItem.Adjust(-item.Quantity, $"Transfer {Id} to {DestinationLocation.Name}");
            if (result.IsError) return result;
        }

        State = TransferState.InTransit;
        ShippedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Initiated(Id, SourceLocationId, DestinationLocationId));
        return Result.Success;
    }

    public ErrorOr<Success> Receive(Dictionary<Guid, int> receivedQuantities)
    {
        if (State != TransferState.InTransit)
            return Error.Validation("...", "...");

        // Add to destination location
        foreach (var item in Items)
        {
            int receivedQty = receivedQuantities.GetValueOrDefault(item.Id, item.Quantity);
            
            var stockItem = DestinationLocation.StockItems
                .FirstOrDefault(si => si.VariantId == item.VariantId)
                ?? StockItem.Create(...);

            var result = stockItem.Adjust(receivedQty, $"Received from {SourceLocation.Name}");
            if (result.IsError) return result;

            item.ReceivedQuantity = receivedQty;
        }

        State = TransferState.Received;
        ReceivedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Received(Id));
        return Result.Success;
    }
}
```

---

### PHASE 5: Order Fulfillment Orchestration (Week 3-4)

#### 5.1 Extend Order for Fulfillment

**File:** `src/ReSys.Core/Domain/Orders/Order.cs`

**Add Properties:**

```csharp
public enum FulfillmentType { Ship, Pickup, Mixed }

public FulfillmentType FulfillmentMethod { get; set; } = FulfillmentType.Ship;
public Guid? PreferredPickupLocationId { get; set; }
public StockLocation? PreferredPickupLocation { get; set; }
public ICollection<StorePickup> Pickups { get; set; } = new List<StorePickup>();
public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();

// Per-item fulfillment choice
public ICollection<LineItemFulfillmentChoice> FulfillmentChoices { get; set; } = new List<LineItemFulfillmentChoice>();
```

#### 5.2 Create Fulfillment Orchestration Command Handler

**New File:** `src/ReSys.Core/Feature/Orders/Commands/FulfillOrder/FulfillOrderCommand.cs`

```csharp
public sealed record FulfillOrderCommand(
    Guid OrderId,
    FulfillmentStrategy Strategy = FulfillmentStrategy.Nearest
) : ICommand<FulfillOrderResponse>;

public enum FulfillmentStrategy { Nearest, HighestStock, CostOptimized, Preferred }

public sealed class FulfillOrderHandler : ICommandHandler<FulfillOrderCommand, FulfillOrderResponse>
{
    private readonly IApplicationDbContext _db;
    private readonly IFulfillmentStrategyFactory _strategyFactory;
    private readonly ISender _sender;

    public async Task<ErrorOr<FulfillOrderResponse>> Handle(
        FulfillOrderCommand command,
        CancellationToken ct)
    {
        var order = await _db.Orders
            .Include(o => o.LineItems)
            .ThenInclude(li => li.Variant)
            .FirstOrDefaultAsync(o => o.Id == command.OrderId, ct);

        if (order == null) return Error.NotFound();

        var strategy = _strategyFactory.Create(command.Strategy);

        // Group line items by fulfillment type (ship vs pickup)
        var shipItems = order.LineItems
            .Where(li => li.FulfillmentMethod == FulfillmentMethod.Ship)
            .ToList();
        
        var pickupItems = order.LineItems
            .Where(li => li.FulfillmentMethod == FulfillmentMethod.Pickup)
            .ToList();

        // Process shipping items
        if (shipItems.Any())
        {
            var shipResult = await ProcessShippingAsync(order, shipItems, strategy, ct);
            if (shipResult.IsError) return shipResult;
        }

        // Process pickup items
        if (pickupItems.Any())
        {
            var pickupResult = await ProcessPickupAsync(order, pickupItems, ct);
            if (pickupResult.IsError) return pickupResult;
        }

        await _db.SaveChangesAsync(ct);

        return new FulfillOrderResponse(order.Id, "Order fulfilled successfully");
    }

    private async Task<ErrorOr<Success>> ProcessShippingAsync(
        Order order,
        List<LineItem> items,
        IFulfillmentStrategy strategy,
        CancellationToken ct)
    {
        // For each variant, find available locations
        foreach (var lineItem in items.GroupBy(li => li.VariantId))
        {
            var variant = lineItem.First().Variant;
            int totalQty = lineItem.Sum(li => li.Quantity);

            var availableLocations = await _db.StockLocations
                .Where(l => l.CanShip)
                .Include(l => l.StockItems)
                .ToListAsync(ct);

            var allocations = await strategy.AllocateAsync(
                variant,
                totalQty,
                availableLocations,
                order.ShippingAddress != null ? new CustomerLocation(
                    order.ShippingAddress.Latitude,
                    order.ShippingAddress.Longitude,
                    order.ShippingAddress.City) : null);

            // Create shipments for each allocation
            foreach (var allocation in allocations)
            {
                var location = availableLocations.First(l => l.Id == allocation.LocationId);
                var shipment = await CreateShipmentAsync(order, location, lineItem, allocation, ct);
                if (shipment.IsError) return shipment;
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
            .FirstOrDefaultAsync(l => l.Id == order.PreferredPickupLocationId, ct);

        if (pickupLocation == null)
            return Error.NotFound("Pickup location not found");

        // Verify all items available at location
        foreach (var item in items)
        {
            var stock = pickupLocation.StockItems
                .FirstOrDefault(si => si.VariantId == item.VariantId);

            if (stock == null || stock.CountAvailable < item.Quantity)
                return Error.Conflict("Insufficient stock at pickup location");
        }

        // Deduct stock
        foreach (var item in items)
        {
            var stock = pickupLocation.StockItems
                .First(si => si.VariantId == item.VariantId);

            var result = stock.ConfirmShipment(order.Id, item.Quantity);
            if (result.IsError) return result;
        }

        // Create pickup record
        var pickup = StorePickup.Create(
            order.Id,
            pickupLocation.Id,
            order.User.FullName,
            order.User.PhoneNumber,
            order.User.Email);

        if (pickup.IsError) return pickup;

        _db.StorePickups.Add(pickup.Value);
        return Result.Success;
    }

    private async Task<ErrorOr<Success>> CreateShipmentAsync(
        Order order,
        StockLocation location,
        IGrouping<Guid, LineItem> items,
        FulfillmentAllocation allocation,
        CancellationToken ct)
    {
        // Create shipment
        var shipment = Shipment.Create(order.Id, location.Id);
        if (shipment.IsError) return shipment;

        // Deduct stock
        var stockItem = location.StockItems
            .First(si => si.VariantId == items.Key);

        var result = stockItem.ConfirmShipment(order.Id, allocation.AllocatedQuantity);
        if (result.IsError) return result;

        _db.Shipments.Add(shipment.Value);
        return Result.Success;
    }
}
```

---

### PHASE 6: Query Services (Week 4)

#### 6.1 Stock Availability Query

**New File:** `src/ReSys.Core/Feature/Inventories/Queries/CheckStockAvailability/CheckStockAvailabilityQuery.cs`

```csharp
public sealed record CheckStockAvailabilityQuery(
    Guid VariantId,
    int RequestedQuantity,
    decimal? CustomerLatitude = null,
    decimal? CustomerLongitude = null
) : IQuery<StockAvailabilityResponse>;

public sealed class CheckStockAvailabilityHandler : IQueryHandler<CheckStockAvailabilityQuery, StockAvailabilityResponse>
{
    private readonly IApplicationDbContext _db;

    public async Task<ErrorOr<StockAvailabilityResponse>> Handle(
        CheckStockAvailabilityQuery query,
        CancellationToken ct)
    {
        // Check online availability (shipping)
        var totalShippable = await _db.StockItems
            .Where(si => si.VariantId == query.VariantId &&
                   si.StockLocation.ShipEnabled &&
                   !si.StockLocation.IsDeleted)
            .SumAsync(si => si.CountAvailable, ct);

        var onlineAvailable = totalShippable >= query.RequestedQuantity;

        // Check pickup availability
        var pickupLocations = new List<PickupLocationDto>();
        
        if (query.CustomerLatitude.HasValue && query.CustomerLongitude.HasValue)
        {
            pickupLocations = await GetNearbyPickupLocations(
                query.VariantId,
                query.RequestedQuantity,
                query.CustomerLatitude.Value,
                query.CustomerLongitude.Value,
                ct);
        }

        return new StockAvailabilityResponse(
            VariantId: query.VariantId,
            OnlineAvailable: onlineAvailable,
            PickupAvailable: pickupLocations.Any(),
            NearbyPickupLocations: pickupLocations);
    }

    private async Task<List<PickupLocationDto>> GetNearbyPickupLocations(
        Guid variantId,
        int quantity,
        decimal lat,
        decimal lng,
        CancellationToken ct)
    {
        var locations = await _db.StockLocations
            .Where(l => l.PickupEnabled &&
                   !l.IsDeleted &&
                   l.Latitude != null &&
                   l.Longitude != null)
            .Include(l => l.StockItems)
            .ToListAsync(ct);

        return locations
            .Where(l => l.StockItems
                .Where(si => si.VariantId == variantId)
                .Sum(si => si.CountAvailable) >= quantity)
            .Select(l => new PickupLocationDto(
                Id: l.Id,
                Name: l.Presentation,
                Address: $"{l.Address1}, {l.City}",
                Phone: l.Phone,
                AvailableQuantity: l.StockItems
                    .Where(si => si.VariantId == variantId)
                    .Sum(si => si.CountAvailable),
                Distance: CalculateDistance(l.Latitude!.Value, l.Longitude!.Value, lat, lng),
                OperatingHours: l.OperatingHours))
            .OrderBy(p => p.Distance)
            .Take(10)
            .ToList();
    }

    private static decimal CalculateDistance(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        // Haversine formula implementation
        const decimal earthRadiusKm = 6371;
        var lat1Rad = (double)(lat1 * (decimal)Math.PI / 180);
        var lat2Rad = (double)(lat2 * (decimal)Math.PI / 180);
        var deltaLat = (double)((lat2 - lat1) * (decimal)Math.PI / 180);
        var deltaLng = (double)((lng2 - lng1) * (decimal)Math.PI / 180);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));
        return (decimal)(earthRadiusKm * c);
    }
}

public sealed record StockAvailabilityResponse(
    Guid VariantId,
    bool OnlineAvailable,
    bool PickupAvailable,
    List<PickupLocationDto> NearbyPickupLocations);

public sealed record PickupLocationDto(
    Guid Id,
    string Name,
    string Address,
    string? Phone,
    int AvailableQuantity,
    decimal Distance,
    IDictionary<string, object?>? OperatingHours);
```

---

### PHASE 7: API Endpoints (Week 4)

#### 7.1 Fulfillment Endpoints

**New File:** `src/ReSys.API/Endpoints/OrderFulfillmentEndpoints.cs`

```csharp
public static class OrderFulfillmentEndpoints
{
    public static void MapOrderFulfillmentEndpoints(this WebApplication app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Order Fulfillment");

        group.MapPost("{orderId:guid}/fulfill", FulfillOrder)
            .WithName("FulfillOrder")
            .WithOpenApi();

        group.MapGet("{orderId:guid}/availability", CheckAvailability)
            .WithName("CheckAvailability")
            .WithOpenApi();

        group.MapPost("{orderId:guid}/pickups", CreatePickup)
            .WithName("CreatePickup")
            .WithOpenApi();

        group.MapPut("pickups/{pickupId:guid}/ready", MarkPickupReady)
            .WithName("MarkPickupReady")
            .WithOpenApi();

        group.MapPut("pickups/{pickupId:guid}/complete", CompletePickup)
            .WithName("CompletePickup")
            .WithOpenApi();
    }

    private static async Task<IResult> FulfillOrder(
        Guid orderId,
        ISender sender,
        [FromBody] FulfillOrderRequest request,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new FulfillOrderCommand(orderId, request.Strategy),
            ct);

        return result.Match(
            onValue: response => Results.Ok(response),
            onError: errors => Results.BadRequest(errors.First().Description));
    }

    private static async Task<IResult> CheckAvailability(
        Guid variantId,
        decimal? lat,
        decimal? lng,
        int quantity,
        ISender sender,
        CancellationToken ct)
    {
        var result = await sender.Send(
            new CheckStockAvailabilityQuery(variantId, quantity, lat, lng),
            ct);

        return result.Match(
            onValue: response => Results.Ok(response),
            onError: errors => Results.BadRequest(errors.First().Description));
    }

    // ... other endpoints
}

public sealed record FulfillOrderRequest(FulfillmentStrategy Strategy);
```

---

## 🗂️ Complete File Structure Summary

```
src/ReSys.Core/Domain/Inventories/
├── Locations/
│   ├── StockLocation.cs                          ✏️ ENHANCE
│   ├── StockLocationConfiguration.cs             ✏️ UPDATE
│   ├── StorePickup.cs                            ✨ NEW
│   ├── StorePickupConfiguration.cs               ✨ NEW
│   ├── StockTransfer.cs                          ✏️ ENHANCE
│   ├── StockTransferConfiguration.cs             ✏️ UPDATE
│   ├── FulfillmentStrategies/
│   │   ├── IFulfillmentStrategy.cs               ✨ NEW
│   │   ├── NearestLocationStrategy.cs            ✨ NEW
│   │   ├── HighestStockStrategy.cs               ✨ NEW
│   │   ├── CostOptimizedStrategy.cs              ✨ NEW
│   │   └── IFulfillmentStrategyFactory.cs        ✨ NEW
│   └── README.md                                 ✏️ UPDATE
├── Stocks/
│   ├── StockItem.cs                              ✏️ ENHANCE
│   └── StockItemConfiguration.cs                 ✏️ UPDATE
├── NumberGenerator.cs
└── README.md                                      ✏️ UPDATE

src/ReSys.Core/Feature/Orders/
├── Commands/
│   ├── FulfillOrder/
│   │   ├── FulfillOrderCommand.cs                ✨ NEW
│   │   ├── FulfillOrderHandler.cs                ✨ NEW
│   │   ├── FulfillOrderValidator.cs              ✨ NEW
│   │   └── FulfillOrderResponse.cs               ✨ NEW
│   ├── CreateStorePickup/
│   │   ├── CreateStorePickupCommand.cs           ✨ NEW
│   │   ├── CreateStorePickupHandler.cs           ✨ NEW
│   │   └── CreateStorePickupValidator.cs         ✨ NEW
│   ├── CompletePickup/
│   │   ├── CompletePickupCommand.cs              ✨ NEW
│   │   ├── CompletePickupHandler.cs              ✨ NEW
│   │   └── CompletePickupValidator.cs            ✨ NEW
│   ├── TransferStock/
│   │   ├── InitiateStockTransferCommand.cs       ✨ NEW
│   │   ├── InitiateStockTransferHandler.cs       ✨ NEW
│   │   └── InitiateStockTransferValidator.cs     ✨ NEW
│   └── ...existing commands
├── Queries/
│   ├── CheckStockAvailability/
│   │   ├── CheckStockAvailabilityQuery.cs        ✨ NEW
│   │   ├── CheckStockAvailabilityHandler.cs      ✨ NEW
│   │   ├── StockAvailabilityResponse.cs          ✨ NEW
│   │   └── PickupLocationDto.cs                  ✨ NEW
│   ├── GetNearbyLocations/
│   │   ├── GetNearbyLocationsQuery.cs            ✨ NEW
│   │   └── GetNearbyLocationsHandler.cs          ✨ NEW
│   └── ...existing queries
└── ...existing structure

src/ReSys.API/
├── Endpoints/
│   ├── OrderFulfillmentEndpoints.cs              ✨ NEW
│   ├── StorePickupEndpoints.cs                   ✨ NEW
│   ├── StockTransferEndpoints.cs                 ✨ NEW
│   └── ...existing endpoints
└── ...existing structure

src/ReSys.Infrastructure/
├── Persistence/
│   ├── Configurations/
│   │   ├── StorePickupConfiguration.cs           ✨ NEW
│   │   ├── StockLocationConfiguration.cs         ✏️ UPDATE
│   │   ├── StockItemConfiguration.cs             ✏️ UPDATE
│   │   └── StockTransferConfiguration.cs         ✏️ UPDATE
│   └── ...existing structure
├── Services/
│   ├── Fulfillment/
│   │   ├── IShippingCalculatorService.cs         ✨ NEW
│   │   ├── ShippingCalculatorService.cs          ✨ NEW
│   │   ├── IFulfillmentStrategyFactory.cs        ✨ NEW
│   │   └── FulfillmentStrategyFactory.cs         ✨ NEW
│   └── ...existing services
└── ...existing structure

tests/Core.UnitTests/Domain/
├── Inventories/
│   ├── StockLocationTests.cs                     ✨ NEW
│   ├── StockItemReservationTests.cs              ✨ NEW
│   ├── StorePickupTests.cs                       ✨ NEW
│   └── StockTransferTests.cs                     ✨ NEW
└── ...existing tests
```

---

## 📊 Data Model Relationships

```
Brand (1) ─── (Many) StockLocation
                         ├── (Many) StockItem
                         │              └── (1) Variant
                         ├── (Many) StorePickup
                         │              └── (1) Order
                         └── (Many) Shipment

Order (1) ─── (Many) LineItem
      │            └── (1) Variant
      ├── (Many) StorePickup
      ├── (Many) Shipment
      └── (Many) ShipmentLineItem

StockTransfer
├── (1) SourceLocation (StockLocation)
├── (1) DestinationLocation (StockLocation)
└── (Many) StockTransferItem
          └── (1) Variant
```

---

## 🔄 Key Business Flows

### Flow 1: Online Order (Ship to Home)

```
1. Customer browses and adds items
2. System reserves stock (StockItem.Reserve)
3. At checkout → FulfillOrderCommand
4. Find best location using strategy (IFulfillmentStrategy)
5. Create Shipment(s) for allocation(s)
6. Confirm shipment (StockItem.ConfirmShipment)
7. Stock deducted → Order ready to ship
```

### Flow 2: Store Pickup

```
1. Customer selects pickup location
2. CheckStockAvailabilityQuery validates inventory
3. System reserves stock at location
4. At checkout → CreateStorePickupCommand
5. StorePickup created with pickup code
6. Staff notified → Prepare items
7. Staff marks pickup as Ready
8. Customer arrives with code → CompletePickupCommand
9. Order marked as picked up
```

### Flow 3: Stock Transfer (Rebalancing)

```
1. Store running low → InitiateStockTransferCommand
2. Select source location (warehouse)
3. Stock deducted from source (Adjust)
4. Transfer state: pending → in_transit
5. Items shipped to destination
6. ReceiveStockTransferCommand at destination
7. Stock added to destination (Adjust)
8. Transfer state: received
```

---

## ✅ Testing Strategy

### Unit Tests (No Database)

- **StockLocation Tests:** Location type validation, geographic queries
- **StockItem Tests:** Reserve/Release/ConfirmShipment logic, edge cases
- **StorePickup Tests:** State transitions, code generation, cancellation
- **FulfillmentStrategy Tests:** Allocation algorithms for each strategy

### Integration Tests (With Database)

- **Order Fulfillment Flow:** End-to-end shipping scenario
- **Pickup Flow:** E2E store pickup with code verification
- **Split Shipment:** Multiple locations for single order
- **Stock Transfer:** Source deduction + destination addition
- **Availability Check:** Geographic filtering and stock calculations

### Example Unit Test

```csharp
[TestFixture]
public class StockItemReservationTests
{
    [Test]
    public void Reserve_WithSufficientStock_Succeeds()
    {
        // Arrange
        var stockItem = StockItem.Create(variantId, locationId, sku: "T-SHIRT-001");
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
    public void Reserve_WithInsufficientStock_Fails()
    {
        // Arrange
        var stockItem = StockItem.Create(variantId, locationId, sku: "T-SHIRT-001");
        stockItem.QuantityOnHand = 30;
        var orderId = Guid.NewGuid();

        // Act
        var result = stockItem.Reserve(orderId, 50);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError.Code, Is.EqualTo("StockItem.InsufficientStock"));
    }

    [Test]
    public void ConfirmShipment_WithValidReservation_DeductsStock()
    {
        // Arrange
        var stockItem = StockItem.Create(variantId, locationId, sku: "T-SHIRT-001");
        stockItem.QuantityOnHand = 100;
        var orderId = Guid.NewGuid();
        stockItem.Reserve(orderId, 50);

        // Act
        var result = stockItem.ConfirmShipment(orderId, 50);

        // Assert
        Assert.That(result.IsSuccess, Is.True);
        Assert.That(stockItem.QuantityOnHand, Is.EqualTo(50));
        Assert.That(stockItem.QuantityReserved, Is.EqualTo(0));
    }
}
```

---

## 📋 Implementation Checklist

### Phase 1: Domain Enhancement
- [ ] Enhance `StockLocation` with location types, capabilities, geographic data
- [ ] Add factory method to `StockLocation.Create()`
- [ ] Add domain events to `StockLocation`
- [ ] Enhance `StockItem` with `Reserve()`, `Release()`, `ConfirmShipment()` methods
- [ ] Add domain events to `StockItem`
- [ ] Update `StockLocationConfiguration` and `StockItemConfiguration`

### Phase 2: Fulfillment Strategy
- [ ] Create `IFulfillmentStrategy` interface
- [ ] Implement `NearestLocationStrategy`
- [ ] Implement `HighestStockStrategy`
- [ ] Implement `CostOptimizedStrategy`
- [ ] Create `IFulfillmentStrategyFactory`

### Phase 3: Store Pickup
- [ ] Create `StorePickup` aggregate
- [ ] Add state transitions: Pending → Ready → PickedUp/Cancelled
- [ ] Implement pickup code generation
- [ ] Create `StorePickupConfiguration`
- [ ] Add domain events

### Phase 4: Stock Transfer
- [ ] Enhance `StockTransfer` with `Initiate()` and `Receive()` methods
- [ ] Add domain events
- [ ] Create `StockTransferItem` owned entity
- [ ] Update configuration

### Phase 5: Order Fulfillment
- [ ] Extend `Order` with fulfillment properties
- [ ] Create `FulfillOrderCommand` and handler
- [ ] Create `CreateStorePickupCommand` and handler
- [ ] Create `CompletePickupCommand` and handler
- [ ] Create `InitiateStockTransferCommand` and handler

### Phase 6: Queries
- [ ] Create `CheckStockAvailabilityQuery` and handler
- [ ] Create `GetNearbyLocationsQuery` and handler
- [ ] Implement distance calculations

### Phase 7: API
- [ ] Create fulfillment endpoints
- [ ] Create pickup endpoints
- [ ] Create stock transfer endpoints
- [ ] Create availability check endpoints

### Phase 8: Testing
- [ ] Unit tests for domain models
- [ ] Integration tests for workflows
- [ ] API endpoint tests
- [ ] Edge case coverage

---

## 🚀 Development Tips

### When Implementing Each Phase:

1. **Start with domain models** (not services or queries)
2. **Write tests as you go** (TDD approach)
3. **Use factory methods** (never `new`)
4. **Return ErrorOr<T>** (never throw in domain)
5. **Publish domain events** (for integration)
6. **Check constraints** (CommonInput.Constraints)
7. **Use validators** (FluentValidation)
8. **Configure EF properly** (configurations)
9. **Create mappers** (Mapster)
10. **Write handlers** (MediatR)

### Key Files to Reference:

- `PaymentMethod.cs` - Excellent example of complete aggregate implementation
- `Order.cs` - Complex aggregate with state transitions
- `src/ReSys.Core/Feature/` - CQRS pattern examples
- `src/ReSys.Infrastructure/Persistence/Configurations/` - EF configurations

---

## 📞 Questions to Answer During Implementation

1. **How do we handle backorder items in fulfillment?** (Some items available, some backordered)
2. **Should fulfillment be fully automatic or require manual approval?**
3. **How do we integrate with shipping carrier APIs?** (tracking numbers, labels)
4. **What happens if stock changes after allocation?** (race conditions)
5. **Should customers be able to split their own orders?** (some items ship, some pickup)
6. **How do we handle returns and stock adjustments?**
7. **Should we cache location/distance data?** (geo queries can be expensive)
8. **How do we handle multi-currency pricing?** (shipping costs)

---

## 🔗 Related Documentation

- See: `docs/INVENTORY_LOCATION_DESIGN_ANALYSIS.md` (existing inventory analysis)
- See: `docs/API_SPECIFICATION.md` (API contract definitions)
- See: `.github/copilot-instructions.md` (architecture guidelines)

---

## 📝 Notes

This plan follows **ReSys.Shop domain-driven design principles**:
- ✅ Aggregate roots manage their own state
- ✅ Owned entities accessed through roots only
- ✅ ErrorOr pattern for error handling
- ✅ Factory methods for safe creation
- ✅ Domain events for integration
- ✅ CQRS pattern for commands/queries
- ✅ Separation of concerns (Domain / Feature / Infrastructure)
- ✅ FluentValidation for rules
- ✅ Mapster for DTO mapping

**Next Step:** Mark TODO items as in-progress and begin Phase 1 implementation.
