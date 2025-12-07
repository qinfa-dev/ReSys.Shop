# Feature Implementation Plan: Unit Tests for Inventories Domain

## 📋 Todo Checklist
- [x] Create test file `StockLocationTests.cs`
- [x] Create test file `StockTransferTests.cs`
- [x] Create test file `StockItemTests.cs`
- [x] Create test file `StockMovementTests.cs`
- [x] Create test file `InventoryUnitTests.cs`
- [x] Create test file `NumberGeneratorTests.cs`
- [ ] Run all tests and ensure they pass

## 🔍 Analysis & Investigation

### Codebase Structure
The domain models for the `Inventories` bounded context are located in `src/ReSys.Core/Domain/Inventories/`. The primary entities identified are:
- `StockLocation`: Manages inventory at a physical location.
- `StockTransfer`: Orchestrates moving stock between locations.
- `StockItem`: Represents the stock of a specific variant at a location.
- `StockMovement`: An audit log for all stock changes.
- `InventoryUnit`: A trackable unit of a variant tied to an order.
- `NumberGenerator`: A utility for creating prefixed sequential numbers.

The unit tests are located in `tests/Core.UnitTests/`. The directory structure of the tests mirrors the source directory structure. Existing tests for `StockLocation` and `StockItem` invariants (`StockLocationInvariantTests.cs` and `StockItemInvariantTests.cs`) provide a clear template for the testing approach.

### Current Architecture
The project uses a clean architecture approach. The domain models are rich with business logic, validation, and domain events. The testing strategy relies on:
- **xUnit:** As the core testing framework.
- **FluentAssertions:** for expressive and readable assertions.
- **NSubstitute:** For creating test doubles (mocks, stubs), although not heavily used in the existing inventory tests, it is available.
- **Factory Methods:** Domain objects are created via static `Create` factory methods, which return an `ErrorOr<T>` type. Tests must check for both successful creation and validation errors.
- **Private Helpers:** Test classes use private helper methods to construct test data (e.g., `CreateTestVariant`, `CreateTestStockItem`), promoting reusability and readability.
- **Reflection:** Is used to bypass encapsulation (private setters) specifically for the purpose of testing invariant validation rules. This is an accepted pattern in this codebase for ensuring the robustness of domain entities.

### Dependencies & Integration Points
- The domain entities have dependencies on each other (e.g., `StockTransfer` depends on `StockLocation` and `StockItem`). Tests will need to construct these dependencies.
- The entities also raise `DomainEvent`s. Tests should assert that the correct events are raised after a state change.

### Considerations & Challenges
- The `NumberGenerator` is a static class with a static field, which will persist its state across tests running in parallel. The tests should be designed to either not rely on a specific generated number or handle the shared state appropriately. Given its simplicity, testing its format is the main goal.
- Some business logic, like in `StockTransfer.Transfer`, has complex interactions between multiple aggregates (`StockLocation`, `StockItem`). These tests will require careful setup of all participating objects.

## 📝 Implementation Plan

### Prerequisites
- Ensure the .NET 9 SDK is installed.
- The solution should build successfully via `dotnet build`.

### Step-by-Step Implementation

1.  **Create `StockLocationTests.cs`**
    -   **File to create**: `tests/Core.UnitTests/Domain/Inventories/Locations/StockLocationTests.cs`
    -   **Changes needed**:
        -   Add tests for the `Create` factory method:
            -   Success case: a valid `StockLocation` is created with correct properties.
            -   Success case: domain event `Created` is raised.
        -   Add tests for the `Update` method for partial and full updates.
        -   Add tests for `MakeDefault`, `Delete`, and `Restore` methods, checking state changes and domain events.
        -   Add tests for `StockItemOrCreate`:
            -   It should return an existing `StockItem`.
            -   It should create a new `StockItem` if one doesn't exist.
        -   Add tests for `Restock` and `Unstock` logic, ensuring they correctly call the underlying `StockItem`.
        -   Add tests for `LinkStore` and `UnlinkStore`, checking for success, failure (e.g., linking an already linked store), and domain events.

2.  **Create `StockTransferTests.cs`**
    -   **File to create**: `tests/Core.UnitTests/Domain/Inventories/Locations/StockTransferTests.cs`
    -   **Changes needed**:
        -   Add tests for the `Create` factory method:
            -   Success case for a transfer (with source and destination).
            -   Success case for a receipt (destination only).
            -   Failure case when source and destination are the same.
            -   Assert that the `Number` is generated.
            -   Assert the `StockTransferCreated` event is raised.
        -   Add tests for the `Transfer` method:
            -   Success case with sufficient stock.
            -   Failure case with insufficient stock (for non-backorderable items).
            -   Failure case with invalid quantity (e.g., zero or negative).
            -   Assert that `Unstock` on source and `Restock` on destination are called.
        -   Add tests for the `Receive` method for supplier receipts.

3.  **Create `StockItemTests.cs`**
    -   **File to create**: `tests/Core.UnitTests/Domain/Inventories/Stocks/StockItemTests.cs`
    -   **Changes needed**:
        -   Add tests for the `Create` factory method for success and failure (e.g., negative quantity).
        -   Add tests for the `Adjust` method for positive and negative adjustments and `StockAdjusted` event.
        -   Add tests for `Reserve`, `Release`, and `ConfirmShipment`, checking for correct changes to `QuantityOnHand` and `QuantityReserved` and corresponding domain events.
        -   Test backorderable vs. non-backorderable behavior for reservations.
        -   Add tests for `ProcessBackorders`, ensuring it fills backordered `InventoryUnit`s when stock is added.

4.  **Create `StockMovementTests.cs`**
    -   **File to create**: `tests/Core.UnitTests/Domain/Inventories/Stocks/StockMovementTests.cs`
    -   **Changes needed**:
        -   Add tests for the `Create` factory method:
            -   Success case with valid quantity.
            -   Failure case when quantity is zero.
            -   Assert properties are set correctly.

5.  **Create `InventoryUnitTests.cs`**
    -   **File to create**: `tests/Core.UnitTests/Domain/Inventories/Stocks/InventoryUnitTests.cs`
    -   **Changes needed**:
        -   Add tests for the `CreateForLineItem` factory method, ensuring it creates the correct number of units.
        -   Test the state machine transitions: `FillBackorder`, `Ship`, `Return`, and `Cancel`.
        -   Test invalid state transitions (e.g., trying to ship a backordered unit).
        -   Assert that the correct domain events are raised for each state change.
        -   Test `SetStockLocation`.

6.  **Create `NumberGeneratorTests.cs`**
    -   **File to create**: `tests/Core.UnitTests/Domain/Inventories/NumberGeneratorTests.cs`
    -   **Changes needed**:
        -   Add a test to verify that `Generate` returns a string with the correct prefix.
        -   Add a test to verify that subsequent calls generate different (incrementing) numbers. Note: This test may need to be run serially if xUnit parallelization causes issues.

### Testing Strategy
-   **Isolation**: Each test should be isolated. Use `NSubstitute` if you need to mock dependencies to avoid relying on the concrete implementation of other classes, though for domain model testing, using the real objects is often preferred if they are not expensive to create. The existing tests favor using real objects.
-   **Arrange, Act, Assert**: Follow this pattern clearly in all tests.
-   **Data Builders/Helpers**: Continue using and expanding the private helper methods to create test data.
-   **Run Tests**: After implementing the tests, run `dotnet test` from the root of the repository to ensure all new and existing tests pass.

## 🎯 Success Criteria
- All domain models in the `ReSys.Core/Domain/Inventories` namespace have corresponding unit test classes in `tests/Core.UnitTests/Domain/Inventories`.
- The tests provide comprehensive coverage of the factory methods, business logic methods, state transitions, and validation rules.
- All tests pass when `dotnet test` is executed.
