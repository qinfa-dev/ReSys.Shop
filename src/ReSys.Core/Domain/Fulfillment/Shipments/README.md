# Orders.Shipments Bounded Context

This document describes the `Orders.Shipments` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the logistics of delivering products associated with a customer order. It tracks the state of physical shipments, generates unique tracking numbers, records shipping events (shipped, delivered, canceled), and integrates with chosen shipping methods to provide a comprehensive view of the order fulfillment process.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Orders.Shipments` bounded context.

-   **Shipment**: A record representing the delivery process for a portion or all of an `Order`. Represented by the `Shipment` aggregate.
-   **Order**: The parent order to which this shipment belongs. (Referenced from `Orders` Bounded Context).
-   **Shipping Method**: The chosen method for delivering the shipment (e.g., "Standard Shipping", "Express Delivery"). (Referenced from `Shipping` Bounded Context).
-   **Number**: A unique, internally generated identifier for the shipment.
-   **Shipment State**: The current status of the shipment, defined by the `ShipmentState` enumeration (e.g., `Pending`, `Ready`, `Shipped`, `Delivered`, `Canceled`).
-   **Tracking Number**: An identifier provided by the shipping carrier for external tracking.
-   **Shipped At**: The timestamp when the shipment was dispatched.
-   **Delivered At**: The timestamp when the shipment reached its destination.
-   **Is Shipped**: A computed property indicating if the shipment has been dispatched.
-   **Is Delivered**: A computed property indicating if the shipment has reached its final destination.
-   **Is Canceled**: A computed property indicating if the shipment has been canceled.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Shipment`**: This is the Aggregate Root. It encapsulates all information related to a single delivery and is responsible for managing its state transitions and ensuring accurate tracking of the delivery process.
    -   **Entities**: None directly owned by `Shipment`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `OrderId`, `ShippingMethodId`, `Number`, `State`, `TrackingNumber`, `ShippedAt`, and `DeliveredAt` are intrinsic attributes of the `Shipment` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Order` (from `Core.Domain.Orders`): Referenced by `Shipment`, but managed by its own aggregate.
-   `ShippingMethod` (from `Core.Domain.Shipping`): Referenced by `Shipment`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`ShipmentState`**: An enumeration defining the various stages of a shipment's lifecycle.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All business logic related to shipment state transitions and record-keeping is encapsulated within the `Shipment` aggregate.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Orders.Shipments` bounded context.

-   A `Shipment` must always be associated with a valid `OrderId` and `ShippingMethodId`.
-   A unique `Number` is generated for each shipment.
-   `TrackingNumber` must adhere to a maximum length constraint.
-   A `Shipment` cannot be canceled if it has already been `Shipped`.
-   A `Shipment` must be in a `Shipped` state before it can be `Delivered`.
-   `Shipment` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.
-   State transitions must follow logical sequences (e.g., `Pending` -> `Ready` -> `Shipped` -> `Delivered`).

---

## 🤝 Relationships & Dependencies

-   **`Shipment` to `Order`**: Many-to-one relationship. `Shipment` is owned by `Order` (from `Orders`).
-   **`Shipment` to `ShippingMethod`**: Many-to-one relationship. `Shipment` links to `ShippingMethod` (from `Shipping`).
-   **Shared Kernel**: `Shipment` inherits from `Aggregate` (from `SharedKernel.Domain.Entities`), providing common base properties. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Shipment**: Instantiate a new `Shipment` for an order, specifying the chosen shipping method and an initial state (`Pending`).
-   **Mark as Ready**: Transition a `Pending` shipment to `Ready` for dispatch.
-   **Ship Shipment**: Mark a shipment as `Shipped`, recording the `ShippedAt` timestamp and optionally a `TrackingNumber`.
-   **Deliver Shipment**: Mark a `Shipped` shipment as `Delivered`, recording the `DeliveredAt` timestamp.
-   **Cancel Shipment**: Cancel a shipment before it is dispatched.
-   **Update Tracking Number**: Add or modify the `TrackingNumber` for a shipment.
-   **Generate Shipment Number**: Automatically generate a unique number for new shipments.
-   **Publish Domain Events**: Emit events for shipment creation, state changes (ready, shipped, delivered, canceled), and tracking updates, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   `Shipment` acts as a child entity within the `Order` aggregate, and its lifecycle is managed by the `Order` aggregate.
-   The shipment state machine is crucial for accurately reflecting the physical movement of goods and coordinating with logistics partners.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   Domain Events are important for triggering external actions like sending shipping notifications to customers.
