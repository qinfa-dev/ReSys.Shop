# Inventories.Locations Bounded Context

This document describes the `Inventories.Locations` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines and manages the physical or logical locations where inventory is stored, as well as the processes for transferring stock between these locations or receiving stock from external suppliers. It ensures accurate tracking of stock movements, supports multi-location inventory management, and provides the foundation for efficient logistics and fulfillment operations.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Inventories.Locations` bounded context.

-   **Stock Location**: A physical or logical place where inventory is held (e.g., a warehouse, a retail store, a staging area). Represented by the `StockLocation` aggregate.
-   **Stock Transfer**: An orchestrated process of moving stock between two `Stock Location`s or receiving stock from an external vendor into a `Stock Location`. Represented by the `StockTransfer` aggregate.
-   **Source Location**: The `Stock Location` from which inventory is dispatched during a transfer.
-   **Destination Location**: The `Stock Location` to which inventory is received during a transfer or from a supplier.
-   **Stock Item**: A record representing the quantity of a specific product variant at a particular `Stock Location`. (Referenced from `Inventories.Stocks` Bounded Context).
-   **Stock Movement**: A historical record of changes in stock levels. (Referenced from `Inventories.Stocks` Bounded Context).
-   **Store**: A sales channel or storefront that can be linked to `Stock Location`s. (Referenced from `Stores` Bounded Context).
-   **Store Stock Location**: A junction entity linking a `Store` to a `Stock Location`.
-   **Default Stock Location**: A `Stock Location` designated as the primary location for operations when no specific location is indicated.
-   **Reference Number**: A unique identifier assigned to a `Stock Transfer`.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`StockLocation`**: This is an Aggregate Root. It represents a physical or logical location for inventory and is responsible for managing its own properties, state (active, default, deleted), and associated `StockItem`s. It orchestrates basic stock operations like `Restock` and `Unstock`.
    -   **Entities**: None directly owned by `StockLocation` within this module, but it aggregates `StockItem`s (from `Inventories.Stocks`).
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Presentation`, `Active`, `Default`, address components (`Address1`, `City`, `Zipcode`), `Phone`, `Company`, `PublicMetadata`, and `PrivateMetadata` act as intrinsic attributes.

-   **`StockTransfer`**: This is an Aggregate Root. It orchestrates the complex process of moving stock, ensuring that appropriate `StockMovement`s are recorded at the affected `StockItem`s in both source and destination `StockLocation`s.
    -   **Entities**: It coordinates `StockMovement`s (from `Inventories.Stocks`), which are ultimately owned by `StockItem`s.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Number` (generated), `Reference`, `SourceLocationId`, and `DestinationLocationId` are intrinsic attributes.

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

-   None explicitly defined as separate classes. The `Transfer` and `Receive` methods on `StockTransfer` could be considered acting as domain service methods due to their orchestration of multiple aggregates.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Inventories.Locations` bounded context.

-   A `StockLocation` cannot be deleted if it has any associated `StockItem`s.
-   A `StockLocation` can be marked as `Default`, and the system should ensure only one default is active per system context.
-   `StockLocation`s support soft-deletion and restoration.
-   For a `StockTransfer`, `SourceLocation` and `DestinationLocation` cannot be the same.
-   A `StockTransfer` must involve positive quantities of `Variant`s.
-   `StockTransfer`s ensure `StockMovement`s are correctly recorded at the relevant `StockItem`s.
-   Sufficient stock must be available in the `SourceLocation` for a `StockTransfer` to proceed for non-backorderable items.

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

-   **Create Stock Location**: Define a new physical or logical location for storing inventory.
-   **Update Stock Location Details**: Modify name, address, active status, or metadata of an existing `StockLocation`.
-   **Mark as Default**: Designate a `StockLocation` as the system's default.
-   **Delete Stock Location**: Soft-delete a `StockLocation`, provided it has no associated stock items.
-   **Restore Stock Location**: Reactivate a previously soft-deleted `StockLocation`.
-   **Link/Unlink Store**: Associate a `StockLocation` with a `Store` to make its inventory available to that sales channel.
-   **Create Stock Transfer**: Initiate a new stock transfer, specifying source and destination locations, and a reference.
-   **Perform Stock Transfer**: Execute the movement of specified `Variant` quantities between two `StockLocation`s, updating `StockItem`s and recording `StockMovement`s.
-   **Receive Stock from Supplier**: Record the receipt of new `Variant` quantities into a `DestinationLocation` from an external source.

---

## 📝 Considerations / Notes

-   The `StockLocation` aggregate is crucial for managing multi-location inventory, enabling granular control over where products are stored and sourced.
-   The `StockTransfer` aggregate is an orchestrator, ensuring the atomicity and consistency of stock movements across different locations and stock items.
-   Domain Events are used to communicate significant changes in stock location and transfer activities to other parts of the system (e.g., inventory reports, product availability updates).
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
-   The `IAddress` interface ensures consistent address information across different domain entities requiring it.
