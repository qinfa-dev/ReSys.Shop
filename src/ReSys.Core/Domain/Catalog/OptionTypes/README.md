# Catalog.OptionTypes Bounded Context

This document describes the `Catalog.OptionTypes` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the definition of product option types (e.g., "Color", "Size") and their corresponding values (e.g., "Red", "Blue", "Small", "Large"). It ensures that product variants can be consistently defined and managed based on these options, providing a robust system for defining configurable product attributes and managing specific option choices.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.OptionTypes` bounded context.

-   **Option Type**: A classification for a product characteristic (e.g., Color, Size). Represented by the `OptionType` aggregate.
-   **Option Value**: A specific instance or choice within an Option Type (e.g., Red, Blue for Color; Small, Large for Size). Represented by the `OptionValue` entity.
-   **Presentation**: A user-friendly display name for an Option Type or Option Value.
-   **Position**: The order in which an Option Type or Option Value should be displayed.
-   **Metadata**: Additional, unstructured data associated with an Option Type or Option Value, separated into `PublicMetadata` and `PrivateMetadata`.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`OptionType`**: This is the Aggregate Root. It manages a collection of `OptionValue`s and ensures their consistency.
    -   **Entities**: `OptionValue` (owned by `OptionType`). Represents a specific choice for an `OptionType`.
    -   **Value Objects**: None explicitly defined as separate classes within this aggregate. Properties like `Name`, `Presentation`, `Position` act as attributes.

### Entities (not part of an Aggregate Root, if any)

-   None. `OptionValue` is an entity within the `OptionType` aggregate.
-   **`ProductOptionType`**: A junction entity representing the many-to-many relationship between a `Product` and an `OptionType`. It indicates which option types are applicable to a specific product.
-   **`VariantOptionValue`**: A junction entity representing the many-to-many relationship between a `Variant` and an `OptionValue`. It indicates that a specific product variant has a particular option value assigned to it.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `OptionType` aggregate encapsulates logic for adding and removing `OptionValue`s.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.OptionTypes` bounded context.

-   An `OptionType` cannot be deleted if it has associated `OptionValue`s. (Enforced by `OptionType.Delete()`)
-   An `OptionValue` cannot be deleted if it is associated with any existing `VariantOptionValues`. (Enforced by `OptionValue.Delete()`)
-   `OptionValue` names must be unique within an `OptionType`. (Enforced by database unique index).
-   `Position` values for `OptionType` and `OptionValue` are non-negative.
-   `Name` and `Presentation` values are normalized (e.g., trimmed) upon creation and update.

---

## 🤝 Relationships & Dependencies

-   **`OptionType` to `OptionValue`**: A one-to-many relationship, where an `OptionType` can have multiple `OptionValue`s. `OptionValue`s are owned by their parent `OptionType` aggregate.
-   **`OptionType` to `ProductOptionType`**: A many-to-many relationship (via `ProductOptionType` junction entity) linking `OptionType` to `Product` entities.
-   **`OptionValue` to `VariantOptionValue`**: A many-to-many relationship (via `VariantOptionValue` junction entity) linking `OptionValue` to `Variant` entities.
-   **Shared Kernel**: Both `OptionType` and `OptionValue` inherit from `Aggregate` (which in turn inherits from `AuditableEntity`) and implement interfaces like `IHasParameterizableName`, `IHasPosition`, and `IHasMetadata` from the `ReSys.Core.Common` project, providing common behaviors and attributes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Option Type**: Define a new product characteristic (e.g., "Color").
-   **Manage Option Values**: Add, update, or remove specific choices for an `OptionType` (e.g., "Red", "Blue" for "Color").
-   **Update Option Type/Value Details**: Modify properties like name, presentation, position, or metadata.
-   **Delete Option Type**: Remove an `OptionType`, provided it has no associated `OptionValue`s.
-   **Associate Option Type with Product**: Link an `OptionType` to a `Product` to define its configurable attributes.
-   **Associate Option Value with Product Variant**: Link an `OptionValue` to a `Product Variant` to specify its characteristics.

---

## 📝 Usage Example

Here is an example of how to create an `OptionType` for "Color" and add several `OptionValue`s to it.

```csharp
// In an Application Service

// 1. Create the "Color" OptionType
var colorOptionTypeResult = OptionType.Create(
    name: "color",
    presentation: "Color",
    position: 1,
    filterable: true
);

if (colorOptionTypeResult.IsError)
{
    // Handle validation errors
    return;
}

var colorOptionType = colorOptionTypeResult.Value;

// 2. Create OptionValues for "Color"
var redResult = OptionValue.Create(colorOptionType.Id, "red", "Red");
var greenResult = OptionValue.Create(colorOptionType.Id, "green", "Green");
var blueResult = OptionValue.Create(colorOptionType.Id, "blue", "Blue");

// 3. Add the OptionValues to the OptionType
if (!redResult.IsError) 
    colorOptionType.AddOptionValue(redResult.Value);

if (!greenResult.IsError)
    colorOptionType.AddOptionValue(greenResult.Value);

if (!blueResult.IsError)
    colorOptionType.AddOptionValue(blueResult.Value);

// 4. Persist the OptionType aggregate, which will also save the new OptionValues
// (Assuming a repository and Unit of Work pattern)
_optionTypeRepository.Add(colorOptionType);
await _unitOfWork.SaveChangesAsync();

// Domain events like OptionType.Events.Created and OptionType.Events.OptionValueAdded
// will be available on the colorOptionType object to be dispatched.
```

---

## 📝 Considerations / Notes

-   The `OptionType` aggregate is responsible for maintaining the integrity of its `OptionValue`s.
-   The use of `ErrorOr` for return types indicates a functional approach to error handling within the domain.
-   Domain Events (`Created`, `Updated`, `Deleted`, `OptionValueAdded`, `OptionValueRemoved`) are published for significant state changes, enabling a decoupled architecture.
-   The `Product` and `Variant` classes mentioned in relationships are part of other bounded contexts (likely `Catalog.Products` and `Catalog.Products.Variants`).
