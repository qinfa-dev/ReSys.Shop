# ReSys.Shop Domain Layer - Complete Setup & Reference Guide

## ?? Overview

This guide provides step-by-step instructions for understanding, setting up, and extending domain models in the ReSys.Shop Core.Domain layer. It covers:

- ? Domain architecture and patterns
- ? How to create new aggregates
- ? Applying domain concerns
- ? Setting up EF Core configurations
- ? Creating domain events
- ? Error handling patterns
- ? Testing domain logic

---

## ?? Quick Start: Creating a New Domain Model

### Step 1: Define the Aggregate Structure

```csharp
// File: src/ReSys.Core/Domain/YourContext/YourAggregate.cs

using ErrorOr;
using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;

namespace ReSys.Core.Domain.YourContext;

/// <summary>
/// Brief description of your aggregate's purpose
/// </summary>
public sealed class YourAggregate : Aggregate, 
    IHasUniqueName,      // Apply concerns that fit your domain
    IHasMetadata,
    ISoftDeletable
{
    #region Constraints
    /// <summary>
    /// Business constraints for this aggregate
    /// </summary>
    public static class Constraints
    {
        public const int NameMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
        public const int DescriptionMaxLength = CommonInput.Constraints.Text.LongTextMaxLength;
        public const int MinValue = 1;
        public const int MaxValue = 1000;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Domain-specific errors for this aggregate
    /// </summary>
    public static class Errors
    {
        public static Error NotFound(Guid id) => 
            Error.NotFound(code: "YourAggregate.NotFound", 
                description: $"YourAggregate with ID '{id}' was not found.");

        public static Error NameRequired => 
            CommonInput.Errors.Required(prefix: nameof(YourAggregate), field: nameof(Name));

        public static Error NameTooLong => 
            CommonInput.Errors.TooLong(prefix: nameof(YourAggregate), 
                field: nameof(Name), 
                maxLength: Constraints.NameMaxLength);

        public static Error InvalidStatus => 
            Error.Validation(code: "YourAggregate.InvalidStatus", 
                description: "Invalid status provided.");

        public static Error CannotPerformAction => 
            Error.Validation(code: "YourAggregate.CannotPerform", 
                description: "Cannot perform action in current state.");
    }
    #endregion

    #region Properties
    // Inherited from Aggregate:
    // public Guid Id { get; }
    // public DateTimeOffset CreatedAt { get; set; }
    // public DateTimeOffset? UpdatedAt { get; set; }
    // public string? CreatedBy { get; set; }
    // public string? UpdatedBy { get; set; }
    // public long Version { get; set; }

    // From IHasUniqueName
    public string Name { get; set; } = string.Empty;

    // From IHasMetadata
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();

    // From ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    // Domain-specific properties
    public string? Description { get; set; }
    public bool IsActive { get; set; } = true;
    public int Value { get; set; }
    #endregion

    #region Relationships
    // Define relationships to other aggregates by ID only
    // Example: public Guid StoreId { get; set; }
    
    // For owned entities, use collections:
    // public ICollection<ChildEntity> Children { get; set; } = new List<ChildEntity>();
    #endregion

    #region Computed Properties
    // Add read-only properties that derive from stored data
    // Example:
    public bool CanActivate => IsDeleted == false && IsActive == false;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for EF Core
    /// </summary>
    private YourAggregate() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Factory method to create a new aggregate with validation
    /// </summary>
    public static ErrorOr<YourAggregate> Create(
        string name,
        string? description = null,
        int value = 0)
    {
        // Validate inputs
        if (string.IsNullOrWhiteSpace(name))
            return Errors.NameRequired;

        if (name.Length > Constraints.NameMaxLength)
            return Errors.NameTooLong;

        if (value < 1 || value > Constraints.MaxValue)
            return Error.Validation(code: "YourAggregate.InvalidValue", 
                description: $"Value must be between 1 and {Constraints.MaxValue}");

        // Create aggregate
        var aggregate = new YourAggregate
        {
            Id = Guid.NewGuid(),
            Name = name.Trim(),
            Description = description?.Trim(),
            Value = value,
            IsActive = true,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Publish creation event
        aggregate.AddDomainEvent(new Events.Created(YourAggregateId: aggregate.Id, Name: aggregate.Name));

        return aggregate;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Example business logic method
    /// </summary>
    public ErrorOr<YourAggregate> Update(string? name = null, string? description = null)
    {
        bool changed = false;

        if (name != null && name != Name)
        {
            if (name.Length > Constraints.NameMaxLength)
                return Errors.NameTooLong;

            Name = name.Trim();
            changed = true;
        }

        if (description != null && description != Description)
        {
            if (description.Length > Constraints.DescriptionMaxLength)
                return Error.Validation(code: "YourAggregate.DescriptionTooLong", 
                    description: $"Description cannot exceed {Constraints.DescriptionMaxLength} characters");

            Description = description.Trim();
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            AddDomainEvent(new Events.Updated(YourAggregateId: Id));
        }

        return this;
    }

    /// <summary>
    /// Example state change method
    /// </summary>
    public ErrorOr<YourAggregate> Activate()
    {
        if (IsActive)
            return this; // Already active

        if (IsDeleted)
            return Error.Conflict(code: "YourAggregate.DeletedCannotActivate", 
                description: "Cannot activate a deleted aggregate");

        IsActive = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Activated(YourAggregateId: Id));

        return this;
    }

    /// <summary>
    /// Example soft delete
    /// </summary>
    public ErrorOr<Deleted> Delete()
    {
        if (IsDeleted)
            return Result.Deleted; // Already deleted

        DeletedAt = DateTimeOffset.UtcNow;
        IsDeleted = true;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.Deleted(YourAggregateId: Id));

        return Result.Deleted;
    }
    #endregion

    #region Events
    /// <summary>
    /// Domain events published by this aggregate
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Raised when a new aggregate is created
        /// </summary>
        public sealed record Created(Guid YourAggregateId, string Name) : DomainEvent;

        /// <summary>
        /// Raised when the aggregate is updated
        /// </summary>
        public sealed record Updated(Guid YourAggregateId) : DomainEvent;

        /// <summary>
        /// Raised when the aggregate is activated
        /// </summary>
        public sealed record Activated(Guid YourAggregateId) : DomainEvent;

        /// <summary>
        /// Raised when the aggregate is deleted
        /// </summary>
        public sealed record Deleted(Guid YourAggregateId) : DomainEvent;
    }
    #endregion
}
```

### Step 2: Create EF Core Configuration

```csharp
// File: src/ReSys.Core/Domain/YourContext/YourAggregateConfiguration.cs

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReSys.Core.Domain.YourContext;

public class YourAggregateConfiguration : IEntityTypeConfiguration<YourAggregate>
{
    public void Configure(EntityTypeBuilder<YourAggregate> builder)
    {
        // Primary key
        builder.HasKey(x => x.Id);

        // Primitive properties
        builder.Property(x => x.Name)
            .IsRequired()
            .HasMaxLength(YourAggregate.Constraints.NameMaxLength)
            .HasComment("The unique name of the aggregate");

        builder.Property(x => x.Description)
            .HasMaxLength(YourAggregate.Constraints.DescriptionMaxLength)
            .HasComment("Description or summary");

        builder.Property(x => x.Value)
            .HasDefaultValue(0)
            .HasComment("A numeric value");

        builder.Property(x => x.IsActive)
            .HasDefaultValue(true)
            .HasComment("Whether this aggregate is active");

        // Complex types
        builder.OwnsOne(x => x.PublicMetadata, mb => mb.ToJson());
        builder.OwnsOne(x => x.PrivateMetadata, mb => mb.ToJson());

        // Configure concerns
        builder.ConfigureUniqueName<YourAggregate>();     // IHasUniqueName
        builder.ConfigureAuditableEntity<YourAggregate>(); // IHasAuditable
        builder.ConfigureSoftDelete<YourAggregate>();     // ISoftDeletable

        // Indexes
        builder.HasIndex(x => x.Name).IsUnique();
        builder.HasIndex(x => x.IsActive);
        builder.HasIndex(x => x.CreatedAt);

        // Relationships (if any)
        // builder.HasOne(x => x.Parent)
        //     .WithMany()
        //     .HasForeignKey("ParentId")
        //     .OnDelete(DeleteBehavior.Restrict);

        // Owned entities (if any)
        // builder.OwnsMany(x => x.Children, cb =>
        // {
        //     cb.HasKey("Id");
        //     cb.WithOwner().HasForeignKey("YourAggregateId");
        //     cb.ToTable("YourAggregateChildren");
        // });

        // Table name (if not convention)
        builder.ToTable("YourAggregates", schema: "dbo");

        // Comments
        builder.HasComment("Aggregate root for the YourContext bounded context");
    }
}
```

### Step 3: Define Fluent Validation (Optional)

```csharp
// File: src/ReSys.Core/Domain/YourContext/YourAggregateValidator.cs

using FluentValidation;
using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Domain.YourContext;

public class YourAggregateValidator : AbstractValidator<YourAggregate>
{
    public YourAggregateValidator()
    {
        // Configure validation rules
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithErrorCode(CommonInput.Errors.Required(field: nameof(YourAggregate.Name)).Code)
            .WithMessage(CommonInput.Errors.Required(field: nameof(YourAggregate.Name)).Description)
            .MaximumLength(YourAggregate.Constraints.NameMaxLength)
            .WithErrorCode(CommonInput.Errors.TooLong(field: nameof(YourAggregate.Name), 
                maxLength: YourAggregate.Constraints.NameMaxLength).Code)
            .WithMessage(CommonInput.Errors.TooLong(field: nameof(YourAggregate.Name), 
                maxLength: YourAggregate.Constraints.NameMaxLength).Description);

        RuleFor(x => x.Description)
            .MaximumLength(YourAggregate.Constraints.DescriptionMaxLength)
            .WithErrorCode(CommonInput.Errors.TooLong(field: nameof(YourAggregate.Description), 
                maxLength: YourAggregate.Constraints.DescriptionMaxLength).Code)
            .WithMessage(CommonInput.Errors.TooLong(field: nameof(YourAggregate.Description), 
                maxLength: YourAggregate.Constraints.DescriptionMaxLength).Description)
            .When(x => !string.IsNullOrEmpty(x.Description));

        // Apply concern validation rules
        this.AddUniqueNameRules<YourAggregate>();
    }
}
```

### Step 4: Register in DbContext

```csharp
// In ApplicationDbContext.cs

public class ApplicationDbContext : DbContext
{
    // ... existing code ...

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ... existing configurations ...

        // Add your configuration
        modelBuilder.ApplyConfiguration(new YourAggregateConfiguration());

        base.OnModelCreating(modelBuilder);
    }

    public DbSet<YourAggregate> YourAggregates { get; set; }
}
```

### Step 5: Create Domain Event Handler

```csharp
// File: src/ReSys.API/Handlers/YourAggregateCreatedEventHandler.cs

using MediatR;
using ReSys.Core.Domain.YourContext;

namespace ReSys.API.Handlers;

/// <summary>
/// Handles YourAggregate.Events.Created domain event
/// </summary>
public class YourAggregateCreatedEventHandler : INotificationHandler<YourAggregate.Events.Created>
{
    private readonly ILogger<YourAggregateCreatedEventHandler> _logger;

    public YourAggregateCreatedEventHandler(ILogger<YourAggregateCreatedEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(YourAggregate.Events.Created notification, CancellationToken cancellationToken)
    {
        _logger.LogInformation("YourAggregate '{AggregateId}' created with name '{Name}'", 
            notification.YourAggregateId, notification.Name);

        // Perform any additional actions
        // - Send notifications
        // - Update read models
        // - Trigger external systems
        // - Create audit entries

        await Task.CompletedTask;
    }
}
```

---

## ?? Applying Domain Concerns

### Complete Concern Reference

| Concern | Purpose | Provides |
|---------|---------|----------|
| `IHasDisplayOn` | Visibility control | `AvailableOnFrontEnd()`, `AvailableOnBackEnd()` |
| `IHasUniqueName` | Unique name constraint | Unique index, validation, async uniqueness check |
| `IHasSlug` | URL-friendly slugs | Slug validation, generation helper |
| `IHasMetadata` | Custom metadata storage | Public/Private metadata dictionaries |
| `IHasAuditable` | Audit tracking | CreatedAt, UpdatedAt, CreatedBy, UpdatedBy |
| `ISoftDeletable` | Soft delete support | DeletedAt, DeletedBy, IsDeleted, query filter |
| `IHasSeoMetadata` | SEO fields | MetaTitle, MetaDescription, MetaKeywords |
| `IHasParameterizableName` | Dual name storage | Name + Presentation with normalization |
| `IHasCreator` | Creator tracking | CreatedBy |
| `IHasUpdater` | Updater tracking | UpdatedBy |
| `IHasVersion` | Version control | Version for optimistic concurrency |
| `IHasPosition` | Ordering | Position for sequencing |
| `IHasIdentity` | Identity info | UserId or similar |
| `IHasAssignable` | Assignment support | AssignedTo, AssignedAt |
| `IAsset` | Asset/file tracking | Asset metadata |
| `IHasFilterParam` | Filter parameters | Filter-related properties |

### Applying Multiple Concerns

```csharp
public sealed class Product : Aggregate,
    IHasParameterizableName,  // Name + Presentation
    IHasUniqueName,           // Unique Name constraint
    IHasMetadata,             // Public/Private metadata
    IHasSlug,                 // URL slug
    IHasSeoMetadata,          // SEO fields
    ISoftDeletable,           // Soft deletion
    IHasAuditable             // Inherited from Aggregate
{
    // All properties from concerns are now part of this aggregate
    
    // From IHasParameterizableName
    public string Name { get; set; }
    public string Presentation { get; set; }

    // From IHasMetadata
    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }

    // From IHasSlug
    public string Slug { get; set; }

    // From IHasSeoMetadata
    public string? MetaTitle { get; set; }
    public string? MetaDescription { get; set; }
    public string? MetaKeywords { get; set; }

    // From ISoftDeletable
    public DateTimeOffset? DeletedAt { get; set; }
    public string? DeletedBy { get; set; }
    public bool IsDeleted { get; set; }

    // From IHasAuditable (via Aggregate)
    // public DateTimeOffset CreatedAt { get; set; }
    // public DateTimeOffset? UpdatedAt { get; set; }
    // public string? CreatedBy { get; set; }
    // public string? UpdatedBy { get; set; }
}
```

### Configuring Concerns in EF Core

```csharp
public class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.HasKey(x => x.Id);

        // Configure concerns
        builder.ConfigureUniqueName<Product>();           // IHasUniqueName
        builder.ConfigureAuditableEntity<Product>();      // IHasAuditable
        builder.ConfigureSoftDelete<Product>();           // ISoftDeletable
        builder.ConfigureDisplayOn<Product>();            // IHasDisplayOn

        // Configure primitive properties
        builder.Property(x => x.Slug)
            .IsRequired()
            .HasMaxLength(200);

        // Configure complex types
        builder.OwnsOne(x => x.PublicMetadata, mb => mb.ToJson());
        builder.OwnsOne(x => x.PrivateMetadata, mb => mb.ToJson());

        // ... rest of configuration
    }
}
```

---

## ?? Error Handling Patterns

### Standard Error Structure

```csharp
public static class Errors
{
    // NotFound errors
    public static Error NotFound(Guid id) => 
        Error.NotFound(code: "Entity.NotFound", description: $"Entity '{id}' not found");

    // Validation errors
    public static Error InvalidName => 
        Error.Validation(code: "Entity.InvalidName", description: "Name is invalid");

    // Conflict errors
    public static Error AlreadyExists(Guid id) => 
        Error.Conflict(code: "Entity.AlreadyExists", description: $"Entity '{id}' already exists");

    // Failure errors
    public static Error OperationFailed => 
        Error.Failure(code: "Entity.OperationFailed", description: "Operation failed");
}
```

### Error Usage in Methods

```csharp
public static ErrorOr<Entity> Create(string name)
{
    if (string.IsNullOrWhiteSpace(name))
        return Errors.NameRequired;  // Return error immediately

    if (name.Length > Constraints.NameMaxLength)
        return Errors.NameTooLong;   // Return error immediately

    var entity = new Entity
    {
        Id = Guid.NewGuid(),
        Name = name.Trim(),
        CreatedAt = DateTimeOffset.UtcNow
    };

    return entity;  // Return success
}
```

### Propagating Errors

```csharp
public async Task<IActionResult> CreateEntity(CreateEntityRequest request)
{
    var createResult = Entity.Create(request.Name);
    
    if (createResult.IsError)
    {
        return BadRequest(createResult.FirstError);  // Return first error to client
    }

    var entity = createResult.Value;

    _dbContext.Entities.Add(entity);
    await _dbContext.SaveChangesAsync();

    return Ok(new { id = entity.Id });
}
```

---

## ?? Complete Example: Order Domain

For a complete, production-ready example, see:

**Domain Model**: `src/ReSys.Core/Domain/Orders/Order.cs`
**Configuration**: `src/ReSys.Core/Domain/Orders/OrderConfiguration.cs`
**README**: `src/ReSys.Core/Domain/Orders/README.md`

This demonstrates:
- ? Complex aggregate root
- ? State machine pattern
- ? Owned entities (LineItem, Adjustment, Shipment, Payment)
- ? Multiple domain events
- ? Business logic methods with ErrorOr
- ? Metadata support
- ? Financial calculations (using cents)

---

## ?? Testing Domain Models

### Unit Test Template

```csharp
using ErrorOr;
using NUnit.Framework;
using ReSys.Core.Domain.YourContext;

namespace ReSys.Core.Tests.Domain.YourContext;

[TestFixture]
public class YourAggregateTests
{
    private YourAggregate? _aggregate;

    [SetUp]
    public void Setup()
    {
        var result = YourAggregate.Create(
            name: "Test Aggregate",
            description: "A test aggregate",
            value: 10
        );

        Assert.That(result.IsError, Is.False);
        _aggregate = result.Value;
    }

    [Test]
    public void Create_WithValidData_ReturnsAggregate()
    {
        // Arrange
        var name = "Test";
        var description = "Test description";
        var value = 5;

        // Act
        var result = YourAggregate.Create(name, description, value);

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(result.Value.Name, Is.EqualTo(name));
        Assert.That(result.Value.IsActive, Is.True);
    }

    [Test]
    public void Create_WithEmptyName_ReturnsError()
    {
        // Act
        var result = YourAggregate.Create(string.Empty);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError.Code, Is.EqualTo("YourAggregate.NameRequired"));
    }

    [Test]
    public void Create_WithNameTooLong_ReturnsError()
    {
        // Act
        var longName = new string('a', YourAggregate.Constraints.NameMaxLength + 1);
        var result = YourAggregate.Create(longName);

        // Assert
        Assert.That(result.IsError, Is.True);
        Assert.That(result.FirstError.Code, Is.EqualTo("YourAggregate.NameTooLong"));
    }

    [Test]
    public void Update_WithValidName_UpdatesAndPublishesEvent()
    {
        // Act
        var result = _aggregate!.Update(name: "Updated Name");

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(_aggregate.Name, Is.EqualTo("Updated Name"));
        Assert.That(_aggregate.UpdatedAt, Is.Not.Null);
        Assert.That(_aggregate.DomainEvents, Has.Some.TypeOf<YourAggregate.Events.Updated>());
    }

    [Test]
    public void Activate_WhenNotActive_ActivatesAndPublishesEvent()
    {
        // Arrange
        _aggregate!.IsActive = false;

        // Act
        var result = _aggregate.Activate();

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(_aggregate.IsActive, Is.True);
        Assert.That(_aggregate.DomainEvents, Has.Some.TypeOf<YourAggregate.Events.Activated>());
    }

    [Test]
    public void Delete_WhenNotDeleted_DeletesAndPublishesEvent()
    {
        // Act
        var result = _aggregate!.Delete();

        // Assert
        Assert.That(result.IsError, Is.False);
        Assert.That(_aggregate.IsDeleted, Is.True);
        Assert.That(_aggregate.DeletedAt, Is.Not.Null);
        Assert.That(_aggregate.DomainEvents, Has.Some.TypeOf<YourAggregate.Events.Deleted>());
    }
}
```

---

## ?? Checklist for New Domain Models

When creating a new domain model, ensure you:

- [ ] **Create Aggregate Root** in appropriate bounded context directory
  - [ ] Inherit from `Aggregate`
  - [ ] Define `Constraints` class
  - [ ] Define `Errors` class
  - [ ] Apply appropriate `Concern` interfaces
  - [ ] Implement properties for all concerns
  - [ ] Create `Events` class with domain events
  - [ ] Implement factory method `Create()`
  - [ ] Implement business logic methods
  - [ ] Return `ErrorOr<T>` from public methods
  - [ ] Call `AddDomainEvent()` when state changes

- [ ] **Create EF Core Configuration**
  - [ ] Implement `IEntityTypeConfiguration<T>`
  - [ ] Configure primary key
  - [ ] Configure all properties
  - [ ] Apply concern configurations
  - [ ] Define relationships and owned entities
  - [ ] Add indexes for query performance
  - [ ] Set table name and schema
  - [ ] Add comments

- [ ] **Create Validator** (optional but recommended)
  - [ ] Implement `AbstractValidator<T>`
  - [ ] Add rules for all properties
  - [ ] Use `CommonInput` error codes
  - [ ] Apply concern validators

- [ ] **Create Domain Event Handlers**
  - [ ] Implement `INotificationHandler<TEvent>` for each event
  - [ ] Handle domain events appropriately
  - [ ] Log significant events
  - [ ] Update read models if needed

- [ ] **Register in DbContext**
  - [ ] Add `DbSet<T>` property
  - [ ] Apply configuration in `OnModelCreating()`

- [ ] **Create Unit Tests**
  - [ ] Test factory method with valid data
  - [ ] Test factory method with invalid data
  - [ ] Test business logic methods
  - [ ] Test state transitions
  - [ ] Test domain event publishing
  - [ ] Test error scenarios

- [ ] **Create README.md** for context
  - [ ] Document purpose
  - [ ] Define ubiquitous language
  - [ ] List core components
  - [ ] Document business rules
  - [ ] Explain relationships

---

## ?? Common Relationships Between Aggregates

### One-to-Many Composition (Owned Entities)

```csharp
// Parent aggregate
public sealed class Order : Aggregate
{
    public ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
    
    public ErrorOr<Order> AddLineItem(Variant variant, int quantity)
    {
        var lineItemResult = LineItem.Create(Id, variant, quantity, Currency);
        if (lineItemResult.IsError) return lineItemResult.FirstError;
        
        LineItems.Add(lineItemResult.Value);
        return this;
    }
}

// Child entity (owned by Order)
public class LineItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public Guid VariantId { get; set; }
    public int Quantity { get; set; }
    
    // Factory method for creating owned entity
    public static ErrorOr<LineItem> Create(Guid orderId, Variant variant, int quantity, string currency)
    {
        if (quantity < 1) return Error.Validation("Quantity must be at least 1");
        
        return new LineItem
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            VariantId = variant.Id,
            Quantity = quantity
        };
    }
}

// Configuration
public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        // Owned entities
        builder.OwnsMany(o => o.LineItems, lb =>
        {
            lb.HasKey("Id");
            lb.WithOwner().HasForeignKey("OrderId");
            lb.ToTable("OrderLineItems");
            
            // Configure LineItem properties
            lb.Property(li => li.Quantity).IsRequired();
            lb.HasIndex(li => li.VariantId);
        });
    }
}
```

### Many-to-One Reference (by ID)

```csharp
// Parent aggregate
public sealed class Order : Aggregate
{
    public Guid StoreId { get; set; }
    public Guid? UserId { get; set; }
}

// Configuration
builder.HasOne(o => o.Store)
    .WithMany()
    .HasForeignKey(o => o.StoreId)
    .IsRequired()
    .OnDelete(DeleteBehavior.Restrict);

builder.HasOne(o => o.User)
    .WithMany()
    .HasForeignKey(o => o.UserId)
    .OnDelete(DeleteBehavior.SetNull);
```

### Many-to-Many via Junction Entity

```csharp
// Product aggregate
public sealed class Product : Aggregate
{
    public ICollection<ProductOptionType> ProductOptionTypes { get; set; } = new List<ProductOptionType>();
}

// Junction entity (owned by Product)
public class ProductOptionType
{
    public Guid Id { get; set; }
    public Guid ProductId { get; set; }
    public Guid OptionTypeId { get; set; }
    
    // Navigation to external aggregate
    public OptionType OptionType { get; set; } = null!;
}

// Configuration
builder.OwnsMany(p => p.ProductOptionTypes, potb =>
{
    potb.HasKey("Id");
    potb.WithOwner().HasForeignKey("ProductId");
    potb.ToTable("ProductOptionTypes");
    
    potb.HasOne(pot => pot.OptionType)
        .WithMany()
        .HasForeignKey("OptionTypeId");
});
```

---

## ?? Best Practices Summary

1. **Always use Factory Methods** for creating aggregates
2. **Return ErrorOr<T>** from all public business logic methods
3. **Publish Domain Events** when state changes significantly
4. **Validate Inputs** at aggregate boundary
5. **Encapsulate Relationships** - own entities or reference by ID only
6. **Use Constraints Class** for all magic numbers and limits
7. **Use Errors Class** for all domain-specific error messages
8. **Apply Concerns** to leverage reusable patterns
9. **Configure EF Core** properly with relationships and indexes
10. **Test Business Logic** thoroughly and independently

---

## ?? Additional Resources

- **Main README**: `src/ReSys.Core/Domain/README.md` - Comprehensive guide with examples
- **Order Example**: `src/ReSys.Core/Domain/Orders/Order.cs` - Production-ready aggregate
- **Concerns**: `src/ReSys.Core/Common/Domain/Concerns/` - All available concern implementations
- **Constraints**: `src/ReSys.Core/Common/Constants/CommonInput.Constraints.cs` - Validation rules
- **Errors**: `src/ReSys.Core/Common/Constants/CommonInput.Errors.cs` - Standard error helpers

---

**Last Updated**: 2024
**ReSys.Shop v1.0**
**Built with .NET 9**
