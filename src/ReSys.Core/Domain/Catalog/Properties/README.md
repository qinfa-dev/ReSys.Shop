# Catalog.Properties Bounded Context

This document describes the `Catalog.Properties` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines and manages generic product properties (attributes) that can be associated with products. It allows for flexible definition of property types (e.g., short text, number, rich text) and their behavior (e.g., filterable, display location), enabling a dynamic and extensible product specification system within the catalog.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Properties` bounded context.

-   **Property**: A definable attribute or characteristic that can be assigned to a product (e.g., "Material", "Color", "Weight"). Represented by the `Property` aggregate.
-   **Property Kind**: The data type or input type of a property, determining how its value is stored and validated (e.g., `ShortText`, `Number`, `RichText`, `Boolean`).
-   **Filterable**: A boolean flag indicating if a property can be used for filtering products in a catalog or search interface.
-   **Display On**: Specifies where the property should be displayed in the user interface (e.g., product page, search results, both, or none).
-   **Position**: An integer indicating the display order of a property when presented in a list.
-   **Filter Param**: A URL-friendly key (slug) automatically generated or explicitly set, used for constructing filter queries based on this property.
-   **Metadata**: Additional, unstructured key-value data associated with a property, separated into `PublicMetadata` and `PrivateMetadata`.
-   **Product Property**: The specific value of a `Property` assigned to a particular `Product`. (Referenced from `Catalog.Products` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Property`**: This is the Aggregate Root. It defines the characteristics and behavior of a generic product attribute. It is responsible for its own lifecycle and ensuring the integrity of its definition.
    -   **Entities**: None directly owned by `Property` within this file. `ProductProperty` is a junction entity that links `Property` to `Product`, but `ProductProperty` is likely owned by the `Product` aggregate.
    -   **Value Objects**: None explicitly defined as separate classes. Attributes like `Name`, `Presentation`, `FilterParam`, `Kind`, `Filterable`, `DisplayOn`, `Position`, `PublicMetadata`, `PrivateMetadata` are intrinsic properties of the `Property` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `ProductProperty` (from `Core.Domain.Catalog.Products.Properties`): This entity represents the specific value of a `Property` for a given `Product`. It acts as a bridge between `Product` and `Property`.
-   `Product` (from `Core.Domain.Catalog.Products`): Referenced, as properties are ultimately applied to products.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. Utility methods like `GetValidationConditionForKind` and `GetMetaFieldType` provide logic related to `PropertyKind` but are part of the `Property` aggregate itself.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Properties` bounded context.

-   A `Property` cannot be deleted if it has associated `ProductProperty` entries. This ensures data integrity by preventing orphaned product property values. (Enforced by `Property.Delete()`).
-   `Name` and `Presentation` values are normalized (e.g., trimmed) upon creation and update to maintain consistency.
-   `FilterParam` is automatically generated as a URL-friendly slug from the `Name` if the property is `Filterable` and no explicit `FilterParam` is provided.
-   `Position` values are always non-negative, ensuring valid display ordering.
-   Validation rules for property values (e.g., length constraints, regex patterns for numbers) are determined by their `PropertyKind`.

---

## 🤝 Relationships & Dependencies

-   **`Property` to `ProductProperty`**: A one-to-many relationship, where a `Property` can be associated with many `ProductProperty` instances. `ProductProperty` acts as an explicit link to `Product` entities.
-   **`Property` to `Product`**: An indirect relationship established through the `ProductProperty` junction entity.
-   **Shared Kernel**: The `Property` aggregate inherits from `AuditableEntity` and implements several interfaces from `ReSys.Core.Common.Domain` (`IHasParameterizableName`, `IHasPosition`, `IHasMetadata`, `IHasDisplayOn`, `IHasFilterParam`, `IHasUniqueName`), leveraging common patterns for naming, positioning, metadata, display control, filtering, and uniqueness.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Property**: Define a new generic product attribute (e.g., "Material", "Weight") specifying its `Name`, `Presentation`, `PropertyKind`, `Filterable` status, `DisplayOn` location, and `Position`.
-   **Update Property**: Modify the definition of an existing property, including its name, presentation, kind, filterability, display settings, position, filter parameter, and metadata.
-   **Delete Property**: Remove a property from the system, provided there are no associated `ProductProperty` entries.
-   **Determine Validation Rules**: Retrieve appropriate validation conditions (min/max length, regex) based on the property's `PropertyKind`.
-   **Generate UI Input Type**: Suggest the appropriate HTML input element type based on the property's `PropertyKind` for UI rendering.
-   **Publish Domain Events**: Emit events for creation, update, and deletion of properties, as well as events to trigger updates on related products (`TouchAllProducts`, `EnsureProductPropertiesHaveFilterParams`) for cache invalidation or re-indexing.

---

## 📝 Usage Example

Here is an example of how to create a new `Property`.

```csharp
// In an Application Service

// 1. Define the parameters for the new property
var name = "material";
var presentation = "Material";
var kind = Property.PropertyKind.ShortText;
var filterable = true;

// 2. Create the Property entity using its factory method
var propertyResult = Property.Create(
    name: name,
    presentation: presentation,
    kind: kind,
    filterable: filterable,
    position: 1,
    displayOn: DisplayOn.Both
);

if (propertyResult.IsError)
{
    // Handle validation errors
    return;
}

var newProperty = propertyResult.Value;

// 3. Persist the new Property
// (Assuming a repository and Unit of Work pattern)
_propertyRepository.Add(newProperty);
await _unitOfWork.SaveChangesAsync();

// The Property.Events.Created domain event is now available on the
// newProperty object for dispatching.
```

---

## 📝 Considerations / Notes

-   The `Property` aggregate focuses solely on defining the *type* of attribute, while the actual *value* of that attribute for a specific product is managed by the `ProductProperty` entity within the `Catalog.Products` bounded context.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   Domain Events are crucial for maintaining consistency across related aggregates (e.g., `Product`) when a `Property` is updated or deleted.
-   The `FilterParam` provides a mechanism for building dynamic filtering capabilities in the application's frontend.
