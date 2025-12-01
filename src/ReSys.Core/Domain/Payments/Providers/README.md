# Payments.Providers Bounded Context

This document describes the `Payments.Providers` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines a **unified interface** for integrating with external payment gateways (e.g., Stripe, PayPal, VNPay). It establishes a standardized contract for payment processing operations: initialization, authorization, capture, void, and refund. This promotes extensibility and allows new payment gateways to be added without altering core business logic.

---

## 🗣️ Ubiquitous Language

- **Payment Gateway**: An external payment processing service (e.g., Stripe, PayPal, VNPay) accessed through the `IPaymentGateway` interface.
- **Gateway ID**: A unique identifier for a payment gateway (e.g., "stripe", "paypal", "vnpay").
- **Supported Types**: The list of `PaymentMethod.PaymentType` values that a gateway supports.
- **Payment Request**: Request models (`GatewayAuthorizationRequest`, `GatewayCaptureRequest`, etc.) containing details for payment operations.
- **Payment Result**: Result object (`PaymentResult`) summarizing the outcome of payment operations.
- **Authorization**: Process of reserving funds without immediate transfer (authorize before capture).
- **Capture**: Process of transferring authorized funds.
- **Void**: Process of canceling an authorized payment before capture.
- **Refund**: Process of returning captured funds to the customer.
- **Initialization**: Configuring a gateway instance with `PaymentMethod`'s credentials and settings.

---

## 🏛️ Core Components

### Interfaces

#### `IPaymentGateway` - Main Gateway Contract
Defines the canonical contract for all payment gateway implementations.

**Methods:**
- `Initialize(PaymentMethod, IPaymentCredentialEncryptor)` - Setup with credentials
- `AuthorizeAsync(GatewayAuthorizationRequest)` - Authorize payment
- `CaptureAsync(GatewayCaptureRequest)` - Capture authorized funds
- `VoidAsync(GatewayVoidRequest)` - Cancel authorization
- `RefundAsync(GatewayRefundRequest)` - Refund captured payment

#### `IPaymentGatewayFactory` - Gateway Factory
Factory for creating and retrieving gateway instances.

**Methods:**
- `GetGatewayAsync(PaymentMethod, CancellationToken)` - Get initialized gateway

#### `IPaymentCredentialEncryptor` - Credential Management
Service for encrypting and decrypting payment credentials.

**Methods:**
- `Encrypt(string)` - Encrypt plaintext credential
- `Decrypt(string)` - Decrypt encrypted credential

### Request/Response Models

#### `GatewayAuthorizationRequest`
Request for payment authorization.
- `Amount`: Payment amount
- `Currency`: ISO 4217 currency code
- `PaymentToken`: Tokenized payment details
- `OrderNumber`: Order reference
- `CustomerEmail`: Customer email
- `BillingAddress`: Billing address
- `ShippingAddress`: Shipping address
- `Metadata`: Additional data

#### `GatewayCaptureRequest`
Request for capturing authorized payment.
- `TransactionId`: Authorization transaction ID
- `Amount`: Amount to capture
- `Currency`: Currency code

#### `GatewayVoidRequest`
Request for voiding authorized payment.
- `TransactionId`: Authorization transaction ID

#### `GatewayRefundRequest`
Request for refunding captured payment.
- `TransactionId`: Capture transaction ID
- `Amount`: Refund amount
- `Currency`: Currency code
- `Reason`: Refund reason (optional)

#### `GatewayAddress`
Address for billing or shipping.
- Standard address fields (FirstName, LastName, Company, AddressLine1, AddressLine2, City, State, PostalCode, Country, Phone)

#### `PaymentResult`
Result of payment operation (standardized response from all gateways).
- `TransactionId`: Gateway transaction ID
- `Status`: Operation status
- `Amount`: Processed amount
- `Currency`: Currency code
- `ProcessedAt`: Timestamp
- `ErrorMessage`: Error details (if failed)
- `ProviderData`: Gateway-specific data

---

## 🔄 Payment Flows

### Two-Step Payment (Authorize → Capture)

```
1. Authorize: Reserve funds (holds payment)
2. Review/Fulfill order
3. Capture: Transfer funds
```

### One-Step Payment (Direct Capture)

Some payment types support direct capture without authorization.

### Refund

Return funds from captured payment to customer.

### Void

Cancel authorized payment before capture.

---

## 🔐 Security Pattern

**Credential Storage:**
- Credentials stored encrypted in `PaymentMethod.PrivateMetadata`
- Decrypted only during `Initialize()` call
- Never logged or exposed

**Encryption:**
- Use `IPaymentCredentialEncryptor` for encrypt/decrypt
- Implement with strong encryption (AES-256+)
- Store keys separately from encrypted data

---

## 🚀 Implementing a Gateway

### Step 1: Implement `IPaymentGateway`

```csharp
public sealed class StripeGateway : IPaymentGateway
{
    private string? _apiKey;

    public void Initialize(PaymentMethod paymentMethod, IPaymentCredentialEncryptor encryptor)
    {
        _apiKey = encryptor.Decrypt((string)paymentMethod.PrivateMetadata["api_key"]);
    }

    public async Task<ErrorOr<PaymentResult>> AuthorizeAsync(
        GatewayAuthorizationRequest request, CancellationToken ct)
    {
        // Stripe-specific authorization
    }

    // Implement remaining methods...
}
```

### Step 2: Register in Factory

Implement `IPaymentGatewayFactory` with gateway registry.

### Step 3: Configure PaymentMethod

Store encrypted credentials in `PaymentMethod.PrivateMetadata`.

---

## 📝 Key Design Principles

1. **Gateway-Agnostic**: Application code independent of gateway implementation
2. **Initialization-Based**: Explicit initialize call before operations
3. **ErrorOr Pattern**: Functional error handling
4. **Secure by Default**: Credentials encrypted at rest
5. **Extensible**: Add gateways without modifying core

---

## 🔗 Related Contexts

- **Payments.Core** - `PaymentMethod` aggregate
- **Orders.Payments** - Payment records and lifecycle
- **Stores** - Store-specific gateway activation
- **Identity** - User authentication
