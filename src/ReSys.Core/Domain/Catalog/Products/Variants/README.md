# Catalog.Products.Variants Bounded Context

This document describes the `Catalog.Products.Variants` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the different variations of a product that can be sold. It encompasses the definition of distinct SKUs, their physical attributes (weight, dimensions), pricing, inventory tracking settings, and how they are differentiated by option values. This ensures that a single product can be offered in multiple configurable forms (e.g., different colors, sizes) each with its own specific details and availability.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Variants` bounded context.

-   **Variant**: A specific, sellable version of a product. It can have unique properties like SKU, price, and stock. Represented by the <see cref="Variant"/> aggregate.
-   **Product**: The parent entity to which the variant belongs. (Referenced from `Catalog.Products` Bounded Context).
-   **Is Master**: A boolean flag indicating if this variant is the primary or default variant for its product.
-   **SKU (Stock Keeping Unit)**: A unique identifier for a product variant, used for inventory management.
-   **Barcode**: A machine-readable optical label often used for scanning.
-   **Physical Attributes**: Properties like <c>Weight</c>, <c>Height</c>, <c>Width</c>, <c>Depth</c>, along with their respective <c>DimensionsUnit</c> and <c>WeightUnit</c>.
-   **Track Inventory**: A boolean flag indicating if stock levels should be monitored for this variant.
-   **Cost Price**: The cost of acquiring or producing this variant.
-   **Cost Currency**: The currency in which the <c>CostPrice</c> is denominated.
-   **Position**: The display order of the variant among other variants for the same product.
-   **Discontinue On**: A date after which the variant is no longer available for sale.
-   **Deleted At / Deleted By / Is Deleted**: Fields for soft-deletion, allowing the variant to be marked as deleted without permanent removal.
-   **Option Value**: A specific choice within an <see cref="OptionType"/> (e.g., "Red" for "Color"). (Referenced from `Catalog.OptionTypes` Bounded Context).
-   **Variant Option Value**: A junction entity (<see cref="VariantOptionValue"/>) linking a <see cref="Variant"/> to a specific <see cref="OptionValue"/>.
-   **Purchasable**: A computed property indicating if the variant can be added to an order (in stock or backorderable, and has a price).
-   **In Stock**: A computed property indicating if the variant has available inventory or is backorderable.
-   **Backorderable**: A flag indicating if the variant can be ordered even if out of physical stock.
-   **Backordered**: A computed property indicating if the variant is out of stock but can still be ordered.
-   **Can Supply**: A computed property indicating if the variant can be supplied (in stock or backorderable).
-   **Options Text**: A human-readable string combining the variant's assigned option values (e.g., "Red, Small").
-   **Descriptive Name**: A human-readable name for the variant (e.g., "T-Shirt - Red, Small").
-   **Metadata**: Additional, unstructured data (<c>PublicMetadata</c>/<c>PrivateMetadata</c>) associated with the variant.
-   **Row Version**: A timestamp used for optimistic concurrency control (<c>RowVersion</c>).

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

-   A <see cref="Variant"/> must always be associated with a valid <c>ProductId</c>.
-   A <c>Master Variant</c> (<see cref="Variant.IsMaster"/> = <c>true</c>) cannot have associated <see cref="OptionValue"/>s. Attempts to add option values to a master variant will result in <see cref="Variant.Errors.MasterCannotHaveOptionValues"/>.
-   A <c>Master Variant</c> cannot be deleted. Every product must have exactly one master variant for fallback configuration. Attempts to delete a master variant will result in <see cref="Variant.Errors.MasterCannotBeDeleted"/>.
-   A <see cref="Variant"/> cannot be hard-deleted if it is attached to completed orders. Variants with order history must be soft-deleted to preserve order data. The <see cref="Variant.Delete()"/> method enforces this, returning <see cref="Variant.Errors.CannotDeleteWithCompleteOrders"/>.
-   <c>SKU</c> and <c>Barcode</c> must adhere to maximum length constraints (<see cref="Variant.Constraints.SkuMaxLength"/>, <see cref="Variant.Constraints.BarcodeMaxLength"/>) and be unique within a product (if provided).
-   Physical attributes (<c>Weight</c>, <c>Height</c>, <c>Width</c>, <c>Depth</c>) and their units (<c>DimensionsUnit</c>, <c>WeightUnit</c>) must be valid as defined in <see cref="Variant.Constraints.ValidDimensionUnits"/> and <see cref="Variant.Constraints.ValidWeightUnits"/>. Invalid units will result in <see cref="Variant.Errors.InvalidDimensionUnit"/> or <see cref="Variant.Errors.InvalidWeightUnit"/>.
-   <c>CostPrice</c> and <c>Amount</c> (for <see cref="Price"/> entities) must be non-negative. Invalid prices will result in <see cref="Variant.Errors.InvalidPrice"/>.
-   <c>Position</c> values are always non-negative.
-   <see cref="Variant"/> instances support soft-deletion, meaning they are logically marked as deleted but retained in the database for historical purposes.
-   A <see cref="Variant"/> can only have one <see cref="Price"/> per <c>Currency</c>.
-   A <see cref="Variant"/> can only have one <see cref="StockItem"/> per <c>StockLocation</c>.
-   An <see cref="OptionValue"/> can only be linked to a <see cref="Variant"/> if its <see cref="OptionType"/> is associated with the parent <see cref="Product"/>. Invalid associations will result in <see cref="Variant.Errors.InvalidOptionValue"/>.
-   Discontinued variants cannot be purchased (controlled by the <c>DiscontinueOn</c> property and <see cref="Variant.Discontinue()"/> method).

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

-   **Create Variant**: Instantiate a new <see cref="Variant"/> using <see cref="Variant.Create(Guid, bool, string?, string?, decimal?, decimal?, decimal?, decimal?, string?, string?, decimal?, string?, bool, int?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>. This method handles initial validation and sets up physical attributes, pricing, and inventory tracking.
-   **Update Variant Details**: Modify various attributes such as <c>SKU</c>, <c>Barcode</c>, physical dimensions, cost, inventory tracking settings, or <c>Position</c> using <see cref="Variant.Update(string?, string?, decimal?, decimal?, decimal?, decimal?, string?, string?, bool?, decimal?, string?, int?, DateTimeOffset?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>.
-   **Set Price**: Define or update the selling <see cref="Price"/> for the variant in a specific <c>Currency</c> using <see cref="Variant.SetPrice(decimal?, decimal?, string)"/>. This method creates or updates <see cref="Price"/> entities.
-   **Manage Option Values**:
    -   <see cref="Variant.AddOptionValue(OptionValue?)"/>: Associate specific <see cref="OptionValue"/>s (e.g., "Red", "Small") with the variant. This method ensures that master variants do not have option values.
    -   <see cref="Variant.RemoveOptionValue(Guid)"/>: Disassociate an <see cref="OptionValue"/> from the variant.
-   **Manage Images**:
    -   <see cref="Variant.AddAsset(ProductImage?)"/>: Add a new image specific to this variant.
    -   <see cref="Variant.RemoveAsset(Guid)"/>: Remove an image from this variant.
-   **Soft Delete Variant**: Mark a variant as deleted (<see cref="Variant.Delete()"/>), preventing its further use while retaining historical data and enforcing rules against deleting master variants or variants with completed orders.
-   **Discontinue Variant**: Mark a variant as no longer available for purchase using <see cref="Variant.Discontinue()"/>, setting its <c>DiscontinueOn</c> date.
-   **Check Availability**: Determine if the variant is <c>Purchasable</c>, <c>InStock</c>, <c>Backorderable</c>, or <c>Available</c> through its computed properties.

---

## 📝 Considerations / Notes

-   The <see cref="Variant"/> aggregate is a crucial root for managing product variations and their sellable attributes. Its integrity is vital for accurate inventory, pricing, and fulfillment.
-   The complex interplay between <see cref="Variant"/>, <see cref="Price"/>, and <see cref="StockItem"/> is managed within this domain, often orchestrated by application services that use these aggregates.
-   Domain Events are extensively used to communicate changes in variant state (e.g., <see cref="Variant.Events.VariantAdded"/>, <see cref="Variant.Events.StockSet"/>, <see cref="Variant.Events.VariantPriceChanged"/>) to other parts of the system, promoting loose coupling.
-   The <c>IsMaster</c> flag is a key differentiator, and specific rules apply to the master variant (e.g., cannot have option values, cannot be deleted), ensuring a consistent product definition.
-   The use of <see cref="ErrorOr"/> for return types promotes a functional approach to error handling, making business rule violations explicit.
-   Optimistic concurrency control is supported via the <c>RowVersion</c> property, helping to prevent conflicting updates in a multi-user environment.
-   Physical dimensions (<c>Weight</c>, <c>Height</c>, <c>Width</c>, <c>Depth</c>) and their units are critical for accurate shipping cost calculations and warehouse management.

---
