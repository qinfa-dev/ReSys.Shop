# Promotions.Rules Bounded Context

This document describes the `Promotions.Rules` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the definition and evaluation of eligibility rules for promotions. It provides a flexible mechanism to specify conditions that must be met by an order or its contents (e.g., specific products, categories, minimum quantities, user roles) for a promotion to be applicable, thereby enabling targeted and conditional marketing campaigns.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Promotions.Rules` bounded context.

-   **Promotion Rule**: A specific condition that must be met for a `Promotion` to be applicable to an `Order`. Represented by the `PromotionRule` aggregate.
-   **Promotion**: The parent promotion that owns this rule. (Referenced from `Promotions.Promotions` Bounded Context).
-   **Rule Type**: The kind of condition the rule evaluates (e.g., `ProductInclude`, `CategoryInclude`, `MinimumQuantity`, `UserRole`).
-   **Value**: The specific data or pattern to match against for the rule (e.g., a Product ID, a minimum quantity).
-   **Promotion Rule Taxon**: A junction entity linking a `PromotionRule` to a specific `Taxon` for category-based rules.
-   **Promotion Rule User**: A junction entity linking a `PromotionRule` to a specific `ApplicationUser` for user-based rules.
-   **Taxon**: A category or node in a taxonomy. (Referenced from `Catalog.Taxonomies.Taxa` Bounded Context).
-   **Application User**: A user of the system. (Referenced from `Identity.Users` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`PromotionRule`**: This is the Aggregate Root. It defines a specific eligibility condition for a promotion and is responsible for managing its own properties and its associated `PromotionRuleTaxon`s and `PromotionRuleUser`s. It ensures the consistency and integrity of the rule definition.
    -   **Entities**:
        -   `PromotionRuleTaxon` (owned by `PromotionRule`): Links this `PromotionRule` to a specific `Taxon`.
        -   `PromotionRuleUser` (owned by `PromotionRule`): Links this `PromotionRule` to a specific `ApplicationUser`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `PromotionId`, `Type`, and `Value` are intrinsic attributes of the `PromotionRule` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Promotion` (from `Core.Domain.Promotions.Promotions`): Referenced by `PromotionRule`, but managed by its own aggregate.
-   `Taxon` (from `Core.Domain.Catalog.Taxonomies.Taxa`): Referenced by `PromotionRuleTaxon`, but managed by its own aggregate.
-   `ApplicationUser` (from `Core.Domain.Identity.Users`): Referenced by `PromotionRuleUser`, but managed by its own aggregate.
-   `Order` (from `Core.Domain.Orders`): Used by the `Evaluate` method, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`RuleType`**: An enumeration defining the various types of conditions a `PromotionRule` can check.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `Evaluate` method within `PromotionRule` performs the core logic of checking if an `Order` matches the rule.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Promotions.Rules` bounded context.

-   A `PromotionRule` must always be associated with a valid `PromotionId`.
-   `Rule Type` must be one of the predefined `Constraints.ValidRuleTypes`.
-   `Value` must adhere to maximum length constraints.
-   `PromotionRuleTaxon`s and `PromotionRuleUser`s ensure that taxons and users are not duplicated within a rule.
-   `PromotionRule` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`PromotionRule` to `Promotion`**: Many-to-one relationship. `PromotionRule` is owned by `Promotion` (from `Promotions.Promotions`).
-   **`PromotionRule` to `PromotionRuleTaxon`**: One-to-many composition. `PromotionRule` is the aggregate root for its `PromotionRuleTaxon`s.
-   **`PromotionRule` to `PromotionRuleUser`**: One-to-many composition. `PromotionRule` is the aggregate root for its `PromotionRuleUser`s.
-   **`PromotionRuleTaxon` to `Taxon`**: Many-to-one relationship.
-   **`PromotionRuleUser` to `ApplicationUser`**: Many-to-one relationship.
-   **Shared Kernel**: `PromotionRule` inherits from `Aggregate<Guid>` (from `SharedKernel.Domain.Entities`), providing common base properties. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Promotion Rule**: Instantiate a new `PromotionRule` for a promotion, specifying its type and value.
-   **Update Promotion Rule**: Modify the value of an existing rule.
-   **Delete Promotion Rule**: Remove a rule from a promotion.
-   **Evaluate Rule**: Determine if a given `Order` meets the conditions defined by the rule.
-   **Add/Remove Taxons**: Associate or disassociate specific `Taxon`s with a category-based rule.
-   **Add/Remove Users**: Associate or disassociate specific `ApplicationUser`s with a user-based rule.
-   **Publish Domain Events**: Emit events for rule creation, update, deletion, and changes to associated taxons/users, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   `PromotionRule` acts as a child aggregate within the `Promotion` aggregate, responsible for its own internal consistency but part of the larger `Promotion`'s lifecycle.
-   The `Evaluate` method is central to the promotion engine's ability to determine eligibility dynamically.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
-   Junction entities (`PromotionRuleTaxon`, `PromotionRuleUser`) allow for flexible and auditable many-to-many relationships.
