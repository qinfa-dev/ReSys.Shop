# Stores.Products Bounded Context

This document describes the `Stores.Products` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the explicit association of products with individual stores. It enables fine-grained control over which products are available in each storefront, their visibility, and whether they are featured, thereby allowing for customized product catalogs and merchandising strategies across a multi-store setup.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Stores.Products` bounded context.

-   **Store Product**: An explicit link indicating that a specific `Product` is configured for use within a particular `Store`. Represented by the `StoreProduct` entity.
-   **Store**: The sales channel or storefront where the product is available. (Referenced from `Stores` Bounded Context).
-   **Product**: The item being associated with a store. (Referenced from `Catalog.Products` Bounded Context).
-   **Visible**: A boolean flag indicating if the linked product is active and displayed in this specific store.
-   **Featured**: A boolean flag indicating if the product is highlighted as a featured item in this store.
-   **Position**: The display order of the product within a store's product listings, if applicable.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `StoreProduct` is an entity that is owned by the `Store` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`StoreProduct`**: This is the central entity of this bounded context. It serves as a junction entity for the many-to-many relationship between `Store` and `Product`, carrying information about visibility, featured status, and display position. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `StoreId`, `ProductId`, `Visible`, `Featured`, and `Position` are intrinsic attributes of the `StoreProduct` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Stores.Products` bounded context.

-   A `StoreProduct` must always be associated with a valid `StoreId` and `ProductId`.
-   A specific `Product` can only be linked to a `Store` once (ensuring uniqueness).
-   `Position` values are always non-negative.
-   `StoreProduct` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`StoreProduct` to `Store`**: Many-to-one relationship. `StoreProduct` is owned by `Store` (from `Stores`).
-   **`StoreProduct` to `Product`**: Many-to-one relationship. `StoreProduct` links to `Product` (from `Catalog.Products`).
-   **Shared Kernel**: `StoreProduct` inherits from `AuditableEntity` and implements `IHasPosition` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing and positioning. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Store Product**: Establish a new link between a store and a product, defining its initial visibility and featured status.
-   **Update Product Settings**: Modify the `Visible` or `Featured` status of a linked product within the store.
-   **Delete Store Product**: Remove a product's association with a specific store.

---

## 📝 Considerations / Notes

-   `StoreProduct` primarily serves as a junction entity, managing the relationship between `Store` and `Product`. Its lifecycle is managed by the `Store` aggregate.
-   The `Visible` and `Featured` flags are crucial for merchandising and tailoring product offerings per store.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
