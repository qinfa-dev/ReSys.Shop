# Stores Bounded Context - Comprehensive Guide

This document describes the `Stores` bounded context, outlining its purpose, ubiquitous language, core components, key behaviors, and practical guidance for working with multi-store systems.

---

## 🎯 Purpose & Vision

The **Stores** domain manages the definition and configuration of individual stores or sales channels within a multi-store e-commerce platform. Each Store represents an independent sales channel with its own:

- **Branding & Identity**: Name, logo, social media, contact information
- **Configuration**: Currency, language, timezone, SEO settings
- **Catalog**: Product visibility and featured settings per store
- **Fulfillment**: Warehouse locations with priority ordering
- **Payments & Shipping**: Independent payment and shipping method availability
- **Orders**: Completely isolated order management
- **Security**: Optional password protection for private/beta stores

This architecture enables businesses to operate multiple storefronts simultaneously—each with distinct brand identities, pricing strategies, and operational configurations—while sharing a common product catalog and inventory backend.

---

## 🏢 Multi-Store Architecture Patterns

### Store Isolation Strategy

```
Shared (Global) Resources:
├── Products (shared catalog, visibility controlled per store)
├── Inventory (shared warehouses)
├── Customers (global accounts can shop all stores)
├── Payment Gateways (shared API credentials)
└── Shipping Providers (shared accounts)

Isolated (Per-Store) Resources:
├── Store Configuration (name, code, url, currency, timezone)
├── Orders (completely separate per store)
├── Product Visibility (Product X visible in Store A, hidden in Store B)
├── Fulfillment Priority (Warehouse priority different per store)
├── Shipping Methods (Store A offers DHL, Store B offers FedEx)
├── Payment Methods (Store A accepts Stripe, Store B accepts PayPal)
├── SEO Metadata (unique per-store homepage meta tags)
└── Contact Information (support email/phone per store)
```

### Real-World Scenarios

**Scenario 1: Multi-Brand Business**
```
Company: Global Fashion Retail Inc.

Store 1: Women's Fashion
  - URL: women.example.com
  - Currency: EUR
  - Timezone: Europe/Paris
  - Language: French, English
  - Warehouse: Paris warehouse (primary)

Store 2: Men's Fashion
  - URL: mens.example.com
  - Currency: USD
  - Timezone: America/New_York
  - Language: English
  - Warehouse: New York warehouse (primary)
```

**Scenario 2: Regional Expansion**
```
Company: TechGadgets Inc.

Store 1: North America
  - URL: us.techgadgets.com
  - Currency: USD
  - Shipping: UPS, FedEx, USPS
  
Store 2: Europe
  - URL: eu.techgadgets.com
  - Currency: EUR
  - Shipping: DPD, GLS, Deutsche Post
  
Store 3: Asia-Pacific
  - URL: apac.techgadgets.com
  - Currency: SGD
  - Shipping: Singpost, DHL, FedEx
```

**Scenario 3: Beta/Private Launch**
```
Company: Startup Fashion Co.

Store 1: Public Production
  - Available: true
  - PasswordProtected: false
  - Status: Open to all customers

Store 2: Beta Program
  - Available: true
  - PasswordProtected: true
  - Status: Limited to invited testers
```

---

## 🗣️ Ubiquitous Language

### Core Concepts

- **Store**: A distinct sales channel/storefront representing an independent business entity
- **Store Code**: Unique system identifier (uppercase, 1-50 chars) for subdomain routing and API slugs
- **Store Name**: Human-readable unique display name (1-100 chars)
- **Store Presentation**: Alternative branding name for UI (differs from Name if needed)
- **Default Store**: The fallback store used when no specific store is requested
- **Available**: Boolean flag indicating if store is active and open for customer orders

### Configuration Terms

- **Default Currency**: Transaction currency (USD, EUR, GBP, VND)
- **Default Locale**: Language/region code (en, fr, de, es, etc.)
- **Timezone**: Store's local timezone for scheduling and reporting
- **Mail From Address**: Sender email for order confirmations and notifications
- **Customer Support Email**: Contact email for customer inquiries

### Relationship Terms

- **Store Product**: Product linked to store with visibility/featured status
- **Store Stock Location**: Warehouse linked to store with fulfillment priority
- **Store Shipping Method**: Shipping option available in this store
- **Store Payment Method**: Payment option available in this store

---

## 🏛️ Domain Components & Architecture

### Aggregate Root: Store

The **Store** aggregate manages complete configuration for a single sales channel.

**Key Responsibilities:**
- Store identity management (name, code, URL)
- Configuration (currency, locale, timezone)
- Contact and address information
- SEO metadata for storefront
- Product catalog visibility per store
- Fulfillment warehouse assignments with priority
- Shipping and payment method availability
- Store lifecycle (create, update, delete, restore)

**Structural Overview:**
```
Store (Aggregate Root)
│
├─ Core Identity
│  ├─ Id (unique identifier)
│  ├─ Code (unique, uppercase, 1-50 chars)
│  ├─ Name (unique, 1-100 chars)
│  ├─ Url (unique, lowercase, 1-255 chars)
│  └─ Presentation (alternative name for UI)
│
├─ Configuration
│  ├─ DefaultCurrency (USD|EUR|GBP|VND)
│  ├─ DefaultLocale (en, fr, de, es, etc.)
│  └─ Timezone (valid timezone identifier)
│
├─ Communication
│  ├─ MailFromAddress (transactional email sender)
│  ├─ CustomerSupportEmail (customer inquiries)
│  ├─ Phone (optional)
│  └─ Social (Facebook, Instagram, Twitter URLs)
│
├─ Physical Address
│  ├─ Address1, Address2
│  ├─ City, Zipcode
│  ├─ Company
│  ├─ Country (reference)
│  └─ State (reference)
│
├─ SEO & Branding
│  ├─ MetaTitle, MetaDescription, MetaKeywords
│  ├─ SeoTitle
│  └─ Metadata (public for API, private for admin)
│
├─ Operational Flags
│  ├─ Available (open for business)
│  ├─ Default (fallback store)
│  ├─ GuestCheckoutAllowed (registration required?)
│  ├─ PasswordProtected (beta/private store)
│  └─ IsDeleted (soft deletion)
│
├─ Owned Entities
│  ├─ StoreProduct[] (product visibility per store)
│  ├─ StoreStockLocation[] (warehouse assignments with priority)
│  ├─ StoreShippingMethod[] (shipping options per store)
│  └─ StorePaymentMethod[] (payment options per store)
│
├─ Computed Properties
│  ├─ Products (linked products read-only)
│  ├─ StockLocations (ordered by priority)
│  ├─ PrimaryStockLocation (highest priority warehouse)
│  ├─ AvailableShippingMethods (filtered by availability)
│  ├─ AvailablePaymentMethods (filtered by availability)
│  ├─ IsConfigured (has all required setup)
│  └─ ActiveOrderCount, CompletedOrderCount, VisibleProductCount
│
└─ Cross-Cutting Concerns
   ├─ IHasMetadata (public & private)
   ├─ IHasUniqueName (Name uniqueness)
   ├─ IHasSeoMetadata (Meta fields)
   ├─ IAddress (Physical address)
   ├─ IHasParameterizableName (Name + Presentation)
   ├─ ISoftDeletable (soft delete support)
   └─ IHasAuditable (CreatedAt, UpdatedAt)
```

### Owned Entities

#### StoreProduct
Maps products to stores with visibility control.

```csharp
StoreProduct
├─ StoreId, ProductId (composite FK)
├─ Visible (show/hide per store)
├─ Featured (highlight per store)
├─ Position (sort order)
└─ CreatedAt, UpdatedAt (audit)
```

#### StoreStockLocation
Assigns warehouses to stores with fulfillment priority.

```csharp
StoreStockLocation
├─ StoreId, StockLocationId (composite FK)
├─ Priority (1=check first, 2=check second, etc.)
├─ CanFulfillOrders (fulfillment toggle)
├─ IsAvailableForFulfillment (computed: can fulfill && warehouse active)
└─ CreatedAt, UpdatedAt (audit)
```

#### StoreShippingMethod
Links shipping options to stores with optional cost overrides.

```csharp
StoreShippingMethod
├─ StoreId, ShippingMethodId (composite FK)
├─ Available (enable/disable per store)
├─ StoreBaseCost (override global cost)
└─ CreatedAt, UpdatedAt (audit)
```

#### StorePaymentMethod
Links payment options to stores with availability control.

```csharp
StorePaymentMethod
├─ StoreId, PaymentMethodId (composite FK)
├─ Available (enable/disable per store)
└─ CreatedAt, UpdatedAt (audit)
```

---

## 📜 Business Rules & Invariants

### Critical Constraints

**Identity Rules**
- Store name must be unique (1-100 characters)
- Store code must be unique (1-50 characters, uppercase)
- Store URL must be unique (1-255 characters, lowercase)
- Codes auto-generated from names if not provided
- Names and codes must not be empty

**Configuration Rules**
- Currency must be one of: USD, EUR, GBP, VND
- Timezone must be valid TimeZoneInfo identifier
- Email addresses must be in valid RFC format (if provided)
- Locale defaults to 'en', timezone defaults to 'UTC'
- All email/timezone validation happens during Create/Update

**Relationship Rules**
- A product cannot be added twice to same store
- A warehouse cannot be linked twice to same store
- A shipping method cannot be added twice to same store
- A payment method cannot be added twice to same store

**Deletion Rules**
- Default store cannot be deleted without override
- Store with active orders cannot be deleted
- Soft deletion (not hard delete) for audit trail
- Can restore soft-deleted stores

**Operational Rules**
- Available flag controls customer access
- GuestCheckoutAllowed controls registration requirement
- PasswordProtected enables private store access
- Only one store should be marked as Default

---

## 🔄 Core Business Methods

### Creation
```csharp
Store.Create(name, presentation?, code?, url?, currency?, locale?, timezone?, 
             mailFromAddress?, customerSupportEmail?, isDefault?, publicMetadata?, privateMetadata?)
```

**Validation:**
- Name: required, 1-100 chars
- Code: auto-generated or 1-50 chars
- URL: auto-generated or 1-255 chars
- Currency: one of valid currencies
- Email addresses: valid format
- Timezone: valid timezone identifier

### Configuration Updates
```csharp
store.Update(name?, presentation?, url?, mailFromAddress?, customerSupportEmail?,
             metaTitle?, metaDescription?, metaKeywords?, seoTitle?,
             available?, guestCheckoutAllowed?, timezone?, defaultLocale?, defaultCurrency?,
             publicMetadata?, privateMetadata?)
```

**Behavior:**
- Only provided parameters are updated
- Triggers StoreUpdated event if changes made
- No event if no changes detected

### Address Management
```csharp
store.SetAddress(address1?, address2?, city?, zipcode?, phone?, company?, countryId?, stateId?)
```

### Social Media
```csharp
store.SetSocialLinks(facebook?, instagram?, twitter?)
```

### Store Status
```csharp
store.MakeDefault()  // Set as default store
store.ProtectWithPassword(hashedPassword)  // Enable password protection
store.RemovePasswordProtection()  // Disable password protection
store.Delete(force?)  // Soft delete (can restore)
store.Restore()  // Restore from soft delete
```

### Product Management
```csharp
store.AddProduct(product, visible?, featured?)
store.RemoveProduct(productId)
store.UpdateProductSettings(productId, visible?, featured?)
```

### Warehouse Configuration
```csharp
store.AddStockLocation(location, priority?)
store.RemoveStockLocation(stockLocationId)
store.UpdateStockLocationPriority(stockLocationId, priority)
```

### Shipping Configuration
```csharp
store.AddShippingMethod(method, available?, storeBaseCost?)
store.RemoveShippingMethod(shippingMethodId)
store.UpdateShippingMethodSettings(shippingMethodId, available?, storeBaseCost?)
```

### Payment Configuration
```csharp
store.AddPaymentMethod(method, available?)
store.RemovePaymentMethod(paymentMethodId)
```

---

## 🌍 Multi-Store Patterns

### Pattern 1: Product Visibility Control

**Use Case**: Same product sold in different stores with different visibility

```csharp
var jacket = await dbContext.Products.FindAsync(jacketId);

// Show in main fashion store
await mainStore.AddProduct(jacket, visible: true, featured: true);

// Hide in outlet store (out of stock in outlet)
await outletStore.AddProduct(jacket, visible: false);

// Update later when stock arrives
outletStore.UpdateProductSettings(jacketId, visible: true);
```

**Business Value**: Single product master data, per-store visibility control

### Pattern 2: Fulfillment Priority Ordering

**Use Case**: Different warehouses serve different stores, with priority ordering

```csharp
// Fashion store: prefer European warehouse
fashionStore.AddStockLocation(parisWarehouse, priority: 1);
fashionStore.AddStockLocation(chicagoWarehouse, priority: 2);

// Tech store: prefer US warehouse
techStore.AddStockLocation(chicagoWarehouse, priority: 1);
techStore.AddStockLocation(parisWarehouse, priority: 2);

// On order creation, fulfillment picks primary location
var warehouse = fashionStore.PrimaryStockLocation;  // Paris
var warehouse = techStore.PrimaryStockLocation;     // Chicago
```

**Business Value**: Minimize shipping costs by fulfilling from closest warehouse

### Pattern 3: Store-Specific Pricing

**Use Case**: Global shipping method, different costs per store

```csharp
var fastShipping = await dbContext.ShippingMethods.FindAsync(fastShippingId);

// US store: $12 expedited
usStore.AddShippingMethod(fastShipping, storeBaseCost: 12.00m);

// EU store: €15 expedited (higher international cost)
euStore.AddShippingMethod(fastShipping, storeBaseCost: 15.00m);

// Order shows correct cost based on store
```

**Business Value**: Region-appropriate pricing without duplicating configuration

### Pattern 4: Regional Configuration

**Use Case**: Each region has independent locale/currency

```csharp
// Asia-Pacific
apacStore.DefaultCurrency = "SGD";
apacStore.DefaultLocale = "en-SG";
apacStore.Timezone = "Asia/Singapore";

// Europe
euStore.DefaultCurrency = "EUR";
euStore.DefaultLocale = "de";
euStore.Timezone = "Europe/Berlin";

// Orders created in store use appropriate defaults
```

**Business Value**: Natural user experience in each market

### Pattern 5: Beta/Private Launches

**Use Case**: Limited access testing before public launch

```csharp
// Create as available but password protected
betaStore.ProtectWithPassword(hashedBetaPassword);
betaStore.Available = true;

// Later, go public
betaStore.RemovePasswordProtection();

// Or keep private permanently
```

**Business Value**: Controlled access for testing, phased rollout, or VIP exclusivity

---

## 📧 Metadata Usage Guide

### Public Metadata Examples

```csharp
store.PublicMetadata = new Dictionary<string, object?>
{
    // UI Configuration
    ["storeLogo"] = "https://cdn.example.com/stores/1/logo.png",
    ["bannerImage"] = "https://cdn.example.com/stores/1/banner.jpg",
    ["theme"] = "modern",  // light, dark, retro, etc.
    
    // Content
    ["tagline"] = "Premium Fashion at Outlet Prices",
    ["description"] = "Our flagship outlet store offering...",
    
    // Features
    ["maxProductsPerPage"] = 24,
    ["enableRatings"] = true,
    ["enableReviews"] = true,
    ["enableWishlist"] = true,
    
    // Branding
    ["primaryColor"] = "#FF5733",
    ["secondaryColor"] = "#33FF57"
};
```

### Private Metadata Examples

```csharp
store.PrivateMetadata = new Dictionary<string, object?>
{
    // Payment Gateway Integration
    ["stripeAccountId"] = "acct_1234567890",
    ["stripeWebhookSecret"] = "whsec_...",
    
    // Operational
    ["warehouseManagerEmail"] = "warehouse@company.com",
    ["accountManagerId"] = "mgr_001",
    ["vendorCommissionPercent"] = 0.15,
    
    // Audit & Compliance
    ["lastAuditDate"] = "2024-11-15",
    ["auditedBy"] = "john.doe@company.com",
    ["complianceStatus"] = "verified",
    
    // Internal Notes
    ["internalNotes"] = "VIP account - high volume, custom SLA",
    ["legacySystemId"] = "SHOP_00123",
    
    // Integrations
    ["erpsystemId"] = "ERP_FASHION_001",
    ["accountingCode"] = "1200-FASHION"
};
```

---

## 🔐 Security & Password Protection

### Use Cases for Password Protection

1. **Beta Testing**: Limit access to testers before public launch
2. **VIP/Loyalty**: Exclusive access for special customers
3. **Seasonal**: Temporary launches for limited campaigns
4. **Regional**: Different markets opening at different times

### Implementation Pattern

```csharp
// In Application/Command Layer
public class ProtectStoreWithPasswordCommand
{
    public Guid StoreId { get; set; }
    public string PlainPassword { get; set; }
}

public class ProtectStoreWithPasswordHandler : ICommandHandler<...>
{
    public async Task<ErrorOr<Unit>> Handle(ProtectStoreWithPasswordCommand command, ...)
    {
        var store = await _dbContext.Stores.FindAsync(command.StoreId);
        if (store == null) return Errors.StoreNotFound;
        
        // 1. Hash password in application layer
        var hashedPassword = BCrypt.Net.BCrypt.HashPassword(command.PlainPassword, 10);
        
        // 2. Apply to domain model
        var result = store.ProtectWithPassword(hashedPassword);
        if (result.IsError) return result.FirstError;
        
        // 3. Persist
        await _dbContext.SaveChangesAsync();
        return Unit();
    }
}
```

### Security Recommendations

- Use bcrypt, Argon2, or scrypt for hashing (minimum 10 rounds for bcrypt)
- Never store plain-text passwords in domain or database
- Implement rate limiting on password verification
- Consider two-factor authentication for sensitive stores
- Log all password protection changes for audit

---

## 📊 Soft Deletion & Audit Trail

Stores use soft deletion for compliance, audit, and recovery.

```csharp
// Soft delete (marks as deleted, data preserved)
var deleteResult = store.Delete();
// Sets: DeletedAt = DateTimeOffset.UtcNow, IsDeleted = true

// Query automatically excludes soft-deleted stores
var stores = await dbContext.Stores.ToListAsync();  // No deleted

// Include deleted if needed
var allStores = await dbContext.Stores
    .IgnoreQueryFilters()
    .ToListAsync();  // Includes deleted

// Restore if needed
var restoreResult = store.Restore();
// Sets: DeletedAt = null, IsDeleted = false
```

**Benefits**:
- Audit trail: See when store was deleted and by whom
- Recovery: Restore stores if deletion was mistake
- Compliance: Don't lose historical data
- Analytics: Track store lifecycle

---

## 🤝 External Dependencies

### Boundary Contexts

- **Catalog.Products**: Products linked via StoreProduct
- **Location**: Country/State references for addresses
- **Inventories**: StockLocations linked via StoreStockLocation
- **Shipping**: ShippingMethods linked via StoreShippingMethod
- **Payments**: PaymentMethods linked via StorePaymentMethod
- **Orders**: Orders isolated per store

### Event Subscribers

- **Analytics**: Track store creation, updates, deletions
- **Search**: Index/de-index products based on visibility
- **Webhooks**: Notify external systems of store changes
- **Cache**: Invalidate caches on configuration changes
- **Email**: Send notifications on important events

---

## 🚀 Common Workflows & Recipes

### Setup New Storefront (Complete)

```csharp
// 1. Create store with minimal info
var storeResult = Store.Create(
    name: "Summer Collection",
    code: "SUMMER",
    url: "summer.example.com",
    currency: "USD",
    mailFromAddress: "orders@summer.example.com",
    customerSupportEmail: "support@summer.example.com"
);
if (storeResult.IsError) throw new DomainException(storeResult.FirstError);
var store = storeResult.Value;

// 2. Set physical address
var addressResult = store.SetAddress(
    address1: "123 Fashion Avenue",
    city: "New York",
    zipcode: "10001",
    phone: "+1-800-555-0123",
    company: "Summer Fashion Inc.",
    countryId: usCountryId,
    stateId: nyStateId
);
if (addressResult.IsError) throw new DomainException(addressResult.FirstError);

// 3. Configure fulfillment
var addLocationResult = store.AddStockLocation(nyWarehouse, priority: 1);
if (addLocationResult.IsError) throw new DomainException(addLocationResult.FirstError);

// 4. Add shipping options
var shippingMethod = await dbContext.ShippingMethods.FindAsync(standardShippingId);
var addShippingResult = store.AddShippingMethod(
    shippingMethod,
    available: true,
    storeBaseCost: 10.00m
);
if (addShippingResult.IsError) throw new DomainException(addShippingResult.FirstError);

// 5. Add payment options
var paymentMethod = await dbContext.PaymentMethods.FindAsync(creditCardId);
var addPaymentResult = store.AddPaymentMethod(paymentMethod, available: true);
if (addPaymentResult.IsError) throw new DomainException(addPaymentResult.FirstError);

// 6. Link products
var productsToAdd = await dbContext.Products
    .Where(p => p.Status == ProductStatus.Active)
    .ToListAsync();

foreach (var product in productsToAdd)
{
    var addProductResult = store.AddProduct(product, visible: true);
    if (addProductResult.IsError) throw new DomainException(addProductResult.FirstError);
}

// 7. Set SEO metadata
var updateResult = store.Update(
    metaTitle: "Summer Collection 2024 - Fashion Store",
    metaDescription: "Browse our summer collection of premium fashion items",
    metaKeywords: "summer, fashion, collection, 2024"
);
if (updateResult.IsError) throw new DomainException(updateResult.FirstError);

// 8. Persist
dbContext.Stores.Add(store);
await dbContext.SaveChangesAsync();  // Events published

// Store is now ready: IsConfigured == true
```

### Migrate Legacy Store

```csharp
// Load with related data
var legacyStore = await dbContext.Stores
    .Include(s => s.StoreProducts)
    .Include(s => s.StoreStockLocations)
    .FirstOrDefaultAsync(s => s.Code == "LEGACY");

if (legacyStore != null)
{
    // Update core configuration
    var updateResult = legacyStore.Update(
        available: true,
        defaultCurrency: "EUR",
        timezone: "Europe/Amsterdam",
        defaultLocale: "nl"
    );
    if (updateResult.IsError) return Problem(updateResult.FirstError.Description);
    
    // Re-prioritize warehouses
    var warehouses = await dbContext.StockLocations.ToListAsync();
    var euWarehouse = warehouses.First(w => w.Code == "AMSTERDAM");
    
    // Remove old priority, add new
    legacyStore.RemoveStockLocation(oldWarehouseId);
    legacyStore.AddStockLocation(euWarehouse, priority: 1);
    
    await dbContext.SaveChangesAsync();
}
```

---

## 📈 Computed Properties Reference

### Status Properties

- `store.Available` - Is store active?
- `store.Default` - Is this the default store?
- `store.IsDeleted` - Is store soft-deleted?
- `store.PasswordProtected` - Does store require password?

### Configuration Status

- `store.IsConfigured` - Has all required setup:
  - `Available && StockLocations.Any() && AvailableShippingMethods.Any() && AvailablePaymentMethods.Any()`

### Resource Counts

- `store.Products` - All linked products (read-only)
- `store.VisibleProductCount` - Products visible to customers
- `store.ActiveOrderCount` - Current, non-completed orders
- `store.CompletedOrderCount` - Successfully completed orders

### Fulfillment

- `store.StockLocations` - All linked warehouses (ordered by priority)
- `store.PrimaryStockLocation` - Preferred warehouse (priority 1)
- `store.AvailableShippingMethods` - Enabled shipping options
- `store.AvailablePaymentMethods` - Enabled payment options

---

## ⚠️ Important Notes

1. **Code Generation**: Automatically generates from name if not provided; format: UPPERCASE_WITH_UNDERSCORES
2. **Uniqueness**: Name, Code, URL are all unique at database level
3. **Validation**: Emails and timezones validated during Create/Update
4. **Soft Deletion**: Always use soft deletion; hard delete loses audit trail
5. **Default Store**: Keep exactly one store marked as default
6. **Cascade Effects**: Deleting store cascades to StoreProducts, StoreShippingMethods, etc.
7. **No Hard Delete**: Use soft delete and archiving; hard delete not recommended
8. **Metadata Extensibility**: Use metadata for custom attributes without changing schema

---

## 🔗 Related Documentation

- **STORES_QUICK_REFERENCE.md** - Fast lookup for operations, constraints, errors
- **STORES_REFINEMENT_ANALYSIS.md** - Refinement roadmap and improvements
- **Store.cs** - Full implementation with method documentation and examples

---

**Document Version:** 1.0 Enhanced  
**Last Updated:** December 1, 2025  
**Status:** Production Ready
