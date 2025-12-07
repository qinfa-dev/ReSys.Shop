# Inventories Bounded Context

This document describes the `Inventories` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the comprehensive tracking and movement of product stock across various physical or logical locations within the system. It provides core functionalities for maintaining accurate stock levels, handling reservations, performing adjustments, and orchestrating transfers, thereby ensuring precise inventory counts and supporting diverse fulfillment and supply chain strategies.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Inventories` bounded context.

-   **Stock Item**: A record representing the quantity of a specific product `Variant` at a particular `Stock Location`. It is the fundamental unit of inventory tracking. Represented by the `StockItem` aggregate.
-   **Stock Location**: A physical or logical place where inventory is held (e.g., a warehouse, a retail store, a staging area). Represented by the `StockLocation` aggregate.
-   **Quantity On Hand**: The actual physical count of a `Stock Item` currently present at a `Stock Location`.
-   **Quantity Reserved**: The portion of `Quantity On Hand` that has been allocated for pending orders or other commitments but has not yet been shipped or moved.
-   **Count Available**: The quantity of a `Stock Item` that is immediately available for new orders or allocations (`QuantityOnHand - QuantityReserved`).
-   **Backorderable**: A boolean flag indicating if a `Stock Item` can be ordered by customers even if its `Count Available` is zero or negative.
-   **In Stock**: A computed property indicating if a `Stock Item` has `Count Available` greater than zero or is `Backorderable`.
-   **Stock Movement**: A historical record of any change in the `QuantityOnHand` or `QuantityReserved` of a `Stock Item`, detailing the quantity, originator, action, and reason.
-   **Movement Originator**: The source or cause of a `Stock Movement` (e.g., `Order`, `Adjustment`, `StockTransfer`, `Shipment`, `Return`, `Supplier`).
-   **Movement Action**: The specific type of action performed in a `Stock Movement` (e.g., `Received`, `Sold`, `Reserved`, `Released`, `Damaged`, `Lost`).
-   **Stock Transfer**: A process that orchestrates the movement of stock between two `Stock Location`s or the receipt of stock from an external supplier into a `Stock Location`. Represented by the `StockTransfer` aggregate.
-   **Source Location**: The `Stock Location` from which stock is dispatched during a `Stock Transfer`.
-   **Destination Location**: The `Stock Location` to which stock is received during a `Stock Transfer`.
-   **Number Generator**: A utility responsible for generating unique, sequential reference numbers for inventory-related transactions (e.g., stock transfers).
-   **Track Inventory Levels**: A global configuration setting (`Config.TrackInventoryLevels`) that enables or disables the entire inventory tracking system.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`StockLocation`**: This is an Aggregate Root. It represents a physical or logical location for inventory and is responsible for managing its associated `StockItem`s. It ensures the integrity of stock within its boundaries and orchestrates operations like `Restock` and `Unstock`.
    -   **Entities**: `StockItem` (owned by `StockLocation`). Represents the inventory of a specific product variant at this location.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Presentation`, `Active`, `Default`, address components (`Address1`, `City`, `Zipcode`), `Phone`, `Company`, `PublicMetadata`, and `PrivateMetadata` act as intrinsic attributes.

-   **`StockItem`**: This is also an Aggregate Root. It represents the inventory of a specific product `Variant` at a `StockLocation`. It manages its own quantities (`QuantityOnHand`, `QuantityReserved`) and records all `StockMovement`s affecting it. It ensures that stock levels remain consistent. It also manages backordered `InventoryUnit`s, automatically filling them when stock is replenished.
    -   **Entities**: `StockMovement` (owned by `StockItem`). A historical record of changes to the `StockItem`'s quantities.
    -   **Related Aggregates**: `InventoryUnit` (order fulfillment tracking in Orders domain)
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Sku`, `QuantityOnHand`, `QuantityReserved`, `Backorderable`, `PublicMetadata`, and `PrivateMetadata` act as intrinsic attributes.

-   **`StockTransfer`**: This is an Aggregate Root. It orchestrates the complex process of moving stock between `StockLocation`s or receiving stock from external vendors. It ensures that all necessary `StockMovement`s are correctly recorded at the affected `StockItem`s.
    -   **Entities**: `StockMovement` (orchestrated by `StockTransfer` but ultimately owned by `StockItem`).
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Number` (generated), and `Reference` act as intrinsic attributes.

-   **`InventoryUnit`** (from `Orders.Orders` boundary, tracked here for fulfillment): Represents a trackable unit of inventory associated with an order line item. It progresses through fulfillment states: `OnHand` → `Shipped` → optionally `Returned`. One line item with quantity N creates N inventory units for granular tracking.
    -   **Related Entities**: Links to `Variant`, `Order`, `LineItem`, `Shipment`, `StockLocation`
    -   **Related Domain**: Orders, Returns
    -   **Purpose**: Enables unit-level fulfillment tracking and return processing

### Entities (not part of an Aggregate Root, if any)

-   `Variant` (from `Core.Domain.Catalog.Products.Variants`): Referenced by `StockItem` to identify the product being stocked, but `Variant` is managed by its own aggregate.
-   `Country` (from `Core.Domain.Location`): Referenced by `StockLocation` for geographical context, but `Country` is managed by its own aggregate.
-   `State` (from `Core.Domain.Location`): Referenced by `StockLocation` for geographical context, but `State` is managed by its own aggregate.
-   `Store` (from `Core.Domain.Stores`): Referenced by `StockLocationStore`, but `Store` is managed by its own aggregate.
-   `StockLocationStore` (from `Core.Domain.Stores.StockLocations`): A junction entity linking a `StockLocation` to a `Store`.

### Value Objects (standalone, if any)

-   **`Config`**: A static class providing global configuration settings for the inventory domain, such as `TrackInventoryLevels`.
-   **<see cref="NumberGenerator"/>**: A static utility class responsible for generating unique, sequential reference numbers for various inventory transactions (e.g., stock transfer numbers) in the format <c>{PREFIX}{YYMMDD}{COUNTER}</c>.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `StockTransfer.Transfer` and `StockTransfer.Receive` methods encapsulate complex orchestration logic involving multiple `StockLocation` and `StockItem` aggregates. While this logic is currently within the `StockTransfer` aggregate, it exhibits characteristics of a domain service due to its coordination role.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Inventories` bounded context.

-   <c>CountAvailable</c> for a <see cref="StockItem"/> cannot be negative unless the item is <c>Backorderable</c>. This is validated by <see cref="StockLocation.ValidateInvariants()"/>.
-   It is not possible to reserve more quantity than <c>CountAvailable</c> for non-backorderable items. Enforced by <see cref="StockItem.Reserve(int, StockMovement.MovementOriginator, Guid?)"/>.
-   It is not possible to release more quantity than <c>QuantityReserved</c>. Enforced by <see cref="StockItem.Release(int, StockMovement.MovementOriginator, Guid?)"/>.
-   It is not possible to confirm shipment of more quantity than <c>QuantityReserved</c>. Enforced by <see cref="StockItem.ConfirmShipment(int, StockMovement.MovementOriginator, Guid?)"/>.
-   When stock is replenished (positive adjustment), backordered <see cref="InventoryUnit"/>s are automatically filled in order of creation (FIFO). This behavior is orchestrated by the <see cref="StockItem.Adjust(int, StockMovement.MovementOriginator, string?, Guid?)"/> method.
-   A <see cref="StockLocation"/> cannot be deleted if it has any associated <see cref="StockItem"/>s. (Enforced by <see cref="StockLocation.Delete(bool, bool, bool)"/>, returning <see cref="StockLocation.Errors.HasStockItems"/> or <see cref="StockLocation.Errors.HasReservedStock"/>).
-   For a <see cref="StockTransfer"/>, the <c>SourceLocation</c> and <c>DestinationLocation</c> cannot be the same. Enforced by <see cref="StockTransfer.Create(Guid, Guid?, string?)"/> and <see cref="StockTransfer.Update(Guid, Guid?, string?)"/> methods, returning <see cref="StockTransfer.Errors.SourceEqualsDestination"/>.
-   A <see cref="StockTransfer"/> must involve at least one product <c>Variant</c> with a positive transfer quantity. Enforced by <see cref="StockTransfer.Transfer(StockLocation, StockLocation, IReadOnlyDictionary{Variant, int}?)"/> and <see cref="StockTransfer.Receive(StockLocation, IReadOnlyDictionary{Variant, int}?)"/>, returning <see cref="StockTransfer.Errors.NoVariants"/> or <see cref="StockTransfer.Errors.InvalidQuantity"/>.
-   A <see cref="StockMovement"/> must always have a non-zero quantity.
-   <see cref="StockItem"/>s can be marked as <c>Backorderable</c>, allowing orders even when out of physical stock. Backordered orders are tracked via <c>InventoryUnit</c> state transitions.
-   The global <c>Config.TrackInventoryLevels</c> setting dictates whether inventory tracking is active across the system.

---

## 🤝 Relationships & Dependencies

-   **`StockLocation` to `StockItem`**: One-to-many composition. `StockLocation` is the aggregate root for its `StockItem`s.
-   **`StockItem` to `StockMovement`**: One-to-many composition. `StockItem` is the aggregate root for its `StockMovement` records.
-   **`StockTransfer` to `StockMovement`**: `StockTransfer` orchestrates the creation of `StockMovement`s, which are then owned by the respective `StockItem` aggregates.
-   **`StockLocation` to `StockLocationStore`**: One-to-many composition. `StockLocation` manages its links to `Store`s.
-   **External Aggregates**: References `Variant` (from `Catalog.Products.Variants`), `Country`, `State` (from `Location`), and `Store` (from `Stores`).
-   **Shared Kernel**: `StockItem`, `StockLocation`, and `StockTransfer` inherit from `Aggregate` and implement various interfaces from `SharedKernel.Domain` (e.g., `IAddress`, `IHasParameterizableName`, `IHasUniqueName`, `IHasMetadata`). `StockMovement` inherits from `AuditableEntity`.

---

## 🚀 Key Use Cases / Behaviors

-   **Create and Manage Stock Locations**: Define new <see cref="StockLocation"/>s, update their details (name, address, active status), link/unlink them to <see cref="Store"/>s, and mark them as default.
-   **Create and Manage Stock Items**: Establish <see cref="StockItem"/>s for product <c>Variant</c>s at specific <see cref="StockLocation"/>s, setting initial quantities and backorderability. (Managed by the <see cref="StockItem"/> aggregate).
-   **Adjust Stock Levels**: Modify the <c>QuantityOnHand</c> of a <see cref="StockItem"/> using <see cref="StockItem.Adjust(int, StockMovement.MovementOriginator, string?, Guid?)"/> (e.g., due to damage, loss, or recount), recording a <see cref="StockMovement"/>.
-   **Reserve Stock**: Allocate <see cref="StockItem"/>s for pending orders using <see cref="StockItem.Reserve(int, StockMovement.MovementOriginator, Guid?)"/>, decreasing <c>Count Available</c> and increasing <c>Quantity Reserved</c>.
-   **Release Reserved Stock**: Free up <c>Quantity Reserved</c> using <see cref="StockItem.Release(int, StockMovement.MovementOriginator, Guid?)"/> (e.g., due to order cancellation), increasing <c>Count Available</c>.
-   **Confirm Shipment**: Decrement both <c>QuantityOnHand</c> and <c>QuantityReserved</c> upon product shipment using <see cref="StockItem.ConfirmShipment(int, StockMovement.MovementOriginator, Guid?)"/>.
-   **Transfer Stock**: Orchestrate the movement of stock between two <see cref="StockLocation"/>s using <see cref="StockTransfer.Transfer(StockLocation, StockLocation, IReadOnlyDictionary{Variant, int}?)"/>, ensuring corresponding <see cref="StockMovement"/>s are recorded at both ends.
-   **Receive Stock**: Record the receipt of new <c>Variant</c> quantities into a <c>DestinationLocation</c> from external sources using <see cref="StockTransfer.Receive(StockLocation, IReadOnlyDictionary{Variant, int}?)"/>.
-   **Generate Unique Numbers**: Provide unique reference numbers for inventory transactions using <see cref="NumberGenerator.Generate(string)"/>.
-   **Publish Domain Events**: Emit domain events for stock item changes, stock location changes, and stock transfers, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `Inventories` domain is critical for maintaining accurate product availability information, directly impacting sales and fulfillment.
-   The use of `StockItem` as an aggregate root, even though it's owned by `StockLocation`, highlights the importance of its internal consistency and lifecycle management.
-   `StockTransfer` acts as an orchestrator, coordinating actions across multiple `StockLocation` and `StockItem` aggregates, which is a common pattern for complex domain operations.
-   Domain Events are extensively used to communicate changes in inventory state, allowing other parts of the system (e.g., order processing, product availability updates) to react.
-   The `Config` static class provides a simple way to manage global inventory settings, but for more complex configurations, a dedicated `Configuration` aggregate might be considered.
-   The `NumberGenerator` ensures unique identifiers for transactions, which is crucial for auditing and tracking.
