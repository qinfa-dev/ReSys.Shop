# Payments.Providers Bounded Context

This document describes the `Payments.Providers` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors in the **new, refactored architecture**.

---

## 🎯 Purpose

This domain defines a **unified and extensible interface** for integrating with external payment providers (e.g., Stripe, PayPal, VNPay). It establishes a standardized contract for payment processing operations (authorization, capture, void, and refund) and webhook handling. This promotes extensibility, reduces boilerplate, and allows new payment providers to be added without altering core business logic, while also separating provider configuration from concrete implementations.

---

## 🗣️ Ubiquitous Language

-   **Payment Provider**: An external payment processing service (e.g., Stripe, PayPal, VNPay) accessed through the `IPaymentProvider` interface, encapsulating provider-specific logic.
-   **Provider Options**: Strongly-typed configuration settings for a specific payment provider (e.g., `StripeProviderOptions`), passed to the provider's constructor.
-   **Provider Name**: A unique identifier for a payment provider (e.g., "Stripe", "PayPal").
-   **Payment Request**: Request models (`AuthorizationRequest`, `CaptureRequest`, etc.) containing standardized details for payment operations.
-   **Provider Response**: Standardized result object (`ProviderResponse`) summarizing the outcome of payment operations, including a `PaymentStatus` enum.
-   **Payment Status**: An enumeration (`PaymentStatus`) indicating the detailed state of a payment transaction (e.g., `Succeeded`, `Failed`, `Authorized`, `Captured`).
-   **Idempotency Key**: A unique key included in requests to ensure that an operation is performed only once, even if called multiple times.
-   **Authorization**: Process of reserving funds without immediate transfer (authorize before capture).
-   **Capture**: Process of transferring authorized funds from an authorization.
-   **Void**: Process of canceling an authorized payment before capture.
-   **Refund**: Process of returning captured funds to the customer.
-   **Webhook Event**: A notification from a payment provider about an asynchronous event (e.g., payment succeeded, charge refunded).
-   **Webhook Handler**: A component (`IWebhookHandler`) responsible for processing webhook events from a specific provider.
-   **Webhook Processor**: A central service (`WebhookProcessor`) that dispatches incoming webhook events to the correct `IWebhookHandler`.

---

## 🏛️ Core Components (Refactored Architecture)

### Interfaces

#### `IPaymentProvider` - Main Provider Contract
Defines the canonical contract for all payment provider implementations. This interface is decoupled from `PaymentMethod` entity configuration.

**Methods:**
-   `AuthorizeAsync(AuthorizationRequest)` - Authorize payment
-   `CaptureAsync(CaptureRequest)` - Capture authorized funds
-   `VoidAsync(VoidRequest)` - Cancel authorization
-   `RefundAsync(RefundRequest)` - Refund captured payment

#### `IPaymentProviderFactory` - Provider Factory
Factory for creating and retrieving configured `IPaymentProvider` instances.

**Methods:**
-   `GetProviderAsync(PaymentMethod, CancellationToken)` - Get initialized provider. The factory is responsible for extracting configuration from `PaymentMethod` and passing it to the provider's constructor.

#### `IWebhookHandler` - Webhook Event Handler
Defines the contract for handling webhook events from a specific payment provider.

**Properties:**
-   `ProviderName`: Unique name of the provider this handler supports.

**Methods:**
-   `HandleWebhookAsync(WebhookEvent)`: Processes a generic webhook event.

### Base Classes

#### `PaymentProviderBase<TProviderOptions>` - Common Provider Logic
An abstract base class for payment provider implementations.
-   Implements `IPaymentProvider`.
-   Provides a constructor to receive `TProviderOptions` (strongly-typed configuration).
-   Reduces boilerplate and provides a common foundation for concrete provider implementations.

### Request/Response Models (New)

#### `AuthorizationRequest`
Standardized request for payment authorization.
-   `Amount`: Payment amount (`decimal`)
-   `Currency`: ISO 4217 currency code
-   `PaymentToken`: Tokenized payment details
-   `OrderNumber`: Order reference
-   `CustomerEmail`: Customer email
-   `BillingAddress`: Billing address
-   `ShippingAddress`: Shipping address
-   `IdempotencyKey`: Unique key for idempotent requests
-   `Metadata`: Additional data

#### `CaptureRequest`
Standardized request for capturing authorized payment.
-   `TransactionId`: Authorization transaction ID
-   `Amount`: Amount to capture (`decimal`)
-   `Currency`: Currency code
-   `IdempotencyKey`: Unique key for idempotent requests

#### `VoidRequest`
Standardized request for voiding authorized payment.
-   `TransactionId`: Authorization transaction ID
-   `IdempotencyKey`: Unique key for idempotent requests

#### `RefundRequest`
Standardized request for refunding captured payment.
-   `TransactionId`: Capture transaction ID
-   `Amount`: Refund amount (`decimal`)
-   `Currency`: Currency code
-   `Reason`: Refund reason (optional)
-   `IdempotencyKey`: Unique key for idempotent requests

#### `ProviderResponse`
Standardized result of any payment operation.
-   `Status`: `PaymentStatus` enum (e.g., `Succeeded`, `Failed`, `Authorized`)
-   `TransactionId`: Provider's transaction ID
-   `ErrorCode`: Error code (if failed)
-   `ErrorMessage`: Error details (if failed)
-   `RawResponse`: Raw data from the provider (optional)

#### `PaymentStatus` (Enum)
Strongly-typed enumeration for payment transaction states: `Undefined`, `Succeeded`, `Failed`, `Pending`, `RequiresAction`, `Authorized`, `Captured`, `Voided`, `Refunded`, `PartiallyRefunded`.

#### `WebhookEvent`
Generic model for incoming webhook data.
-   `ProviderName`: Name of the provider sending the event.
-   `EventType`: Type of the event (e.g., `charge.succeeded`).
-   `EventId`: Unique ID of the event.
-   `RawPayload`: Raw string payload.
-   `CreatedAt`: Event timestamp.
-   `Headers`: HTTP headers of the webhook request.

### Services

#### `WebhookProcessor` - Centralized Webhook Dispatcher
A service responsible for receiving raw webhook requests, constructing `WebhookEvent` objects, and dispatching them to the correct `IWebhookHandler` based on the `ProviderName`.
-   Automatically resolves `IWebhookHandler` implementations from the DI container.

### Configuration Models

#### `ProviderOptions` (Abstract Base)
Abstract base class for provider-specific configuration.

#### `StripeProviderOptions` (Example)
Concrete implementation of `ProviderOptions` for Stripe, including `PublishableKey` and other Stripe-specific settings.

---

## 🚀 Implementing a New Payment Provider

To add a new payment provider (e.g., "PayPal") to the system:

### Step 1: Define `PayPalProviderOptions`
Create a new class `PayPalProviderOptions` inheriting from `ProviderOptions` to hold PayPal-specific configuration (e.g., client ID, client secret).

### Step 2: Implement `PayPalProvider`
Create `PayPalProvider.cs` inheriting from `PaymentProviderBase<PayPalProviderOptions>` and implement the `IPaymentProvider` methods (`AuthorizeAsync`, `CaptureAsync`, etc.) by calling the PayPal API. The constructor will receive `PayPalProviderOptions`.

### Step 3: Implement `PayPalWebhookHandler` (Optional)
If PayPal provides webhooks, create `PayPalWebhookHandler.cs` implementing `IWebhookHandler` to process PayPal's webhook events.

### Step 4: Register Components
Register `PayPalProviderOptions`, `PayPalProvider`, `PayPalWebhookHandler` (if applicable), and map the provider type/name to its implementation in the Dependency Injection container. The `PaymentProviderFactory` will use this registration to resolve the correct provider.

### Step 5: Update `PaymentMethod` Entity
Ensure the `PaymentMethod` entity can store the necessary configuration (e.g., serialized `PayPalProviderOptions` or a reference to a configuration ID) and provider name ("PayPal").

---

## 📝 Key Design Principles (New Architecture)

1.  **Provider-Agnostic Core**: Application logic remains independent of any specific payment provider.
2.  **Decoupled Configuration**: Provider configurations are injected via constructors, removing the tight coupling with the `PaymentMethod` entity for initialization.
3.  **Strongly-Typed Models**: Uses `decimal` for monetary values and enums for statuses, enhancing clarity and reducing errors.
4.  **Idempotency Support**: Request models include `IdempotencyKey` to prevent duplicate transactions.
5.  **Unified Webhook Processing**: A generic webhook system (`IWebhookHandler`, `WebhookProcessor`) standardizes the handling of asynchronous notifications from providers.
6.  **Extensible**: Easily add new providers by implementing interfaces and inheriting from `PaymentProviderBase`.
7.  **ErrorOr Pattern**: Functional error handling is maintained for all operations.

---

## 🔗 Related Contexts

-   **Payments.Core** - `PaymentMethod` aggregate (stores provider name and encrypted settings)
-   **Orders.Payments** - Payment records and lifecycle
-   **Stores** - Store-specific provider activation
-   **Identity** - User authentication