# Promotions.Promotions Bounded Context

This document describes the `Promotions.Promotions` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines, configures, and manages individual promotional offers within the system. It encompasses the core attributes of a promotion, its validity period, usage constraints, associated rules for eligibility, and the specific action it performs (e.g., a discount). This centralizes the management of marketing campaigns and ensures consistent application of promotional logic.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Promotions.Promotions` bounded context.

-   **Promotion**: A marketing offer designed to incentivize purchases, consisting of eligibility rules and specific actions that define its effect. Represented by the `Promotion` aggregate.
-   **Name**: A human-readable identifier for the promotion.
-   **Promotion Code**: A unique coupon code that customers can enter to activate a specific promotion.
-   **Description**: A detailed explanation of the promotion.
-   **Minimum Order Amount**: The minimum total value an order must have to qualify for the promotion.
-   **Maximum Discount Amount**: The upper limit on the total discount that can be applied by the promotion.
-   **Starts At / Expires At**: The date and time range during which the promotion is valid.
-   **Usage Limit**: The maximum number of times the promotion can be used across all customers.
-   **Usage Count**: The current number of times the promotion has been successfully applied.
-   **Active**: A boolean flag indicating if the promotion is manually enabled.
-   **Requires Coupon Code**: A flag indicating if a `Promotion Code` must be entered to use this promotion.
-   **Promotion Type**: The classification of the action (e.g., `OrderDiscount`, `ItemDiscount`, `FreeShipping`, `BuyXGetY`).
-   **Is Active**: A computed property indicating if the promotion is currently valid and active (manual active, dates, usage limit met).
-   **Is Expired**: A computed property indicating if the promotion's `ExpiresAt` is in the past.
-   **Promotion Action**: The concrete effect or benefit provided by a promotion (e.g., applying a discount). (Referenced from `Promotions.Actions` Bounded Context).
-   **Promotion Rule**: A condition that must be met for a promotion to be applicable. (Referenced from `Promotions.Rules` Bounded Context).
-   **Order**: A customer's purchase to which the promotion might apply. (Referenced from `Orders` Bounded Context).
-   **Order Adjustment / Line Item Adjustment**: Financial modifications resulting from the promotion. (Referenced from `Orders.Adjustments` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Promotion`**: This is the Aggregate Root. It defines a complete promotional offer and is responsible for managing its lifecycle, configuration, associated `PromotionRule`s, and its `PromotionAction`. It ensures the consistency and integrity of the promotion definition.
    -   **Entities**:
        -   `PromotionAction` (owned by `Promotion`): The specific action (e.g., discount type, free shipping) that the promotion performs.
        -   `PromotionRule` (owned by `Promotion`): Conditions that must be met for the promotion to be valid.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `PromotionCode`, `Description`, `MinimumOrderAmount`, `MaximumDiscountAmount`, `StartsAt`, `ExpiresAt`, `UsageLimit`, `UsageCount`, `Active`, and `RequiresCouponCode` are intrinsic attributes of the `Promotion` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Order` (from `Core.Domain.Orders`): Referenced by `Promotion` when a promotion is applied.
-   `OrderAdjustment` (from `Core.Domain.Orders.Adjustments`): Referenced by `Promotion` when a promotion results in an order adjustment.
-   `LineItemAdjustment` (from `Core.Domain.Orders.Adjustments`): Referenced by `Promotion` when a promotion results in a line item adjustment.

### Value Objects (standalone, if any)

-   **`PromotionType`**: An enumeration categorizing the fundamental action of a promotion.
-   **`DiscountType`**: An enumeration defining how a discount is applied (percentage or fixed amount).

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes within this bounded context, but `PromotionCalculator` (from `Promotions.Calculations`) interacts heavily with the `Promotion` aggregate.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Promotions.Promotions` bounded context.

-   `Name` is required and must adhere to length constraints.
-   `PromotionCode` (if `RequiresCouponCode` is true) is required and must adhere to length constraints.
-   `MinimumOrderAmount`, `MaximumDiscountAmount` must be non-negative.
-   `UsageLimit` must be at least 1 if specified.
-   `StartsAt` must be before `ExpiresAt` if both are provided.
-   `Promotion` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.
-   A promotion's `IsActive` status is a composite derived from its `Active` flag, dates, and `UsageLimit`.
-   The promotion `Action` cannot be null.

---

## 🤝 Relationships & Dependencies

-   **`Promotion` to `PromotionAction`**: One-to-one composition. `Promotion` is the aggregate root for its `PromotionAction` (from `Promotions.Actions`).
-   **`Promotion` to `PromotionRule`**: One-to-many composition. `Promotion` is the aggregate root for its `PromotionRule`s (from `Promotions.Rules`).
-   **`Promotion` to `Order`**: One-to-many relationship. `Orders` (from `Orders`) can reference a `Promotion`.
-   **`Promotion` to `OrderAdjustment` / `LineItemAdjustment`**: One-to-many relationships (from `Orders.Adjustments`).
-   **Shared Kernel**: `Promotion` inherits from `Aggregate` and implements `IHasUniqueName` (from `SharedKernel.Domain.Entities`), leveraging common patterns for uniqueness. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Promotion**: Instantiate a new `Promotion`, defining its name, code, validity, usage, and associated `PromotionAction`.
-   **Update Promotion Details**: Modify various attributes of an existing promotion.
-   **Increment Usage**: Increase the `UsageCount` each time the promotion is successfully applied.
-   **Validate Promotion**: Check for internal consistency (e.g., dates, code requirements).
-   **Add/Remove Rules**: Manage the eligibility conditions for the promotion.
-   **Activate/Deactivate**: Manually change the `Active` status of the promotion.
-   **Publish Domain Events**: Emit events for creation, update, usage increase, activation, deactivation, rule changes, and usage, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `Promotion` aggregate is designed to be highly configurable, allowing for a wide range of marketing scenarios.
-   The strict validation rules and date/usage constraints are critical for controlling promotion costs and preventing abuse.
-   Domain Events are essential for communicating changes in promotion status (e.g., `UsageIncreased`, `Activated`) to other parts of the system (e.g., reporting, analytics).
-   The composition with `PromotionAction` and `PromotionRule` allows for flexible and extensible behavior without bloating the core `Promotion` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
