# Catalog.Taxonomies.Images Bounded Context

This document describes the `Catalog.Taxonomies.Images` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the visual assets (images) associated with taxons (categories or nodes in a taxonomy). It provides mechanisms for storing image URLs, alternative text, categorization (types), and basic dimensions for display purposes, thereby enriching the visual presentation of the product catalog's hierarchical structure.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Taxonomies.Images` bounded context.

-   **Taxon Image**: A digital image associated with a specific `Taxon`. Represented by the `TaxonImage` entity.
-   **Taxon**: The category or node in a taxonomy with which the image is associated. (Referenced from `Catalog.Taxonomies.Taxa` Bounded Context).
-   **URL**: The web address where the image file is located.
-   **Alt Text**: Alternative text for the image, used for accessibility and SEO.
-   **Position**: The display order of the image among other images for the same taxon.
-   **Image Type**: A categorization of the image's purpose (e.g., `default`, `square`).
-   **Dimensions**: The width and height of the image in pixels, often stored as metadata.
-   **Size**: The file size of the image, often stored as metadata.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `TaxonImage` is an entity that is owned by the `Taxon` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`TaxonImage`**: This is the central entity of this bounded context. It represents a single image asset and its associated data for a taxon. It inherits from `BaseImageAsset`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `TaxonId`, `Type`, `Url`, `Alt`, `Position`, and metadata for `size`, `width`, `height` are intrinsic attributes of the `TaxonImage` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Taxonomies.Images` bounded context.

-   A `TaxonImage` must always be associated with a valid `TaxonId`.
-   A `Taxon` can typically have only one image of a given `Type`. If a new image with an existing `Type` is added, it replaces the old one.
-   `TaxonImage`s can be categorized by `Type` (e.g., "default", "square") to serve different display contexts.
-   `Url` and `Alt` text have maximum length constraints.
-   `Position` values are always non-negative, ensuring valid display ordering.

---

## 🤝 Relationships & Dependencies

-   **`TaxonImage` to `Taxon`**: Many-to-one relationship. `TaxonImage` is owned by `Taxon` (from `Catalog.Taxonomies.Taxa`).
-   **Shared Kernel**: `TaxonImage` inherits from `BaseImageAsset` and implements `IHasPosition` (from `SharedKernel.Domain`), leveraging common patterns for asset management and positioning. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Taxon Image**: Instantiate a new `TaxonImage` for a specific taxon, defining its URL, type, alt text, and position.
-   **Update Taxon Image Details**: Modify the URL, alt text, type, or metadata of an existing taxon image.
-   **Remove Taxon Image**: Delete an image associated with a taxon.

---

## 📝 Considerations / Notes

-   `TaxonImage` acts as a child entity within the `Taxon` aggregate, and its lifecycle is managed by the `Taxon` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   Images are linked to `Taxon`s, allowing categories to have their own visual branding or representative imagery.
