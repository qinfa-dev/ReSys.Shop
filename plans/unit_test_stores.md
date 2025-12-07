## 📋 Todo Checklist
- [x] Create `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
- [x] Implement unit tests for `Store.Create` factory method.
- [x] Implement unit tests for `Store.Update` method and its properties.
- [x] Implement unit tests for `Store.SetAddress` and `Store.SetSocialLinks`.
- [x] Implement unit tests for `Store.MakeDefault`, `ProtectWithPassword`, and `RemovePasswordProtection`.
- [x] Implement unit tests for `Store.AddProduct`, `RemoveProduct`, and `UpdateProductSettings` methods.
- [x] Implement unit tests for `Store.AddStockLocation`, `RemoveStockLocation`, and `UpdateStockLocationPriority` methods.
- [x] Implement unit tests for `Store.AddShippingMethod`, `RemoveShippingMethod`, and `UpdateShippingMethodSettings` methods.
- [x] Implement unit tests for `Store.AddPaymentMethod` and `RemovePaymentMethod` methods.
- [x] Implement unit tests for `Store.Delete` and `Store.Restore` methods.
- [x] Verify domain event publishing for all tested methods.
- [x] Ensure all relevant error conditions (`Store.Errors`) are covered.
- [x] Final Review and Testing

## 🔍 Analysis & Investigation

### Codebase Structure
The `Store` aggregate is defined in `src/ReSys.Core/Domain/Stores/Store.cs`. It acts as an aggregate root, managing its own lifecycle and ensuring consistency through its internal business logic and explicit error handling via `ErrorOr<T>`.

Key related entities and their relationships with `Store` include:
- `StoreProduct`: An associative entity linking `Store` to `Product` (from `ReSys.Core.Domain.Catalog.Products`). This entity manages product visibility and featured status within a store.
- `StoreStockLocation`: An associative entity linking `Store` to `StockLocation` (from `ReSys.Core.Domain.Inventories.Locations`). This manages fulfillment priorities for stock locations per store.
- `Order`: Directly linked to `Store` via an `ICollection<Order> Orders` property, indicating that orders are placed within a specific store context.
- `StoreShippingMethod` and `StorePaymentMethod`: Associative entities linking `Store` to available `ShippingMethod` and `PaymentMethod` configurations.

### Current Architecture
The project follows a Clean Architecture pattern, with `ReSys.Core` housing the domain and application logic. Domain entities, like `Store`, are designed with internal consistency in mind, using private constructors, static factory methods (`Store.Create`), and instance methods for state changes (e.g., `Update`, `AddProduct`).

Error handling is implemented using the `ErrorOr<T>` functional approach, returning `Error` objects instead of throwing exceptions for expected business rule violations.

Domain events are extensively used to signal state changes, allowing for decoupled integration with other bounded contexts or application services. The `Aggregate` base class provides functionality for managing and clearing domain events.

### Dependencies & Integration Points
The `Store` aggregate has dependencies on:
- `ReSys.Core.Domain.Catalog.Products.Product`: For product management within the store.
- `ReSys.Core.Domain.Inventories.Locations.StockLocation`: For managing inventory locations.
- `ReSys.Core.Domain.Orders.Order`: For checking active orders during store deletion.
- `ReSys.Core.Domain.Location.Country` and `ReSys.Core.Domain.Location.State`: For address information.
- `ReSys.Core.Domain.Shipping.ShippingMethod` and `ReSys.Core.Domain.Payments.PaymentMethod`: For configuring store-specific shipping and payment options.

The unit tests for `Store` will primarily focus on the internal logic of the `Store` aggregate and its direct interactions with these linked entities (e.g., ensuring `StoreProduct` is correctly created/removed, not testing the `Product` entity itself). Mocking will be necessary for external entities like `Product` and `StockLocation` where full aggregate instances are not required for the specific test case.

### Considerations & Challenges
1.  **Complex State Transitions:** `Store` has several state-changing methods (e.g., `Update`, `MakeDefault`, `Delete`, `ProtectWithPassword`) and properties that influence its behavior (`Available`, `PasswordProtected`, `IsDeleted`). Thorough testing requires covering various combinations of these states.
2.  **ErrorOr Handling:** Each business method returns `ErrorOr<T>`. Tests need to assert both successful outcomes (`IsError` is false, `Value` is correct) and error conditions (`IsError` is true, `FirstError` matches expected `Store.Errors`).
3.  **Domain Event Verification:** The `Store` aggregate publishes numerous domain events. Tests must ensure the correct events are added to `DomainEvents` collection upon specific actions and that `ClearDomainEvents()` is used appropriately to isolate events per test.
4.  **Helper Entity Creation:** Related entities like `Product`, `StockLocation`, `ShippingMethod`, and `PaymentMethod` will need to be created or mocked for interactions. Simple helper factory methods will be beneficial.
5.  **Data Consistency and Invariants:** Numerous constraints (e.g., `NameMaxLength`, `ValidCurrencies`) and business rules are embedded. Edge cases (e.g., invalid input, null checks) must be tested.
6.  **`GenerateStoreCode` Method:** This private static method is used during `Store.Create`. While it's private, its logic can be tested indirectly through `Store.Create` or via reflection if deemed necessary for complete coverage (though indirect testing is usually sufficient for private helpers).
7.  **Timezone and Email Validation:** The `IsValidTimezone` and `IsValidEmail` private static methods are crucial for `Store.Create` and `Store.Update`. These should be implicitly tested through the public methods that use them.

## 📝 Implementation Plan

### Prerequisites
-   Familiarity with xUnit and FluentAssertions.
-   Understanding of `ErrorOr<T>` for functional error handling.
-   Knowledge of `Aggregate` base class and domain event patterns.

### Step-by-Step Implementation

1.  **Create Test Project and Folder Structure:**
    -   Create a new directory: `tests/Core.UnitTests/Domain/Stores/`.
    -   Create the main test file: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`.
    -   Add any necessary `using` statements (e.g., `ReSys.Core.Domain.Stores`, `FluentAssertions`, `Xunit`).
    -   **Implementation Notes**: Directory `tests/Core.UnitTests/Domain/Stores/` created. File `tests/Core.UnitTests/Domain/Stores/StoreTests.cs` created.
    -   **Status**: ✅ Completed

2.  **Implement Helper Methods:**
    -   Create a static helper method `CreateValidStore()` within `StoreTests.cs` to generate a valid `Store` instance, similar to `CreateValidProduct` in `ProductTests.cs`. This will streamline test setup.
    -   Create similar helper methods for `Product`, `StockLocation`, `ShippingMethod`, `PaymentMethod` that return basic valid instances, to be used when interacting with `Store`'s methods. These can be simple mocks or valid instances of the entities themselves if their `Create` methods are simple enough.
    -   **Implementation Notes**: `CreateValidStore`, `CreateValidProduct`, `CreateValidStockLocation`, `CreateValidShippingMethod`, `CreateValidPaymentMethod`, and `CreateValidOrder` helper methods added to `StoreTests.cs`. A placeholder `Product_Create_ShouldReturnProduct_WhenValidParameters` test was also added and will be removed later.
    -   **Status**: ✅ Completed

3.  **Test `Store.Create` Factory Method:**
    -   **Test Cases:**
        -   Valid parameters should return a `Store` instance and publish `StoreCreated` event.
        -   Invalid `name` (null/empty, too long) should return `NameRequired` or `NameTooLong` error.
        -   Invalid `presentation` (too long) should return `PresentationTooLong` error.
        -   Invalid `code` (null/empty, too long) should return `CodeRequired` or `CodeTooLong` error.
        -   Invalid `url` (null/empty, too long) should return `UrlRequired` or `UrlTooLong` error.
        -   Invalid `currency` should return `InvalidCurrency` error.
        -   Invalid `timezone` should return `InvalidTimezone` error.
        -   Invalid `mailFromAddress` or `customerSupportEmail` should return `InvalidMailFromAddress` or `InvalidCustomerSupportEmail` error.
        -   Test default values for `currency`, `locale`, `timezone`, `Available`, `GuestCheckoutAllowed`, `PasswordProtected`.
        -   Test `GenerateStoreCode` logic indirectly.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` and `[Theory]` methods covering the above scenarios.
    -   **Implementation Notes**: Implemented `Store_Create_ShouldReturnStore_WhenValidParameters`, `Store_Create_ShouldUseDefaultValues_WhenOptionalParametersAreNull`, and various `Store_Create_ShouldReturn...Error_When...` tests for name, presentation, code, url, currency, timezone, mailFromAddress, and customerSupportEmail. Also added tests for auto-generated code and URL and `isDefault` parameter. The placeholder `Product_Create` test was removed.
    -   **Status**: ✅ Completed

4.  **Test `Store.Update` Method:**
    -   **Test Cases:**
        -   Updating individual properties (e.g., `name`, `url`, `metaTitle`, `available`) should succeed, update the property, set `UpdatedAt`, and publish `StoreUpdated` event.
        -   Attempting to update with invalid values (e.g., `name` too long, invalid `currency`) should return appropriate errors.
        -   Calling `Update` with no changes should not update `UpdatedAt` and should not publish `StoreUpdated` event.
        -   Test updates to `publicMetadata` and `privateMetadata`.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods.
    -   **Implementation Notes**: Implemented `Store_Update_ShouldUpdateNameAndPresentation_WhenValidNamesProvided`, `Store_Update_ShouldUpdateUrl_WhenValidUrlProvided`, `Store_Update_ShouldReturnNameTooLongError_WhenNewNameExceedsMaxLength`, `Store_Update_ShouldReturnPresentationTooLongError_WhenNewPresentationExceedsMaxLength`, `Store_Update_ShouldUpdateMailFromAddressAndCustomerSupportEmail_WhenValidEmailsProvided`, `Store_Update_ShouldUpdateSeoMetadata_WhenProvided`, `Store_Update_ShouldUpdateAvailabilityAndGuestCheckout_WhenProvided`, `Store_Update_ShouldUpdateDefaultLocaleAndCurrency_WhenValidValuesProvided`, `Store_Update_ShouldReturnInvalidCurrencyError_WhenInvalidCurrencyProvided`, `Store_Update_ShouldNotUpdate_WhenNoChangesAreMade`, `Store_Update_ShouldUpdateMetadata_WhenProvided`.
    -   **Status**: ✅ Completed

5.  **Test `Store.SetAddress` and `Store.SetSocialLinks` Methods:**
    -   **Test Cases:**
        -   Setting address fields (e.g., `address1`, `city`, `zipcode`, `countryId`) should update properties, set `UpdatedAt`, and publish `StoreAddressUpdated` event.
        -   Setting social links (e.g., `facebook`, `instagram`, `twitter`) should update properties, set `UpdatedAt`, and publish `StoreSocialLinksUpdated` event.
        -   Calling with no changes should not update `UpdatedAt` and should not publish events.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods.
    -   **Implementation Notes**: Implemented `Store_SetAddress_ShouldUpdateAddressProperties_WhenProvided`, `Store_SetAddress_ShouldNotUpdate_WhenNoChangesAreMade`, `Store_SetSocialLinks_ShouldUpdateSocialLinks_WhenProvided`, `Store_SetSocialLinks_ShouldNotUpdate_WhenNoChangesAreMade`.
    -   **Status**: ✅ Completed

6.  **Test `Store.MakeDefault`, `ProtectWithPassword`, `RemovePasswordProtection` Methods:**
    -   **Test Cases:**
        -   `MakeDefault` should set `Default` to true, set `UpdatedAt`, and publish `StoreMadeDefault` event.
        -   Calling `MakeDefault` when already default should not change state or publish event.
        -   `ProtectWithPassword` with valid hashed password should set `PasswordProtected` to true, `StorefrontPassword`, `UpdatedAt`, and publish `StorePasswordProtectionEnabled` event.
        -   `ProtectWithPassword` with null/empty password should return `InvalidPassword` error.
        -   `RemovePasswordProtection` should set `PasswordProtected` to false, `StorefrontPassword` to null, `UpdatedAt`, and publish `StorePasswordProtectionRemoved` event.
        -   Calling `RemovePasswordProtection` when not password protected should not change state or publish event.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods.
    -   **Implementation Notes**: Implemented `Store_MakeDefault_ShouldSetDefaultToTrueAndRaiseEvent_WhenNotDefault`, `Store_MakeDefault_ShouldNotChangeStateOrRaiseEvent_WhenAlreadyDefault`, `Store_ProtectWithPassword_ShouldSetPasswordProtectedAndRaiseEvent_WhenValidPasswordProvided`, `Store_ProtectWithPassword_ShouldReturnInvalidPasswordError_WhenPasswordIsNullOrEmpty`, `Store_RemovePasswordProtection_ShouldRemovePasswordProtectionAndRaiseEvent_WhenProtected`, `Store_RemovePasswordProtection_ShouldNotChangeStateOrRaiseEvent_WhenNotProtected`.
    -   **Status**: ✅ Completed

7.  **Test Product Management Methods (`AddProduct`, `RemoveProduct`, `UpdateProductSettings`):**
    -   **Test Cases:**
        -   `AddProduct` with a valid `Product` should add to `StoreProducts`, set `UpdatedAt`, and publish `ProductAddedToStore` event.
        -   `AddProduct` with null `Product` should return `InvalidProduct` error.
        -   `AddProduct` with an already existing product should return `ProductAlreadyInStore` error.
        -   `RemoveProduct` with an existing `productId` should remove from `StoreProducts`, set `UpdatedAt`, and publish `ProductRemovedFromStore` event.
        -   `RemoveProduct` with a non-existent `productId` should return `ProductNotInStore` error.
        -   `UpdateProductSettings` for an existing product should update `StoreProduct` properties (visible, featured), set `UpdatedAt`, and publish `ProductSettingsUpdated` event.
        -   `UpdateProductSettings` with no actual changes should not update `UpdatedAt` or publish event.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods, utilizing helper `Product` instances.
    -   **Implementation Notes**: Implemented `Store_AddProduct_ShouldAddProductAndRaiseEvent_WhenValidProductProvided`, `Store_AddProduct_ShouldReturnInvalidProductError_WhenNullProductProvided`, `Store_AddProduct_ShouldReturnProductAlreadyInStoreError_WhenProductAlreadyExists`, `Store_RemoveProduct_ShouldRemoveProductAndRaiseEvent_WhenProductExists`, `Store_RemoveProduct_ShouldReturnProductNotInStoreError_WhenProductDoesNotExist`, `Store_UpdateProductSettings_ShouldUpdateSettingsAndRaiseEvent_WhenProductExists`, `Store_UpdateProductSettings_ShouldNotUpdateOrRaiseEvent_WhenNoChanges`, `Store_UpdateProductSettings_ShouldReturnProductNotInStoreError_WhenProductDoesNotExist`.
    -   **Status**: ✅ Completed

8.  **Test Stock Location Management Methods (`AddStockLocation`, `RemoveStockLocation`, `UpdateStockLocationPriority`):**
    -   **Test Cases:** Similar to product management, covering adding, removing, and updating `StoreStockLocation` links, including error conditions (null location, already added, not found). Verify `StockLocationAddedToStore`, `StockLocationRemovedFromStore`, `StockLocationPriorityUpdated` events.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods, utilizing helper `StockLocation` instances.
    -   **Implementation Notes**: Implemented `Store_AddStockLocation_ShouldAddLocationAndRaiseEvent_WhenValidLocationProvided`, `Store_AddStockLocation_ShouldReturnInvalidStockLocationError_WhenNullLocationProvided`, `Store_AddStockLocation_ShouldReturnStockLocationAlreadyAddedError_WhenLocationAlreadyExists`, `Store_RemoveStockLocation_ShouldRemoveLocationAndRaiseEvent_WhenLocationExists`, `Store_RemoveStockLocation_ShouldReturnStockLocationNotFoundError_WhenLocationDoesNotExist`, `Store_UpdateStockLocationPriority_ShouldUpdatePriorityAndRaiseEvent_WhenLocationExists`, `Store_UpdateStockLocationPriority_ShouldNotUpdateOrRaiseEvent_WhenNoChanges`, `Store_UpdateStockLocationPriority_ShouldReturnStockLocationNotFoundError_WhenLocationDoesNotExist`.
    -   **Status**: ✅ Completed

9.  **Test Shipping Method Management Methods (`AddShippingMethod`, `RemoveShippingMethod`, `UpdateShippingMethodSettings`):**
    -   **Test Cases:** Similar to product and stock location management, covering adding, removing, and updating `StoreShippingMethod` links, including error conditions. Verify `ShippingMethodAddedToStore`, `ShippingMethodRemovedFromStore`, `ShippingMethodSettingsUpdated` events.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods, utilizing helper `ShippingMethod` instances.
    -   **Implementation Notes**: Implemented `Store_AddShippingMethod_ShouldAddMethodAndRaiseEvent_WhenValidMethodProvided`, `Store_AddShippingMethod_ShouldReturnInvalidShippingMethodError_WhenNullMethodProvided`, `Store_AddShippingMethod_ShouldReturnShippingMethodAlreadyAddedError_WhenMethodAlreadyExists`, `Store_RemoveShippingMethod_ShouldRemoveMethodAndRaiseEvent_WhenMethodExists`, `Store_RemoveShippingMethod_ShouldReturnShippingMethodNotFoundError_WhenMethodDoesNotExist`, `Store_UpdateShippingMethodSettings_ShouldUpdateSettingsAndRaiseEvent_WhenMethodExists`, `Store_UpdateShippingMethodSettings_ShouldNotUpdateOrRaiseEvent_WhenNoChanges`, `Store_UpdateShippingMethodSettings_ShouldReturnShippingMethodNotFoundError_WhenMethodDoesNotExist`.
    -   **Status**: ✅ Completed

10. **Test Payment Method Management Methods (`AddPaymentMethod`, `RemovePaymentMethod`):**
    -   **Test Cases:** Similar to other management methods, covering adding and removing `StorePaymentMethod` links, including error conditions. Verify `PaymentMethodAddedToStore`, `PaymentMethodRemovedFromStore` events.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods, utilizing helper `PaymentMethod` instances.
    -   **Implementation Notes**: Implemented `Store_AddPaymentMethod_ShouldAddMethodAndRaiseEvent_WhenValidMethodProvided`, `Store_AddPaymentMethod_ShouldReturnInvalidPaymentMethodError_WhenNullMethodProvided`, `Store_AddPaymentMethod_ShouldReturnPaymentMethodAlreadyAddedError_WhenMethodAlreadyExists`, `Store_RemovePaymentMethod_ShouldRemoveMethodAndRaiseEvent_WhenMethodExists`, `Store_RemovePaymentMethod_ShouldReturnPaymentMethodNotFoundError_WhenMethodDoesNotExist`.
    -   **Status**: ✅ Completed

11. **Test Deletion Methods (`Delete`, `Restore`):**
    -   **Test Cases:**
        -   `Delete` should set `IsDeleted` to true, `DeletedAt`, `UpdatedAt`, and publish `StoreDeleted` event.
        -   `Delete` on a default store without `force` should return `CannotDeleteDefaultStore` error.
        -   `Delete` on a store with active orders without `force` should return `HasActiveOrders` error. (This will require setting up mock `Order` objects that are "active").
        -   `Delete` with `force = true` should bypass default/active order checks.
        -   `Delete` when already deleted should return `Result.Deleted` without further changes or events.
        -   `Restore` should set `IsDeleted` to false, `DeletedAt` to null, `UpdatedAt`, and publish `StoreRestored` event.
        -   `Restore` when not deleted should not change state or publish event.
    -   Files to modify: `tests/Core.UnitTests/Domain/Stores/StoreTests.cs`
    -   Changes needed: Add `[Fact]` methods, utilizing mock `Order` instances for active order checks.
    -   **Implementation Notes**: Implemented `Store_Delete_ShouldSetIsDeletedToTrueAndRaiseEvent_WhenNotDefaultAndNoActiveOrders`, `Store_Delete_ShouldReturnCannotDeleteDefaultStoreError_WhenIsDefaultAndForceIsFalse`, `Store_Delete_ShouldDeleteDefaultStore_WhenForceIsTrue`, `Store_Delete_ShouldReturnHasActiveOrdersError_WhenHasActiveOrdersAndForceIsFalse`, `Store_Delete_ShouldDeleteStoreWithActiveOrders_WhenForceIsTrue`, `Store_Delete_ShouldReturnDeleted_WhenAlreadyDeleted`, `Store_Restore_ShouldSetIsDeletedToFalseAndRaiseEvent_WhenDeleted`, `Store_Restore_ShouldNotChangeStateOrRaiseEvent_WhenNotDeleted`.
    -   **Status**: ✅ Completed