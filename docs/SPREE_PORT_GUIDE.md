# Ruby on Rails Spree Ecommerce → .NET 9 ReSys.Shop Migration Guide

## Overview

This document outlines the successful port of Spree's core inventory and returns models from Ruby on Rails to .NET 9 following ReSys.Shop's Domain-Driven Design (DDD) and Clean Architecture patterns.

**Key Principle**: All models have been ported as **persistence-ignorant domain aggregates** with no database concerns mixed into domain logic. Tax calculations have been removed from InventoryUnit and ReturnItem, moving all tax handling to the Orders/LineItem level.

---

## Models Ported

### 1. **InventoryUnit** (New to Orders Domain)
**Location**: `src/ReSys.Core/Domain/Inventories/Stocks/InventoryUnit.cs`

**Purpose**: Tracks individual product units through the fulfillment and return lifecycle.

**Rails Model Mapping**:
```ruby
class Spree::InventoryUnit < Spree.base_class
  # State machine: on_hand → backordered → shipped → returned
  # Line item relationship
  # Shipment tracking
  # Return item tracking
end
```

**Key Changes from Rails**:
- ✅ Tax methods removed (`additional_tax_total`, `included_tax_total`) - moved to LineItem
- ✅ State machine implemented as enum + methods rather than gem dependency
- ✅ ErrorOr pattern for error handling (no exceptions)
- ✅ Domain events for state transitions
- ✅ Unit splitting support for partial fulfillments
- ✅ Backordered unit tracking linked to StockItem

**States**:
```csharp
public enum InventoryUnitState
{
    OnHand = 0,
    Backordered = 1,
    Shipped = 2,
    Returned = 3
}
```

**Key Methods**:
- `Create()` - Factory method with validation
- `FillBackorder()` - Transition from backordered to on-hand
- `Ship()` - Transition to shipped
- `Return()` - Transition to returned (for return processing)
- `Split()` - Partition a unit (for partial fulfillments)
- `SetStockLocation()` - Assign fulfillment location
- `GetCurrentReturnItem()` - Retrieve associated return

**Domain Events**:
- `Created` - New unit created
- `BackorderFilled` - Backorder filled when stock becomes available
- `Shipped` - Unit shipped
- `Returned` - Unit returned by customer
- `Split` - Unit split into two
- `StockLocationAssigned` - Fulfillment location assigned

---

### 2. **ReturnItem** (New to Orders.Returns Domain)
**Location**: `src/ReSys.Core/Domain/Orders/Returns/ReturnItem.cs`

**Purpose**: Manages return requests, refund eligibility, acceptance status, and exchange processing.

**Rails Model Mapping**:
```ruby
class Spree::ReturnItem < Spree.base_class
  # Return eligibility validation
  # Reception status state machine (awaiting → received → given/cancelled)
  # Acceptance status state machine (pending → accepted/rejected/manual)
  # Exchange variant selection
  # Reimbursement tracking
end
```

**Key Changes from Rails**:
- ✅ Tax removed (`pre_tax_amount` only, no `included_tax_total` or `additional_tax_total`)
- ✅ Two state machines implemented as enums + methods
- ✅ ErrorOr pattern for error handling
- ✅ Eligibility validator removed from aggregate (can be injected via service)
- ✅ Exchange variant support for customer exchanges
- ✅ Reimbursement association for refund tracking
- ✅ Resellable flag for inventory restoration logic

**Enums**:
```csharp
public enum ReturnReceptionStatus
{
    Awaiting = 0,       // Waiting for item receipt
    Received = 1,       // Item received from customer
    GivenToCustomer = 2, // Warehouse/store return
    Cancelled = 3       // Return cancelled
}

public enum ReturnAcceptanceStatus
{
    Pending = 0,                     // Awaiting decision
    Accepted = 1,                    // Approved for refund/exchange
    Rejected = 2,                    // Denied
    ManualInterventionRequired = 3   // Needs review
}
```

**Key Methods**:
- `Create()` - Factory method
- `FromInventoryUnit()` - Get or create from unit
- `Receive()` - Mark as received & attempt acceptance
- `GiveToCustomer()` - In-store return
- `Cancel()` - Cancel return
- `AttemptAccept()` - Auto-accept based on eligibility
- `Accept()` / `Reject()` / `RequireManualIntervention()` - Manual status overrides
- `SetExchangeVariant()` - Select exchange product
- `AssociateReimbursement()` - Link reimbursement
- `ProcessInventoryUnit()` - Transition inventory unit to returned & trigger restoration
- Computed properties: `IsExchangeRequested`, `IsExchangeProcessed`, `IsExchangeRequired`, `IsDecided`, `IsReimbursed`

**Domain Events**:
- `Created` - Return initiated
- `Received` - Item received
- `GivenToCustomer` - In-store return processed
- `Cancelled` - Return cancelled
- `Accepted` - Return accepted
- `Rejected` - Return rejected
- `ManualInterventionRequired` - Escalated for review
- `ExchangeVariantSelected` - Exchange variant chosen
- `ReimbursementAssociated` - Refund approved
- `InventoryRestored` - Inventory should be restored

---

### 3. **StockItem Enhancement** (Updated)
**Location**: `src/ReSys.Core/Domain/Inventories/Stocks/StockItem.cs`

**Enhancements from Rails Version**:
- ✅ Backordered `InventoryUnit` relationship added
- ✅ `ProcessBackorders()` method automatically fills backordered units when stock is replenished
- ✅ FIFO backorder filling (oldest orders filled first)
- ✅ Backorder event publishing for audit trail
- ✅ Quantity splitting support for partial backordered fulfillment

**New Event**:
- `BackorderProcessed` - Backordered unit filled from replenishment

---

## Architecture Patterns Applied

### 1. **Factory Methods** (No `new` keyword in domain)
```csharp
// ✅ Correct
var unitResult = InventoryUnit.Create(variantId, orderId, lineItemId);

// ❌ Wrong
var unit = new InventoryUnit { VariantId = variantId, ... };
```

### 2. **Railway-Oriented Error Handling** (ErrorOr Pattern)
```csharp
// ✅ Correct
var shipResult = unit.Ship();
if (shipResult.IsError) 
    return Problem(shipResult.FirstError.Description);

// ❌ Wrong - Exceptions are domain leakage
try { unit.Ship(); } catch (Exception ex) { }
```

### 3. **Domain Events** (No Direct Coupling)
```csharp
// Events published in domain methods
public ErrorOr<InventoryUnit> Return()
{
    State = InventoryUnitState.Returned;
    AddDomainEvent(new Events.Returned(Id, VariantId, OrderId));
    return this;
}

// Handlers subscribed separately (in Infrastructure/Features)
public class InventoryUnitReturnedEventHandler 
    : IEventHandler<InventoryUnit.Events.Returned>
{
    public async Task Handle(InventoryUnit.Events.Returned evt, CancellationToken ct)
    {
        // Trigger inventory restoration, customer notifications, etc.
    }
}
```

### 4. **Aggregate Boundaries** (Proper Encapsulation)
```csharp
// ✅ Correct: Access related entities through aggregate root
var returnItem = returnItems.First();
var unit = returnItem.InventoryUnit;  // Loaded via FK
var inventory = unit.StockLocation;   // Loaded via FK

// ❌ Wrong: Direct child entity query
var unit = dbContext.InventoryUnits.Find(id);  // Bypasses aggregate root
```

### 5. **State Machines Without Gems**
```csharp
// Implemented as enums + validation logic in methods
public ErrorOr<InventoryUnit> Ship()
{
    // Guard clause: only OnHand units can ship
    if (State != InventoryUnitState.OnHand)
        return Errors.CannotShipFromBackordered;
    
    State = InventoryUnitState.Shipped;
    return this;
}
```

---

## Database Schema (EF Core Migrations)

### InventoryUnit Table
```sql
CREATE TABLE inventory_units (
    id UUID PRIMARY KEY,
    variant_id UUID NOT NULL,
    order_id UUID NOT NULL,
    line_item_id UUID NOT NULL,
    shipment_id UUID,
    stock_location_id UUID,
    quantity INT NOT NULL,
    state INT NOT NULL,  -- 0=OnHand, 1=Backordered, 2=Shipped, 3=Returned
    original_return_item_id UUID,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP,
    
    FOREIGN KEY (variant_id) REFERENCES variants(id) ON DELETE NO ACTION,
    FOREIGN KEY (order_id) REFERENCES orders(id) ON DELETE CASCADE,
    FOREIGN KEY (line_item_id) REFERENCES line_items(id) ON DELETE NO ACTION,
    FOREIGN KEY (shipment_id) REFERENCES shipments(id) ON DELETE NO ACTION,
    FOREIGN KEY (stock_location_id) REFERENCES stock_locations(id) ON DELETE NO ACTION,
    FOREIGN KEY (original_return_item_id) REFERENCES return_items(id) ON DELETE NO ACTION
);

CREATE INDEX idx_inventory_units_order_id ON inventory_units(order_id);
CREATE INDEX idx_inventory_units_order_state ON inventory_units(order_id, state);
CREATE INDEX idx_inventory_units_variant_state ON inventory_units(variant_id, state);
```

### ReturnItem Table
```sql
CREATE TABLE return_items (
    id UUID PRIMARY KEY,
    inventory_unit_id UUID NOT NULL,
    return_authorization_id UUID,
    customer_return_id UUID,
    reimbursement_id UUID,
    exchange_variant_id UUID,
    return_quantity INT NOT NULL,
    pre_tax_amount_cents BIGINT NOT NULL,
    reception_status INT NOT NULL,    -- 0=Awaiting, 1=Received, 2=GivenToCustomer, 3=Cancelled
    acceptance_status INT NOT NULL,   -- 0=Pending, 1=Accepted, 2=Rejected, 3=Manual
    resellable BOOLEAN NOT NULL DEFAULT true,
    acceptance_status_errors JSONB,
    created_at TIMESTAMP NOT NULL,
    updated_at TIMESTAMP,
    
    FOREIGN KEY (inventory_unit_id) REFERENCES inventory_units(id) ON DELETE CASCADE,
    FOREIGN KEY (exchange_variant_id) REFERENCES variants(id) ON DELETE NO ACTION
);

CREATE INDEX idx_return_items_inventory_unit_id ON return_items(inventory_unit_id);
CREATE INDEX idx_return_items_reception_status ON return_items(reception_status);
CREATE INDEX idx_return_items_acceptance_status ON return_items(acceptance_status);
CREATE INDEX idx_return_items_status_compound ON return_items(reception_status, acceptance_status);
```

---

## Usage Examples

### Creating and Shipping Inventory Units

```csharp
// Create units for a line item
var variant = await _dbContext.Variants.FindAsync(variantId);
for (int i = 0; i < quantity; i++)
{
    var unitResult = InventoryUnit.Create(
        variantId: variant.Id,
        orderId: order.Id,
        lineItemId: lineItem.Id);
        
    if (unitResult.IsError)
        return Problem(unitResult.FirstError);
    
    order.InventoryUnits.Add(unitResult.Value);
}

await _dbContext.SaveChangesAsync(ct);

// Later: Ship the units
foreach (var unit in shipment.InventoryUnits)
{
    var shipResult = unit.Ship();
    if (shipResult.IsError)
        return Problem(shipResult.FirstError);
}

await _dbContext.SaveChangesAsync(ct);  // Shipment domain event published
```

### Processing Returns

```csharp
// Create return from shipped unit
var returnResult = ReturnItem.Create(inventoryUnitId: unit.Id);
if (returnResult.IsError)
    return Problem(returnResult.FirstError);

var returnItem = returnResult.Value;

// Receive return from customer
var receiveResult = returnItem.Receive();
if (receiveResult.IsError)
    return Problem(receiveResult.FirstError);

// Accept return (triggers inventory restoration event)
var acceptResult = returnItem.Accept();
if (acceptResult.IsError)
    return Problem(acceptResult.FirstError);

// Process return (inventory unit transitions to Returned)
var processResult = returnItem.ProcessInventoryUnit();
if (processResult.IsError)
    return Problem(processResult.FirstError);

_dbContext.ReturnItems.Add(returnItem);
await _dbContext.SaveChangesAsync(ct);  // Events published for inventory restoration
```

### Exchange Processing

```csharp
// Customer wants to exchange for different size
var exchangeVariant = await _dbContext.Variants.FindAsync(newVariantId);
var exchangeResult = returnItem.SetExchangeVariant(exchangeVariant);
if (exchangeResult.IsError)
    return Problem(exchangeResult.FirstError);

// Create exchange inventory unit
var exchangeUnitResult = InventoryUnit.Create(
    variantId: exchangeVariant.Id,
    orderId: returnItem.InventoryUnit.OrderId,
    lineItemId: returnItem.InventoryUnit.LineItemId);

returnItem.ExchangeInventoryUnits.Add(exchangeUnitResult.Value);
await _dbContext.SaveChangesAsync(ct);
```

### Backorder Auto-Fill

```csharp
// When stock is replenished at a location:
var stockItem = await _dbContext.StockItems.FindAsync(stockItemId);

// Adjust adds stock
var adjustResult = stockItem.Adjust(
    quantity: 10,
    originator: StockMovement.MovementOriginator.Supplier);

if (adjustResult.IsError)
    return Problem(adjustResult.FirstError);

// ProcessBackorders is called automatically
// - Gets backordered InventoryUnits for this variant at this location
// - Fills them in FIFO order (oldest first)
// - Splits units if needed
// - Publishes BackorderProcessed events

await _dbContext.SaveChangesAsync(ct);  // Events published
```

---

## Tax Handling (Important!)

### Rails Spree Approach (Removed):
```ruby
# Tax was calculated per inventory unit
def additional_tax_total
  line_item.additional_tax_total * percentage_of_line_item
end
```

### ReSys.Shop Approach (New):
- **Tax is calculated at LineItem level** (not per unit)
- **Returns**: Pre-tax amount stored; tax adjustment handled separately
- **Reason**: Simplifies unit-level operations, reduces complexity in fulfillment/returns

```csharp
// LineItem handles tax
var lineItemTax = lineItem.CalculateTax(taxRate);

// InventoryUnit only tracks pre-tax amount
var returnItem = ReturnItem.Create(
    inventoryUnitId: unit.Id,
    preTaxAmountCents: lineItem.PreTaxAmountCents);
```

---

## Migration Checklist

- [x] InventoryUnit aggregate created with state machine
- [x] ReturnItem aggregate created with dual state machines
- [x] EF Core configurations with proper relationships
- [x] Domain events defined for all state transitions
- [x] Backordered inventory unit auto-fill logic
- [x] Tax handling removed (moved to LineItem level)
- [x] Factory methods for safe instantiation
- [x] ErrorOr pattern for error handling
- [x] Aggregate boundary enforcement
- [ ] Integration tests for InventoryUnit state transitions
- [ ] Integration tests for ReturnItem acceptance/rejection flows
- [ ] Integration tests for backorder auto-fill
- [ ] Integration tests for exchange processing
- [ ] Feature handlers for domain events
- [ ] API endpoints for return management
- [ ] Database migration scripts

---

## Key Differences from Rails

| Aspect | Rails Spree | ReSys.Shop .NET |
|--------|------------|-----------------|
| **Error Handling** | Exceptions, validation errors | ErrorOr<T>, no exceptions |
| **State Machines** | state_machine gem | Enums + method guards |
| **Tax** | Per-unit tax calculation | Line-item level tax |
| **Events** | Active Job async | Domain events + handlers |
| **Relationships** | AR associations | EF Core navigation properties |
| **Validation** | AR validations | FluentValidation + factory methods |
| **Database** | Rails migrations | EF Core migrations |
| **Aggregates** | Rails models | DDD aggregates |

---

## Next Steps

1. **Create Feature Handlers** for domain events:
   - `InventoryUnitShippedEventHandler` - Reserve inventory
   - `InventoryUnitReturnedEventHandler` - Update return tracking
   - `ReturnItemAcceptedEventHandler` - Trigger reimbursement
   - `ReturnItemInventoryRestoredEventHandler` - Restore stock

2. **Create CQRS Commands/Queries**:
   - `CreateReturnItemCommand` / `CreateReturnItemHandler`
   - `ProcessReturnReceptionCommand`
   - `AcceptReturnItemCommand`
   - `GetBackorderedUnitsQuery`

3. **Create API Endpoints**:
   - POST `/api/returns` - Create return
   - PATCH `/api/returns/{id}/receive` - Mark received
   - PATCH `/api/returns/{id}/accept` - Accept return
   - GET `/api/returns?status=accepted` - Query returns

4. **Add Integration Tests**:
   - Unit state transitions
   - Return acceptance flows
   - Backorder auto-fill
   - Exchange processing

5. **Update Database Migrations** to create tables

---

## References

- **ReSys.Shop Architecture**: `/docs/`
- **Domain-Driven Design**: Eric Evans' DDD Book
- **ErrorOr Pattern**: NuGet `ErrorOr` library
- **EF Core**: Microsoft Docs
- **CQRS/MediatR**: Jimmy Bogard's MediatR library

---

## Support & Questions

For questions or clarifications on the port:
1. Review domain README files in respective contexts
2. Check existing aggregates for patterns
3. Refer to ReSys.Shop COPILOT_INSTRUCTIONS.md
