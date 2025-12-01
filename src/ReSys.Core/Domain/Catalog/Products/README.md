# Catalog.Products Bounded Context

This document describes the `Catalog.Products` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the core product entity within the catalog, serving as the central aggregate for all product-related information and operations. It encompasses the definition of products, their variants, associated images, properties, categorization, and availability status, ensuring a comprehensive and consistent representation of items offered for sale.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products` bounded context.

-   **Product**: A distinct item offered for sale, which can have multiple variations (variants). Represented by the `Product` aggregate.
-   **Variant**: A specific version of a product, differing by options (e.g., size, color). Each product has a master variant.
-   **Master Variant**: The default or primary variant of a product, often used for base pricing or when no options are selected.
-   **Option Type**: A characteristic that differentiates product variants (e.g., Color, Size). (Referenced from `Catalog.OptionTypes` Bounded Context).
-   **Option Value**: A specific choice for an Option Type (e.g., Red, Small). (Referenced from `Catalog.OptionTypes` Bounded Context).
-   **Product Image**: An image associated with a product.
-   **Product Property**: An additional attribute or specification for a product (e.g., Material, Weight).
-   **Classification**: The categorization of a product within a taxonomy (e.g., "T-Shirt" in "Apparel").
-   **Taxon**: A node in a hierarchical taxonomy. (Referenced from `Catalog.Taxonomies` Bounded Context).
-   **Taxonomy**: A hierarchical classification system for products. (Referenced from `Catalog.Taxonomies` Bounded Context).
-   **Store Product**: The association of a product with a specific store, potentially with store-specific settings. (Referenced from `Stores` Bounded Context).
-   **Review**: User-generated feedback or rating for a product. (Referenced from `Catalog.Products.Reviews` Bounded Context).
-   **Slug**: A URL-friendly identifier for the product.
-   **SEO Metadata**: Information for search engine optimization (Meta Title, Meta Description, Meta Keywords).
-   **Status**: The current lifecycle stage of a product (Draft, Active, Archived).
-   **Available On/Discontinue On/Make Active At**: Dates controlling product visibility and activation.
-   **Digital Product**: A product that is digital (e.g., downloadable).
-   **Metadata**: Additional, unstructured data (Public/Private).
-   **Purchasable**: Indicates if a product/variant can be bought.
-   **In Stock**: Indicates if a product/variant has available inventory.
-   **Backorderable**: Indicates if a product/variant can be ordered even if out of stock.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Product`**: This is the Aggregate Root. It encapsulates and controls access to its variants, images, product option types, product properties, and classifications. It ensures the consistency and integrity of all product-related data.
    -   **Entities**:
        -   `Variant` (owned by `Product`): Represents a specific variation of the product. Each product must have at least one master variant.
        -   `ProductImage` (owned by `Product`): Represents an image associated with the product, including its URL, alt text, and position.
        -   `ProductOptionType` (owned by `Product`): A junction entity linking a `Product` to an `OptionType`, defining which option types are applicable to the product.
        -   `ProductProperty` (owned by `Product`): A junction entity linking a `Product` to a `Property`, allowing for product-specific attribute values.
        -   `Classification` (owned by `Product`): A junction entity linking a `Product` to a `Taxon`, categorizing the product within a taxonomy.
        -   `StoreProduct` (owned by `Product`): A junction entity linking a `Product` to a `Store`, managing store-specific product settings.
    -   **Value Objects**: None explicitly defined as separate classes within this aggregate. Properties like `Name`, `Presentation`, `Description`, `Slug`, `MetaTitle`, `MetaDescription`, `MetaKeywords`, `AvailableOn`, `DiscontinueOn`, `MakeActiveAt`, `Status`, `IsDigital`, `PublicMetadata`, `PrivateMetadata` act as attributes.

### Entities (not part of an Aggregate Root, if any)

-   `OptionType` (from `Core.Domain.Catalog.OptionTypes`)
-   `Property` (from `Core.Domain.Catalog.Properties`)
-   `Taxon` (from `Core.Domain.Catalog.Taxonomies`)
-   `Taxonomy` (from `Core.Domain.Catalog.Taxonomies`)
-   `Store` (from `Core.Domain.Stores`)
-   `Review` (from `Core.Domain.Catalog.Products.Reviews`)
-   `Order` (from `Core.Domain.Orders`)

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes within `Product.cs`. Complex logic for managing variants, stock, and prices is handled within the `Product` aggregate and its owned entities (like `Variant` and `StockItem`).

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products` bounded context.

-   A `Product` must always have a `Master Variant`.
-   A `Product` cannot be deleted if it has completed orders. (Enforced by `Product.Delete()`)
-   A `Product` cannot be deleted if it has existing variants (other than the master). (Implicitly handled by `Product.Delete()` and `Variant.Delete()`)
-   An `OptionType` or `Property` cannot be added to a `Product` if it's already linked. (Enforced by `AddOptionType()` and `AddProductProperty()`)
-   `Name` and `Slug` are required and adhere to defined length constraints.
-   `DiscontinueOn` date must not be before `MakeActiveAt` date.
-   Product status transitions are managed (Draft -> Active -> Archived).
-   Inventory tracking logic (e.g., `TotalOnHand`, `Purchasable`, `InStock`, `Backorderable`) is consistently applied across variants.
-   Product images have positions and types, and duplicate asset types are prevented.
-   Product properties can be set or updated, and new ones created if they don't exist.

---

## 🤝 Relationships & Dependencies

-   **`Product` to `Variant`**: One-to-many composition. `Product` is the aggregate root for `Variant`s.
-   **`Product` to `ProductImage`**: One-to-many composition. `Product` is the aggregate root for `ProductImage`s.
-   **`Product` to `ProductOptionType`**: One-to-many composition. `Product` is the aggregate root for `ProductOptionType`s, which link to `OptionType` (from `Catalog.OptionTypes`).
-   **`Product` to `ProductProperty`**: One-to-many composition. `Product` is the aggregate root for `ProductProperty`s, which link to `Property` (from `Catalog.Properties`).
-   **`Product` to `Classification`**: One-to-many composition. `Product` is the aggregate root for `Classification`s, which link to `Taxon` (from `Catalog.Taxonomies`).
-   **`Product` to `StoreProduct`**: One-to-many composition. `Product` is the aggregate root for `StoreProduct`s, which link to `Store` (from `Stores`).
-   **External Aggregates**: References `OptionType`, `Property`, `Taxon`, `Taxonomy`, `Store`, `Review`, `Order` from other domains, indicating integration points.
-   **Shared Kernel**: Implements interfaces like `IHasParameterizableName`, `IHasUniqueName`, `IHasMetadata`, `IHasSlug`, `IHasSeoMetadata`, `ISoftDeletable` from `SharedKernel.Domain`, providing common behaviors and attributes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Product**: Instantiate a new product, including its initial master variant.
-   **Update Product Details**: Modify core product information such as name, description, SEO metadata, availability dates, and general metadata.
-   **Manage Product Lifecycle**: Change the product's `Status` (Activate, Archive, Draft, Discontinue).
-   **Manage Product Assets**: Add, remove, or update product images.
-   **Manage Product Options**: Associate `OptionType`s with the product and manage their relationships.
-   **Manage Product Categories**: Link the product to `Taxon`s within various `Taxonomy`s.
-   **Manage Product Variants**: Add or remove specific `Variant`s for the product.
-   **Manage Product Properties**: Add, remove, or set values for product-specific attributes.
-   **Track Product Engagement**: Increment view and add-to-cart counts.
-   **Soft Delete Product**: Mark a product as deleted, preventing its further use while retaining historical data.
-   **Check Product Availability**: Determine if a product is `Available`, `Purchasable`, `InStock`, or `Backorderable`.
-   **Manage Inventory**: Clear master variant stock and prices.

---

## 📝 Considerations / Notes

-   The `Product` aggregate is a complex aggregate root, responsible for maintaining the consistency of a large graph of related entities.
-   The use of `ErrorOr` for return types indicates a functional approach to error handling within the domain, promoting explicit error handling.
-   Domain Events (`Created`, `ProductUpdated`, `VariantAdded`, etc.) are extensively used to signal significant state changes, enabling a highly decoupled architecture and facilitating integration with other bounded contexts or external systems.
-   The `Product` aggregate relies on other domain aggregates (e.g., `OptionType`, `Property`, `Taxon`) for their definitions, but manages its own relationships to instances of these external aggregates.
