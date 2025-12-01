# Catalog.Taxonomies.Taxa Bounded Context

This document describes the `Catalog.Taxonomies.Taxa` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the individual nodes (taxons) within a hierarchical product classification system. It provides comprehensive mechanisms for defining taxon properties, maintaining their hierarchical structure (using a nested set model), generating SEO-friendly URLs and names, associating images and rules for automatic product classification, and linking products to these categories.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies.Taxa` bounded context.

-   **Taxon**: A single node within a `Taxonomy`, representing a category, subcategory, or tag. `Taxon`s form a tree structure. Represented by the `Taxon` aggregate.
-   **Taxonomy**: The overarching classification system (e.g., "Categories", "Brands") to which the taxon belongs. (Referenced from `Catalog.Taxonomies` Bounded Context).
-   **Parent/Child Taxon**: Defines the hierarchical relationship between taxons.
-   **Name**: The internal, unique identifier of the taxon.
-   **Presentation**: The human-readable display name of the taxon.
-   **Description**: A detailed explanation of the taxon.
-   **Permalink**: A unique, URL-friendly path generated based on the taxon's position in the hierarchy.
-   **Pretty Name**: A human-readable, hierarchical name (e.g., "Electronics -> Laptops").
-   **Hide From Nav**: A flag indicating whether the taxon should be visible in navigation menus.
-   **Position**: The display order among siblings.
-   **Nested Set Properties**: `Lft` (left), `Rgt` (right), and `Depth` values used for efficient tree traversal and querying.
-   **Automatic**: A flag indicating if product assignments to this taxon are managed dynamically by rules.
-   **Rules Match Policy**: Defines how multiple `TaxonRule`s are combined for automatic classification (e.g., "all" or "any").
-   **Sort Order**: Specifies the default sorting for products displayed within this taxon.
-   **Marked For Regenerate Taxon Products**: A flag indicating that product associations for this taxon need to be re-evaluated due to rule changes.
-   **SEO Properties**: `MetaTitle`, `MetaDescription`, `MetaKeywords` for search engine optimization.
-   **Metadata**: Additional, unstructured key-value data (`PublicMetadata`, `PrivateMetadata`).
-   **Taxon Image**: An image associated with the taxon. (Referenced from `Catalog.Taxonomies.Images` Bounded Context).
-   **Taxon Rule**: A rule for automatic product classification. (Referenced from `Catalog.Taxonomies.Rules` Bounded Context).
-   **Classification**: A link between a product and this taxon. (Referenced from `Catalog.Products.Classifications` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Taxon`**: This is the Aggregate Root. It represents a single node in the taxonomy tree and is responsible for managing its hierarchical integrity, associated images, rules, and classifications.
    -   **Entities**:
        -   `TaxonImage` (owned by `Taxon`): Images specifically associated with this taxon.
        -   `TaxonRule` (owned by `Taxon`): Rules that define criteria for automatically classifying products into this taxon.
        -   `Classification` (owned by `Taxon`): Links to `Product` entities, indicating membership in this taxon.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Presentation`, `Description`, `Permalink`, `PrettyName`, `HideFromNav`, `Position`, `Lft`, `Rgt`, `Depth`, `Automatic`, `RulesMatchPolicy`, `SortOrder`, `MarkedForRegenerateTaxonProducts`, `MetaTitle`, `MetaDescription`, `MetaKeywords`, `PublicMetadata`, and `PrivateMetadata` are intrinsic attributes of the `Taxon` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Taxonomy` (from `Core.Domain.Catalog.Taxonomies`): Referenced by `Taxon`, but managed by its own aggregate.
-   `PromotionRuleTaxon` (from `Core.Domain.Promotions.Rules`): A junction entity linking a `PromotionRule` to a `Taxon`.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. `TaxonRuleExtensions` provides helper methods for integrating `TaxonRule`s with query filters but is not a domain service in the traditional sense of operating across multiple aggregates.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies.Taxa` bounded context.

-   A `Taxon` cannot be its own parent.
-   A child `Taxon` must belong to the same `Taxonomy` as its parent.
-   A `Taxon` cannot be deleted if it has children.
-   `Permalink` and `PrettyName` are automatically generated and updated based on the `Taxon`'s name and hierarchical position.
-   Nested set properties (`Lft`, `Rgt`, `Depth`) are maintained to accurately reflect the hierarchy.
-   Changes to `Automatic`, `RulesMatchPolicy`, or `SortOrder` (if affecting precomputed lists) will `MarkForRegenerateTaxonProducts`.
-   `TaxonRule`s must be valid before being added to an automatic taxon.
-   Duplicate `TaxonRule`s are not allowed for a single taxon.
-   `TaxonImage`s can be added or removed, with handling for replacing existing images of the same type.

---

## 🤝 Relationships & Dependencies

-   **`Taxon` to `Taxonomy`**: Many-to-one relationship. `Taxon` is owned by `Taxonomy` (from `Catalog.Taxonomies`).
-   **`Taxon` to `Taxon` (Parent/Child)**: One-to-many hierarchical relationship within the `Taxon` aggregate.
-   **`Taxon` to `TaxonImage`**: One-to-many composition. `Taxon` is the aggregate root for its `TaxonImage`s.
-   **`Taxon` to `TaxonRule`**: One-to-many composition. `Taxon` is the aggregate root for its `TaxonRule`s.
-   **`Taxon` to `Classification`**: One-to-many composition. `Taxon` is the aggregate root for `Classification`s, which link to `Product` (from `Catalog.Products`).
-   **Shared Kernel**: `Taxon` inherits from `Aggregate` and implements `IHasParameterizableName`, `IHasPosition`, `IHasSeoMetadata`, `IHasUniqueName`, `IHasMetadata` (from `SharedKernel.Domain`), leveraging common patterns for naming, positioning, SEO, uniqueness, and metadata. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Taxon**: Instantiate a new `Taxon` within a `Taxonomy`, specifying its name, parent, and initial properties.
-   **Update Taxon Details**: Modify name, presentation, description, position, SEO metadata, or automatic classification settings.
-   **Move Taxon**: Change a taxon's parent and its position among siblings, triggering hierarchy recalculations.
-   **Delete Taxon**: Remove a taxon, ensuring no children exist.
-   **Manage Taxon Images**: Add, update, or remove images associated with the taxon.
-   **Manage Taxon Rules**: Add or remove rules for automatic product classification.
-   **Regenerate Permalinks and Pretty Names**: Automatically update URL-friendly and human-readable names after hierarchical changes.
-   **Update Nested Set Properties**: Maintain `Lft`, `Rgt`, `Depth` values for efficient tree operations.
-   **Publish Domain Events**: Emit events for creation, update, deletion, movement, and product regeneration, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `Taxon` aggregate is central to building and maintaining a product hierarchy, enabling rich categorization and navigation.
-   The Nested Set model is critical for performance when querying hierarchical data.
-   Automatic taxons and taxon rules provide powerful dynamic classification capabilities.
-   Domain Events are crucial for coordinating changes across related aggregates and bounded contexts, especially for re-indexing products or updating caches.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
