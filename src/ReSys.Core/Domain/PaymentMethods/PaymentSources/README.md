# Payments.PaymentSources Bounded Context

This document describes the `Payments.PaymentSources` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the secure storage and retrieval of payment method details associated with individual users. It enables users to save their payment information (e.g., credit card details) for future transactions, thereby streamlining the checkout process and enhancing user convenience while adhering to security best practices. By tokenizing or masking sensitive data, it minimizes PCI compliance scope for the application.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Payments.PaymentSources` bounded context.

-   **Payment Source**: A record representing a user's stored payment method (e.g., a tokenized credit card, PayPal account reference). Represented by the `PaymentSource` entity.
-   **User**: The application user who owns this payment source. (Referenced from `Identity.Users` Bounded Context).
-   **Payment Method**: The generic payment method definition (e.g., Credit Card, PayPal). (Referenced from `Payments` Bounded Context).
-   **Type**: A string describing the type of payment source (e.g., "CreditCard", "PayPal"), often specific to the provider or internal system.
-   **Last4**: The last four digits of a credit card number, for display purposes only.
-   **Brand**: The brand of the credit card (e.g., "Visa", "MasterCard").
-   **Expiration Month / Year**: The expiration date of the credit card.
-   **Is Default**: A boolean flag indicating if this is the user's preferred payment source for a given `PaymentMethod` type.
-   **Public Metadata**: A dictionary (`IDictionary<string, object?>`) to store publicly visible, payment source-specific configuration and data.
-   **Private Metadata**: A dictionary (`IDictionary<string, object?>`) to store sensitive, internal-only payment source-specific data (e.g., payment gateway tokens, vault IDs).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `PaymentSource` is an entity that is owned by the `User` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`PaymentSource`**: This is the central entity of this bounded context. It represents a single stored payment method for a user and is an `AuditableEntity` implementing `IHasMetadata`.
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
-   `ExpirationMonth` must be between 1 and 12 (if provided).
-   `ExpirationYear` must be a valid future or current year, and not in the past relative to the current month (if provided).
-   A user can typically have only one payment source marked as `IsDefault` for a given `PaymentMethod` type (this might be enforced at the application service layer).
-   `PaymentSource` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`PaymentSource` to `User`**: Many-to-one relationship. `PaymentSource` is owned by `User` (from `Identity.Users`).
-   **`PaymentSource` to `PaymentMethod`**: Many-to-one relationship. `PaymentSource` links to `PaymentMethod` (from `Payments`).
-   **Shared Kernel**: `PaymentSource` inherits from `AuditableEntity<Guid>` and implements `IHasMetadata`, leveraging common patterns for auditing and metadata. It uses `ErrorOr` for a functional approach to error handling.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Payment Source**: Instantiate a new `PaymentSource` for a user, providing details like type, masked card number (Last4), brand, expiration date, default status, and optional metadata.
-   **Update Payment Source Details**: Modify details of an existing payment source, such as expiration date, default status, or metadata.
-   **Delete Payment Source**: Remove a stored payment method from a user's account. This typically results in physical deletion.
-   **Set Default Payment Source**: Mark a specific `PaymentSource` as the preferred default for a user. (Enforcement of single default is usually application service logic).

---

## 📝 Considerations / Notes

-   `PaymentSource` acts as a child entity within the `User` aggregate, and its lifecycle is tied to the user.
-   Sensitive payment information (e.g., full card numbers, CVV) should **never** be stored directly in this entity. Instead, rely on secure payment gateway tokenization, where a non-sensitive token or identifier is stored here.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling.
-   This domain supports tokenization of payment information, improving security and PCI compliance.

---

## ?? Security Checklist

-   [ ] **Never** store full payment card numbers (PAN) directly.
-   [ ] Only store the **last 4 digits** of card numbers for display and user identification.
-   [ ] Use payment gateway tokens or vault IDs to reference actual payment instruments.
-   [ ] **Encrypt sensitive `PrivateMetadata`** at rest (e.g., gateway tokens) in the database.
-   [ ] Restrict access to `PrivateMetadata` to authorized backend services and administrators only.
-   [ ] Validate expiration dates before use to prevent processing with expired cards.
-   [ ] Implement proper access control to ensure users can only manage their own payment sources.
-   [ ] Log all payment source creation, updates, and deletions for audit trails.
-   [ ] Use **HTTPS** for all communication involving payment source data.
-   [ ] Implement strong user authentication to protect payment source access.

---

## ?? Metadata Usage Examples

### Public Metadata (Visible to User)
```csharp
var publicMetadata = new Dictionary<string, object?>
{
    ["nickname"] = "My Primary Card",
    ["cardType"] = "Credit Card",
    ["lastUsed"] = "2024-03-01"
};
```

### Private Metadata (Admin/System Only)
```csharp
var privateMetadata = new Dictionary<string, object?>
{
    ["stripePaymentMethodId"] = "pm_xxxxx",
    ["paypalBillingAgreementId"] = "BA-XXXXXX",
    ["gatewayToken"] = "tok_xxxxx",
    ["vaultId"] = "vault_abc123"
};
```