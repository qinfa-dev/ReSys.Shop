# 📊 Multi-Location Fulfillment System - Visual Summary

## System Architecture at a Glance

```
┌────────────────────────────────────────────────────────────────────┐
│                         ONE WEBSITE                                │
│                    (Single E-Commerce Store)                       │
└──────────────────────────┬─────────────────────────────────────────┘
                           │
        ┌──────────────────┼──────────────────┐
        │                  │                  │
        ▼                  ▼                  ▼
    ┌────────┐        ┌────────┐        ┌────────┐
    │Warehouse        │Warehouse        │Warehouse
    │Central          │North            │South
    │(100 units)      │(50 units)       │(30 units)
    └────────┘        └────────┘        └────────┘
        │                  │                  │
        │              SHIP INVENTORY         │
        │                  │                  │
        ▼                  ▼                  ▼
    ┌────────────────────────────────────────────────┐
    │                Fulfillment Network              │
    │     (Ship from nearest/cheapest/fullest)       │
    └─────────┬──────────────────────────────────────┘
              │
        ┌─────┴──────────────────────┐
        │                            │
        ▼                            ▼
┌─────────────────┐        ┌──────────────────┐
│  Store #1       │        │  Store #2        │
│  District 1     │        │  District 7      │
│  (Retail+Ship)  │        │  (Retail+Ship)   │
│  20 units       │        │  30 units        │
│                 │        │                  │
│ IN-STORE PICKUP │        │ IN-STORE PICKUP  │
└────────┬────────┘        └────────┬─────────┘
         │                          │
         │   CUSTOMERS CHOOSE       │
         │   (Online or In-Store)   │
         ▼                          ▼
    ┌──────────────────────────────────┐
    │  1 Customer Orders 5 Items       │
    │  Location: District 1, HCMC      │
    │  Coordinates: (10.7756, 106.7019)│
    └──────────────────────────────────┘
```

---

## Customer Journey: Three Paths

### Path 1️⃣: Ship to Home (Online)

```
Customer Decision: "Ship to my home"
                    │
                    ▼
System Analysis:
├─ Customer Location: (10.7756, 106.7019)
├─ Distance to District 1 Store: 2km ✅
├─ Distance to District 7 Store: 10km
├─ Distance to Central Warehouse: 25km
└─ Distance to North Warehouse: 500km

Fulfillment Strategy (Nearest):
├─ District 1 Store has 20 units ✅ (closest!)
├─ Pick from District 1
└─ Create Shipment

Stock Deduction:
├─ Before: District 1 has 20 units
├─ After: District 1 has 15 units
└─ Stock is GONE (shipped out)

Result:
├─ Order ships from District 1 (2km away)
├─ Fast delivery (1-2 days)
└─ Customer happy ✅
```

### Path 2️⃣: Store Pickup (In-Store)

```
Customer Decision: "Pick up at District 1 Store"
                    │
                    ▼
Available Stores Near Customer:
├─ District 1 Store: 20 units, 2km ✅
├─ District 7 Store: 30 units, 10km
└─ (shown in real-time map)

Customer Selects: District 1
                    │
                    ▼
Stock Deduction:
├─ Before: District 1 has 20 units
├─ After: District 1 has 15 units
└─ Stock is RESERVED at location

Pickup Code Generated:
├─ Code: ABC123 (6-char alphanumeric)
├─ Unique & secure
└─ SMS sent to customer

Store Staff Prepares:
├─ Gathers 5 items
├─ Quality check
├─ Ready in 2 hours

Staff Marks "Ready":
├─ SMS to customer: "Your order is ready! Code: ABC123"
├─ Store status changes to READY
└─ Customer can now pick up

Customer Arrives:
├─ Shows code: ABC123
├─ Staff verifies
├─ Hands over items

Order Complete:
├─ State: PickedUp
├─ No shipping needed
└─ Customer happy ✅
```

### Path 3️⃣: Mixed Order (Some Ship + Some Pickup)

```
Customer Decision: "Mix of both"
                    │
        ┌───────────┼───────────┐
        │           │           │
     Item 1      Item 2      Item 3
    (Ship)      (Ship)      (Pickup)
        │           │           │
        ▼           ▼           ▼

Item 1 & 2: Ship from District 1 (has 20 units)
├─ 2 units deducted
├─ Shipment created
├─ Will arrive in 1-2 days

Item 3: Pickup at District 1 Store
├─ 1 unit deducted
├─ Pickup record created
├─ Code generated

Customer Receives:
├─ 2 packages (shipped items) in 1-2 days
└─ Picks up 1 item at store
```

---

## Stock Lifecycle in Detail

### Before Order

```
District 1 Store Inventory:
┌──────────────────────────────┐
│ Product: T-Shirt Blue Size M │
├──────────────────────────────┤
│ QuantityOnHand:     20       │  (Physical count)
│ QuantityReserved:    0       │  (In carts)
│ CountAvailable:     20       │  (20 - 0)
│ Backorderable:      Yes      │  (Can order more if out)
└──────────────────────────────┘
```

### During Checkout (Cart Abandoned)

```
MOMENT 1: Customer Adds 5 to Cart

District 1 Store Inventory:
┌──────────────────────────────┐
│ Product: T-Shirt Blue Size M │
├──────────────────────────────┤
│ QuantityOnHand:     20       │  (unchanged)
│ QuantityReserved:    5       │  (reserved for order)
│ CountAvailable:     15       │  (20 - 5) ← only 15 left for others!
│ Backorderable:      Yes      │
└──────────────────────────────┘

MOMENT 2: Customer Abandons Cart (30 min later)

District 1 Store Inventory:
┌──────────────────────────────┐
│ Product: T-Shirt Blue Size M │
├──────────────────────────────┤
│ QuantityOnHand:     20       │  (restored)
│ QuantityReserved:    0       │  (released)
│ CountAvailable:     20       │  (20 - 0) ← back to full!
│ Backorderable:      Yes      │
└──────────────────────────────┘
```

### After Payment (Stock Confirmed)

```
MOMENT 3: Payment Successful → Confirm Shipment

District 1 Store Inventory:
┌──────────────────────────────┐
│ Product: T-Shirt Blue Size M │
├──────────────────────────────┤
│ QuantityOnHand:     15       │  (physically gone)
│ QuantityReserved:    0       │  (no longer reserved, it shipped)
│ CountAvailable:     15       │  (15 - 0)
│ Backorderable:      Yes      │
└──────────────────────────────┘

Status: Order ships with tracking number
        Delivery in 1-2 days
```

### If Customer Returns (After Shipment)

```
MOMENT 4: Customer Returns 5 Units

District 1 Store Inventory:
┌──────────────────────────────┐
│ Product: T-Shirt Blue Size M │
├──────────────────────────────┤
│ QuantityOnHand:     20       │  (items back in stock!)
│ QuantityReserved:    0       │
│ CountAvailable:     20       │  (back to normal)
│ Backorderable:      Yes      │
└──────────────────────────────┘
```

---

## Fulfillment Strategies Comparison

```
╔════════════════════════════════════════════════════════════════════════╗
║                        FULFILLMENT STRATEGIES                          ║
╠════════════════════════════════════════════════════════════════════════╣
║                                                                        ║
║ NEAREST LOCATION STRATEGY                                             ║
║ ├─ Calculates distance from each location to customer                 ║
║ ├─ Picks the closest location with stock                             ║
║ ├─ Formula: Haversine (lat/lng distance)                              ║
║ ├─ Best for: Fast delivery, customer satisfaction                    ║
║ └─ Example: Orders from District 1 → Ships from District 1 Store     ║
║                                                                        ║
║ HIGHEST STOCK STRATEGY                                                ║
║ ├─ Finds location with most available inventory                       ║
║ ├─ Picks from that location first                                     ║
║ ├─ Balances inventory across network                                  ║
║ ├─ Best for: Inventory optimization                                   ║
║ └─ Example: Orders → Ship from warehouse (not retail)                 ║
║                                                                        ║
║ COST-OPTIMIZED STRATEGY                                               ║
║ ├─ Calculates shipping cost from each location                         ║
║ ├─ Picks cheapest option                                              ║
║ ├─ Requires: carrier API integration                                  ║
║ ├─ Best for: Margin optimization, bulk orders                         ║
║ └─ Example: Heavy items → Ship from nearby warehouse                  ║
║                                                                        ║
║ PREFERRED LOCATION STRATEGY                                           ║
║ ├─ Customer explicitly selects location                               ║
║ ├─ Fallback to nearest if stock unavailable                          ║
║ ├─ Best for: Pickup orders, specific warehouse requests               ║
║ └─ Example: Customer selects District 1 Store for pickup              ║
║                                                                        ║
╚════════════════════════════════════════════════════════════════════════╝
```

---

## Data Model: Key Relationships

```
┌─────────────────────────────────────────────────────────────────┐
│                                                                 │
│                         ONE BRAND                              │
│                   (ReSys.Shop Fashion)                         │
│                                                                 │
└──────────────────────┬──────────────────────────────────────────┘
                       │ (1:Many)
        ┌──────────────┴──────────────┐
        │                             │
        ▼                             ▼
    ┌──────────────────┐        ┌──────────────────┐
    │ StockLocation    │        │ StockLocation    │
    │                  │        │                  │
    │ Type: Warehouse  │        │ Type: RetailStore│
    │ ShipEnabled: ✅  │        │ ShipEnabled: ❌  │
    │ PickupEnabled: ❌│        │ PickupEnabled: ✅│
    │                  │        │                  │
    │ (1:Many)         │        │ (1:Many)         │
    └────────┬─────────┘        └────────┬─────────┘
             │                           │
             ▼                           ▼
        ┌───────────────┐           ┌───────────────┐
        │ StockItem #1  │           │ StockItem #5  │
        │ Variant: Shirt│           │ Variant: Shirt│
        │ OnHand: 100   │           │ OnHand: 20    │
        │ Reserved: 0   │           │ Reserved: 5   │
        │ Available: 100│           │ Available: 15 │
        └───────────────┘           └───────────────┘

    ┌─────────────────────────────────────────────┐
    │              ORDER (Customer)                │
    │                                             │
    │ FulfillmentType: Ship / Pickup / Mixed      │
    │                                             │
    │ (1:Many)        (1:Many)        (1:Many)    │
    └────┬─────────────┬──────────────┬───────────┘
         │             │              │
         ▼             ▼              ▼
    ┌─────────┐  ┌──────────┐  ┌────────────┐
    │Shipment │  │StorePickup│  │LineItem    │
    │         │  │           │  │            │
    │State: Pending          │  │Variant: Shirt
    │From: Warehouse         │  │Qty: 5      │
    │Cost: $12.50            │  │            │
    └─────────┘  ├─ Code: ABC123
                 ├─ State: Ready
                 └─ ReadyAt: 2h from now
```

---

## Stock Transfer (Inventory Balancing)

```
SCENARIO: District 1 Store Running Low

Before Transfer:
┌──────────────────────────────────────────────────────────┐
│ District 1 Store     │ Central Warehouse                 │
├──────────────────────┼──────────────────────────────────┤
│ T-Shirt (3 units) ⚠️ │ T-Shirt (100 units) ✅             │
│ (Critical stock!)    │ (Overstocked)                   │
└──────────────────────┴──────────────────────────────────┘

Transfer Request: Move 30 units from Central to District 1

State 1: Pending
├─ Admin initiates transfer
└─ Shipment label printed

State 2: InTransit
├─ Central deducts 30 from inventory:
│  Before: 100 → After: 70 ✅
├─ Transfer tracked with number
└─ In transit for 1-2 days

State 3: Received (at District 1)
├─ District 1 adds 30 to inventory:
│  Before: 3 → After: 33 ✅
├─ Receives reconciliation
│  Expected: 30
│  Actual: 30 (perfect!)
└─ Transfer complete

After Transfer:
┌──────────────────────────────────────────────────────────┐
│ District 1 Store     │ Central Warehouse                 │
├──────────────────────┼──────────────────────────────────┤
│ T-Shirt (33 units) ✅│ T-Shirt (70 units) ✅             │
│ (Back to normal)     │ (Still well-stocked)             │
└──────────────────────┴──────────────────────────────────┘
```

---

## Pickup Code Flow

```
┌─────────────────────────────────────────┐
│ Customer Pays for Order                 │
│ Fulfillment: Store Pickup               │
│ Location: District 1 Store              │
└──────────────────┬──────────────────────┘
                   │
                   ▼
┌─────────────────────────────────────────┐
│ Pickup Code Generated:                  │
│                                         │
│ ╔═══════════════════════════════════╗   │
│ ║ CODE: ABC123                      ║   │
│ ║ Order: #ORD-2025-001234           ║   │
│ ║ Store: District 1 Store           ║   │
│ ║ Ready in: 2 hours                 ║   │
│ ╚═══════════════════════════════════╝   │
└──────────────────┬──────────────────────┘
                   │
                   ▼
        ┌──────────────────────┐
        │ SMS to Customer:     │
        │ "Your order is ready!│
        │  Pickup code: ABC123 │
        │  District 1 Store"   │
        └──────────────────────┘
                   │
                   ▼
        ┌──────────────────────┐
        │ Customer Arrives at  │
        │ Store (next day)     │
        └──────────────────────┘
                   │
                   ▼
        ┌──────────────────────┐
        │ Staff Asks: Code?    │
        │                      │
        │ Customer: ABC123     │
        └──────────────────────┘
                   │
        ┌──────────┴──────────┐
        │                     │
    ✅ VALID               ❌ INVALID
        │                     │
        ▼                     ▼
    Hand Over          Deny Pickup
    Items              (Security)
        │
        ▼
    Mark: PickedUp
    Order Complete ✅
```

---

## Implementation Roadmap (Visual)

```
WEEK 1-2
┌─────────────────────────────────────────────────────────┐
│ PHASE 1: Domain Enhancement                             │
│                                                         │
│ StockLocation                  StockItem               │
│ ├─ Location types              ├─ reserve()            │
│ ├─ Geographic data             ├─ release()            │
│ ├─ Ship/Pickup flags           ├─ confirmShipment()    │
│ └─ Domain events               └─ adjust()             │
│                                                         │
│ Status: 🟢 FOUNDATION                                   │
└─────────────────────────────────────────────────────────┘
                    │
                    ▼
WEEK 2
┌─────────────────────────────────────────────────────────┐
│ PHASE 2: Fulfillment Strategy Pattern                   │
│                                                         │
│ ├─ Nearest Location Strategy                            │
│ ├─ Highest Stock Strategy                               │
│ ├─ Cost-Optimized Strategy                              │
│ └─ Strategy Factory                                     │
│                                                         │
│ Status: 🟢 SMART ALLOCATION                             │
└─────────────────────────────────────────────────────────┘
        │                          │
        └─────────┬────────────────┘
                  ▼
WEEK 2-3
┌─────────────────────────────────────────────────────────┐
│ PHASE 3: Store Pickup Management                        │
│                                                         │
│ StorePickup                                             │
│ ├─ States: Pending → Ready → PickedUp                   │
│ ├─ Code generation (ABC123)                             │
│ ├─ State transitions                                    │
│ └─ Domain events                                        │
│                                                         │
│ Status: 🟢 PICKUP READY                                 │
└─────────────────────────────────────────────────────────┘
        │                          │
        └─────────┬────────────────┘
                  ▼
WEEK 3
┌─────────────────────────────────────────────────────────┐
│ PHASE 4: Stock Transfer Management                      │
│                                                         │
│ StockTransfer                                           │
│ ├─ Initiate (Deduct from source)                        │
│ ├─ States: Pending → InTransit → Received              │
│ ├─ Receive with reconciliation                          │
│ └─ Domain events                                        │
│                                                         │
│ Status: 🟢 TRANSFER READY                               │
└─────────────────────────────────────────────────────────┘
        │                          │
        └─────────┬────────────────┘
                  ▼
WEEK 3-4
┌─────────────────────────────────────────────────────────┐
│ PHASE 5: Order Fulfillment Orchestration                │
│                                                         │
│ Commands:                                               │
│ ├─ FulfillOrder (uses strategies)                       │
│ ├─ CreateStorePickup                                    │
│ ├─ CompletePickup (code verification)                   │
│ └─ InitiateStockTransfer                                │
│                                                         │
│ Status: 🟢 ORCHESTRATION READY                          │
└─────────────────────────────────────────────────────────┘
        │                          │
        └─────────┬────────────────┘
                  ▼
WEEK 4
┌─────────────────────────────────────────────────────────┐
│ PHASE 6: Query Services                                 │
│                                                         │
│ Queries:                                                │
│ ├─ CheckStockAvailability (online + pickup)             │
│ ├─ GetNearbyLocations (with distance)                   │
│ └─ Distance calculations (Haversine)                    │
│                                                         │
│ Status: 🟢 QUERIES READY                                │
└─────────────────────────────────────────────────────────┘
        │                          │
        └─────────┬────────────────┘
                  ▼
WEEK 4
┌─────────────────────────────────────────────────────────┐
│ PHASE 7: API Endpoints                                  │
│                                                         │
│ Routes:                                                 │
│ ├─ POST /orders/{id}/fulfill                            │
│ ├─ GET /availability                                    │
│ ├─ POST /pickups                                        │
│ ├─ PUT /pickups/{id}/ready                              │
│ ├─ PUT /pickups/{id}/complete                           │
│ └─ Stock transfer endpoints                             │
│                                                         │
│ Status: 🟢 API READY                                    │
└─────────────────────────────────────────────────────────┘
        │                          │
        └─────────┬────────────────┘
                  ▼
WEEK 5
┌─────────────────────────────────────────────────────────┐
│ PHASE 8: Testing & Documentation                        │
│                                                         │
│ ├─ Unit tests (domain models)                           │
│ ├─ Integration tests (workflows)                        │
│ ├─ API tests (endpoints)                                │
│ ├─ Update README & docs                                 │
│ └─ Production ready! 🚀                                 │
│                                                         │
│ Status: 🟢 RELEASE READY                                │
└─────────────────────────────────────────────────────────┘
```

---

## Key Metrics to Track

```
┌────────────────────────────────────────────────────────┐
│ FULFILLMENT METRICS                                    │
├────────────────────────────────────────────────────────┤
│                                                        │
│ Average Fulfillment Time                              │
│ ├─ Nearest Strategy: 1-2 days                         │
│ ├─ Highest Stock: 1-3 days                            │
│ └─ Cost-Optimized: 2-4 days                           │
│                                                        │
│ Pickup Performance                                     │
│ ├─ Avg Preparation Time: 2 hours                      │
│ ├─ Pickup Rate: 95%+ (of available)                   │
│ └─ Customer Satisfaction: 98%+                        │
│                                                        │
│ Stock Accuracy                                         │
│ ├─ Inventory Match: 99.8%                             │
│ ├─ Transfer Losses: <0.2%                             │
│ └─ Return Rate: <1%                                   │
│                                                        │
│ Location Utilization                                   │
│ ├─ Warehouse: 85% capacity (balance)                  │
│ ├─ Retail Stores: 70% capacity                        │
│ └─ Distribution: Optimal                              │
│                                                        │
└────────────────────────────────────────────────────────┘
```

---

## Success Criteria

```
✅ Domain models correctly implement aggregate pattern
✅ Stock reservation prevents overselling
✅ Fulfillment strategies work with various scenarios
✅ Pickup codes are unique & secure
✅ Stock transfers maintain inventory accuracy
✅ API endpoints respond within 200ms
✅ All tests pass (unit + integration)
✅ Zero inventory discrepancies after transfers
✅ Customers see real-time availability
✅ System handles concurrent orders
✅ Geographic calculations accurate to <1km
✅ Documentation complete and clear
```

---

**Created:** December 10, 2025
**Status:** Planning Complete ✅
**Ready to Implement:** YES ✅

See `MULTI_LOCATION_FULFILLMENT_PLAN.md` for detailed implementation guide.
