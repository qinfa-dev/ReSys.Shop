# Inventories.Locations Bounded Context

This document describes the `Inventories.Locations` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines and manages the physical or logical locations where inventory is stored, as well as the processes for transferring stock between these locations or receiving stock from external suppliers. It ensures accurate tracking of stock movements, supports multi-location inventory management, and provides the foundation for efficient logistics and fulfillment operations.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Inventories.Locations` bounded context.

-   **Stock Location**: A physical or logical place where inventory is held (e.g., a warehouse, a retail store, a staging area). Represented by the <see cref="StockLocation"/> aggregate.
-   **Stock Transfer**: An orchestrated process of moving stock between two <see cref="StockLocation"/>s or receiving stock from an external vendor into a <see cref="StockLocation"/>. Represented by the `StockTransfer` aggregate.
-   **Source Location**: The <see cref="StockLocation"/> from which inventory is dispatched during a transfer.
-   **Destination Location**: The <see cref="StockLocation"/> to which inventory is received during a transfer or from a supplier.
-   **Stock Item**: A record representing the quantity of a specific product variant at a particular <see cref="StockLocation"/>. (Referenced from `Inventories.Stocks` Bounded Context).
-   **Stock Movement**: A historical record of changes in stock levels. (Referenced from `Inventories.Stocks` Bounded Context).
-   **Store**: A sales channel or storefront that can be linked to <see cref="StockLocation"/>s. (Referenced from `Stores` Bounded Context).
-   **Store Stock Location**: A junction entity linking a <see cref="Store"/> to a <see cref="StockLocation"/>.
-   **Default Stock Location**: A <see cref="StockLocation"/> designated as the primary location for operations when no specific location is indicated.
-   **Reference Number**: A unique identifier assigned to a <see cref="StockTransfer"/>.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`StockLocation`**: This is an Aggregate Root. It represents a physical or logical location for inventory and is responsible for managing its own properties, state (active, default, deleted), and associated <see cref="StockItem"/>s. It orchestrates basic stock operations like <see cref="StockLocation.Restock(Variant, int, StockMovement.MovementOriginator, Guid?)"/> and <see cref="StockLocation.Unstock(Variant?, int, StockMovement.MovementOriginator, Guid?)"/>.
    -   **Entities**: None directly owned by <see cref="StockLocation"/> within this module, but it implicitly contains <see cref="StockItem"/>s (from `Inventories.Stocks`).
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>Name</c>, <c>Presentation</c>, <c>Active</c>, <c>Default</c>, address components (<c>Address1</c>, <c>City</c>, <c>Zipcode</c>), <c>Phone</c>, <c>Company</c>, <c>PublicMetadata</c>, and <c>PrivateMetadata</c> act as intrinsic attributes of the <see cref="StockLocation"/> aggregate.

-   **`StockTransfer`**: This is an Aggregate Root. It orchestrates the complex process of moving stock, ensuring that appropriate <see cref="StockMovement"/>s are recorded at the affected <see cref="StockItem"/>s in both source and destination <see cref="StockLocation"/>s.
    -   **Entities**: It coordinates <see cref="StockMovement"/>s (from `Inventories.Stocks`), which are ultimately owned by <see cref="StockItem"/>s.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>Number</c> (generated), <c>Reference</c>, <c>SourceLocationId</c>, and <c>DestinationLocationId</c> are intrinsic attributes.

### Entities (not part of an Aggregate Root, if any)

-   `StockItem` (from `Core.Domain.Inventories.Stocks`): While logically "contained" by `StockLocation`, it is an Aggregate Root in its own right in `Inventories.Stocks`. `StockLocation` references it.
-   `Variant` (from `Core.Domain.Catalog.Products.Variants`): Referenced by `StockItem`, but managed by its own aggregate.
-   `Country` (from `Core.Domain.Location`): Referenced by `StockLocation` for geographical context.
-   `State` (from `Core.Domain.Location`): Referenced by `StockLocation` for geographical context.
-   `Store` (from `Core.Domain.Stores`): Referenced by `StoreStockLocation`, but managed by its own aggregate.
-   `StoreStockLocation` (from `Core.Domain.Stores.StockLocations`): A junction entity linking a `StockLocation` to a `Store`.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `Transfer` and `Receive` methods on <see cref="StockTransfer"/> (which will be documented in `StockTransfer.cs`) could be considered acting as domain service methods due to their orchestration of multiple aggregates.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Inventories.Locations` bounded context.

-   A <see cref="StockLocation"/> cannot be deleted if it has any associated <see cref="StockItem"/>s (<see cref="StockLocation.Errors.HasStockItems"/>). This includes having <see cref="StockItem"/>s with reserved quantities (<see cref="StockLocation.Errors.HasReservedStock"/>).
-   A <see cref="StockLocation"/> can be marked as <c>Default</c> using <see cref="StockLocation.MakeDefault()"/>, and the system should ensure only one default is active per system context.
-   <see cref="StockLocation"/>s support soft-deletion via <see cref="StockLocation.Delete(bool, bool, bool)"/> and restoration via <see cref="StockLocation.Restore()"/>.
-   For a <see cref="StockTransfer"/>, <c>SourceLocation</c> and <c>DestinationLocation</c> cannot be the same.
-   A <see cref="StockTransfer"/> must involve positive quantities of <c>Variant</c>s.
-   <see cref="StockTransfer"/>s ensure <see cref="StockMovement"/>s are correctly recorded at the relevant <see cref="StockItem"/>s.
-   Sufficient stock must be available in the <c>SourceLocation</c> for a <see cref="StockTransfer"/> to proceed for non-backorderable items. Checks are performed during <see cref="StockLocation.Unstock(Variant?, int, StockMovement.MovementOriginator, Guid?)"/>.
-   Internal consistency checks for <see cref="StockItem"/>s (e.g., <c>QuantityReserved</c> not exceeding <c>QuantityOnHand</c> for non-backorderable items, non-negative quantities) are enforced by <see cref="StockLocation.ValidateInvariants()"/>.
-   <see cref="StockLocation"/> deletion also checks for pending shipments (<see cref="StockLocation.Errors.HasPendingShipments"/>), active stock transfers (<see cref="StockLocation.Errors.HasActiveStockTransfers"/>), and backordered inventory units (<see cref="StockLocation.Errors.HasBackorderedInventoryUnits"/>).

---

## 🤝 Relationships & Dependencies

-   **`StockLocation` to `StockItem`**: One-to-many relationship (composition at the logical level). `StockLocation` contains `StockItem`s.
-   **`StockLocation` to `StoreStockLocation`**: One-to-many composition. `StockLocation` manages its links to `Store`s.
-   **`StockLocation` to `Country` / `State`**: Many-to-one relationships for geographical address details.
-   **`StockTransfer` to `StockLocation`**: Many-to-one relationships for `SourceLocation` (optional) and `DestinationLocation`.
-   **`StockTransfer` to `StockMovement`**: `StockTransfer` creates `StockMovement`s, which are owned by `StockItem`s.
-   **Shared Kernel**: `StockLocation` and `StockTransfer` inherit from `Aggregate` and implement various interfaces like `IAddress`, `IHasParameterizableName`, `IHasUniqueName`, `IHasMetadata`, `ISoftDeletable` (from `SharedKernel.Domain`), leveraging common patterns. They use `ErrorOr` for a functional approach to error handling and publish `DomainEvent`s.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Stock Location**: Define a new physical or logical location for storing inventory using <see cref="StockLocation.Create(string, string?, bool, bool, Guid?, string?, string?, string?, string?, Guid?, string?, string?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>.
-   **Update Stock Location Details**: Modify name, address, active status, or metadata of an existing <see cref="StockLocation"/> using <see cref="StockLocation.Update(string?, string?, bool?, string?, string?, string?, string?, Guid?, Guid?, string?, string?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>.
-   **Mark as Default**: Designate a <see cref="StockLocation"/> as the system's default fulfillment location using <see cref="StockLocation.MakeDefault()"/>.
-   **Delete Stock Location**: Soft-delete a <see cref="StockLocation"/> using <see cref="StockLocation.Delete(bool, bool, bool)"/>, provided it meets constraints (e.g., no active stock items, pending shipments, or transfers). Returns specific errors if constraints are violated.
-   **Restore Stock Location**: Reactivate a previously soft-deleted <see cref="StockLocation"/> using <see cref="StockLocation.Restore()"/>.
-   **Link/Unlink Store**: Associate a <see cref="StockLocation"/> with a <see cref="Store"/> using <see cref="StockLocation.LinkStore(Store)"/> or disassociate it using <see cref="StockLocation.UnlinkStore(Store)"/> to manage its inventory availability for that sales channel.
-   **Create Stock Transfer**: Initiate a new stock transfer, specifying source and destination locations, and a reference (handled by `StockTransfer` aggregate).
-   **Perform Stock Transfer**: Execute the movement of specified <c>Variant</c> quantities between two <see cref="StockLocation"/>s, updating <see cref="StockItem"/>s and recording <see cref="StockMovement"/>s. This involves <see cref="StockLocation.Unstock(Variant?, int, StockMovement.MovementOriginator, Guid?)"/> from the source and <see cref="StockLocation.Restock(Variant, int, StockMovement.MovementOriginator, Guid?)"/> at the destination.
-   **Receive Stock from Supplier**: Record the receipt of new <c>Variant</c> quantities into a <c>DestinationLocation</c> from an external source using <see cref="StockLocation.Restock(Variant, int, StockMovement.MovementOriginator, Guid?)"/>.
-   **Validate Invariants**: Ensure the internal consistency of <see cref="StockLocation"/> and its <see cref="StockItem"/>s using <see cref="StockLocation.ValidateInvariants()"/>.

---

## 📝 Considerations / Notes

-   The `StockLocation` aggregate is crucial for managing multi-location inventory, enabling granular control over where products are stored and sourced.
-   The `StockTransfer` aggregate is an orchestrator, ensuring the atomicity and consistency of stock movements across different locations and stock items.
-   Domain Events are used to communicate significant changes in stock location and transfer activities to other parts of the system (e.g., inventory reports, product availability updates).
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
-   The `IAddress` interface ensures consistent address information across different domain entities requiring it.
