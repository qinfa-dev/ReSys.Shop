# Shipping Domain - Business Context & Patterns Guide

**Architectural decisions, integration patterns, and design rationale for the Shipping bounded context**

---

## 📚 Table of Contents

1. [Overview](#overview)
2. [ShippingType Enum Guide](#shippingtype-enum-guide)
3. [Cost Calculation Strategy](#cost-calculation-strategy)
4. [Store-Specific Configuration](#store-specific-configuration)
5. [Integration with Orders Domain](#integration-with-orders-domain)
6. [Integration with Stores Domain](#integration-with-stores-domain)
7. [Metadata Usage Patterns](#metadata-usage-patterns)
8. [Multi-Currency Support](#multi-currency-support)
9. [Performance Considerations](#performance-considerations)
10. [Common Patterns](#common-patterns)

---

## 🎯 Overview

### Purpose in Multi-Store Architecture

The Shipping domain defines and manages delivery methods available across all stores in the e-commerce platform. Unlike Store-specific configuration, shipping methods are defined globally but can be customized per store through `StoreShippingMethod` mappings.

**Key Architectural Principle:**
- **Global Definition** → **Per-Store Customization** via StoreShippingMethod
- One "Ground Shipping" method used by 50 stores, each with potentially different cost/availability

### Core Responsibilities

1. **Define shipping methods** (name, type, base cost, delivery estimates)
2. **Calculate shipping costs** based on order weight and method configuration
3. **Enable store-specific customization** (pricing, availability)
4. **Provide audit trail** of shipping costs and availability changes
5. **Raise integration events** for external fulfillment systems

---

## 📦 ShippingType Enum Guide

### Type: Standard
```csharp
Type = ShippingMethod.ShippingType.Standard,
BaseCost = 5.99m,
EstimatedDaysMin = 5,
EstimatedDaysMax = 7
```

**Characteristics:**
- Economical, default option for most customers
- Ground-based delivery (USPS, UPS Ground, FedEx Ground)
- Typical delivery: 5-10 business days
- Lowest cost tier

**Use Cases:**
- Primary shipping option for price-conscious customers
- Bulk/heavy item shipments
- Non-urgent deliveries

**Business Configuration:**
```csharp
var result = ShippingMethod.Create(
    name: "Ground Shipping",
    presentation: "Standard Ground (5-7 days)",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 5.99m,
    estimatedDaysMin: 5,
    estimatedDaysMax: 7,
    maxWeight: 50,  // Applies surcharge to orders > 50 lbs
    position: 2);   // Lower priority than Express
```

### Type: Express
```csharp
Type = ShippingMethod.ShippingType.Express,
BaseCost = 14.99m,
EstimatedDaysMin = 2,
EstimatedDaysMax = 3
```

**Characteristics:**
- Faster delivery for business customers
- Premium cost tier
- Typical delivery: 2-3 business days
- Air-based or expedited ground

**Use Cases:**
- Business/professional customers
- Time-sensitive orders
- High-value items

**Business Configuration:**
```csharp
var result = ShippingMethod.Create(
    name: "Express Delivery",
    presentation: "Express (2-3 business days)",
    type: ShippingMethod.ShippingType.Express,
    baseCost: 14.99m,
    estimatedDaysMin: 2,
    estimatedDaysMax: 3,
    maxWeight: 30,  // Limited to lighter shipments
    position: 1);   // Higher priority than Ground
```

### Type: Overnight
```csharp
Type = ShippingMethod.ShippingType.Overnight,
BaseCost = 24.99m,
EstimatedDaysMin = 1,
EstimatedDaysMax = 1
```

**Characteristics:**
- Premium, guaranteed next-day delivery
- Highest cost tier
- 1 business day guaranteed
- Typically FedEx Overnight or UPS Next Day Air

**Use Cases:**
- Last-minute/emergency orders
- High-value/urgent items
- Executive customers

**Business Configuration:**
```csharp
var result = ShippingMethod.Create(
    name: "Overnight Express",
    presentation: "Overnight (guaranteed next day)",
    type: ShippingMethod.ShippingType.Overnight,
    baseCost: 24.99m,
    estimatedDaysMin: 1,
    estimatedDaysMax: 1,
    maxWeight: 20,  // Restricted to lightweight items
    position: 0);   // Highest priority (premium option)
```

### Type: Pickup
```csharp
Type = ShippingMethod.ShippingType.Pickup,
BaseCost = 0,
EstimatedDaysMin = 0,
EstimatedDaysMax = 1
```

**Characteristics:**
- Free or minimal cost fulfillment
- Customer picks up at store/warehouse
- Same-day or next-day availability
- Reduces shipping and logistics costs

**Use Cases:**
- "Buy Online, Pickup In Store" (BOPIS)
- Local fulfillment centers
- Reduces return shipping costs

**Business Configuration:**
```csharp
var result = ShippingMethod.Create(
    name: "Store Pickup",
    presentation: "Pick up today or tomorrow",
    type: ShippingMethod.ShippingType.Pickup,
    baseCost: 0,
    estimatedDaysMin: 0,
    estimatedDaysMax: 1,
    position: 1,
    publicMetadata: new Dictionary<string, object?>
    {
        { "pickup_locations", "5 nearby stores" }
    });
```

### Type: FreeShipping
```csharp
Type = ShippingMethod.ShippingType.FreeShipping,
BaseCost = 0,
EstimatedDaysMin = 7,
EstimatedDaysMax = 10
```

**Characteristics:**
- Promotional or threshold-based free shipping
- `IsFreeShipping` property automatically returns true
- Used for marketing campaigns
- Slower delivery (standard ground speed)

**Use Cases:**
- Seasonal promotions ("Free Shipping This Weekend")
- Order threshold incentives ("Free Shipping on $50+")
- Customer loyalty rewards

**Business Configuration:**
```csharp
var metadata = new Dictionary<string, object?>
{
    { "campaign_code", "SPRING_FREE_SHIP" },
    { "minimum_order_amount", 50m },
    { "campaign_start", "2025-03-01" },
    { "campaign_end", "2025-04-30" }
};

var result = ShippingMethod.Create(
    name: "Free Shipping - Spring 2025",
    presentation: "FREE Shipping (on orders $50+)",
    type: ShippingMethod.ShippingType.FreeShipping,
    baseCost: 0,
    estimatedDaysMin: 7,
    estimatedDaysMax: 10,
    position: 0,
    publicMetadata: metadata);
```

---

## 💰 Cost Calculation Strategy

### Algorithm Overview

```csharp
public decimal CalculateCost(decimal orderWeight, decimal orderTotal)
{
    // Step 1: Check for free shipping
    if (IsFreeShipping) return 0;
    
    // Step 2: Check weight surcharge
    if (MaxWeight.HasValue && orderWeight > MaxWeight.Value)
        return BaseCost * 1.5m;  // 50% surcharge for overweight
    
    // Step 3: Return standard cost
    return BaseCost;
}
```

### Pricing Tiers

**Standard Tier (No Surcharge)**
```
Cost = BaseCost
Condition: orderWeight ≤ MaxWeight (or MaxWeight not set)
Example: $5.99 base, 15 lb order, 20 lb limit → $5.99
```

**Overweight Tier (1.5x Surcharge)**
```
Cost = BaseCost * 1.5
Condition: orderWeight > MaxWeight
Example: $5.99 base, 25 lb order, 20 lb limit → $8.99 (1.5x)
```

**Free Shipping Tier**
```
Cost = $0
Condition: Type == FreeShipping OR BaseCost == 0
Example: Free Shipping method or $0 base cost → $0
```

### Configuration Example

```csharp
// Standard Ground with weight-based surcharge
var ground = ShippingMethod.Create(
    name: "Ground Shipping",
    presentation: "Ground (5-7 days)",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 5.99m,
    maxWeight: 20);  // Surcharge for > 20 lbs

// Cost matrix for this method:
// 5 lb order  → $5.99
// 15 lb order → $5.99
// 20 lb order → $5.99 (at limit)
// 21 lb order → $8.99 (over limit: 5.99 * 1.5)
// 50 lb order → $8.99 (still: 5.99 * 1.5)
```

### Future Extension Points

The algorithm can be extended to support:

**Tier-Based Pricing:**
```csharp
// Hypothetical future enhancement
if (orderTotal > 100) return BaseCost * 0.8m;  // 20% discount
if (orderTotal > 50) return BaseCost * 0.9m;   // 10% discount
```

**Destination-Based Pricing:**
```csharp
// Hypothetical future enhancement
if (destination == "Hawaii") return BaseCost * 2;
if (destination == "Alaska") return BaseCost * 1.8;
```

**Carrier Rate Integration:**
```csharp
// Hypothetical future enhancement
var carrierRate = await _carrierService.GetRate(
    weight: orderWeight,
    destination: address);
return carrierRate;
```

---

## 🏪 Store-Specific Configuration

### Architecture: Global + Per-Store Customization

**Global Level (ShippingMethod):**
- Define method once: name, type, base cost, delivery days
- Standard cost applies globally
- Represents the "standard" configuration

**Store Level (StoreShippingMethod):**
- Customize per store: availability, cost override
- Enable/disable method per store
- Override cost for different currencies/regions

### Example: Multi-Region Setup

```csharp
// 1. Define global shipping methods
var ground = ShippingMethod.Create(
    name: "Ground Shipping",
    presentation: "Ground (5-7 days)",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 5.99m).Value;

var overnight = ShippingMethod.Create(
    name: "Overnight",
    presentation: "Overnight (1 day)",
    type: ShippingMethod.ShippingType.Overnight,
    baseCost: 24.99m).Value;

// 2. Add to US store
var usStore = /* fetch store */;
usStore.StoreShippingMethods.Add(StoreShippingMethod.Create(
    storeId: usStore.Id,
    shippingMethodId: ground.Id,
    available: true,
    storeBaseCost: 5.99m).Value);
usStore.StoreShippingMethods.Add(StoreShippingMethod.Create(
    storeId: usStore.Id,
    shippingMethodId: overnight.Id,
    available: true,
    storeBaseCost: 24.99m).Value);

// 3. Add to EU store with EUR pricing
var euStore = /* fetch store */;
euStore.StoreShippingMethods.Add(StoreShippingMethod.Create(
    storeId: euStore.Id,
    shippingMethodId: ground.Id,
    available: true,
    storeBaseCost: 5.50m).Value);  // EUR 5.50 instead of USD 5.99
euStore.StoreShippingMethods.Add(StoreShippingMethod.Create(
    storeId: euStore.Id,
    shippingMethodId: overnight.Id,
    available: false).Value);  // Overnight not available in EU

// 4. Add to Japan store with JPY pricing
var jpStore = /* fetch store */;
jpStore.StoreShippingMethods.Add(StoreShippingMethod.Create(
    storeId: jpStore.Id,
    shippingMethodId: ground.Id,
    available: true,
    storeBaseCost: 650m).Value);  // JPY 650
jpStore.StoreShippingMethods.Add(StoreShippingMethod.Create(
    storeId: jpStore.Id,
    shippingMethodId: overnight.Id,
    available: false).Value);

await dbContext.SaveChangesAsync();
```

### Pattern: Temporary Regional Disable

```csharp
// Bad weather in Northeast - disable shipping
var affected = await dbContext.StoreShippingMethods
    .Where(sm => sm.Store.Region == "Northeast" && 
                 sm.ShippingMethod.Name == "Ground Shipping")
    .ToListAsync();

foreach (var sm in affected)
{
    sm.Update(available: false);
}

await dbContext.SaveChangesAsync();

// Later, re-enable
foreach (var sm in affected)
{
    sm.Update(available: true);
}

await dbContext.SaveChangesAsync();
```

---

## 🔗 Integration with Orders Domain

### Relationship: ShippingMethod → Shipment

**One-to-Many Relationship:**
```
ShippingMethod
  ↓ (one to many)
  Shipment (in Orders domain)
```

**Delete Constraint: Restrict**
- Cannot delete ShippingMethod if Shipments reference it
- Prevents broken shipment references
- Error: `ShippingMethod.Errors.InUse`

**Recommended Approach:**
```csharp
// Instead of deleting:
method.Update(active: false);  // Deactivate

// Benefits:
// - Preserves shipment history
// - Audit trail remains intact
// - Can be re-enabled later
```

### Order Checkout Flow Integration

```
1. Customer adds items to cart
   ↓
2. System queries available ShippingMethods for this store:
   var methods = await dbContext.ShippingMethods
       .Where(m => m.Active)
       .Where(m => m.StoreShippingMethods.Any(
           sm => sm.StoreId == storeId && sm.Available))
       .OrderBy(m => m.Position)
   ↓
3. Present options to customer (name, presentation, cost, delivery)
   ↓
4. Customer selects method
   ↓
5. System calculates shipping cost:
   var cost = method.CalculateCost(
       orderWeight: order.TotalWeight,
       orderTotal: order.Total);
   ↓
6. On order completion, create Shipment:
   var shipment = Shipment.Create(
       orderId: order.Id,
       shippingMethodId: method.Id,
       ...)
   ↓
7. Events.Updated event published
```

---

## 🏢 Integration with Stores Domain

### Bidirectional References

```
ShippingMethod
  ↓ owns
  StoreShippingMethod
  ↑ owned by
Store
```

### Cascade Behavior

**Delete ShippingMethod:**
```
→ Cascade delete all StoreShippingMethod entries
→ Stores no longer offer that method
→ No data loss (cascade is soft, not hard delete from DB)
```

**Delete Store:**
```
→ Cascade delete all StoreShippingMethod entries
→ ShippingMethod remains available to other stores
→ Clean removal of store's configuration
```

### Query Pattern: Methods by Store

```csharp
// Get all available methods for a store
var storeId = Guid.Parse("...");

var availableMethods = await dbContext.ShippingMethods
    .Where(m => m.Active)
    .Where(m => m.StoreShippingMethods.Any(sm => 
        sm.StoreId == storeId && 
        sm.Available))
    .OrderBy(m => m.Position)
    .Select(m => new
    {
        m.Id,
        m.Name,
        m.Presentation,
        Cost = dbContext.StoreShippingMethods
            .First(sm => sm.ShippingMethodId == m.Id && 
                         sm.StoreId == storeId)
            .StoreBaseCost ?? m.BaseCost,
        m.EstimatedDaysMin,
        m.EstimatedDaysMax
    })
    .ToListAsync();
```

---

## 📋 Metadata Usage Patterns

### PublicMetadata (Customer-Visible)

```csharp
var method = ShippingMethod.Create(
    name: "USPS Ground",
    presentation: "USPS Ground",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 4.99m,
    publicMetadata: new Dictionary<string, object?>
    {
        // Carrier info
        { "carrier_name", "USPS" },
        { "carrier_website", "https://www.usps.com" },
        { "carrier_logo_url", "https://cdn.example.com/usps-logo.png" },
        
        // Promotional info
        { "promotion_text", "Free on orders over $50" },
        { "free_threshold", 50 },
        
        // Environmental
        { "carbon_neutral", true },
        { "eco_friendly", true }
    });
```

**Used for:**
- Displaying carrier logo in checkout
- Marketing/promotional messaging
- Environmental marketing
- Trust indicators

### PrivateMetadata (Internal Use)

```csharp
var method = ShippingMethod.Create(
    name: "USPS Ground",
    presentation: "USPS Ground",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 4.99m,
    privateMetadata: new Dictionary<string, object?>
    {
        // Carrier API configuration
        { "carrier_api_provider", "USPS" },
        { "carrier_api_key", "sk_live_xxxxx" },
        { "carrier_api_version", "2.1" },
        { "carrier_webhook_url", "https://api.example.com/webhooks/usps" },
        
        // Rate update frequency
        { "rate_update_interval_hours", 24 },
        { "last_rate_update", DateTimeOffset.UtcNow },
        
        // Deprecation tracking
        { "is_deprecated", false },
        { "deprecation_date", null },
        { "replacement_method_id", null }
    });
```

**Used for:**
- Third-party carrier API keys and configuration
- Integration endpoints and webhooks
- Rate update intervals
- Deprecation/migration tracking

---

## 💱 Multi-Currency Support

### Strategy: Store-Level Pricing

```csharp
// Global method (USD base)
var ground = ShippingMethod.Create(
    name: "Ground Shipping",
    presentation: "Ground (5-7 days)",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 5.99m,
    currency: "USD").Value;

// US Store (inherits USD)
var usStore = StoreShippingMethod.Create(
    storeId: usStoreId,
    shippingMethodId: ground.Id,
    available: true);
    // storeBaseCost = null (uses global 5.99 USD)

// EU Store (EUR converted via StoreBaseCost)
var euStore = StoreShippingMethod.Create(
    storeId: euStoreId,
    shippingMethodId: ground.Id,
    available: true,
    storeBaseCost: 5.50m);  // Converted to EUR equivalent
    // Customer sees €5.50 instead of $5.99

// Japan Store (JPY converted)
var jpStore = StoreShippingMethod.Create(
    storeId: jpStoreId,
    shippingMethodId: ground.Id,
    available: true,
    storeBaseCost: 650m);   // Converted to JPY equivalent
    // Customer sees ¥650 instead of $5.99
```

### Pricing Table

| Store | Currency | Method | Price | Source |
|-------|----------|--------|-------|--------|
| US | USD | Ground | $5.99 | Global (ShippingMethod.BaseCost) |
| EU | EUR | Ground | €5.50 | Store Override (StoreShippingMethod.StoreBaseCost) |
| JP | JPY | Ground | ¥650 | Store Override (StoreShippingMethod.StoreBaseCost) |

### Currency Configuration (Store)

```csharp
// Store defines its currency
var euStore = Store.Create(
    name: "European Store",
    currency: "EUR",
    ...);

// When mapping shipping method to store,
// StoreBaseCost should be in EUR
var storeShippingMethod = StoreShippingMethod.Create(
    storeId: euStore.Id,
    shippingMethodId: groundShipping.Id,
    storeBaseCost: 5.50m);  // EUR 5.50

// Later, when calculating order total:
var orderTotal = order.Items.Sum(i => i.Price);  // In EUR
var shippingCost = storeMethod.StoreBaseCost ?? shippingMethod.BaseCost;
// Result: €5.50
```

---

## ⚡ Performance Considerations

### Indexes

**Current:**
```sql
-- Unique index on Name
CREATE UNIQUE INDEX IX_ShippingMethods_Name ON ShippingMethods(Name);
```

**Recommended Additional:**
```sql
-- For "Active methods" queries
CREATE INDEX IX_ShippingMethods_Active ON ShippingMethods(Active);

-- For "Type-based" queries
CREATE INDEX IX_ShippingMethods_Type ON ShippingMethods(Type);

-- For "Position ordering" in checkout
CREATE INDEX IX_ShippingMethods_Position_Active 
    ON ShippingMethods(Position, Active);
```

### Query Optimization

**❌ Inefficient:**
```csharp
var methods = await dbContext.ShippingMethods.ToListAsync();
var active = methods.Where(m => m.Active).ToList();
var ordered = active.OrderBy(m => m.Position).ToList();
```
(Loads all records into memory, then filters)

**✅ Efficient:**
```csharp
var methods = await dbContext.ShippingMethods
    .Where(m => m.Active)
    .OrderBy(m => m.Position)
    .ToListAsync();
```
(Filters and orders at database level)

### Caching Strategy

```csharp
// Cache store-specific shipping options
var cacheKey = $"store:{storeId}:shipping-methods";

var methods = await _cache.GetOrCreateAsync(cacheKey, async entry =>
{
    entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1);
    return await dbContext.ShippingMethods
        .Where(m => m.Active)
        .Where(m => m.StoreShippingMethods.Any(sm => 
            sm.StoreId == storeId && sm.Available))
        .OrderBy(m => m.Position)
        .ToListAsync();
});

// Invalidate cache on method update
[MediatR event handler]
public async Task Handle(ShippingMethod.Events.Updated notification)
{
    await _cache.RemoveAsync($"store:*:shipping-methods");
}
```

---

## 🔄 Common Patterns

### Pattern 1: Seasonal Enable/Disable

```csharp
// Before summer, enable Free Shipping
var freeShipping = await dbContext.ShippingMethods
    .FirstAsync(m => m.Name == "Summer Free Shipping");

freeShipping.Update(active: true);
await dbContext.SaveChangesAsync();

// After summer, disable
freeShipping.Update(active: false);
await dbContext.SaveChangesAsync();
```

### Pattern 2: A/B Testing Methods

```csharp
// Create two variants for A/B testing
var methodA = ShippingMethod.Create(
    name: "Express (Test A)",
    presentation: "Fast Shipping",
    type: ShippingMethod.ShippingType.Express,
    baseCost: 12.99m).Value;

var methodB = ShippingMethod.Create(
    name: "Express (Test B)",
    presentation: "Express - Same Day Cut-off",
    type: ShippingMethod.ShippingType.Express,
    baseCost: 14.99m).Value;

// Add to store test group
testStoreA.StoreShippingMethods.Add(
    StoreShippingMethod.Create(testStoreA.Id, methodA.Id, true).Value);

testStoreB.StoreShippingMethods.Add(
    StoreShippingMethod.Create(testStoreB.Id, methodB.Id, true).Value);

// Track conversion rates and choose winner
```

### Pattern 3: Regional Restrictions

```csharp
// Not available in certain regions
var methodMetadata = new Dictionary<string, object?>
{
    { "excluded_regions", new[] { "Hawaii", "Alaska", "APO" } },
    { "excluded_states", new[] { "PR", "GU", "VI" } }
};

var result = ShippingMethod.Create(
    name: "Express",
    presentation: "Express (2-3 days)",
    type: ShippingMethod.ShippingType.Express,
    baseCost: 14.99m,
    publicMetadata: methodMetadata);

// At checkout, filter based on destination address
var shippingOptions = availableMethods
    .Where(m => IsAvailableForAddress(m, customerAddress))
    .ToList();
```

---

**Last Updated:** December 1, 2025  
**Version:** 1.0  
**Status:** Complete
