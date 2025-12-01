# Stores Bounded Context

This document describes the `Stores` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the definition and configuration of individual stores or sales channels within the application. It provides comprehensive functionalities for setting up store details, managing currency, contact information, SEO metadata, and crucially, associating products and stock locations with each store. This enables the operation of multiple distinct storefronts, each with its own branding, product catalog, and inventory management.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Stores` bounded context.

-   **Store**: A distinct sales channel, storefront, or brand within the application. It represents an independent entity through which products are sold. Represented by the `Store` aggregate.
-   **Name**: The human-readable name of the store (e.g., "Main Store", "Fashion Outlet").
-   **Code**: A unique, internal identifier for the store (e.g., "MAIN", "FASHION"). This is often used for programmatic identification.
-   **URL**: The primary web address or domain associated with the store.
-   **Default Currency**: The default currency used for transactions and pricing within this specific store (e.g., "USD", "EUR").
-   **Mail From Address**: The email address used as the sender for system-generated emails originating from this store (e.g., order confirmations).
-   **Customer Support Email**: The designated email address for customer support inquiries related to this store.
-   **Meta Title / Meta Description / Meta Keywords**: SEO (Search Engine Optimization) metadata specifically for the store's homepage or main landing pages, used to improve search engine visibility.
-   **SEO Title**: A computed or explicitly set SEO-friendly title for the store.
-   **Available**: A boolean flag indicating if the store is currently active, accessible, and open for business.
-   **Address**: Physical address details for the store, including `Address1`, `City`, `Zipcode`, `Phone`, and `Company`.
-   **Product**: An item offered for sale. (Referenced from `Catalog.Products` Bounded Context).
-   **Store Product**: The explicit association of a `Product` with a `Store`, including store-specific attributes like visibility and featured status.
-   **Order**: A customer's purchase placed within this specific store. (Referenced from `Orders` Bounded Context).
-   **Stock Location**: A physical or logical place where inventory is held. (Referenced from `Inventories.Locations` Bounded Context).
-   **Stock Location Store**: The explicit association of a `Stock Location` with a `Store`, indicating which inventory locations serve this store.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Store`**: This is the Aggregate Root. It defines a store and is responsible for managing its configuration, availability, and its associations with products and stock locations. It ensures the integrity and consistent state of the store's definition and its direct relationships.
    -   **Entities**:
        -   `StoreProduct` (owned by `Store`): Represents a product's specific association with this store, including its visibility and whether it's featured.
        -   `StockLocationStore` (owned by `Store`): Represents a `Stock Location`'s association with this store, indicating that the stock from this location is available to the store.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Code`, `Url`, `DefaultCurrency`, `MailFromAddress`, `CustomerSupportEmail`, `MetaTitle`, `MetaDescription`, `MetaKeywords`, `SeoTitle`, `Available`, address components (`Address1`, `City`, `Zipcode`, `Phone`, `Company`), `PublicMetadata`, and `PrivateMetadata` are intrinsic attributes of the `Store` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Country` (from `Core.Domain.Location`): Referenced by `Store` for its physical address, but managed by its own aggregate.
-   `State` (from `Core.Domain.Location`): Referenced by `Store` for its physical address, but managed by its own aggregate.
-   `Product` (from `Core.Domain.Catalog.Products`): Referenced by `StoreProduct`, but managed by its own aggregate.
-   `Order` (from `Core.Domain.Orders`): Referenced by `Store` (via `Order.StoreId`), but managed by its own aggregate.
-   `StockLocation` (from `Core.Domain.Inventories.Locations`): Referenced by `StockLocationStore`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `Validate` helper method is a static utility function within the `Store` aggregate, performing basic input validation.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Stores` bounded context.

-   `Store` name, code, and URL are required and must adhere to defined maximum length constraints (`Constraints.NameMaxLength`, `Constraints.CodeMaxLength`, `Constraints.UrlMaxLength`).
-   `Store` codes are automatically converted to uppercase for standardization.
-   `Store` URLs are automatically converted to lowercase for consistency.
-   The `DefaultCurrency` must be one of the predefined valid currencies (`Constraints.ValidCurrencies`).
-   A `Store` cannot be deleted if it has active orders associated with it. (Enforced by `Errors.HasActiveOrders` - though the current `Delete` method only raises an event, the check would typically be in an application service or a more robust aggregate method).
-   A `Product` cannot be added to a store if it is already associated with that store. (Enforced by `Errors.ProductAlreadyAdded`).
-   A `StockLocation` cannot be added to a store if it is already associated with that store. (Enforced by `Errors.StockLocationAlreadyAdded`).
-   `Store` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.
-   `Store` implements `IAddress`, ensuring a consistent structure for its physical location details.

---

## 🤝 Relationships & Dependencies

-   **`Store` to `StoreProduct`**: One-to-many composition. `Store` is the aggregate root for its `StoreProduct` associations.
-   **`Store` to `StockLocationStore`**: One-to-many composition. `Store` is the aggregate root for its `StockLocationStore` associations.
-   **External Aggregates**: References `Country` and `State` (from `Core.Domain.Location`) for geographical address details, `Product` (from `Core.Domain.Catalog.Products`) for product catalog management, `Order` (from `Core.Domain.Orders`) for sales tracking, and `StockLocation` (from `Core.Domain.Inventories.Locations`) for inventory management.
-   **Shared Kernel**: Inherits from `Aggregate` and implements `IHasMetadata`, `IHasUniqueName`, `IHasSeoMetadata`, and `IAddress` (from `SharedKernel.Domain.Entities`), leveraging common patterns for metadata, unique naming, SEO, and address structure.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Store**: Instantiate a new `Store` with essential details such as its name, unique code, URL, and default currency.
-   **Update Store Details**: Modify the `Name`, `URL`, `MailFromAddress`, `CustomerSupportEmail`, SEO metadata, or `Available` status of an existing `Store`.
-   **Add/Remove Products**: Associate or disassociate `Product`s with the `Store`, controlling their visibility and featured status within that storefront.
-   **Add Stock Locations**: Link `StockLocation`s to the `Store`, indicating which inventory sources serve this sales channel.
-   **Delete Store**: Remove a `Store` from the system, with checks to prevent deletion if it has active dependencies (e.g., active orders).
-   **Validate Store Properties**: Perform validation checks on store attributes during creation and update.
-   **Publish Domain Events**: Emit domain events (`StoreCreated`, `StoreUpdated`, `StoreDeleted`, `ProductAddedToStore`, `StockLocationAddedToStore`) to signal significant state changes, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `Store` aggregate is a central entity for multi-store or multi-brand e-commerce platforms, providing a clear boundary for managing distinct sales channels.
-   The `StoreProduct` and `StockLocationStore` entities are crucial for defining the specific catalog and inventory available to each store, allowing for highly customized storefront experiences.
-   The `IAddress` and `IHasSeoMetadata` interfaces demonstrate the use of shared kernel concepts to ensure consistency and reduce duplication across related domains.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   While the `Delete` method currently only raises an event, the actual deletion logic (including checks for active orders) would typically be handled by an application service or a more comprehensive aggregate method to ensure transactional integrity.
