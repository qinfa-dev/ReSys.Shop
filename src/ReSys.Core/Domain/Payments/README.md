# Payments Bounded Context

This document describes the `Payments` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines and manages various payment methods available within the system. It provides comprehensive functionalities for configuring payment types, their activation status, auto-capture settings, and store-specific associations. This enables flexible and secure payment processing, allowing the application to support diverse payment options and integrate with different payment gateways.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Payments` bounded context.

-   **Payment Method**: A specific way a customer can pay for an order or service (e.g., "Credit Card", "Cash On Delivery", "Bank Transfer"). It encapsulates the configuration and behavior of a payment option. Represented by the `PaymentMethod` aggregate.
-   **Payment Type**: An enumeration (`PaymentType`) categorizing the fundamental nature of the payment method (e.g., `CreditCard`, `DebitCard`, `BankTransfer`, `CashOnDelivery`, `Wallet`, `Check`).
-   **Name**: The unique, human-readable identifier for the payment method (e.g., "Visa/MasterCard", "PayPal").
-   **Description**: A detailed explanation of the payment method, often displayed to customers.
-   **Active**: A boolean flag indicating if the payment method is currently enabled and available for use in the system.
-   **Position**: An integer value indicating the display order of the payment method in lists or forms.
-   **Auto Capture**: A boolean flag indicating whether payments made with this method should be automatically captured (i.e., funds transferred immediately) or require manual capture (i.e., funds authorized but held for later capture).
-   **Settings**: A dictionary (`IDictionary<string, string?>`) used to store method-specific configuration parameters, such as API keys, merchant IDs, or other gateway-specific credentials.
-   **Store Payment Method**: The explicit association of a `Payment Method` with a specific `Store`, allowing for store-specific activation and configuration.
-   **Payment**: A record of a payment transaction made against an order. (Referenced from `Orders.Payments` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`PaymentMethod`**: This is the Aggregate Root. It defines a payment method and is responsible for managing its configuration, activation status, and associations with stores. It ensures the integrity and consistent state of the payment method's definition.
    -   **Entities**: `StorePaymentMethod` (owned by `PaymentMethod`). This entity represents the link between a `PaymentMethod` and a `Store`, allowing for store-specific settings or activation.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Description`, `Type`, `Active`, `Position`, `AutoCapture`, and `Settings` are intrinsic attributes of the `PaymentMethod` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Store` (from `Core.Domain.Stores`): Referenced by `StorePaymentMethod`, but managed by its own aggregate.
-   `Payment` (from `Core.Domain.Orders.Payments`): Referenced by `PaymentMethod` (via `Payment.PaymentMethodId`), but managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`PaymentType`**: An enumeration that categorizes payment methods into predefined types, facilitating consistent handling and integration.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All business logic related to the `PaymentMethod` is encapsulated within the aggregate itself.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Payments` bounded context.

-   `PaymentMethod` names must be unique across the system to prevent ambiguity.
-   `PaymentMethod` types are restricted to predefined values as defined in the `PaymentType` enumeration, ensuring valid categorization.
-   A `PaymentMethod` cannot be deleted if it is currently in use (i.e., associated with existing `Payment` transactions or active `StorePaymentMethod` links). This prevents data inconsistencies.
-   A `PaymentMethod` cannot be made active for a specific store if it is already active for that store, preventing duplicate entries.
-   `Position` values are non-negative, ensuring valid display ordering.
-   `PaymentMethod` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`PaymentMethod` to `StorePaymentMethod`**: One-to-many composition. The `PaymentMethod` aggregate manages its associations with `Store`s through `StorePaymentMethod` entities.
-   **`PaymentMethod` to `Payment`**: A one-to-many relationship, where `Payment` records (from `Orders.Payments`) reference a `PaymentMethod` via `Payment.PaymentMethodId`.
-   **External Aggregates**: References `Store` (from `Core.Domain.Stores`) for store-specific payment method configurations.
-   **Shared Kernel**: Inherits from `Aggregate` and implements `IHasUniqueName` and `IHasPosition` (from `SharedKernel.Domain.Entities`), leveraging common patterns for unique naming and positioning.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Payment Method**: Instantiate a new `PaymentMethod` with a unique name, its `PaymentType`, an optional description, and initial settings for `Active`, `AutoCapture`, and `Position`.
-   **Update Payment Method Details**: Modify the `Name`, `Description`, `Active` status, `AutoCapture` setting, or `Position` of an existing `PaymentMethod`.
-   **Configure Settings**: Update method-specific configuration `Settings` (e.g., API keys) for a `PaymentMethod`.
-   **Delete Payment Method**: Remove a `PaymentMethod` from the system, with built-in checks to prevent deletion if it has active dependencies.
-   **Publish Domain Events**: Emit domain events (`Created`, `Updated`, `Deleted`) to signal significant state changes in the payment method's lifecycle, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   This domain focuses on the *definition* and *configuration* of payment methods, while the actual processing of payments (e.g., interacting with payment gateways) is typically handled by application services or infrastructure components that utilize these `PaymentMethod` definitions.
-   The `Settings` dictionary provides a flexible way to store arbitrary configuration data for different payment gateway integrations without altering the core domain model.
-   The `AutoCapture` flag is a crucial business rule that dictates the immediate financial impact of a payment.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   The `StorePaymentMethod` entity allows for fine-grained control over which payment methods are available in which stores.
