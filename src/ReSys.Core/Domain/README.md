# Core.Domain Layer - Comprehensive Guide

Welcome to the comprehensive guide for the **ReSys.Shop Core.Domain Layer**. This document provides an overview of the domain architecture, key patterns, how to set up domain models, and practical examples for working with the bounded contexts.

---

## 📚 Table of Contents

1. [Overview](#overview)
2. [Purpose & Principles](#purpose--principles)
3. [Ubiquitous Language](#ubiquitous-language)
4. [Core Architecture Patterns](#core-architecture-patterns)
5. [Domain Concerns (Cross-Cutting Interfaces)](#domain-concerns-cross-cutting-interfaces)
6. [Validation & Constraints](#validation--constraints)
7. [Error Handling](#error-handling)
8. [Domain Events](#domain-events)
9. [Complete Example: Building an Order](#complete-example-building-an-order)
10. [EF Core Configuration Guide](#ef-core-configuration-guide)
11. [Testing Best Practices](#testing-best-practices)
12. [Bounded Contexts Overview](#bounded-contexts-overview)

---

## 🎯 Overview

The **ReSys.Shop Core.Domain** layer is the heart of the application, implementing **Domain-Driven Design (DDD)** principles using .NET 9. It encapsulates all business logic, rules, and invariants, remaining completely independent of infrastructure and presentation concerns.

**Key Characteristics:**
- **Persistence Ignorant**: No direct dependencies on databases, APIs, or UI frameworks
- **Rich Domain Model**: Entities contain behavior (methods) that enforce business rules
- **Testable**: Business logic can be verified independently without external dependencies
- **Decoupled**: Bounded contexts interact through domain events, not direct coupling
- **Modular**: Each bounded context can evolve independently

---

## 🎯 Purpose & Principles

The `Core.Domain` layer serves as the single source of truth for the application's business logic:

-   **Model the Business Domain**: Accurately represent real-world e-commerce concepts
-   **Enforce Business Invariants**: Ensure all business rules are consistently applied
-   **Establish Ubiquitous Language**: Common language between domain experts and developers
-   **Decouple Business Logic**: Keep core logic isolated from technical concerns
-   **Facilitate Scalability**: Organize complex logic into manageable, cohesive Bounded Contexts

---

## 🗣️ Ubiquitous Language

Key terms used consistently throughout the domain:

- **Bounded Context**: A logical boundary within which a domain model is defined
- **Aggregate Root**: Entry point for all operations on a cluster of related objects
- **Entity**: An object defined by its identity; has a lifecycle
- **Value Object**: Immutable object defined by its attributes, not identity
- **Domain Service**: Operations spanning multiple aggregates
- **Business Rule/Invariant**: Conditions that must always be true
- **Domain Event**: Messages signifying important domain state changes
- **Factory Method**: Encapsulates aggregate creation logic with validation
- **Concern**: Reusable interface defining common behavior across entities

---

## 🏛️ Core Architecture Patterns

### 1. Aggregates & Aggregate Roots

Aggregates group related entities under a root entity that controls access:

**Base Classes:**
- `Entity<TId>`: Basic entity with identity
- `AuditableEntity<TId>`: Adds audit tracking (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)
- `Aggregate<TId>`: Adds domain event tracking and versioning
- `Aggregate`: Convenience version using `Guid` as ID

**Example: Order Aggregate**

```csharp
public sealed class Order : Aggregate, IHasMetadata
{
    // Inherited properties
    // public Guid Id { get; }
    // public DateTimeOffset CreatedAt { get; set; }
    // public DateTimeOffset? UpdatedAt { get; set; }
    // public long Version { get; set; }
    // public IReadOnlyList<IDomainEvent> DomainEvents { get; }

    // Domain-specific properties
    public Guid StoreId { get; set; }
    public string Number { get; set; }
    public OrderState State { get; set; }

    // Owned entities (only accessible through Order aggregate)
    public ICollection<LineItem> LineItems { get; set; }
    public ICollection<OrderAdjustment> Adjustments { get; set; }

    // Business logic methods
    public ErrorOr<Order> AddLineItem(Variant variant, int quantity)
    {
        // Validation and state management
        if (variant == null) return Error.Validation("Variant required");
        if (quantity < 1) return Errors.InvalidQuantity;

        var existing = LineItems.FirstOrDefault(li => li.VariantId == variant.Id);
        if (existing != null)
        {
            return existing.UpdateQuantity(existing.Quantity + quantity);
        }

        var lineItemResult = LineItem.Create(Id, variant, quantity, Currency);
        if (lineItemResult.IsError) return lineItemResult.FirstError;

        LineItems.Add(lineItemResult.Value);
        RecalculateTotals();
        AddDomainEvent(new Events.LineItemAdded(Id, variant.Id, quantity));
        return this;
    }

    public ErrorOr<Order> Next()
    {
        return State switch
        {
            Order.OrderState.Cart => ToAddress(),
            Order.OrderState.Address => ToDelivery(),
            Order.OrderState.Delivery => ToPayment(),
            Order.OrderState.Payment => ToConfirm(),
            Order.OrderState.Confirm => Complete(),
            _ => Errors.InvalidStateTransition(State, State + 1)
        };
    }
}
```

### 2. Enumeration Patterns

Domain enumerations define valid state values:

```csharp
public class Order : Aggregate
{
    public enum OrderState 
    { 
        Cart = 0, 
        Address = 1, 
        Delivery = 2, 
        Payment = 3, 
        Confirm = 4, 
        Complete = 5, 
        Canceled = 6 
    }
}

public class Promotion : Aggregate
{
    public enum PromotionType { None, OrderDiscount, ItemDiscount, FreeShipping }
    public enum DiscountType { Percentage, FixedAmount }
}

public class Product : Aggregate
{
    public enum ProductStatus { Draft, Active, Archived }
}
```

### 3. Factory Methods

Factory methods encapsulate creation logic with validation:

```csharp
var orderResult = Order.Create(
    storeId: storeId,
    currency: "USD",
    userId: userId,
    email: "customer@example.com"
);

if (orderResult.IsError)
{
    return BadRequest(orderResult.FirstError.Description);
}

var order = orderResult.Value;
```

### 4. Business Logic Methods

Business logic returns `ErrorOr<T>` for railway-oriented programming:

```csharp
var addItemResult = order.AddLineItem(variant, quantity: 2);
if (addItemResult.IsError)
{
    return BadRequest(addItemResult.FirstError.Description);
}

var addressResult = order.SetShippingAddress(userAddress);
if (addressResult.IsError)
{
    return BadRequest(addressResult.FirstError.Description);
}

var nextStateResult = order.Next();
if (nextStateResult.IsError)
{
    return BadRequest(nextStateResult.FirstError.Description);
}
```

---

## Domain Concerns (Cross-Cutting Interfaces)

Domain Concerns provide reusable patterns with:
- Default implementations for common operations
- Fluent validation rules
- EF Core configuration

### 1. **IHasDisplayOn** - Visibility Control

Controls visibility on frontend, backend, or both:

```csharp
public enum DisplayOn { None, Both, FrontEnd, BackEnd }

public class OptionValue : Entity, IHasDisplayOn
{
    public string Value { get; set; }
    public DisplayOn DisplayOn { get; set; }
}

// Usage
if (optionValue.AvailableOnFrontEnd())
{
    // Show on storefront
}

// Validation
validator.AddDisplayOnRules(prefix: nameof(OptionValue));

// EF Core Configuration
builder.ConfigureDisplayOn();
```

### 2. **IHasUniqueName** - Unique Name Constraint

Ensures entity names are unique:

```csharp
public sealed class Promotion : Aggregate, IHasUniqueName
{
    public string Name { get; set; }
}

// Validate uniqueness
var isUnique = await dbContext.Promotions
    .CheckNameIsUniqueAsync(
        name: "Summer Sale",
        prefix: nameof(Promotion),
        exclusions: promotionId); // Exclude current ID when updating

// EF Core Configuration (creates unique index)
builder.ConfigureUniqueName<Promotion>();
```

### 3. **IHasSlug** - URL-Friendly Slugs

Provides slug functionality:

```csharp
public sealed class Product : Aggregate, IHasSlug
{
    public string Name { get; set; }
    public string Slug { get; set; }
}

// Auto-generate from name
var finalSlug = string.IsNullOrWhiteSpace(slug) 
    ? name.ToSlug() 
    : slug.ToSlug();

// Validate slug format
if (!CommonInput.Constraints.SlugsAndVersions.SlugRegex.IsMatch(finalSlug))
    return Errors.SlugInvalidFormat;
```

### 4. **IHasMetadata** - Custom Metadata Storage

Stores arbitrary key-value metadata:

```csharp
public sealed class Order : Aggregate, IHasMetadata
{
    public IDictionary<string, object?> PublicMetadata { get; set; }
    public IDictionary<string, object?> PrivateMetadata { get; set; }
}

// Set metadata
var order = new Order
{
    PublicMetadata = new Dictionary<string, object?>
    {
        { "gift_message", "Happy Birthday!" },
        { "gift_wrap", true }
    },
    PrivateMetadata = new Dictionary<string, object?>
    {
        { "internal_notes", "Rush delivery" },
        { "cost_center", "SALES-Q3-2024" }
    }
};
```

### 5. **IHasAuditable** - Audit Information

Automatically tracked (inherited by all Aggregates):

```csharp
public sealed class Promotion : Aggregate
{
    // Inherited from Aggregate -> AuditableEntity
    // public DateTimeOffset CreatedAt { get; set; }
    // public DateTimeOffset? UpdatedAt { get; set; }
    // public string? CreatedBy { get; set; }
    // public string? UpdatedBy { get; set; }
}
```

### 6. **ISoftDeletable** - Soft Delete Support

Marks entities as deleted without removing:

```csharp
public sealed class Product : Aggregate, ISoftDeletable
{
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }
}

// Soft delete
var deleteResult = product.Delete();
if (deleteResult.IsError)
{
    return BadRequest(deleteResult.FirstError.Description);
}
// EF Core query filters automatically exclude soft-deleted items
builder.HasQueryFilter(p => !p.IsDeleted);
```

### 7. **IHasSeoMetadata** - SEO Information

```csharp
public sealed class Product : Aggregate, IHasSeoMetadata
{
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }
}
```

### 8. **IHasParameterizableName** - Dual Name Storage

Separates display name from system name:

```csharp
public sealed class Product : Aggregate, IHasParameterizableName
{
    public string Name { get; set; }           // "T-Shirt Blue Medium"
    public string Presentation { get; set; }   // "Classic Blue T-Shirt (Medium)"
}

(name, presentation) = HasParameterizableName.NormalizeParams(name, presentation);
```

### Other Available Concerns
- `IHasCreator`: Track creator information
- `IHasUpdater`: Track last updater information
- `IHasVersion`: Version control support
- `IHasPosition`: Ordering/sequencing support
- `IHasIdentity`: Identity-related properties
- `IHasAssignable`: Assignee support
- `IAsset`: Asset/file tracking
- `IHasFilterParam`: Dynamic filter support

---

## Validation & Constraints

### Constraint Definitions

All constraints centralized in `CommonInput.Constraints`:

```csharp
namespace ReSys.Core.Common.Constants;

public static partial class CommonInput
{
    public static class Constraints
    {
        public static class Text
        {
            public const int MinLength = 1;
            public const int MaxLength = 1000;
            public const int DescriptionMaxLength = 500;
            public const int TitleMaxLength = 200;
        }

        public static class Email
        {
            public const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            public static readonly Regex Regex = new(Pattern, RegexOptions.Compiled);
            public const int MaxLength = 256;
        }

        public static class SlugsAndVersions
        {
            public const string SlugPattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
            public static readonly Regex SlugRegex = new(SlugPattern, RegexOptions.Compiled);
            public const int SlugMaxLength = 200;
        }

        // ... and many more categories
    }
}
```

### Using Constraints in Models

```csharp
public sealed class Promotion : Aggregate
{
    public static class Constraints
    {
        public const int NameMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
        public const int CodeMaxLength = 50;
        public const int MinUsageLimit = 1;
    }

    public static ErrorOr<Promotion> Create(
        string name,
        string? code = null,
        int? usageLimit = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Errors.NameRequired;
        
        if (name.Length > Constraints.NameMaxLength)
            return Errors.NameTooLong;

        if (usageLimit.HasValue && usageLimit < Constraints.MinUsageLimit)
            return Errors.InvalidUsageLimit;

        var promotion = new Promotion
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            PromotionCode = code?.Trim().ToUpperInvariant(),
            UsageLimit = usageLimit,
            CreatedAt = DateTimeOffset.UtcNow
        };

        return promotion;
    }
}
```

---

## Error Handling

### Domain Errors

Errors defined as static properties on aggregates:

```csharp
public sealed class Order : Aggregate
{
    public static class Errors
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound(
                code: "Order.NotFound", 
                description: $"Order with ID '{id}' was not found.");

        public static Error InvalidStateTransition(OrderState from, OrderState to) => 
            Error.Validation(
                code: "Order.InvalidStateTransition", 
                description: $"Cannot transition from {from} to {to}.");

        public static Error CannotCancelCompleted => 
            Error.Validation(
                code: "Order.CannotCancelCompleted", 
                description: "Cannot cancel completed order.");

        public static Error InvalidQuantity => 
            CommonInput.Errors.TooFewItems(
                field: nameof(LineItem.Quantity), 
                min: Constraints.QuantityMinValue);

        public static Error AddressRequired => 
            CommonInput.Errors.Required(field: "Address");
    }
}
```

### Common Errors Helper

Use `CommonInput.Errors` for standardized messages:

```csharp
public static partial class CommonInput
{
    public static class Errors
    {
        public static Error Required(string field, string? prefix = null) => 
            Error.Validation(
                code: "Validation.Required", 
                description: $"{(prefix != null ? $"{prefix}." : "")}{field} is required.");

        public static Error TooLong(string field, int maxLength, string? prefix = null) => 
            Error.Validation(
                code: "Validation.TooLong", 
                description: $"{(prefix != null ? $"{prefix}." : "")}{field} cannot exceed {maxLength} characters.");

        public static Error TooFewItems(string field, int min, string? prefix = null) => 
            Error.Validation(
                code: "Validation.TooFew", 
                description: $"{(prefix != null ? $"{prefix}." : "")}{field} must have at least {min}.");

        // ... and many more
    }
}
```

---

## Domain Events

Domain Events communicate state changes in a decoupled manner:

### Event Definition

```csharp
public sealed class Order : Aggregate
{
    public static class Events
    {
        public sealed record Created(Guid OrderId, Guid StoreId) : DomainEvent;
        public sealed record StateChanged(Guid OrderId, OrderState NewState) : DomainEvent;
        public sealed record Completed(Guid OrderId, Guid StoreId) : DomainEvent;
        public sealed record Canceled(Guid OrderId, Guid StoreId) : DomainEvent;
        public sealed record LineItemAdded(Guid OrderId, Guid VariantId, int Quantity) : DomainEvent;
        public sealed record LineItemRemoved(Guid OrderId, Guid LineItemId) : DomainEvent;
        public sealed record PromotionApplied(Guid OrderId, Guid PromotionId, decimal DiscountAmount) : DomainEvent;
        public sealed record PromotionRemoved(Guid OrderId, Guid PromotionId) : DomainEvent;
        public sealed record FinalizeInventory(Guid OrderId, Guid StoreId) : DomainEvent;
        public sealed record ReleaseInventory(Guid OrderId, Guid StoreId) : DomainEvent;
    }
}
```

### Publishing Events

Events are raised within aggregate methods:

```csharp
public class Order : Aggregate
{
    public static ErrorOr<Order> Create(Guid storeId, string currency, string? userId = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            Currency = currency,
            CreatedAt = DateTimeOffset.UtcNow
        };

        order.AddDomainEvent(new Events.Created(OrderId: order.Id, StoreId: order.StoreId));
        return order;
    }

    public ErrorOr<Order> Complete()
    {
        State = Order.OrderState.Complete;
        CompletedAt = DateTimeOffset.UtcNow;
        
        AddDomainEvent(new Events.Completed(OrderId: Id, StoreId: StoreId));
        AddDomainEvent(new Events.FinalizeInventory(OrderId: Id, StoreId: StoreId));
        
        if (HasPromotion && PromotionId.HasValue)
        {
            AddDomainEvent(new Promotion.Events.Used(PromotionId: PromotionId!.Value, OrderId: Id));
        }
        
        return this;
    }
}
```

### Handling Events

Events are handled by MediatR notification handlers:

```csharp
public class OrderCompletedEventHandler : INotificationHandler<Order.Events.Completed>
{
    private readonly IEmailSenderService _emailService;

    public async Task Handle(Order.Events.Completed notification, CancellationToken cancellationToken)
    {
        // Send order confirmation email
    }
}

public class InventoryFinalizeEventHandler : INotificationHandler<Order.Events.FinalizeInventory>
{
    private readonly IStockService _stockService;

    public async Task Handle(Order.Events.FinalizeInventory notification, CancellationToken cancellationToken)
    {
        // Decrease stock levels
    }
}
```

---

## Complete Example: Building an Order

### Step 1: Create an Order

```csharp
var orderResult = Order.Create(
    storeId: store.Id,
    currency: "USD",
    userId: userId,
    email: "customer@example.com"
);

if (orderResult.IsError)
    return BadRequest(orderResult.FirstError.Description);

var order = orderResult.Value;
```

### Step 2: Add Line Items

```csharp
var variant = await dbContext.Variants
    .Include(v => v.Product)
    .FirstOrDefaultAsync(v => v.Id == variantId);

var addItemResult = order.AddLineItem(variant, quantity: 2);
if (addItemResult.IsError)
    return BadRequest(addItemResult.FirstError.Description);
```

### Step 3: Set Addresses

```csharp
var shipResult = order.SetShippingAddress(shippingAddress);
if (shipResult.IsError)
    return BadRequest(shipResult.FirstError.Description);

var billResult = order.SetBillingAddress(billingAddress);
if (billResult.IsError)
    return BadRequest(billResult.FirstError.Description);
```

### Step 4: Transition State

```csharp
// Cart -> Address
var toAddressResult = order.Next();
if (toAddressResult.IsError)
    return BadRequest(toAddressResult.FirstError.Description);

// Address -> Delivery
var toDeliveryResult = order.Next();
if (toDeliveryResult.IsError)
    return BadRequest(toDeliveryResult.FirstError.Description);
```

### Step 5: Set Fulfillment & Shipping

```csharp
var location = await dbContext.StockLocations.FindAsync(locationId);
var setLocationResult = order.SetFulfillmentLocation(location);
if (setLocationResult.IsError)
    return BadRequest(setLocationResult.FirstError.Description);

var shippingMethod = await dbContext.ShippingMethods.FindAsync(shippingMethodId);
var shipMethodResult = order.SetShippingMethod(shippingMethod);
if (shipMethodResult.IsError)
    return BadRequest(shipMethodResult.FirstError.Description);
```

### Step 6: Apply Promotion

```csharp
var promotion = await dbContext.Promotions.FindAsync(promotionId);
var promoResult = order.ApplyPromotion(promotion, code: "SUMMER20");
if (promoResult.IsError)
    return BadRequest(promoResult.FirstError.Description);
```

### Step 7: Process Payment

```csharp
var paymentAmountCents = (long)(order.Total * 100);
var paymentResult = order.AddPayment(
    amountCents: paymentAmountCents,
    paymentMethodId: paymentMethod.Id,
    paymentMethodType: paymentMethod.Type
);
if (paymentResult.IsError)
    return BadRequest(paymentResult.FirstError.Description);

// Transition to Payment, Confirm, and Complete states
var toPaymentResult = order.Next();
var toConfirmResult = order.Next();
var completeResult = order.Next();

if (completeResult.IsError)
    return BadRequest(completeResult.FirstError.Description);

// Events are published when SaveChangesAsync is called
await dbContext.SaveChangesAsync();
```

---

## EF Core Configuration Guide

### Entity Type Configuration

```csharp
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        // Primitive properties
        builder.Property(o => o.Number)
            .IsRequired()
            .HasMaxLength(20);

        builder.Property(o => o.State)
            .HasConversion<int>();

        builder.Property(o => o.Currency)
            .IsRequired()
            .HasMaxLength(3);

        // Complex types
        builder.OwnsOne(o => o.PublicMetadata, mb => mb.ToJson());
        builder.OwnsOne(o => o.PrivateMetadata, mb => mb.ToJson());

        // Relationships
        builder.HasOne(o => o.Store)
            .WithMany()
            .HasForeignKey(o => o.StoreId)
            .IsRequired()
            .OnDelete(DeleteBehavior.Restrict);

        // Owned entities (composition)
        builder.OwnsMany(o => o.LineItems, lb =>
        {
            lb.HasKey("Id");
            lb.WithOwner().HasForeignKey("OrderId");
            lb.ToTable("OrderLineItems");
        });

        // Indexes
        builder.HasIndex(o => o.Number).IsUnique();
        builder.HasIndex(o => o.StoreId);

        // Concerns
        builder.ConfigureAuditableEntity();
    }
}
```

### Applying Concerns Configuration

```csharp
public class PromotionConfiguration : IEntityTypeConfiguration<Promotion>
{
    public void Configure(EntityTypeBuilder<Promotion> builder)
    {
        builder.HasKey(p => p.Id);

        // Apply concern configurations
        builder.ConfigureUniqueName();
        builder.ConfigureAuditableEntity();

        builder.Property(p => p.PromotionCode)
            .HasMaxLength(50);

        builder.Property(p => p.UsageCount)
            .HasDefaultValue(0);

        builder.HasIndex(p => p.Name).IsUnique();
    }
}
```

---

## Testing Best Practices

### Unit Testing Domain Models

```csharp
[TestFixture]
public class OrderTests
{
    private Order _order;
    private Variant _variant;

    [SetUp]
    public void Setup()
    {
        var orderResult = Order.Create(
            storeId: Guid.NewGuid(),
            currency: "USD"
        );
        _order = orderResult.Value;

        var variantResult = Variant.Create(
            productId: Guid.NewGuid(),
            isMaster: false
        );
        _variant = variantResult.Value;
    }

    [Test]
    public void AddLineItem_ValidVariantAndQuantity_AddsItem()
    {
        // Act
        var result = _order.AddLineItem(_variant, quantity: 2);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(_order.LineItems.Count, Is.EqualTo(1));
        Assert.That(_order.HasUncommittedEvents(), Is.True);
    }

    [Test]
    public void AddLineItem_ZeroQuantity_ReturnsError()
    {
        // Act
        var result = _order.AddLineItem(_variant, quantity: 0);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError.Code, Is.EqualTo("Validation.TooFew"));
    }

    [Test]
    public void Next_FromCart_TransitionsToAddress()
    {
        // Arrange
        _order.AddLineItem(_variant, 1);

        // Act
        var result = _order.Next();

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(_order.State, Is.EqualTo(Order.OrderState.Address));
    }
}
```

---

## 🏛️ Bounded Contexts Overview

The `Core.Domain` layer is composed of the following Bounded Contexts, each with its own specific responsibilities and core aggregates:

### 1. `Catalog.OptionTypes`
**Purpose**: Manages product option types (Size, Color, etc.) and their values
**Core Aggregate**: `OptionType` (manages `OptionValue`s)
**Example**:
```csharp
var optionType = await dbContext.OptionTypes
    .Include(ot => ot.OptionValues)
    .FirstOrDefaultAsync(ot => ot.Name == "Size");
// OptionValues: ["S", "M", "L", "XL"]
```

### 2. `Catalog.Products`
**Purpose**: Manages products with variants, images, properties, and categorization
**Core Aggregate**: `Product` (manages `Variant`s, `ProductImage`s, `ProductProperty`s)
**Key Features**:
- Status control (Draft, Active, Archived)
- Soft deletion support
- SEO metadata
- Parameterized names (Name + Presentation)
- Price tracking and variations

**Example**:
```csharp
var productResult = Product.Create(
    name: "Classic Blue T-Shirt",
    description: "High-quality cotton t-shirt",
    slug: "classic-blue-tshirt",
    metaTitle: "Blue T-Shirt - Premium Quality",
    metaDescription: "Shop our premium blue t-shirt...",
    isDigital: false
);

if (productResult.IsError) return BadRequest(productResult.FirstError.Description);

var product = productResult.Value;
product.Activate();
```

### 3. `Catalog.Properties`
**Purpose**: Defines generic product attributes/properties
**Core Aggregate**: `Property`

### 4. `Catalog.Taxonomies`
**Purpose**: Manages hierarchical product categorization
**Core Aggregates**: `Taxonomy` (manages `Taxon`s)
**Features**:
- Nested set model for hierarchy
- Rules for taxon eligibility
- Multiple levels (Department -> Category -> Subcategory)

### 5. `Identity.Users`
**Purpose**: User account management and authentication
**Core Aggregate**: `ApplicationUser`
**Manages**: Roles, claims, logins, tokens, addresses, orders

### 6. `Identity.Roles`
**Purpose**: Role-based access control
**Core Aggregate**: `Role` (manages claims)

### 7. `Identity.Permissions`
**Purpose**: Fine-grained permission definitions
**Core Entity**: `AccessPermission`

### 8. `Inventories`
**Purpose**: Stock tracking across locations
**Core Aggregates**: 
- `StockLocation`: Warehouse/fulfillment center
- `StockItem`: Individual product stock tracking
- `StockMovement`: Inventory transaction history
- `StockTransfer`: Inter-location transfers

**Example**:
```csharp
// Check stock availability
var stockItem = await dbContext.StockItems
    .FirstOrDefaultAsync(si => 
        si.VariantId == variantId && 
        si.StockLocationId == locationId);

if (stockItem?.QuantityOnHand > 0)
{
    // Product is in stock
}

// Adjust stock
var adjustResult = stockItem.Adjust(
    quantity: -quantity,
    originator: StockMovement.MovementOriginator.Order,
    reason: $"Order #{order.Number}"
);
```

### 9. `Orders`
**Purpose**: Complete order lifecycle management
**Core Aggregate**: `Order` (manages `LineItem`s, `Adjustment`s, `Shipment`s, `Payment`s)
**State Machine**: Cart → Address → Delivery → Payment → Confirm → Complete
**Key Features**:
- State transition validation
- Promotion application with calculation
- Address management
- Shipping method selection
- Payment tracking
- Metadata support

**See "Complete Example: Building an Order" for detailed usage**

### 10. `Promotions`
**Purpose**: Promotional offers and discount management
**Core Aggregates**: 
- `Promotion`: Root aggregate
- `PromotionRule`: Eligibility conditions
- `PromotionAction`: Discount calculation

**Example**:
```csharp
var action = new PromotionAction 
{ 
    Type = Promotion.PromotionType.OrderDiscount,
    DiscountType = Promotion.DiscountType.Percentage,
    Value = 20m
};

var promotionResult = Promotion.Create(
    name: "Summer Sale 20% Off",
    action: action,
    code: "SUMMER20",
    description: "20% discount on all orders",
    minimumOrderAmount: 50m,
    startsAt: DateTime.Now,
    expiresAt: DateTime.Now.AddDays(30),
    usageLimit: 1000,
    requiresCouponCode: true
);

var promotion = promotionResult.Value;

// Apply to order
var applyResult = order.ApplyPromotion(promotion, code: "SUMMER20");
```

### 11. `Payments`
**Purpose**: Payment method definitions
**Core Aggregate**: `PaymentMethod`
**Example**:
```csharp
var paymentMethod = await dbContext.PaymentMethods
    .FirstOrDefaultAsync(pm => pm.Id == paymentMethodId);

var payment = order.AddPayment(
    amountCents: (long)(order.Total * 100),
    paymentMethodId: paymentMethod.Id,
    paymentMethodType: paymentMethod.Type
);
```

### 12. `Shipping`
**Purpose**: Shipping method management
**Core Aggregate**: `ShippingMethod`
**Features**:
- Weight-based and flat-rate calculations
- Per-store shipping options

**Example**:
```csharp
var shippingMethod = await dbContext.ShippingMethods
    .FirstOrDefaultAsync(sm => sm.Id == shippingMethodId);

var cost = shippingMethod.CalculateCost(
    orderWeight: order.TotalWeight,
    orderTotal: order.ItemTotal
);

order.SetShippingMethod(shippingMethod);
```

### 13. `Stores`
**Purpose**: Store/channel configuration
**Core Aggregate**: `Store`
**Manages**: Store-specific products, payment methods, shipping methods

### 14. `Location`
**Purpose**: Geographical data
**Core Aggregate**: `Country` (manages `State`s)

---

## 🤝 Design Patterns Used

### 1. **Aggregate Pattern**
```csharp
// Only Order can be accessed directly
var order = dbContext.Orders.Find(orderId);

// LineItems, Adjustments are accessed through Order
foreach (var lineItem in order.LineItems)
{
    // Process line item
}

// Cannot access LineItem directly from DbContext
// var lineItem = dbContext.LineItems.Find(lineItemId); // ❌ Incorrect

// LineItems are managed exclusively by Order aggregate
var addResult = order.AddLineItem(variant, quantity);
var removeResult = order.RemoveLineItem(lineItemId);
```

### 2. **Factory Method Pattern**
```csharp
// Create with validation
var result = Order.Create(storeId, currency, userId);
if (result.IsError) return BadRequest(result.FirstError);

// Not: new Order() { ... } ❌
```

### 3. **ErrorOr Pattern (Railway-Oriented Programming)**
```csharp
// Chain operations and handle errors
var addResult = order.AddLineItem(variant, 2);
if (addResult.IsError) return BadRequest(addResult.FirstError);

var addressResult = order.SetShippingAddress(address);
if (addressResult.IsError) return BadRequest(addressResult.FirstError);

var nextResult = order.Next();
if (nextResult.IsError) return BadRequest(nextResult.FirstError);
```

### 4. **Domain Events Pattern**
```csharp
// Events are published within aggregates
public ErrorOr<Order> Complete()
{
    // ... validation ...
    
    State = Order.OrderState.Complete;
    AddDomainEvent(new Events.Completed(OrderId: Id, StoreId: StoreId));
    AddDomainEvent(new Events.FinalizeInventory(OrderId: Id, StoreId: StoreId));
    
    return this;
}

// Events are handled asynchronously by other systems
public class InventoryFinalizeEventHandler : INotificationHandler<Order.Events.FinalizeInventory>
{
    public async Task Handle(Order.Events.FinalizeInventory notification, CancellationToken ct)
    {
        // Reduce inventory
    }
}
```

### 5. **Concern Pattern (Reusable Behavior)**
```csharp
// Apply concerns to entities
public sealed class Product : Aggregate,
    IHasUniqueName,           // Ensures unique names
    IHasSlug,                 // Provides slug functionality
    IHasMetadata,             // Custom metadata storage
    IHasSeoMetadata,          // SEO fields
    ISoftDeletable,           // Soft delete support
    IHasAuditable             // Audit tracking (inherited)
{
}

// Concerns provide:
// - Helper extension methods
// - Validation rules
// - EF Core configuration
builder.ConfigureUniqueName<Product>();    // Index + validation
builder.ConfigureSoftDelete<Product>();    // Query filter + audit
builder.ConfigureAuditableEntity<Product>(); // Audit property config
```

---

## ✅ Key Takeaways

1. **Rich Domain Models**: Entities contain behavior, not just data
2. **Aggregates Encapsulate**: Only aggregate roots are accessed; owned entities are managed internally
3. **Factory Methods**: Creation logic is encapsulated with validation
4. **ErrorOr Pattern**: All operations return results that may contain errors
5. **Domain Events**: State changes are communicated asynchronously
6. **Concerns**: Reusable patterns are applied via interfaces and extensions
7. **Validation**: All constraints are centralized and reused
8. **Testing**: Domain logic is testable without infrastructure
9. **Persistence Ignorance**: Domain models don't know about databases
10. **Eventual Consistency**: Distributed operations maintain consistency through events

---

## 📚 Additional Resources

- **Individual Bounded Context READMEs**: Each context directory has detailed documentation
- **Test Examples**: See test files for concrete usage patterns
- **Configuration Examples**: Entity configurations in each context show EF Core setup
- **Domain Scenarios**: Look at application service implementations for real-world flows

---

**Last Updated**: 2024  
**ReSys.Shop v1.0**  
**Built with .NET 9**