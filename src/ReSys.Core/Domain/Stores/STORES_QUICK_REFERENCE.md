# Stores Domain - Quick Reference Guide

**Quick lookup guide for common store operations, constraints, error codes, and patterns.**

---

## 🚀 Quick Start Examples

### Create a New Store
```csharp
var storeResult = Store.Create(
    name: "Fashion Outlet",
    code: "FASHION",
    url: "fashion.example.com",
    currency: "USD",
    mailFromAddress: "orders@fashion.example.com",
    customerSupportEmail: "support@fashion.example.com"
);

if (storeResult.IsError)
    return Problem(storeResult.FirstError.Description);

var store = storeResult.Value;
_dbContext.Stores.Add(store);
await _dbContext.SaveChangesAsync();
```

### Update Store Information
```csharp
var result = store.Update(
    available: false,  // Close for maintenance
    metaTitle: "New SEO Title",
    defaultCurrency: "EUR"
);

if (result.IsError)
    return Problem(result.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Set Store Address
```csharp
var result = store.SetAddress(
    address1: "123 Main Street",
    city: "New York",
    zipcode: "10001",
    countryId: usCountryId,
    phone: "+1-800-555-0123",
    company: "Fashion Inc."
);

if (result.IsError)
    return Problem(result.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Manage Social Media Links
```csharp
var result = store.SetSocialLinks(
    facebook: "https://facebook.com/fashionoutlet",
    instagram: "@fashionoutlet",
    twitter: "@fashionoutlet"
);

if (result.IsError)
    return Problem(result.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Add Products to Store
```csharp
var product = await _dbContext.Products.FindAsync(productId);

var result = store.AddProduct(product, visible: true, featured: true);
if (result.IsError)
    return Problem(result.FirstError.Description);

// Update product settings
var updateResult = store.UpdateProductSettings(productId, visible: false);
if (updateResult.IsError)
    return Problem(updateResult.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Configure Fulfillment (Stock Locations)
```csharp
var warehouse = await _dbContext.StockLocations.FindAsync(warehouseId);

// Add with priority (lower = higher priority)
var result = store.AddStockLocation(warehouse, priority: 1);
if (result.IsError)
    return Problem(result.FirstError.Description);

// Update priority
var updateResult = store.UpdateStockLocationPriority(warehouseId, priority: 2);
if (updateResult.IsError)
    return Problem(updateResult.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Configure Shipping Methods
```csharp
var shippingMethod = await _dbContext.Set<ShippingMethod>().FindAsync(methodId);

var result = store.AddShippingMethod(
    shippingMethod,
    available: true,
    storeBaseCost: 15.00m  // Store-specific cost override
);
if (result.IsError)
    return Problem(result.FirstError.Description);

// Update settings
var updateResult = store.UpdateShippingMethodSettings(
    methodId,
    available: false  // Disable this method
);

await _dbContext.SaveChangesAsync();
```

### Configure Payment Methods
```csharp
var paymentMethod = await _dbContext.Set<PaymentMethod>().FindAsync(methodId);

var result = store.AddPaymentMethod(paymentMethod, available: true);
if (result.IsError)
    return Problem(result.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Enable Password Protection
```csharp
// In application layer: hash the password first
var plainPassword = "MyStorePassword123";
var hashedPassword = BCrypt.Net.BCrypt.HashPassword(plainPassword, 10);

// In domain layer: apply hashed password
var result = store.ProtectWithPassword(hashedPassword);
if (result.IsError)
    return Problem(result.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Delete/Restore Store
```csharp
// Soft delete (can be restored later)
var deleteResult = store.Delete();
if (deleteResult.IsError)
    return Problem(deleteResult.FirstError.Description);

// Restore if needed
var restoreResult = store.Restore();
if (restoreResult.IsError)
    return Problem(restoreResult.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

### Make Store Default
```csharp
// First, clear previous default (in application layer)
var previousDefault = await _dbContext.Stores
    .FirstOrDefaultAsync(s => s.Default && s.Id != store.Id);

if (previousDefault != null)
    previousDefault.Default = false;

// Make this store default
var result = store.MakeDefault();
if (result.IsError)
    return Problem(result.FirstError.Description);

await _dbContext.SaveChangesAsync();
```

---

## 📋 Constraints Quick Reference

| Property | Constraint | Notes |
|----------|-----------|-------|
| **Name** | 1-100 chars | Required, must be unique |
| **Presentation** | 1-100 chars | Optional, alternative display name |
| **Code** | 1-50 chars | Required, must be unique, uppercase |
| **Url** | 1-255 chars | Required, must be unique, lowercase |
| **Currency** | USD, EUR, GBP, VND | Defaults to USD |
| **Locale** | Valid locale code | Defaults to 'en' |
| **Timezone** | Valid timezone ID | Defaults to 'UTC', e.g. 'America/New_York' |
| **Email** | Valid email format | Optional for mailFromAddress, customerSupportEmail |
| **Phone** | 1-50 chars | Optional |
| **Address1** | 1-255 chars | Optional |
| **Address2** | 1-255 chars | Optional |
| **City** | 1-100 chars | Optional |
| **Zipcode** | 1-20 chars | Optional |
| **Company** | 1-255 chars | Optional |
| **Social Links** | 1-500 chars each | Optional: Facebook, Instagram, Twitter |

---

## ❌ Error Reference

### Validation Errors

| Error Code | Message | Cause | Resolution |
|-----------|---------|-------|-----------|
| `Store.NameRequired` | "Store name is required" | Name is null/empty | Provide non-empty name (1-100 chars) |
| `Store.NameTooLong` | "Store name must not exceed 100 characters" | Name exceeds limit | Shorten name to ≤100 characters |
| `Store.CodeRequired` | "Store code is required" | Code is null/empty | Provide code or let system generate from name |
| `Store.CodeTooLong` | "Store code must not exceed 50 characters" | Code exceeds limit | Use shorter code (1-50 chars) |
| `Store.UrlRequired` | "Store URL is required" | URL is null/empty | Provide URL or let system generate from name |
| `Store.UrlTooLong` | "Store URL must not exceed 255 characters" | URL exceeds limit | Use shorter URL (1-255 chars) |
| `Store.InvalidCurrency` | "Currency must be one of: USD, EUR, GBP, VND" | Invalid currency code | Use one of: USD, EUR, GBP, VND |
| `Store.InvalidMailFromAddress` | "Mail-from address must be valid email format" | Email format invalid | Provide valid email (user@domain.com) |
| `Store.InvalidCustomerSupportEmail` | "Customer support email must be valid format" | Email format invalid | Provide valid email (user@domain.com) |
| `Store.InvalidTimezone` | "Timezone must be valid timezone identifier" | Timezone not recognized | Use valid timezone (e.g., 'UTC', 'America/New_York') |
| `Store.InvalidPassword` | "Password cannot be null or empty" | Password is empty | Provide non-empty password hash |

### Not Found Errors

| Error Code | Message | Cause | Resolution |
|-----------|---------|-------|-----------|
| `Store.NotFound` | "Store with ID '{id}' was not found" | Store ID doesn't exist | Verify store ID exists |
| `Store.NotFoundByCode` | "Store with code '{code}' was not found" | Store code not found | Check store code spelling |
| `Store.ProductNotFound` | "Product not associated with this store" | Product not linked to store | Add product to store first |
| `Store.StockLocationNotFound` | "Stock location not found in store" | Warehouse not linked to store | Link stock location to store first |
| `Store.ShippingMethodNotFound` | "Shipping method not found in store" | Shipping not linked to store | Add shipping method to store first |
| `Store.PaymentMethodNotFound` | "Payment method not found in store" | Payment method not linked | Add payment method to store first |

### Conflict/State Errors

| Error Code | Message | Cause | Resolution |
|-----------|---------|-------|-----------|
| `Store.ProductAlreadyAdded` | "Product already added to store" | Product already linked | Remove product first, then re-add |
| `Store.StockLocationAlreadyAdded` | "Stock location already added to store" | Warehouse already linked | Remove location first, then re-add |
| `Store.ShippingMethodAlreadyAdded` | "Shipping method already added to store" | Method already linked | Remove method first, then re-add |
| `Store.PaymentMethodAlreadyAdded` | "Payment method already added to store" | Method already linked | Remove method first, then re-add |
| `Store.CannotDeleteDefault` | "Cannot delete default store. Set another default first" | Trying to delete default store | Use `MakeDefault()` on another store first |
| `Store.HasActiveOrders` | "Cannot delete store with active orders" | Store has active orders | Complete/cancel all orders first |

---

## 🎯 Common Patterns

### Check Store Configuration Status
```csharp
if (store.IsConfigured)
{
    // Store has all required setup (available + stock locations + shipping + payment)
}

if (store.Available)
{
    // Store is currently active/visible to customers
}

if (store.GuestCheckoutAllowed)
{
    // Customers can checkout without creating account
}

if (store.PasswordProtected)
{
    // Store requires password to access
}
```

### Get Store Resources
```csharp
// Get all products in store
var products = store.Products;

// Get all warehouses linked to store (ordered by priority)
var locations = store.StockLocations;

// Get primary warehouse (highest priority)
var primaryWarehouse = store.PrimaryStockLocation;

// Get available shipping options
var shippingOptions = store.AvailableShippingMethods;

// Get available payment options
var paymentOptions = store.AvailablePaymentMethods;
```

### Check Order Status
```csharp
var activeOrderCount = store.ActiveOrderCount;
var completedOrderCount = store.CompletedOrderCount;
var visibleProductCount = store.VisibleProductCount;

if (store.ActiveOrderCount > 0)
{
    // Cannot delete store while it has active orders
}
```

### Access Metadata
```csharp
// Public metadata (visible to customers in API responses)
var nickname = store.PublicMetadata?["nickname"] as string;
var theme = store.PublicMetadata?["theme"] as string;

// Private metadata (admin-only, internal use)
var internalId = store.PrivateMetadata?["legacySystemId"] as string;
var gateway = store.PrivateMetadata?["paymentGatewayId"] as string;
```

---

## 🌍 Multi-Store Concepts

### Store Isolation Strategy
- **Products**: Shared globally but linked per-store via StoreProduct (visibility control)
- **Orders**: Completely isolated per-store (each order belongs to single store)
- **Inventory**: Shared globally but linked per-store via StoreStockLocation (fulfillment priority)
- **Configuration**: Completely isolated (each store has own shipping, payment, SEO settings)
- **Customer Data**: System-wide (customers can shop across all stores)

### Multi-Warehouse Fulfillment
```csharp
// Priority 1 = check first, Priority 2 = check second, etc.
var warehouses = store.StockLocations;  // Ordered by priority (lowest first)

// When creating order, fulfillment location is selected by priority
var primaryLocation = store.PrimaryStockLocation;  // Preferred warehouse
```

### Currency & Localization
```csharp
// Each store has its own defaults
var currency = store.DefaultCurrency;      // "USD", "EUR", etc.
var locale = store.DefaultLocale;          // "en", "fr", "de", etc.
var timezone = store.Timezone;             // "UTC", "America/New_York", etc.
```

---

## 📧 Metadata Guidelines

### Public Metadata Examples (Safe for API)
```csharp
store.PublicMetadata = new Dictionary<string, object?>
{
    ["storeLogo"] = "https://cdn.example.com/logo.png",
    ["bannerImage"] = "https://cdn.example.com/banner.jpg",
    ["theme"] = "modern",
    ["tagline"] = "Premium Fashion at Outlet Prices",
    ["establishedYear"] = 2020,
    ["certifications"] = new[] { "ISO9001", "B-Corp" }
};
```

### Private Metadata Examples (Admin-Only, Internal)
```csharp
store.PrivateMetadata = new Dictionary<string, object?>
{
    ["legacySystemId"] = "SHOP_001",
    ["stripeAccountId"] = "acct_xxxxxxxxxxxxx",
    ["warehouseManagerEmail"] = "warehouse@company.com",
    ["vendorPayoutDays"] = 7,
    ["auditLog"] = "last_reviewed_2024-11-15",
    ["internalNotes"] = "VIP store - high volume"
};
```

---

## 🔄 Domain Events

### Event Reference

| Event | Trigger | Use Case | Subscribers |
|-------|---------|----------|-------------|
| **StoreCreated** | `Store.Create()` | Log creation, initialize integrations | Analytics, Notifications |
| **StoreUpdated** | `Store.Update()` | Track changes, sync cache | Cache, Webhooks |
| **StoreMadeDefault** | `Store.MakeDefault()` | Update system defaults | Config, Admin notifications |
| **StoreDeleted** | `Store.Delete()` | Soft delete log, disable integrations | Audit, Cache invalidation |
| **StoreRestored** | `Store.Restore()` | Restore functionality | Cache, Admin notifications |
| **StoreAddressUpdated** | `Store.SetAddress()` | Update location data | Shipping, Tax service |
| **StoreSocialLinksUpdated** | `Store.SetSocialLinks()` | Update social profiles | Marketing, Webhooks |
| **StorePasswordProtectionEnabled** | `Store.ProtectWithPassword()` | Enable access control | Security audit |
| **StorePasswordProtectionRemoved** | `Store.RemovePasswordProtection()` | Disable access control | Security audit |
| **ProductAddedToStore** | `Store.AddProduct()` | Catalog updated | Search index, Cache |
| **ProductRemovedFromStore** | `Store.RemoveProduct()` | Catalog updated | Search index, Cache |
| **ProductSettingsUpdated** | `Store.UpdateProductSettings()` | Visibility changed | Cache, Webhooks |
| **StockLocationAddedToStore** | `Store.AddStockLocation()` | Fulfillment configured | Inventory, Shipping |
| **StockLocationRemovedFromStore** | `Store.RemoveStockLocation()` | Fulfillment changed | Inventory, Shipping |
| **StockLocationPriorityUpdated** | `Store.UpdateStockLocationPriority()` | Priority changed | Fulfillment routing |
| **ShippingMethodAddedToStore** | `Store.AddShippingMethod()` | Checkout updated | Shipping, Checkout |
| **ShippingMethodRemovedFromStore** | `Store.RemoveShippingMethod()` | Checkout updated | Shipping, Checkout |
| **ShippingMethodSettingsUpdated** | `Store.UpdateShippingMethodSettings()` | Settings changed | Cache, Checkout |
| **PaymentMethodAddedToStore** | `Store.AddPaymentMethod()` | Payment options updated | Payment, Checkout |
| **PaymentMethodRemovedFromStore** | `Store.RemovePaymentMethod()` | Payment options updated | Payment, Checkout |

### Event Handler Example
```csharp
public class StoreCreatedEventHandler : INotificationHandler<Store.Events.StoreCreated>
{
    private readonly ILogger<StoreCreatedEventHandler> _logger;
    private readonly IEmailService _emailService;

    public async Task Handle(Store.Events.StoreCreated notification, CancellationToken ct)
    {
        _logger.LogInformation("Store created: {StoreId} - {StoreName}", 
            notification.StoreId, notification.Name);

        // Send welcome email to store admin
        await _emailService.SendStoreCreatedWelcomeAsync(
            notification.StoreId,
            notification.Name,
            ct
        );

        // Initialize external services
        // - Payment processor
        // - Shipping provider
        // - Email service
    }
}
```

---

## 🔐 Security Checklist

- [ ] Never store plain-text passwords (hash using bcrypt, argon2, or scrypt)
- [ ] Validate email addresses are in correct format
- [ ] Restrict metadata size to prevent storage abuse
- [ ] Use HTTPS for all store URLs
- [ ] Implement rate limiting on password-protected stores
- [ ] Log all store configuration changes for audit trail
- [ ] Use soft deletion (not hard delete) for compliance
- [ ] Encrypt sensitive metadata at rest
- [ ] Restrict access to private metadata to authorized users only
- [ ] Validate timezone against whitelist of system timezones
- [ ] Implement access controls for multi-tenant store updates

---

## 📊 Common Queries

### Get All Available Stores
```csharp
var availableStores = await _dbContext.Stores
    .Where(s => s.Available && !s.IsDeleted)
    .ToListAsync();
```

### Get Default Store
```csharp
var defaultStore = await _dbContext.Stores
    .FirstOrDefaultAsync(s => s.Default && !s.IsDeleted);
```

### Get Stores by Currency
```csharp
var usdStores = await _dbContext.Stores
    .Where(s => s.DefaultCurrency == "USD" && !s.IsDeleted)
    .ToListAsync();
```

### Get Fully Configured Stores (Ready for Orders)
```csharp
var readyStores = await _dbContext.Stores
    .Where(s => s.IsConfigured && !s.IsDeleted)
    .ToListAsync();
```

### Get Password Protected Stores
```csharp
var protectedStores = await _dbContext.Stores
    .Where(s => s.PasswordProtected && !s.IsDeleted)
    .ToListAsync();
```

---

## 🎨 Soft Delete Behavior

- **Soft Delete**: Uses `DeletedAt` timestamp, `IsDeleted` flag, `DeletedBy` user
- **Query Filter**: Automatically excludes soft-deleted stores from queries
- **Restoration**: Use `Store.Restore()` to bring deleted store back
- **Hard Delete**: Not recommended (loss of audit trail)
- **Cascade**: Soft-deleted store cascades to StoreProducts, StoreShippingMethods, etc.

```csharp
// Soft deleted stores are automatically excluded
var stores = await _dbContext.Stores.ToListAsync();  // No deleted stores

// To include deleted stores
var allStores = await _dbContext.Stores.IgnoreQueryFilters().ToListAsync();
```

---

## 🚀 Performance Tips

1. **Eager Load Related Data**: Use `.Include()` for products, locations, methods
   ```csharp
   var store = await _dbContext.Stores
       .Include(s => s.StoreProducts).ThenInclude(sp => sp.Product)
       .Include(s => s.StoreStockLocations).ThenInclude(ssl => ssl.StockLocation)
       .FirstOrDefaultAsync(s => s.Id == storeId);
   ```

2. **Use Computed Properties Carefully**: They work on loaded data in memory
   ```csharp
   var configured = store.IsConfigured;  // Evaluates in-memory, not database
   ```

3. **Index Optimization**: Database has indexes on Name, Code, Url, Default, DeletedAt
4. **Pagination**: For large datasets, use `.Skip().Take()` before `.ToListAsync()`

---

## 📝 Type Safety Notes

### Return Type Meanings
- `ErrorOr<Store>`: Operation modifies store state and returns updated instance (chainable)
- `ErrorOr<Unit>`: Operation completes but doesn't return meaningful value
- `ErrorOr<Deleted>`: Operation removes/deletes resource

### Event Publishing
- Events published on all modifications via `AddDomainEvent()`
- Auto-published after `SaveChangesAsync()` (EF Core interceptor)
- Subscribe via MediatR `INotificationHandler<TEvent>`

---

## 🧪 Testing Patterns

```csharp
[TestFixture]
public class StoreTests
{
    [Test]
    public void Create_WithValidInput_SucceedsAndRaisesEvent()
    {
        // Arrange
        var name = "Test Store";
        var code = "TEST";
        var url = "test.example.com";

        // Act
        var result = Store.Create(name, code: code, url: url);

        // Assert
        Assert.That(result.IsError, Is.False);
        var store = result.Value;
        Assert.That(store.Name, Is.EqualTo(name));
        Assert.That(store.Code, Is.EqualTo(code.ToUpperInvariant()));
        Assert.That(store.HasUncommittedEvents(), Is.True);
    }

    [Test]
    public void Update_WithNoChanges_DoesNotPublishEvent()
    {
        // Arrange
        var store = StoreFixture.CreateValidStore();
        var eventCount = store.UncommittedEvents.Count;

        // Act
        var result = store.Update(name: store.Name); // Same name

        // Assert
        Assert.That(store.UncommittedEvents.Count, Is.EqualTo(eventCount)); // No new event
    }
}
```

---

**Version:** 1.0  
**Status:** Production Ready  
**Last Updated:** December 1, 2025
