# Catalog.Products.Images Bounded Context

This document describes the `Catalog.Products.Images` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the digital assets (images) associated with products and their variants. It provides mechanisms for storing image URLs, alternative text, categorization (types), dimensions, and advanced features like vector embeddings for visual similarity searches, ensuring rich and dynamic product presentations.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Images` bounded context.

-   **Product Image**: A digital image associated with a product or a specific product variant. Represented by the `ProductImage` entity.
-   **URL**: The web address where the image file is located.
-   **Alt Text**: Alternative text for the image, used for accessibility and SEO.
-   **Position**: The display order of the image among other images for the same product/variant.
-   **Image Type**: A categorization of the image's purpose (e.g., `Default`, `Square`, `Thumbnail`, `Gallery`).
-   **Content Type**: The MIME type of the image file (e.g., `image/jpeg`, `image/png`).
-   **Dimensions**: The width and height of the image in pixels.
-   **Dimensions Unit**: The unit of measurement for physical dimensions, if applicable.
-   **Embedding**: A multi-dimensional vector representation of the image, used for content-based image retrieval and similarity calculations.
-   **Embedding Model**: The machine learning model used to generate the image embedding.
-   **Embedding Generated At**: The timestamp when the image embedding was created.
-   **Product**: The parent entity with which the image is associated. (Referenced from `Catalog.Products` Bounded Context).
-   **Variant**: A specific version of a product with which the image is associated. (Referenced from `Catalog.Products.Variants` Bounded Context).
-   **Metadata**: Additional, unstructured data associated with the image.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `ProductImage` is an entity that is owned by either the `Product` or `Variant` aggregates.

### Entities (not part of an Aggregate Root, if any)

-   **`ProductImage`**: This is the central entity of this bounded context. It represents a single image asset and its associated data, including visual embeddings. It inherits from `BaseImageAsset`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Url`, `Alt`, `Position`, `Type`, `ContentType`, `Width`, `Height`, `DimensionsUnit`, `Embedding`, `EmbeddingModel`, `EmbeddingGeneratedAt`, `PublicMetadata`, and `PrivateMetadata` are intrinsic attributes of the `ProductImage` entity.
    -   **Enums**: `ProductImageType` defines standard categories for product images.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. Image manipulation logic (like setting embeddings or calculating similarity) is encapsulated within the `ProductImage` entity itself.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products.Images` bounded context.

-   `ProductImage` URLs must adhere to a maximum length constraint (`Constraints.UrlMaxLength`) and cannot be empty.
-   `ProductImage` `ContentType` must be one of the predefined valid types (e.g., `image/jpeg`, `image/png`).
-   `ProductImage` `Type` must be one of the predefined valid types (e.g., `Default`, `Square`, `Thumbnail`, `Gallery`).
-   `ProductImage` `DimensionsUnit` must be one of the predefined valid units if specified.
-   Image embeddings, if generated, must be of the correct dimension (`Constraints.EmbeddingDimension`).
-   An image cannot be added if an image of the same `Type` already exists for the same product/variant.
-   When an image `Url` changes, any existing `Embedding` is cleared as it's no longer valid for the new image.
-   `Position` values are always non-negative, ensuring valid display ordering.

---

## 🤝 Relationships & Dependencies

-   **`ProductImage` to `Product`**: Many-to-one relationship. `ProductImage` is owned by `Product` (from `Catalog.Products`).
-   **`ProductImage` to `Variant`**: Many-to-one relationship. `ProductImage` can also be owned by `Variant` (from `Catalog.Products.Variants`).
-   **Shared Kernel**: `ProductImage` inherits from `BaseImageAsset` and implements `IHasMetadata` (from `SharedKernel.Domain.Concerns`), leveraging common patterns for asset management and metadata. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Product Image**: Instantiate a new `ProductImage` with its URL, type, and optional metadata and dimensions.
-   **Update Product Image Details**: Modify properties like URL, alt text, position, type, content type, dimensions, or metadata of an existing image.
-   **Set Image Embedding**: Generate and assign a vector embedding to the image for visual similarity searches.
-   **Calculate Image Similarity**: Determine the similarity between two images based on their embeddings.
-   **Delete Product Image**: Remove an image from a product or variant.

---

## 📝 Considerations / Notes

-   `ProductImage` acts as a child entity within the `Product` or `Variant` aggregates, and its lifecycle is managed by them.
-   The embedding functionality highlights the integration of machine learning capabilities within the domain model for enhanced product discoverability.
-   The distinction between image `Type`s allows for flexible rendering and display logic in the frontend.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
