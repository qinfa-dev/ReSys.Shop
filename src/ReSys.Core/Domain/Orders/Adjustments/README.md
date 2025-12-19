# Orders.Adjustments Bounded Context

This document describes the `Orders.Adjustments` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages financial adjustments applied to orders and their individual line items. It provides a structured way to record discounts, taxes, promotions, or other modifications that alter the total cost of an order or its components, thereby ensuring accurate financial calculations and transparent reporting.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Orders.Adjustments` bounded context.

-   **Order Adjustment**: A financial modification (e.g., a discount) applied directly to an entire `Order` or to shipping. Represented by the `OrderAdjustment` entity. It has a `Scope` property to distinguish between `Order` and `Shipping` adjustments.
-   **Line Item Adjustment**: A financial modification applied to a specific `Line Item` within an `Order`. Represented by the `LineItemAdjustment` entity.
-   **Order**: The parent order to which the adjustment applies. (Referenced from `Orders` Bounded Context).
-   **Line Item**: The specific item within an order to which the adjustment applies. (Referenced from `Orders.LineItems` Bounded Context).
-   **Promotion**: The promotional offer that generated the adjustment. (Referenced from `Promotions.Promotions` Bounded Context).
-   **Amount Cents**: The value of the adjustment in cents. Can be positive (e.g., tax) or negative (e.g., discount).
-   **Description**: A human-readable explanation of the adjustment (e.g., "Seasonal Discount", "Shipping Tax").
-   **Is Promotion**: A computed property indicating if the adjustment originated from a `Promotion`.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `OrderAdjustment` and `LineItemAdjustment` are entities owned by the `Order` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`LineItemAdjustment`**: This is a central entity representing an adjustment to a line item. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `LineItemId`, `PromotionId`, `AmountCents`, and `Description` are intrinsic attributes.
-   **`OrderAdjustment`**: This is a central entity representing an adjustment to an entire order or shipping. It is an `AuditableEntity`. It has a `Scope` property of type `AdjustmentScope` to differentiate between order-level and shipping-level adjustments.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `OrderId`, `PromotionId`, `AmountCents`, and `Description` are intrinsic attributes.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Orders.Adjustments` bounded context.

-   An `Adjustment` must always have a `Description`.
-   `AmountCents` can be negative (for discounts) but must meet a minimum value (`Constraints.AmountCentsMinValue`).
-   `Description` must adhere to a maximum length constraint.
-   `Adjustment` instances track their creation timestamps (`CreatedAt`), adhering to auditing requirements.
-   `OrderAdjustment` is linked to an `Order`.
-   `LineItemAdjustment` is linked to a `LineItem`.

---

## 🤝 Relationships & Dependencies

-   **`LineItemAdjustment` to `LineItem`**: Many-to-one relationship. `LineItemAdjustment` is owned by `LineItem` (from `Orders.LineItems`).
-   **`OrderAdjustment` to `Order`**: Many-to-one relationship. `OrderAdjustment` is owned by `Order` (from `Orders`).
-   **`LineItemAdjustment` / `OrderAdjustment` to `Promotion`**: Many-to-one relationship (optional). Adjustments can originate from a `Promotion` (from `Promotions.Promotions`).
-   **Shared Kernel**: Both `LineItemAdjustment` and `OrderAdjustment` inherit from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), providing common auditing fields. They use `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Line Item Adjustment**: Instantiate a new `LineItemAdjustment` for a specific line item, specifying the amount and description.
-   **Create Order Adjustment**: Instantiate a new `OrderAdjustment` for an entire order or for shipping, specifying the amount, description, and scope.

---

## 📝 Considerations / Notes

-   Both `LineItemAdjustment` and `OrderAdjustment` act as child entities within the `Order` aggregate (directly or indirectly via `LineItem`), and their lifecycles are managed by the `Order` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
-   Financial amounts are stored in cents (`AmountCents`) to avoid floating-point precision issues in calculations.