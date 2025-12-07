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

-   An <see cref="OptionType"/> cannot be deleted if it has associated <see cref="OptionValue"/>s. This rule is explicitly enforced by <see cref="OptionType.Delete()"/> to prevent data inconsistencies and orphaned records.
-   An <see cref="OptionValue"/> cannot be deleted if it is associated with any existing <see cref="VariantOptionValue"/>s (meaning it's actively used by a product variant). This ensures that existing product configurations remain valid.
-   <see cref="OptionValue"/> names must be unique within a given <see cref="OptionType"/>. This uniqueness constraint is typically enforced at the database level and through validation in the <see cref="OptionValue"/> factory/update methods.
-   <see cref="OptionType"/> and <see cref="OptionValue"/> <c>Position</c> values are always non-negative, ensuring valid display ordering in user interfaces.
-   <see cref="OptionType"/> and <see cref="OptionValue"/> <c>Name</c> and <c>Presentation</c> values are normalized (e.g., trimmed) upon creation and update to maintain consistency.

---

## 🤝 Relationships & Dependencies

-   **`OptionType` to `OptionValue`**: A one-to-many relationship, where an `OptionType` can have multiple `OptionValue`s. `OptionValue`s are owned by their parent `OptionType` aggregate.
-   **`OptionType` to `ProductOptionType`**: A many-to-many relationship (via `ProductOptionType` junction entity) linking `OptionType` to `Product` entities.
-   **`OptionValue` to `VariantOptionValue`**: A many-to-many relationship (via `VariantOptionValue` junction entity) linking `OptionValue` to `Variant` entities.
-   **Shared Kernel**: Both `OptionType` and `OptionValue` inherit from `Aggregate` (which in turn inherits from `AuditableEntity`) and implement interfaces like `IHasParameterizableName`, `IHasPosition`, and `IHasMetadata` from the `ReSys.Core.Common` project, providing common behaviors and attributes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Option Type**: Define a new product characteristic (e.g., "Color", "Size") using <see cref="OptionType.Create"/>. This initializes the option type and makes it available for managing its specific values.
-   **Update Option Type Details**: Modify properties like the name, presentation, position, filterability, or metadata of an <see cref="OptionType"/> using <see cref="OptionType.Update"/>.
-   **Delete Option Type**: Remove an <see cref="OptionType"/> from the system using <see cref="OptionType.Delete()"/>, provided it has no associated <see cref="OptionValue"/>s to prevent data loss.
-   **Create Option Value**: Add a specific choice (e.g., "Red", "Small") to an <see cref="OptionType"/> using <see cref="OptionValue.Create"/>. This value must be unique within its parent <see cref="OptionType"/>.
-   **Update Option Value Details**: Modify properties like the name, presentation, position, or metadata of an <see cref="OptionValue"/> using <see cref="OptionValue.Update"/>.
-   **Remove Option Value**: Detach an <see cref="OptionValue"/> from its parent <see cref="OptionType"/> using <see cref="OptionType.RemoveOptionValue(Guid)"/>. The <see cref="OptionValue"/> itself cannot be directly deleted if it is associated with any product variants.
-   **Associate Option Type with Product**: Link an <see cref="OptionType"/> to a <see cref="Product"/> (via <c>ProductOptionType</c> junction entity) to define its configurable attributes.
-   **Associate Option Value with Product Variant**: Link an <see cref="OptionValue"/> to a <see cref="Catalog.Products.Variants.Variant"/> (via <c>VariantOptionValue</c> junction entity) to specify its characteristics (e.g., a specific variant is "Blue" and "Large").
-   **Publish Domain Events**: <see cref="OptionType"/> and <see cref="OptionValue"/> publish events upon creation, update, and deletion, enabling a decoupled architecture for other services to react (e.g., for search indexing or cache invalidation).

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
