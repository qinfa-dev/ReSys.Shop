# Catalog.Taxonomies Bounded Context

This document describes the `Catalog.Taxonomies` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the hierarchical classification of products using taxonomies and taxons. It provides robust mechanisms for organizing products into categories, brands, or other hierarchical structures, including features for automatic product classification based on defined rules, SEO optimization, and efficient tree traversal.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies` bounded context.

-   **Taxonomy**: A top-level hierarchical classification system (e.g., "Categories", "Brands"). Represented by the `Taxonomy` aggregate.
-   **Taxon**: A single node within a `Taxonomy`, representing a category, subcategory, or tag. `Taxon`s form a tree structure. Represented by the `Taxon` aggregate.
-   **Root Taxon**: The top-level `Taxon` in a `Taxonomy`, which has no parent.
-   **Nested Set**: A data structure (Lft, Rgt, Depth properties on `Taxon`) used to efficiently manage and query hierarchical data, enabling quick retrieval of ancestors, descendants, and siblings.
-   **Permalink**: A permanent, URL-friendly link to a `Taxon`, automatically generated based on its position in the hierarchy.
-   **Pretty Name**: A human-readable, hierarchical name for a `Taxon` (e.g., "Electronics -> Laptops"), used for display purposes.
-   **Taxon Image**: An image associated with a `Taxon` (e.g., a category icon or banner).
-   **Automatic Taxon**: A `Taxon` whose product assignments are determined dynamically and automatically by a set of defined `TaxonRule`s, rather than manual assignment.
-   **Taxon Rule**: A rule that defines specific criteria (e.g., product name contains "laptop", product price is > 1000) for automatically classifying products into an `Automatic Taxon`.
-   **Rules Match Policy**: Specifies how multiple `TaxonRule`s within an `Automatic Taxon` are combined (e.g., "all" rules must match, or "any" rule can match).
-   **Sort Order**: Defines how products within a `Taxon` are sorted (e.g., "manual", "price_asc", "name_desc").
-   **Classification**: The explicit link between a `Product` and a `Taxon`, indicating that a product belongs to a specific category or classification.
-   **SEO Metadata**: Information for search engine optimization associated with a `Taxon` (Meta Title, Meta Description, Meta Keywords).
-   **Hide From Nav**: A flag indicating if a `Taxon` should be hidden from navigation menus in the frontend.
-   **Metadata**: Additional, unstructured key-value data associated with a `Taxon` or `Taxonomy`, separated into `PublicMetadata` and `PrivateMetadata`.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Taxonomy`**: This is an Aggregate Root. It represents a top-level classification system (e.g., "Categories", "Brands") and is responsible for managing its collection of `Taxon`s. It ensures that a taxonomy has only one root `Taxon` and controls its overall lifecycle.
    -   **Entities**: `Taxon` (owned by `Taxonomy`).
    -   **Value Objects**: None explicitly defined as separate classes.

-   **`Taxon`**: This is also an Aggregate Root. It represents a single node in the taxonomy tree and is responsible for maintaining its hierarchical integrity (nested set properties, permalink, pretty name), managing its associated images, and defining rules for automatic product classification.
    -   **Entities**:
        -   `TaxonImage` (owned by `Taxon`): An image associated with the `Taxon` (e.g., a category icon).
        -   `TaxonRule` (owned by `Taxon`): A rule that defines criteria for automatically classifying products into this `Taxon`.
        -   `Classification` (owned by `Taxon`): A link to a `Product` (from `Catalog.Products`), indicating that a product belongs to this `Taxon`. While `Classification` is also referenced by `Product`, its management for product listing and rule application is handled by `Taxon`.
    -   **Value Objects**: None explicitly defined as separate classes.

### Entities (not part of an Aggregate Root, if any)

-   `Product` (from `Core.Domain.Catalog.Products`): Referenced by `Classification`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `TaxonRule.Apply` method acts as a helper for applying filtering logic to products, but it's part of the `TaxonRule` entity itself. The complex logic for managing the nested set hierarchy is encapsulated within the `Taxon` aggregate.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies` bounded context.

-   A `Taxonomy` can only be deleted if it has no `Taxon`s (other than its root, which is implicitly deleted with the taxonomy). (Enforced by `Taxonomy.Delete()`)
-   A `Taxon` cannot be its own parent. (Enforced by `Taxon.SetParent()`)
-   A child `Taxon` must belong to the same `Taxonomy` as its parent. (Implicitly enforced by `Taxon.Create()` and `Taxon.SetParent()` logic).
-   A `Taxonomy` can have only one root `Taxon`. (Managed by `Taxonomy` aggregate).
-   A `Taxon` cannot be deleted if it has children. (Enforced by `Taxon.Delete()`)
-   `TaxonRule`s must adhere to predefined types and match policies.
-   `TaxonRule`s of type "product_property" require a `propertyName` to specify which product property to evaluate.
-   `TaxonImage`s must have allowed content types (e.g., `image/jpeg`, `image/png`).
-   `Permalink` and `PrettyName` are automatically generated and updated based on the `Taxon` hierarchy to ensure consistent and correct URLs and display names.
-   Nested set properties (`Lft`, `Rgt`, `Depth`) are maintained for efficient tree traversal and querying.

---

## 🤝 Relationships & Dependencies

-   **`Taxonomy` to `Taxon`**: One-to-many composition. `Taxonomy` is the aggregate root for its `Taxon`s, managing their creation and overall structure.
-   **`Taxon` to `Taxon` (Parent/Children)**: Hierarchical relationship managed within the `Taxon` aggregate, forming a tree structure.
-   **`Taxon` to `TaxonImage`**: One-to-many composition. `Taxon` owns its associated images.
-   **`Taxon` to `TaxonRule`**: One-to-many composition. `Taxon` owns its rules for automatic product classification.
-   **`Taxon` to `Classification`**: One-to-many composition. `Taxon` manages its classifications, which link to `Product` (from `Catalog.Products`), indicating product membership in the taxon.
-   **Shared Kernel**: `Taxonomy` and `Taxon` implement interfaces like `IHasParameterizableName`, `IHasPosition`, `IHasSeoMetadata`, `IHasUniqueName`, `IHasMetadata` from `SharedKernel.Domain`, leveraging common patterns for naming, positioning, SEO, uniqueness, and metadata management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Taxonomy**: Establish a new top-level classification system (e.g., "Categories", "Brands").
-   **Create Taxon**: Add a new node within a `Taxonomy`, specifying its name, presentation, and optional parent.
-   **Update Taxonomy/Taxon Details**: Modify properties such as name, presentation, position, SEO metadata, and general metadata for both taxonomies and taxons.
-   **Manage Taxon Hierarchy**: Set a parent for a `Taxon`, move `Taxon`s within the tree, and update their nested set properties.
-   **Delete Taxonomy**: Remove a `Taxonomy` from the system, provided it contains no active `Taxon`s (beyond its root).
-   **Delete Taxon**: Remove a `Taxon` from the system, provided it has no child `Taxon`s.
-   **Manage Taxon Images**: Add, remove, or update images associated with a `Taxon`.
-   **Manage Taxon Rules**: Add or remove `TaxonRule`s to define criteria for automatic product classification for `Automatic Taxon`s.
-   **Regenerate Permalinks and Pretty Names**: Automatically update the URL-friendly and human-readable names based on hierarchical changes.
-   **Apply Taxon Rules**: Use `TaxonRule`s to filter and select products that match the defined criteria.
-   **Publish Domain Events**: Emit events for creation, update, deletion, movement of taxons, and product regeneration, facilitating a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `Taxonomy` and `Taxon` aggregates work in conjunction to manage a complex hierarchical structure. `Taxonomy` acts as a container for `Taxon`s, while `Taxon` manages its own sub-hierarchy and associated data.
-   The use of the Nested Set model (`Lft`, `Rgt`, `Depth`) on `Taxon` is critical for efficient querying of hierarchical data, such as finding all descendants or ancestors.
-   `Automatic Taxon`s and `TaxonRule`s provide powerful capabilities for dynamic product categorization, reducing manual effort.
-   Domain Events are extensively used to signal changes within the taxonomy structure, allowing other parts of the system (e.g., product indexing, cache invalidation) to react appropriately.
-   The `Classification` entity serves as the explicit link between a `Product` and a `Taxon`, enabling products to belong to multiple categories.
