# Shipping Domain - Quick Reference Guide

**Complete developer reference for shipping method operations**  
**Comprehensive code examples, constraints, errors, and patterns**

---

## 📚 Table of Contents

1. [Quick Start Examples](#quick-start-examples)
2. [Constraints Reference](#constraints-reference)
3. [Errors Reference](#errors-reference)
4. [Cost Calculation Examples](#cost-calculation-examples)
5. [ShippingType Reference](#shippingtype-reference)
6. [Store-Specific Patterns](#store-specific-patterns)
7. [Common Workflows](#common-workflows)
8. [Domain Events](#domain-events)
9. [Query Patterns](#query-patterns)
10. [Testing Patterns](#testing-patterns)

---

## ⚡ Quick Start Examples

### Example 1: Create Standard Ground Shipping

```csharp
// Create a basic ground shipping method
var result = ShippingMethod.Create(
    name: "Ground Shipping",
    presentation: "Standard Ground (5-7 business days)",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 5.99m,
    description: "Economical ground delivery option",
    estimatedDaysMin: 5,
    estimatedDaysMax: 7,
    position: 2); // Display after Express

if (result.IsSuccess)
{
    _dbContext.ShippingMethods.Add(result.Value);
    await _dbContext.SaveChangesAsync();
    // Events.Created published automatically
}
```

### Example 2: Create Express Overnight Shipping

```csharp
// Premium next-day delivery
var result = ShippingMethod.Create(
    name: "Overnight Express",
    presentation: "Overnight (1 business day)",
    type: ShippingMethod.ShippingType.Overnight,
    baseCost: 24.99m,
    description: "Get your order tomorrow morning",
    estimatedDaysMin: 1,
    estimatedDaysMax: 1,
    active: true,
    position: 0, // Highest priority/first option
    displayOn: DisplayOn.Both);

if (result.IsSuccess)
{
    var shippingMethod = result.Value;
    _dbContext.ShippingMethods.Add(shippingMethod);
    await _dbContext.SaveChangesAsync();
}
```

### Example 3: Create Free Shipping

```csharp
// Promotional free shipping (for orders over $50)
var metadata = new Dictionary<string, object?>
{
    { "campaign_code", "SUMMER_FREE_SHIP_25" },
    { "minimum_order", 50m },
    { "campaign_start", "2025-06-01" },
    { "campaign_end", "2025-08-31" }
};

var result = ShippingMethod.Create(
    name: "Free Shipping - Summer 2025",
    presentation: "FREE Shipping (on orders $50+)",
    type: ShippingMethod.ShippingType.FreeShipping,
    baseCost: 0,
    description: "Summer promotion - Free shipping on orders over $50",
    estimatedDaysMin: 7,
    estimatedDaysMax: 10,
    position: 1,
    publicMetadata: metadata);

// IsFreeShipping automatically returns true
Debug.Assert(result.Value.IsFreeShipping == true);
```

### Example 4: Update Shipping Method

```csharp
// Find and update a method
var method = await _dbContext.ShippingMethods.FindAsync(methodId);

// Update only specific fields
var updateResult = method.Update(
    baseCost: 6.99m,      // Increase price
    active: false,        // Disable method
    estimatedDaysMin: 4,  // Faster delivery
    estimatedDaysMax: 6);

if (updateResult.IsSuccess)
{
    // UpdatedAt automatically set
    // Events.Updated published
    await _dbContext.SaveChangesAsync();
}
```

### Example 5: Calculate Shipping Cost

```csharp
var method = await _dbContext.ShippingMethods
    .FirstAsync(m => m.Name == "Ground Shipping");

// Standard order (within weight limit)
var cost1 = method.CalculateCost(orderWeight: 10, orderTotal: 100);
// Returns: $5.99

// Overweight order (20 lb limit, order is 30 lbs)
var cost2 = method.CalculateCost(orderWeight: 30, orderTotal: 100);
// Returns: $8.99 (1.5x surcharge)

// Free shipping method
var freeMethod = await _dbContext.ShippingMethods
    .FirstAsync(m => m.Type == ShippingMethod.ShippingType.FreeShipping);
var cost3 = freeMethod.CalculateCost(orderWeight: 100, orderTotal: 50);
// Returns: $0 (IsFreeShipping check)
```

### Example 6: Configure for Multiple Stores

```csharp
var groundShipping = await _dbContext.ShippingMethods
    .FirstAsync(m => m.Name == "Ground Shipping");

// Add to US store with default cost
var usMethod = StoreShippingMethod.Create(
    storeId: usStoreId,
    shippingMethodId: groundShipping.Id,
    available: true);
store.StoreShippingMethods.Add(usMethod.Value);

// Add to EU store with Euro pricing override
var euMethod = StoreShippingMethod.Create(
    storeId: euStoreId,
    shippingMethodId: groundShipping.Id,
    available: true,
    storeBaseCost: 5.50m); // Override to €5.50 instead of $5.99
store.StoreShippingMethods.Add(euMethod.Value);

await _dbContext.SaveChangesAsync();
```

### Example 7: Query Available Methods

```csharp
// Get all active shipping methods ordered by priority
var activeMethods = await _dbContext.ShippingMethods
    .Where(m => m.Active)
    .OrderBy(m => m.Position)
    .Select(m => new 
    { 
        m.Id, 
        m.Name, 
        m.Presentation, 
        m.BaseCost,
        Estimate = $"{m.EstimatedDaysMin}-{m.EstimatedDaysMax} days"
    })
    .ToListAsync();

// Get express shipping methods
var expressMethods = await _dbContext.ShippingMethods
    .Where(m => m.IsExpressShipping && m.Active)
    .ToListAsync();

// Get free shipping
var freeMethods = await _dbContext.ShippingMethods
    .Where(m => m.IsFreeShipping && m.Active)
    .ToListAsync();
```

### Example 8: Bulk Update Prices

```csharp
// Increase all shipping costs by 5%
var methods = await _dbContext.ShippingMethods
    .Where(m => m.Active && !m.IsFreeShipping)
    .ToListAsync();

foreach (var method in methods)
{
    var newCost = method.BaseCost * 1.05m;
    method.Update(baseCost: newCost);
}

await _dbContext.SaveChangesAsync();
```

### Example 9: Deactivate Shipping Method

```csharp
// Don't delete, deactivate instead to preserve shipment history
var method = await _dbContext.ShippingMethods.FindAsync(methodId);

method.Update(active: false);
await _dbContext.SaveChangesAsync();

// Method is now excluded from customer selection but:
// - All existing shipments remain intact
// - History is preserved
// - Can be reactivated later with Update(active: true)
```

### Example 10: Add Carrier Integration Metadata

```csharp
// Store third-party carrier API keys in PrivateMetadata
var result = ShippingMethod.Create(
    name: "USPS Ground",
    presentation: "USPS Ground",
    type: ShippingMethod.ShippingType.Standard,
    baseCost: 4.99m,
    privateMetadata: new Dictionary<string, object?>
    {
        { "carrier", "USPS" },
        { "service_type", "GROUND_ADVANTAGE" },
        { "api_key", "sk_live_usps_xxxxx" },
        { "webhook_url", "https://api.example.com/shipping/usps/webhooks" },
        { "rate_update_interval_hours", 24 },
        { "integration_version", "2.1" }
    },
    publicMetadata: new Dictionary<string, object?>
    {
        { "carrier_logo_url", "https://cdn.example.com/usps-logo.png" },
        { "carrier_website", "https://www.usps.com" }
    });
```

---

## 📋 Constraints Reference

| Constraint | Value | Purpose | Example |
|-----------|-------|---------|---------|
| NameMaxLength | 100 | Max identifier length | "Ground Shipping" |
| BaseCost Min | 0 | Allows free shipping | FreeShipping: 0m |
| BaseCost Max | decimal.MaxValue | No hard upper limit | Premium: 99.99m |
| EstimatedDaysMin | > 0 (optional) | Minimum delivery days | 5 for 5-7 day option |
| EstimatedDaysMax | ≥ EstimatedDaysMin | Maximum delivery days | 7 for 5-7 day option |
| MaxWeight | > 0 (optional) | Weight threshold | 20 lbs for surcharge |
| Position | ≥ 0 | Display order (0=highest) | 0,1,2,3... |
| Type | 5 valid values | Shipping category | Standard, Express, etc. |
| Currency | ISO 4217 code | Currency standard | USD, EUR, GBP, JPY |
| Active | boolean | Enable/disable flag | true/false |

---

## ⚠️ Errors Reference

### Error: Required
```
Code: "ShippingMethod.Required"
Description: "Shipping method is required."
Type: Validation
```

**When it occurs:** At checkout, no shipping method available or selected
**Recovery:** Ensure at least one active, available method exists in the store

```csharp
if (shippingMethod == null)
{
    return ShippingMethod.Errors.Required;
}
```

### Error: NotFound
```
Code: "ShippingMethod.NotFound"
Description: "Shipping method with ID '{id}' was not found."
Type: NotFound
```

**When it occurs:** Requested ID doesn't exist or was deleted
**Recovery:** Verify ID, reload available methods

```csharp
var method = await _dbContext.ShippingMethods.FindAsync(id);
if (method == null)
{
    return ShippingMethod.Errors.NotFound(id);
}
```

### Error: InUse
```
Code: "ShippingMethod.InUse"
Description: "Cannot delete shipping method that is in use."
Type: Conflict
```

**When it occurs:** Attempting to delete a method with active shipments
**Recovery:** Deactivate instead (Active=false) or remove all shipments first

```csharp
var activeShipments = await _dbContext.Shipments
    .Where(s => s.ShippingMethodId == methodId)
    .AnyAsync();

if (activeShipments)
{
    return ShippingMethod.Errors.InUse;
}

// Alternative: Deactivate instead of delete
method.Update(active: false);
await _dbContext.SaveChangesAsync();
```

---

## 💰 Cost Calculation Examples

### Scenario 1: Standard Cost
```csharp
var method = new ShippingMethod 
{ 
    BaseCost = 5.99m, 
    MaxWeight = 20,
    Type = ShippingMethod.ShippingType.Standard 
};

// Order weight: 15 lbs (under limit)
var cost = method.CalculateCost(orderWeight: 15, orderTotal: 150);
Assert.That(cost, Is.EqualTo(5.99m));
```

### Scenario 2: Overweight Surcharge
```csharp
// Order weight: 25 lbs (exceeds 20 lb limit)
var cost = method.CalculateCost(orderWeight: 25, orderTotal: 150);
// Calculation: 5.99 * 1.5 = 8.985 → rounds to 8.99
Assert.That(cost, Is.EqualTo(8.99m));
```

### Scenario 3: Free Shipping (Type)
```csharp
var method = new ShippingMethod 
{ 
    BaseCost = 0, 
    Type = ShippingMethod.ShippingType.FreeShipping 
};

var cost = method.CalculateCost(orderWeight: 100, orderTotal: 50);
Assert.That(cost, Is.EqualTo(0));
```

### Scenario 4: Free Shipping (Cost)
```csharp
var method = new ShippingMethod 
{ 
    BaseCost = 0,  // Free
    Type = ShippingMethod.ShippingType.Standard  // Not FreeShipping type
};

var cost = method.CalculateCost(orderWeight: 50, orderTotal: 100);
Assert.That(cost, Is.EqualTo(0));  // Still free due to BaseCost
```

### Scenario 5: No Weight Limit
```csharp
var method = new ShippingMethod 
{ 
    BaseCost = 12.99m, 
    MaxWeight = null,  // No limit
    Type = ShippingMethod.ShippingType.Express 
};

// Even very heavy orders use base cost
var cost = method.CalculateCost(orderWeight: 500, orderTotal: 1000);
Assert.That(cost, Is.EqualTo(12.99m));
```

---

## 🎯 ShippingType Reference

### Standard
```csharp
public enum ShippingType 
{ 
    Standard,      // Economical, 5-10 days typical
    Express,       // Fast, 2-3 days typical
    Overnight,     // Premium, 1 day guaranteed
    Pickup,        // Store pickup
    FreeShipping   // Promotional/threshold-based
}
```

| Type | Typical Cost | Typical Days | Use Case |
|------|-------------|-------------|----------|
| **Standard** | $5-10 | 5-10 | Default economical option |
| **Express** | $10-20 | 2-3 | Business/urgent orders |
| **Overnight** | $20-35 | 1 | Premium/guaranteed delivery |
| **Pickup** | $0-5 | 0 | In-store or location pickup |
| **FreeShipping** | $0 | 7-10 | Promotions or thresholds |

**Determining Type:**
```csharp
var type = method.Type switch
{
    ShippingMethod.ShippingType.Standard => "5-7 days",
    ShippingMethod.ShippingType.Express => "2-3 days",
    ShippingMethod.ShippingType.Overnight => "1 day",
    ShippingMethod.ShippingType.Pickup => "Immediate",
    ShippingMethod.ShippingType.FreeShipping => "Free (7-10 days)",
    _ => "Unknown"
};
```

---

## 🏪 Store-Specific Patterns

### Pattern 1: Regional Cost Override

```csharp
// Global method definition
var groundMethod = await _dbContext.ShippingMethods
    .FirstAsync(m => m.Name == "Ground Shipping");

// Store-specific costs
var usStore = StoreShippingMethod.Create(
    storeId: usStoreId,
    shippingMethodId: groundMethod.Id,
    available: true,
    storeBaseCost: 5.99m);  // USD

var euStore = StoreShippingMethod.Create(
    storeId: euStoreId,
    shippingMethodId: groundMethod.Id,
    available: true,
    storeBaseCost: 5.50m);  // EUR converted

var jpStore = StoreShippingMethod.Create(
    storeId: jpStoreId,
    shippingMethodId: groundMethod.Id,
    available: true,
    storeBaseCost: 650m);   // JPY

// Result: Same method, different prices per store's currency
```

### Pattern 2: Selective Method Availability

```csharp
// Define methods globally
var overnight = await _dbContext.ShippingMethods
    .FirstAsync(m => m.Name == "Overnight");
var ground = await _dbContext.ShippingMethods
    .FirstAsync(m => m.Name == "Ground");

// Availability per store
var northStore = store1;
northStore.StoreShippingMethods.Add(
    StoreShippingMethod.Create(northStore.Id, overnight.Id, available: true).Value);
northStore.StoreShippingMethods.Add(
    StoreShippingMethod.Create(northStore.Id, ground.Id, available: true).Value);

var remoteStore = store2;  // Remote location, limited options
remoteStore.StoreShippingMethods.Add(
    StoreShippingMethod.Create(remoteStore.Id, ground.Id, available: true).Value);
// No overnight to remote store

await _dbContext.SaveChangesAsync();
```

### Pattern 3: Temporary Disable Without Deletion

```csharp
// Weather event - disable Ground temporarily
var groundMethod = await _dbContext.ShippingMethods
    .FirstAsync(m => m.Name == "Ground");

groundMethod.Update(active: false);  // Still in database
await _dbContext.SaveChangesAsync();

// Later, re-enable
groundMethod.Update(active: true);
await _dbContext.SaveChangesAsync();
```

---

## 🔄 Common Workflows

### Workflow 1: Setup Initial Shipping Methods

```csharp
var methods = new[]
{
    ShippingMethod.Create(
        name: "Ground Shipping",
        presentation: "Standard Ground (5-7 days)",
        type: ShippingMethod.ShippingType.Standard,
        baseCost: 5.99m,
        estimatedDaysMin: 5,
        estimatedDaysMax: 7,
        position: 1).Value,
    
    ShippingMethod.Create(
        name: "Express Delivery",
        presentation: "Express (2-3 days)",
        type: ShippingMethod.ShippingType.Express,
        baseCost: 14.99m,
        estimatedDaysMin: 2,
        estimatedDaysMax: 3,
        position: 0).Value,
    
    ShippingMethod.Create(
        name: "Free Shipping",
        presentation: "FREE (7-10 days)",
        type: ShippingMethod.ShippingType.FreeShipping,
        baseCost: 0,
        estimatedDaysMin: 7,
        estimatedDaysMax: 10,
        position: 2).Value
};

_dbContext.ShippingMethods.AddRange(methods);
await _dbContext.SaveChangesAsync();
```

### Workflow 2: Sync Store Methods After Configuration Change

```csharp
// Global method price increased
var method = await _dbContext.ShippingMethods.FindAsync(methodId);
method.Update(baseCost: 7.99m);

// Update all store overrides that use default cost
var storeMethodsWithDefaultCost = await _dbContext.StoreShippingMethods
    .Where(sm => sm.ShippingMethodId == methodId && sm.StoreBaseCost == null)
    .ToListAsync();

// Already use global cost, no updates needed
// Stores with custom cost unaffected
await _dbContext.SaveChangesAsync();
```

### Workflow 3: Audit Shipping Costs

```csharp
var shippingAudit = await _dbContext.ShippingMethods
    .Where(m => m.Active)
    .Select(m => new 
    { 
        m.Name,
        m.Type,
        m.BaseCost,
        m.MaxWeight,
        m.EstimatedDaysMin,
        m.EstimatedDaysMax,
        m.CreatedAt,
        m.UpdatedAt,
        StoreCount = m.StoreShippingMethods.Count(),
        ShipmentCount = m.Shipments.Count()
    })
    .ToListAsync();

foreach (var item in shippingAudit)
{
    Console.WriteLine($"{item.Name}: ${item.BaseCost} ({item.Type}), " +
        $"Used by {item.StoreCount} stores, {item.ShipmentCount} shipments");
}
```

---

## 📡 Domain Events

### Event: Created
```csharp
// Raised when new shipping method is created
public sealed record ShippingMethod.Events.Created(
    Guid ShippingMethodId,
    string Name
) : DomainEvent;

// Published after: await dbContext.SaveChangesAsync();

// Handlers typically:
// - Update search indices
// - Notify logistics partners
// - Log audit trail
```

### Event: Updated
```csharp
// Raised when shipping method is updated
public sealed record ShippingMethod.Events.Updated(
    Guid ShippingMethodId
) : DomainEvent;

// Handlers typically:
// - Update cached shipping options
// - Notify of price changes
// - Refresh indices
```

### Event: Deleted
```csharp
// Raised when shipping method is deleted
public sealed record ShippingMethod.Events.Deleted(
    Guid ShippingMethodId
) : DomainEvent;

// Handlers typically:
// - Remove from indexes
// - Cascade to stores
// - Archive changes
```

---

## 🔍 Query Patterns

### Get Available Methods for Checkout

```csharp
var checkoutMethods = await _dbContext.ShippingMethods
    .Where(m => m.Active && 
                m.DisplayOn == DisplayOn.FrontEnd || m.DisplayOn == DisplayOn.Both)
    .OrderBy(m => m.Position)
    .Select(m => new ShippingOptionDto
    {
        Id = m.Id,
        Name = m.Presentation,
        Description = m.Description,
        EstimatedDays = $"{m.EstimatedDaysMin}-{m.EstimatedDaysMax} days",
        Cost = m.BaseCost,
        IsFree = m.IsFreeShipping
    })
    .ToListAsync();
```

### Get Methods by Store

```csharp
var storeShippingOptions = await _dbContext.ShippingMethods
    .Where(m => m.StoreShippingMethods.Any(sm => 
                    sm.StoreId == storeId && sm.Available))
    .OrderBy(m => m.Position)
    .ToListAsync();
```

### Find Cheapest Shipping

```csharp
var cheapestOption = await _dbContext.ShippingMethods
    .Where(m => m.Active && m.BaseCost < 10)
    .OrderBy(m => m.BaseCost)
    .FirstOrDefaultAsync();
```

---

## 🧪 Testing Patterns

### Test: Cost Calculation

```csharp
[TestFixture]
public class ShippingCostCalculationTests
{
    [Test]
    public void CalculateCost_WithinWeightLimit_ReturnsBaseCost()
    {
        var method = new ShippingMethod
        {
            BaseCost = 5.99m,
            MaxWeight = 20,
            Type = ShippingMethod.ShippingType.Standard
        };

        var cost = method.CalculateCost(orderWeight: 15, orderTotal: 100);
        Assert.That(cost, Is.EqualTo(5.99m));
    }

    [Test]
    public void CalculateCost_ExceedsWeightLimit_AppliesSurcharge()
    {
        var method = new ShippingMethod
        {
            BaseCost = 5.99m,
            MaxWeight = 20
        };

        var cost = method.CalculateCost(orderWeight: 25, orderTotal: 100);
        Assert.That(cost, Is.EqualTo(8.985m).Within(0.01m));
    }

    [Test]
    public void CalculateCost_FreeShipping_ReturnsZero()
    {
        var method = new ShippingMethod
        {
            Type = ShippingMethod.ShippingType.FreeShipping
        };

        var cost = method.CalculateCost(orderWeight: 100, orderTotal: 500);
        Assert.That(cost, Is.EqualTo(0));
    }
}
```

### Test: Factory Method

```csharp
[Test]
public void Create_ValidParams_SucceedsAndRaisesEvent()
{
    var result = ShippingMethod.Create(
        name: "Test Method",
        presentation: "Test Display",
        type: ShippingMethod.ShippingType.Standard,
        baseCost: 10.00m);

    Assert.That(result.IsSuccess, Is.True);
    Assert.That(result.Value.HasUncommittedEvents(), Is.True);
    Assert.That(result.Value.Name, Is.EqualTo("Test Method"));
}
```

---

**Last Updated:** December 1, 2025  
**Version:** 1.0  
**Status:** Complete
