# Payments Domain - Quick Reference Guide

**Quick lookup guide for common payment domain operations and error codes.**

---

## ?? Quick Start Examples

### Create a Payment Method
```csharp
var result = PaymentMethod.Create(
    name: "Credit Card",
    presentation: "Pay with Credit Card",
    type: PaymentMethod.PaymentType.CreditCard,
    active: true,
    autoCapture: false // Manual capture
);

if (result.IsSuccess)
{
    var paymentMethod = result.Value;
    await repository.AddAsync(paymentMethod);
    await unitOfWork.SaveChangesAsync();
}
```

### Update a Payment Method
```csharp
var method = await repository.GetAsync(id);
var result = method.Update(
    active: false,      // Disable
    position: 10        // Lower priority
);

if (result.IsSuccess)
{
    await unitOfWork.SaveChangesAsync();
}
```

### Delete a Payment Method
```csharp
var method = await repository.GetAsync(id);
var result = method.Delete(); // Soft delete

if (result.IsSuccess)
{
    await unitOfWork.SaveChangesAsync();
}
else
{
    // Cannot delete if payments exist
    var error = result.FirstError; // Errors.InUse
}
```

### Restore a Deleted Payment Method
```csharp
var method = await repository.GetAsync(id);
var result = method.Restore();

if (result.IsSuccess)
{
    await unitOfWork.SaveChangesAsync();
}
```

### Save a Credit Card
```csharp
var sourceResult = PaymentSource.Create(
    userId: user.Id,
    paymentMethodId: paymentMethodId,
    type: "CreditCard",
    last4: "4242",
    brand: "Visa",
    expirationMonth: 12,
    expirationYear: 2026,
    isDefault: true
);

if (sourceResult.IsSuccess)
{
    var source = sourceResult.Value;
    user.PaymentSources.Add(source);
    await unitOfWork.SaveChangesAsync();
}
```

### Update a Payment Source
```csharp
var source = await repository.GetPaymentSourceAsync(id);
var result = source.Update(
    isDefault: true,           // Mark as default
    expirationMonth: 6,        // Update month
    expirationYear: 2025       // Update year
);

if (result.IsSuccess)
{
    await unitOfWork.SaveChangesAsync();
}
```

---

## ?? Payment Types Quick Reference

| Type | Auto-Capture | Source Required | Save Cards | Best For |
|------|--------------|-----------------|------------|----------|
| **CreditCard** | ? | ? | ? | General payment |
| **DebitCard** | ? | ? | ? | Direct account debit |
| **BankTransfer** | ? | ? | ? | B2B payments |
| **PayPal** | ?? | ? | ? | Third-party wallet |
| **Stripe** | ?? | ? | ? | Payment processor |
| **ApplePay** | ?? | ? | ? | Mobile payment |
| **GooglePay** | ?? | ? | ? | Mobile payment |
| **Wallet** | ? | ? | ? | Stored funds |
| **CashOnDelivery** | ? | ? | ? | Offline payment |
| **StoreCredit** | ? | ? | ? | Internal balance |
| **GiftCard** | ? | ? | ? | Gift card payment |
| **Check** | ? | ? | ? | Mailed check |
| **Crypto** | ?? | ? | ? | Cryptocurrency |

---

## ?? Error Reference

### PaymentMethod Errors

| Error Code | Message | Cause | Resolution |
|------------|---------|-------|-----------|
| `PaymentMethod.NameRequired` | "Name is required" | Name is null/empty | Provide a non-empty name |
| `PaymentMethod.Required` | "Payment method is required" | Missing payment method | Select a payment method |
| `PaymentMethod.NotFound` | "Payment method '{id}' not found" | ID doesn't exist | Verify the payment method ID |
| `PaymentMethod.InUse` | "Cannot delete payment method with payments" | Has associated payments | Archive instead of delete |
| `PaymentMethod.AlreadyActiveForStore` | "Already active for this store" | Duplicate store association | Check existing associations |

### PaymentSource Errors

| Error Code | Message | Cause | Resolution |
|------------|---------|-------|-----------|
| `PaymentSource.NotFound` | "Payment source with ID '{id}' was not found" | ID doesn't exist | Verify the payment source ID |
| `PaymentSource.InvalidExpirationDate` | "Expiration date is invalid" | Date is past or invalid | Use valid future date |

---

## ?? Property Validation Rules

### PaymentMethod
```
Name:
  - Required
  - Max length: 100 characters
  - Must be unique (database constraint)

Presentation:
  - Required
  - Max length: 100 characters
  - Can differ from Name

Type:
  - Required
  - Must be valid PaymentType enum value

Position:
  - Must be >= 0
  - Lower values display first
  - Default: 0

AutoCapture:
  - Boolean flag
  - true = immediate capture
  - false = manual capture required
  - Default: false

Active:
  - Boolean flag
  - true = visible to customers
  - false = hidden from checkout
  - Default: true

DisplayOn:
  - Enum value (Both, Storefront, Admin)
  - Controls visibility location
  - Default: Both
```

### PaymentSource
```
Type:
  - Required
  - Max length: 50 characters
  - Examples: "CreditCard", "PayPal"

Last4:
  - Optional
  - Max length: 4 characters
  - Display only, not for processing
  - Example: "4242"

Brand:
  - Optional
  - Max length: 50 characters
  - Examples: "Visa", "Mastercard"

ExpirationMonth:
  - Optional
  - Valid range: 1-12
  - Required with ExpirationYear

ExpirationYear:
  - Optional
  - 4-digit year
  - Cannot be in the past
  - Required with ExpirationMonth

IsDefault:
  - Boolean flag
  - true = auto-selected in checkout
  - false = requires manual selection
  - Default: false
```

---

## ?? Common Patterns

### Check If Payment Method Is Card
```csharp
if (paymentMethod.IsCardPayment)
{
    // Apply card-specific validation
}
```

### Check If Manual Capture Required
```csharp
if (paymentMethod.RequiresManualCapture)
{
    // Authorize but hold, require confirmation
}
else
{
    // Can immediately capture funds
}
```

### Check If Saved Cards Supported
```csharp
if (paymentMethod.SupportsSavedCards)
{
    // Show "Save for future use" option
}
```

### Check If Payment Source Required
```csharp
if (paymentMethod.SourceRequired)
{
    // Require user to provide/select payment source
}
else
{
    // Works without payment source (StoreCredit, GiftCard)
}
```

### Get Method Code
```csharp
var code = paymentMethod.MethodCode; // Lowercase type
// "creditcard", "paypal", "apppay", etc.
```

---

## ?? Domain Events

### PaymentMethod.Events.Created
**Published:** When PaymentMethod.Create() succeeds
**Properties:** `PaymentMethodId`, `Name`, `Type`
**Use Cases:** Initialize gateway, log creation, notify admins

### PaymentMethod.Events.Updated
**Published:** When PaymentMethod.Update() makes changes
**Properties:** `PaymentMethodId`
**Use Cases:** Sync configuration, refresh cache, audit log

### PaymentMethod.Events.Deleted
**Published:** When PaymentMethod.Delete() succeeds
**Properties:** `PaymentMethodId`
**Use Cases:** Disable gateway, archive config, notify users

### PaymentMethod.Events.Restored
**Published:** When PaymentMethod.Restore() succeeds
**Properties:** `PaymentMethodId`
**Use Cases:** Re-enable gateway, restore access, update cache

---

## ??? Domain Structure

### PaymentMethod Relationships
```
PaymentMethod (Aggregate Root)
??? StorePaymentMethods (1-to-Many)
?   ??? Links to Store for multi-store support
??? Payments (1-to-Many)
?   ??? References from Orders.Payments
??? PaymentSources (1-to-Many)
?   ??? References from saved user cards
??? Domain Events
    ??? Created
    ??? Updated
    ??? Deleted
    ??? Restored
```

### PaymentSource Relationships
```
PaymentSource (Entity, owned by User)
??? ApplicationUser (Many-to-One)
?   ??? User who owns this payment source
??? PaymentMethod (Many-to-One)
?   ??? Reference to payment method definition
??? PublicMetadata
?   ??? User-visible data (nicknames, etc.)
??? PrivateMetadata
    ??? Admin-only data (gateway tokens, etc.)
```

---

## ?? Security Checklist

- [ ] Never store full credit card numbers
- [ ] Only display last 4 digits
- [ ] Use payment gateway tokens for processing
- [ ] Encrypt sensitive metadata at rest
- [ ] Restrict metadata access to authorized users
- [ ] Validate expiration dates before use
- [ ] Implement proper access control
- [ ] Log payment method operations for audit
- [ ] Use HTTPS for all payment communication
- [ ] Validate user ownership before allowing modifications

---

## ?? Metadata Usage Examples

### Public Metadata (Visible to User)
```csharp
var publicMetadata = new Dictionary<string, object?>
{
    ["nickname"] = "My Visa",
    ["cardType"] = "Credit Card",
    ["lastUsed"] = "2024-01-15",
    ["processingTime"] = "2-3 business days",
    ["fee"] = "2.9%"
};
```

### Private Metadata (Admin Only)
```csharp
var privateMetadata = new Dictionary<string, object?>
{
    ["stripeAccountId"] = "acct_xxxxx",
    ["apiKey"] = "sk_live_xxxxx",
    ["webhookSecret"] = "whsec_xxxxx",
    ["routingNumber"] = "021000021",
    ["riskScore"] = 45,
    ["enrolledIn3DS"] = true
};
```

---

## ?? Navigation

**For detailed information, see:**
- `PaymentMethod.cs` - Full implementation and XML docs
- `PaymentSource.cs` - Full implementation and XML docs  
- `PAYMENTS_REFINEMENT_SUMMARY.md` - Comprehensive overview
- `README.md` - Domain context and ubiquitous language

---

## ?? Common Tasks

| Task | Location | Method |
|------|----------|--------|
| Create payment method | PaymentMethod | `Create()` |
| Update payment method | PaymentMethod | `Update()` |
| Delete payment method | PaymentMethod | `Delete()` |
| Restore payment method | PaymentMethod | `Restore()` |
| Create saved card | PaymentSource | `Create()` |
| Update saved card | PaymentSource | `Update()` |
| Delete saved card | PaymentSource | `Delete()` |

---

## ?? Important Notes

1. **Soft Deletion**: Payment methods use soft deletion (DeletedAt field) rather than hard deletion. Use `Delete()` for archiving.

2. **Unique Names**: Payment method names must be unique in the database. This is enforced at the database constraint level.

3. **Store Association**: In multi-store deployments, use `StorePaymentMethod` to control which stores have access to each payment method.

4. **Payment Integrity**: Payments cannot be orphaned. You cannot delete a payment method if payments exist (check for `Errors.InUse`).

5. **Expiration Validation**: PaymentSource expiration dates are validated at creation time. Invalid dates return `Errors.InvalidExpirationDate`.

---

**Version:** 1.0  
**Last Updated:** [Current Date]  
**Status:** Production Ready
