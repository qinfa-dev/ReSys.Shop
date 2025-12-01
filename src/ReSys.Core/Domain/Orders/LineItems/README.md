# Orders.LineItems Bounded Context

This document describes the `Orders.LineItems` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the individual items within a customer order. It represents the specific product variants, their quantities, and the prices at which they were ordered, along with any item-level adjustments. It ensures the accurate capture and calculation of item-level financial details, which are critical for the overall order total and fulfillment.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Orders.LineItems` bounded context.

-   **Line Item**: A record representing a specific product `Variant` and its ordered `Quantity` within an `Order`. Represented by the `LineItem` entity.
-   **Order**: The parent order to which this line item belongs. (Referenced from `Orders` Bounded Context).
-   **Variant**: The specific product variation being ordered. (Referenced from `Catalog.Products.Variants` Bounded Context).
-   **Quantity**: The number of units of the product variant being ordered.
-   **Price Cents**: The price of a single unit of the product variant at the time of order, in cents.
-   **Currency**: The currency code (e.g., "USD") in which the price is denominated.
-   **Captured Name**: The name of the product variant as it appeared at the time of order placement.
-   **Captured SKU**: The SKU of the product variant as it appeared at the time of order placement.
-   **Is Promotional**: A flag indicating if this line item was part of a promotion (e.g., a free item in a BuyXGetY deal).
-   **Adjustment**: A financial modification applied to this specific line item. (Referenced from `Orders.Adjustments` Bounded Context).
-   **Subtotal Cents**: The total price for this line item before any adjustments (`PriceCents * Quantity`).
-   **Total Cents**: The final total price for this line item after all adjustments.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `LineItem` is an entity that is owned by the `Order` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`LineItem`**: This is the central entity of this bounded context. It represents a single ordered item within an `Order`, capturing its details and any item-specific adjustments. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `OrderId`, `VariantId`, `Quantity`, `PriceCents`, `Currency`, `CapturedName`, `CapturedSku`, and `IsPromotional` are intrinsic attributes of the `LineItem` entity.
-   `LineItemAdjustment` (from `Core.Domain.Orders.Adjustments`): Owned by `LineItem`.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Orders.LineItems` bounded context.

-   A `LineItem` must always be associated with a valid `OrderId` and `VariantId`.
-   `Quantity` must be at least 1.
-   `PriceCents` must be non-negative.
-   `Currency` is required and must adhere to length constraints.
-   `CapturedName` is required and must adhere to length constraints.
-   `CapturedSku` (if provided) must adhere to length constraints.
-   `LineItem` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.
-   `SubtotalCents` is derived from `PriceCents * Quantity`.
-   `TotalCents` includes `SubtotalCents` plus all associated `Adjustments`.

---

## 🤝 Relationships & Dependencies

-   **`LineItem` to `Order`**: Many-to-one relationship. `LineItem` is owned by `Order` (from `Orders`).
-   **`LineItem` to `Variant`**: Many-to-one relationship. `LineItem` links to `Variant` (from `Catalog.Products.Variants`).
-   **`LineItem` to `LineItemAdjustment`**: One-to-many composition. `LineItem` is the parent for its `LineItemAdjustment`s (from `Orders.Adjustments`).
-   **Shared Kernel**: `LineItem` inherits from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), providing common auditing fields. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Line Item**: Instantiate a new `LineItem` for an order, specifying the product variant, quantity, and current pricing/naming details.
-   **Update Quantity**: Modify the quantity of a product variant within the line item.
-   **Delete Line Item**: Remove a line item from an order.

---

## 📝 Considerations / Notes

-   `LineItem` acts as a child entity within the `Order` aggregate, and its lifecycle is managed by the `Order` aggregate.
-   The "captured" properties (`CapturedName`, `CapturedSku`) are important because product or variant details might change over time, but the order line item should reflect what was purchased.
-   All monetary values are stored in cents (`PriceCents`, `SubtotalCents`, `TotalCents`) to avoid floating-point precision issues.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
