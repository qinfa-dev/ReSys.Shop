# Catalog.Taxonomies.Rules Bounded Context

This document describes the `Catalog.Taxonomies.Rules` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines rules that enable the automatic classification of products into specific taxons. It provides a flexible mechanism for dynamically categorizing products based on various criteria (e.g., product properties, names), reducing manual effort and ensuring consistent product placement within the catalog hierarchy.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies.Rules` bounded context.

-   **Taxon Rule**: A specific condition or set of criteria used to automatically associate products with a <see cref="Taxon"/>. Represented by the <see cref="TaxonRule"/> entity.
-   **Taxon**: The target category or node in a taxonomy to which products are classified. (Referenced from `Catalog.Taxonomies.Taxa` Bounded Context).
-   **Rule Type**: The kind of criteria the rule evaluates (e.g., "product_name", "product_sku", "product_property"). Predefined types are listed in <see cref="TaxonRule.Constraints.RuleTypes"/>.
-   **Value**: The specific data or pattern to match against (e.g., "laptop", "color:red").
-   **Match Policy**: How the <c>Value</c> should be compared (e.g., "is_equal_to", "contains", "greater_than"). Predefined policies are listed in <see cref="TaxonRule.Constraints.MatchPolicies"/>.
-   **Property Name**: For "product_property" rule types, this specifies which product property the rule applies to. It's required for this rule type.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `TaxonRule` is an entity that is owned by the `Taxon` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`TaxonRule`**: This is the central entity of this bounded context. It defines a single rule for automatic product classification. It is an <see cref="AuditableEntity"/> and includes properties for <c>Type</c>, <c>Value</c>, <c>MatchPolicy</c>, and optional <c>PropertyName</c>.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>Type</c>, <c>Value</c>, <c>MatchPolicy</c>, and <c>PropertyName</c> are intrinsic attributes of the <see cref="TaxonRule"/> entity.

### Value Objects (standalone, if any)

-   None.

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `Evaluate` method, although part of the `TaxonRule` entity, performs the core logic of checking if an `Order` (and its products) matches the rule. The extension methods in `Taxon.Extensions.cs` provide helper functions for converting rules to query filters.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies.Rules` bounded context.

-   A <see cref="TaxonRule"/> must always be associated with a valid <c>TaxonId</c>.
-   <c>Rule Type</c> must be one of the predefined types in <see cref="TaxonRule.Constraints.RuleTypes"/>. Invalid types will result in <see cref="TaxonRule.Errors.InvalidType"/>.
-   <c>Match Policy</c> must be one of the predefined policies in <see cref="TaxonRule.Constraints.MatchPolicies"/>. Invalid policies will result in <see cref="TaxonRule.Errors.InvalidMatchPolicy"/>.
-   For "product_property" <c>RuleType</c>, <c>PropertyName</c> is required. If missing, it will result in <see cref="TaxonRule.Errors.PropertyNameRequired"/>.
-   A specific rule (combination of <c>Type</c>, <c>Value</c>, <c>MatchPolicy</c>, <c>PropertyName</c>) should be unique within a <see cref="Taxon"/>. Duplicates may result in <see cref="TaxonRule.Errors.Duplicate"/>.
-   <see cref="TaxonRule"/> instances track their creation and update timestamps (<c>CreatedAt</c>, <c>UpdatedAt</c>), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`TaxonRule` to `Taxon`**: Many-to-one relationship. `TaxonRule` is owned by `Taxon` (from `Catalog.Taxonomies.Taxa`).
-   **Shared Kernel**: `TaxonRule` inherits from `AuditableEntity` (from `SharedKernel.Domain.Entities`), leveraging common patterns for auditing. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Taxon Rule**: Define a new rule for a taxon using <see cref="TaxonRule.Create(Guid, string, string, string?, string?)"/>, specifying its <c>Type</c>, <c>Value</c>, <c>MatchPolicy</c>, and optional <c>PropertyName</c>. This method performs validation for the rule's attributes.
-   **Update Taxon Rule**: Modify the <c>Type</c>, <c>Value</c>, <c>MatchPolicy</c>, or <c>PropertyName</c> of an existing rule using <see cref="TaxonRule.Update(string?, string?, string?, string?)"/>. This supports partial updates and re-validates the rule.
-   **Delete Taxon Rule**: Signal the removal of a rule from a taxon using <see cref="TaxonRule.Delete()"/>. The actual removal from the parent's collection is managed by the <see cref="Taxon"/> aggregate.
-   **Evaluate Rule**: The <see cref="TaxonRule"/> entity itself contains logic (though not exposed as a public method in the snippet, it's implicitly part of the domain's evaluation process) to determine if a given <c>Product</c> (and its associated attributes) satisfies the conditions defined by the rule.

---

## 📝 Considerations / Notes

-   `TaxonRule` acts as a child entity within the `Taxon` aggregate, and its lifecycle is managed by the `Taxon` aggregate.
-   The domain model provides static arrays (`Constraints.MatchPolicies`, `Constraints.RuleTypes`) to ensure only valid policies and types are used.
-   The `Evaluate` method is crucial for the "Automatic Taxon" feature, dynamically assigning products based on these rules.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
