# ReSys.Shop Domain Layer - Quick Reference

## ?? Creating an Aggregate in 5 Minutes

### Template

```csharp
// File: src/ReSys.Core/Domain/YourContext/YourEntity.cs
using ErrorOr;
using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;

namespace ReSys.Core.Domain.YourContext;

public sealed class YourEntity : Aggregate, IHasUniqueName, IHasMetadata
{
    public static class Constraints
    {
        public const int NameMaxLength = 100;
    }

    public static class Errors
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound("YourEntity.NotFound", $"Not found: {id}");
        public static Error NameRequired => 
            CommonInput.Errors.Required(field: nameof(Name));
    }

    #region Properties
    public string Name { get; set; } = string.Empty;
    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }
    #endregion

    #region Factory
    public static ErrorOr<YourEntity> Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name)) return Errors.NameRequired;
        
        var entity = new YourEntity
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        
        entity.AddDomainEvent(new Events.Created(entity.Id, entity.Name));
        return entity;
    }
    #endregion

    #region Business Logic
    public ErrorOr<YourEntity> Update(string? name = null)
    {
        if (name != null && name != Name)
        {
            Name = name.Trim();
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.Updated(Id));
        }
        return this;
    }
    #endregion

    #region Events
    public static class Events
    {
        public sealed record Created(Guid EntityId, string Name) : DomainEvent;
        public sealed record Updated(Guid EntityId) : DomainEvent;
    }
    #endregion
}
```

---

## ?? Domain Concerns Quick Lookup

```csharp
// Add to aggregate declaration:
public sealed class MyEntity : Aggregate,
    IHasUniqueName,          // Unique name constraint
    IHasSlug,                // URL slug support
    IHasMetadata,            // Public/Private metadata
    IHasSeoMetadata,         // Meta title/desc/keywords
    ISoftDeletable,          // Soft delete (DeletedAt, IsDeleted)
    IHasDisplayOn,           // Frontend/Backend visibility
    IHasParameterizableName  // Name + Presentation
{
}

// Configure in EF Core:
builder.ConfigureUniqueName<MyEntity>();
builder.ConfigureSoftDelete<MyEntity>();
builder.ConfigureDisplayOn<MyEntity>();
builder.ConfigureAuditableEntity<MyEntity>();
```

---

## ?? Standard Error Messages

```csharp
// Use these patterns:
Error.NotFound("Code", "Entity not found");
Error.Validation("Code", "Invalid input");
Error.Conflict("Code", "Already exists");
Error.Failure("Code", "Operation failed");

// Or use CommonInput.Errors helpers:
CommonInput.Errors.Required(field: "Name");
CommonInput.Errors.TooLong(field: "Name", maxLength: 100);
CommonInput.Errors.TooFewItems(field: "Quantity", min: 1);
CommonInput.Errors.Null("SomethingRequired");
```

---

## ?? State Machine Pattern

```csharp
public sealed class Order : Aggregate
{
    public enum OrderState { Cart, Address, Delivery, Payment, Confirm, Complete, Canceled }
    public OrderState State { get; set; } = OrderState.Cart;

    public ErrorOr<Order> Next()
    {
        return State switch
        {
            OrderState.Cart => ToAddress(),
            OrderState.Address => ToDelivery(),
            OrderState.Delivery => ToPayment(),
            OrderState.Payment => ToConfirm(),
            OrderState.Confirm => Complete(),
            _ => Errors.InvalidStateTransition(State, State + 1)
        };
    }

    private ErrorOr<Order> ToAddress()
    {
        if (!LineItems.Any()) return Error.Validation("EmptyCart", "Cannot checkout empty cart");
        State = OrderState.Address;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.StateChanged(Id, OrderState.Address));
        return this;
    }
}
```

---

## ?? ErrorOr Pattern

```csharp
// Create aggregate
var result = MyEntity.Create("Test");
if (result.IsError) return BadRequest(result.FirstError.Description);
var entity = result.Value;

// Update aggregate
var updateResult = entity.Update("New Name");
if (updateResult.IsError) return BadRequest(updateResult.FirstError);

// Chain operations
var result1 = entity.DoSomething();
if (result1.IsError) return BadRequest(result1.FirstError);

var result2 = entity.DoSomethingElse();
if (result2.IsError) return BadRequest(result2.FirstError);
```

---

## ??? EF Core Configuration Template

```csharp
public class MyEntityConfiguration : IEntityTypeConfiguration<MyEntity>
{
    public void Configure(EntityTypeBuilder<MyEntity> builder)
    {
        builder.HasKey(x => x.Id);

        // Properties
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(100);

        // Concerns
        builder.ConfigureUniqueName<MyEntity>();
        builder.ConfigureAuditableEntity<MyEntity>();
        builder.ConfigureSoftDelete<MyEntity>();

        // Metadata
        builder.OwnsOne(x => x.PublicMetadata, mb => mb.ToJson());
        builder.OwnsOne(x => x.PrivateMetadata, mb => mb.ToJson());

        // Relationships
        builder.HasOne(x => x.Parent)
            .WithMany()
            .HasForeignKey("ParentId")
            .OnDelete(DeleteBehavior.Restrict);

        // Owned entities
        builder.OwnsMany(x => x.Children, cb =>
        {
            cb.HasKey("Id");
            cb.WithOwner().HasForeignKey("ParentId");
            cb.ToTable("MyEntityChildren");
        });

        // Indexes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.CreatedAt);

        builder.ToTable("MyEntities", schema: "dbo");
    }
}
```

---

## ?? Domain Events Pattern

```csharp
// Define events
public static class Events
{
    public sealed record Created(Guid EntityId) : DomainEvent;
    public sealed record Updated(Guid EntityId) : DomainEvent;
    public sealed record Deleted(Guid EntityId) : DomainEvent;
}

// Publish events
public static ErrorOr<MyEntity> Create(string name)
{
    var entity = new MyEntity { ... };
    entity.AddDomainEvent(new Events.Created(entity.Id));
    return entity;
}

// Handle events
public class MyEntityCreatedHandler : INotificationHandler<MyEntity.Events.Created>
{
    public async Task Handle(MyEntity.Events.Created notification, CancellationToken ct)
    {
        // React to creation
    }
}
```

---

## ?? Test Template

```csharp
[TestFixture]
public class MyEntityTests
{
    [Test]
    public void Create_WithValidData_ReturnsEntity()
    {
        var result = MyEntity.Create("Test");
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.Name, Is.EqualTo("Test"));
    }

    [Test]
    public void Create_WithoutName_ReturnsError()
    {
        var result = MyEntity.Create("");
        Assert.That(result.IsError, Is.True);
    }

    [Test]
    public void Update_PublishesEvent()
    {
        var entity = MyEntity.Create("Test").Value;
        entity.Update("Updated");
        
        Assert.That(entity.DomainEvents, 
            Has.Some.TypeOf<MyEntity.Events.Updated>());
    }
}
```

---

## ?? Folder Structure

```
src/ReSys.Core/Domain/
??? YourContext/
?   ??? YourEntity.cs                    # Aggregate root
?   ??? YourEntityConfiguration.cs       # EF Core config
?   ??? YourEntityValidator.cs          # Fluent validation
?   ??? ChildEntity.cs                  # Owned entity (if any)
?   ??? YourDomainService.cs            # Domain service (if needed)
?   ??? README.md                        # Bounded context docs
??? Common/
?   ??? Domain/
?   ?   ??? Concerns/                   # Shared interfaces
?   ?   ??? Entities/                   # Base classes
?   ?   ??? Events/                     # Event infrastructure
?   ??? Constants/
?       ??? CommonInput.*.cs            # Constraints, errors
??? README.md                            # Main domain guide
```

---

## ? Pre-Commit Checklist

- [ ] Aggregate inherits from `Aggregate`
- [ ] Implements at least one concern interface
- [ ] All public methods return `ErrorOr<T>`
- [ ] Has `Constraints` and `Errors` classes
- [ ] Has `Events` class with domain events
- [ ] Domain events published at key points
- [ ] EF Core configuration implemented
- [ ] Concerns configured in EF Core
- [ ] Owned entities use `OwnsMany()` or `OwnsOne()`
- [ ] Relationships configured with `DeleteBehavior`
- [ ] Indexes defined for query columns
- [ ] Unit tests written
- [ ] README.md created for context

---

## ?? Key Files

```
Constraints:    src/ReSys.Core/Common/Constants/CommonInput.Constraints.cs
Errors:         src/ReSys.Core/Common/Constants/CommonInput.Errors.cs
Concerns:       src/ReSys.Core/Common/Domain/Concerns/
Base Classes:   src/ReSys.Core/Common/Domain/Entities/
Events:         src/ReSys.Core/Common/Domain/Events/
Full Guide:     src/ReSys.Core/Domain/README.md
Setup Guide:    src/ReSys.Core/DOMAIN_SETUP_GUIDE.md
```

---

## ?? Examples in Codebase

Look at these files for real-world examples:

- **Order** (Complex aggregate):
  - `src/ReSys.Core/Domain/Orders/Order.cs`
  - `src/ReSys.Core/Domain/Orders/OrderConfiguration.cs`

- **Product** (With metadata & soft delete):
  - `src/ReSys.Core/Domain/Catalog/Products/Product.cs`
  - `src/ReSys.Core/Domain/Catalog/Products/ProductConfiguration.cs`

- **Promotion** (With rules & state):
  - `src/ReSys.Core/Domain/Promotions/Promotions/Promotion.cs`
  - `src/ReSys.Core/Domain/Promotions/Promotions/PromotionConfiguration.cs`

---

**Quick Help**: `Ctrl+F` in IDE to search:
- "IHasUniqueName" ? See concern usage
- "ConfigureUniqueName" ? See EF Core configuration
- "ErrorOr<" ? See error handling pattern
- "AddDomainEvent" ? See event publishing

---

**Last Updated**: 2024  
**Built with .NET 9**
