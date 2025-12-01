# Orders Bounded Context

This document describes the `Orders` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the entire lifecycle of a customer order, from its initial `Cart` state through various stages including address, delivery, payment, confirmation, and final completion or cancellation. It acts as an orchestrator, coordinating interactions with multiple sub-domains (e.g., products, shipping, payments, promotions) to ensure a consistent, valid, and robust order processing workflow.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Orders` bounded context.

-   **Order**: A customer's request to purchase products, representing a complete transaction and its associated state. Represented by the `Order` aggregate.
-   **Order State**: The current stage of an order in its lifecycle, defined by the `OrderState` enumeration (e.g., `Cart`, `Address`, `Delivery`, `Payment`, `Confirm`, `Complete`, `Canceled`).
-   **Line Item**: A specific product `Variant` and its ordered quantity within an `Order`.
-   **Item Total Cents**: The sum of the base prices of all `Line Item`s in an `Order` (quantity * unit price).
-   **Shipment Total Cents**: The calculated cost of shipping for an `Order`.
-   **Total Cents**: The final total cost of an `Order` after all `Adjustment`s (discounts, taxes, shipping) have been applied.
-   **Adjustment Total Cents**: The sum of all discounts, taxes, or other financial modifications applied to an `Order` or its `Line Item`s.
-   **Currency**: The ISO 4217 currency code in which the `Order` is placed and its amounts are denominated.
-   **Email**: The customer's email address, used for order-related communications.
-   **Special Instructions**: Any specific notes or requests provided by the customer for the order.
-   **Promo Code**: A specific code used by a customer to apply a `Promotion` to an `Order`.
-   **Completed At / Canceled At**: Timestamps indicating when the order reached its final `Complete` or `Canceled` state.
-   **Ship Address**: The `UserAddress` where the products of the order are to be delivered.
-   **Bill Address**: The `UserAddress` used for billing purposes associated with the order.
-   **Shipping Method**: The chosen method for delivering the order (e.g., "Standard Shipping", "Express Delivery").
-   **Payment**: A record of a financial transaction made towards an `Order`.
-   **Promotion**: A discount, offer, or incentive applied to an `Order` or its `Line Item`s.
-   **Variant**: A specific version of a product (e.g., "Red T-Shirt, Size M"). (Referenced from `Catalog.Products.Variants` Bounded Context).
-   **Store**: The specific store or sales channel where the order was placed. (Referenced from `Stores` Bounded Context).
-   **Application User**: The user who initiated or placed the order. (Referenced from `Identity.Users` Bounded Context).
-   **Is Fully Digital**: A computed property indicating if all `Line Item`s in the order are digital products, which might affect shipping requirements.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Order`**: This is the Aggregate Root. It encapsulates the entire order process, including its state, financial totals, and collections of `LineItem`s, `OrderAdjustment`s, `Shipment`s, and `Payment`s. It orchestrates state transitions, ensures the consistency of the order's data, and coordinates with other aggregates.
    -   **Entities**:
        -   `LineItem` (owned by `Order`): Represents a specific product `Variant` and its quantity within the order.
        -   `OrderAdjustment` (owned by `Order`): Represents a discount, tax, or other financial modification applied directly to the order.
        -   `Shipment` (owned by `Order`): Represents a shipment created for the order, detailing what is being shipped and how.
        -   `Payment` (owned by `Order`): Represents a financial transaction made for the order, tracking amount, method, and status.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `StoreId`, `UserId`, `PromotionId`, `ShippingMethodId`, `Number`, `State`, `ItemTotalCents`, `ShipmentTotalCents`, `TotalCents`, `AdjustmentTotalCents`, `Currency`, `Email`, `SpecialInstructions`, `PromoCode`, `CompletedAt`, `CanceledAt`, `PublicMetadata`, and `PrivateMetadata` are intrinsic attributes of the `Order` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Variant` (from `Core.Domain.Catalog.Products.Variants`): Referenced by `LineItem`, but managed by its own aggregate.
-   `UserAddress` (from `Core.Domain.Identity.UserAddresses`): Referenced by `Order` for shipping and billing addresses, but managed by its own aggregate.
-   `Store` (from `Core.Domain.Stores`): Referenced by `Order` to indicate where the order was placed, but managed by its own aggregate.
-   `ApplicationUser` (from `Core.Domain.Identity.Users`): Referenced by `Order` to identify the customer, but managed by its own aggregate.
-   `Promotion` (from `Core.Domain.Promotions.Promotions`): Referenced by `Order` when a promotion is applied, but managed by its own aggregate.
-   `ShippingMethod` (from `Core.Domain.Shipping`): Referenced by `Order` to determine shipping costs, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`OrderState`**: An enumeration defining the various stages of an order's lifecycle, ensuring controlled transitions.

---

## ⚙️ Domain Services (if any)

-   **`PromotionCalculator` (from `Core.Domain.Promotions.Calculations`)**: This is a domain service that calculates the financial adjustments (discounts) to an order based on a given `Promotion`. It is used by the `Order` aggregate to apply promotional logic.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Orders` bounded context.

-   Order state transitions must follow a predefined, valid sequence (e.g., `Cart` -> `Address` -> `Delivery` -> `Payment` -> `Confirm` -> `Complete`). Invalid transitions are prevented.
-   An `Order` cannot be canceled if it has already reached the `Complete` state.
-   An `Order` cannot be completed if the total amount of `Payment`s is insufficient to cover the `Total Cents`.
-   `Line Item` quantities must be at least 1 (`Constraints.QuantityMinValue`).
-   Only `Purchasable` `Variant`s can be added to an `Order`.
-   For non-fully digital orders, both `Ship Address` and `Bill Address` are required before proceeding to `Delivery` state.
-   A `Shipping Method` must be selected and its cost calculated before transitioning to the `Payment` state.
-   `AmountCents` for any `Payment` must be non-negative (`Constraints.AmountCentsMinValue`).
-   `Promotion`s can only be applied once to an order. If a promotion requires a coupon code, the provided `Promo Code` must be valid.
-   Order totals (`ItemTotalCents`, `ShipmentTotalCents`, `AdjustmentTotalCents`, `TotalCents`) are consistently recalculated after any change to `Line Item`s, `Adjustment`s, or `Shipping Method`.

---

## 🤝 Relationships & Dependencies

-   **`Order` to `LineItem`**: One-to-many composition. `Order` is the aggregate root for its `LineItem`s.
-   **`Order` to `OrderAdjustment`**: One-to-many composition. `Order` is the aggregate root for its `OrderAdjustment`s.
-   **`Order` to `Shipment`**: One-to-many composition. `Order` is the aggregate root for its `Shipment`s.
-   **`Order` to `Payment`**: One-to-many composition. `Order` is the aggregate root for its `Payment`s.
-   **External Aggregates**: References `Variant` (from `Catalog.Products.Variants`), `UserAddress` (from `Identity.UserAddresses`), `Store` (from `Stores`), `ApplicationUser` (from `Identity.Users`), `Promotion` (from `Promotions.Promotions`), and `ShippingMethod` (from `Shipping`).
-   **Shared Kernel**: Inherits from `Aggregate` and implements `IHasMetadata` (from `SharedKernel.Domain.Entities`), leveraging common patterns for metadata management. It also uses `SharedKernel.Validations` for consistent input validation.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Order**: Instantiate a new `Order` in the `Cart` state for a given store and currency, optionally associating it with a user and email.
-   **Progress Order State**: Transition the `Order` through its various lifecycle states (`Next()` method), enforcing valid transitions and prerequisites.
-   **Cancel Order**: Change the `Order`'s state to `Canceled`, triggering inventory release and preventing further processing.
-   **Manage Line Items**: Add new `Line Item`s (product `Variant`s with quantities), remove existing ones, or update quantities of `Line Item`s.
-   **Recalculate Order Totals**: Automatically update all financial totals (`ItemTotalCents`, `ShipmentTotalCents`, `AdjustmentTotalCents`, `TotalCents`) after any relevant change.
-   **Set Addresses**: Assign `Ship Address` and `Bill Address` to the order.
-   **Apply/Remove Promotions**: Apply a `Promotion` to the order using a `Promo Code` (if applicable) and remove previously applied promotions, recalculating totals.
-   **Set Shipping Method**: Choose a `Shipping Method` for the order and calculate its `Shipment Total Cents`.
-   **Add Payment**: Record a `Payment` made towards the order.
-   **Generate Order Number**: Automatically generate a unique order number for new orders.
-   **Publish Domain Events**: Emit domain events for `Order` creation, state changes, completion, cancellation, `Line Item` changes, inventory finalization/release, and promotion application/removal, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `Order` aggregate is a highly central and complex aggregate root, orchestrating a significant portion of the application's core business process.
-   The use of `OrderState` and explicit state transition methods (`Next()`, `ToAddress()`, etc.) ensures that the order progresses through its lifecycle in a controlled and valid manner.
-   Domain Events are extensively used to communicate changes in order state to other parts of the system (e.g., inventory, promotions, notifications), promoting loose coupling and extensibility.
-   The `PromotionCalculator` is a clear example of a domain service that encapsulates complex domain logic that doesn't naturally fit within a single aggregate.
-   The `Order` aggregate relies heavily on references to other aggregates (e.g., `Variant`, `UserAddress`, `Promotion`), but it maintains its own consistency and business rules.
-   Financial calculations are handled in `Cents` to avoid floating-point precision issues.
