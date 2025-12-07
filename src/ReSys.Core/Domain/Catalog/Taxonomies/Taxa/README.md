# Catalog.Taxonomies.Taxa Bounded Context

This document describes the `Catalog.Taxonomies.Taxa` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the individual nodes (taxons) within a hierarchical product classification system. It provides comprehensive mechanisms for defining taxon properties, maintaining their hierarchical structure (using a nested set model), generating SEO-friendly URLs and names, associating images and rules for automatic product classification, and linking products to these categories.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies.Taxa` bounded context.

-   **Taxon**: A single node within a <see cref="Taxonomy"/>, representing a category, subcategory, or tag. <see cref="Taxon"/>s form a tree structure. Represented by the <see cref="Taxon"/> aggregate.
-   **Taxonomy**: The overarching classification system (e.g., "Categories", "Brands") to which the taxon belongs. (Referenced from `Catalog.Taxonomies` Bounded Context).
-   **Parent/Child Taxon**: Defines the hierarchical relationship between taxons.
-   **Name**: The internal, unique identifier of the taxon.
-   **Presentation**: The human-readable display name of the taxon.
-   **Description**: A detailed explanation of the taxon.
-   **Permalink**: A unique, URL-friendly path generated based on the taxon's position in the hierarchy.
-   **Pretty Name**: A human-readable, hierarchical name (e.g., "Electronics -> Laptops").
-   **Hide From Nav**: A flag indicating whether the taxon should be visible in navigation menus.
-   **Position**: The display order among siblings.
-   **Nested Set Properties**: <c>Lft</c> (left), <c>Rgt</c> (right), and <c>Depth</c> values used for efficient tree traversal and querying.
-   **Automatic**: A flag indicating if product assignments to this taxon are managed dynamically by rules.
-   **Rules Match Policy**: Defines how multiple <see cref="TaxonRule"/>s are combined for automatic classification (e.g., "all" or "any").
-   **Sort Order**: Specifies the default sorting for products displayed within this taxon.
-   **Marked For Regenerate Taxon Products**: A flag indicating that product associations for this taxon need to be re-evaluated due to rule changes.
-   **SEO Properties**: <c>MetaTitle</c>, <c>MetaDescription</c>, <c>MetaKeywords</c> for search engine optimization.
-   **Metadata**: Additional, unstructured key-value data (<c>PublicMetadata</c>, <c>PrivateMetadata</c>).
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

-   None explicitly defined as separate classes. However, <see cref="TaxonRuleExtensions"/> (defined in <c>Taxon.Extensions.cs</c>) provides static helper methods for integrating <see cref="TaxonRule"/>s with query filters, acting as a utility for dynamic query building rather than a traditional domain service that operates across multiple aggregates.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies.Taxa` bounded context.

-   A <see cref="Taxon"/> cannot be its own parent. The <see cref="Taxon.SetParent(Guid?, int)"/> method enforces this by returning <see cref="Taxon.Errors.SelfParenting"/>.
-   A child <see cref="Taxon"/> must belong to the same <see cref="Taxonomy"/> as its parent. The <see cref="Taxon.SetParent(Guid?, int)"/> method enforces this by returning <see cref="Taxon.Errors.ParentTaxonomyMismatch"/>.
-   Only one root taxon is allowed per <see cref="Taxonomy"/>. Attempts to create a second root will result in <see cref="Taxon.Errors.RootConflict"/>.
-   A <see cref="Taxon"/> cannot be deleted if it has children. The <see cref="Taxon.Delete()"/> method enforces this by returning <see cref="Taxon.Errors.HasChildren"/>. Children must be reparented or deleted first.
-   <c>Permalink</c> and <c>PrettyName</c> are automatically generated and updated based on the <see cref="Taxon"/>'s name, presentation, and hierarchical position using <see cref="Taxon.RegeneratePermalinkAndPrettyName(string?, string?)"/>.
-   Nested set properties (<c>Lft</c>, <c>Rgt</c>, <c>Depth</c>) are maintained via <see cref="Taxon.UpdateNestedSet(int, int, int)"/> to accurately reflect the hierarchy and facilitate efficient tree operations.
-   Changes to <c>Automatic</c> flag, <c>RulesMatchPolicy</c>, or <c>SortOrder</c> (if it affects precomputed product listings) will set the <see cref="Taxon.MarkedForRegenerateTaxonProducts"/> flag, triggering product association recalculations.
-   <see cref="TaxonRule"/>s must be valid before being added to an automatic taxon (e.g., correct <c>Type</c>, <c>MatchPolicy</c>, required <c>PropertyName</c>). Adding a rule with an invalid <c>TaxonId</c> will result in <see cref="TaxonRule.Errors.TaxonMismatch(Guid, Guid)"/>.
-   Duplicate <see cref="TaxonRule"/>s (with the same type, value, match policy, and property name) are not allowed for a single taxon. Adding a duplicate will result in <see cref="TaxonRule.Errors.Duplicate"/>.
-   When adding <see cref="TaxonImage"/>s, if an image of the same type already exists, it is typically replaced.

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

-   **Create Taxon**: Instantiate a new <see cref="Taxon"/> using <see cref="Taxon.Create(Guid, string, Guid?, string?, string?, int, bool, bool, string?, string?, string?, string?, string?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/> within a <see cref="Taxonomy"/>, specifying its name, optional parent, and initial properties including SEO and automatic classification settings.
-   **Update Taxon Details**: Modify various attributes such as name, presentation, description, position, SEO metadata, or automatic classification settings using <see cref="Taxon.Update(string?, string?, Guid?, string?, int?, bool?, bool?, string?, string?, string?, string?, string?, IDictionary{string, object?}?, IDictionary{string, object?}?)"/>. This method intelligently tracks changes that necessitate product regeneration and emits appropriate domain events.
-   **Move Taxon**: Change a taxon's parent and its position among siblings using <see cref="Taxon.SetParent(Guid?, int)"/>. This triggers hierarchy recalculations (handled externally by domain event listeners) and emits an <see cref="Taxon.Events.Moved"/> event, also validating against self-parenting and cross-taxonomy moves.
-   **Delete Taxon**: Remove a taxon using <see cref="Taxon.Delete()"/>, which returns an error (<see cref="Taxon.Errors.HasChildren"/>) if child taxons are present, enforcing that children must be handled (reparented or deleted) first.
-   **Manage Taxon Images**:
    -   <see cref="Taxon.AddImage(TaxonImage)"/>: Add an image to the taxon. If an image of the same type already exists, it is replaced.
    -   <see cref="Taxon.RemoveImage(Guid)"/>: Remove an image associated with the taxon. This returns <see cref="ErrorOr{Success}"/> or <see cref="Error.NotFound"/> if the image is not found.
-   **Manage Taxon Rules (for Automatic Taxons)**:
    -   <see cref="Taxon.AddTaxonRule(TaxonRule?)"/>: Add a rule for automatic product classification. This method validates the rule, handles duplicates, sets <see cref="MarkedForRegenerateTaxonProducts"/>, and emits <see cref="Taxon.Events.RegenerateProducts"/>.
    -   <see cref="Taxon.RemoveRule(Guid)"/>: Remove a rule from the taxon, also triggering product regeneration.
-   **Regenerate Permalinks and Pretty Names**: Automatically update URL-friendly (<c>Permalink</c>) and human-readable (<c>PrettyName</c>) names after hierarchical or naming changes, using <see cref="Taxon.RegeneratePermalinkAndPrettyName(string?, string?)"/>.
-   **Update Nested Set Properties**: Maintain <c>Lft</c>, <c>Rgt</c>, <c>Depth</c> values for efficient tree operations using <see cref="Taxon.UpdateNestedSet(int, int, int)"/>. This is typically managed by an external service that re-calculates the entire tree.
-   **Publish Domain Events**: <see cref="Taxon"/> publishes events such as <see cref="Taxon.Events.Created"/>, <see cref="Taxon.Events.Updated"/>, <see cref="Taxon.Events.Deleted"/>, <see cref="Taxon.Events.Moved"/>, and <see cref="Taxon.Events.RegenerateProducts"/> for cross-domain communication and asynchronous processing.
-   **Manage Children**: <see cref="Taxon.AddChild(Taxon?)"/> and <see cref="Taxon.RemoveChild(Taxon?)"/> methods allow for direct manipulation of the Children collection, emitting <see cref="Taxon.Events.Moved"/> events for each affected child.

## 📝 Usage Example

Here are usage examples demonstrating how to create and manage <see cref="Taxon"/>s for both manual and automatic classification.

### Typical Usage - Manual Category:

This example shows how to create a root taxon and a child subcategory, then associate a product with the child category.

```csharp
// In an Application Service or relevant domain logic

// Assume mainCatalog.Id is the Guid of an existing Taxonomy aggregate
Guid mainTaxonomyId = Guid.NewGuid(); // Placeholder for an existing Taxonomy ID

// 1. Create a root taxon for "Apparel"
var apparelResult = Taxon.Create(
    taxonomyId: mainTaxonomyId,
    name: "apparel",
    parentId: null,  // Indicates a root node
    presentation: "Apparel");

if (apparelResult.IsError) { /* Handle error */ return; }
var apparelTaxon = apparelResult.Value;

// 2. Create a child subcategory "Men's Clothing" under "Apparel"
var mensClothingResult = Taxon.Create(
    taxonomyId: mainTaxonomyId, // Must belong to the same taxonomy
    name: "mens-clothing",
    parentId: apparelTaxon.Id, // Parent is the Apparel taxon
    presentation: "Men's Clothing",
    position: 10); // Display order within Apparel children

if (mensClothingResult.IsError) { /* Handle error */ return; }
var mensClothingTaxon = mensClothingResult.Value;

// Assuming 'product' is an existing Product aggregate instance
var product = GetProductInstance(); // Placeholder for fetching a Product

// 3. Assign the product to the "Men's Clothing" taxon
// Note: product.AddClassification expects a Classification object, which references the Taxon
var classificationResult = product.AddClassification(Classification.Create(product.Id, mensClothingTaxon.Id).Value);
if (classificationResult.IsError) { /* Handle error */ return; }

// 4. Save changes to persist new taxons and classifications
// (Assuming a repository and Unit of Work pattern)
// _taxonRepository.Add(apparelTaxon);
// _taxonRepository.Add(mensClothingTaxon);
// _productRepository.Update(product); // Mark product as updated to save classification changes
// await _unitOfWork.SaveChangesAsync(); // Nested set values will be updated by an external service
                                     // listening to Taxon.Events.Moved or upon persistence.

Console.WriteLine($"Taxon hierarchy created: {apparelTaxon.Presentation} -> {mensClothingTaxon.Presentation}");
```

### Typical Usage - Automatic Category:

This example shows how to create an automatic taxon (e.g., "Best Sellers") and add rules to it. Products matching these rules will be automatically assigned to this category.

```csharp
// In an Application Service or relevant domain logic

// Assume mainCatalog.Id is the Guid of an existing Taxonomy aggregate
Guid mainTaxonomyId = Guid.NewGuid(); // Placeholder for an existing Taxonomy ID

// 1. Create an automatic "Best Sellers" taxon
var bestSellersResult = Taxon.Create(
    taxonomyId: mainTaxonomyId,
    name: "best-sellers",
    parentId: null, // Can also be a root
    presentation: "Best Sellers",
    automatic: true, // Mark as automatic
    rulesMatchPolicy: "all", // All rules must pass
    sortOrder: "best-selling"); // Algorithmic sort order

if (bestSellersResult.IsError) { /* Handle error */ return; }
var bestSellersTaxon = bestSellersResult.Value;

// 2. Add rules to define automatic product membership
// Rule 1: Sales count greater than 1000
var salesRuleResult = TaxonRule.Create(
    taxonId: bestSellersTaxon.Id,
    type: "product_sales_count", // Custom rule type (must be supported by rule evaluator)
    value: "1000",
    matchPolicy: "greater_than");

if (salesRuleResult.IsError) { /* Handle error */ return; }
var addSalesRuleResult = bestSellersTaxon.AddTaxonRule(salesRuleResult.Value);
if (addSalesRuleResult.IsError) { /* Handle error */ return; }

// Rule 2: Average rating greater than or equal to 4.5 stars
var ratingRuleResult = TaxonRule.Create(
    taxonId: bestSellersTaxon.Id,
    type: "product_average_rating", // Custom rule type
    value: "4.5",
    matchPolicy: "greater_than_or_equal");

if (ratingRuleResult.IsError) { /* Handle error */ return; }
var addRatingRuleResult = bestSellersTaxon.AddTaxonRule(ratingRuleResult.Value);
if (addRatingRuleResult.IsError) { /* Handle error */ return; }

// 3. Save changes
// (Assuming a repository and Unit of Work pattern)
// _taxonRepository.Add(bestSellersTaxon);
// await _unitOfWork.SaveChangesAsync();
// Products matching BOTH rules (due to "all" policy) will now be automatically
// assigned to the "Best Sellers" category by an external rule processing service
// listening to Taxon.Events.RegenerateProducts events.
Console.WriteLine($"Automatic Taxon '{bestSellersTaxon.Presentation}' created with rules.");
```

---

## 📝 Considerations / Notes

-   The <see cref="Taxon"/> aggregate is central to building and maintaining a product hierarchy, enabling rich categorization and navigation.
-   The Nested Set model is critical for performance when querying hierarchical data.
-   Automatic taxons and taxon rules provide powerful dynamic classification capabilities.
-   Domain Events are crucial for coordinating changes across related aggregates and bounded contexts, especially for re-indexing products or updating caches.
-   The use of <see cref="ErrorOr"/> for return types promotes a functional approach to error handling.