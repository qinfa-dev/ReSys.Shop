# Promotions Bounded Context

This document describes the `Promotions` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the creation, configuration, and application of various promotional offers (e.g., discounts, free shipping, buy-X-get-Y deals) to customer orders. It defines flexible rules for determining promotion eligibility and specific actions for calculating the resulting financial adjustments, thereby enabling dynamic marketing strategies and incentivizing customer purchases.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Promotions` bounded context.

-   **Promotion**: A marketing offer designed to incentivize purchases, consisting of eligibility `PromotionRule`s and specific `PromotionAction`s that define its effect. Represented by the `Promotion` aggregate.
-   **Promotion Type**: The classification of a promotional action (e.g., `OrderDiscount`, `ItemDiscount`, `FreeShipping`, `BuyXGetY`).
-   **Discount Type**: Specifies how a discount is applied, either as a `Percentage` of a value or a `FixedAmount`.
-   **Promotion Action**: The concrete effect or benefit provided by a promotion (e.g., applying a discount to an order, offering free shipping). Abstract base class `PromotionAction` with concrete implementations like `OrderDiscountAction`, `ItemDiscountAction`, `FreeShippingAction`, and `BuyXGetYDiscountAction`.
-   **Promotion Rule**: A condition that must be met for a promotion to be applicable to an order (e.g., minimum order quantity, specific products in cart, user roles). Represented by the `PromotionRule` aggregate.
-   **Rule Type**: The kind of condition a `PromotionRule` checks (e.g., `ProductInclude`, `CategoryInclude`, `MinimumQuantity`, `UserRole`).
-   **Promotion Rule Taxon**: A specific `Taxon` (category) associated with a `PromotionRule` for category-based eligibility criteria.
-   **Promotion Rule User**: A specific `ApplicationUser` associated with a `PromotionRule` for user-based eligibility criteria.
-   **Promotion Code**: A unique coupon code that customers can enter to activate a specific promotion.
-   **Minimum Order Amount**: The minimum total value an order must have to qualify for a promotion.
-   **Maximum Discount Amount**: The upper limit on the total discount that can be applied by a promotion.
-   **Usage Limit**: The total number of times a promotion can be used across all customers.
-   **Usage Count**: The current number of times a promotion has been successfully applied.
-   **Active**: A boolean flag indicating if a promotion is currently enabled and available for use.
-   **Starts At / Expires At**: The date and time range during which a promotion is valid and can be applied.
-   **Promotion Calculation Context**: An immutable object (`PromotionCalculationContext`) encapsulating all necessary data (the `Promotion`, the `Order`, and a list of `EligibleItems`) required for calculating promotion adjustments.
-   **Promotion Adjustment**: A record (`PromotionAdjustment`) representing a specific financial modification (discount, free shipping cost) applied by a promotion, including its description, amount, and optionally the `LineItem` it applies to.
-   **Promotion Calculation Result**: An immutable object (`PromotionCalculationResult`) encapsulating the outcome of applying a promotion, containing the `PromotionId` and a list of `PromotionAdjustment`s.
-   **Promotion Calculator**: A domain service responsible for evaluating promotion rules and calculating the resulting adjustments.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Promotion`**: This is the Aggregate Root. It defines a complete promotional offer, including its name, code, description, validity period, usage limits, and its associated `PromotionRule`s and `PromotionAction`. It is responsible for managing its own lifecycle and ensuring its eligibility and effects are consistently applied.
    -   **Entities**:
        -   `PromotionRule` (owned by `Promotion`): Represents a specific condition that must be met for the promotion to apply.
        -   `PromotionAction` (owned by `Promotion`): Represents the concrete effect of the promotion (e.g., a discount, free shipping). This is a polymorphic relationship, with concrete types like `OrderDiscountAction`, `ItemDiscountAction`, etc.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Code`, `Description`, `MinimumOrderAmount`, `MaximumDiscountAmount`, `StartsAt`, `ExpiresAt`, `UsageLimit`, `UsageCount`, `Active`, and `RequiresCouponCode` are intrinsic attributes of the `Promotion` aggregate.

-   **`PromotionRule`**: This is also an Aggregate Root. It defines a specific condition that must be met for a promotion to apply. It can have associated `PromotionRuleTaxon`s and `PromotionRuleUser`s to define more complex eligibility criteria.
    -   **Entities**:
        -   `PromotionRuleTaxon` (owned by `PromotionRule`): Links a `PromotionRule` to a specific `Taxon` (from `Catalog.Taxonomies`) for category-based eligibility.
        -   `PromotionRuleUser` (owned by `PromotionRule`): Links a `PromotionRule` to a specific `ApplicationUser` (from `Identity.Users`) for user-based eligibility.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Type`, `Value`, and `Settings` are intrinsic attributes of the `PromotionRule` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Order` (from `Core.Domain.Orders`): Referenced by `PromotionCalculator` and `PromotionRule` for evaluation, but managed by its own aggregate.
-   `LineItem` (from `Core.Domain.Orders.LineItems`): Referenced by `PromotionCalculator` for identifying eligible items, but managed by its own aggregate.
-   `Variant` (from `Core.Domain.Catalog.Products.Variants`): Referenced by `PromotionRule` and `PromotionAction` implementations, but managed by its own aggregate.
-   `Taxon` (from `Core.Domain.Catalog.Taxonomies`): Referenced by `PromotionRuleTaxon`, but managed by its own aggregate.
-   `ApplicationUser` (from `Core.Domain.Identity.Users`): Referenced by `PromotionRuleUser`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`PromotionCalculationContext`**: An immutable record that bundles the `Promotion`, the `Order`, and a list of `EligibleItems` needed for promotion calculation.
-   **`PromotionAdjustment`**: An immutable record representing a financial adjustment (e.g., a discount) applied by a promotion.
-   **`PromotionCalculationResult`**: An immutable record encapsulating the outcome of a promotion calculation, including the `PromotionId` and a list of `PromotionAdjustment`s.
-   **`PromotionType`**: An enumeration defining the various kinds of promotional actions.
-   **`DiscountType`**: An enumeration defining how a discount is applied (percentage or fixed amount).
-   **`RuleType`**: An enumeration defining the various types of conditions a `PromotionRule` can check.

---

## ⚙️ Domain Services (if any)

-   **`PromotionCalculator`**: This is a domain service responsible for orchestrating the evaluation of a `Promotion` against an `Order`. It checks the `PromotionRule`s for eligibility and then delegates to the `PromotionAction` to calculate the resulting `PromotionAdjustment`s. This service encapsulates the complex logic of promotion application.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Promotions` bounded context.

-   A `Promotion` must be `Active` and within its `StartsAt` and `ExpiresAt` date range to be considered applicable.
-   A `Promotion` cannot be applied if its `UsageLimit` has been reached (`UsageCount >= UsageLimit`).
-   If a `Promotion` has a `MinimumOrderAmount`, the order's item total must meet or exceed this amount.
-   If a `Promotion` has a `MaximumDiscountAmount`, the calculated discount will be capped at this value.
-   If a `Promotion` `RequiresCouponCode`, the provided `Promotion Code` must match the promotion's code.
-   `PromotionRule`s must be of a valid `RuleType` as defined in `PromotionRule.Constraints.ValidRuleTypes`.
-   `PromotionRule`s of type `UserRole` require an associated `ApplicationUser`.
-   `PromotionRule`s of type `CategoryInclude` or `CategoryExclude` require associated `Taxon`s.
-   `PromotionAction`s (e.g., `BuyXGetYDiscountAction`, `ItemDiscountAction`, `OrderDiscountAction`) enforce their own specific constraints on discount values or quantities.
-   `Promotion` names and codes have defined length constraints (`Constraints.NameMaxLength`, `Constraints.CodeMaxLength`).
-   `UsageLimit` must be at least 1 if specified.
-   Discount values (for `ItemDiscountAction` and `OrderDiscountAction`) must be non-negative.

---

## 🤝 Relationships & Dependencies

-   **`Promotion` to `PromotionRule`**: One-to-many composition. `Promotion` is the aggregate root for its `PromotionRule`s.
-   **`Promotion` to `PromotionAction`**: One-to-one composition. `Promotion` is the aggregate root for its `PromotionAction`.
-   **`PromotionRule` to `PromotionRuleTaxon`**: One-to-many composition. `PromotionRule` is the aggregate root for its `PromotionRuleTaxon`s.
-   **`PromotionRule` to `PromotionRuleUser`**: One-to-many composition. `PromotionRule` is the aggregate root for its `PromotionRuleUser`s.
-   **External Aggregates**: References `Order` (from `Core.Domain.Orders`), `LineItem` (from `Core.Domain.Orders.LineItems`), `Variant` (from `Core.Domain.Catalog.Products.Variants`), `Taxon` (from `Core.Domain.Catalog.Taxonomies`), and `ApplicationUser` (from `Core.Domain.Identity.Users`).
-   **Shared Kernel**: `Promotion` and `PromotionRule` inherit from `Aggregate` and `Aggregate<Guid>` respectively, and implement `IHasUniqueName` (from `SharedKernel.Domain.Entities`). They also leverage `SharedKernel.Validations` for consistent input validation.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Promotion**: Define a new promotional offer, specifying its name, optional code, description, validity period, usage limits, and the specific `PromotionAction` it performs.
-   **Update Promotion Details**: Modify the properties of an existing `Promotion`, including its name, code, dates, limits, active status, and whether it requires a coupon code.
-   **Increment Promotion Usage**: Record each successful application of a promotion, increasing its `UsageCount`.
-   **Add/Remove Promotion Rules**: Define or modify the eligibility criteria for a `Promotion` by adding or removing `PromotionRule`s.
-   **Add/Remove Taxons/Users to Rules**: Refine `PromotionRule`s by associating them with specific `Taxon`s (for category-based rules) or `ApplicationUser`s (for user-specific rules).
-   **Evaluate Promotion Eligibility**: Use the `PromotionCalculator` to determine if a `Promotion` is applicable to a given `Order` based on its `PromotionRule`s and other criteria.
-   **Calculate Promotion Adjustments**: Determine the financial impact of an eligible `Promotion` on an `Order` by executing its `PromotionAction`.
-   **Publish Domain Events**: Emit domain events for `Promotion` creation, update, usage, and rule changes, enabling a decoupled architecture where other parts of the system can react asynchronously.

---

## 📝 Considerations / Notes

-   The `Promotions` domain is highly dynamic and rule-driven, allowing for complex marketing campaigns.
-   The separation of `PromotionRule`s and `PromotionAction`s from the `Promotion` aggregate itself promotes flexibility and extensibility, allowing new rules and actions to be added without modifying the core `Promotion` logic.
-   The `PromotionCalculator` acts as a crucial domain service, orchestrating the evaluation and application of promotions, which involves interactions between multiple aggregates and entities.
-   Domain Events are essential for communicating promotion-related activities (e.g., `PromotionUsed`) to other parts of the system, such as reporting or analytics.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   The design supports various types of promotions through polymorphism of `PromotionAction` and `PromotionRule`.
