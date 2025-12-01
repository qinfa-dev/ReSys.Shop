# Catalog.Products.OptionTypes Bounded Context

This document describes the `Catalog.Products.OptionTypes` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the association between individual products and the specific option types (e.g., Color, Size) that are applicable to them. It ensures that products can be configured with the correct set of characteristics that differentiate their variants.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.OptionTypes` bounded context.

-   **Product Option Type**: An explicit link representing that a specific `OptionType` is available for a `Product`. Represented by the `ProductOptionType` entity.
-   **Product**: The entity to which the option type is being applied. (Referenced from `Catalog.Products` Bounded Context).
-   **Option Type**: A characteristic that differentiates product variants (e.g., Color, Size). (Referenced from `Catalog.OptionTypes` Bounded Context).
-   **Position**: The display order of the `OptionType` for a given `Product`.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `ProductOptionType` is an entity that is owned by the `Product` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`ProductOptionType`**: This is the central entity of this bounded context. It serves as a junction entity for the many-to-many relationship between `Product` and `OptionType`, carrying information about the display order. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Position`, `ProductId`, and `OptionTypeId` are intrinsic attributes of the `ProductOptionType` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products.OptionTypes` bounded context.

-   A `ProductOptionType` must always be associated with a valid `ProductId` and `OptionTypeId`.
-   A specific `OptionType` can only be linked to a `Product` once (ensuring uniqueness).
-   `Position` values are always non-negative, ensuring valid display ordering.

---

## 🤝 Relationships & Dependencies

-   **`ProductOptionType` to `Product`**: Many-to-one relationship. `ProductOptionType` is owned by `Product` (from `Catalog.Products`).
-   **`ProductOptionType` to `OptionType`**: Many-to-one relationship. `ProductOptionType` links to `OptionType` (from `Catalog.OptionTypes`).
-   **Shared Kernel**: `ProductOptionType` inherits from `AuditableEntity` and implements `IHasPosition` (from `SharedKernel.Domain`), leveraging common patterns for auditing and positioning.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Product Option Type**: Establish a new link between a product and an option type, defining its display order.
-   **Update Position**: Modify the display order of an associated option type for a product.
-   **Delete Product Option Type**: Remove an option type's association with a specific product.

---

## 📝 Considerations / Notes

-   `ProductOptionType` primarily serves as a junction entity, managing the relationship between `Product` and `OptionType`. Its lifecycle is managed by the `Product` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
