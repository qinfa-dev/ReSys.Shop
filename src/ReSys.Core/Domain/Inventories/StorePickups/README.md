# StorePickup Aggregate - Phase 3

## Overview

The `StorePickup` aggregate manages the store pickup fulfillment option, allowing customers to collect orders from retail locations. Each pickup is associated with an order and a specific stock location (retail store).

**File**: `src/ReSys.Core/Domain/Inventories/StorePickups/StorePickup.cs`
**Configuration**: `src/ReSys.Core/Domain/Inventories/StorePickups/StorePickupConfiguration.cs`

---

## State Machine

Store pickups progress through a well-defined state machine:

```
Pending (initial)
  ↓ MarkReady()
Ready (items prepared, ready for customer)
  ↓ MarkPickedUp()
PickedUp (customer collected items, complete)

OR at any point before PickedUp:
  → Cancel() (releases reserved stock)
```

### State Transitions

| Current State | Transition | New State | Effect |
|---------------|-----------|-----------|--------|
| Pending | `MarkReady()` | Ready | ReadyAt timestamp recorded |
| Ready | `MarkPickedUp()` | PickedUp | PickedUpAt timestamp recorded |
| Pending | `Cancel()` | Cancelled | Stock released, CancelledAt recorded |
| Ready | `Cancel()` | Cancelled | Stock released, CancelledAt recorded |
| PickedUp | Cannot cancel | N/A | Error: CannotCancelPickedUp |

---

## Pickup Code Generation

Each pickup generates a unique, human-readable code for customer identification at the store.

**Format**: `{LOCATION-PREFIX}-{CHECKSUM}-{RANDOM-SUFFIX}`

**Example**: `NYC-12345-A7K9`

### Components

1. **Location Prefix** (4-6 characters)
   - Derived from stock location ID
   - Hexadecimal location ID converted to alphabetic
   - Helps staff identify which location the pickup is from

2. **Checksum** (5 digits)
   - Hash of order ID modulo 100000
   - Provides light security validation
   - Customer cannot easily generate fake codes

3. **Random Suffix** (4 characters)
   - Cryptographically random alphanumeric
   - Ensures uniqueness across all pickups
   - Prevents code prediction

### Example Generation

```csharp
// Order ID: 5e8d7b3e-8c2a-4b5f-9d1e-2c8f4b6a9e3d
// Location ID: 3a7f2c8e-9d1a-4b5f-8c2e-1f9a3b5c7d4e
// Location hex: 3a7f
// Location alpha: DLAH
// Order checksum: 5e8d = 24205
// Random suffix: K7M9

// Result: DLAH-24205-K7M9
```

---

## Key Features

### 1. **Scheduled Pickups**
Support for time-window-based pickups:
```csharp
var pickup = StorePickup.Create(
    orderId: order.Id,
    stockLocationId: nyStore.Id,
    scheduledPickupTime: DateTimeOffset.Now.AddHours(2) // 2-hour window
);
```

### 2. **Expiration Policy**
Pickups automatically expire after 14 days:
```csharp
public bool IsExpired => 
    Constraints.PickupExpirationDays.HasValue && 
    CreatedAt.AddDays(Constraints.PickupExpirationDays.Value) < DateTimeOffset.UtcNow;

public bool IsActive => State == PickupState.Ready && !IsExpired;
```

### 3. **Rescheduling**
Customers can reschedule their pickups:
```csharp
var result = pickup.ReschedulePickup(newTime);
// Publishes Rescheduled domain event
```

### 4. **Cancellation with Reason**
Track why pickups were cancelled:
```csharp
var result = pickup.Cancel(reason: "Customer requested cancellation");
// CancellationReason stored for reporting
```

---

## Integration with Previous Phases

### Phase 1: StockLocation Integration
```csharp
// Ensures location supports store pickup
location.PickupEnabled == true  // Required

// Access location info from pickup
var location = pickup.StockLocation;
var operatingHours = location.OperatingHours;
var contactEmail = location.Email;
```

### Phase 2: FulfillmentStrategy Integration
```csharp
// Strategy selects pickup-enabled location
var strategy = new NearestLocationStrategy();
var location = strategy.SelectLocation(
    variant: item,
    requiredQuantity: qty,
    availableLocations: stores.Where(s => s.PickupEnabled)
);

// If location found, create pickup
if (location != null)
{
    var pickupResult = StorePickup.Create(order.Id, location.Id);
    // ...
}
```

---

## Domain Events

StorePickup publishes 5 domain events for decoupled integration:

### 1. **Created** Event
```csharp
public sealed record Created(
    Guid StorePickupId,
    Guid OrderId,
    Guid StockLocationId,
    string PickupCode) : DomainEvent;

// Published when: Pickup is created
// Handled by: Order workflow, notification service
```

### 2. **Ready** Event
```csharp
public sealed record Ready(
    Guid StorePickupId,
    Guid OrderId,
    Guid StockLocationId,
    string PickupCode) : DomainEvent;

// Published when: MarkReady() called
// Handled by: Email/SMS notification service
// Trigger: "Your order is ready for pickup - Code: NYC-12345-A7K9"
```

### 3. **PickedUp** Event
```csharp
public sealed record PickedUp(
    Guid StorePickupId,
    Guid OrderId,
    Guid StockLocationId) : DomainEvent;

// Published when: MarkPickedUp() called
// Handled by: Inventory finalization, sales reporting
// Effect: Reduce inventory, complete order, update analytics
```

### 4. **Cancelled** Event
```csharp
public sealed record Cancelled(
    Guid StorePickupId,
    Guid OrderId,
    Guid StockLocationId,
    string? Reason) : DomainEvent;

// Published when: Cancel() called
// Handled by: Inventory release (stock returned to available)
// Effect: Release reserved stock, notify store
```

### 5. **Rescheduled** Event
```csharp
public sealed record Rescheduled(
    Guid StorePickupId,
    Guid OrderId,
    DateTimeOffset ScheduledPickupTime) : DomainEvent;

// Published when: ReschedulePickup() called
// Handled by: Notification service, store management
```

---

## Common Patterns

### Pattern 1: Create Pickup for Retail Location
```csharp
public sealed class CreateStorePickupCommand : ICommand<StorePickupResponse>
{
    public Guid OrderId { get; init; }
    public Guid StockLocationId { get; init; }
    public DateTimeOffset? ScheduledPickupTime { get; init; }
}

public sealed class CreateStorePickupHandler : ICommandHandler<CreateStorePickupCommand, StorePickupResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public async Task<ErrorOr<StorePickupResponse>> Handle(
        CreateStorePickupCommand command,
        CancellationToken ct)
    {
        // Validate location supports pickup
        var location = await _dbContext.StockLocations.FindAsync(command.StockLocationId, ct);
        if (!location?.PickupEnabled == true)
            return Error.Validation("Location does not support pickup");

        // Create pickup
        var result = StorePickup.Create(
            command.OrderId,
            command.StockLocationId,
            command.ScheduledPickupTime);

        if (result.IsError)
            return result.FirstError;

        var pickup = result.Value;
        _dbContext.StorePickups.Add(pickup);
        await _dbContext.SaveChangesAsync(ct);

        return pickup.Adapt<StorePickupResponse>();
    }
}
```

### Pattern 2: Mark Pickup Ready (Store Workflow)
```csharp
public sealed class MarkPickupReadyCommand : ICommand<StorePickupResponse>
{
    public Guid StorePickupId { get; init; }
}

public sealed class MarkPickupReadyHandler : ICommandHandler<MarkPickupReadyCommand, StorePickupResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public async Task<ErrorOr<StorePickupResponse>> Handle(
        MarkPickupReadyCommand command,
        CancellationToken ct)
    {
        var pickup = await _dbContext.StorePickups.FindAsync(command.StorePickupId, ct);
        if (pickup == null)
            return StorePickup.Errors.NotFound(command.StorePickupId);

        var result = pickup.MarkReady();
        if (result.IsError)
            return result.FirstError;

        await _dbContext.SaveChangesAsync(ct);
        // Event published: Ready → triggers customer notification
        
        return pickup.Adapt<StorePickupResponse>();
    }
}
```

### Pattern 3: Complete Pickup (Customer Pickup)
```csharp
public sealed class CompletePickupCommand : ICommand<StorePickupResponse>
{
    public Guid StorePickupId { get; init; }
    public string PickupCode { get; init; }  // Customer verifies code
}

public sealed class CompletePickupHandler : ICommandHandler<CompletePickupCommand, StorePickupResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public async Task<ErrorOr<StorePickupResponse>> Handle(
        CompletePickupCommand command,
        CancellationToken ct)
    {
        var pickup = await _dbContext.StorePickups.FindAsync(command.StorePickupId, ct);
        if (pickup == null)
            return StorePickup.Errors.NotFound(command.StorePickupId);

        // Verify pickup code
        if (pickup.PickupCode != command.PickupCode)
            return Error.Validation("Invalid pickup code");

        var result = pickup.MarkPickedUp();
        if (result.IsError)
            return result.FirstError;

        await _dbContext.SaveChangesAsync(ct);
        // Event published: PickedUp → triggers inventory finalization
        
        return pickup.Adapt<StorePickupResponse>();
    }
}
```

### Pattern 4: Automatic Expiration Check
```csharp
public sealed class GetPickupQuery : IQuery<StorePickupResponse>
{
    public Guid StorePickupId { get; init; }
}

public sealed class GetPickupHandler : IQueryHandler<GetPickupQuery, StorePickupResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public async Task<ErrorOr<StorePickupResponse>> Handle(
        GetPickupQuery query,
        CancellationToken ct)
    {
        var pickup = await _dbContext.StorePickups.FindAsync(query.StorePickupId, ct);
        if (pickup == null)
            return StorePickup.Errors.NotFound(query.StorePickupId);

        // Check if expired
        if (pickup.IsExpired && pickup.State == StorePickup.PickupState.Ready)
            return StorePickup.Errors.PickupExpired;

        return pickup.Adapt<StorePickupResponse>();
    }
}
```

---

## Constraints and Validation

### Length Constraints
```csharp
public static class Constraints
{
    public const int PickupCodeMaxLength = 50;
    public const int CancellationReasonMaxLength = 500;
    public const int LocationPrefixMaxLength = 10;
    public const int PickupCodeRandomSuffixLength = 4;
    public const int? PickupExpirationDays = 14;  // 14 days until expiration
}
```

### Validation Rules

| Field | Rule | Error |
|-------|------|-------|
| OrderId | Must not be empty | InvalidOrder |
| StockLocationId | Must not be empty | InvalidStockLocation |
| ScheduledPickupTime | Must be in future (if provided) | InvalidScheduledTime |
| State | Pending→Ready, Ready→PickedUp only | InvalidState* |
| PickupCode | Generated automatically, unique | PickupCodeGenerationFailed |
| CancellationReason | Max 500 characters | CancellationReasonTooLong |
| Expiration | 14 days from creation | PickupExpired |

---

## Database Schema

### Table: `store_pickups`
```sql
CREATE TABLE inventories.store_pickups (
    id UUID PRIMARY KEY,
    order_id UUID NOT NULL,
    stock_location_id UUID NOT NULL REFERENCES inventories.stock_locations(id),
    state INT NOT NULL DEFAULT 1,  -- Pending=1, Ready=2, PickedUp=3, Cancelled=4
    pickup_code VARCHAR(50) NOT NULL UNIQUE,
    scheduled_pickup_time TIMESTAMPTZ,
    ready_at TIMESTAMPTZ,
    picked_up_at TIMESTAMPTZ,
    cancelled_at TIMESTAMPTZ,
    cancellation_reason VARCHAR(500),
    created_at TIMESTAMPTZ NOT NULL,
    updated_at TIMESTAMPTZ
);

-- Indexes for common queries
CREATE INDEX ix_store_pickups_order_id ON inventories.store_pickups(order_id);
CREATE INDEX ix_store_pickups_stock_location_id ON inventories.store_pickups(stock_location_id);
CREATE UNIQUE INDEX ix_store_pickups_pickup_code_unique ON inventories.store_pickups(pickup_code);
CREATE INDEX ix_store_pickups_location_ready ON inventories.store_pickups(stock_location_id, state) WHERE state = 2;
CREATE INDEX ix_store_pickups_active ON inventories.store_pickups(state, created_at) WHERE state = 2;
CREATE INDEX ix_store_pickups_state ON inventories.store_pickups(state);
CREATE INDEX ix_store_pickups_location_state_scheduled ON inventories.store_pickups(stock_location_id, state, scheduled_pickup_time);
```

---

## Query Patterns

### Find All Ready Pickups for a Location
```csharp
var readyPickups = await _dbContext.StorePickups
    .Where(p => p.StockLocationId == locationId && p.State == StorePickup.PickupState.Ready)
    .OrderBy(p => p.ScheduledPickupTime)
    .ToListAsync();
```

### Find Pickup by Code
```csharp
var pickup = await _dbContext.StorePickups
    .SingleOrDefaultAsync(p => p.PickupCode == code);
```

### Find Active Pickups (Ready, Not Expired)
```csharp
var now = DateTimeOffset.UtcNow;
var expirationDate = now.AddDays(-StorePickup.Constraints.PickupExpirationDays ?? 14);

var activePickups = await _dbContext.StorePickups
    .Where(p => p.State == StorePickup.PickupState.Ready && p.CreatedAt > expirationDate)
    .ToListAsync();
```

### Find Pickups by Order
```csharp
var pickups = await _dbContext.StorePickups
    .Where(p => p.OrderId == orderId)
    .ToListAsync();
```

---

## Next Steps (Phase 4+)

### Phase 4: StockTransfer Enhancement
- Enhance StockTransfer with Initiate and Receive methods
- Add state transitions for transfer workflow
- Add domain events for transfer lifecycle

### Phase 5: Order Fulfillment Orchestration
- Create FulfillOrderCommand that uses Phase 2 strategies
- Integrate StorePickup creation for pickup-enabled locations
- Handle multi-location fulfillment decisions
- Release inventory when pickup is completed

### Phase 6: Stock Availability Queries
- Query nearby locations for pickup options
- Validate stock availability before creating pickup
- Check for in-stock vs backorder status

### Phase 7: API Endpoints
- POST /orders/{orderId}/store-pickups - Create pickup
- GET /store-pickups/{code} - Lookup by code
- PATCH /store-pickups/{id}/ready - Mark ready
- PATCH /store-pickups/{id}/pickup - Complete pickup
- GET /locations/{locationId}/ready-pickups - Store dashboard

---

## Testing Recommendations

### Unit Tests

1. **State Machine Validation**
   - Valid transitions succeed
   - Invalid transitions return errors
   - State fields updated on transition

2. **Pickup Code Generation**
   - Unique codes generated
   - Format complies with constraints
   - Deterministic for same inputs (for testing)

3. **Expiration Logic**
   - IsExpired calculates correctly
   - IsActive reflects correct state
   - Time-based logic works as expected

### Integration Tests

1. **Create and Transition**
   - Create pickup → Ready → PickedUp
   - Verify all timestamps recorded
   - Verify events published

2. **Cancellation Flow**
   - Cancel from Pending → Cancelled
   - Cancel from Ready → Cancelled
   - Cannot cancel from PickedUp

3. **Rescheduling**
   - Change scheduled time
   - Verify new time validated
   - Verify event published

### Example Test
```csharp
[TestFixture]
public class StorePickupTests
{
    [Test]
    public void Create_WithValidInput_SucceedsAndGeneratesCode()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var locationId = Guid.NewGuid();

        // Act
        var result = StorePickup.Create(orderId, locationId);

        // Assert
        Assert.That(result.IsError, Is.False);
        var pickup = result.Value;
        Assert.That(pickup.OrderId, Is.EqualTo(orderId));
        Assert.That(pickup.State, Is.EqualTo(StorePickup.PickupState.Pending));
        Assert.That(pickup.PickupCode, Is.Not.Empty);
        Assert.That(pickup.PickupCode.Length, Is.LessThanOrEqualTo(50));
    }

    [Test]
    public void MarkReady_FromPending_TransitionsToReadyAndRecordsTime()
    {
        // Arrange
        var pickup = StorePickup.Create(Guid.NewGuid(), Guid.NewGuid()).Value;

        // Act
        var result = pickup.MarkReady();

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(pickup.State, Is.EqualTo(StorePickup.PickupState.Ready));
        Assert.That(pickup.ReadyAt, Is.Not.Null);
        Assert.That(pickup.HasUncommittedEvents(), Is.True);
    }
}
```

---

## See Also

- **Phase 1**: `src/ReSys.Core/Domain/Inventories/Locations/StockLocation.cs` - Location capabilities
- **Phase 2**: `src/ReSys.Core/Domain/Inventories/FulfillmentStrategies/` - Location selection strategies
- **Orders**: `src/ReSys.Core/Domain/Orders/Order.cs` - Order lifecycle
- **Shipments**: `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs` - Shipping fulfillment
