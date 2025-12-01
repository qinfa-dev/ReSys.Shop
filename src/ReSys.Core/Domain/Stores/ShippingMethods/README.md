# Stores.ShippingMethods Bounded Context

This document describes the `Stores.ShippingMethods` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the association of specific shipping methods with individual stores. It enables fine-grained control over which shipping options are available in each storefront, allowing for store-specific pricing and activation statuses, thereby supporting diverse logistics and delivery strategies across a multi-store setup.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Stores.ShippingMethods` bounded context.

-   **Store Shipping Method**: An explicit link indicating that a `ShippingMethod` is configured for use within a specific `Store`. Represented by the `StoreShippingMethod` entity.
-   **Store**: The sales channel or storefront where the shipping method is made available. (Referenced from `Stores` Bounded Context).
-   **Shipping Method**: The delivery option (e.g., Standard, Express) being associated with a store. (Referenced from `Shipping` Bounded Context).
-   **Available**: A boolean flag indicating if the linked shipping method is active and accessible in this specific store.
-   **Store Base Cost**: An optional store-specific base cost for the shipping method, overriding the global `ShippingMethod.BaseCost`.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `StoreShippingMethod` is an entity that is owned by the `Store` aggregate (and indirectly by `ShippingMethod`).

### Entities (not part of an Aggregate Root, if any)

-   **`StoreShippingMethod`**: This is the central entity of this bounded context. It serves as a junction entity for the many-to-many relationship between `Store` and `ShippingMethod`, primarily controlling availability and store-specific costs. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `StoreId`, `ShippingMethodId`, `Available`, and `StoreBaseCost` are intrinsic attributes of the `StoreShippingMethod` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Stores.ShippingMethods` bounded context.

-   A `StoreShippingMethod` must always be associated with a valid `StoreId` and `ShippingMethodId`.
-   A specific `ShippingMethod` can only be linked to a `Store` once (ensuring uniqueness).
-   `StoreShippingMethod` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`StoreShippingMethod` to `Store`**: Many-to-one relationship. `StoreShippingMethod` is owned by `Store` (from `Stores`).
-   **`StoreShippingMethod` to `ShippingMethod`**: Many-to-one relationship. `StoreShippingMethod` links to `ShippingMethod` (from `Shipping`).
-   **Shared Kernel**: `StoreShippingMethod` inherits from `AuditableEntity` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Store Shipping Method**: Establish a new link between a store and a shipping method, defining its initial availability and optional store-specific base cost.
-   **Update Availability and Cost**: Modify whether a linked shipping method is `Available` in the store or update its `StoreBaseCost`.
-   **Delete Store Shipping Method**: Remove a shipping method's association with a specific store.

---

## 📝 Considerations / Notes

-   `StoreShippingMethod` primarily serves as a junction entity, managing the relationship between `Store` and `ShippingMethod`. Its lifecycle is managed by the `Store` aggregate.
-   The `Available` flag and `StoreBaseCost` property are crucial for dynamically enabling/disabling and pricing delivery options per store.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
