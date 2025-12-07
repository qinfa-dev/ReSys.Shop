# Quick Comparison: Current vs Spree-Aligned

## One-Page Summary

### Current Architecture (Complex)

```
ORDER DOMAIN
├─ Order (customer purchase)
├─ Shipment (customer tracking)
└─ LineItem

+

FULFILLMENT DOMAIN (separate)
├─ FulfillmentOrder (warehouse directive)
├─ FulfillmentLineItem
└─ FulfillmentState machine

=

COORDINATION LAYER (event handlers)
├─ Shipment.Created → FulfillmentOrder.Created
├─ FulfillmentOrder.Shipped → Shipment.TrackingNumber
└─ FulfillmentOrder.Delivered → Shipment.State
```

**Problems:**
- Two domains for one concept
- Duplicate state machines
- Cross-domain events needed
- 1500+ lines of code
- Not proven at scale

---

### Spree-Aligned (Simple)

```
ORDER DOMAIN
├─ Order (customer purchase)
├─ Shipment (warehouse + customer tracking)
│   ├─ State: pending → ready → picked → packed → readyToShip → shipped → delivered
│   ├─ Warehouse workflow methods
│   ├─ InventoryUnits (items in shipment)
│   └─ StockMovements (audit trail)
└─ LineItem

=

SINGLE DOMAIN (no coordination needed)
```

**Benefits:**
- One domain for one concept
- Single state machine
- No event coordination
- 800 lines of code
- Proven by Spree

---

## Side-by-Side: Key Concepts

| Concept | Current | Spree-Aligned |
|---------|---------|---------------|
| **Where is warehouse state stored?** | FulfillmentOrder aggregate | Shipment aggregate |
| **How many state machines?** | 2 (Shipment + FulfillmentOrder) | 1 (Shipment) |
| **How does warehouse staff work?** | Queries FulfillmentOrder | Queries Shipment |
| **How do we track inventory?** | InventoryUnits + events | InventoryUnits + StockMovements |
| **How do we audit changes?** | Domain events only | StockMovements table |
| **Does customer see FulfillmentOrder?** | No (hidden) | N/A (doesn't exist) |
| **Does warehouse see Shipment?** | Yes (confusing mix) | Yes (single source) |
| **Multi-warehouse?** | Works (complex) | Works (simple) |
| **Code complexity** | High | Low |
| **Learning curve** | Steep | Proven |

---

## State Machines: Simplified

### Current (Two Machines - Confusing)

```
Shipment:
  Pending → Shipped → Delivered → Canceled

FulfillmentOrder:
  Pending → Allocated → Picking → Picked → Packing → ReadyToShip → Shipped → Delivered → Canceled
```

**Who's "Shipped" first? Both? Just FulfillmentOrder?**

---

### Spree-Aligned (One Machine - Clear)

```
Shipment:
  Pending → Ready → Picked → Packed → ReadyToShip → Shipped → Delivered → Canceled
  
  (Each state is clear, no duplication)
```

**Who progresses through states? Shipment. Period.**

---

## Code Example: Allocate Inventory

### Current (Confusing)

```csharp
// Creates shipment in Orders domain
var shipment = Shipment.Create(order.Id, warehouse.Id, shippingMethod.Id);

// Event fires → Fulfillment domain handler creates FulfillmentOrder
// (hidden logic)

// Warehouse staff sees FulfillmentOrder and calls:
var ffOrder = await _dbContext.FulfillmentOrders.FindAsync(id);
var result = ffOrder.AllocateInventory();

// Result: FulfillmentOrder is Allocated
// Question: What's Shipment state now? (Still Pending!)
// Answer: Confusing. Must read event handler code.
```

### Spree-Aligned (Clear)

```csharp
// Creates shipment in Orders domain
var shipment = Shipment.Create(order.Id, warehouse.Id, shippingMethod.Id);

// Warehouse staff sees Shipment and calls:
var result = shipment.AllocateInventory();

// Result: Shipment is Ready
// Question: What's Shipment state now? Ready!
// Answer: Clear. No hidden event handlers.
```

---

## Database Impact: Minimal

### Current Schema

```sql
Orders
Shipments (with: orderId, stateId, trackingNumber)
InventoryUnits

FulfillmentOrders          ← SEPARATE TABLE
FulfillmentLineItems       ← SEPARATE TABLE
```

### Spree-Aligned Schema

```sql
Orders
Shipments (with: orderId, stateId, trackingNumber, 
                 allocatedAt, pickedAt, packedAt, readyToShipAt, shippedAt, deliveredAt,
                 packageId)
InventoryUnits

StockMovements (new audit table)

FulfillmentOrders          ← DELETED
FulfillmentLineItems       ← DELETED
```

**Net effect:** Delete 2 tables, expand 1 table, add 1 audit table. Simple.

---

## Migration Path: 4 Weeks

### Week 1: Expand Shipment
- Add warehouse fields to Shipment
- Create StockMovement table
- No code changes yet

### Week 2: Move Commands
- Move AllocateInventoryCommand from Fulfillment → Orders/Shipments
- Update handlers to use Shipment
- Tests pass

### Week 3: Move Logic
- Add state machine methods to Shipment
- Delete FulfillmentOrder aggregate
- Update warehouse dashboard queries

### Week 4: Cleanup
- Integration tests
- Delete Fulfillment domain folder
- Team training

**No customer-facing API changes at any point.**

---

## Real Impact on Features

### Warehouse Dashboard Query

**Current (2 steps):**
```csharp
// Step 1: Find FulfillmentOrders
var ffOrders = await _dbContext.FulfillmentOrders.Where(...).ToListAsync();

// Step 2: Map to UI
var result = ffOrders.Select(fo => new WorkItemDto { 
    Id = fo.Id,
    OrderId = fo.OrderId,
    Items = fo.Items.Select(...)
}).ToList();
```

**Spree-Aligned (1 step):**
```csharp
// Direct query: Find Shipments with Pending state
var shipments = await _dbContext.Shipments.Where(...).ToListAsync();

// Same DTO, simpler query
var result = shipments.Select(s => new WorkItemDto { 
    Id = s.Id,
    OrderId = s.OrderId,
    Items = s.Order.LineItems.Select(...)
}).ToList();
```

---

### Customer Tracking Query

**Current (Same for both):**
```csharp
GET /api/orders/{id}/shipments
→ Returns Shipment data (tracking, state)
```

**Spree-Aligned (Still same):**
```csharp
GET /api/orders/{id}/shipments
→ Returns Shipment data (tracking, state)
→ No changes!
```

---

## Risk Assessment

### Risk: Low

**Why?**
- Shipment already exists, we're just expanding it
- No API changes needed
- Database migration straightforward
- Event handlers consolidated
- Proven by Spree at scale

**Mitigation:**
- Comprehensive test coverage (80%+)
- Feature flag for new Shipment methods
- Gradual rollout (single warehouse first)
- Keep both code paths for 1 sprint (safety)

---

## Team Communication

### For Developers
**"We're simplifying the codebase by moving warehouse operations from a separate domain into the Shipment aggregate. Same functionality, cleaner code."**

### For Business
**"We're aligning with Spree's proven architecture, which will make the system easier to maintain and cheaper to scale."**

### For QA
**"Test warehouse workflow (allocate, pick, pack, ship) with Shipment instead of FulfillmentOrder. Same scenarios, simpler queries."**

---

## Checklist: Is This Right for ReSys.Shop?

✅ Do you have multi-warehouse? → Still supported by Spree model  
✅ Do you need complex warehouse workflows? → Shipment can handle  
✅ Do you track inventory per variant? → Yes, via StockItems  
✅ Do you need audit trail? → Yes, via StockMovements  
✅ Is this proven? → Yes, Spree uses it at scale  
✅ Will it save code? → Yes, reduce by ~40%  
✅ Will team understand it? → Yes, simpler than current  

**Verdict: YES. This is the right move.** ✅

---

## Next Steps

1. **Decision**: Approve this refactoring (YES/NO)
2. **Planning**: Assign team for 4-week effort
3. **Week 1**: Expand Shipment model
4. **Week 2**: Move commands
5. **Week 3**: Delete FulfillmentOrder
6. **Week 4**: Testing & cleanup
7. **Result**: Clean, maintainable, Spree-aligned codebase

---

## Questions?

**Q: Will this break customer APIs?**
A: No. Shipment responses stay the same. Back-end changes only.

**Q: How long will refactoring take?**
A: ~4 weeks for 1-2 developers. See SPREE_ALIGNED_REFACTORING_PLAN.md for details.

**Q: Do we lose any functionality?**
A: No. Everything FulfillmentOrder does, Shipment can do. Plus it's clearer.

**Q: What about 3PL integration?**
A: Simpler with Shipment-only model. Can publish Shipment events for external systems.

**Q: Is this a breaking change?**
A: No. Shipment evolves, FulfillmentOrder disappears. Customers see no difference.

---

**Decision: Remove FulfillmentOrder, align with Spree.** ✅
