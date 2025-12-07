# Spree-Aligned Refactoring: Remove Fulfillment Domain

## Decision: ❌ REMOVE Fulfillment, Align with Spree Model

**Rationale**: Spree's battle-tested approach keeps everything in `Shipment` with `InventoryUnit` state tracking. ReSys.Shop's separate `FulfillmentOrder` domain adds complexity without clear benefits.

---

## Current ReSys.Shop Architecture (❌ Wrong)

```
Order → Shipment (basic tracking)
    ↓
    → FulfillmentOrder (separate domain, duplicate state machine)
        ↓
        → InventoryUnit (state tracking)
```

**Problems:**
- Two state machines (Shipment + FulfillmentOrder) = confusion
- Event handlers bounce between domains
- Warehouse operations scattered
- Not proven at scale like Spree

---

## Target: Spree-Aligned Architecture (✅ Correct)

```
Order
  ├─ LineItem
  │   └─ InventoryUnit (state: on_hand → backordered → shipped)
  │
  └─ Shipment (aggregate for warehouse + customer tracking)
      ├─ State: pending → ready → shipped → canceled
      ├─ TrackingNumber
      ├─ StockLocation
      ├─ Inventory Movements (reserve, confirm, ship)
      └─ InventoryUnits (items in this shipment)
```

**Benefits:**
- ✅ Single state machine (Shipment)
- ✅ No cross-domain coordination
- ✅ Proven at scale (Spree uses this)
- ✅ Simpler codebase
- ✅ Multi-warehouse native

---

## Phase 1: Expand Shipment Model (Instead of FulfillmentOrder)

### Current Shipment (Too Simple)

```csharp
public class Shipment : Entity
{
    public Guid OrderId { get; set; }
    public Guid StockLocationId { get; set; }
    public string? TrackingNumber { get; set; }
    public ShipmentState State { get; set; } // pending | shipped | delivered | canceled
}

public enum ShipmentState
{
    Pending,
    Shipped,
    Delivered,
    Canceled
}
```

### Target Shipment (Spree-Aligned)

```csharp
public sealed class Shipment : Aggregate
{
    // Basic Properties
    public Guid OrderId { get; set; }
    public Guid StockLocationId { get; set; }
    public string? TrackingNumber { get; set; }
    
    // Warehouse Workflow (added from FulfillmentOrder)
    public ShipmentState State { get; set; }
    
    // Timestamps for warehouse operations
    public DateTimeOffset? AllocatedAt { get; set; }     // ← NEW: When stock was reserved
    public DateTimeOffset? PickingStartedAt { get; set; } // ← NEW: When warehouse staff started picking
    public DateTimeOffset? PickedAt { get; set; }        // ← NEW: When all items picked
    public DateTimeOffset? PackedAt { get; set; }        // ← NEW: When packed in box
    public DateTimeOffset? ReadyToShipAt { get; set; }   // ← NEW: When ready for pickup
    public DateTimeOffset? ShippedAt { get; set; }       // ← NEW: When handed to carrier
    public DateTimeOffset? DeliveredAt { get; set; }     // ← NEW: When customer received
    
    public string? PackageId { get; set; }               // ← NEW: Physical package ID
    
    // Inventory Units (items in this shipment)
    public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
    
    // Stock Movements (audit trail)
    public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
    
    // Navigation Properties
    public Order Order { get; set; } = null!;
    public StockLocation StockLocation { get; set; } = null!;
}

public enum ShipmentState
{
    Pending,      // Waiting to process (backorder, unpaid, or just created)
    Ready,        // Can be shipped (stock available, order paid) - ← Replaces "Allocated"
    Picked,       // Items picked from shelves
    Packed,       // Items in box
    ReadyToShip,  // On dock, ready for carrier
    Shipped,      // With carrier
    Delivered,    // Customer received
    Canceled      // Order or shipment canceled
}
```

**Key Changes:**
- Warehouse states now part of Shipment
- Add timestamps for each warehouse stage
- InventoryUnits track individual item state
- StockMovements track inventory adjustments

---

## Phase 2: Enhance InventoryUnit (Spree Model)

### Current InventoryUnit (Too Simple)

```csharp
public class InventoryUnit : Entity
{
    public Guid VariantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid? ShipmentId { get; set; }
    public string State { get; set; } // "on_hand", "backordered", "shipped"
}
```

### Target InventoryUnit (Spree-Aligned)

```csharp
public sealed class InventoryUnit : Entity
{
    public Guid VariantId { get; set; }
    public Guid OrderId { get; set; }
    public Guid ShipmentId { get; set; }          // ← NOW REQUIRED (always in a shipment)
    
    public InventoryUnitState State { get; set; }
    public DateTimeOffset StateChangedAt { get; set; }
    
    // Link back to originating line item
    public Guid LineItemId { get; set; }
    public LineItem LineItem { get; set; } = null!;
    
    // Navigation
    public Variant Variant { get; set; } = null!;
    public Order Order { get; set; } = null!;
    public Shipment Shipment { get; set; } = null!;
    
    // Business Logic
    public ErrorOr<InventoryUnit> TransitionToShipped()
    {
        if (State == InventoryUnitState.Shipped) return this; // Idempotent
        if (State != InventoryUnitState.OnHand && State != InventoryUnitState.Backordered)
            return Error.Validation("Cannot ship unit not on hand or backordered");
        
        State = InventoryUnitState.Shipped;
        StateChangedAt = DateTimeOffset.UtcNow;
        return this;
    }
}

public enum InventoryUnitState
{
    OnHand,      // In warehouse, ready to ship
    Backordered, // Expected later
    Shipped,     // Handed to carrier
    Returned,    // Customer returned it
}
```

---

## Phase 3: Create Stock Movement Tracking

### New: StockMovement (From Spree)

Replaces fulfillment events with simple inventory tracking:

```csharp
public sealed class StockMovement : Entity
{
    public Guid StockItemId { get; set; }
    public int Quantity { get; set; }              // Positive = add, Negative = remove
    
    public string MovementType { get; set; }       // "allocated", "shipped", "returned", etc.
    public string? OriginatorType { get; set; }    // "Shipment", "Return", "Adjustment"
    public Guid? OriginatorId { get; set; }
    
    public DateTimeOffset CreatedAt { get; set; }
    
    // Navigation
    public StockItem StockItem { get; set; } = null!;
}

/*
Usage:
- When Shipment.AllocateInventory(): Create StockMovement(quantity: -5, type: "allocated", originator: Shipment)
- When Shipment.Ship(): Create StockMovement(quantity: -5, type: "shipped", originator: Shipment)
- When Customer Returns: Create StockMovement(quantity: +5, type: "returned", originator: Return)
- Audit trail: Query all movements for a StockItem to see full history
*/
```

---

## Phase 4: Warehouse Workflow Commands

Move all warehouse operations to Shipment Commands (not separate domain):

### Commands (No Separate Fulfillment Domain)

```
src/ReSys.Core/Feature/Orders/Shipments/
├── Commands/
│   ├── AllocateShipmentInventoryCommand.cs      ← WAS in Fulfillment
│   ├── StartPickingCommand.cs                  ← WAS in Fulfillment
│   ├── CompletePickingCommand.cs               ← WAS in Fulfillment
│   ├── PackShipmentCommand.cs                  ← WAS in Fulfillment
│   ├── MarkReadyToShipCommand.cs               ← WAS in Fulfillment
│   ├── ShipCommand.cs                          ← WAS in Fulfillment (renamed from Ship)
│   └── ConfirmDeliveryCommand.cs               ← NEW: For delivery confirmation
│
└── Queries/
    ├── GetWarehouseWorkQueueQuery.cs           ← WAS in Fulfillment
    ├── GetShipmentStatusQuery.cs
    └── GetInventoryMovementsQuery.cs
```

### Example: AllocateShipmentInventoryCommand

```csharp
// File: src/ReSys.Core/Feature/Orders/Shipments/Commands/AllocateShipmentInventoryCommand.cs

public sealed record AllocateShipmentInventoryCommand(Guid ShipmentId) : ICommand<ShipmentResponse>;

public sealed class AllocateShipmentInventoryValidator : AbstractValidator<AllocateShipmentInventoryCommand>
{
    public AllocateShipmentInventoryValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}

public sealed class AllocateShipmentInventoryHandler 
    : ICommandHandler<AllocateShipmentInventoryCommand, ShipmentResponse>
{
    private readonly IApplicationDbContext _dbContext;
    
    public async Task<ErrorOr<ShipmentResponse>> Handle(
        AllocateShipmentInventoryCommand command, 
        CancellationToken ct)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Order.LineItems)
            .Include(s => s.StockLocation.StockItems)
            .FirstOrDefaultAsync(s => s.Id == command.ShipmentId, cancellationToken: ct);
        
        if (shipment == null)
            return Errors.ShipmentNotFound(command.ShipmentId);
        
        // Allocate inventory for each inventory unit in this shipment
        foreach (var lineItem in shipment.Order.LineItems)
        {
            var stockItem = shipment.StockLocation.StockItems
                .FirstOrDefault(si => si.VariantId == lineItem.VariantId);
            
            if (stockItem == null)
                return Error.NotFound("StockItem not found for variant");
            
            // Reserve stock
            var reserveResult = stockItem.ReserveQuantity(lineItem.Quantity);
            if (reserveResult.IsError)
                return reserveResult.FirstError;
            
            // Create stock movement record
            var movement = new StockMovement
            {
                StockItemId = stockItem.Id,
                Quantity = -lineItem.Quantity,
                MovementType = "allocated",
                OriginatorType = nameof(Shipment),
                OriginatorId = shipment.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            
            _dbContext.StockMovements.Add(movement);
        }
        
        // Transition shipment state
        shipment.State = ShipmentState.Ready;
        shipment.AllocatedAt = DateTimeOffset.UtcNow;
        shipment.UpdatedAt = DateTimeOffset.UtcNow;
        
        _dbContext.Shipments.Update(shipment);
        await _dbContext.SaveChangesAsync(ct);
        
        return shipment.Adapt<ShipmentResponse>();
    }
}
```

---

## Phase 5: Delete Fulfillment Domain Entirely

### What Gets Deleted

```
DELETE: src/ReSys.Core/Domain/Fulfillment/
  ├─ FulfillmentOrder.cs           ← Move logic to Shipment
  ├─ FulfillmentLineItem.cs        ← Use InventoryUnit instead
  ├─ FulfillmentOrderConfiguration.cs
  ├─ FulfillmentOrderEvents.cs     ← Use Shipment.Events instead
  └─ README.md

DELETE: src/ReSys.Core/Feature/Fulfillment/
  ├─ Commands/                     ← Move to Shipments/Commands
  ├─ Queries/                      ← Move to Shipments/Queries
  ├─ EventHandlers/                ← Move to Shipments/EventHandlers
  └─ Validators/                   ← Move to Shipments/Validators
```

### What Gets Added/Modified

```
ADD: src/ReSys.Core/Domain/Orders/
  └─ StockMovement.cs              ← New entity for audit trail

MODIFY: src/ReSys.Core/Domain/Orders/Shipments/
  ├─ Shipment.cs                   ← Add warehouse workflow + events
  ├─ InventoryUnit.cs              ← Add state machine + validation
  └─ Shipment.Events.cs            ← Add warehouse events

MOVE: src/ReSys.Core/Feature/Orders/Shipments/
  ├─ Commands/                     ← From Fulfillment/Commands
  ├─ Queries/                      ← From Fulfillment/Queries
  └─ EventHandlers/                ← From Fulfillment/EventHandlers
```

---

## Phase 6: Update Shipment Events

### New Shipment Events (Replaces FulfillmentOrder Events)

```csharp
public sealed partial class Shipment : Aggregate
{
    public static class Events
    {
        // CREATED
        public sealed record Created(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        
        // WAREHOUSE WORKFLOW
        public sealed record InventoryAllocated(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
        public sealed record PickingStarted(Guid ShipmentId, Guid OrderId) : DomainEvent;
        public sealed record PickingCompleted(Guid ShipmentId, Guid OrderId) : DomainEvent;
        public sealed record PackingCompleted(Guid ShipmentId, Guid OrderId, string PackageId) : DomainEvent;
        public sealed record ReadyToShip(Guid ShipmentId, Guid OrderId) : DomainEvent;
        
        // SHIPPING & DELIVERY
        public sealed record Shipped(Guid ShipmentId, Guid OrderId, string TrackingNumber) : DomainEvent;
        public sealed record Delivered(Guid ShipmentId, Guid OrderId) : DomainEvent;
        
        // CANCELLATION
        public sealed record Canceled(Guid ShipmentId, Guid OrderId) : DomainEvent;
    }
}
```

---

## Phase 7: Warehouse Dashboard Still Works

### Same Query, Different Source

```csharp
// BEFORE: Queried FulfillmentOrder
var workQueue = await _dbContext.FulfillmentOrders
    .Where(fo => fo.StockLocationId == warehouseId)
    .Where(fo => fo.State == FulfillmentState.Pending)
    .ToListAsync();

// AFTER: Query Shipments (simpler!)
var workQueue = await _dbContext.Shipments
    .Where(s => s.StockLocationId == warehouseId)
    .Where(s => s.State == ShipmentState.Pending)
    .Include(s => s.Order.LineItems)
    .Include(s => s.InventoryUnits)
    .ToListAsync();
```

**No warehouse API changes!** Same endpoint, better implementation.

---

## Phase 8: Multi-Warehouse Support (Still Works!)

### Before (With Separate FulfillmentOrder)
```
Order
├─ Shipment#1 (LA warehouse) → FulfillmentOrder#1
└─ Shipment#2 (NY warehouse) → FulfillmentOrder#2
```

### After (Shipment-Only)
```
Order
├─ Shipment#1 (LA warehouse) - Self-contained state machine
└─ Shipment#2 (NY warehouse) - Self-contained state machine
```

**Even simpler!** No coordination layer needed.

---

## 4-Week Refactoring Plan

### Week 1: Expand Shipment Model

**Tasks:**
- [ ] Add warehouse workflow fields to Shipment (AllocatedAt, PickedAt, etc.)
- [ ] Add PackageId, expand State enum
- [ ] Create StockMovement entity
- [ ] Update Shipment EF configuration
- [ ] Create database migration

**Deliverable:** Shipment can store warehouse state

**Time:** 12 hours

---

### Week 2: Move Commands from Fulfillment → Shipments

**Tasks:**
- [ ] Rename/Move AllocateInventoryCommand → AllocateShipmentInventoryCommand
- [ ] Rename/Move StartPickingCommand → StartShipmentPickingCommand (etc.)
- [ ] Update handlers to use Shipment instead of FulfillmentOrder
- [ ] Add StockMovement creation to each handler
- [ ] Unit test all commands

**Deliverable:** All warehouse commands work on Shipment

**Time:** 20 hours

---

### Week 3: Add Shipment State Machine Methods

**Tasks:**
- [ ] Add methods to Shipment:
  - `AllocateInventory()`
  - `StartPicking()`
  - `CompletePicking()`
  - `Pack(packageId)`
  - `MarkReadyToShip()`
  - `Ship(trackingNumber)`
  - `Deliver()`
  - `Cancel()`
- [ ] Add state validation to each method
- [ ] Add domain events to each method
- [ ] Unit tests (state transitions, validations)
- [ ] Delete FulfillmentOrder aggregate

**Deliverable:** Shipment is complete aggregate

**Time:** 20 hours

---

### Week 4: Integration & Cleanup

**Tasks:**
- [ ] Integration tests: Order → Shipment → Ship → Deliver
- [ ] Multi-warehouse tests (2+ Shipments per Order)
- [ ] Update warehouse dashboard queries
- [ ] Delete Fulfillment domain folder entirely
- [ ] Update documentation
- [ ] Team training on new model

**Deliverable:** Fulfillment domain gone, Spree-aligned architecture

**Time:** 15 hours

---

## Database Migration

### New Tables/Changes

```sql
-- MODIFY: Shipments table (add warehouse workflow columns)
ALTER TABLE Shipments ADD COLUMN AllocatedAt TIMESTAMP NULL;
ALTER TABLE Shipments ADD COLUMN PickingStartedAt TIMESTAMP NULL;
ALTER TABLE Shipments ADD COLUMN PickedAt TIMESTAMP NULL;
ALTER TABLE Shipments ADD COLUMN PackedAt TIMESTAMP NULL;
ALTER TABLE Shipments ADD COLUMN ReadyToShipAt TIMESTAMP NULL;
ALTER TABLE Shipments ADD COLUMN ShippedAt TIMESTAMP NULL;
ALTER TABLE Shipments ADD COLUMN DeliveredAt TIMESTAMP NULL;
ALTER TABLE Shipments ADD COLUMN PackageId VARCHAR(255) NULL;

-- MODIFY: Shipments.State enum (add new states)
-- pending, ready, picked, packed, readyToShip, shipped, delivered, canceled

-- CREATE: StockMovements table
CREATE TABLE StockMovements (
    Id UUID PRIMARY KEY,
    StockItemId UUID NOT NULL,
    Quantity INT NOT NULL,
    MovementType VARCHAR(50) NOT NULL,
    OriginatorType VARCHAR(100) NULL,
    OriginatorId UUID NULL,
    CreatedAt TIMESTAMP NOT NULL,
    FOREIGN KEY (StockItemId) REFERENCES StockItems(Id)
);

-- DELETE: FulfillmentOrders table
DROP TABLE FulfillmentLineItems;
DROP TABLE FulfillmentOrders;
```

---

## Benefits of Spree-Aligned Model

| Aspect | With Fulfillment | Spree-Aligned (NEW) |
|--------|------------------|-------------------|
| **Aggregates** | 2 (Order, Shipment, FulfillmentOrder) | 1 (Order, Shipment) |
| **State Machines** | 2 (Shipment, FulfillmentOrder) | 1 (Shipment) |
| **Event Coordination** | Complex (cross-domain) | Simple (within domain) |
| **Code Lines** | 1500+ | 800 |
| **Warehouse Operations** | Separate domain | Part of Orders domain |
| **Learning Curve** | Steep | Proven (matches Spree) |
| **Multi-warehouse** | ✅ Works | ✅ Still works (simpler) |
| **Maintenance Burden** | High | Low |
| **Battle-Tested** | Not at scale | Yes (Spree uses it) |

---

## No Breaking Changes

### Customer-Facing APIs

```csharp
// BEFORE
GET /api/orders/{id}/shipments
→ Returns Shipment with TrackingNumber

// AFTER
GET /api/orders/{id}/shipments
→ Same response! No changes!
```

### Warehouse APIs

```csharp
// BEFORE
GET /api/warehouse/{id}/work-queue
→ Returned FulfillmentOrder list

// AFTER
GET /api/warehouse/{id}/work-queue
→ Returns Shipment list (same fields!)
→ Implementation simpler
```

**Result:** Drop-in replacement. Zero API changes.

---

## Summary

### Before: Complicated

```
Orders Domain → Fulfillment Domain → Coordination via Events
                                   ↓
                              Complex event handlers
                              Duplicate state machines
                              Unclear responsibilities
```

### After: Simple (Spree Way)

```
Orders Domain
  ├─ Shipment (everything warehouse + customer tracking)
  ├─ InventoryUnit (item state tracking)
  └─ StockMovement (audit trail)
  
Clear, proven, maintainable.
```

---

## Files to Delete

```
❌ src/ReSys.Core/Domain/Fulfillment/
   ├─ FulfillmentOrder.cs
   ├─ FulfillmentLineItem.cs
   ├─ FulfillmentOrderConfiguration.cs
   ├─ FulfillmentOrderEvents.cs
   ├─ README.md
   └─ (entire folder)

❌ src/ReSys.Core/Feature/Fulfillment/
   ├─ Commands/
   ├─ Queries/
   ├─ EventHandlers/
   ├─ Validators/
   └─ (entire folder)

❌ docs/
   ├─ FULFILLMENT_ARCHITECTURE_ANALYSIS.md
   ├─ FULFILLMENT_IMPLEMENTATION_GUIDE.md
   ├─ FULFILLMENT_REFACTORING_CHECKLIST.md
   └─ (all fulfillment docs)
```

---

## Files to Create

```
✅ src/ReSys.Core/Domain/Orders/
   └─ StockMovement.cs (new entity)

✅ Move from Fulfillment → Orders/Shipments:
   ├─ Feature/Orders/Shipments/Commands/
   │   ├─ AllocateShipmentInventoryCommand.cs
   │   ├─ ShipCommand.cs (renamed from Ship)
   │   └─ ...
   └─ Feature/Orders/Shipments/Queries/
       ├─ GetWarehouseWorkQueueQuery.cs
       └─ ...
```

---

## Implementation Starting Point

**When you're ready, start with:**

1. Expand Shipment model (add warehouse fields)
2. Create StockMovement entity
3. Move commands one-by-one
4. Delete FulfillmentOrder
5. Test everything

**Result:** Clean, maintainable, Spree-aligned architecture ✅

This is the way forward.
