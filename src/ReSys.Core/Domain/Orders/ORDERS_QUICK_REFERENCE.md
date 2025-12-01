# Orders Domain - Quick Reference Guide

## Overview

Quick lookup guide for developers working with the Orders bounded context. For detailed context and examples, see `ORDERS_COMPLETE_GUIDE.md`.

---

## Order State Machine

### State Progression Flow

```
┌─────────────────────────────────────────────────────────────┐
│ START: Create new order                                      │
└─────────────┬───────────────────────────────────────────────┘
              │
              ▼
         ┌─────────┐
         │  CART   │ ◄─── Add/remove line items, review
         │ State=0 │      quantities and prices
         └────┬────┘
              │ Next() - requires ≥1 item
              ▼
         ┌─────────┐
         │ ADDRESS │ ◄─── Set shipping & billing addresses
         │ State=1 │      (physical orders only)
         └────┬────┘
              │ Next() - addresses required for physical
              ▼
         ┌─────────┐
         │ DELIVERY│ ◄─── Select shipping method,
         │ State=2 │      assign fulfillment location
         └────┬────┘
              │ Next() - shipping method required
              ▼
         ┌─────────┐
         │ PAYMENT │ ◄─── Process payment, authorize
         │ State=3 │      or capture charges
         └────┬────┘
              │ Next() - payment must cover total
              ▼
         ┌─────────┐
         │ CONFIRM │ ◄─── Final order review
         │ State=4 │
         └────┬────┘
              │ Next() - captured payment required
              ▼
         ┌─────────┐
         │ COMPLETE│ ◄─── Order finalized, inventory
         │ State=5 │      reduced. TERMINAL STATE
         └─────────┘

At any non-Complete state:
         Cancel() ──► CANCELED (terminal, inventory released)
```

### State Characteristics

| State | Purpose | Key Actions | Requirements | Digital OK? |
|-------|---------|-------------|--------------|-----------|
| **Cart** | Shopping | Add/remove items, set email | ≥1 item to progress | ✅ |
| **Address** | Location | Set ship & bill addresses | Addresses for physical | ❌ |
| **Delivery** | Fulfillment | Pick warehouse, shipping | Shipping method | ❌ |
| **Payment** | Authorization | Add payment records | Payment received | ✅ |
| **Confirm** | Review | Final checks | Payment captured | ✅ |
| **Complete** | Final | Inventory adjusted | (terminal state) | ✅ |
| **Canceled** | Cancelled | Cleanup | (terminal state) | ✅ |

---

## Common Operations

### Creating an Order

```csharp
// Factory method returns ErrorOr
var result = Order.Create(
    storeId: store.Id,
    currency: "USD",
    userId: user?.Id,
    email: "customer@example.com");

if (result.IsError) return Problem(result.FirstError);
var order = result.Value;
```

### Adding Items

```csharp
// Add variant with quantity
var addResult = order.AddLineItem(variant, quantity: 2);
if (addResult.IsError) return Problem(addResult.FirstError);

// Already in cart? Quantity increments automatically
// Price captured at addition time (snapshot)
```

### Managing Addresses

```csharp
// Physical orders require both addresses
var shipResult = order.SetShippingAddress(address);
var billResult = order.SetBillingAddress(address);

// Digital orders reject shipping address
// Both return ErrorOr<Order>
```

### State Transitions

```csharp
// Progress to next state
var result = order.Next(); // Validates prerequisites
if (result.IsError) return Problem(result.FirstError);

// Specific transitions also available:
// - order.ToAddress()
// - order.ToDelivery()
// - order.ToPayment()
// - order.ToConfirm()
// - order.Complete()
```

### Applying Promotions

```csharp
// Apply with optional coupon code
var result = order.ApplyPromotion(
    promotion: promo,
    code: "SUMMER20");  // Optional if coupon not required

if (result.IsError) return Problem(result.FirstError);

// Remove if needed
var removeResult = order.RemovePromotion();
```

### Setting Shipping

```csharp
// Physical orders only
var result = order.SetShippingMethod(shippingMethod);
if (result.IsError) return Problem(result.FirstError);

// Calculates cost based on weight and order total
// ShipmentTotalCents updated automatically
```

### Processing Payments

```csharp
// Add payment record (starts in Pending state)
var paymentResult = order.AddPayment(
    amountCents: (long)(150.00m * 100),  // $150.00
    paymentMethodId: paymentMethodId,
    paymentMethodType: "CreditCard");

if (paymentResult.IsError) return Problem(paymentResult.FirstError);
var payment = paymentResult.Value;

// Transition payment to captured (externally, via Payment aggregate)
// Later: check if payment captured before transitioning to Confirm
```

### Canceling Orders

```csharp
// Can cancel from any non-Complete state
var result = order.Cancel();
if (result.IsError) return Problem(result.FirstError);
// Terminal state; releases inventory, publishes events
```

---

## Key Classes Reference

### Order (Aggregate Root)

**Factory:** `Order.Create(storeId, currency, userId?, email?)`

**Key Methods:**
- `Next()` - Progress to next state
- `AddLineItem(variant, quantity)` - Add or increment item
- `RemoveLineItem(lineItemId)` - Remove item
- `SetShippingAddress(address)` - Set shipping address
- `SetBillingAddress(address)` - Set billing address
- `SetShippingMethod(method)` - Select shipping & calculate cost
- `ApplyPromotion(promo, code?)` - Apply promotion with adjustments
- `RemovePromotion()` - Remove promotion adjustments
- `AddPayment(cents, methodId, type)` - Record payment
- `SetFulfillmentLocation(location)` - Assign warehouse
- `Cancel()` - Cancel order (non-terminal states only)

**Key Properties:**
- `State` - Current OrderState (Cart, Address, etc.)
- `ItemTotalCents` - Sum of line item subtotals
- `ShipmentTotalCents` - Shipping cost
- `AdjustmentTotalCents` - Sum of all adjustments
- `TotalCents` - Grand total = Item + Shipment + Adjustment
- `IsFullyDigital` - All items are digital products

**Computed Properties:**
- `Total`, `ItemTotal`, `ShipmentTotal` - Decimal versions (÷100)
- `IsCart`, `IsComplete`, `IsCanceled` - State checks
- `ItemCount` - Total unit count
- `HasPromotion` - Promotion applied
- `PromotionTotal` - Discount amount

### LineItem (Owned Entity)

**Factory:** `LineItem.Create(orderId, variant, quantity, currency)`

**Key Properties:**
- `VariantId` - Product variant
- `Quantity` - Units ordered
- `PriceCents` - Unit price captured at order time
- `Currency` - Inherited from Order
- `CapturedName` - Product name snapshot
- `CapturedSku` - SKU snapshot
- `IsPromotional` - Free/bonus item flag

**Computed Properties:**
- `SubtotalCents` - Quantity × PriceCents
- `TotalCents` - SubtotalCents + Adjustments
- `Subtotal`, `Total`, `UnitPrice` - Decimal versions

### Payment (Aggregate)

**Factory:** `Payment.Create(orderId, amountCents, currency, type, methodId)`

**States:** Pending → Processing → Completed / Failed / Void / Refunded

**Key Methods:**
- `Capture(transactionId)` - Complete payment
- `StartProcessing()` - Begin processing
- `Void()` - Cancel authorization
- `Refund()` - Refund completed payment
- `MarkAsFailed(errorMsg)` - Record failure

### Shipment (Aggregate)

**Factory:** `Shipment.Create(orderId, shippingMethodId, stockLocationId?)`

**States:** Pending → Ready → Shipped → Delivered / Canceled

**Key Methods:**
- `AssignStockLocation(location)` - Assign warehouse
- `Ready()` - Mark ready for pickup
- `Ship(trackingNumber)` - Record shipment
- `Deliver()` - Mark delivered
- `Cancel()` - Cancel shipment

### Adjustments

- **OrderAdjustment**: Order-level adjustments (typically order discounts)
- **LineItemAdjustment**: Item-level adjustments (typically item discounts)

---

## Constraints Quick Lookup

### String Lengths
- **Email**: 256 characters max
- **SpecialInstructions**: 500 characters max
- **PromoCode**: 50 characters max
- **Currency**: 3 characters (ISO 4217)
- **CapturedName**: 255 characters max
- **CapturedSku**: 50 characters max

### Numeric Values
- **QuantityMinValue**: 1 (no zero or negative)
- **AmountCentsMinValue**: 0 (no negative payments)

---

## Error Codes Reference

### Order Errors
| Code | Meaning | Recovery |
|------|---------|----------|
| `Order.InvalidStateTransition` | Cannot move from current state to requested | Check current state; ensure prerequisites met |
| `Order.EmptyCart` | Cannot checkout empty cart | Add at least one item |
| `Order.CannotCancelCompleted` | Cannot cancel completed order | Check if order is complete |
| `Order.LineItemNotFound` | Referenced line item doesn't exist | Verify line item ID |
| `Order.PromotionAlreadyApplied` | Can't apply second promotion | Remove previous before applying new |
| `Order.DigitalOrderNoShipping` | Digital order can't have shipping | Check `IsFullyDigital` flag |

### Payment Errors
| Code | Meaning | Recovery |
|------|---------|----------|
| `Payment.AlreadyCaptured` | Payment already captured | Check payment state |
| `Payment.CannotVoidCaptured` | Can't void captured payment | Only pending/processing can void |
| `Payment.CannotRefundNonCompleted` | Can only refund completed payments | Check payment state first |

---

## Validation Rules (Business Rules)

### Cart → Address Transition
- **Requirement**: Order must have ≥ 1 line item
- **Digital Orders**: Skip address validation

### Address → Delivery Transition
- **Physical Orders**: Both ShipAddress and BillAddress required
- **Digital Orders**: Skip (no addresses needed)

### Delivery → Payment Transition
- **Physical Orders**: ShippingMethod must be set; Shipment created
- **Digital Orders**: Skip shipping; no shipment created

### Payment → Confirm Transition
- **All Orders**: Sum of payment amounts ≥ TotalCents

### Confirm → Complete Transition
- **All Orders**: Completed/captured payments ≥ TotalCents
- **Side effects**: FinalizeInventory event published; inventory reduced

### Promotion Application
- **Single Promotion**: Only one per order; new replaces old
- **Coupon Validation**: If promotion requires code, must match
- **Adjustment Preservation**: Non-promotion adjustments preserved

---

## Event Reference

Events published to enable integrations:

| Event | When | Listeners |
|-------|------|-----------|
| **Order.Created** | New order created | Inventory, Notifications |
| **Order.StateChanged** | State transitions | Analytics |
| **Order.Completed** | Order finalized | Inventory, Notifications, Fulfillment |
| **Order.Canceled** | Order canceled | Inventory, Notifications |
| **Order.LineItemAdded** | Item added | Inventory (reservation) |
| **Order.LineItemRemoved** | Item removed | Inventory (release) |
| **Order.FinalizeInventory** | On completion | Inventory (reduce stock) |
| **Order.ReleaseInventory** | On cancellation | Inventory (restore stock) |
| **Order.PromotionApplied** | Promotion added | Analytics |
| **Order.PromotionRemoved** | Promotion removed | Analytics |
| **Order.ShippingMethodSelected** | Shipping chosen | Logistics |
| **Payment.PaymentCaptured** | Payment captured | Notifications, Fulfillment |

---

## Digital vs Physical Order Differences

### Digital Orders
- Skip Address state validation
- Skip Delivery state (no shipping)
- No ShippingMethod required
- No FulfillmentLocation needed
- Shipment NOT created
- Direct Cart → Address → Payment → Confirm → Complete

### Physical Orders
- Require ShipAddress and BillAddress
- Require ShippingMethod in Delivery state
- ShipmentTotalCents calculated from weight + value
- FulfillmentLocation assigned from warehouse
- Shipment created when moving Payment state
- Full Cart → Address → Delivery → Payment → Confirm → Complete

---

## Calculation Formulas

### Order Totals
```
ItemTotalCents = SUM(LineItem.SubtotalCents)
                = SUM(LineItem.PriceCents × LineItem.Quantity)

AdjustmentTotalCents = SUM(OrderAdjustment.AmountCents)
                     + SUM(LineItem.Adjustments.Sum(a => a.AmountCents))

ShipmentTotalCents = ShippingMethod.CalculateCost(weight, itemTotal) × 100

TotalCents = ItemTotalCents + ShipmentTotalCents + AdjustmentTotalCents
```

### Line Item Totals
```
SubtotalCents = PriceCents × Quantity

TotalCents = SubtotalCents + SUM(LineItemAdjustment.AmountCents)
```

### Payment Sufficiency
```
Completed_Payments = SUM(Payment.AmountCents WHERE Payment.IsCompleted)
Can_Confirm = Completed_Payments ≥ TotalCents
```

---

## Common Patterns

### Building a Complete Order Flow

```csharp
// 1. Create order
var orderResult = Order.Create(storeId, "USD", userId);
var order = orderResult.Value;

// 2. Add items
foreach (var item in cartItems)
{
    var addResult = order.AddLineItem(item.Variant, item.Quantity);
    if (addResult.IsError) return Problem(addResult.FirstError);
}

// 3. Set addresses (physical only)
if (!order.IsFullyDigital)
{
    await order.SetShippingAddress(address);
    await order.SetBillingAddress(address);
}

// 4. Progress through states
var progressResult = order.Next(); // Cart → Address
if (progressResult.IsError) return Problem(progressResult.FirstError);

if (!order.IsFullyDigital)
{
    order.SetShippingMethod(shippingMethod);
    order.SetFulfillmentLocation(warehouse);
}

order.Next(); // Address → Delivery
order.Next(); // Delivery → Payment

// 5. Apply promotion if provided
if (!string.IsNullOrEmpty(promoCode))
{
    var applyResult = order.ApplyPromotion(promotion, promoCode);
    if (applyResult.IsError) return Problem(applyResult.FirstError);
}

// 6. Process payment
var paymentResult = order.AddPayment(
    (long)(order.Total * 100),
    paymentMethodId,
    "CreditCard");
if (paymentResult.IsError) return Problem(paymentResult.FirstError);

// 7. Complete order
var completeResult = order.Next(); // Payment → Confirm
completeResult = order.Next();     // Confirm → Complete
if (completeResult.IsError) return Problem(completeResult.FirstError);

// 8. Save (events published after)
_dbContext.Orders.Add(order);
await _dbContext.SaveChangesAsync();
```

---

## Testing Checklist

When testing Orders functionality:

- [ ] State transitions respect prerequisites
- [ ] AddLineItem captures prices correctly
- [ ] Totals recalculate after changes
- [ ] Digital orders skip address/shipping validation
- [ ] Promotion adjustments distributed correctly
- [ ] Events published for all significant changes
- [ ] Cancel releases inventory
- [ ] Complete finalizes inventory
- [ ] Payment states transition validly
- [ ] Shipment created in Delivery→Payment transition
- [ ] Error codes accurate and helpful

---

## Useful Links

- **Full Guide**: See `ORDERS_COMPLETE_GUIDE.md`
- **Refinement Summary**: See `ORDERS_REFINEMENT_SUMMARY.md`
- **Domain Events**: `Order.Events` and `Payment.Events` classes
- **Constraints**: `Order.Constraints`, `LineItem.Constraints`, etc.
- **Errors**: `Order.Errors`, `LineItem.Errors`, etc.

---

## FAQ

**Q: Can I add items after Address state?**  
A: Technically yes (no code enforcement), but UI should prevent it (cart locked).

**Q: What if payment fails?**  
A: Mark payment as failed; order stays in Payment state; retry or use different method.

**Q: Can I modify shipment address after order placed?**  
A: No; addresses locked once in Address state. Must cancel and recreate.

**Q: How are adjustments distributed?**  
A: PromotionCalculator determines order-level vs line-item level based on promotion rules.

**Q: Do I need to await SetShippingAddress?**  
A: No; Order methods are synchronous. SaveChangesAsync() called once at end.

**Q: Can I cancel a completed order?**  
A: No; Complete is terminal state and returns `CannotCancelCompleted` error.

---

**Last Updated**: 2024  
**Version**: 1.0  
**Audience**: Developers, QA Engineers  
**Maintainer**: Development Team
