# Promotions.Calculations Bounded Context

This document describes the `Promotions.Calculations` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain is responsible for the precise evaluation and application of promotional offers to customer orders. It encapsulates the complex logic required to check eligibility rules, identify eligible line items, and calculate the resulting financial adjustments, ensuring that promotions are applied accurately and consistently according to their defined criteria.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Promotions.Calculations` bounded context.

-   **Promotion Calculator**: A domain service responsible for evaluating `PromotionRule`s and calculating `PromotionAdjustment`s for an `Order`. Represented by the `PromotionCalculator` static class.
-   **Promotion Calculation Context**: An immutable record (`PromotionCalculationContext`) encapsulating all necessary data (the `Promotion`, the `Order`, and a list of `EligibleItems`) required for calculating promotion adjustments.
-   **Promotion Adjustment**: A record (`PromotionAdjustment`) representing a specific financial modification (e.g., a discount) applied by a promotion, including its `Description`, `Amount`, and optionally the `LineItem` it applies to.
-   **Promotion Calculation Result**: An immutable record (`PromotionCalculationResult`) encapsulating the outcome of applying a promotion, containing the `PromotionId` and a list of `PromotionAdjustment`s.
-   **Promotion**: The marketing offer being evaluated. (Referenced from `Promotions.Promotions` Bounded Context).
-   **Order**: The customer's order against which the promotion is being applied. (Referenced from `Orders` Bounded Context).
-   **Line Item**: Individual items within the `Order` that may be eligible for promotion. (Referenced from `Orders.LineItems` Bounded Context).
-   **Eligible Items**: A subset of `Line Item`s within an order that qualify for a promotion's item-level actions.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. This domain primarily consists of value objects and a domain service.

### Entities (not part of an Aggregate Root, if any)

-   None.

### Value Objects (standalone, if any)

-   **`PromotionAdjustment`**: An immutable record defining a single financial adjustment resulting from a promotion.
-   **`PromotionCalculationContext`**: An immutable record that provides all contextual data needed for promotion calculation.
-   **`PromotionCalculationResult`**: An immutable record encapsulating the output of a promotion calculation.

---

## ⚙️ Domain Services (if any)

-   **`PromotionCalculator`**: This static class acts as a domain service. It orchestrates the process of checking promotion validity (dates, usage limits), evaluating rules (delegating to `PromotionRule.Evaluate`), and then calculating adjustments (delegating to `PromotionAction.Calculate`). It ensures that promotions are applied correctly according to all defined criteria.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Promotions.Calculations` bounded context.

-   A promotion must be `Active` and within its `StartsAt`/`ExpiresAt` dates to be considered.
-   The promotion's `UsageLimit` must not be exceeded.
-   All associated `PromotionRule`s must evaluate to true (or according to the `RulesMatchPolicy`) for the promotion to apply.
-   If a `MinimumOrderAmount` is set for the promotion, the order's item total must meet this requirement.
-   Calculated discounts are capped by the promotion's `MaximumDiscountAmount`, if specified.
-   The calculation process must correctly identify eligible `Line Item`s based on `PromotionRule`s.
-   All financial adjustments are handled in cents to avoid precision issues.

---

## 🤝 Relationships & Dependencies

-   **`PromotionCalculator` to `Promotion`**: Depends on the `Promotion` aggregate (from `Promotions.Promotions`) for its rules, actions, and general configuration.
-   **`PromotionCalculator` to `Order`**: Depends on the `Order` aggregate (from `Orders`) for order details and line items.
-   **`PromotionCalculator` to `PromotionRule`**: Delegates rule evaluation to the `PromotionRule` entity (from `Promotions.Rules`).
-   **`PromotionCalculator` to `PromotionAction`**: Delegates adjustment calculation to the `PromotionAction` entity (from `Promotions.Actions`).

---

## 🚀 Key Use Cases / Behaviors

-   **Calculate Promotion**: Given a `Promotion` and an `Order`, determine if the promotion is applicable and, if so, compute the resulting `PromotionAdjustment`s.
-   **Identify Eligible Line Items**: Filter the `Order`'s `Line Item`s based on item-level `PromotionRule`s to determine which items qualify for item-specific discounts.

---

## 📝 Considerations / Notes

-   This domain is purely functional, focused on the calculation logic of promotions without altering the state of the `Promotion` or `Order` aggregates directly. It returns immutable results.
-   The `PromotionCalculator` is a prime example of a Domain Service, coordinating multiple aggregates and encapsulating complex domain logic that doesn't fit naturally within a single aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, explicitly communicating validation failures during calculation.
-   The context object (`PromotionCalculationContext`) ensures all necessary data for calculation is passed cleanly.
