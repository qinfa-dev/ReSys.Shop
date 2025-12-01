# Catalog.Products.Properties Bounded Context

This document describes the `Catalog.Products.Properties` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the specific values of generic properties (attributes) as they apply to individual products. It acts as a bridge between products and their defined properties, allowing for product-specific attribute values, ordering, and filterable parameters.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Properties` bounded context.

-   **Product Property**: A specific value assigned to a generic `Property` for a particular `Product`. It is a junction entity. Represented by the `ProductProperty` entity.
-   **Product**: The entity to which the property value is assigned. (Referenced from `Catalog.Products` Bounded Context).
-   **Property**: The generic attribute definition (e.g., "Material", "Color"). (Referenced from `Catalog.Properties` Bounded Context).
-   **Value**: The specific data for the property (e.g., "Cotton", "Red").
-   **Position**: The display order of this property for the product.
-   **Filter Param**: A URL-friendly slug generated from the `Value` (or explicitly set) for filtering purposes, if the `Property` is filterable.
-   **Is Filterable**: A computed property indicating if the associated generic `Property` is configured to be used for filtering.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `ProductProperty` is an entity that is owned by the `Product` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`ProductProperty`**: This is the central entity of this bounded context. It represents the explicit many-to-many relationship between a `Product` and a `Property`, holding the specific `Value` and display `Position`. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `ProductId`, `PropertyId`, `Position`, `Value`, and `FilterParam` are intrinsic attributes of the `ProductProperty` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products.Properties` bounded context.

-   A `ProductProperty` must always be associated with a valid `ProductId` and `PropertyId`.
-   A specific `Property` can only be linked to a `Product` once (ensuring uniqueness).
-   `Value` must adhere to a maximum length constraint (`Constraints.MaxValueLength`).
-   `Position` values are always non-negative.
-   `FilterParam` is automatically generated from the `Value` if the associated `Property` is `Filterable` and no explicit `FilterParam` is provided.

---

## 🤝 Relationships & Dependencies

-   **`ProductProperty` to `Product`**: Many-to-one relationship. `ProductProperty` is owned by `Product` (from `Catalog.Products`).
-   **`ProductProperty` to `Property`**: Many-to-one relationship. `ProductProperty` links to `Property` (from `Catalog.Properties`).
-   **Shared Kernel**: `ProductProperty` inherits from `AuditableEntity` and implements `IHasPosition` and `IHasFilterParam` (from `SharedKernel.Domain`), leveraging common patterns for auditing, positioning, and filter parameter management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Product Property**: Establish a new link between a product and a generic property, assigning a specific `Value` and display `Position`.
-   **Update Product Property**: Modify the `Value`, `Position`, or `FilterParam` of an existing product property.
-   **Delete Product Property**: Remove a property's association and value from a specific product.

---

## 📝 Considerations / Notes

-   `ProductProperty` primarily serves as a junction entity, managing the relationship between `Product` and `Property`. Its lifecycle is managed by the `Product` aggregate.
-   The domain model enforces that the `Value` is the direct string representation, while validation rules based on the `Property.Kind` would typically be applied at the application service layer.
-   The `FilterParam` is crucial for building dynamic filtering capabilities in a product catalog.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
