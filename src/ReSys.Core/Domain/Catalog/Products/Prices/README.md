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
-   **Amount**: The current selling price of the product variant. This is a `decimal` value.
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

-   A <see cref="Price"/> must always be associated with a valid <c>VariantId</c>. This is enforced during creation via <see cref="Price.Errors.VariantRequired"/>.
-   <c>Currency</c> is required, must adhere to <see cref="Price.Constraints.CurrencyCodeMaxLength"/> (3 characters), and must be one of the <see cref="Price.Constraints.ValidCurrencies"/>. Invalid currencies will result in <see cref="Price.Errors.InvalidCurrency"/>.
-   <c>Amount</c> and <c>CompareAtAmount</c> (if present) must be non-negative. Attempts to set negative values will result in <see cref="Price.Errors.InvalidAmount"/>.
-   The <c>OnSale</c> status is dynamically derived from comparing <c>Amount</c> and <c>CompareAtAmount</c>. If <c>CompareAtAmount</c> is not greater than <c>Amount</c> when a sale is intended, this will result in <see cref="Price.Errors.InvalidSalePrice"/>.
-   <see cref="Price"/> instances track their creation and update timestamps (<c>CreatedAt</c>, <c>UpdatedAt</c>), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`Price` to `Variant`**: Many-to-one relationship. `Price` is often considered to be owned by `Variant` (from `Catalog.Products.Variants`).
-   **Shared Kernel**: `Price` inherits from `Aggregate` (from `SharedKernel.Domain.Entities`), providing common base properties. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s (`Created`, `Updated`, `Deleted`) for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Price**: Instantiate a new <see cref="Price"/> for a specific product <see cref="Variant"/> using <see cref="Price.Create(Guid, decimal?, string, decimal?)"/>. This method validates the currency and initial amount.
-   **Update Price Details**: Modify the <c>Amount</c> or <c>CompareAtAmount</c> of an existing price using <see cref="Price.Update(decimal?, decimal?)"/>. Currency cannot be changed after creation.
-   **Delete Price**: Remove a price entry for a <see cref="Variant"/> using <see cref="Price.Delete()"/>. This signals the removal of pricing for a specific currency from the variant.
-   **Determine Sale Status**: Check if a product variant is currently <c>OnSale</c> (via the computed property <see cref="Price.OnSale"/>) and calculate the <c>DiscountAmount</c> or <c>DiscountPercent</c>.
-   **Publish Domain Events**: <see cref="Price"/> publishes events upon creation (<see cref="Price.Events.Created"/>), update (<see cref="Price.Events.Updated"/>), and deletion (<see cref="Price.Events.Deleted"/>), allowing other parts of the system (e.g., search indexing, promotions) to react.

---

## 📝 Considerations / Notes

-   While <see cref="Price"/> is treated as an Aggregate Root here, in many contexts, it might be considered a Value Object or a child Entity within the <see cref="Variant"/> aggregate. The current design implies it can have its own lifecycle and direct repository access if needed, but in practice, its management is often orchestrated by the <see cref="Variant"/> aggregate.
-   The use of <see cref="ErrorOr"/> for return types promotes a functional approach to error handling, making business rule violations explicit.
-   All monetary values (Amount, CompareAtAmount) are stored as <c>decimal</c> in the model. However, when dealing with financial transactions at a lower level or integrating with certain payment gateways, these values are typically handled as integers (e.g., in cents) to avoid floating-point precision issues, and converted to <c>decimal</c> for display and business logic.
-   The "Price Capture" pattern mentioned in the class XML documentation (<see cref="Price"/>) is critical for ensuring order integrity when variant prices change.

---
