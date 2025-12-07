# Orders.Returns Bounded Context

## Overview

The **Returns** bounded context manages the complete lifecycle of returned items within the Orders domain. It tracks customer returns, validates return eligibility, processes refunds and exchanges, and coordinates inventory restoration.

## Core Aggregates

### ReturnItem

Represents a single return request for an inventory unit with full lifecycle management.

**Responsibility:**
- Tracks return eligibility and acceptance status
- Manages return reception (awaiting, received, given to customer, cancelled)
- Coordinates exchange variants and refund reimbursements
- Triggers inventory restoration for accepted returns

**Key States:**

**Reception Status (When was the item received?):**
- **Awaiting** → Item pending receipt from customer
- **Received** → Item received and inspected
- **GivenToCustomer** → Item processed at store/warehouse (no formal receipt)
- **Cancelled** → Return cancelled before completion

**Acceptance Status (Is the return acceptable?):**
- **Pending** → Initial state, awaiting eligibility evaluation
- **Accepted** → Return is eligible for refund/exchange
- **Rejected** → Return denied, no reimbursement
- **ManualInterventionRequired** → Return needs operator review

## Key Relationships

```
ReturnItem
├── InventoryUnit (the unit being returned)
│   ├── Variant (product)
│   ├── Order (customer's order)
│   └── LineItem (specific line item)
├── ExchangeVariant (if customer wants exchange instead of refund)
├── ExchangeInventoryUnits (units created if exchange approved)
└── Reimbursement (if refund is being processed)
```

## Usage Patterns

### Creating a Return

```csharp
// Option 1: Create explicit return
var returnResult = ReturnItem.Create(
    inventoryUnitId: unit.Id,
    returnQuantity: 2,
    preTaxAmountCents: 5000);

if (returnResult.IsError) return Problem(returnResult.FirstError);
var returnItem = returnResult.Value;

// Option 2: Get or create from inventory unit
var existingOrNew = ReturnItem.FromInventoryUnit(inventoryUnit);
```

### Processing Return Reception

```csharp
// Customer receives item, begins inspection
var receiveResult = returnItem.Receive();

// Alternative: In-store/warehouse return without formal mailing
var giveResult = returnItem.GiveToCustomer();

// Accept return after inspection
var acceptResult = returnItem.Accept();

// Or reject if damaged/non-returnable
var rejectResult = returnItem.Reject();

// Or flag for manual review
var manualResult = returnItem.RequireManualIntervention();
```

### Processing Exchanges

```csharp
// Customer wants to exchange for different size/color
var exchangeVariant = await _dbContext.Variants.FindAsync(newVariantId);
var exchangeResult = returnItem.SetExchangeVariant(exchangeVariant);

if (exchangeResult.IsError) return Problem(exchangeResult.FirstError);

// Check if exchange needs processing
if (returnItem.IsExchangeRequired)
{
    // Create new inventory unit with exchange variant
    var newUnit = InventoryUnit.Create(
        variantId: exchangeVariant.Id,
        orderId: returnItem.InventoryUnit.OrderId,
        lineItemId: returnItem.InventoryUnit.LineItemId);
    
    // Link as exchange unit
    returnItem.ExchangeInventoryUnits.Add(newUnit.Value);
}
```

### Processing Refunds

```csharp
// After return is accepted, process reimbursement
if (returnItem.AcceptanceStatus == ReturnAcceptanceStatus.Accepted)
{
    // Create reimbursement record
    var reimbursement = Reimbursement.Create(
        amountCents: returnItem.PreTaxAmountCents);
    
    // Associate with return
    var reimbResult = returnItem.AssociateReimbursement(reimbursement.Id);
    if (reimbResult.IsError) return Problem(reimbResult.FirstError);
    
    // Process payment (via Payments context)
    await ProcessPaymentRefund(reimbursement);
}
```

### Inventory Restoration

```csharp
// After return is received and accepted, restore inventory
if (returnItem.Resellable && returnItem.HasCompletedReception)
{
    var inventoryResult = returnItem.ProcessInventoryUnit();
    if (inventoryResult.IsError) return Problem(inventoryResult.FirstError);
    
    // Event handler in Inventories context will create stock movement
    // to restore inventory if item is resellable
}
```

## Domain Events

| Event | When | Use Case |
|-------|------|----------|
| `Created` | Return initiated | Log return, notify warehouse |
| `Received` | Item received from customer | Trigger inspection workflow |
| `GivenToCustomer` | In-store return processed | Update inventory |
| `Cancelled` | Return cancelled | Remove from pending |
| `Accepted` | Return approved after inspection | Approve reimbursement |
| `Rejected` | Return denied | Notify customer, close |
| `ManualInterventionRequired` | Needs operator review | Escalate to queue |
| `ExchangeVariantSelected` | Customer wants different item | Prepare replacement |
| `ReimbursementAssociated` | Refund approved | Process payment |
| `InventoryRestored` | Accepted return in stock | Restore availability |

## Queries

### Common Queries

```csharp
// Returns awaiting receipt
var awaitingReturn = dbContext.ReturnItems
    .Where(ri => ri.ReceptionStatus == ReturnItem.ReturnReceptionStatus.Awaiting)
    .Include(ri => ri.InventoryUnit)
    .ToList();

// Returns pending acceptance decision
var pendingAcceptance = dbContext.ReturnItems
    .Where(ri => ri.AcceptanceStatus == ReturnItem.ReturnAcceptanceStatus.Pending)
    .Where(ri => ri.ReceptionStatus != ReturnItem.ReturnReceptionStatus.Awaiting)
    .ToList();

// Returns requiring reimbursement
var needsReimbursement = dbContext.ReturnItems
    .Where(ri => ri.AcceptanceStatus == ReturnItem.ReturnAcceptanceStatus.Accepted)
    .Where(ri => !ri.IsReimbursed)
    .ToList();

// Exchange returns requiring processing
var pendingExchanges = dbContext.ReturnItems
    .Where(ri => ri.IsExchangeRequired)
    .Include(ri => ri.ExchangeVariant)
    .ToList();

// Returns for a specific order
var orderReturns = dbContext.ReturnItems
    .Where(ri => ri.InventoryUnit.OrderId == orderId)
    .Include(ri => ri.InventoryUnit)
    .ToList();

// Reimbursed returns (completed refunds)
var reimbursedReturns = dbContext.ReturnItems
    .Where(ri => ri.IsReimbursed)
    .ToList();
```

## Related Bounded Contexts

### Orders
- `InventoryUnit`: Units being returned
- `LineItem`: Pricing information for calculations

### Inventories
- Stock restoration via events (`InventoryRestored`)
- Inventory unit state transitions

### Payments
- Reimbursement processing
- Refund payment execution

### Catalog
- `Variant`: Exchange variant selection
- Product information for restocking

## Implementation Notes

### Tax Handling
- **Removed in this port**: Tax calculations were moved to LineItem level
- Returns only track `PreTaxAmountCents` for refunds
- Tax adjustments are handled separately at order level

### Backorder Integration
- Returns don't directly trigger backorder filling
- Inventory restoration events trigger stock availability
- Stock availability events trigger backorder processing (separate concern)

### Partial Returns
- `ReturnQuantity` may be less than `InventoryUnit.Quantity`
- When partial, a new InventoryUnit is created for the returned portion
- The split tracking occurs via `InventoryUnit.Split()` in domain logic

### Eligibility Validation
- Overridable eligibility validator (pluggable for business rules)
- Default behavior: Auto-accepts on reception
- Can be extended with custom validators (30-day window, condition checks, etc.)

## Future Enhancements

- [ ] Multi-leg returns (partial processing)
- [ ] Return reason tracking and analytics
- [ ] RMA (Return Authorization) number generation
- [ ] Return shipping label generation
- [ ] Conditional refunds (store credit vs payment method)
- [ ] Return analytics (abuse detection, trending items)
- [ ] Reverse logistics optimization
