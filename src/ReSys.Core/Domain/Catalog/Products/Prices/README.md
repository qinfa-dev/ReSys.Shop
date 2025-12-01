# Catalog.Products.Prices Bounded Context

This document describes the `Catalog.Products.Prices` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the pricing information for individual product variants. It allows for defining current prices, comparison prices (for sales/discounts), and supports multi-currency configurations, ensuring flexible and accurate pricing displays.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Catalog.Products.Prices` bounded context.

-   **Price**: The financial value associated with a specific product variant, including its amount, currency, and comparison price. Represented by the `Price` aggregate.
-   **Variant**: The specific version of a product to which this price applies. (Referenced from `Catalog.Products.Variants` Bounded Context).
-   **Amount**: The current selling price of the product variant.
-   **Currency**: The ISO 4217 currency code (e.g., "USD", "EUR") in which the price is denominated.
-   **Compare At Amount**: An optional higher price used to indicate a sale or discount, typically displayed alongside the `Amount`.
-   **On Sale**: A computed property indicating if the `Amount` is lower than the `Compare At Amount`.
-   **Discounted**: Synonym for `On Sale`.
-   **Discount Amount**: The calculated difference between `Compare At Amount` and `Amount`.
-   **Discount Percent**: The calculated percentage discount.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Price`**: This is an Aggregate Root. It encapsulates the pricing details for a `Variant` and is responsible for managing its own state, including creation, updates, and deletion.
    -   **Entities**: None directly owned by `Price`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `VariantId`, `Amount`, `Currency`, and `CompareAtAmount` are intrinsic attributes of the `Price` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Variant` (from `Core.Domain.Catalog.Products.Variants`): Referenced by `Price` (via `VariantId`), but managed by its own aggregate.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Catalog.Products.Prices` bounded context.

-   A `Price` must always be associated with a valid `VariantId`.
-   `Currency` is required, must adhere to `Price.Constraints.CurrencyCodeLength`, and must be one of the `Price.Constraints.ValidCurrencies`.
-   `Amount` and `CompareAtAmount` (if present) must be non-negative.
-   The `OnSale` status is derived from comparing `Amount` and `CompareAtAmount`.
-   `Price` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`Price` to `Variant`**: Many-to-one relationship. `Price` is often considered to be owned by `Variant` (from `Catalog.Products.Variants`).
-   **Shared Kernel**: `Price` inherits from `Aggregate` (from `SharedKernel.Domain.Entities`), providing common base properties. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s (`Created`, `Updated`, `Deleted`) for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Price**: Instantiate a new `Price` for a specific product variant, setting its amount, currency, and optional comparison amount.
-   **Update Price Details**: Modify the `Amount` or `CompareAtAmount` of an existing price.
-   **Delete Price**: Remove a price entry for a variant.
-   **Determine Sale Status**: Check if a product variant is currently `OnSale` and calculate the `DiscountAmount` or `DiscountPercent`.
-   **Publish Domain Events**: Emit events for price creation, update, and deletion, allowing other parts of the system (e.g., search indexing, promotions) to react.

---

## 📝 Considerations / Notes

-   While `Price` is treated as an Aggregate Root here, in many contexts, it might be considered a Value Object or a child Entity within the `Variant` aggregate. The current design implies it can have its own lifecycle and direct repository access if needed, but in practice, its management is often orchestrated by the `Variant` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   All monetary values are likely stored as `long` (e.g., `AmountCents`) in cents to avoid floating-point precision issues, and converted to `decimal` for display.
