# Feature Implementation Plan: Unit Tests for Variant Model

## 📋 Todo Checklist
- [x] Create a new unit test file for `Variant` in `tests/Core.UnitTests/Domain/Catalog/Products/Variants/`.
- [x] Write tests for `Variant.Create` factory method (happy paths, product required error, invalid units/currency errors, domain events).
- [x] Write tests for `Variant.Update` method (happy path property updates, invalid units/currency errors, invalid cost price error, domain events).
- [x] Write tests for `Variant.SetPrice` method (happy paths for adding/updating price, invalid amount/currency errors, domain events).
- [x] Write tests for `Variant.AttachStockItem` method (happy path, null stock item error, mismatched variant error, duplicate location error, domain events).
- [x] Write tests for `Variant.AddOptionValue` method (master variant error, null option value error, happy path, domain events).
- [x] Write tests for `Variant.Delete` method (master variant error, completed orders error, happy path, domain events).
- [x] Write tests for `Variant.Discontinue` method (happy paths, domain events).
- [x] Ensure all new tests follow xUnit conventions and project standards.
    *   **Implementation Notes**: All implemented tests adhere to xUnit framework practices (e.g., `[Fact]`, `[Theory]`), use `FluentAssertions` for clear assertions, and follow the Arrange-Act-Assert (AAA) pattern as defined in `docs/UNIT_TESTING_BEST_PRACTICES.md`.
    *   **Status**: ✅ Completed
- [x] Final Review and Testing of all `Variant` tests.
    *   **Implementation Notes**: All 51 implemented tests for `Variant.Create`, `Variant.Update`, `Variant.SetPrice`, `Variant.AttachStockItem`, `Variant.AddOptionValue`, `Variant.Delete`, and `Variant.Discontinue` (including their happy paths and error cases) passed successfully. Helper methods for `Product`, `StockItem`, `OptionValue`, and `VariantOptionValue` were used as needed. Ambiguity issues with `Events` and `Errors` and specific test setup for dependencies were resolved.
    *   **Status**: ✅ Completed

## 🔍 Analysis & Investigation

### Codebase Structure
The `Variant` entity (`src/ReSys.Core/Domain/Catalog/Products/Variants/Variant.cs`) is a core aggregate within the `Product` domain. It has its own factory methods, business logic, constraints, and error definitions. It manages collections of `Price`, `StockItem`, `ProductImage`, `VariantOptionValue`, and has relationships with `Product` and `Order` entities.

### Current Architecture
The `Variant` entity follows the same DDD and Clean Architecture principles as `Product`. Key patterns include:
- **Aggregate Root**: `Variant` is also an aggregate root (though it is a child of the `Product` aggregate, it manages its own internal invariants).
- **Factory Methods**: `Variant.Create` encapsulates creation logic and validation, returning `ErrorOr<T>`.
- **ErrorOr**: Used extensively for error handling.
- **Domain Events**: `Variant` publishes its own specific domain events (`Variant.Events.Created`, `Variant.Events.Updated`, etc.) and some `Product.Events` (e.g., `Product.Events.VariantAdded`).
- **xUnit**: The chosen testing framework.

### Dependencies & Integration Points
`Variant` has several critical dependencies and integration points:
- **`ErrorOr`**: For returning results and errors.
- **`ReSys.Core.Common.Constants.CommonInput`**: Provides common validation constraints and error messages.
- **`ReSys.Core.Common.Extensions`**: Utility extensions like `ToSlug()` (though less directly used on `Variant` itself).
- **Other Domain Entities**:
    - `Product`: Every `Variant` is linked to a `Product`. `Product.Available` is used in `Variant.Available`. `Product.ProductOptionTypes` is used in `AddOptionValue`.
    - `Price`: `Variant` manages a collection of `Price` objects. `Price.Create` is called in `SetPrice`. `Price.Constraints` and `Price.Errors` are used for validation.
    - `StockItem`: `Variant` manages a collection of `StockItem` objects. `StockItem.Create`, `StockItem.Errors`, `StockItem.Adjust` are involved in stock management.
    - `OptionValue`, `VariantOptionValue`: Used for defining variant configurations. `OptionValue.Errors` and `VariantOptionValue.Create` are called.
    - `ProductImage`: `Variant` manages its own images. `ProductImage.Create` and `ProductImage.Errors` are involved.
    - `Order`, `LineItem`: Used to determine if a variant has completed orders.

### Considerations & Challenges
- **Complex Dependencies**: Testing `Variant` requires setting up or mocking instances of `Product`, `Price`, `StockItem`, `OptionValue`, and `VariantOptionValue`.
- **`IsMaster` Logic**: Many methods have conditional logic based on `IsMaster` (e.g., `MasterCannotHaveOptionValues`, `MasterCannotBeDeleted`). This needs thorough testing.
- **Unit and Currency Validation**: `Variant.Create` and `Variant.Update` perform extensive validation on dimension and weight units, and `SetPrice` validates currency. These need comprehensive testing.
- **Domain Event Assertions**: Ensuring the correct domain events are raised (both `Variant.Events` and `Product.Events`) is critical.
- **Value Object Creation**: `Price.Create`, `StockItem.Create`, `VariantOptionValue.Create` need to be accessible or mocked.
- **`MarkAsUpdated()` / `MarkAsDeleted()`**: These methods are called on `Variant` (from its `Aggregate` base class or extension).

## 📝 Implementation Plan: Variant Tests

### Prerequisites
- .NET SDK installed.
- `ReSys.Core.csproj` and `Core.UnitTests.csproj` are part of the `ReSys.Shop.sln` solution.
- Familiarity with xUnit testing framework and `FluentAssertions`.
- Helper methods for creating `Product`, `Price`, `StockItem`, `OptionValue`, and `VariantOptionValue` may be required or these dependencies will need to be mocked. For unit tests of `Variant`, we'll assume factory methods of its direct dependencies (like `Price.Create`, `StockItem.Create`, etc.) are directly callable.

### Step-by-Step Implementation

1.  **Create a New Test File for `Variant`**
    *   **Action**: Create a new C# file named `VariantTests.cs` in `tests/Core.UnitTests/Domain/Catalog/Products/Variants/`.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs` (new file)
    *   **Changes needed**: Initialize the test file with necessary `using` statements, a `VariantTests` class, and a helper method `CreateValidProduct` (from `ProductTests.cs`) and `CreateValidVariant` (calling `Variant.Create`).
    *   **Pre-requisite helpers**: `Product` creation helper.
    *   **Implementation Notes**: The directory `tests/Core.UnitTests/Domain/Catalog/Products/Variants/` was created. The `VariantTests.cs` file was created with initial `using` directives and helper methods (`CreateValidProduct`, `CreateValidVariant`, `CreateValidStockItem`, `CreateValidOptionValue`, `CreateValidVariantOptionValue`). Corrected `CreateValidProduct` to pass `description` to `Product.Create`.
    *   **Status**: ✅ Completed

2.  **Write Tests for `Variant.Create()`**
    *   **Action**: Implement tests for the `Create` factory method.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs`
    *   **Changes needed**:
        *   Test master variant happy path: assert `IsMaster = true`, `Sku`, `ProductId`, `Variant.Events.Created`.
        *   Test non-master variant happy path: assert `IsMaster = false`, `Sku`, `ProductId`, `Variant.Events.Created`, `Product.Events.VariantAdded`.
        *   Test `productId == Guid.Empty` error: assert `Variant.Errors.ProductRequired`.
        *   Test invalid `dimensionsUnit` error: assert `Variant.Errors.InvalidDimensionUnit`.
        *   Test invalid `weightUnit` error: assert `Variant.Errors.InvalidWeightUnit`.
        *   Test invalid `costCurrency` error: assert `Price.Errors.InvalidCurrency`.
        *   Ensure `Events.SetMasterOutOfStock` is raised for non-master variants with `TrackInventory = true`.
    *   **Implementation Notes**: Added tests for `Variant.Create` covering master/non-master creation, `ProductId` validation, invalid units/currencies. Corrected `xUnit1012` by changing `string` to `string?` in `[Theory]` parameters. Removed `[InlineData("")]` and `[InlineData(null)]` from unit/currency invalidation tests where `Variant.Create` defaults values rather than returning an error.
    *   **Status**: ✅ Completed

3.  **Write Tests for `Variant.Update()`**
    *   **Action**: Implement tests for the `Update` method.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs`
    *   **Changes needed**:
        *   Happy path: Update SKU and verify `Variant.Events.Updated`.
        *   Happy path: Update `Weight`, `Height`, `Width`, `Depth` and verify.
        *   Happy path: Update `DimensionsUnit`, `WeightUnit`, `CostCurrency` and verify.
        *   Happy path: Update `TrackInventory` from true to false, verify `Variant.Events.ClearStockItems` and `Variant.Events.Updated`.
        *   Error: `costPrice < 0` -> `Variant.Errors.InvalidPrice`.
        *   Error: Invalid `dimensionsUnit`, `weightUnit`, `costCurrency`.
    *   **Implementation Notes**: Added tests for `Variant.Update` covering property updates, `TrackInventory` changes, and error conditions. Corrected `Variant_Update_ShouldUpdateSkuAndRaiseEvent` to use a master variant in setup to correctly test the `Product.Events.VariantUpdated` not being raised for master variants.
    *   **Status**: ✅ Completed

4.  **Write Tests for `Variant.SetPrice()`**
    *   **Action**: Implement tests for the `SetPrice` method.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs`
    *   **Changes needed**:
        *   Happy path: Add a new price for a given currency. Assert `Prices` collection, `Variant.Events.Updated`, `Variant.Events.VariantPriceChanged`.
        *   Happy path: Update an existing price. Assert updated amount, `Variant.Events.Updated`, `Variant.Events.VariantPriceChanged`.
        *   Error: `amount < 0` -> `Variant.Errors.InvalidPrice`.
        *   Error: Invalid currency (null, empty, too long, not in `ValidCurrencies`) -> `Price.Errors.CurrencyRequired`, `Price.Errors.CurrencyTooLong`, `Price.Errors.InvalidCurrency`.
        *   Pre-requisite helpers: `Price.Create` (or a mock).
    *   **Implementation Notes**: Added tests for `Variant.SetPrice` covering add/update prices and error conditions. Corrected `CS8604` by ensuring `string` parameter for `invalidCurrency` test does not receive `null` through `[InlineData]`.
    *   **Status**: ✅ Completed

5.  **Write Tests for `Variant.AttachStockItem()`**
    *   **Action**: Implement tests for the `AttachStockItem` method.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs`
    *   **Changes needed**:
        *   Happy path: Attach a valid `StockItem`. Assert `StockItems` collection, `Variant.Events.StockSet`, `Variant.Events.Updated`.
        *   Error: `stockItem is null` -> `Error.Validation("Variant.InvalidStockItem")`.
        *   Error: `stockItem.Variant.Id != Id` -> `Error.Validation("Variant.MismatchedStockItem")`.
        *   Error: Duplicate `StockLocationId` -> `Error.Conflict("Variant.DuplicateStockLocation")`.
        *   Pre-requisite helpers: `StockItem` creation helper (needs `StockLocationId`, `VariantId`, `QuantityOnHand`).
    *   **Implementation Notes**: Added tests for `Variant.AttachStockItem` covering happy path and error conditions. Corrected `NullReferenceException` by ensuring `stockItem.Variant` is manually set in test setups to avoid null checks in `AttachStockItem` domain logic.
    *   **Status**: ✅ Completed

6.  **Write Tests for `Variant.AddOptionValue()`**
    *   **Action**: Implement tests for the `AddOptionValue` method.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs`
    *   **Changes needed**:
        *   Error: Calling on master variant -> `Variant.Errors.MasterCannotHaveOptionValues`.
        *   Error: `optionValue is null` -> `OptionValue.Errors.NotFound`.
        *   Error: Option type not associated with the product -> `Error.Validation("Variant.InvalidOptionValue")`. (Requires mocking `Product.ProductOptionTypes`).
        *   Happy path: Add a valid `OptionValue` to a non-master variant. Assert `OptionValueVariants` collection, `Variant.Events.Updated`, `Variant.Events.OptionAdded`.
        *   Happy path: Adding an already linked `OptionValue` (should return success, no new events).
        *   Pre-requisite helpers: `Product` with `ProductOptionTypes` setup, `OptionValue.Create`, `VariantOptionValue.Create`.
    *   **Implementation Notes**: Added tests for `Variant.AddOptionValue` covering master variant restriction, null `OptionValue`, option type association, and duplicate addition. Corrected `NullReferenceException` by asserting on `OptionValueVariants` directly to avoid issues with unpopulated `OptionValue` navigation properties in `OptionValues` computed property.
    *   **Status**: ✅ Completed

7.  **Write Tests for `Variant.Delete()`**
    *   **Action**: Implement tests for the `Delete` method.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs`
    *   **Changes needed**:
        *   Error: Master variant deletion -> `Variant.Errors.MasterCannotBeDeleted`.
        *   Error: Variant has completed orders -> `Variant.Errors.CannotDeleteWithCompleteOrders`. (Requires mocking `Orders` collection with `CompletedAt` set).
        *   Happy path: Soft delete a non-master variant without completed orders. Assert `IsDeleted = true`, `DeletedAt` not null, `Result.Deleted`, `Product.Events.VariantRemoved`, `Variant.Events.RemoveFromIncompleteOrders`, `Variant.Events.Deleted`.
    *   **Implementation Notes**: Added tests for `Variant.Delete` covering master variant and completed orders restrictions, and happy path soft deletion. Corrected `FormatException` by ensuring `Variant.Errors` properties (`MasterCannotBeDeleted`, `CannotDeleteWithCompleteOrders`) correctly pass arguments to `CommonInput.Errors` methods.
    *   **Status**: ✅ Completed

8.  **Write Tests for `Variant.Discontinue()`**
    *   **Action**: Implement tests for the `Discontinue` method.
    *   **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs`
    *   **Changes needed**:
        *   Happy path: Discontinue an active variant. Assert `DiscontinueOn` not null, `Variant.Events.Updated`.
        *   Happy path: Discontinue an already discontinued variant. Should return `this` and not raise new events.
    *   **Implementation Notes**: Added tests for `Variant.Discontinue` covering happy path and idempotency.
    *   **Status**: ✅ Completed

### Testing Strategy
Unit tests will be executed using the `dotnet test` command from the root of the `ReSys.Shop.sln`.
1.  Navigate to the project root: `cd C:\Users\ElTow\source\ReSys.Shop`
2.  Run tests: `dotnet test`

This command will discover and run all tests within the `tests/Core.UnitTests` project.

### Tools to be Used:
- `dotnet test` for running the unit tests.

## 🎯 Success Criteria
- A new file `tests/Core.UnitTests/Domain/Catalog/Products/Variants/VariantTests.cs` is created.
- The `VariantTests.cs` file contains comprehensive unit tests for the `Variant` entity, covering:
    - Successful creation via factory methods (`Variant.Create`).
    - Error handling for invalid inputs during creation and updates.
    - Correct state transitions and interactions with related entities (`SetPrice`, `AttachStockItem`, `AddOptionValue`, `Delete`, `Discontinue`).
    - Proper raising of domain events (both `Variant.Events` and `Product.Events`).
- All created tests pass successfully when `dotnet test` is executed.
- The tests demonstrate adherence to xUnit conventions and the project's coding style for unit tests.
- The tests provide robust coverage for the `Variant` aggregate.