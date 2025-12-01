# Catalog.Products.Variants Bounded Context

This document describes the `Catalog.Products.Variants` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the different variations of a product that can be sold. It encompasses the definition of distinct SKUs, their physical attributes (weight, dimensions), pricing, inventory tracking settings, and how they are differentiated by option values. This ensures that a single product can be offered in multiple configurable forms (e.g., different colors, sizes) each with its own specific details and availability.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Variants` bounded context.

-   **Variant**: A specific, sellable version of a product. It can have unique properties like SKU, price, and stock. Represented by the `Variant` entity.
-   **Product**: The parent entity to which the variant belongs. (Referenced from `Catalog.Products` Bounded Context).
-   **Is Master**: A boolean flag indicating if this variant is the primary or default variant for its product.
-   **SKU (Stock Keeping Unit)**: A unique identifier for a product variant, used for inventory management.
-   **Barcode**: A machine-readable optical label often used for scanning.
-   **Physical Attributes**: Properties like `Weight`, `Height`, `Width`, `Depth`, along with their respective `DimensionsUnit` and `WeightUnit`.
-   **Track Inventory**: A boolean flag indicating if stock levels should be monitored for this variant.
-   **Cost Price**: The cost of acquiring or producing this variant.
-   **Cost Currency**: The currency in which the `CostPrice` is denominated.
-   **Position**: The display order of the variant among other variants for the same product.
-   **Discontinue On**: A date after which the variant is no longer available for sale.
-   **Deleted At / Deleted By / Is Deleted**: Fields for soft-deletion, allowing the variant to be marked as deleted without permanent removal.
-   **Option Value**: A specific choice within an `OptionType` (e.g., "Red" for "Color"). (Referenced from `Catalog.OptionTypes` Bounded Context).
-   **Variant Option Value**: A junction entity linking a `Variant` to a specific `OptionValue`.
-   **Purchasable**: A computed property indicating if the variant can be added to an order (in stock or backorderable, and has a price).
-   **In Stock**: A computed property indicating if the variant has available inventory or is backorderable.
-   **Backorderable**: A flag indicating if the variant can be ordered even if out of physical stock.
-   **Options Text**: A human-readable string combining the variant's assigned option values (e.g., "Red, Small").
-   **Descriptive Name**: A human-readable name for the variant (e.g., "T-Shirt - Red, Small").
-   **Metadata**: Additional, unstructured data associated with the variant.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Variant`**: This is an Aggregate Root. While often considered part of the `Product` aggregate, it manages its own lifecycle regarding pricing, stock, images, and option values. It is responsible for its own state consistency.
    -   **Entities**:
        -   `Price` (owned by `Variant`): Represents the pricing details (amount, currency, compare-at price) for this specific variant.
        -   `StockItem` (owned by `Variant`): Represents the inventory levels (quantity on hand, reserved) for this variant at a specific location.
        -   `ProductImage` (owned by `Variant`): Represents images specifically associated with this variant.
        -   `VariantOptionValue` (owned by `Variant`): A junction entity linking this `Variant` to an `OptionValue`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Sku`, `Barcode`, `Weight`, `Height`, `Width`, `Depth`, `DimensionsUnit`, `WeightUnit`, `TrackInventory`, `CostPrice`, `CostCurrency`, `Position`, `DiscontinueOn`, `PublicMetadata`, and `PrivateMetadata` are intrinsic attributes of the `Variant` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Product` (from `Core.Domain.Catalog.Products`): Referenced by `Variant`, but managed by its own aggregate.
-   `OptionValue` (from `Core.Domain.Catalog.OptionTypes`): Referenced by `VariantOptionValue`, but managed by its own aggregate.
-   `LineItem` (from `Core.Domain.Orders.LineItems`): References `Variant`, but managed by its own aggregate.
-   `Order` (from `Core.Domain.Orders`): References `Variant` via `LineItem`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. Complex logic for managing stock and prices is handled within the `Variant` aggregate and its owned entities (`StockItem`, `Price`).

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products.Variants` bounded context.

-   A `Variant` must always be associated with a valid `ProductId`.
-   A `Master Variant` cannot have associated `OptionValue`s.
-   A `Master Variant` cannot be deleted.
-   A `Variant` cannot be deleted if it is attached to completed orders.
-   `SKU` and `Barcode` must adhere to maximum length constraints.
-   Physical attributes (`Weight`, `Height`, `Width`, `Depth`) and their units (`DimensionsUnit`, `WeightUnit`) must be valid.
-   `CostPrice` and `Amount` must be non-negative.
-   `Position` values are always non-negative.
-   `Variant` instances can be soft-deleted.
-   A `Variant` can only have one `Price` per `Currency`.
-   A `Variant` can only have one `StockItem` per `StockLocation`.
-   An `OptionValue` can only be linked to a `Variant` if its `OptionType` is associated with the parent `Product`.

---

## 🤝 Relationships & Dependencies

-   **`Variant` to `Product`**: Many-to-one relationship. `Variant` is owned by `Product` (from `Catalog.Products`).
-   **`Variant` to `Price`**: One-to-many composition. `Variant` is the aggregate root for its `Price`s.
-   **`Variant` to `StockItem`**: One-to-many composition. `Variant` is the aggregate root for its `StockItem`s.
-   **`Variant` to `ProductImage`**: One-to-many composition. `Variant` is the aggregate root for its `ProductImage`s.
-   **`Variant` to `VariantOptionValue`**: One-to-many composition. `Variant` is the aggregate root for its `VariantOptionValue`s, which link to `OptionValue` (from `Catalog.OptionTypes`).
-   **External Aggregates**: References `OptionValue` (from `Catalog.OptionTypes`).
-   **Shared Kernel**: `Variant` inherits from `Aggregate` and implements `IHasPosition`, `ISoftDeletable`, `IHasMetadata` (from `SharedKernel.Domain`), leveraging common patterns for positioning, soft-deletion, and metadata. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Variant**: Instantiate a new `Variant` for a product, optionally marking it as the master variant, and setting its physical attributes, pricing, and stock tracking.
-   **Update Variant Details**: Modify attributes such as SKU, barcode, weight, dimensions, cost price, stock tracking, or position.
-   **Set Price**: Define or update the selling `Price` for the variant in a specific `Currency`.
-   **Manage Stock**: Attach `StockItem`s, update stock levels, reserve, and release quantities.
-   **Manage Option Values**: Associate or disassociate specific `OptionValue`s with the variant.
-   **Manage Images**: Add or remove `ProductImage`s specifically for this variant.
-   **Soft Delete Variant**: Mark a variant as deleted, preventing its further use but retaining historical data.
-   **Discontinue Variant**: Set a date after which the variant will no longer be available for sale.
-   **Check Availability**: Determine if the variant is `Purchasable`, `InStock`, or `Backorderable`.

---

## 📝 Considerations / Notes

-   `Variant` is a crucial aggregate root for managing product variations and their sellable attributes.
-   The complex interplay between `Variant`, `Price`, and `StockItem` is managed within this domain, often orchestrated by application services that use these aggregates.
-   Domain Events are extensively used to communicate changes in variant state (e.g., `VariantAdded`, `StockSet`, `VariantPriceChanged`) to other parts of the system.
-   The `IsMaster` flag is a key differentiator, and specific rules apply to the master variant (e.g., cannot have option values, cannot be deleted).
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
-   Concurrency control is supported via `RowVersion`.
