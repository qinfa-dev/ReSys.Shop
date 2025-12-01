# Catalog Quick Reference Guide

**Status:** In Progress  
**Domain:** Catalog Bounded Context  
**Core Classes:** Product, Variant, OptionType, Taxon  
**Last Updated:** 2024

---

## 🎯 Quick Navigation

- **[Product Lifecycle](#product-lifecycle)** - State machine and transitions
- **[Variant Strategy](#variant-strategy)** - Master vs non-master variants
- **[Pricing](#pricing-strategy)** - Multi-currency, cost vs sale price
- **[Inventory](#inventory-management)** - Stock tracking, backorders
- **[Categories](#category-management)** - Manual and automatic taxons
- **[Common Operations](#common-operations)** - Create, update, assign
- **[Error Reference](#error-reference)** - Validation and conflict errors
- **[FAQ](#frequently-asked-questions)** - Best practices

---

## Product Lifecycle

### State Transitions

```
┌────────┐
│ DRAFT  │ ← Initial state (not visible to customers)
└───┬────┘
    │ Activate()
    ↓
┌────────┐
│ ACTIVE │ ← Visible on storefront, available for purchase
└───┬────┘
    │ Archive() or Discontinue()
    ↓
┌──────────┐
│ ARCHIVED │ ← Retired product, kept for order history
└──────────┘
```

### Status Details

| Status | Visible | Purchasable | Editable | Use Case |
|--------|---------|-------------|----------|----------|
| **Draft** | ❌ No | ❌ No | ✅ Yes | Work in progress, preparing product |
| **Active** | ✅ Yes | ✅ Yes* | ✅ Yes | Product live on storefront |
| **Archived** | ❌ No | ❌ No | ✅ Limited | Product retired, orders preserved |

*Purchasable if at least one variant has inventory and is marked purchasable

### Automatic Status Changes

Products can have automatic status transitions based on dates:

```csharp
// Create product with date-based transitions
var product = Product.Create(
    name: "Limited Edition T-Shirt",
    makeActiveAt: DateTime.UtcNow.AddDays(7),      // Auto-active in 7 days
    discontinueOn: DateTime.UtcNow.AddDays(30));   // Auto-discontinue in 30 days
```

---

## Variant Strategy

### Master vs Non-Master Variants

| Aspect | Master | Non-Master |
|--------|--------|-----------|
| **Quantity** | Exactly 1 per product | 0 or more |
| **Option Values** | ❌ Cannot have | ✅ Can have (e.g., Blue, Small) |
| **Deletion** | ❌ Cannot delete | ✅ Can delete |
| **Purpose** | Default variant | Specific configurations (color, size) |
| **Typical Use** | Base pricing, default stock | Different SKUs, color/size combos |

### When to Create Variants

**Create variants when:**
```csharp
// ✅ Same product, different options
Product: "T-Shirt"
├── Variant 1 (Master): Default
├── Variant 2: Blue, Size S
├── Variant 3: Blue, Size M
├── Variant 4: Red, Size S
└── Variant 5: Red, Size M

// ✅ Same product, different SKUs
├── Variant 1 (Master): Base model
├── Variant 2: Premium model (higher price, different specs)
└── Variant 3: Economy model (lower price)
```

**Create separate products when:**
```csharp
// ❌ Product is fundamentally different
X Shirts with Shorts - Create 2 separate products
X Related but independent items

// ❌ Requires completely different descriptions
X T-shirt vs Hoodie from same brand
```

### Variant Price Inheritance

```csharp
// Variant can have multiple prices (multi-currency)
variant.Prices
├── Price: USD $29.99 (PriceId: guid1)
├── Price: EUR €28.99 (PriceId: guid2)
└── Price: GBP £24.99 (PriceId: guid3)

// At order time, LineItem captures specific variant price
lineItem.UnitPriceCents = 2999;  // Frozen at order creation
// Later price changes to variant don't affect this lineItem
```

---

## Pricing Strategy

### Multi-Currency Setup

```csharp
// Each variant supports pricing in multiple currencies
var result = variant.AddPrice(
    currencyCode: "USD",
    amountCents: 2999);  // $29.99

var result = variant.AddPrice(
    currencyCode: "EUR",
    amountCents: 2899);  // €28.99
```

### Cost vs Sale Price

```csharp
// Variant tracks both cost and sale price
variant.CostPrice = 800;         // $8.00 (internal cost)
variant.CostCurrency = "USD";
variant.Prices[0].AmountCents = 2999;  // $29.99 (sale price)

// Used for margin calculations (not enforced in domain)
Margin = (SalePrice - CostPrice) / SalePrice = 73%
```

### Price Capture Strategy

**Critical: Prices are captured at order time, not dynamically retrieved**

```csharp
// When LineItem is created, price is frozen
var lineItem = order.AddLineItem(
    variant: blueShirtMedium,
    quantity: 2);
// lineItem.UnitPriceCents = current variant price (captured)

// Later, you change variant price:
variant.AddPrice("USD", 3999);  // Increase to $39.99

// Existing order's lineItem is UNAFFECTED
lineItem.UnitPriceCents;  // Still 2999 ($29.99)
```

---

## Inventory Management

### Multi-Location Stock Tracking

```
Product
└── Variant (e.g., "Blue T-Shirt Medium")
    ├── StockItem: Warehouse A
    │   └── QuantityOnHand: 150
    ├── StockItem: Warehouse B
    │   └── QuantityOnHand: 45
    └── StockItem: Retail Store NYC
        └── QuantityOnHand: 8

Total Quantity On Hand = 150 + 45 + 8 = 203
```

### Stock Availability Decisions

```csharp
// Variant.Purchasable: Can customer buy this variant?
variant.Purchasable 
    = variant.TrackInventory == false              // Digital products
    || (variant.QuantityOnHand > 0)                // In stock
    || (variant.Backorderable && !variant.TrackInventory);

// Variant.InStock: Is variant currently available?
variant.InStock = !variant.TrackInventory || variant.QuantityOnHand > 0;

// Variant.Backorderable: Can customer preorder when out of stock?
variant.Backorderable = variant.BackorderableQuantity > 0;

// Product-level checks aggregate across variants
product.InStock = product.Variants.Any(v => v.InStock);
product.Purchasable = product.Variants.Any(v => v.Purchasable);
```

### Digital vs Physical Products

```csharp
// Physical Product (Default)
var tshirt = Product.Create(
    name: "T-Shirt",
    isDigital: false);

tshirt.Variants[0].TrackInventory = true;
tshirt.Variants[0].AddStockItem(warehouse, quantity: 100);

// Digital Product
var ebook = Product.Create(
    name: "E-book",
    isDigital: true);

ebook.Variants[0].TrackInventory = false;  // Infinite inventory
// No stock locations needed
```

---

## Category Management

### Manual vs Automatic Categories

#### Manual Categories (Traditional Hierarchy)

```
Apparel (Taxonomy)
├── Men's (Taxon) - Manual
│   ├── Shirts (Taxon) - Manual
│   │   ├── T-Shirts (Taxon) - Manual
│   │   └── Dress Shirts (Taxon) - Manual
│   └── Pants (Taxon) - Manual
└── Women's (Taxon) - Manual
    └── Dresses (Taxon) - Manual
```

**Usage:**
```csharp
// Create hierarchy manually
var apparel_taxon = CreateTaxon("Apparel", parent: null);
var mens_taxon = CreateTaxon("Men's", parent: apparel_taxon);
var shirts_taxon = CreateTaxon("Shirts", parent: mens_taxon);

// Assign product to category
product.AddClassification(shirts_taxon);
```

#### Automatic Categories (Rule-Based)

```
Best Sellers (Automatic Taxon)
├── Rule: Sales count > 1000
├── Rule: Rating > 4.5 stars
└── Match Policy: ALL rules must pass
```

**Behavior:** Products matching rules are automatically assigned

```csharp
// Create automatic taxon
var bestSellers = Taxon.Create(
    taxonomyId: categories.Id,
    name: "Best Sellers",
    automatic: true,
    rulesMatchPolicy: "all",  // All rules must match
    sortOrder: "best-selling");

// Add rules (system automatically evaluates)
bestSellers.AddRule(salesCountRule);
bestSellers.AddRule(ratingRule);

// Products matching both rules automatically belong
// When sales count or rating changes, product membership updates
```

### Nested Set Model (Performance Optimization)

Categories use **nested set model** for efficient hierarchical queries:

```
             Root
           /  |  \
          /   |   \
        L1   L1    L1
       / \   / \   / \
      L2  L2 L2 L2 L2 L2

Lft/Rgt values enable:
✅ Get all descendants: WHERE Lft > parent.Lft AND Rgt < parent.Rgt
✅ Check if ancestor: WHERE Lft < node.Lft AND Rgt > node.Rgt
✅ Count descendants: (node.Rgt - node.Lft - 1) / 2
```

---

## Common Operations

### Creating a Product

```csharp
// 1. Create with basic info
var result = Product.Create(
    name: "Premium Cotton T-Shirt",
    slug: "premium-cotton-t-shirt",
    description: "100% organic cotton, sustainable...",
    metaTitle: "Premium T-Shirts | Our Store",
    metaDescription: "Shop premium organic cotton t-shirts...",
    isDigital: false);

if (result.IsError) return Problem(result.FirstError);
var product = result.Value;

// 2. Add pricing to master variant
var master = product.GetMaster().Value;
master.AddPrice("USD", 2999);  // $29.99
master.AddPrice("EUR", 2899);  // €28.99

// 3. Add inventory
master.AddOrUpdateStockItem(
    stockLocationId: warehouse.Id,
    quantity: 500);

// 4. Add non-master variants
var blueSmall = product.AddVariant(sku: "TS-BLU-SM").Value;
var blueSmall.AddOptionValue(blueOption);
var blueSmall.AddOptionValue(smallOption);

// 5. Assign to categories
product.AddClassification(mensShirts);
product.AddClassification(apparel);

// 6. Activate
product.Activate();

// 7. Save
await dbContext.SaveChangesAsync();
```

### Adding Variants with Options

```csharp
// Create variant
var variant = product.AddVariant(sku: "TS-RED-LG").Value;

// Link option values (size, color)
variant.AddOptionValue(redColorOption);
variant.AddOptionValue(largeOption);

// Now this variant represents: Red, Large configuration
```

### Managing Images

```csharp
// Add product images
product.AddImage(
    imageUrl: "https://cdn.example.com/ts-blue.jpg",
    altText: "Blue T-Shirt Front View",
    position: 0);

product.AddImage(
    imageUrl: "https://cdn.example.com/ts-blue-back.jpg",
    altText: "Blue T-Shirt Back View",
    position: 1);

// Retrieve default (first) image
var defaultImage = product.DefaultImage;
var secondaryImage = product.SecondaryImage;
```

### Discontinuing Products

```csharp
// Option 1: Immediate discontinuation
var result = product.Discontinue();

// Option 2: Schedule discontinuation
var result = product.Discontinue(until: DateTime.UtcNow.AddDays(90));
```

---

## Error Reference

### Validation Errors

| Error | Cause | Resolution |
|-------|-------|-----------|
| `NameRequired` | Product name is missing | Provide non-empty name |
| `NameTooLong` | Name exceeds max length | Shorten to <200 chars |
| `SlugInvalidFormat` | Slug contains invalid characters | Use lowercase, alphanumeric, hyphens only |
| `DescriptionTooLong` | Description exceeds limit | Shorten description |
| `InvalidStatus` | Status not in [Draft, Active, Archived] | Use valid status values |

### Conflict Errors

| Error | Cause | Resolution |
|-------|-------|-----------|
| `HasVariants` | Product has non-master variants | Delete variants first or soft-delete product |
| `CannotDeleteWithCompleteOrders` | Product has order history | Use soft delete instead |
| `OptionTypeAlreadyAdded` | Option type already linked | Remove old link before re-adding |
| `PropertyAlreadyAdded` | Property already linked | Remove old link before re-adding |

### Date Validation

| Error | Cause | Resolution |
|-------|-------|-----------|
| `DiscontinueOnBeforeMakeActiveAt` | Discontinue date before activate date | Set discontinue after activate date |

---

## Frequently Asked Questions

### When should I create a variant vs a separate product?

**Create variants when:**
- Same core product with different options (colors, sizes)
- Same SKU stem but different configurations
- Prices should be related/comparable
- Customers expect to see together

**Create separate products when:**
- Fundamentally different items (shirt vs pants)
- Different brands or manufacturers
- Completely different pricing strategy
- Different product categories

### How do I handle digital products?

```csharp
var ebook = Product.Create(
    name: "JavaScript Guide",
    isDigital: true);  // ← Key difference

// No inventory tracking needed
var master = ebook.GetMaster().Value;
master.TrackInventory = false;  // Infinite availability
master.AddPrice("USD", 1999);   // $19.99
```

### Can I change a product's category after creation?

**Yes!** Use `AddClassification()` and `RemoveClassification()`:

```csharp
product.AddClassification(newTaxon);
product.RemoveClassification(oldTaxon);
```

### What happens when I change a variant's price?

**New orders capture the new price; existing orders are unaffected:**

```csharp
// Original order captured price
originalOrder.LineItem.UnitPriceCents = 2999;  // $29.99

// Change price
variant.UpdatePrice("USD", 3999);  // $39.99

// New order captures new price
newOrder.LineItem.UnitPriceCents = 3999;  // $39.99
// Original order unaffected
```

### How do automatic taxons work?

```csharp
// 1. Define rules (e.g., sales count > 1000)
// 2. System evaluates rules on products
// 3. Matching products automatically assigned
// 4. When rules re-evaluate, membership updates

// Benefits:
// - No manual category maintenance
// - Dynamic grouping based on data
// - Always up-to-date
```

### Can a product be in multiple categories?

**Yes!** Use multiple classifications:

```csharp
product.AddClassification(mensShirts);    // Men's > Shirts
product.AddClassification(summerwear);    // Seasonal > Summer
product.AddClassification(bestsellers);   // Auto-generated
```

### What's the difference between Name and Presentation?

```csharp
product.Name = "premium-cotton-t-shirt";      // Database/system
product.Presentation = "Premium Cotton T-Shirt";  // Display to customers

// Allows:
// - Clean internal names
// - Formatted display names with capitals, special chars
```

### How does soft delete work?

```csharp
product.SoftDelete(deletedBy: currentUserId);

// Result:
product.IsDeleted = true;
product.DeletedAt = DateTimeOffset.UtcNow;
product.DeletedBy = "user-123";

// Behavior:
// ✅ Product data preserved
// ✅ Queries auto-exclude soft-deleted products
// ❌ Product not visible on storefront
// ❌ Cannot add new orders
```

---

## Product Concerns (Built-In Features)

All products automatically support these cross-cutting features:

| Concern | Features | Example |
|---------|----------|---------|
| **IHasParameterizableName** | Name + Presentation | Internal name vs display name |
| **IHasUniqueName** | Unique constraint | Database enforces uniqueness |
| **IHasSlug** | URL-friendly slug | `/products/premium-t-shirt` |
| **IHasSeoMetadata** | Meta tags | MetaTitle, MetaDescription, MetaKeywords |
| **IHasMetadata** | Flexible KV storage | PublicMetadata, PrivateMetadata |
| **ISoftDeletable** | Logical deletion | DeletedAt, DeletedBy, IsDeleted |

---

## Performance Considerations

### Inventory Calculations

```csharp
// ⚠️ Expensive: Full aggregation
product.TotalOnHand  // Sums all variants' all stock items

// ✅ Prefer: Specific queries when possible
var warehouseStock = variant.StockItems
    .Where(si => si.StockLocationId == warehouse.Id)
    .Sum(si => si.QuantityOnHand);
```

### Category Hierarchy Queries

Thanks to nested set model:

```csharp
// ✅ Fast: Get all descendants
var descendants = dbContext.Taxons
    .Where(t => t.Lft > parent.Lft && t.Rgt < parent.Rgt)
    .ToList();

// ✅ Fast: Get all ancestors
var ancestors = dbContext.Taxons
    .Where(t => t.Lft < node.Lft && t.Rgt > node.Rgt)
    .OrderByDescending(t => t.Depth)
    .ToList();
```

---

## Domain Events

Products raise these events for subscribers:

| Event | When | Use Case |
|-------|------|----------|
| `Created` | New product created | Update search index, notify analytics |
| `Updated` | Product details changed | Update product cache |
| `Activated` | Status → Active | Notify subscribers it's live |
| `Archived` | Product archived | Update storefront visibility |
| `VariantAdded` | Non-master variant added | Update variant cache |
| `ClassificationAdded` | Added to category | Update category product count |

---

**Reference Completed:** Quick Reference guide provides rapid developer lookup  
**Next Steps:** Continue with Variant, OptionType, Taxon documentation and create refinement summary

