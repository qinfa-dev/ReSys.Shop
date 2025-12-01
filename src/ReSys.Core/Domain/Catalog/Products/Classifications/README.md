# Catalog.Products.Classifications Bounded Context

This document describes the `Catalog.Products.Classifications` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the explicit association of products with specific taxons (categories) within a taxonomy. It defines how a product is categorized and its position within that classification, enabling structured product listings and navigation within the catalog.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Classifications` bounded context.

-   **Classification**: An explicit link between a `Product` and a `Taxon`, indicating that a product belongs to a specific category or classification. Represented by the `Classification` entity.
-   **Product**: The item being classified. (Referenced from `Catalog.Products` Bounded Context).
-   **Taxon**: A node in a hierarchical taxonomy (category). (Referenced from `Catalog.Taxonomies` Bounded Context).
-   **Position**: The display order of the product within the taxon's product list.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `Classification` is an entity that is owned by the `Product` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`Classification`**: This is the central entity of this bounded context. It represents the explicit many-to-many relationship between a `Product` and a `Taxon`. It is an `AuditableEntity` and tracks its display `Position`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Position`, `ProductId`, and `TaxonId` are intrinsic attributes of the `Classification` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products.Classifications` bounded context.

-   A `Classification` must always be associated with a valid `ProductId` and `TaxonId`.
-   A specific `Product` cannot be linked to the same `Taxon` multiple times (ensuring uniqueness of classification).
-   `Position` values are always non-negative, ensuring valid display ordering.

---

## 🤝 Relationships & Dependencies

-   **`Classification` to `Product`**: Many-to-one relationship. `Classification` is owned by `Product` (from `Catalog.Products`).
-   **`Classification` to `Taxon`**: Many-to-one relationship. `Classification` links to `Taxon` (from `Catalog.Taxonomies`).
-   **Shared Kernel**: `Classification` inherits from `AuditableEntity` and implements `IHasPosition` (from `SharedKernel.Domain`), leveraging common patterns for auditing and positioning.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Classification**: Establish a new link between a product and a taxon, defining the product's position within that taxon.
-   **Delete Classification**: Remove a product's association with a specific taxon.

---

## 📝 Considerations / Notes

-   `Classification` primarily serves as a junction entity, managing the relationship between `Product` and `Taxon`. Its lifecycle is managed by the `Product` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   This entity contributes to the overall categorization system, allowing products to appear in multiple categories and enabling ordered displays.
