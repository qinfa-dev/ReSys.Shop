# Implementation Timeline & Dependency Graph

## 📅 Timeline Overview

```
WEEK 1-2: PHASE 1 - Domain Enhancement
├─ Enhance StockLocation (location types, geographic data)
├─ Enhance StockItem (reservation methods)
└─ Update EF configurations

WEEK 2: PHASE 2 - Fulfillment Strategy Pattern
├─ Create IFulfillmentStrategy interface
├─ Implement 4 strategies (Nearest, HighestStock, CostOptimized, Preferred)
└─ Create strategy factory

WEEK 2-3: PHASE 3 - Store Pickup Management
├─ Create StorePickup aggregate
├─ Implement state machine (Pending → Ready → PickedUp/Cancelled)
└─ Create EF configuration & migrations

WEEK 3: PHASE 4 - Stock Transfer Management
├─ Enhance StockTransfer with Initiate/Receive methods
├─ Create StockTransferItem owned entity
└─ Update configurations

WEEK 3-4: PHASE 5 - Order Fulfillment Orchestration
├─ Extend Order with fulfillment properties
├─ Create FulfillOrder command handler
├─ Create StorePickup command handler
└─ Create StockTransfer command handler

WEEK 4: PHASE 6 - Query Services
├─ Create CheckStockAvailability query
├─ Create GetNearbyLocations query
└─ Implement distance calculations

<!-- WEEK 4: PHASE 7 - API Endpoints
├─ Create fulfillment endpoints
├─ Create pickup endpoints
├─ Create stock transfer endpoints
└─ Create availability endpoints -->

WEEK 5: PHASE 8 - Testing & Documentation
├─ Unit tests for all domain models
├─ Integration tests for workflows
├─ API endpoint tests
└─ Update README & architecture docs
```

## 🔗 Dependency Graph

```
┌─────────────────────────────────────────────────────────────┐
│                    PHASE 1: Domain Models                    │
│  (Foundation - must be done first)                           │
│  ├─ StockLocation ⬆ Location Type + Geographic             │
│  ├─ StockItem ⬆ Reserve/Release/Confirm methods             │
│  └─ StorePickup ✨ NEW (State Machine)                      │
└────────────┬────────────────────────────────────────────────┘
             │
             ▼
┌─────────────────────────────────────────────────────────────┐
│          PHASE 2: Fulfillment Strategies                     │
│  (Required by: Order Fulfillment)                            │
│  ├─ IFulfillmentStrategy interface                          │
│  ├─ NearestLocationStrategy                                  │
│  ├─ HighestStockStrategy                                     │
│  └─ CostOptimizedStrategy                                    │
└────────┬──────────────────────────────────────────────────────┘
         │
         ▼
┌──────────────────────────────────────────────────────────────┐
│     PHASE 5: Order Fulfillment Orchestration                 │
│  (Uses: Strategies + StockLocation + StorePickup)            │
│  ├─ FulfillOrderCommand                                      │
│  ├─ CreateStorePickupCommand                                 │
│  ├─ CompletePickupCommand                                    │
│  └─ Shipment creation logic                                  │
└───┬──────────────────────────────────────────────────────────┘
    │
    ▼
┌──────────────────────────────────────────────────────────────┐
│     PHASE 6: Query Services                                  │
│  (Uses: StockLocation + StockItem)                           │
│  ├─ CheckStockAvailabilityQuery                              │
│  └─ GetNearbyLocationsQuery                                  │
└───┬──────────────────────────────────────────────────────────┘
    │
    ▼
┌──────────────────────────────────────────────────────────────┐
│     PHASE 7: API Endpoints                                   │
│  (Uses: All commands + queries)                              │
│  ├─ POST /orders/{id}/fulfill                                │
│  ├─ GET /availability                                        │
│  ├─ POST /pickups                                            │
│  └─ ...more endpoints                                        │
└──────────────────────────────────────────────────────────────┘
```

## 🎯 Module Dependencies

```
ReSys.Core.Domain.Inventories
├── Locations/
│   ├── StockLocation (ENHANCED ⬆)
│   │   └── Concerns: IAddress, IHasMetadata, ISoftDeletable
│   ├── StorePickup (NEW ✨)
│   │   └── References: Order, StockLocation
│   ├── StockTransfer (ENHANCED ⬆)
│   │   └── References: StockLocation (source + destination)
│   └── FulfillmentStrategies/
│       ├── IFulfillmentStrategy
│       ├── NearestLocationStrategy
│       ├── HighestStockStrategy
│       └── CostOptimizedStrategy
└── Stocks/
    └── StockItem (ENHANCED ⬆)
        ├── Methods: Reserve(), Release(), ConfirmShipment()
        └── References: StockLocation, Variant

ReSys.Core.Domain.Orders
├── Order (ENHANCED ⬆)
│   ├── FulfillmentType enum
│   ├── PreferredPickupLocationId
│   └── Collections: Pickups, Shipments, FulfillmentChoices
└── LineItem (ENHANCED ⬆)
    ├── FulfillmentMethod property
    └── References: Variant

ReSys.Core.Feature.Orders.Commands
├── FulfillOrder/ (NEW ✨)
│   ├── FulfillOrderCommand
│   ├── FulfillOrderHandler
│   └── FulfillOrderValidator
├── CreateStorePickup/ (NEW ✨)
│   ├── CreateStorePickupCommand
│   ├── CreateStorePickupHandler
│   └── CreateStorePickupValidator
├── CompletePickup/ (NEW ✨)
│   ├── CompletePickupCommand
│   ├── CompletePickupHandler
│   └── CompletePickupValidator
└── TransferStock/ (NEW ✨)
    ├── InitiateStockTransferCommand
    ├── InitiateStockTransferHandler
    └── InitiateStockTransferValidator

ReSys.Core.Feature.Inventories.Queries
├── CheckStockAvailability/ (NEW ✨)
│   ├── CheckStockAvailabilityQuery
│   └── CheckStockAvailabilityHandler
└── GetNearbyLocations/ (NEW ✨)
    ├── GetNearbyLocationsQuery
    └── GetNearbyLocationsHandler

ReSys.Infrastructure.Persistence.Configurations
├── StockLocationConfiguration (UPDATED ⬆)
├── StockItemConfiguration (UPDATED ⬆)
├── StorePickupConfiguration (NEW ✨)
└── StockTransferConfiguration (UPDATED ⬆)

ReSys.Infrastructure.Services
├── Fulfillment/ (NEW ✨)
│   ├── IFulfillmentStrategyFactory
│   ├── FulfillmentStrategyFactory
│   ├── IShippingCalculatorService
│   └── ShippingCalculatorService
```

## 📊 State Diagrams

### StorePickup State Machine
```
         ┌─────────────┐
         │   Pending   │
         └──────┬──────┘
                │ MarkReady()
                ▼
         ┌─────────────┐
         │    Ready    │
         └──────┬──────┘
                │ CompletePickup(code)
                ▼
         ┌──────────────┐
         │   PickedUp   │
         └──────────────┘

         Cancel() can happen from:
         - Pending → Cancelled
         - Ready → Cancelled
```

### Stock Reservation Lifecycle
```
StockItem
├─ QuantityOnHand = 100
├─ QuantityReserved = 0
├─ CountAvailable = 100

Order 1: Reserve(50)
├─ QuantityOnHand = 100 (unchanged)
├─ QuantityReserved = 50
└─ CountAvailable = 50

Order 2: Reserve(40) (partial)
├─ QuantityOnHand = 100 (unchanged)
├─ QuantityReserved = 90
└─ CountAvailable = 10

Order 1: ConfirmShipment(50) ← PAYMENT SUCCESS
├─ QuantityOnHand = 50 (deducted!)
├─ QuantityReserved = 40
└─ CountAvailable = 10

Order 2: Release(40) ← CART ABANDONED
├─ QuantityOnHand = 50
├─ QuantityReserved = 0
└─ CountAvailable = 50
```

### Transfer State Machine
```
         ┌────────────┐
         │  Pending   │
         └──────┬─────┘
                │ Initiate()
                │ (Deduct from source)
                ▼
         ┌────────────┐
         │ InTransit  │
         └──────┬─────┘
                │ Receive(quantities)
                │ (Add to destination)
                ▼
         ┌────────────┐
         │  Received  │
         └────────────┘

         Cancel() can happen from:
         - Pending → Cancelled
         - InTransit → Cancelled (with reversal)
```

## 🎬 User Flow Diagrams

### Flow 1: Online Shipping Order
```
┌─ Customer Browses ─┐
└─────────┬──────────┘
          │
          ▼
┌─ Add to Cart ─────────────────────┐
│ Reserve Stock                     │
│ (StockItem.Reserve)               │
└─────────┬────────────────────────┘
          │
          ▼
┌─ Checkout & Payment ──────────────┐
│ Payment successful                │
└─────────┬────────────────────────┘
          │
          ▼
┌─ FulfillOrderCommand ──────────────────────────────┐
│ 1. Find best location (strategy)                  │
│ 2. Create Shipment from location                 │
│ 3. ConfirmShipment (deduct stock)                │
│ 4. Generate shipping label                       │
└─────────┬────────────────────────────────────────┘
          │
          ▼
┌─ Order Ready to Ship ─────────────┐
│ Stock = 100 → 95                  │
└───────────────────────────────────┘
```

### Flow 2: Store Pickup Order
```
┌─ Customer Browses ────────────────┐
└─────────┬────────────────────────┘
          │
          ▼
┌─ Check Availability ──────────────────────┐
│ CheckStockAvailabilityQuery               │
│ → Show nearby stores with quantity       │
└─────────┬────────────────────────────────┘
          │
          ▼
┌─ Customer Selects Store ──────────┐
└─────────┬────────────────────────┘
          │
          ▼
┌─ Add to Cart ──────────────────────┐
│ Reserve at selected location       │
│ (StockItem.Reserve)                │
└─────────┬────────────────────────┘
          │
          ▼
┌─ Checkout & Payment ──────────────┐
│ Payment successful                │
└─────────┬────────────────────────┘
          │
          ▼
┌─ CreateStorePickupCommand ────────────────┐
│ 1. Verify stock at location              │
│ 2. ConfirmShipment (deduct stock)       │
│ 3. Create StorePickup (Pending)         │
│ 4. Generate pickup code (e.g., ABC123)  │
│ 5. Notify store staff                   │
└─────────┬──────────────────────────────┘
          │
          ▼
┌─ Staff Prepares Items ────────────┐
└─────────┬────────────────────────┘
          │
          ▼
┌─ MarkPickupReady ─────────────────┐
│ State: Pending → Ready            │
│ Send SMS to customer              │
│ "Your order is ready! Code: ABC123"
└─────────┬────────────────────────┘
          │
          ▼
┌─ Customer Arrives ────────────────┐
└─────────┬────────────────────────┘
          │
          ▼
┌─ CompletePickup Command ──────────┐
│ 1. Verify pickup code             │
│ 2. State: Ready → PickedUp        │
│ 3. Mark order complete            │
└───────────────────────────────────┘
```

### Flow 3: Split Shipment (Multiple Locations)
```
Customer orders 5 items
District 1 store has 3
Central warehouse has 2

FulfillOrderCommand
├─ Strategy: Nearest → District 1 (3 units)
│  └─ Create Shipment #1 from District 1
│
└─ Remaining 2 units → Central warehouse
   └─ Create Shipment #2 from Central

Result:
├─ Shipment #1: 3 units from District 1 → Customer (ships today)
└─ Shipment #2: 2 units from Central → Customer (ships tomorrow)

Customer receives 2 packages
```

## 🧪 Test Coverage Map

```
UNIT TESTS (No Database)
├─ StockLocation
│  ├─ Location type classification
│  ├─ Distance calculations
│  └─ Validation
├─ StockItem
│  ├─ Reserve / Release / ConfirmShipment logic
│  ├─ Multiple reservations per order
│  ├─ Insufficient stock handling
│  └─ Edge cases (negative qty, invalid state)
├─ StorePickup
│  ├─ State transitions
│  ├─ Pickup code generation
│  ├─ Invalid code rejection
│  └─ Cancellation logic
├─ FulfillmentStrategies
│  ├─ NearestLocationStrategy allocation
│  ├─ HighestStockStrategy selection
│  ├─ CostOptimizedStrategy pricing
│  └─ Fallback/split handling
└─ StockTransfer
   ├─ Initiate deduction logic
   ├─ Receive addition logic
   └─ Cancellation reversal

INTEGRATION TESTS (With Database)
├─ Order Fulfillment
│  ├─ Online shipping end-to-end
│  ├─ Store pickup end-to-end
│  ├─ Mixed order (ship + pickup)
│  └─ Split shipment across locations
├─ Stock Availability
│  ├─ Online availability check
│  ├─ Nearby location filtering
│  ├─ Distance sorting
│  └─ Operating hours display
├─ Stock Transfer
│  ├─ Source location deduction
│  ├─ Destination location addition
│  ├─ Partial transfer handling
│  └─ Reconciliation
└─ Workflows
   ├─ Complete order flow
   ├─ Cancellation & reversal
   ├─ Multiple concurrent orders
   └─ Inventory accuracy after fulfillment

API ENDPOINT TESTS
├─ POST /orders/{id}/fulfill
├─ GET /availability
├─ POST /pickups
├─ PUT /pickups/{id}/ready
├─ PUT /pickups/{id}/complete
├─ POST /transfers
├─ PUT /transfers/{id}/receive
└─ Error scenarios for each
```

## 📈 Complexity Breakdown by Phase

```
PHASE 1: Domain Enhancement
└─ Complexity: LOW
   - Add properties to existing entities
   - Add methods with straightforward logic
   - Update configs
   - Estimated: 2-3 days

PHASE 2: Fulfillment Strategy
└─ Complexity: MEDIUM
   - Create interface + 4 implementations
   - Distance calculation (math)
   - Cost calculation (more complex for CostOptimized)
   - Estimated: 1-2 days

PHASE 3: Store Pickup
└─ Complexity: MEDIUM
   - New aggregate (standard pattern)
   - State machine (4 states)
   - Code generation
   - Domain events
   - Estimated: 1-2 days

PHASE 4: Stock Transfer
└─ Complexity: MEDIUM-HIGH
   - Enhanced aggregate
   - Two-way inventory movement
   - Partial receives
   - Reconciliation
   - Estimated: 1-2 days

PHASE 5: Order Fulfillment
└─ Complexity: HIGH
   - Multiple strategies
   - Split shipment logic
   - Coordinates with many services
   - Error handling across domains
   - Estimated: 2-3 days

PHASE 6: Queries
└─ Complexity: MEDIUM
   - Geographic calculations
   - Efficient filtering
   - Cache considerations
   - Estimated: 1 day

PHASE 7: API
└─ Complexity: LOW-MEDIUM
   - Standard CRUD endpoints
   - Error mapping
   - Request validation
   - Estimated: 1 day

PHASE 8: Testing
└─ Complexity: MEDIUM-HIGH
   - Comprehensive test coverage
   - Edge case handling
   - Performance testing
   - Estimated: 3-4 days

TOTAL ESTIMATED: 4-5 weeks
```

## 🔄 Iteration Strategy

```
Week 1: Implement & Test Phase 1
├─ Commit: "feat: enhance StockLocation with location types"
├─ Commit: "feat: enhance StockItem with reservation methods"
└─ Commit: "test: add unit tests for inventory domain"

Week 2: Implement & Test Phase 2-3
├─ Commit: "feat: implement fulfillment strategy pattern"
├─ Commit: "feat: create store pickup aggregate"
├─ Commit: "test: add tests for strategies and pickups"
└─ Deploy to dev/staging

Week 3: Implement & Test Phase 4-5
├─ Commit: "feat: enhance stock transfer management"
├─ Commit: "feat: implement order fulfillment orchestration"
└─ Commit: "test: add integration tests for fulfillment"

Week 4: Implement & Test Phase 6-7
├─ Commit: "feat: add stock availability queries"
├─ Commit: "feat: add fulfillment API endpoints"
└─ Commit: "test: add API endpoint tests"

Week 5: Final Testing & Documentation
├─ Commit: "test: comprehensive workflow testing"
├─ Commit: "docs: update architecture documentation"
└─ Commit: "release: multi-location fulfillment v1.0"
```

---

**Note:** Times are estimates. Actual timing depends on team size and complexity discovered during implementation.
