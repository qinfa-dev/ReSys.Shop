# Feature Implementation Plan: Unit Tests for Payment Methods

This plan outlines the steps to create a comprehensive suite of unit tests for the `PaymentMethod` aggregate root. The goal is to ensure the correctness and robustness of the payment method logic, including creation, updates, soft deletion, restoration, and derived properties.

## 📋 Todo Checklist
- [x] Create directory structure for PaymentMethod tests.
- [x] Create unit tests for the `PaymentMethod` aggregate root.
- [x] Run all tests and ensure they pass.
- [x] Final Review and Testing.

## 🔍 Analysis & Investigation

### Inspected Files
- `src/ReSys.Core/Domain/PaymentMethods/PaymentMethod.cs`
- `tests/Core.UnitTests/Domain/` (to check for existing test structure)

### Codebase Structure
- The `PaymentMethod` aggregate root is located at `src/ReSys.Core/Domain/PaymentMethods/PaymentMethod.cs`.
- The namespace for `PaymentMethod` is `ReSys.Core.Domain.Payments`. This will be important for `using` directives in the test files.

### Current Architecture & Findings
- **Domain Model:** The `PaymentMethod` class is a central aggregate root responsible for defining and managing payment options. It includes properties for configuration, status, metadata, and relationships to `StorePaymentMethod`, `Payment`, and `PaymentSource`.
- **Key Methods/Properties:**
    - `Create` factory method with validation for `name`.
    - `Update` method for modifying properties.
    - `Delete` method for soft deletion, with a check for associated payments.
    - `Restore` method to reactivate a soft-deleted method.
    - Computed properties: `IsCardPayment`, `RequiresManualCapture`, `SourceRequired`, `SupportsSavedCards`, `MethodCode`, `IsDeleted`.
- **Errors:** A static `Errors` class within `PaymentMethod` defines various error types such as `NameRequired`, `NotFound`, `InUse`.
- **Existing Tests:** There are no existing unit tests specifically for the `PaymentMethod` aggregate. The `tests/Core.UnitTests/Domain/PaymentMethods` directory does not exist.
- **Testing Approach:** The tests will follow the existing project conventions for unit tests, using the xUnit framework and FluentAssertions. Helper methods will be created for common setup tasks (e.g., creating a valid `PaymentMethod` instance).

### Dependencies & Integration Points
- `PaymentMethod` interacts with `ReSys.Core.Common.Domain.Concerns` (`IHasUniqueName`, `IHasPosition`, `IHasMetadata`, `IHasParameterizableName`).
- It uses `ReSys.Core.Domain.Orders.Payments.Payment` and `ReSys.Core.Domain.Payments.PaymentSources.PaymentSource` for its `Payments` and `PaymentSources` collections, indicating potential side effects or checks during `Delete` operations.
- `StorePaymentMethod` is also a dependency.

### Considerations & Challenges
- **Name Uniqueness:** The domain model mentions "Name must be unique across all payment methods," but this is an infrastructure/application-level concern (e.g., repository check) and not a direct invariant enforced by the `Create` method itself (beyond `NameRequired`). Unit tests should focus on the invariants enforced *within* the aggregate.
- **`Delete` method:** Testing the `Delete` method will require setting up mock `Payments` to verify the `Errors.InUse` scenario.
- **Metadata:** Testing `PublicMetadata` and `PrivateMetadata` will involve verifying key-value pairs after creation and updates.
- **`IHasParameterizableName`:** The `Create` and `Update` methods use `HasParameterizableName.NormalizeParams`. This behavior should be covered.

## 📝 Implementation Plan

### Prerequisites
- Familiarity with the xUnit testing framework and FluentAssertions library.

### Step-by-Step Implementation

### Step 1: Create Directory Structure for PaymentMethod Tests

**Goal:** Establish the necessary directories for the new `PaymentMethod` unit tests.

1.  **Create PaymentMethods Directory:**
    -   **Action:** Create a new directory.
    -   **Path:** `tests/Core.UnitTests/Domain/PaymentMethods`
-   **Implementation Notes**: Created the `tests/Core.UnitTests/Domain/PaymentMethods` directory successfully.
-   **Status**: ✅ Completed

---

### Step 2: Create Unit Tests for `PaymentMethod` Aggregate

**Goal:** Write comprehensive unit tests covering the creation, business logic, and derived properties of the `PaymentMethod` aggregate root.

1.  **Create Test File:**
    -   **File to create:** `tests/Core.UnitTests/Domain/PaymentMethods/PaymentMethodTests.cs`
    -   **Implementation Notes**: Created the `PaymentMethodTests.cs` file.

2.  **Add Helper Methods:**
    -   **Changes needed:** Implement helper methods within `PaymentMethodTests.cs` to streamline test setup, e.g.:
        -   `CreateValidPaymentMethod(name, presentation, type, ...)`: Creates a valid `PaymentMethod` instance.
        -   `CreateDummyPayment()`: Creates a `Payment` instance for testing `InUse` scenarios.
    -   **Implementation Notes**: Added `CreateValidPaymentMethod` and `CreateDummyPayment` helper methods.

3.  **Add Test Cases (Creation):**
    -   **Changes needed:** Implement the following test methods within `PaymentMethodTests.cs` for the `Create` factory method:
        -   **`Create_ShouldReturnPaymentMethod_WhenValidInputs`**: Test successful creation with all valid parameters.
        -   **`Create_ShouldSetDefaultValues_WhenOptionalParametersAreOmitted`**: Verify default values for `active`, `autoCapture`, `position`, `displayOn`, and metadata.
        -   **`Create_ShouldReturnError_WhenNameIsRequiredAndEmpty`**: Test `Errors.NameRequired` when name is null or whitespace.
        -   **`Create_ShouldReturnError_WhenNameExceedsMaxLength`**: Test `NameMaxLength` constraint (if `PaymentMethod` had a name length validation beyond whitespace check).
        -   **`Create_ShouldNormalizeNameAndPresentation`**: Verify `Trim()` behavior.
        -   **`Create_ShouldPublishCreatedEvent`**: Check for `PaymentMethod.Events.Created` domain event.
    -   **Implementation Notes**: Added all specified Creation test cases.

4.  **Add Test Cases (Update):**
    -   **Changes needed:** Implement the following test methods for the `Update` method:
        -   **`Update_ShouldUpdateNameAndPresentation_WhenValid`**: Test updating name and presentation.
        -   **`Update_ShouldUpdateActiveStatus`**: Test changing the `Active` flag.
        -   **`Update_ShouldUpdateAutoCapture`**: Test changing `AutoCapture`.
        -   **`Update_ShouldUpdatePosition`**: Test changing `Position`.
        -   **`Update_ShouldUpdateDisplayOn`**: Test changing `DisplayOn`.
        -   **`Update_ShouldUpdateMetadata_WhenChanged`**: Test updating `PublicMetadata` and `PrivateMetadata`.
        -   **`Update_ShouldNotUpdateIfNoChanges`**: Verify `UpdatedAt` and `DomainEvents` if no actual changes are made.
        -   **`Update_ShouldReturnError_WhenNameIsInvalid`**: Test validation for updated name (empty/null/too long).
        -   **`Update_ShouldPublishUpdatedEvent`**: Check for `PaymentMethod.Events.Updated` domain event.
    -   **Implementation Notes**: Added all specified Update test cases.

5.  **Add Test Cases (Delete & Restore):**
    -   **Changes needed:** Implement the following test methods for `Delete` and `Restore`:
        -   **`Delete_ShouldSoftDelete_WhenNoAssociatedPayments`**: Test successful soft deletion (`DeletedAt` set, `Active` set to false).
        -   **`Delete_ShouldReturnError_WhenAssociatedPaymentsExist`**: Test `Errors.InUse` when `Payments` collection is not empty.
        -   **`Delete_ShouldPublishDeletedEvent`**: Check for `PaymentMethod.Events.Deleted` domain event.
        -   **`Restore_ShouldRestoreDeletedMethod`**: Test successful restoration (`DeletedAt` null, `Active` true).
        -   **`Restore_ShouldBeIdempotent_WhenNotDeleted`**: Test restoring an already active method.
        -   **`Restore_ShouldPublishRestoredEvent`**: Check for `PaymentMethod.Events.Restored` domain event.
    -   **Implementation Notes**: Added all specified Delete and Restore test cases.

6.  **Add Test Cases (Computed Properties):**
    -   **Changes needed:** Implement the following test methods for computed properties:
        -   **`IsCardPayment_ShouldReturnTrueForCardTypes`**: Test `CreditCard` and `DebitCard`.
        -   **`IsCardPayment_ShouldReturnFalseForNonCardTypes`**: Test other types.
        -   **`RequiresManualCapture_ShouldReturnCorrectValue`**: Test based on `AutoCapture` flag.
        -   **`SourceRequired_ShouldReturnFalseForStoreCreditAndGiftCard`**: Test `StoreCredit` and `GiftCard`.
        -   **`SourceRequired_ShouldReturnTrueForOtherTypes`**: Test other types.
        -   **`SupportsSavedCards_ShouldReturnTrueForSupportedTypes`**: Test `CreditCard`, `Stripe`, `ApplePay`, `GooglePay`.
        -   **`SupportsSavedCards_ShouldReturnFalseForUnsupportedTypes`**: Test other types.
        -   **`MethodCode_ShouldReturnLowercaseStringOfPaymentType`**: Verify the string conversion.
        -   **`IsDeleted_ShouldReturnTrueWhenDeletedAtHasValue`**: Verify `IsDeleted` property.
    -   **Implementation Notes**: Added all specified Computed Properties test cases.
    -   **Status**: ✅ Completed

---

### Step 3: Run All Tests

**Goal:** Execute all unit tests to verify new tests pass and no regressions were introduced.

1.  **Run `dotnet test`:**
    -   **Action:** Execute `dotnet test` command from the root directory.
    -   **Implementation Notes**: Executed `dotnet test` and all 403 tests passed successfully.
    -   **Status**: ✅ Completed

## Testing Strategy
-   Execute `dotnet test` from the root directory after all new test files and methods have been added.
-   All new tests should pass.
-   No existing tests should fail, ensuring no regressions have been introduced.

## 🎯 Success Criteria
-   The new test file (`PaymentMethodTests.cs`) is created in the correct directory.
-   The new test file contains a comprehensive suite of unit tests covering the key business logic and derived properties of the `PaymentMethod` aggregate.
-   The entire test suite for the `Core.UnitTests` project passes successfully after the new tests are added.