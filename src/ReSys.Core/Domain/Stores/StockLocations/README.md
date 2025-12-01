# Stores.StockLocations Bounded Context

This document describes the `Stores.StockLocations` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the association of specific stock locations with individual stores. It enables fine-grained control over which inventory sources serve each storefront, allowing for prioritized sourcing of products and supporting diverse fulfillment strategies across a multi-store setup.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Stores.StockLocations` bounded context.

-   **Store Stock Location**: An explicit link indicating that a specific `StockLocation` is configured to supply products to a particular `Store`. Represented by the `StoreStockLocation` entity.
-   **Store**: The sales channel or storefront that draws inventory from a stock location. (Referenced from `Stores` Bounded Context).
-   **Stock Location**: The physical or logical place where inventory is held. (Referenced from `Inventories.Locations` Bounded Context).
-   **Priority**: An integer value indicating the preference or order in which a store should source products from this stock location, relative to others.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `StoreStockLocation` is an entity that is owned by the `Store` aggregate (and indirectly by `StockLocation`).

### Entities (not part of an Aggregate Root, if any)

-   **`StoreStockLocation`**: This is the central entity of this bounded context. It serves as a junction entity for the many-to-many relationship between `Store` and `StockLocation`, primarily controlling sourcing `Priority`. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `StockLocationId`, `StoreId`, and `Priority` are intrinsic attributes of the `StoreStockLocation` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Stores.StockLocations` bounded context.

-   A `StoreStockLocation` must always be associated with a valid `StoreId` and `StockLocationId`.
-   A specific `StockLocation` can only be linked to a `Store` once (ensuring uniqueness).
-   `Priority` values must be within a defined range (e.g., `Constraints.MinPriority` to `Constraints.MaxPriority`).
-   `StoreStockLocation` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`StoreStockLocation` to `Store`**: Many-to-one relationship. `StoreStockLocation` is owned by `Store` (from `Stores`).
-   **`StoreStockLocation` to `StockLocation`**: Many-to-one relationship. `StoreStockLocation` links to `StockLocation` (from `Inventories.Locations`).
-   **Shared Kernel**: `StoreStockLocation` inherits from `AuditableEntity` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Store Stock Location**: Establish a new link between a store and a stock location, defining its initial priority.
-   **Update Priority**: Modify the sourcing `Priority` of a linked stock location for the store.
-   **Delete Store Stock Location**: Remove a stock location's association with a specific store.

---

## 📝 Considerations / Notes

-   `StoreStockLocation` primarily serves as a junction entity, managing the relationship between `Store` and `StockLocation`. Its lifecycle is managed by the `Store` aggregate.
-   The `Priority` is crucial for implementing complex inventory sourcing logic (e.g., fulfilling from the closest or most cost-effective warehouse first).
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
