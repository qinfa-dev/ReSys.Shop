# ReSys.Shop - AI Coding Agent Instructions

## Project Overview

ReSys.Shop is a **.NET 9** fashion e-commerce platform with visual similarity recommendations, Vue frontends, and a comprehensive multi-store system. The backend uses **Domain-Driven Design (DDD)** with CQRS, event-driven architecture, and strict separation of concerns.

**Tech Stack**: .NET 9.0.307 | Entity Framework Core 9.0.11 | MediatR 13.1.0 | FluentValidation 12.1.0 | Mapster 7.4.0 | PostgreSQL (pgvector 0.3.2) | Serilog 4.3.0 | Carter 9.0.0

---

## 🏛️ Architecture Essentials

### Clean Architecture Layers

```
ReSys.Shop.sln
├── src/ReSys.API/              ← HTTP entry point (controllers, middleware, Program.cs)
├── src/ReSys.Core/             ← Domain models + CQRS handlers (MediatR)
│   ├── Domain/                 ← Persistence-ignorant business logic (10 bounded contexts)
│   ├── Feature/                ← Commands, Queries, Handlers, Validators, Mappers
│   ├── Common/                 ← Shared constants, extensions, concerns, mediator interfaces
│   └── DependencyInjection.cs  ← Core services registration
└── src/ReSys.Infrastructure/   ← EF Core DbContext, repositories, external services
```

**Key principle**: Domain models (`ReSys.Core.Domain`) are **persistence-ignorant**. No EF, no APIs, no UI code in domain classes.

### Three-Layer Feature Architecture

Every feature flows through:
1. **Command/Query** (CQRS request with `ICommand<T>` or `IQuery<T>`)
2. **Validator** (FluentValidation rules, auto-discovered)
3. **Handler** (MediatR `ICommandHandler<TCommand, TResponse>` or `IQueryHandler<TQuery, TResponse>`)
4. **Mapper** (Mapster `IRegister` config, auto-discovered)

Example command flow:
```csharp
// User request → Command
public sealed record CreateProductCommand(string Name, string Slug) : ICommand<ProductResponse>;

// Validate input
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(Constraints.NameMaxLength);
        RuleFor(x => x.Slug).NotEmpty().Matches(Constraints.SlugRegex);
    }
}

// Handle business logic
public sealed class CreateProductHandler : ICommandHandler<CreateProductCommand, ProductResponse>
{
    private readonly IApplicationDbContext _dbContext;
    
    public async Task<ErrorOr<ProductResponse>> Handle(CreateProductCommand command, CancellationToken ct)
    {
        // Validation happens automatically via ValidationBehavior pipeline
        var productResult = Product.Create(command.Name, command.Slug);
        if (productResult.IsError) return productResult.FirstError;
        
        var product = productResult.Value;
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(ct);
        
        return product.Adapt<ProductResponse>();
    }
}
```

---

## 🎯 Domain Model Patterns (Critical)

### Aggregate Pattern - The Single Golden Rule

**ONLY aggregate roots** are queried directly from `DbContext`:
```csharp
// ✅ Correct: Query aggregate root
var order = await _dbContext.Orders.FindAsync(orderId);
var lineItems = order.LineItems; // Access children through root

// ❌ Wrong: Never query owned entities directly
var lineItem = await _dbContext.LineItems.FindAsync(lineItemId); // This doesn't exist!
```

All child modifications go through the aggregate root:
```csharp
// ✅ Correct: Modify through aggregate
var result = order.AddLineItem(variant, quantity: 2);
var removeResult = order.RemoveLineItem(lineItemId);

// ❌ Wrong: Direct child manipulation
order.LineItems.Add(new LineItem(...)); // Don't do this!
```

### ErrorOr Pattern - Railway-Oriented Programming

All domain operations return `ErrorOr<T>` (never throw):
```csharp
// Create with validation
var orderResult = Order.Create(storeId, currency, userId);
if (orderResult.IsError) return Problem(orderResult.FirstError.Description);

// Chain operations
var addResult = order.AddLineItem(variant, 2);
if (addResult.IsError) return Problem(addResult.FirstError.Description);

// Multiple sequential operations - check all results at end
var results = new[] { orderResult, addResult, ... };
var firstError = results.FirstOrDefault(r => r.IsError);
if (firstError?.IsError == true) return Problem(firstError.FirstError.Description);
```

### Factory Methods - Safe Creation

Always use factory methods (never `new` on aggregates):
```csharp
// ✅ Correct
var result = Product.Create(name: "T-Shirt", slug: "t-shirt");

// ❌ Wrong - bypasses validation
var product = new Product { Name = "T-Shirt" };
```

Factory methods encapsulate validation and initialization:
```csharp
public sealed class Product : Aggregate
{
    public static ErrorOr<Product> Create(
        string name,
        string slug,
        string? description = null)
    {
        if (string.IsNullOrWhiteSpace(name))
            return Errors.NameRequired;
        
        if (name.Length > Constraints.NameMaxLength)
            return Errors.NameTooLong;

        var product = new Product
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Slug = slug.ToSlug(),
            Description = description,
            CreatedAt = DateTimeOffset.UtcNow
        };

        product.AddDomainEvent(new Events.Created(product.Id));
        return product;
    }
}
```

### Domain Events - Decoupled Communication

Events signal important state changes. Publish from aggregates, consume via MediatR:

```csharp
// 1. Define event in aggregate (nested class)
public sealed class Order : Aggregate
{
    public static class Events
    {
        public sealed record Created(Guid OrderId, Guid StoreId) : DomainEvent;
        public sealed record Completed(Guid OrderId, Guid StoreId) : DomainEvent;
        public sealed record Canceled(Guid OrderId, string Reason) : DomainEvent;
    }
}

// 2. Raise event in aggregate method
public ErrorOr<Order> Complete()
{
    State = OrderState.Complete;
    CompletedAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.Completed(OrderId: Id, StoreId: StoreId));
    return this;
}

// 3. Handle event asynchronously (elsewhere in codebase)
public sealed class OrderCompletedEventHandler : IEventHandler<Order.Events.Completed>
{
    private readonly IEmailService _emailService;
    
    public async Task Handle(Order.Events.Completed notification, CancellationToken ct)
    {
        // Send confirmation email, update analytics, etc.
        await _emailService.SendOrderConfirmationAsync(notification.OrderId, ct);
    }
}

// Events are auto-published after SaveChangesAsync via EF Core interceptors
await _dbContext.SaveChangesAsync(ct);
```

---

## 📋 Bounded Contexts Quick Reference

The domain is split into **10 bounded contexts**, each in `src/ReSys.Core/Domain/{ContextName}/`:

| Context | Core Aggregate | Key Responsibility |
|---------|----------------|-------------------|
| **Catalog.Products** | `Product` | Product variants, images, properties, SEO |
| **Catalog.Taxonomies** | `Taxonomy` / `Taxon` | Hierarchical categories (nested set model) |
| **Catalog.OptionTypes** | `OptionType` | Size, Color, other product options |
| **Orders** | `Order` | Order lifecycle: Cart→Address→Delivery→Payment→Confirm→Complete |
| **Inventories** | `StockLocation`, `StockItem` | Stock tracking across warehouse locations |
| **Promotions** | `Promotion` | Discounts with rules and actions |
| **Payments** | `PaymentMethod` | Payment provider configuration |
| **Shipping** | `ShippingMethod` | Shipping options and cost calculation |
| **Stores** | `Store` | Multi-store/channel configuration |
| **Identity** | `ApplicationUser`, `Role` | Users, roles, permissions, authentication |

**Each context has a dedicated README** in `src/ReSys.Core/Domain/{Context}/README.md` with examples and patterns specific to that domain.

---

## 🔄 Order Domain - The Complex Case

Orders demonstrate the full pattern with state machine. Key states and transitions:

```
Cart (customer adds items)
  ↓ Next() 
Address (shipping & billing addresses required)
  ↓ Next()
Delivery (select fulfillment location & shipping method)
  ↓ Next()
Payment (payment authorization)
  ↓ Next()
Confirm (review final order)
  ↓ Next()
Complete (order finalized, inventory reduced)

OR at any point:
  → Canceled (releases inventory)
```

Example: complete checkout flow
```csharp
// 1. Create order
var orderResult = Order.Create(storeId: storeId, currency: "USD", userId: userId);
if (orderResult.IsError) return BadRequest(orderResult.FirstError);
var order = orderResult.Value;

// 2. Add items
var variant = await _dbContext.Variants.FindAsync(variantId);
var addResult = order.AddLineItem(variant, quantity: 2);
if (addResult.IsError) return BadRequest(addResult.FirstError);

// 3. Set addresses & transition
var shipResult = order.SetShippingAddress(address);
var toAddressResult = order.Next(); // Cart → Address

var toDeliveryResult = order.Next(); // Address → Delivery

// 4. Select shipping
var shippingMethod = await _dbContext.ShippingMethods.FindAsync(methodId);
order.SetShippingMethod(shippingMethod);
var toPaymentResult = order.Next(); // Delivery → Payment

// 5. Apply promotion (optional)
var promo = await _dbContext.Promotions.FindAsync(promoId);
var applyResult = order.ApplyPromotion(promo, code: "SUMMER20");

// 6. Process payment
var paymentResult = order.AddPayment(
    amountCents: (long)(order.Total * 100),
    paymentMethodId: paymentMethodId,
    paymentMethodType: PaymentMethodType.Card
);

// 7. Complete state transitions
var toConfirmResult = order.Next(); // Payment → Confirm
var completeResult = order.Next();  // Confirm → Complete

// 8. Save and events are published
await _dbContext.SaveChangesAsync(ct);
```

---

## 🎨 Concern Pattern - Reusable Entity Behaviors

Concerns implement cross-cutting functionality. Apply via interfaces found in `src/ReSys.Core/Common/Domain/Concerns/`:

```csharp
public sealed class Product : Aggregate,
    IHasUniqueName,          // Ensures name uniqueness (unique index + validation)
    IHasSlug,                // Auto-generate URL-friendly slugs
    IHasMetadata,            // Custom key-value metadata storage
    IHasSeoMetadata,         // Meta title/description/keywords
    ISoftDeletable,          // Soft delete with query filters
    IHasDisplayOn            // Frontend/backend visibility control
{
    public string Name { get; set; }
    public string Slug { get; set; }
    public IDictionary<string, object?> PublicMetadata { get; set; }
    public IDictionary<string, object?> PrivateMetadata { get; set; }
    // ... IHasSeoMetadata properties
}

// Configure in EF (in Persistence/Configurations/{Entity}Configuration.cs)
public void Configure(EntityTypeBuilder<Product> builder)
{
    builder.ConfigureUniqueName<Product>();      // Index + constraint
    builder.ConfigureSoftDelete<Product>();      // Query filter + audit
    builder.ConfigureDisplayOn();                 // Value conversions
}

// Use concern methods
if (product.AvailableOnFrontEnd()) { ... }
var slug = product.Name.ToSlug();
```

**Common Concerns** (found in `src/ReSys.Core/Common/Domain/Concerns/`):
- `IHasUniqueName` - enforces unique name index
- `IHasSlug` - URL-friendly identifiers  
- `IHasMetadata` - flexible key-value storage (PublicMetadata, PrivateMetadata)
- `ISoftDeletable` - logical deletion with DeletedAt, DeletedBy, IsDeleted
- `IHasDisplayOn` - multi-channel visibility (FrontEnd, BackEnd, Both, None)
- `IHasSeoMetadata` - SEO fields (MetaTitle, MetaDescription, MetaKeywords)
- `IHasAuditable` - auto-inherited (CreatedAt, UpdatedAt, CreatedBy, UpdatedBy)

---

## 🔐 Validation Best Practices

### Constraint Definitions (Centralized)

All limits in `src/ReSys.Core/Common/Constants/CommonInput.Constraints`:

```csharp
public static partial class CommonInput
{
    public static class Constraints
    {
        public static class Text
        {
            public const int MaxLength = 1000;
            public const int DescriptionMaxLength = 500;
        }
        public static class Email
        {
            public const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            public const int MaxLength = 256;
        }
        public static class SlugsAndVersions
        {
            public const string SlugPattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
            public static readonly Regex SlugRegex = new(SlugPattern, RegexOptions.Compiled);
        }
        // ... more
    }
}
```

### Validator Pattern

```csharp
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Name is required")
            .MaximumLength(CommonInput.Constraints.Text.MaxLength)
                .WithMessage($"Name cannot exceed {CommonInput.Constraints.Text.MaxLength} characters");

        RuleFor(x => x.Slug)
            .NotEmpty()
            .Matches(CommonInput.Constraints.SlugsAndVersions.SlugRegex)
                .WithMessage("Slug must be lowercase with hyphens only");
    }
}

// Validators auto-discovered and applied by ValidationBehavior in MediatR pipeline
// See: src/ReSys.Core/Feature/Common/Behaviors/ValidationBehavior.cs
```

---

## 🚀 Common Development Workflows

### Adding a New Feature

1. **Create Command or Query** in `src/ReSys.Core/Feature/{Context}/{Feature}/`
2. **Create Validator** next to command (auto-discovered by FluentValidation)
3. **Create Handler** implementing `ICommandHandler<T, TResponse>` or `IQueryHandler<T, TResponse>`
4. **Create Mapper** (if response DTO differs from domain model) - auto-discovered
5. **Dispatch from Controller or Endpoint** in `src/ReSys.API/`

Example: Create Product
```csharp
// 1. Command definition (src/ReSys.Core/Feature/Catalog/Products/Commands/CreateProductCommand.cs)
public sealed record CreateProductCommand(
    string Name,
    string Slug,
    string? Description = null
) : ICommand<ProductResponse>;

// 2. Validator (auto-discovered)
public sealed class CreateProductValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductValidator()
    {
        RuleFor(x => x.Name).NotEmpty().MaximumLength(200);
        RuleFor(x => x.Slug).NotEmpty().Matches(CommonInput.Constraints.SlugsAndVersions.SlugRegex);
    }
}

// 3. Handler (src/ReSys.Core/Feature/Catalog/Products/Commands/CreateProductHandler.cs)
public sealed class CreateProductHandler : ICommandHandler<CreateProductCommand, ProductResponse>
{
    private readonly IApplicationDbContext _dbContext;

    public async Task<ErrorOr<ProductResponse>> Handle(CreateProductCommand command, CancellationToken ct)
    {
        var result = Product.Create(command.Name, command.Slug, command.Description);
        if (result.IsError) return result.FirstError;

        var product = result.Value;
        _dbContext.Products.Add(product);
        await _dbContext.SaveChangesAsync(ct);

        return product.Adapt<ProductResponse>();
    }
}

// 4. Mapper configuration (auto-discovered)
public sealed class ProductMappingConfig : IRegister
{
    public void Register(TypeAdapterConfig config)
    {
        config.NewConfig<Product, ProductResponse>()
            .Map(dest => dest.Id, src => src.Id)
            .Map(dest => dest.Name, src => src.Name)
            .Map(dest => dest.Slug, src => src.Slug);
    }
}

// 5. Controller/Endpoint (src/ReSys.API/Controllers/)
[ApiController]
[Route("api/products")]
public class ProductsController(ISender sender) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateProductCommand command, CancellationToken ct)
    {
        var result = await sender.Send(command, ct);
        return result.Match(
            onValue: product => CreatedAtAction(nameof(Get), new { id = product.Id }, product),
            onError: errors => Problem(errors.First().Description)
        );
    }
}
```

### Running & Building

```powershell
# Build solution
dotnet build

# Run API (default: https://localhost:5001)
dotnet run --project src/ReSys.API

# Watch mode for TDD (rebuilds on file changes)
dotnet watch --project src/ReSys.API

# Run tests
dotnet test

# Run specific test
dotnet test --filter "ProductTests"

# With coverage
dotnet test /p:CollectCoverage=true
```

### Database Migrations

```powershell
# Create migration
dotnet ef migrations add {MigrationName} --project src/ReSys.Infrastructure --startup-project src/ReSys.API

# Apply migrations
dotnet ef database update --project src/ReSys.Infrastructure --startup-project src/ReSys.API

# Drop database (careful!)
dotnet ef database drop --project src/ReSys.Infrastructure --startup-project src/ReSys.API
```

### Project Structure for New Features

```
src/ReSys.Core/
├── Feature/Catalog/Products/            ← Feature folder (context/domain)
│   ├── Commands/
│   │   ├── CreateProductCommand.cs
│   │   └── CreateProductHandler.cs
│   ├── Queries/
│   │   ├── GetProductQuery.cs
│   │   └── GetProductHandler.cs
│   ├── CreateProductValidator.cs        ← Validator
│   └── ProductMappingConfig.cs          ← Mapper
└── Domain/Catalog/Products/
    ├── Product.cs                       ← Aggregate root
    └── README.md                        ← Domain documentation
```

---

## 📊 Key Files Reference

| Purpose | Location |
|---------|----------|
| Domain models (aggregates, entities, VOs) | `src/ReSys.Core/Domain/{Context}/{Aggregate}` |
| CQRS commands/queries | `src/ReSys.Core/Feature/{Context}/{Feature}/{Command\|Query}.cs` |
| Command/query handlers | `src/ReSys.Core/Feature/{Context}/{Feature}/{Handler}.cs` |
| Validators | `src/ReSys.Core/Feature/{Context}/{Feature}/{Command\|Query}Validator.cs` |
| Mappers/Configurations | `src/ReSys.Core/Feature/{Context}/{Feature}MappingConfig.cs` |
| Shared constants & constraints | `src/ReSys.Core/Common/Constants/CommonInput.cs` |
| Shared error definitions | `src/ReSys.Core/Common/Constants/CommonInput.Errors.cs` |
| Domain concerns (interfaces) | `src/ReSys.Core/Common/Domain/Concerns/` |
| Mediator interfaces | `src/ReSys.Core/Common/Domain/Mediators/` (ICommand, IQuery, etc.) |
| HTTP controllers | `src/ReSys.API/Controllers/{Context}Controller.cs` |
| Dependency injection setup | `src/ReSys.Core/DependencyInjection.cs` |
| Configuration | `src/ReSys.API/appsettings.json` |

---

## ⚠️ Common Pitfalls to Avoid

| Pitfall | ❌ Wrong | ✅ Right |
|---------|----------|----------|
| **Owned entity access** | `dbContext.LineItems.Find(id)` | `order.LineItems` through Order aggregate |
| **Bypassing validation** | `new Product { Name = "..." }` | `Product.Create(name, ...)` |
| **Direct mutation** | `lineItem.Quantity = 5` | `lineItem.UpdateQuantity(5)` |
| **Async exceptions** | `throw new Exception(...)` | `return Error.Validation(...)` |
| **Logic in mappers** | Database queries in IRegister | Extract to domain service or handler |
| **Event handling** | Sync event consumption | Async handlers via IEventHandler |
| **Constraint duplication** | Hardcoded 200, 500, etc | Use CommonInput.Constraints |
| **No validation** | Accepting commands directly | Always use FluentValidation + ValidationBehavior |
| **Skipping error checks** | `order.AddItem(item)` then use order | Always check `if (result.IsError)` |
| **Querying child entities** | `dbContext.Variants.Where(...)` | Load parent aggregate and access children |

---

## 🔗 Integration Points

### ReSys.Infrastructure Responsibilities

- **DbContext**: EF Core configuration, query builders, migrations
- **Repositories**: Query abstractions (many features query directly from handlers)
- **External Services**: Email, payment processors, file uploads, SMS
- **DependencyInjection.cs**: Register infrastructure services, logging setup, caching

### ReSys.API Responsibilities

- **Controllers**: HTTP request → Command/Query dispatch via MediatR `ISender`
- **Program.cs**: Service registration, middleware setup, authentication, authorization
- **Middleware**: Logging, error handling, CORS, request/response validation
- **DTOs**: Separate from domain models (use Mapster for mapping)

### Database

- **PostgreSQL** with pgvector support (for visual similarity search)
- **Migrations**: `dotnet ef migrations add` → `dotnet ef database update`
- **Query Filters**: Soft-deletable entities auto-excluded (configured in EF)
- **Domain Events**: Auto-published after SaveChangesAsync via EF Core interceptors

---

## 🧪 Testing Philosophy

- **Unit tests** target domain models (aggregates, value objects) - no database needed
- **Integration tests** verify command handlers + database interactions
- **No mocking domain models** - they should be simple enough to instantiate directly
- Use `ErrorOr<T>` assertions: `Assert.That(result.IsError, Is.False)`
- Test location: `tests/` folder (project structure mirrors `src/`)

Example domain unit test:
```csharp
[TestFixture]
public class OrderTests
{
    [Test]
    public void AddLineItem_WithValidVariantAndQuantity_SucceedsAndRaisesEvent()
    {
        // Arrange
        var orderResult = Order.Create(Guid.NewGuid(), "USD");
        var order = orderResult.Value;
        var variant = Variant.Create(Guid.NewGuid(), isMaster: false).Value;

        // Act
        var result = order.AddLineItem(variant, quantity: 2);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(order.LineItems.Count, Is.EqualTo(1));
        Assert.That(order.HasUncommittedEvents(), Is.True);
    }
}
```

---

## 📌 Key Dependencies & Versions

Managed centrally in `Directory.Packages.props`:

| Dependency | Version | Purpose |
|-----------|---------|---------|
| **.NET SDK** | 9.0.307 | Target framework (see `global.json`) |
| **MediatR** | 13.1.0 | CQRS pattern implementation |
| **FluentValidation** | 12.1.0 | Command/query validation rules |
| **Mapster** | 7.4.0 | Object mapping (faster than AutoMapper) |
| **ErrorOr** | 2.0.1 | Railway-oriented error handling |
| **Entity Framework Core** | 9.0.11 | ORM and database migrations |
| **Npgsql** | 9.0.4 | PostgreSQL provider for EF Core |
| **pgvector** | 0.3.2 | Vector similarity for ML recommendations |
| **Carter** | 9.0.0 | Lightweight routing (alternative to controllers) |
| **Serilog** | 4.3.0 | Structured logging |

Check `Directory.Packages.props` for complete list and transitive versions.

---

## 🤝 When to Ask for Clarification

- Unclear which aggregate owns a behavior
- Need to determine if new feature requires new bounded context
- Deciding between command vs. query vs. domain service
- Determining if domain event is needed
- Questions about multi-store isolation or permission boundaries
- Unsure about which concern interfaces to apply to an entity

This codebase values **explicit domain modeling** over generic patterns—prefer clarity over cleverness.

---

## 🚀 Development Quick Start

```powershell
# 1. Clone and navigate
git clone <repo>
cd ReSys.Shop

# 2. Check .NET version
dotnet --version  # Should be 9.0.307

# 3. Build solution
dotnet build

# 4. Run API (starts on https://localhost:5001)
dotnet run --project src/ReSys.API

# 5. Open browser
# API docs available at https://localhost:5001/openapi/v1.json
```

Ensure PostgreSQL is running with pgvector extension for full functionality.
