# Feature Implementation Plan: Unit Tests for Shipping Methods

This plan outlines the steps to create a comprehensive suite of unit tests for the `ShippingMethod` aggregate root. The goal is to ensure the correctness and robustness of the shipping method logic, including creation, updates, soft deletion, restoration, and derived properties.

## 📋 Todo Checklist
- [ ] Create directory structure for ShippingMethod tests.
- [x] Add validation to `ShippingMethod` domain model.
- [x] Create unit tests for the `ShippingMethod` aggregate root.
- [ ] Run all tests and ensure they pass.
- [ ] Final Review and Testing.

## 🔍 Analysis & Investigation

### Inspected Files
- `src/ReSys.Core/Domain/ShippingMethods/ShippingMethod.cs`
- `tests/Core.UnitTests/Domain/` (to check for existing test structure)

### Codebase Structure
- The `ShippingMethod` aggregate root is located at `src/ReSys.Core/Domain/ShippingMethods/ShippingMethod.cs`.
- The namespace for `ShippingMethod` is `ReSys.Core.Domain.Shipping`.

### Current Architecture & Findings
- **Domain Model:** The `ShippingMethod` class defines and manages shipping options, including type classification, cost calculation, and estimated delivery times.
- **Key Methods/Properties:**
    - `Create` factory method: Lacks explicit validation despite documented constraints for `name`, `presentation`, `type`, `baseCost`, `estimatedDaysMin/Max`, `position`.
    - `Update` method: Lacks explicit validation for `name`, `presentation`, `baseCost`, `estimatedDaysMin/Max`, `maxWeight`, `position`.
    - `CalculateCost`: Logic for determining shipping cost with surcharge.
    - `Delete`: Raises an event, but actual checks for associated shipments/stores happen in an application service.
    - Computed properties: `IsFreeShipping`, `IsExpressShipping`, `EstimatedDelivery`.
- **Errors:** A static `Errors` class defines `Required`, `NotFound`, `InUse`.
- **Existing Tests:** There are no existing unit tests for the `ShippingMethod` aggregate.
- **Testing Approach:** Tests will follow existing project conventions (xUnit, FluentAssertions), with helper methods for setup.

### Dependencies & Integration Points
- `ShippingMethod` interacts with `ReSys.Core.Common.Domain.Concerns` (`IHasUniqueName`, `IHasPosition`, `IHasParameterizableName`, `IHasMetadata`, `IHasDisplayOn`).
- Uses `ReSys.Core.Domain.Orders.Shipments.Shipment` for its `Shipments` collection.

### Considerations & Challenges
- **Missing Validation:** The primary challenge is that the `Create` and `Update` methods in `ShippingMethod.cs` do not currently perform the validations described in their XML comments. These validations must be implemented *before* writing tests for them.
- **`Delete` method:** The `Delete` method in `ShippingMethod` itself only raises a domain event. The actual check for associated shipments or `StoreShippingMethod` links is expected to occur in an application service or handler, not within the aggregate root's `Delete` method.
- **Metadata:** Testing `PublicMetadata` and `PrivateMetadata` will involve verifying key-value pairs.
- **`IHasParameterizableName`:** The `Create` and `Update` methods use `HasParameterizableName.NormalizeParams`. This behavior should be covered.

## 📝 Implementation Plan

### Prerequisites
- Familiarity with the xUnit testing framework and FluentAssertions library.

### Step-by-Step Implementation

### Step 1: Create Directory Structure for ShippingMethod Tests

**Goal:** Establish the necessary directories for the new `ShippingMethod` unit tests.

1.  **Create ShippingMethods Directory:**
    -   **Action:** Create a new directory.
    -   **Path:** `tests/Core.UnitTests/Domain/ShippingMethods`

---

### Step 2: Enhance `ShippingMethod` with Validation

**Goal:** Add missing validation logic to the `ShippingMethod.Create` and `ShippingMethod.Update` methods to enforce domain invariants.

1.  **Add Errors to `ShippingMethod.Errors`:**
    -   **File to modify:** `src/ReSys.Core/Domain/ShippingMethods/ShippingMethod.cs`
    -   **Changes needed:** Add the following static `Error` definitions:
        -   `NameRequired`
        -   `NameTooLong`
        -   `PresentationRequired`
        -   `BaseCostNegative`
        -   `EstimatedDaysRangeInvalid`
        -   `PositionNegative`
        -   `InvalidType`

2.  **Implement Validation in `ShippingMethod.Create`:**
    -   **File to modify:** `src/ReSys.Core/Domain/ShippingMethods/ShippingMethod.cs`
    -   **Changes needed:**
        -   Validate `name` (not null/whitespace, max length).
        -   Validate `presentation` (not null/whitespace).
        -   Validate `type` (valid enum value).
        -   Validate `baseCost` (non-negative).
        -   Validate `estimatedDaysMin` and `estimatedDaysMax` (range consistency, non-negative).
        -   Validate `position` (non-negative).
        -   Return appropriate errors if validation fails.

3.  **Implement Validation in `ShippingMethod.Update`:**
    -   **File to modify:** `src/ReSys.Core/Domain/ShippingMethods/ShippingMethod.cs`
    -   **Changes needed:**
        -   Add validation for `name` (if updated).
        -   Add validation for `presentation` (if updated).
        -   Add validation for `baseCost` (if updated).
        -   Add validation for `estimatedDaysMin/Max` (if updated).
        -   Add validation for `maxWeight` (non-negative, if updated).
        -   Add validation for `position` (non-negative, if updated).
        -   Return appropriate errors if validation fails.

---

### Step 3: Create Unit Tests for `ShippingMethod` Aggregate

**Goal:** Write comprehensive unit tests covering the creation, business logic, and derived properties of the `ShippingMethod` aggregate root, including the newly added validations.

1.  **Create Test File:**
    -   **File to create:** `tests/Core.UnitTests/Domain/ShippingMethods/ShippingMethodTests.cs`

2.  **Add Helper Methods:**
    -   **Changes needed:** Implement helper methods within `ShippingMethodTests.cs` to streamline test setup, e.g.:
        -   `CreateValidShippingMethod(name, presentation, type, baseCost, ...)`: Creates a valid `ShippingMethod` instance.
        -   `CreateDummyShipment(shippingMethodId)`: Creates a `Shipment` instance for testing `InUse` scenarios.

3.  **Add Test Cases (Creation):**
    -   **Changes needed:** Implement test methods for `Create` factory method, including:
        -   **`Create_ShouldReturnShippingMethod_WhenValidInputs`**: Successful creation.
        -   **`Create_ShouldReturnError_WhenNameIsInvalid`**: For null/empty/too long name.
        -   **`Create_ShouldReturnError_WhenPresentationIsInvalid`**: For null/empty presentation.
        -   **`Create_ShouldReturnError_WhenTypeIsInvalid`**: For invalid `ShippingType`.
        -   **`Create_ShouldReturnError_WhenBaseCostIsNegative`**: For negative `baseCost`.
        -   **`Create_ShouldReturnError_WhenEstimatedDaysRangeIsInvalid`**: For `estimatedDaysMin > estimatedDaysMax`.
        -   **`Create_ShouldReturnError_WhenPositionIsNegative`**: For negative `position`.
        -   **`Create_ShouldNormalizeNameAndPresentation`**: Verify `Trim()` behavior.
        -   **`Create_ShouldPublishCreatedEvent`**: Check for `Events.Created`.

4.  **Add Test Cases (Update):**
    -   **Changes needed:** Implement test methods for `Update` method, including:
        -   **`Update_ShouldUpdateAllProperties_WhenValid`**: Update various properties successfully.
        -   **`Update_ShouldReturnError_WhenNameIsInvalid`**: For null/empty/too long name.
        -   **`Update_ShouldReturnError_WhenPresentationIsInvalid`**: For null/empty presentation.
        -   **`Update_ShouldReturnError_WhenBaseCostIsNegative`**: For negative `baseCost`.
        -   **`Update_ShouldReturnError_WhenEstimatedDaysRangeIsInvalid`**: For `estimatedDaysMin > estimatedDaysMax`.
        -   **`Update_ShouldReturnError_WhenPositionIsNegative`**: For negative `position`.
        -   **`Update_ShouldReturnError_WhenMaxWeightIsNegative`**: For negative `maxWeight`.
        -   **`Update_ShouldNotUpdateIfNoChanges`**: Verify `UpdatedAt` and `DomainEvents` if no actual changes are made.
        -   **`Update_ShouldPublishUpdatedEvent`**: Check for `Events.Updated`.

5.  **Add Test Cases (Business Logic):**
    -   **Changes needed:** Implement test methods for `CalculateCost`:
        -   **`CalculateCost_ShouldReturnZeroForFreeShipping`**.
        -   **`CalculateCost_ShouldApplySurchargeForOverweight`**.
        -   **`CalculateCost_ShouldReturnBaseCostForNormalWeight`**.

6.  **Add Test Cases (Delete):**
    -   **Changes needed:** Implement test methods for `Delete` method:
        -   **`Delete_ShouldPublishDeletedEvent`**: Verify the event.
        -   **`Delete_ShouldNotContainInternalCheckForAssociatedEntities`**: Confirm `Delete` only raises the event and relies on application service for dependency checks.

7.  **Add Test Cases (Computed Properties):**
    -   **Changes needed:** Implement test methods for computed properties:
        -   **`IsFreeShipping_ShouldReturnCorrectValue`**.
        -   **`IsExpressShipping_ShouldReturnCorrectValue`**.
        -   **`EstimatedDelivery_ShouldReturnFormattedString`**.
        -   **`EstimatedDelivery_ShouldReturnDefaultString_WhenEstimatesAreNull`**.

---

### Step 4: Run All Tests

**Goal:** Execute all unit tests to verify new tests pass and no regressions were introduced.

1.  **Run `dotnet test`:**
    -   **Action:** Execute `dotnet test` command from the root directory.

## Testing Strategy
-   Execute `dotnet test` from the root directory after all new test files and methods have been added.
-   All new tests should pass.
-   No existing tests should fail, ensuring no regressions have been introduced.

## 🎯 Success Criteria
-   The `ShippingMethod` domain model contains the necessary validation logic in its `Create` and `Update` methods.
-   The new test file (`ShippingMethodTests.cs`) is created in the correct directory.
-   The new test file contains a comprehensive suite of unit tests covering the key business logic and derived properties of the `ShippingMethod` aggregate.
-   The entire test suite for the `Core.UnitTests` project passes successfully after the new tests are added.
