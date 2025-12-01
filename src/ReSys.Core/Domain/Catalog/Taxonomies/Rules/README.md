# Catalog.Taxonomies.Rules Bounded Context

This document describes the `Catalog.Taxonomies.Rules` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines rules that enable the automatic classification of products into specific taxons. It provides a flexible mechanism for dynamically categorizing products based on various criteria (e.g., product properties, names), reducing manual effort and ensuring consistent product placement within the catalog hierarchy.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies.Rules` bounded context.

-   **Taxon Rule**: A specific condition or set of criteria used to automatically associate products with a `Taxon`. Represented by the `TaxonRule` entity.
-   **Taxon**: The target category or node in a taxonomy to which products are classified. (Referenced from `Catalog.Taxonomies.Taxa` Bounded Context).
-   **Rule Type**: The kind of criteria the rule evaluates (e.g., "product_name", "product_sku", "product_property").
-   **Value**: The specific data or pattern to match against (e.g., "laptop", "color:red").
-   **Match Policy**: How the `Value` should be compared (e.g., "is_equal_to", "contains", "greater_than").
-   **Property Name**: For "product_property" rule types, this specifies which product property the rule applies to.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `TaxonRule` is an entity that is owned by the `Taxon` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`TaxonRule`**: This is the central entity of this bounded context. It defines a single rule for automatic product classification. It is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Type`, `Value`, `MatchPolicy`, and `PropertyName` are intrinsic attributes of the `TaxonRule` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `Evaluate` method, although part of the `TaxonRule` entity, performs the core logic of checking if an `Order` (and its products) matches the rule. The extension methods in `Taxon.Extensions.cs` provide helper functions for converting rules to query filters.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies.Rules` bounded context.

-   A `TaxonRule` must always be associated with a valid `TaxonId`.
-   `Rule Type` must be one of the predefined `Constraints.RuleTypes`.
-   `Match Policy` must be one of the predefined `Constraints.MatchPolicies`.
-   For "product_property" `RuleType`, `PropertyName` is required.
-   A specific rule (combination of `Type`, `Value`, `MatchPolicy`, `PropertyName`) cannot be duplicated within a `Taxon`.
-   `TaxonRule` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`TaxonRule` to `Taxon`**: Many-to-one relationship. `TaxonRule` is owned by `Taxon` (from `Catalog.Taxonomies.Taxa`).
-   **Shared Kernel**: `TaxonRule` inherits from `AuditableEntity` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Taxon Rule**: Define a new rule for a taxon, specifying its type, value, match policy, and optional product property name.
-   **Update Taxon Rule**: Modify the type, value, match policy, or property name of an existing rule.
-   **Delete Taxon Rule**: Remove a rule from a taxon.
-   **Evaluate Rule**: Determine if a given `Order` (and its associated products) satisfies the conditions defined by the rule.

---

## 📝 Considerations / Notes

-   `TaxonRule` acts as a child entity within the `Taxon` aggregate, and its lifecycle is managed by the `Taxon` aggregate.
-   The domain model provides static arrays (`Constraints.MatchPolicies`, `Constraints.RuleTypes`) to ensure only valid policies and types are used.
-   The `Evaluate` method is crucial for the "Automatic Taxon" feature, dynamically assigning products based on these rules.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
