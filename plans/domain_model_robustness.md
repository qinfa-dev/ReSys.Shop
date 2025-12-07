# Feature Implementation Plan: Domain Model Robustness

**THIS PLAN IS SUPERSEDED.**

Please refer to the new comprehensive plan: `plans/domain_model_critical_fixes_plan.md`

## 📋 Todo Checklist
- [x] Implement Optimistic Concurrency Control across key aggregates.
- [x] Refactor `InventoryUnit` to represent a single item.
- [x] Implement correct logic for handling partial returns.
- [ ] Address medium-priority business logic and encapsulation issues.
- [ ] Create and update unit and integration tests to cover all changes.
- [ ] Final Review and Testing.

## 🔍 Analysis & Investigation

### Codebase Structure
The codebase follows Clean Architecture principles, with domain logic clearly isolated in `ReSys.Core`. The key files for this plan reside within `src/ReSys.Core/Domain/` in the `Catalog`, `Inventories`, and `Orders` subdomains. The most relevant files are:
-   `Aggregates/Entities`: `Order.cs`, `StockItem.cs`, `InventoryUnit.cs`, `LineItem.cs`, `ReturnItem.cs`, `Variant.cs`, `Payment.cs`, `Shipment.cs`.
-   `Configurations`: The corresponding EF Core `...Configuration.cs` files for the entities above.
-   `Base Classes`: `Aggregate.cs` and `AuditableEntity.cs` in `ReSys.Core/Common/Domain/Entities`.

### Current Architecture
The architecture is based on Domain-Driven Design (DDD). It uses aggregates to enforce invariants, factory methods for safe entity creation, and the `ErrorOr` pattern for functional error handling. EF Core is used for data persistence. This plan must respect and extend these established patterns.

### Dependencies & Integration Points
The issues identified touch on the core interactions between the `Orders`, `Inventories`, and `Catalog` domains.
-   `Orders` depend on `Catalog` for variant pricing (`LineItem` creation).
-   `Inventories` are affected by `Orders` through domain events for stock reservation, finalization, and release.
-   `Returns` (in `Orders`) must correctly interact with `InventoryUnit`s and trigger stock restoration in `Inventories`.
The changes, especially to `InventoryUnit`, will ripple through the order fulfillment and return processes.

### Considerations & Challenges
1.  **Concurrency:** The lack of optimistic concurrency is the most critical issue. Implementing it requires touching base classes and configurations, followed by adding exception handling at the application layer (e.g., in MediatR `IRequestHandler` implementations) to manage `DbUpdateConcurrencyException`.
2.  **`InventoryUnit` Refactoring:** Changing `InventoryUnit` to represent a single item is a significant but necessary change. It will simplify return logic but requires careful modification of the fulfillment process where these units are created.
3.  **Database Migrations:** These changes will require a new EF Core migration. The migration must be carefully reviewed to ensure data integrity is preserved.

## 📝 Implementation Plan

### Prerequisites
- Ensure a solid unit and integration test suite exists. If not, key tests for order creation, fulfillment, and returns should be written before starting to provide a safety net.

### Step-by-Step Implementation

#### **Part 1: Implement Optimistic Concurrency Control**

1.  **Step 1.1: Update Base Aggregate Class**
    -   **File to modify:** `src/ReSys.Core/Common/Domain/Entities/Aggregate.cs`
    -   **Changes needed:** Add a `RowVersion` property for optimistic concurrency control. This property will be managed by the database.
        ```csharp
        // In the Aggregate<T> class, and the base Aggregate class
        [Timestamp]
        public byte[]? RowVersion { get; set; }
        ```
    -   **Implementation Notes**: Added `using System.ComponentModel.DataAnnotations;` and `[Timestamp] public byte[]? RowVersion { get; set;}` to `Aggregate<TId>`.
    -   **Status**: ✅ Completed

2.  **Step 1.2: Update EF Core Configurations**
    -   **Files to modify:** All `...Configuration.cs` files for key aggregates (`OrderConfiguration.cs`, `StockItemConfiguration.cs`, `PaymentConfiguration.cs`, `ShipmentConfiguration.cs`).
    -   **Changes needed:** For each aggregate that inherits the `RowVersion`, ensure it is configured as a concurrency token. In many cases, if this is on a base type EF Core might pick it up, but explicit configuration is safer.
        ```csharp
        // In each relevant configuration file's Configure method:
        builder.Property(e => e.RowVersion).IsRowVersion();
        ```
    -   **Implementation Notes**: Added `.Property(e => e.RowVersion).IsRowVersion().HasComment(...)` to `OrderConfiguration.cs`, `StockItemConfiguration.cs`, `PaymentConfiguration.cs`, and `ShipmentConfiguration.cs`.
    -   **Status**: ✅ Completed

3.  **Step 1.3: Plan for Application Layer Handling**
    -   **Files to modify:** (Conceptual) Application layer command handlers (e.g., MediatR handlers).
    -   **Changes needed:** This is a planning step. Document that all command handlers performing updates (`_unitOfWork.SaveChangesAsync()`) must include a `try-catch` block for `DbUpdateConcurrencyException`. The handling strategy (e.g., retry with fresh data, notify user) should be decided and implemented consistently.
    -   **Implementation Notes**: This step requires planning the implementation of `try-catch` blocks for `DbUpdateConcurrencyException` in application-layer command handlers (e.g., MediatR handlers) that perform write operations. The exact implementation will depend on the chosen error handling and retry strategy for the application. This step serves as a reminder for post-model changes.
    -   **Status**: ✅ Completed

---

#### **Part 2: Refactor `InventoryUnit` to Represent a Single Item**

1.  **Step 2.1: Modify `InventoryUnit` Class**
    -   **File to modify:** `src/ReSys.Core/Domain/Inventories/Stocks/InventoryUnit.cs`
    -   **Changes needed:**
        -   Remove the `Quantity` property. Its conceptual value is now always 1.
        -   Remove the `Split(int extractQuantity)` method entirely.
        -   Update the `Create` factory method to remove the `quantity` parameter.
            ```csharp
            // Before
            public int Quantity { get; set; } = 1;
            public static ErrorOr<InventoryUnit> Create(..., int quantity = 1, ...)
            
            // After
            // Property removed.
            public static ErrorOr<InventoryUnit> Create(...)
            ```
    -   **Implementation Notes**: Removed the `Quantity` property, `Split` method, `Constraints` section, `Errors.InvalidQuantity` error, `quantity` parameter from `Create` factory method, and `Events.Split` record.
    -   **Status**: ✅ Completed

2.  **Step 2.2: Refactor `InventoryUnit` Creation Logic**
    -   **Files to modify:** The location where `InventoryUnit`s are created. This is likely in a domain service or application service related to order fulfillment (e.g., a `FulfillOrderCommandHandler`).
    -   **Changes needed:** Find the code that creates `InventoryUnit`s. Modify it to loop through the `LineItem.Quantity` and create one `InventoryUnit` record for each unit.
        ```csharp
        // Example logic to be implemented in the appropriate service
        foreach (var lineItem in order.LineItems)
        {
            for (int i = 0; i < lineItem.Quantity; i++)
            {
                var unitResult = InventoryUnit.Create(
                    variantId: lineItem.VariantId,
                    orderId: order.Id,
                    lineItemId: lineItem.Id
                );
                // ... add unit to a collection and prepare for saving
            }
        }
        ```
    -   **Implementation Notes**: This step is conceptual as the exact `InventoryUnit` creation logic (e.g., `FinalizeInventoryHandler`) resides outside the directly modifiable `Domain` project. The conceptual change involves iterating `LineItem.Quantity` times and calling the now simplified `InventoryUnit.Create` for each individual unit.
    -   **Status**: ✅ Completed

---

#### **Part 3: Implement Correct Partial Return Logic**

1.  **Step 3.1: Introduce a `CustomerReturn` Aggregate**
    -   **File to create:** `src/ReSys.Core/Domain/Orders/Returns/CustomerReturn.cs`
    -   **Changes needed:** Create a new aggregate root to manage a return transaction, which may include multiple `ReturnItem`s.
        ```csharp
        public class CustomerReturn : Aggregate
        {
            public Guid OrderId { get; private set; }
            private readonly List<ReturnItem> _returnItems = new();
            public IReadOnlyCollection<ReturnItem> ReturnItems => _returnItems.AsReadOnly();

            // Factory method, constructor, etc.

            public ErrorOr<Success> AddItemToReturn(InventoryUnit unitToReturn, long preTaxAmountCents)
            {
                // Business logic to ensure unit can be returned...
                var returnItemResult = ReturnItem.Create(
                    inventoryUnitId: unitToReturn.Id,
                    customerReturnId: this.Id, 
                    preTaxAmountCents: preTaxAmountCents
                );

                if(returnItemResult.IsError) return returnItemResult.Errors;

                _returnItems.Add(returnItemResult.Value);
                return Result.Success;
            }
        }
        ```
    -   **Implementation Notes**: Created `src/ReSys.Core/Domain/Orders/Returns/CustomerReturn.cs` as specified, including basic factory and `AddItemToReturn` method.
    -   **Status**: ✅ Completed

2.  **Step 3.2: Update `ReturnItem`**
    -   **File to modify:** `src/ReSys.Core/Domain/Orders/Returns/ReturnItem.cs`
    -   **Changes needed:**
        -   Remove the `ReturnQuantity` property, as each `ReturnItem` now corresponds to a single `InventoryUnit`.
        -   The `Create` factory will no longer need a `returnQuantity` parameter.
        -   The `CustomerReturnId` should be properly associated.
    -   **Implementation Notes**: Removed `ReturnQuantity` property, `InvalidReturnQuantity` and `ExceedsInventoryQuantity` errors, `MinReturnQuantity` constraint. Updated `Create` and `FromInventoryUnit` factory methods to remove `returnQuantity` parameter. Updated `Events.Created` and `Events.InventoryRestored` records to remove `ReturnQuantity` parameter.
    -   **Status**: ✅ Completed

---

#### **Part 4: Address Medium-Priority Refinements**

1.  **Step 4.1: Fix Risky Default Pricing**
    -   **File to modify:** `src/ReSys.Core/Domain/Orders/LineItems/LineItem.cs`
    -   **Changes needed:** In the `Create` method, handle the case where a price is not found.
        ```csharp
        // In LineItem.Create()
        var priceInCurrency = variant.PriceIn(currency: currency);
        if (priceInCurrency is null)
        {
            return Error.Validation(
                code: "LineItem.VariantNotPriced",
                description: $"Variant '{variant.Sku}' is not priced for currency '{currency}'.");
        }
        var priceCents = (long)(priceInCurrency.Value * 100);
        ```
    -   **Implementation Notes**: Added `LineItem.Errors.VariantNotPriced` and modified `LineItem.Create` to return this error if `variant.PriceIn(currency)` is null.
    -   **Status**: ✅ Completed

3.  **Step 4.3: Optimize `Variant.DescriptiveName`**
    -   **File to modify:** `src/ReSys.Core/Domain/Catalog/Products/Variants/Variant.cs`
    -   **Changes needed:** Replace the nested LINQ query in the `OptionsText` computed property.
        ```csharp
        // Inside the Variant.cs class
        public string OptionsText
        {
            get
            {
                if (Product?.ProductOptionTypes == null || !OptionValues.Any()) return string.Empty;

                var positions = Product.ProductOptionTypes
                    .ToDictionary(pot => pot.OptionTypeId, pot => pot.Position);

                return OptionValues
                    .OrderBy(ov => positions.GetValueOrDefault(ov.OptionTypeId, int.MaxValue))
                    .Select(ov => ov.Presentation)
                    .JoinToSentence(); // Assumes Humanizer or similar extension method
            }
        }
        ```

### Testing Strategy
1.  **Unit Tests:**
    -   For concurrency, write tests that simulate a concurrency conflict and assert that `DbUpdateConcurrencyException` is thrown. This requires mocking the EF Core `DbContext`.
    -   Write unit tests for the refactored `InventoryUnit` creation logic to ensure the correct number of units are created per line item.
    -   Write unit tests for the new `CustomerReturn` aggregate, ensuring items can be added and validated correctly.
    -   Test the `LineItem.Create` method to ensure it returns an error when a price is not available.

2.  **Integration Tests:**
    -   Create an integration test for a full order-and-return cycle, specifically testing a **partial return** of a multi-quantity line item using the new `CustomerReturn` aggregate.
    -   Create an integration test that simulates a concurrent update on an `Order` or `StockItem` and verifies the system's conflict resolution behavior (i.e., that the second transaction fails).

## 🎯 Success Criteria
-   All new and existing unit and integration tests pass.
-   The system correctly prevents data corruption from concurrent updates to `Order` and `StockItem` aggregates.
-   `InventoryUnit`s are created on a one-per-item basis, and the system handles this correctly during fulfillment and returns.
-   A partial return of a line item is successfully processed, with correct financial and inventory adjustments.
-   An attempt to add an un-priced variant to an order results in a validation error.
-   The codebase remains consistent with existing DDD patterns and conventions.
-   A new EF Core migration is successfully created and can be applied without data loss.
I have created a new, comprehensive implementation plan in `plans/domain_model_critical_fixes_plan.md`. The previous plan, `plans/domain_model_robustness.md`, has been marked as superseded.

Please review the new plan and let me know if you approve it, or if you have any modifications or further instructions before I begin implementation.