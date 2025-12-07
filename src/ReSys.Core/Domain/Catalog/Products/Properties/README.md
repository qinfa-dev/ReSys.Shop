# Catalog.Products.Properties Bounded Context

This document describes the `Catalog.Products.Properties` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the specific values of generic properties (attributes) as they apply to individual products. It acts as a bridge between products and their defined properties, allowing for product-specific attribute values, ordering, and filterable parameters.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Properties` bounded context.

-   **Product Property**: A specific value assigned to a generic <see cref="Property"/> for a particular <see cref="Product"/>. It is a junction entity. Represented by the <see cref="ProductProperty"/> entity.
-   **Product**: The item to which the property value is assigned. (Referenced from `Catalog.Products` Bounded Context).
-   **Property**: The generic attribute definition (e.g., "Material", "Color"). (Referenced from `Catalog.Properties` Bounded Context).
-   **Value**: The specific data for the property (e.g., "Cotton", "Red").
-   **Position**: The display order of this property for the product.
-   **Filter Param**: A URL-friendly slug generated from the <c>Value</c> (or explicitly set) for filtering purposes, if the <see cref="Property"/> is filterable.
-   **Is Filterable**: A computed property indicating if the associated generic <see cref="Property"/> is configured to be used for filtering.

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

-   A <see cref="ProductProperty"/> must always be associated with a valid <c>ProductId</c> and <c>PropertyId</c>. This is typically enforced during creation of the <see cref="ProductProperty"/> entity.
-   A specific <see cref="Property"/> can only be linked to a <see cref="Product"/> once. This uniqueness is usually enforced by the parent <see cref="Product"/> aggregate when <see cref="Product.AddProductProperty(ProductProperty)"/> is called, or by unique constraints in the persistence layer.
-   <c>Value</c> must adhere to a maximum length constraint (<see cref="ProductProperty.Constraints.MaxValueLength"/>).
-   <c>Position</c> values are always non-negative, ensuring valid display ordering, enforced during creation via <c>Math.Max(0, position)</c> and in the <see cref="ProductProperty.Update(string?, int?, string?)"/> method.
-   <c>FilterParam</c> is automatically generated from the <c>Value</c> if the associated <see cref="Property.Filterable"/> is true and no explicit <c>FilterParam</c> is provided. The <c>FilterParam</c> must also adhere to <see cref="ProductProperty.Constraints.FilterParamPattern"/>.

---

## 🤝 Relationships & Dependencies

-   **`ProductProperty` to `Product`**: Many-to-one relationship. `ProductProperty` is owned by `Product` (from `Catalog.Products`).
-   **`ProductProperty` to `Property`**: Many-to-one relationship. `ProductProperty` links to `Property` (from `Catalog.Properties`).
-   **Shared Kernel**: `ProductProperty` inherits from `AuditableEntity` and implements `IHasPosition` and `IHasFilterParam` (from `SharedKernel.Domain`), leveraging common patterns for auditing, positioning, and filter parameter management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Product Property**: Establish a new link between a product and a generic property using <see cref="ProductProperty.Create(Guid, Guid, string, int, bool, string?)"/>. This method assigns a specific <c>Value</c> and display <c>Position</c>. The actual addition to a <see cref="Product"/> aggregate is done via <see cref="Product.AddProductProperty(ProductProperty)"/>.
-   **Update Product Property**: Modify the <c>Value</c>, <c>Position</c>, or <c>FilterParam</c> of an existing product property using <see cref="ProductProperty.Update(string?, int?, string?)"/>. This supports partial updates.
-   **Delete Product Property**: Signal the removal of a property's association and value from a specific product using <see cref="ProductProperty.Delete()"/>. The actual removal from the product's collection is managed by <see cref="Product.RemoveProperty(Guid)"/> method in the parent <see cref="Product"/> aggregate.

---

## 📝 Considerations / Notes

-   `ProductProperty` primarily serves as a junction entity, managing the relationship between `Product` and `Property`. Its lifecycle is managed by the `Product` aggregate.
-   The domain model enforces that the `Value` is the direct string representation, while validation rules based on the `Property.Kind` would typically be applied at the application service layer.
-   The `FilterParam` is crucial for building dynamic filtering capabilities in a product catalog.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
