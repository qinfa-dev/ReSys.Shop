# Feature Implementation Plan: Unit Tests for Promotions

This plan outlines the steps to create a comprehensive suite of unit tests for the Promotions domain model in the `ReSys.Core` project. The goal is to ensure the correctness and robustness of the promotion logic, including creation, state transitions, rule management, and validation.

## 📋 Todo Checklist
- [x] Create directory structure for promotion tests.
- [x] Create unit tests for the `Promotion` aggregate root.
- [x] Create unit tests for the `PromotionUsage` action entity.
- [x] Create unit tests for the `PromotionRule` entity.
- [x] Run all tests and ensure they pass.
- [x] Final Review.

## 🔍 Analysis & Investigation

### Inspected Files
- `src/ReSys.Core/Domain/Promotions/Promotions/Promotion.cs`
- `src/ReSys.Core/Domain/Promotions/Actions/PromotionUsage.cs`
- `src/ReSys.Core/Domain/Promotions/Rules/PromotionRule.cs`
- `tests/Core.UnitTests/Domain/` (to check for existing test structure)

### Current Architecture & Findings
- **Domain Models:** The Promotions domain is centered around the `Promotion` aggregate root, which is associated with `PromotionUsage` (defining the action) and a collection of `PromotionRule` entities (defining eligibility).
- **Existing Tests:** The investigation confirmed that no unit tests currently exist for the Promotions domain. The directory `tests/Core.UnitTests/Domain/Promotions` does not exist.
- **Testing Approach:** The plan will follow the existing project conventions for unit tests found in other domain folders (e.g., `tests/Core.UnitTests/Domain/Orders`). This involves creating separate test classes for each domain entity and using the xUnit framework with FluentAssertions. Helper methods will be used to create test data and mock dependencies where necessary.

## 📝 Implementation Plan

### Prerequisites
- Familiarity with the xUnit testing framework and FluentAssertions library.

### Step 1: Create Directory Structure for Tests

**Goal:** Establish the necessary directories for the new promotion unit tests, mirroring the domain model structure.

1.  **Create Promotions Directory:**
    -   **Action:** Create a new directory.
    -   **Path:** `tests/Core.UnitTests/Domain/Promotions`

2.  **Create Sub-directories:**
    -   **Action:** Create new sub-directories within the `Promotions` folder.
    -   **Paths:**
        -   `tests/Core.UnitTests/Domain/Promotions/Promotions`
        -   `tests/Core.UnitTests/Domain/Promotions/Actions`
        -   `tests/Core.UnitTests/Domain/Promotions/Rules`
-   **Implementation Notes**: Created all necessary directories for the promotion unit tests.
-   **Status**: ✅ Completed

---

### Step 2: Enhance `Promotion.Create` with Validation and Create Unit Tests

**Goal:** Add missing validation logic to the `Promotion.Create` factory method and write unit tests to cover the creation, validation, and business logic of the `Promotion` aggregate root.

1.  **Add Validation to `Promotion.Create`:**
    -   **File to modify:** `src/ReSys.Core/Domain/Promotions/Promotions/Promotion.cs`
    -   **Changes needed:**
        -   Add validation to ensure the `name` parameter is not null/empty and adheres to `Constraints.MinNameLength` and `Constraints.NameMaxLength`.
        -   Add new `Errors` to `Promotion.Errors` static class for `NameRequired` and `NameTooLong`.
    -   **Implementation Notes**: Added missing validation for the `name` parameter and corresponding error codes to `Promotion.Create`.
    -   **Status**: ✅ Completed

2.  **Create Test File:**
    -   **File to create:** `tests/Core.UnitTests/Domain/Promotions/Promotions/PromotionTests.cs`

3.  **Add Test Cases:**
    -   **Changes needed:** Implement the following test methods within `PromotionTests.cs`.
        -   **`Create_ShouldReturnPromotion_WhenValidInputs`**: Test the successful creation of a `Promotion` with valid parameters.
        -   **`Create_ShouldReturnError_WhenNameIsInvalid`**: Test that creation fails if the name is null, empty, or too short/long (now that validation is added).
        -   **`Create_ShouldReturnError_WhenNumericParametersAreInvalid`**: Test that creation fails for negative `minimumOrderAmount`, `maximumDiscountAmount`, or `usageLimit`.
        -   **`Update_ShouldUpdateProperties_WhenValidParametersProvided`**: Test that the `Update` method correctly changes properties.
        -   **`Activate_ShouldSetIsActive_WhenNotExpired`**: Test that `Activate` correctly sets the `Active` flag.
        -   **`Deactivate_ShouldSetIsInactive`**: Test that `Deactivate` correctly sets the `Active` flag to false.
        -   **`AddRule_ShouldAddRule_WhenRuleIsValid`**: Test that a `PromotionRule` is correctly added to the `PromotionRules` collection.
        -   **`AddRule_ShouldReturnError_WhenRuleIsDuplicate`**: Test that adding the same rule twice returns a conflict error.
        -   **`RemoveRule_ShouldRemoveRule_WhenRuleExists`**: Test the successful removal of a rule.
        -   **`IncrementUsage_ShouldIncreaseUsageCount`**: Test that the usage count is correctly incremented.
        -   **`Validate_ShouldReturnSuccess_WhenPromotionIsConsistent`**: Test the `Validate` method for a valid promotion configuration.
        -   **`Validate_ShouldReturnError_WhenDateRangeIsInvalid`**: Test that `Validate` returns an error if `StartsAt` is after `ExpiresAt`.
        -   **`Validate_ShouldReturnError_WhenCodeIsRequiredButMissing`**: Test that `Validate` returns an error if `RequiresCouponCode` is true but `PromotionCode` is null or empty.
    -   **Implementation Notes**: Created the `PromotionTests.cs` file and added a comprehensive suite of unit tests covering the `Promotion` aggregate. Fixed `CS1503` errors by changing string literals to `PromotionRule.RuleType` enum values.
    -   **Status**: ✅ Completed

---

### Step 3: Enhance `PromotionUsage` Validation and Create Unit Tests

**Goal:** Add missing percentage range validation to `PromotionUsage` factory methods and verify their correct creation.

1.  **Add Validation to `PromotionUsage.CreateOrderDiscount` and `CreateItemDiscount`:**
    -   **File to modify:** `src/ReSys.Core/Domain/Promotions/Actions/PromotionUsage.cs`
    -   **Changes needed:**
        -   Modify both `CreateOrderDiscount` and `CreateItemDiscount` methods to include validation:
            -   If `discountType` is `Promotion.DiscountType.Percentage`, then `value` must be between `0.0m` and `1.0m` (inclusive).
            -   Add a new `Error` to `PromotionUsage.Errors` static class, e.g., `InvalidPercentageValue`.
    -   **Implementation Notes**: Added missing validation for percentage values and corresponding error code to `PromotionUsage.CreateOrderDiscount` and `CreateItemDiscount`.
    -   **Status**: ✅ Completed

2.  **Create Test File:**
    -   **File to create:** `tests/Core.UnitTests/Domain/Promotions/Actions/PromotionUsageTests.cs`

3.  **Add Test Cases:**
    -   **Changes needed:** Implement the following test methods within `PromotionUsageTests.cs`.
        -   **`CreateOrderDiscount_ShouldCreateCorrectUsage_ForFixedAmount`**: Test the factory for a fixed amount order discount.
        -   **`CreateOrderDiscount_ShouldCreateCorrectUsage_ForPercentage`**: Test the factory for a percentage-based order discount.
        -   **`CreateItemDiscount_ShouldCreateCorrectUsage_ForFixedAmount`**: Test the factory for a fixed amount item discount.
        -   **`CreateItemDiscount_ShouldReturnError_ForInvalidPercentage`**: Test that creating a percentage discount outside the 0-1 range returns `PromotionUsage.Errors.InvalidPercentageValue`.
        -   **`CreateOrderDiscount_ShouldReturnError_ForInvalidPercentage`**: Test that creating a percentage discount outside the 0-1 range returns `PromotionUsage.Errors.InvalidPercentageValue`.
    -   **Implementation Notes**: Created the `PromotionUsageTests.cs` file and added unit tests for the factory methods. Resolved `CS1061` errors by adding `using ReSys.Core.Common.Domain.Concerns;` and explicitly casting to `IHasMetadata` for `GetPrivate` calls.
    -   **Status**: ✅ Completed

---

### Step 4: Enhance `PromotionRule.Create` with Validation and Create Unit Tests

**Goal:** Add missing validation logic to the `PromotionRule.Create` factory method and ensure the `PromotionRule` entity is created correctly, including validation scenarios.

1.  **Add Validation to `PromotionRule.Create`:**
    -   **File to modify:** `src/ReSys.Core/Domain/Promotions/Rules/PromotionRule.cs`
    -   **Changes needed:**
        -   Add validation to ensure the `value` parameter is not null/empty and does not exceed `Constraints.ValueMaxLength`.
        -   Add validation to ensure the `type` parameter is a valid `RuleType` enum value.
        -   Add new `Errors` to `PromotionRule.Errors` static class for `InvalidType` and `ValueTooLong` (or similar).
    -   **Implementation Notes**: Added the missing validation logic and new error codes to `PromotionRule.cs`.
    -   **Status**: ✅ Completed

2.  **Create Test File:**
    -   **File to create:** `tests/Core.UnitTests/Domain/Promotions/Rules/PromotionRuleTests.cs`

3.  **Add Test Cases:**
    -   **Changes needed:** Implement the following test methods within `PromotionRuleTests.cs`.
        -   **`Create_ShouldReturnRule_WhenValidInputs`**: Test the successful creation of a `PromotionRule`.
        -   **`Create_ShouldReturnError_WhenValueIsInvalid`**: Test that creation fails if the `value` is null, empty, or too long.
        -   **`Create_ShouldReturnError_WhenTypeIsInvalid`**: Test that providing an invalid or out-of-range enum value for the rule type returns an error (now that validation is added).
    -   **Implementation Notes**: Created the `PromotionRuleTests.cs` file and added unit tests reflecting the newly added validation.
    -   **Status**: ✅ Completed

### Testing Strategy
-   Execute `dotnet test` from the root directory after all new test files and methods have been added.
-   All new tests should pass.
-   No existing tests should fail, ensuring no regressions have been introduced.

## 🎯 Success Criteria
-   The new test files (`PromotionTests.cs`, `PromotionUsageTests.cs`, `PromotionRuleTests.cs`) are created in the correct directories.
-   The new test files contain a comprehensive suite of unit tests covering the key business logic of their respective domain entities.
-   The entire test suite for the `Core.UnitTests` project passes successfully after the new tests are added.
