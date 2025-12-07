# Spree Alignment Refactoring Plan

**Objective**: Simplify ReSys.Shop Order model and remove separate Fulfillment domain to align with Spree's proven architecture.

**Key Changes**:
1. Remove `FulfillmentOrder` aggregate (separate domain eliminated)
2. Expand `Shipment` to include warehouse workflow states
3. Simplify payment states (remove complex state machine)
4. Remove tax calculation complexity
5. Simplify shipping rates (flat-rate primary pattern)
6. Consolidate inventory coordination into Shipment

---

## Current State vs Target State

### Current ReSys Architecture
```
Order (Customer perspective)
  ├─ Shipment (Minimal - just tracking)
  └─ FulfillmentOrder (Warehouse workflow) ❌ TO REMOVE
```

### Target Spree-Aligned Architecture
```
Order (Unified view)
  ├─ Shipment (Complete - includes warehouse workflow + tracking)
  └─ LineItem + InventoryUnit (Simpler inventory tracking)
```

---

## Implementation Phases

### Phase 1: Shipment Model Expansion (Week 1)

**Goal**: Expand `Shipment` to include warehouse workflow states

#### Current Shipment States
```csharp
public enum ShipmentState { Pending, Shipped, Delivered, Canceled }
```

#### Target Spree States
```csharp
public enum ShipmentState 
{ 
    Pending,      // Initial state
    Ready,        // Allocated, ready to ship
    Shipped,      // Handed to carrier
    Delivered,    // Received by customer
    Canceled,     // Order/shipment canceled
    Partial       // Partially shipped (if split shipments)
}
```

#### Add Warehouse Workflow Methods to Shipment
```csharp
public ErrorOr<Shipment> AllocateInventory()
public ErrorOr<Shipment> MarkReady()
public ErrorOr<Shipment> Ship(string trackingNumber)
public ErrorOr<Shipment> Deliver()
public ErrorOr<Shipment> Cancel()
```

**Files to Modify**:
- `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs` - Add states and methods
- `src/ReSys.Core/Domain/Orders/Shipments/ShipmentConfiguration.cs` - Update EF mapping

---

### Phase 2: Order Model Simplification (Week 1)

**Goal**: Simplify Order to delegate fulfillment to Shipment

#### Remove
- `FulfillmentLocationId` (move to Shipment)
- Complex state validation in `Complete()`
- `OrderFulfillmentDomainService` dependency
- Tax-related totals (if not needed)

#### Keep
- Basic state machine: Cart → Address → Delivery → Payment → Confirm → Complete
- Line items, payments, adjustments
- Shipment collection

#### Update `Complete()` Method
```csharp
private ErrorOr<Order> Complete()
{
    // Simpler validation - just check payments and shipments are ready
    if (!Payments.Any(p => p.IsCompleted)) 
        return Error.Validation(...);
    
    if (!IsFullyDigital && !Shipments.Any(s => s.State == ShipmentState.Ready || s.State == ShipmentState.Shipped))
        return Error.Validation(...);
    
    CompletedAt = DateTimeOffset.UtcNow;
    State = OrderState.Complete;
    // Publish events
    return this;
}
```

**Files to Modify**:
- `src/ReSys.Core/Domain/Orders/Order.cs` - Remove fulfillment logic, simplify Complete()
- Remove dependency on `OrderFulfillmentDomainService`

---

### Phase 3: Remove Fulfillment Domain (Week 2)

**Goal**: Delete separate Fulfillment bounded context

#### Delete
```
src/ReSys.Core/Domain/Fulfillment/
  ├─ FulfillmentOrder.cs ❌
  ├─ FulfillmentLineItem.cs ❌
  ├─ FulfillmentOrderConfiguration.cs ❌
  ├─ FulfillmentLineItemConfiguration.cs ❌
  └─ README.md ❌
```

#### Delete EF Migrations
- Remove FulfillmentOrder, FulfillmentLineItem table creations

#### Update DbContext
```csharp
// Remove from IApplicationDbContext
public DbSet<FulfillmentOrder> FulfillmentOrders { get; set; } // ❌ REMOVE
```

---

### Phase 4: Simplify Inventory Tracking (Week 2)

**Goal**: Use `InventoryUnit` from Spree pattern instead of complex allocation

#### Pattern: InventoryUnit States
```csharp
public enum InventoryUnitState
{
    OnHand,       // In stock
    Backordered,  // Waiting for restock
    Shipped,      // Left warehouse
    Received      // Delivered to customer
}
```

#### Inventory Unit Workflow
```
LineItem created
  ↓
InventoryUnit created (State: OnHand or Backordered)
  ↓
Shipment.AllocateInventory() 
  ↓
Shipment.Ship(trackingNumber)
  → InventoryUnit transitioned to Shipped
  ↓
Shipment.Deliver()
  → InventoryUnit transitioned to Received
```

**Files to Modify/Create**:
- Create: `src/ReSys.Core/Domain/Orders/InventoryUnits/InventoryUnit.cs`
- Create: `src/ReSys.Core/Domain/Orders/InventoryUnits/InventoryUnitConfiguration.cs`
- Update: `src/ReSys.Core/Domain/Orders/LineItems/LineItem.cs` - Reference InventoryUnits

---

### Phase 5: Payment State Simplification (Week 2)

**Goal**: Align with Spree payment states: balance_due, paid, void

#### Current Complex States
```csharp
enum PaymentState { Pending, Completed, Failed, Refunded, ... }
```

#### Target Spree States
```csharp
enum PaymentState
{
    BalanceDue,    // Payment needed
    Paid,          // Payment completed
    Failed,        // Payment failed
    Void,          // Payment voided
    Refunded       // Payment refunded
}
```

**Simplification**:
- Remove intermediate states
- Focus on: "Do we have full payment? Yes/No"
- Handle refunds via `Refund` entity, not payment state

**Files to Modify**:
- `src/ReSys.Core/Domain/Orders/Payments/Payment.cs` - Align states
- `src/ReSys.Core/Domain/Orders/Payments/PaymentConfiguration.cs`

---

### Phase 6: Shipping Rate Simplification (Week 3)

**Goal**: Use flat-rate pattern instead of complex calculators

#### Current Complex Pattern
```csharp
ShippingMethod.CalculateCost(weight, orderTotal)
  → Complex calculator logic
  → Tax considerations
  → Per-item rates
  → Flexible rates
```

#### Target Spree Flat-Rate Pattern
```csharp
// Simple ShippingRate
public class ShippingRate
{
    public Guid ShippingMethodId { get; set; }
    public decimal Amount { get; set; } // Fixed or calculated simply
    public string DisplayName { get; set; } // "Standard Shipping - $5.00"
}
```

#### Shipping Calculation Simplified
```csharp
// In Shipment
public ErrorOr<decimal> CalculateShippingCost(ShippingMethod method)
{
    // Simple: Check if method applies to zone
    // Return: Method.Rate (flat rate)
    // If special: Check weight/value, apply simple multiplier
    return method.Rate;
}
```

**Files to Modify**:
- `src/ReSys.Core/Domain/ShippingMethods/ShippingMethod.cs` - Simplify to flat rate
- `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs` - Add shipping rate calculation
- Remove complex Calculator pattern

---

### Phase 7: Event Handler Consolidation (Week 3)

**Goal**: Update event handlers to work with new Shipment-centric model

#### Current Handlers to Remove
```
ShipmentCreatedEventHandler (creates FulfillmentOrder) ❌
FulfillmentOrderShippedEventHandler (updates Shipment) ❌
```

#### New Handlers to Keep/Create
```
Shipment.ShippedEvent 
  → Update tracking on Shipment (already done)
  → Publish notification to customer

Shipment.DeliveredEvent
  → Mark order as delivery-complete
  → Publish notification

Order.CompletedEvent
  → Trigger inventory finalization
  → Publish notifications
```

**Files to Modify/Create**:
- Update/consolidate event handlers in `src/ReSys.Core/Feature/Orders/EventHandlers/`
- Delete handlers in `src/ReSys.Core/Feature/Fulfillment/EventHandlers/`

---

## Spree Alignment: Key Concepts

### 1. Order States (Simplified)
```
cart → address → delivery → payment → confirm → complete
                                                   ↓
                                              (terminal)
```

**State Validation**:
- cart: Add/remove items only
- address: Have addresses (physical orders)
- delivery: Have shipping method, create shipments
- payment: Have payment method
- confirm: (optional step)
- complete: Finalized, events published

### 2. Shipment States (Expanded)
```
pending (created, inventory not yet allocated)
  ↓
ready (inventory allocated, can be shipped)
  ↓
shipped (handed to carrier, has tracking)
  ↓
delivered (customer received) [TERMINAL]
```

### 3. Line Item → Inventory Unit Mapping
```
LineItem (what customer ordered)
  ├─ quantity: 2
  └─ variant_id: shirt-123
  
InventoryUnits (individual items)
  ├─ unit 1: variant_id=shirt-123, state=on_hand
  └─ unit 2: variant_id=shirt-123, state=on_hand
```

When shipment ships, all units transition to shipped.

### 4. Payment is Binary
```
Payment States:
- balance_due: Still owe money
- paid: Full amount received
- failed: Payment rejected
- void: Payment voided
- refunded: Money returned
```

**No intermediate states** - just "Do we have enough money? Yes/No"

### 5. No Tax Complexity (If Not Needed)
```
Option 1: Remove tax from core Order (if not business requirement)
  Total = ItemTotal + ShipmentTotal + Adjustments

Option 2: Keep tax but simplified
  Total = ItemTotal + ShipmentTotal + TaxTotal + Adjustments
  (TaxTotal = single number, not broken into included/additional)
```

---

## Migration Steps

### Step 1: Backup & Branch
```bash
git checkout -b spree-alignment
```

### Step 2: Expand Shipment (Keep FulfillmentOrder for now)
- Add warehouse methods to Shipment
- Add InventoryUnit domain
- Test Shipment workflows

### Step 3: Update Order.Complete()
- Remove FulfillmentLocationId validation
- Simplify to check Shipments.Ready
- Test order completion flow

### Step 4: Create Migration
- Add Shipment state columns (if needed)
- Migrate FulfillmentOrder data to Shipment (backfill)
- Create InventoryUnits from LineItems

### Step 5: Delete Fulfillment Domain
- Remove FulfillmentOrder files
- Remove Fulfillment feature handlers
- Update DbContext

### Step 6: Update API Endpoints
- Remove `/api/warehouse/fulfillment/*`
- Update shipment endpoints to expose warehouse operations

### Step 7: Comprehensive Testing
- Order workflow end-to-end
- Shipment state transitions
- Inventory tracking
- Payment completion
- Event publishing

---

## Benefits of This Alignment

✅ **Simpler Codebase**: 1 aggregate (Shipment) instead of 2 (Shipment + FulfillmentOrder)
✅ **Proven Pattern**: Spree's approach battle-tested in production
✅ **Easier Onboarding**: Junior devs understand single responsibility
✅ **Less Maintenance**: Fewer domains = fewer event coordinations
✅ **Faster Development**: Standard patterns, not custom architecture
✅ **Community Standard**: If you later adopt Spree gems, they'll work naturally

---

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| Breaking existing orders | Create migration to backfill ShipmentOrder.InventoryUnits |
| API consumers shocked | Keep old endpoints, add deprecation warnings |
| Losing warehouse tracking | Shipment now has all warehouse states |
| Inventory not tracked | InventoryUnit replaces FulfillmentOrder |
| Payment validation breaks | Simplify but maintain same business rules |

---

## Timeline

| Phase | Tasks | Duration |
|-------|-------|----------|
| 1 | Expand Shipment | 2-3 days |
| 2 | Simplify Order | 2-3 days |
| 3 | Remove Fulfillment | 1 day |
| 4 | Inventory Units | 2-3 days |
| 5 | Payment Simplification | 1-2 days |
| 6 | Shipping Rates | 2 days |
| 7 | Event Handlers | 1-2 days |
| 8 | Testing & Polish | 3-5 days |
| **Total** | | **2-3 weeks** |

---

## Success Criteria

✅ All Order state transitions work
✅ Shipments track complete warehouse workflow
✅ InventoryUnits created/updated automatically
✅ Payments validated simply
✅ No FulfillmentOrder references remain
✅ Existing tests pass (updated as needed)
✅ Event flow still works (Order → Shipment → Customer)
✅ API still works (old endpoints deprecated but functional)
