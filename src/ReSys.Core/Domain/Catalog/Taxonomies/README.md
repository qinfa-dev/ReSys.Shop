# Catalog.Taxonomies Bounded Context

This document describes the `Catalog.Taxonomies` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the hierarchical classification of products using taxonomies and taxons. It provides robust mechanisms for organizing products into categories, brands, or other hierarchical structures, including features for automatic product classification based on defined rules, SEO optimization, and efficient tree traversal.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies` bounded context.

-   **Taxonomy**: A top-level hierarchical classification system (e.g., "Categories", "Brands"). Represented by the <see cref="Taxonomy"/> aggregate.
-   **Taxon**: A single node within a <see cref="Taxonomy"/>, representing a category, subcategory, or tag. <see cref="Taxon"/>s form a tree structure. Represented by the <see cref="Taxon"/> aggregate.
-   **Parent/Child Taxon**: Defines the hierarchical relationship between taxons.
-   **Name**: The internal, unique identifier of the <see cref="Taxonomy"/> or <see cref="Taxon"/>.
-   **Presentation**: The human-readable display name of the <see cref="Taxonomy"/> or <see cref="Taxon"/>.
-   **Description**: A detailed explanation of the <see cref="Taxon"/>.
-   **Permalink**: A unique, URL-friendly path generated based on the <see cref="Taxon"/>'s position in the hierarchy.
-   **Pretty Name**: A human-readable, hierarchical name for a <see cref="Taxon"/> (e.g., "Electronics -> Laptops"), used for display purposes.
-   **Hide From Nav**: A flag indicating whether the <see cref="Taxon"/> should be visible in navigation menus.
-   **Position**: The display order among siblings (for <see cref="Taxon"/>) or among other taxonomies (for <see cref="Taxonomy"/>).
-   **Nested Set Properties**: <c>Lft</c>, <c>Rgt</c>, and <c>Depth</c> values on <see cref="Taxon"/>s used for efficient tree traversal and querying.
-   **Automatic Taxon**: A <see cref="Taxon"/> whose product assignments are determined dynamically and automatically by a set of defined <see cref="TaxonRule"/>s, rather than manual assignment.
-   **Taxon Rule**: A rule that defines specific criteria (e.g., product name contains "laptop", product price is > 1000) for automatically classifying products into an <c>Automatic Taxon</c>.
-   **Rules Match Policy**: Specifies how multiple <see cref="TaxonRule"/>s within an <c>Automatic Taxon</c> are combined (e.g., "all" rules must match, or "any" rule can match).
-   **Sort Order**: Defines how products within a <see cref="Taxon"/> are sorted (e.g., "manual", "price_asc", "name_desc").
-   **Classification**: The explicit link between a <c>Product</c> and a <see cref="Taxon"/>, indicating that a product belongs to a specific category or classification.
-   **SEO Metadata**: Information for search engine optimization associated with a <see cref="Taxon"/> (<c>MetaTitle</c>, <c>MetaDescription</c>, <c>MetaKeywords</c>).
-   **Metadata**: Additional, unstructured key-value data associated with a <see cref="Taxon"/> or <see cref="Taxonomy"/>, separated into <c>PublicMetadata</c> and <c>PrivateMetadata</c>.
-   **Taxon Image**: An image associated with a <see cref="Taxon"/>. (Referenced from `Catalog.Taxonomies.Images` Bounded Context).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Taxonomy`**: This is an Aggregate Root. It represents a top-level classification system (e.g., "Categories", "Brands") and is responsible for managing its collection of <see cref="Taxon"/>s. It ensures that a taxonomy has only one root <see cref="Taxon"/> and controls its overall lifecycle.
    -   **Entities**: <see cref="Taxon"/> (owned by <see cref="Taxonomy"/>).
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>Name</c>, <c>Presentation</c>, <c>Position</c>, <c>StoreId</c>, <c>PublicMetadata</c>, and <c>PrivateMetadata</c> are intrinsic attributes of the <see cref="Taxonomy"/> aggregate.

-   **`Taxon`**: This is also an Aggregate Root. It represents a single node in the taxonomy tree and is responsible for maintaining its hierarchical integrity (nested set properties, permalink, pretty name), managing its associated images, and defining rules for automatic product classification.
    -   **Entities**:
        -   <see cref="TaxonImage"/> (owned by <see cref="Taxon"/>): Images specifically associated with this taxon.
        -   <see cref="TaxonRule"/> (owned by <see cref="Taxon"/>): Rules that define criteria for automatically classifying products into this taxon.
        -   <see cref="Classification"/> (owned by <see cref="Taxon"/>): Links to `Product` entities, indicating membership in this taxon.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>Name</c>, <c>Presentation</c>, <c>Description</c>, <c>Permalink</c>, <c>PrettyName</c>, <c>HideFromNav</c>, <c>Position</c>, <c>Lft</c>, <c>Rgt</c>, <c>Depth</c>, <c>Automatic</c>, <c>RulesMatchPolicy</c>, <c>SortOrder</c>, <c>MarkedForRegenerateTaxonProducts</c>, <c>MetaTitle</c>, <c>MetaDescription</c>, <c>MetaKeywords</c>, <c>PublicMetadata</c>, and <c>PrivateMetadata</c> are intrinsic attributes of the <see cref="Taxon"/> aggregate.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `TaxonRule.Apply` method acts as a helper for applying filtering logic to products, but it's part of the `TaxonRule` entity itself. The complex logic for managing the nested set hierarchy is encapsulated within the `Taxon` aggregate.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies` bounded context.

-   A <see cref="Taxonomy"/> can only be deleted if it contains no <see cref="Taxon"/>s (or only its root taxon). The <see cref="Taxonomy.Delete()"/> method enforces this by returning <see cref="Taxonomy.Errors.HasTaxons"/>.
-   A <see cref="Taxon"/> cannot be its own parent. The <see cref="Taxon.SetParent(Guid?, int)"/> method enforces this by returning <see cref="Taxon.Errors.SelfParenting"/>.
-   A child <see cref="Taxon"/> must belong to the same <see cref="Taxonomy"/> as its parent. The <see cref="Taxon.SetParent(Guid?, int)"/> method enforces this by returning <see cref="Taxon.Errors.ParentTaxonomyMismatch"/>.
-   A <see cref="Taxonomy"/> can have only one root <see cref="Taxon"/>. Attempts to create a second root will result in <see cref="Taxon.Errors.RootConflict"/>.
-   A <see cref="Taxon"/> cannot be deleted if it has children. The <see cref="Taxon.Delete()"/> method enforces this by returning <see cref="Taxon.Errors.HasChildren"/>. Children must be reparented or deleted first.
-   <c>Permalink</c> and <c>PrettyName</c> for a <see cref="Taxon"/> are automatically generated and updated based on its name and hierarchical position using <see cref="Taxon.RegeneratePermalinkAndPrettyName(string?, string?)"/>.
-   Nested set properties (<c>Lft</c>, <c>Rgt</c>, <c>Depth</c>) on <see cref="Taxon"/>s are maintained via <see cref="Taxon.UpdateNestedSet(int, int, int)"/> to accurately reflect the hierarchy and facilitate efficient tree operations.
-   Changes to <see cref="Taxon"/>'s <c>Automatic</c> flag, <c>RulesMatchPolicy</c>, or <c>SortOrder</c> (if affecting precomputed product listings) will set the <see cref="Taxon.MarkedForRegenerateTaxonProducts"/> flag, triggering product association recalculations.
-   <see cref="TaxonRule"/>s must be valid before being added to an automatic taxon (e.g., correct <c>Type</c>, <c>MatchPolicy</c>, required <c>PropertyName</c>). Invalid rules will result in <see cref="TaxonRule.Errors.InvalidType"/> or <see cref="TaxonRule.Errors.InvalidMatchPolicy"/>.
-   Duplicate <see cref="TaxonRule"/>s (with the same type, value, match policy, and property name) are not allowed for a single taxon. Adding a duplicate will result in <see cref="TaxonRule.Errors.Duplicate"/>.
-   <see cref="TaxonImage"/>s must have allowed content types (as defined in <see cref="Taxon.Constraints.ImageContentTypes"/>).

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

-   **Create Taxonomy**: Establish a new top-level classification system using <see cref="Taxonomy.Create(Guid, string, string, int, IDictionary{string, object?}?, IDictionary{string, object?}?)"/> (e.g., "Categories", "Brands").
-   **Create Taxon**: Add a new node within a <see cref="Taxonomy"/> using <see cref="Taxon.Create(Guid, string, Guid?, string?, string?, int, bool, bool, string?, string?, string?, string?, string?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>, specifying its name, parent, and initial properties.
-   **Update Taxonomy/Taxon Details**:
    -   Modify properties of a <see cref="Taxonomy"/> (name, presentation, position, metadata) using <see cref="Taxonomy.Update(Guid?, string?, string?, int?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>.
    -   Modify properties of a <see cref="Taxon"/> (name, presentation, description, SEO, automatic settings) using <see cref="Taxon.Update(string?, string?, Guid?, string?, int?, bool?, bool?, string?, string?, string?, string?, string?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>.
-   **Manage Taxon Hierarchy**:
    -   Change a <see cref="Taxon"/>'s parent and its position among siblings using <see cref="Taxon.SetParent(Guid?, int)"/>, triggering hierarchy recalculations (handled externally).
    -   Directly add/remove child taxons using <see cref="Taxon.AddChild(Taxon?)"/> and <see cref="Taxon.RemoveChild(Taxon?)"/>.
-   **Delete Taxonomy**: Remove a <see cref="Taxonomy"/> from the system using <see cref="Taxonomy.Delete()"/>, which only succeeds if it contains no <see cref="Taxon"/>s (or only a root taxon).
-   **Delete Taxon**: Remove a <see cref="Taxon"/> from the system using <see cref="Taxon.Delete()"/>, which returns an error if child taxons are present.
-   **Manage Taxon Images**:
    -   <see cref="Taxon.AddImage(TaxonImage)"/>: Add an image to a <see cref="Taxon"/>.
    -   <see cref="Taxon.RemoveImage(Guid)"/>: Remove an image from a <see cref="Taxon"/>.
-   **Manage Taxon Rules (for Automatic Taxons)**:
    -   <see cref="Taxon.AddTaxonRule(TaxonRule?)"/>: Add a rule for automatic product classification.
    -   <see cref="Taxon.RemoveRule(Guid)"/>: Remove a rule from a <see cref="Taxon"/>.
-   **Regenerate Permalinks and Pretty Names**: Automatically update URL-friendly (<c>Permalink</c>) and human-readable (<c>PrettyName</c>) names for a <see cref="Taxon"/> after hierarchical or naming changes, using <see cref="Taxon.RegeneratePermalinkAndPrettyName(string?, string?)"/>.
-   **Update Nested Set Properties**: Maintain <c>Lft</c>, <c>Rgt</c>, <c>Depth</c> values for efficient tree operations using <see cref="Taxon.UpdateNestedSet(int, int, int)"/>, typically managed by an external service.
-   **Publish Domain Events**: <see cref="Taxonomy"/> and <see cref="Taxon"/> publish various events (e.g., <see cref="Taxonomy.Events.Created"/>, <see cref="Taxon.Events.Moved"/>, <see cref="Taxon.Events.RegenerateProducts"/>) for cross-domain communication and asynchronous processing.

---

## 📝 Considerations / Notes

-   The `Taxonomy` and `Taxon` aggregates work in conjunction to manage a complex hierarchical structure. `Taxonomy` acts as a container for `Taxon`s, while `Taxon` manages its own sub-hierarchy and associated data.
-   The use of the Nested Set model (`Lft`, `Rgt`, `Depth`) on `Taxon` is critical for efficient querying of hierarchical data, such as finding all descendants or ancestors.
-   `Automatic Taxon`s and `TaxonRule`s provide powerful capabilities for dynamic product categorization, reducing manual effort.
-   Domain Events are extensively used to signal changes within the taxonomy structure, allowing other parts of the system (e.g., product indexing, cache invalidation) to react appropriately.
-   The `Classification` entity serves as the explicit link between a `Product` and a `Taxon`, enabling products to belong to multiple categories.
