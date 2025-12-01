# Stores.PaymentMethods Bounded Context

This document describes the `Stores.PaymentMethods` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the association of specific payment methods with individual stores. It enables fine-grained control over which payment options are available in each storefront, allowing for store-specific configurations and activation statuses, thereby supporting diverse payment strategies across a multi-store setup.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Stores.PaymentMethods` bounded context.

-   **Store Payment Method**: An explicit link indicating that a `PaymentMethod` is configured for use within a specific `Store`. Represented by the `StorePaymentMethod` entity.
-   **Store**: The sales channel or storefront where the payment method is made available. (Referenced from `Stores` Bounded Context).
-   **Payment Method**: The payment option (e.g., Credit Card, PayPal) being associated with a store. (Referenced from `Payments` Bounded Context).
-   **Available**: A boolean flag indicating if the linked payment method is active and accessible in this specific store.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `StorePaymentMethod` is an entity that is owned by the `Store` aggregate (and indirectly by `PaymentMethod`).

### Entities (not part of an Aggregate Root, if any)

-   **`StorePaymentMethod`**: This is the central entity of this bounded context. It serves as a junction entity for the many-to-many relationship between `Store` and `PaymentMethod`, primarily controlling availability. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `StoreId`, `PaymentMethodId`, and `Available` are intrinsic attributes of the `StorePaymentMethod` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Stores.PaymentMethods` bounded context.

-   A `StorePaymentMethod` must always be associated with a valid `StoreId` and `PaymentMethodId`.
-   A specific `PaymentMethod` can only be linked to a `Store` once (ensuring uniqueness).
-   `StorePaymentMethod` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`StorePaymentMethod` to `Store`**: Many-to-one relationship. `StorePaymentMethod` is owned by `Store` (from `Stores`).
-   **`StorePaymentMethod` to `PaymentMethod`**: Many-to-one relationship. `StorePaymentMethod` links to `PaymentMethod` (from `Payments`).
-   **Shared Kernel**: `StorePaymentMethod` inherits from `AuditableEntity` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Store Payment Method**: Establish a new link between a store and a payment method, defining its initial availability.
-   **Update Availability**: Modify whether a linked payment method is `Available` in the store.
-   **Delete Store Payment Method**: Remove a payment method's association with a specific store.

---

## 📝 Considerations / Notes

-   `StorePaymentMethod` primarily serves as a junction entity, managing the relationship between `Store` and `PaymentMethod`. Its lifecycle is managed by the `Store` aggregate.
-   The `Available` flag is crucial for dynamically enabling or disabling payment options per store.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
