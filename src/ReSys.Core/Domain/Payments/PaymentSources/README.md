# Payments.PaymentSources Bounded Context

This document describes the `Payments.PaymentSources` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the secure storage and retrieval of payment method details associated with individual users. It enables users to save their payment information (e.g., credit card details) for future transactions, thereby streamlining the checkout process and enhancing user convenience while adhering to security best practices.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Payments.PaymentSources` bounded context.

-   **Payment Source**: A record representing a user's stored payment method (e.g., a credit card, PayPal account reference). Represented by the `PaymentSource` entity.
-   **User**: The application user who owns this payment source. (Referenced from `Identity.Users` Bounded Context).
-   **Payment Method**: The generic payment method definition (e.g., Credit Card, PayPal). (Referenced from `Payments` Bounded Context).
-   **Type**: A string describing the type of payment source (e.g., "CreditCard", "PayPal"), often specific to the provider.
-   **Last4**: The last four digits of a credit card number, for display purposes.
-   **Brand**: The brand of the credit card (e.g., "Visa", "MasterCard").
-   **Expiration Month / Year**: The expiration date of the credit card.
-   **Is Default**: A boolean flag indicating if this is the user's preferred payment source.
-   **Metadata**: Additional, unstructured key-value data.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `PaymentSource` is an entity that is owned by the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`PaymentSource`**: This is the central entity of this bounded context. It represents a single stored payment method for a user and is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `UserId`, `PaymentMethodId`, `Type`, `Last4`, `Brand`, `ExpirationMonth`, `ExpirationYear`, `IsDefault`, `PublicMetadata`, and `PrivateMetadata` are intrinsic attributes of the `PaymentSource` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Payments.PaymentSources` bounded context.

-   A `PaymentSource` must always be associated with a valid `UserId` and `PaymentMethodId`.
-   `Type`, `Last4`, and `Brand` must adhere to maximum length constraints.
-   `ExpirationMonth` must be between 1 and 12.
-   `ExpirationYear` must be a valid future or current year, and not in the past relative to the current month if in the current year.
-   A user can typically have only one payment source marked as `IsDefault` for a given payment method type (this might be enforced at the application service layer).
-   `PaymentSource` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`PaymentSource` to `ApplicationUser`**: Many-to-one relationship. `PaymentSource` is owned by `ApplicationUser` (from `Identity.Users`).
-   **`PaymentSource` to `PaymentMethod`**: Many-to-one relationship. `PaymentSource` links to `PaymentMethod` (from `Payments`).
-   **Shared Kernel**: `PaymentSource` inherits from `AuditableEntity<Guid>` and implements `IHasMetadata` (from `SharedKernel.Domain`), leveraging common patterns for auditing and metadata. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Payment Source**: Instantiate a new `PaymentSource` for a user, providing details like type, last four digits, brand, expiration date, and default status.
-   **Update Payment Source Details**: Modify details of an existing payment source, such as expiration date or default status.
-   **Delete Payment Source**: Remove a stored payment method from a user's account.

---

## 📝 Considerations / Notes

-   `PaymentSource` acts as a child entity within the `ApplicationUser` aggregate, and its lifecycle is managed by the `ApplicationUser` aggregate.
-   Sensitive payment information (e.g., full card numbers) should never be stored directly in this entity but should be handled by a secure payment gateway and referenced by a token or identifier stored here.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
-   This domain supports tokenization of payment information, improving security and PCI compliance.
