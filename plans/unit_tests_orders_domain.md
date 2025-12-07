# Feature Implementation Plan: Unit Tests for Orders Domain

## 📋 Todo Checklist
- [x] Create test file for `Order` entity.
   - **Implementation Notes**: Created `tests/Core.UnitTests/Domain/Orders/OrderTests.cs` by copying the content from `OrderInvariantTests.cs` and renaming the class. The original `OrderInvariantTests.cs` still exists and will be preserved.
   - **Status**: ✅ Completed
- [x] Implement `Order` factory and state transition tests.
   - **Implementation Notes**: Added tests for `Order.Create` factory method and all state transitions (`Next()` method, including `Cart -> Address`, `Address -> Delivery`, `Delivery -> Payment`, `Payment -> Confirm`, `Confirm -> Complete`) and `Cancel()` method to `OrderTests.cs`.
   - **Status**: ✅ Completed
- [x] Implement `Order` business logic tests (line items, promotions, etc.).
   - **Implementation Notes**: Added tests for Line Item Management (`AddLineItem`, `RemoveLineItem`, `UpdateLineItemQuantity`), Promotion Management (`ApplyPromotion`, `RemovePromotion`), and other business logic (`SetShippingAddress`, `SetBillingAddress`, `SetShippingMethod`, `AddPayment`, `SetFulfillmentLocation`) to `OrderTests.cs`.
   - **Status**: ✅ Completed
- [x] Create test file for `LineItem` entity and implement tests.
   - **Implementation Notes**: Created `tests/Core.UnitTests/Domain/Orders/LineItems/LineItemTests.cs` and added tests for `Create` factory method (valid/invalid inputs, variant not priced), `UpdateQuantity` method (valid/invalid quantities), and Computed Properties (`SubtotalCents`, `TotalCents`) with and without adjustments.
   - **Status**: ✅ Completed
- [x] Create test files for `OrderAdjustment` and `LineItemAdjustment` and implement tests.
   - **Implementation Notes**: Created `tests/Core.UnitTests/Domain/Orders/Adjustments/AdjustmentTests.cs` and added tests for `OrderAdjustment.Create` (valid/invalid description, with/without promotion) and `LineItemAdjustment.Create` (valid/invalid description, with/without promotion). Also tested the `IsPromotion` computed property for both.
   - **Status**: ✅ Completed
- [x] Create test file for `Payment` entity and implement tests.
   - **Implementation Notes**: Created `tests/Core.UnitTests/Domain/Orders/Payments/PaymentTests.cs` and added tests for `Payment.Create` factory method (valid/invalid inputs), state transition methods (`StartProcessing`, `Capture`, `Void`, `Refund`, `MarkAsFailed`) including valid and invalid transitions, and Computed Properties.
   - **Status**: ✅ Completed
- [x] Create test file for `Shipment` entity and implement tests.
   - **Implementation Notes**: Created `tests/Core.UnitTests/Domain/Orders/Shipments/ShipmentTests.cs` and added tests for `Shipment.Create` factory method (valid/invalid inputs), `AssignStockLocation` method (valid/invalid scenarios), state transition methods (`Ready`, `Ship`, `Deliver`, `Cancel`) including valid and invalid transitions and prerequisites, and `UpdateTrackingNumber` method. Also tested Computed Properties.
   - **Status**: ✅ Completed
- [x] Update `ReturnItem` tests to cover more than just invariants.
   - **Implementation Notes**: Renamed `tests/Core.UnitTests/Domain/Orders/Returns/ReturnItemInvariantTests.cs` to `ReturnItemTests.cs` and updated the class name. Added comprehensive tests for `ReturnItem` including `Create` and `FromInventoryUnit` factory methods, Reception Status transitions (`Receive`, `GiveToCustomer`, `Cancel`), Acceptance Status transitions (`AttemptAccept`, `Accept`, `Reject`, `RequireManualIntervention`), Exchange Handling (`SetExchangeVariant`), Reimbursement (`AssociateReimbursement`), and Inventory Processing (`ProcessInventoryUnit`).
   - **Status**: ✅ Completed
- [x] Final Review and Testing of all new unit tests.

## 🔍 Analysis & Investigation

### Codebase Structure
The `src/ReSys.Core/Domain/Orders/` directory contains the core domain logic for the "Orders" bounded context. It follows a clear aggregate pattern with `Order.cs` as the aggregate root. Child entities and owned entities are organized into subdirectories:
- `Order.cs`: The aggregate root, managing the overall state and lifecycle.
- `LineItems/LineItem.cs`: Represents a product added to the order, capturing price and quantity.
- `Adjustments/OrderAdjustment.cs` & `LineItemAdjustment.cs`: Handle financial adjustments (discounts, taxes) at both order and line item levels.
- `Payments/Payment.cs`: Manages the payment lifecycle with its own state machine.
- `Shipments/Shipment.cs`: Manages the fulfillment lifecycle with its own state machine.
- `Returns/ReturnItem.cs`: A complex entity for managing product returns, with two separate state machines (`ReceptionStatus` and `AcceptanceStatus`).

The corresponding test project is `tests/Core.UnitTests/`. Existing tests in `tests/Core.UnitTests/Domain/Orders/` (e.g., `OrderInvariantTests.cs`, `ReturnItemInvariantTests.cs`) show a well-established pattern for testing domain entities.

### Current Architecture
- **Domain-Driven Design (DDD):** The code heavily employs DDD principles, including Aggregates, Entities, Value Objects, and Domain Events. The `Order` is a clear aggregate root.
- **Error Handling:** The `ErrorOr` library is used for railway-oriented programming, ensuring that all operations that can fail return a result object instead of throwing exceptions.
- **Testing Stack:**
    - **xUnit:** The primary testing framework.
    - **FluentAssertions:** Used for expressive and readable assertions (e.g., `result.Should().BeTrue()`).
    - **NSubstitute:** Used for creating test doubles (mocks, stubs). The current pattern favors using real domain objects for setup and `NSubstitute` for abstract dependencies (which are less common in these pure domain tests).
- **Test Conventions:** Tests are structured using the Arrange-Act-Assert pattern. Helper methods are used extensively to construct valid test data, reducing boilerplate in test methods. Test files are named `[EntityName]Tests.cs`.

### Dependencies & Integration Points
The domain entities are self-contained. The tests will primarily depend on other domain entities within the `ReSys.Core` project (e.g., an `Order` test needs `Variant`, `Product`, etc.). There are no external dependencies like databases or APIs for these unit tests.

### Considerations & Challenges
- **Rich Domain Model:** The `Order` aggregate is large and complex, with many business rules and state transitions. This requires a large number of tests to achieve good coverage.
- **Test Data Setup:** Creating valid test data is critical. The existing helper method pattern (`CreateTestVariant`, `CreateShippedInventoryUnit`, etc.) should be continued and expanded. This ensures that tests are readable and focused on the behavior being tested, not the setup.
- **State Machines:** `Order`, `Payment`, `Shipment`, and `ReturnItem` all have state machines. Each state transition must be tested for both valid and invalid paths.
- **`ErrorOr`:** Every test for a method that returns `ErrorOr` must check both the success path (`IsError.Should().BeFalse()`) and failure paths (checking for specific `FirstError.Code`).

## 📝 Implementation Plan

### Prerequisites
- Ensure the .NET 9 SDK is installed and the project builds successfully.
- Familiarize with `xUnit`, `FluentAssertions`, and `NSubstitute` syntax.

### Step-by-Step Implementation

1. **Create `OrderTests.cs`**
   - **File to create:** `tests/Core.UnitTests/Domain/Orders/OrderTests.cs`
   - **Changes needed:** Create a new test class to house all tests for the `Order` aggregate root. Copy the helper methods (`CreateTestVariant`, `CreateTestOrder`, etc.) from `OrderInvariantTests.cs` to get started.
   - **Tests to implement:**
     - **`Create` Factory:**
       - Test that `Create` succeeds with valid inputs.
       - Test that a unique `Number` is generated.
       - Test that the initial state is `Cart`.
     - **State Transitions (`Next()`):**
       - `Cart -> Address`: Test that it succeeds with items and fails with an empty cart.
       - `Address -> Delivery`: Test for physical orders (succeeds with addresses, fails without) and digital orders (succeeds without addresses).
       - `Delivery -> Payment`: Test for physical orders (succeeds with shipping method, fails without) and that a `Shipment` is created.
       - `Payment -> Confirm`: Test that it succeeds with sufficient payment and fails with insufficient or failed payments.
       - `Confirm -> Complete`: Test that it succeeds with completed payments, fails otherwise. Test that `CompletedAt` is set and `FinalizeInventory` event is raised.
     - **`Cancel()`:**
       - Test that it succeeds for a non-completed order.
       - Test that it fails for a completed order.
       - Test that `CanceledAt` is set and `ReleaseInventory` event is raised.
     - **Line Item Management:**
       - `AddLineItem`: Test adding a new item, adding an existing item (should update quantity), adding an unpurchasable item, and adding an item when the order is not in the `Cart` state.
       - `RemoveLineItem`: Test removing an existing and non-existent item.
       - `UpdateLineItemQuantity`: Test valid and invalid quantities.
     - **Promotion Management:**
       - `ApplyPromotion`: Test applying a valid promotion (with and without a code). Test applying a second, different promotion (should fail). Test that totals are recalculated correctly.
       - `RemovePromotion`: Test that adjustments are removed and totals are recalculated.
     - **Other Business Logic:** Test `SetShippingAddress`, `SetBillingAddress`, `SetShippingMethod`, `AddPayment`, and `SetFulfillmentLocation`.

2. **Create `LineItemTests.cs`**
   - **File to create:** `tests/Core.UnitTests/Domain/Orders/LineItems/LineItemTests.cs`
   - **Changes needed:** Add tests for the `LineItem` entity.
   - **Tests to implement:**
     - **`Create` Factory:** Test success case, invalid quantity, null variant, and variant not priced in the given currency.
     - **`UpdateQuantity`:** Test valid and invalid (<1) quantities.
     - **Computed Properties:** Test `SubtotalCents` and `TotalCents` calculations, especially when `LineItemAdjustment`s are present.

3. **Create `AdjustmentTests.cs`**
   - **File to create:** `tests/Core.UnitTests/Domain/Orders/Adjustments/AdjustmentTests.cs`
   - **Changes needed:** Add tests for both `OrderAdjustment` and `LineItemAdjustment`.
   - **Tests to implement:**
     - `OrderAdjustment.Create`: Test valid cases (positive/negative amounts, with/without promotion) and failure cases (null/long description).
     - `LineItemAdjustment.Create`: Test valid cases (positive/negative amounts, with/without promotion) and failure cases (null/long description).
     - `IsPromotion` property on both classes.

4. **Create `PaymentTests.cs`**
   - **File to create:** `tests/Core.UnitTests/Domain/Orders/Payments/PaymentTests.cs`
   - **Changes needed:** Add tests for the `Payment` entity's state machine.
   - **Tests to implement:**
     - **`Create` Factory:** Test success and failure cases.
     - **State Transitions:** For each method (`Capture`, `Void`, `Refund`, `MarkAsFailed`, `StartProcessing`), test the valid transition and at least one invalid transition (e.g., `Void` a `Completed` payment).
     - **Input Validation:** Test that `Capture` requires a `transactionId`.

5. **Create `ShipmentTests.cs`**
   - **File to create:** `tests/Core.UnitTests/Domain/Orders/Shipments/ShipmentTests.cs`
   - **Changes needed:** Add tests for the `Shipment` entity's state machine.
   - **Tests to implement:**
     - **`Create` Factory:** Test success and failure cases.
     - **`AssignStockLocation`:** Test success in `Pending` state and failure in other states.
     - **State Transitions:** For each method (`Ready`, `Ship`, `Deliver`, `Cancel`), test valid and invalid transitions.
     - **Prerequisites:** Test that `Ready` and `Ship` fail if no `StockLocationId` is set.
     - **`UpdateTrackingNumber`:** Test valid and invalid tracking numbers.

6. **Enhance `ReturnItemTests.cs`**
   - **File to modify:** `tests/Core.UnitTests/Domain/Orders/Returns/ReturnItemInvariantTests.cs`
   - **Changes needed:** Rename the file to `ReturnItemTests.cs` and expand it beyond just invariant tests.
   - **Tests to implement:**
     - **Factories:** Test `Create` and `FromInventoryUnit` (for eligible, ineligible, and already-returned units).
     - **Reception Status:** Test `Receive`, `GiveToCustomer`, and `Cancel` transitions, including failure cases.
     - **Acceptance Status:** Test `AttemptAccept` logic (return window, shipped state), and manual `Accept`/`Reject` methods.
     - **Exchange Logic:** Test `SetExchangeVariant` with valid/invalid variants and check that `PreTaxAmountCents` is zeroed out.
     - **`ProcessInventoryUnit`:** Test that the `InventoryUnit` state is changed and the correct event is raised.

### Testing Strategy
- **Isolation:** Each test should be isolated. Use the helper methods to create fresh instances of domain entities for each test to avoid side effects between tests.
- **Coverage:** Aim to test every public method on each entity. For methods with conditional logic, test each path (e.g., `if`/`else` branches). For methods returning `ErrorOr`, test at least one success case and all defined error cases.
- **Assertions:** Use `FluentAssertions` for all assertions to ensure tests are readable.
- **Running Tests:** Use the `dotnet test` command from the root of the repository to run all tests and ensure no regressions are introduced.

## 🎯 Success Criteria
- A new test file exists for each major entity in the `src/ReSys.Core/Domain/Orders` directory (`Order`, `LineItem`, `Payment`, `Shipment`, and a combined `Adjustment` test file).
- The existing `ReturnItemInvariantTests.cs` is expanded to cover the full business logic of the `ReturnItem` entity.
- The new tests provide comprehensive coverage of the factory methods, business logic, state transitions, and error conditions for each entity.
- All new and existing tests pass when `dotnet test` is executed.
