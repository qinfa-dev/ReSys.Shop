# Orders.Payments Bounded Context

This document describes the `Orders.Payments` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages financial transactions made against customer orders. It provides a comprehensive system for recording payment attempts, tracking their state (pending, completed, refunded, voided, failed), and associating them with specific payment methods. It ensures the integrity of payment records and facilitates the reconciliation of financial flows within the order lifecycle.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Orders.Payments` bounded context.

-   **Payment**: A record of a financial transaction made towards an `Order`. Represented by the `Payment` aggregate.
-   **Order**: The parent order to which this payment applies. (Referenced from `Orders` Bounded Context).
-   **Payment Method**: The method used for the payment (e.g., Credit Card, PayPal). (Referenced from `Payments` Bounded Context).
-   **Amount Cents**: The total amount of the payment in cents.
-   **Currency**: The ISO 4217 currency code (e.g., "USD") in which the payment is denominated.
-   **Payment State**: The current status of the payment, defined by the `PaymentState` enumeration (e.g., `Pending`, `Processing`, `Completed`, `Failed`, `Void`, `Refunded`).
-   **Payment Method Type**: A string describing the type of payment method used (e.g., "CreditCard", "PayPal"), captured at the time of transaction.
-   **Transaction ID**: A unique identifier provided by the payment gateway for the transaction.
-   **Captured At**: The timestamp when the payment funds were successfully captured.
-   **Voided At**: The timestamp when the payment was canceled before capture.
-   **Refunded At**: The timestamp when the payment was refunded.
-   **Failure Reason**: A description of why the payment failed, if applicable.
-   **Is Completed**: A computed property indicating if the payment has reached the `Completed` state.
-   **Is Pending**: A computed property indicating if the payment is in the `Pending` state.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`Payment`**: This is the Aggregate Root. It encapsulates all information related to a single payment transaction and is responsible for managing its state transitions and ensuring financial integrity.
    -   **Entities**: None directly owned by `Payment`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `OrderId`, `PaymentMethodId`, `AmountCents`, `Currency`, `State`, `PaymentMethodType`, `TransactionId`, `CapturedAt`, `VoidedAt`, `RefundedAt`, and `FailureReason` are intrinsic attributes of the `Payment` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `Order` (from `Core.Domain.Orders`): Referenced by `Payment`, but managed by its own aggregate.
-   `PaymentMethod` (from `Core.Domain.Payments`): Referenced by `Payment`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`PaymentState`**: An enumeration defining the various stages of a payment transaction.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All business logic related to payment state transitions and financial record-keeping is encapsulated within the `Payment` aggregate.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Orders.Payments` bounded context.

-   A `Payment` must always be associated with a valid `OrderId` and `PaymentMethodId`.
-   `AmountCents` must be non-negative.
-   `Currency` and `PaymentMethodType` are required and must adhere to length constraints.
-   A `Payment` cannot be captured if it is already `Completed` or `Voided`.
-   A `Payment` in a `Completed` state cannot be voided.
-   Only `Completed` payments can be refunded.
-   `TransactionId` and `FailureReason` must adhere to maximum length constraints.
-   `Payment` instances track their creation and update timestamps (`CreatedAt`, `UpdatedAt`), adhering to auditing requirements.
-   State transitions must follow logical sequences (e.g., `Pending` -> `Processing` -> `Completed`).

---

## 🤝 Relationships & Dependencies

-   **`Payment` to `Order`**: Many-to-one relationship. `Payment` is owned by `Order` (from `Orders`).
-   **`Payment` to `PaymentMethod`**: Many-to-one relationship. `Payment` links to `PaymentMethod` (from `Payments`).
-   **Shared Kernel**: `Payment` inherits from `Aggregate` (from `SharedKernel.Domain.Entities`), providing common base properties. It uses `ErrorOr` for a functional approach to error handling and publishes `DomainEvent`s for state changes.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Payment**: Instantiate a new `Payment` for an order, specifying the amount, currency, payment method type, and initial state (`Pending`).
-   **Start Processing**: Transition a `Pending` payment to `Processing`.
-   **Capture Payment**: Mark a payment as `Completed`, recording the `TransactionId` and `CapturedAt` timestamp.
-   **Void Payment**: Cancel a payment that has not yet been captured.
-   **Refund Payment**: Initiate a refund for a `Completed` payment.
-   **Mark as Failed**: Record that a payment attempt has failed, along with a `FailureReason`.
-   **Publish Domain Events**: Emit events for payment creation, state changes (processing, captured, voided, refunded, failed), enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   `Payment` acts as a child entity within the `Order` aggregate, and its lifecycle is managed by the `Order` aggregate.
-   All monetary values are stored in cents (`AmountCents`) to avoid floating-point precision issues.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   The payment state machine is critical for accurately reflecting the status of funds and coordinating with external payment gateways.
