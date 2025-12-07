# Fulfillment Bounded Context

This document describes the `Fulfillment` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the process of fulfilling customer orders, from inventory allocation and picking to packing and shipment. It acts as a dedicated logistics layer, decoupling the physical handling of goods from the core order management process. This ensures efficient, trackable, and robust order delivery, integrating with inventory and shipping services.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Fulfillment` bounded context.

-   **Fulfillment Order**: A directive to prepare and ship a set of items for a customer order. It represents a single unit of work for the warehouse or logistics team. Represented by the `FulfillmentOrder` aggregate.
-   **Fulfillment State**: The current stage of a `FulfillmentOrder` in its lifecycle (e.g., `Pending`, `Allocated`, `Picking`, `Shipped`, `Delivered`). Represented by the `FulfillmentState` enum.
-   **Fulfillment Line Item**: A specific product variant and its quantity that needs to be fulfilled as part of a `FulfillmentOrder`.
-   **Stock Location**: The physical or logical location from which items are dispatched for fulfillment.
-   **Package ID**: An identifier for the physical package containing the fulfilled items.
-   **Tracking Number**: A unique code provided by a shipping carrier for tracking the package's journey.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`FulfillmentOrder`**: This is the Aggregate Root. It encapsulates the entire fulfillment process for a set of items, managing its state transitions, associated line items, and tracking details. It ensures the consistency and integrity of the fulfillment workflow.
    -   **Entities**:
        -   `FulfillmentLineItem` (owned by `FulfillmentOrder`): Represents a specific product variant and quantity to be fulfilled within this order.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `OrderId`, `StockLocationId`, `State`, `PackageId`, and `TrackingNumber` are intrinsic attributes of the `FulfillmentOrder` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Order` (from `Core.Domain.Orders`): Referenced by `FulfillmentOrder` to link back to the customer's purchase.
-   `StockLocation` (from `Core.Domain.Inventories.Locations`): Referenced by `FulfillmentOrder` to specify the origin of the items.
-   `Variant` (from `Core.Domain.Catalog.Products.Variants`): Referenced by `FulfillmentLineItem` to detail the product being fulfilled.

### Value Objects (standalone, if any)

-   **`FulfillmentState`**: An enumeration defining the various stages of a fulfillment order's lifecycle.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The state transition methods within the `FulfillmentOrder` aggregate (e.g., `AllocateInventory`, `Ship`, `Deliver`) encapsulate the core business logic for managing the fulfillment workflow.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Fulfillment` bounded context.

-   `FulfillmentOrder` state transitions must follow a predefined, valid sequence (e.g., `Pending` -> `Allocated` -> `Picking` -> `Picked` -> `Packing` -> `ReadyToShip` -> `Shipped` -> `Delivered`). Invalid transitions are prevented.
-   A `FulfillmentOrder` cannot be shipped or delivered if it has been canceled.
-   A `FulfillmentOrder` cannot be canceled if it has already been shipped or delivered.
-   A `PackageId` is required to transition to the `Packing` state.
-   A `TrackingNumber` is required to transition to the `Shipped` state.
-   Each `FulfillmentOrder` must be associated with a valid `OrderId` and `StockLocationId`.
-   Each `FulfillmentOrder` must contain at least one `FulfillmentLineItem`.
-   `FulfillmentLineItem` quantities must be positive.
-   `FulfillmentOrder` and `FulfillmentLineItem` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`FulfillmentOrder` to `FulfillmentLineItem`**: One-to-many composition. `FulfillmentOrder` is the aggregate root for its `FulfillmentLineItem`s.
-   **`FulfillmentOrder` to `Order`**: Many-to-one relationship. `FulfillmentOrder` links to `Order` (from `Orders`).
-   **`FulfillmentOrder` to `StockLocation`**: Many-to-one relationship. `FulfillmentOrder` links to `StockLocation` (from `Inventories.Locations`).
-   **`FulfillmentLineItem` to `Variant`**: Many-to-one relationship. `FulfillmentLineItem` links to `Variant` (from `Catalog.Products.Variants`).
-   **Shared Kernel**: Both `FulfillmentOrder` and `FulfillmentLineItem` inherit from `Aggregate` or `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), providing common base properties. They utilize `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Fulfillment Order**: Instantiate a new `FulfillmentOrder` for a given customer order, specifying the `StockLocation` and the `FulfillmentLineItem`s to be fulfilled.
-   **Allocate Inventory**: Transition the order from `Pending` to `Allocated`, signifying that inventory has been reserved.
-   **Manage Picking Process**: Initiate picking (`StartPicking`) and mark completion (`CompletePicking`).
-   **Pack Order**: Transition to `Packing` and assign a `PackageId`.
-   **Mark Ready for Shipment**: Transition to `ReadyToShip`, indicating the package is prepared for carrier pickup.
-   **Ship Order**: Transition to `Shipped` and record the carrier's `TrackingNumber`.
-   **Deliver Order**: Mark the order as `Delivered` upon customer receipt.
-   **Cancel Fulfillment**: Cancel an active fulfillment order, potentially releasing reserved inventory.
-   **Publish Domain Events**: Emit events for state changes (e.g., `Created`, `InventoryAllocated`, `Shipped`, `Delivered`, `Canceled`), enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `FulfillmentOrder` aggregate is designed as a robust state machine, ensuring that items progress through the physical fulfillment process in a controlled manner.
-   Domain Events are critical for communicating changes in fulfillment status to other parts of the system, such as updating the customer's order status or triggering inventory adjustments.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   This domain often serves as a bridge between order processing and external logistics systems, providing the necessary data and status updates.
