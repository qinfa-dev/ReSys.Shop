# Catalog.Taxonomies.Images Bounded Context

This document describes the `Catalog.Taxonomies.Images` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the visual assets (images) associated with taxons (categories or nodes in a taxonomy). It provides mechanisms for storing image URLs, alternative text, categorization (types), and basic dimensions for display purposes, thereby enriching the visual presentation of the product catalog's hierarchical structure.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies.Images` bounded context.

-   **Taxon Image**: A digital image associated with a specific <see cref="Taxon"/>. Represented by the <see cref="TaxonImage"/> entity.
-   **Taxon**: The category or node in a taxonomy with which the image is associated. (Referenced from `Catalog.Taxonomies.Taxa` Bounded Context).
-   **URL**: The web address where the image file is located.
-   **Alt Text**: Alternative text for the image, used for accessibility and SEO.
-   **Position**: The display order of the image among other images for the same taxon.
-   **Image Type**: A categorization of the image's purpose (e.g., <c>default</c>, <c>square</c>).
-   **Dimensions**: The width and height of the image in pixels, stored as metadata.
-   **Size**: The file size of the image, stored as metadata.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `TaxonImage` is an entity that is owned by the `Taxon` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`TaxonImage`**: This is the central entity of this bounded context. It represents a single image asset and its associated data for a taxon. It inherits from <see cref="BaseImageAsset"/>.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>TaxonId</c>, <c>Type</c>, <c>Url</c>, <c>Alt</c>, <c>Position</c>, and metadata for <c>size</c>, <c>width</c>, <c>height</c> (stored in <c>PublicMetadata</c>) are intrinsic attributes of the <see cref="TaxonImage"/> entity.

### Value Objects (standalone, if any)

-   None.

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies.Images` bounded context.

-   A <see cref="TaxonImage"/> must always be associated with a valid <c>TaxonId</c>.
-   A <c>Taxon</c> can typically have only one image of a given <c>Type</c>. If a new image with an existing <c>Type</c> is added via <see cref="Taxon.AddImage(TaxonImage)"/>, it may replace the old one or result in an error depending on the parent aggregate's implementation.
-   <see cref="TaxonImage"/>s can be categorized by <c>Type</c> (e.g., "default", "square") to serve different display contexts.
-   <c>Url</c> and <c>Alt</c> text have maximum length constraints (inherited from <see cref="BaseImageAsset"/> and <see cref="ProductImage.Constraints"/> where applicable).
-   <c>Position</c> values are always non-negative, ensuring valid display ordering, enforced during creation via <c>SetPosition(int)</c> and factory method parameters.

---

## 🤝 Relationships & Dependencies

-   **`TaxonImage` to `Taxon`**: Many-to-one relationship. `TaxonImage` is owned by `Taxon` (from `Catalog.Taxonomies.Taxa`).
-   **Shared Kernel**: `TaxonImage` inherits from `BaseImageAsset` and implements `IHasPosition` (from `SharedKernel.Domain`), leveraging common patterns for asset management and positioning. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Taxon Image**: Instantiate a new <see cref="TaxonImage"/> using <see cref="TaxonImage.Create(Guid, string, string?, string?, int, int?, int?, int?)"/> for a specific taxon, defining its URL, type, alt text, and position.
-   **Update Taxon Image Details**: Modify the <c>URL</c>, <c>Alt</c> text, <c>Type</c>, or metadata (including <c>size</c>, <c>width</c>, <c>height</c>) of an existing taxon image using <see cref="TaxonImage.Update(string?, string?, string?, int?, int?, int?)"/>.
-   **Remove Taxon Image**: Delete an image associated with a taxon. This is typically managed by the parent <see cref="Taxon"/> aggregate.

---

## 📝 Considerations / Notes

-   `TaxonImage` acts as a child entity within the `Taxon` aggregate, and its lifecycle is managed by the `Taxon` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   Images are linked to `Taxon`s, allowing categories to have their own visual branding or representative imagery.
