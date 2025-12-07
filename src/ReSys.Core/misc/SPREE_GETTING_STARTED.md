# Getting Started: Spree-Aligned Refactoring

## Pre-Implementation Checklist

### Documentation Review (1-2 hours)
- [ ] Read `SPREE_ALIGNED_DECISION.md` (executive summary)
- [ ] Read `SPREE_ALIGNED_REFACTORING_PLAN.md` (detailed plan)
- [ ] Read `SPREE_COMPARISON_QUICK_REFERENCE.md` (comparison)
- [ ] Team discusses: Any questions?

### Team Alignment (30 min)
- [ ] Senior Dev: "Architecture is sound" ✓
- [ ] Tech Lead: "Timeline is realistic" ✓
- [ ] QA Lead: "Testing strategy clear" ✓
- [ ] Product: "No feature regressions" ✓

### Pre-Work (This Week)

**Task 1: Set Up Database Backup**
```powershell
# Backup current database
pg_dump resys_shop > backup_before_spree_refactor_$(date +%Y%m%d).sql

# Verify backup
ls -lh backup_before_spree_refactor_*.sql
```

**Task 2: Create Rollback Branch**
```powershell
cd c:\Users\ElTow\source\ReSys.Shop
git checkout -b feature/spree-aligned-refactor
git push origin feature/spree-aligned-refactor
```

**Task 3: Create Tracking Issue**
```
Title: Spree-Aligned Refactoring: Remove FulfillmentOrder
Timeline: 4 weeks (start [DATE])
Assignee: [Senior Dev]
Tasks:
  - Week 1: Expand Shipment (15h)
  - Week 2: Migrate Commands (20h)
  - Week 3: State Machine (20h)
  - Week 4: Cleanup (15h)
```

---

## Week 1: Expand Shipment Model (15 hours)

### Files to Modify

**1. Expand Shipment Entity**

File: `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs`

```csharp
// ADD after existing properties:

/// <summary>
/// Warehouse workflow timestamps (replacing FulfillmentOrder)
/// </summary>
public DateTimeOffset? AllocatedAt { get; set; }
public DateTimeOffset? PickingStartedAt { get; set; }
public DateTimeOffset? PickedAt { get; set; }
public DateTimeOffset? PackedAt { get; set; }
public DateTimeOffset? ReadyToShipAt { get; set; }
public DateTimeOffset? ShippedAt { get; set; }
public DateTimeOffset? DeliveredAt { get; set; }
public DateTimeOffset? CanceledAt { get; set; }

/// <summary>
/// Physical package identifier (box/pallet/etc.)
/// </summary>
public string? PackageId { get; set; }

/// <summary>
/// Inventory units in this shipment
/// </summary>
public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();

/// <summary>
/// Stock movements (audit trail of inventory changes)
/// </summary>
public ICollection<StockMovement> StockMovements { get; set; } = new List<StockMovement>();
```

**2. Update Shipment State Enum**

File: `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs`

```csharp
// REPLACE existing ShipmentState:

public enum ShipmentState
{
    /// <summary>Shipment waiting to process (no stock, unpaid, etc.)</summary>
    Pending,
    
    /// <summary>Stock allocated and ready to ship</summary>
    Ready,
    
    /// <summary>Items picked from shelves</summary>
    Picked,
    
    /// <summary>Items packed in box/container</summary>
    Packed,
    
    /// <summary>On dock, ready for carrier pickup</summary>
    ReadyToShip,
    
    /// <summary>Handed to carrier, in transit</summary>
    Shipped,
    
    /// <summary>Customer received package</summary>
    Delivered,
    
    /// <summary>Shipment canceled, inventory returned</summary>
    Canceled
}
```

**3. Create StockMovement Entity**

Create: `src/ReSys.Core/Domain/Orders/StockMovement.cs`

```csharp
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Orders.Shipments;

namespace ReSys.Core.Domain.Orders;

/// <summary>
/// Represents an inventory movement (allocation, shipment, return, etc.)
/// Used for audit trail and inventory history tracking.
/// </summary>
public sealed class StockMovement : Entity
{
    /// <summary>References the stock item this movement applies to</summary>
    public Guid StockItemId { get; set; }
    
    /// <summary>The quantity changed (positive = add, negative = remove)</summary>
    public int Quantity { get; set; }
    
    /// <summary>Type of movement (allocated, confirmed, shipped, returned, etc.)</summary>
    public string MovementType { get; set; } = string.Empty;
    
    /// <summary>What triggered this movement (Shipment, Return, Adjustment, etc.)</summary>
    public string? OriginatorType { get; set; }
    
    /// <summary>ID of the originator (ShipmentId, ReturnId, etc.)</summary>
    public Guid? OriginatorId { get; set; }
    
    // Navigation
    public StockItem StockItem { get; set; } = null!;
}
```

**4. Create StockMovement Configuration**

Create: `src/ReSys.Infrastructure/Persistence/Configurations/StockMovementConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ReSys.Core.Domain.Orders;

namespace ReSys.Infrastructure.Persistence.Configurations;

public sealed class StockMovementConfiguration : IEntityTypeConfiguration<StockMovement>
{
    public void Configure(EntityTypeBuilder<StockMovement> builder)
    {
        builder.ToTable("StockMovements");
        
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();
        
        builder.Property(x => x.StockItemId).IsRequired();
        builder.Property(x => x.Quantity).IsRequired();
        builder.Property(x => x.MovementType).HasMaxLength(50).IsRequired();
        builder.Property(x => x.OriginatorType).HasMaxLength(100);
        builder.Property(x => x.CreatedAt).IsRequired();
        
        builder.HasOne(x => x.StockItem)
            .WithMany()
            .HasForeignKey(x => x.StockItemId)
            .OnDelete(DeleteBehavior.Cascade);
        
        builder.HasIndex(x => x.StockItemId);
        builder.HasIndex(x => x.CreatedAt);
    }
}
```

**5. Update Shipment Configuration**

File: `src/ReSys.Infrastructure/Persistence/Configurations/ShipmentConfiguration.cs`

```csharp
// ADD to Configure() method:

builder.Property(x => x.AllocatedAt);
builder.Property(x => x.PickingStartedAt);
builder.Property(x => x.PickedAt);
builder.Property(x => x.PackedAt);
builder.Property(x => x.ReadyToShipAt);
builder.Property(x => x.ShippedAt);
builder.Property(x => x.DeliveredAt);
builder.Property(x => x.CanceledAt);
builder.Property(x => x.PackageId).HasMaxLength(255);

builder.HasMany(x => x.InventoryUnits)
    .WithOne()
    .HasForeignKey(x => x.ShipmentId)
    .OnDelete(DeleteBehavior.Cascade);

builder.HasMany(x => x.StockMovements)
    .WithOne()
    .HasForeignKey(x => x.OriginatorId)
    .OnDelete(DeleteBehavior.Cascade);
```

**6. Create Database Migration**

```powershell
cd c:\Users\ElTow\source\ReSys.Shop

# Create migration
dotnet ef migrations add AddWarehouseWorkflowToShipments `
    --project src/ReSys.Infrastructure `
    --startup-project src/ReSys.API

# Verify migration created
ls src/ReSys.Infrastructure/Migrations | tail -1

# Review the migration file and make sure it looks correct
```

**7. Run Tests**

```powershell
# Unit tests for new entities
dotnet test tests/Core.UnitTests/Domain/Orders/ -v m

# Should pass (or be skipped if not written yet)
```

### Checklist: Week 1 Complete

- [ ] Shipment entity expanded with warehouse fields
- [ ] ShipmentState enum updated
- [ ] StockMovement entity created
- [ ] StockMovement configuration created
- [ ] Shipment configuration updated
- [ ] Database migration created
- [ ] Migration tested locally
- [ ] Unit tests pass

---

## Week 2: Migrate Commands (20 hours)

### Files to Move (Copy → Update → Delete Original)

**For each command below:**
1. Copy from `src/ReSys.Core/Feature/Fulfillment/Commands/`
2. Move to `src/ReSys.Core/Feature/Orders/Shipments/Commands/`
3. Rename to use "Shipment" prefix
4. Update handlers to use Shipment instead of FulfillmentOrder
5. Add StockMovement creation
6. Delete original

**Commands to migrate:**
- AllocateInventoryCommand → AllocateShipmentInventoryCommand
- StartPickingCommand → StartShipmentPickingCommand
- CompletePickingCommand → CompleteShipmentPickingCommand
- PackCommand → PackShipmentCommand
- MarkReadyToShipCommand → MarkShipmentReadyCommand
- ShipCommand → ShipShipmentCommand

### Example: AllocateShipmentInventoryCommand

```csharp
// Location: src/ReSys.Core/Feature/Orders/Shipments/Commands/AllocateShipmentInventoryCommand.cs

public sealed record AllocateShipmentInventoryCommand(Guid ShipmentId) 
    : ICommand<ShipmentResponse>;

public sealed class AllocateShipmentInventoryValidator 
    : AbstractValidator<AllocateShipmentInventoryCommand>
{
    public AllocateShipmentInventoryValidator()
    {
        RuleFor(x => x.ShipmentId).NotEmpty();
    }
}

public sealed class AllocateShipmentInventoryHandler 
    : ICommandHandler<AllocateShipmentInventoryCommand, ShipmentResponse>
{
    private readonly IApplicationDbContext _dbContext;
    
    public async Task<ErrorOr<ShipmentResponse>> Handle(
        AllocateShipmentInventoryCommand command, 
        CancellationToken ct)
    {
        var shipment = await _dbContext.Shipments
            .Include(s => s.Order.LineItems)
            .Include(s => s.StockLocation.StockItems)
            .FirstOrDefaultAsync(s => s.Id == command.ShipmentId, cancellationToken: ct);
        
        if (shipment == null)
            return Errors.ShipmentNotFound(command.ShipmentId);
        
        // Allocate inventory for each line item
        foreach (var lineItem in shipment.Order.LineItems)
        {
            var stockItem = shipment.StockLocation.StockItems
                .FirstOrDefault(si => si.VariantId == lineItem.VariantId);
            
            if (stockItem == null)
                return Error.NotFound($"Stock for variant {lineItem.VariantId} not found");
            
            // Create stock movement (audit trail)
            var movement = new StockMovement
            {
                Id = Guid.NewGuid(),
                StockItemId = stockItem.Id,
                Quantity = -lineItem.Quantity,
                MovementType = "allocated",
                OriginatorType = nameof(Shipment),
                OriginatorId = shipment.Id,
                CreatedAt = DateTimeOffset.UtcNow
            };
            
            _dbContext.StockMovements.Add(movement);
        }
        
        // Update shipment state
        shipment.State = ShipmentState.Ready;
        shipment.AllocatedAt = DateTimeOffset.UtcNow;
        
        _dbContext.Shipments.Update(shipment);
        await _dbContext.SaveChangesAsync(ct);
        
        return shipment.Adapt<ShipmentResponse>();
    }
}
```

### Checklist: Week 2 Complete

- [ ] All 6 commands moved from Fulfillment → Orders/Shipments
- [ ] Command names updated (AllocateShipmentInventory, etc.)
- [ ] Handlers updated to use Shipment
- [ ] StockMovement creation added to each handler
- [ ] All commands tested (unit tests)
- [ ] Original Fulfillment command files deleted
- [ ] Code compiles without errors
- [ ] All warehouse operations work on Shipment

---

## Week 3: Add State Machine Methods (20 hours)

### Add Methods to Shipment Aggregate

File: `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs`

Add these methods to Shipment class:

```csharp
public ErrorOr<Shipment> AllocateInventory()
{
    if (State != ShipmentState.Pending)
        return Error.Validation("Shipment.InvalidState", "Cannot allocate non-pending shipment");
    
    State = ShipmentState.Ready;
    AllocatedAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.InventoryAllocated(Id, OrderId, StockLocationId));
    return this;
}

public ErrorOr<Shipment> StartPicking()
{
    if (State != ShipmentState.Ready)
        return Error.Validation("Shipment.InvalidState", "Shipment must be ready");
    
    State = ShipmentState.Picked; // Simplify: go directly to Picked
    PickingStartedAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.PickingStarted(Id, OrderId));
    return this;
}

public ErrorOr<Shipment> Pack(string packageId)
{
    if (State != ShipmentState.Picked)
        return Error.Validation("Shipment.InvalidState", "Shipment must be picked");
    if (string.IsNullOrWhiteSpace(packageId))
        return Error.Validation("Shipment.PackageIdRequired", "Package ID required");
    
    State = ShipmentState.Packed;
    PackageId = packageId;
    PackedAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.PackingCompleted(Id, OrderId, packageId));
    return this;
}

public ErrorOr<Shipment> MarkReadyToShip()
{
    if (State != ShipmentState.Packed)
        return Error.Validation("Shipment.InvalidState", "Shipment must be packed");
    
    State = ShipmentState.ReadyToShip;
    ReadyToShipAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.ReadyToShip(Id, OrderId));
    return this;
}

public ErrorOr<Shipment> Ship(string trackingNumber)
{
    if (State != ShipmentState.ReadyToShip)
        return Error.Validation("Shipment.InvalidState", "Shipment must be ready to ship");
    if (string.IsNullOrWhiteSpace(trackingNumber))
        return Error.Validation("Shipment.TrackingRequired", "Tracking number required");
    
    State = ShipmentState.Shipped;
    TrackingNumber = trackingNumber;
    ShippedAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.Shipped(Id, OrderId, trackingNumber));
    return this;
}

public ErrorOr<Shipment> Deliver()
{
    if (State != ShipmentState.Shipped)
        return Error.Validation("Shipment.InvalidState", "Shipment must be shipped");
    
    State = ShipmentState.Delivered;
    DeliveredAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.Delivered(Id, OrderId));
    return this;
}

public ErrorOr<Shipment> Cancel()
{
    if (State == ShipmentState.Delivered || State == ShipmentState.Shipped)
        return Error.Validation("Shipment.CannotCancel", "Cannot cancel shipped/delivered");
    if (State == ShipmentState.Canceled)
        return this; // Idempotent
    
    State = ShipmentState.Canceled;
    CanceledAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.Canceled(Id, OrderId));
    return this;
}
```

### Add Events to Shipment

```csharp
public static class Events
{
    public sealed record Created(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
    public sealed record InventoryAllocated(Guid ShipmentId, Guid OrderId, Guid StockLocationId) : DomainEvent;
    public sealed record PickingStarted(Guid ShipmentId, Guid OrderId) : DomainEvent;
    public sealed record PickingCompleted(Guid ShipmentId, Guid OrderId) : DomainEvent;
    public sealed record PackingCompleted(Guid ShipmentId, Guid OrderId, string PackageId) : DomainEvent;
    public sealed record ReadyToShip(Guid ShipmentId, Guid OrderId) : DomainEvent;
    public sealed record Shipped(Guid ShipmentId, Guid OrderId, string TrackingNumber) : DomainEvent;
    public sealed record Delivered(Guid ShipmentId, Guid OrderId) : DomainEvent;
    public sealed record Canceled(Guid ShipmentId, Guid OrderId) : DomainEvent;
}
```

### Write Unit Tests

Create: `tests/Core.UnitTests/Domain/Orders/ShipmentStateTransitionsTests.cs`

```csharp
[TestFixture]
public class ShipmentStateTransitionsTests
{
    [Test]
    public void AllocateInventory_FromPending_TransitionsToReady()
    {
        var shipment = Shipment.Create(...).Value;
        var result = shipment.AllocateInventory();
        
        Assert.That(result.IsError, Is.False);
        Assert.That(shipment.State, Is.EqualTo(ShipmentState.Ready));
        Assert.That(shipment.AllocatedAt, Is.Not.Null);
    }
    
    // ... more tests
}
```

### Checklist: Week 3 Complete

- [ ] All state machine methods added to Shipment
- [ ] Shipment.Events class created
- [ ] All events raise correctly
- [ ] Unit tests for state transitions pass
- [ ] FulfillmentOrder aggregate deleted from codebase
- [ ] No references to FulfillmentOrder remain
- [ ] All warehouse operations now on Shipment

---

## Week 4: Integration & Cleanup (15 hours)

### Tasks

- [ ] Integration tests: Order → Shipment full workflow
- [ ] Multi-warehouse tests (2+ Shipments per Order)
- [ ] Update warehouse dashboard queries (Shipment instead of FulfillmentOrder)
- [ ] Update warehouse API responses (if any changes needed)
- [ ] Delete entire `src/ReSys.Core/Domain/Fulfillment/` folder
- [ ] Delete entire `src/ReSys.Core/Feature/Fulfillment/` folder
- [ ] Delete old fulfillment documentation
- [ ] Update main README.md
- [ ] Team training session (30 min)
- [ ] Final code review
- [ ] Test coverage report

### Final Checklist

- [ ] All tests pass (unit + integration)
- [ ] Code coverage 80%+
- [ ] No FulfillmentOrder references in codebase
- [ ] Fulfillment domain folder deleted
- [ ] Fulfillment feature folder deleted
- [ ] Warehouse dashboard works with Shipment queries
- [ ] Customer APIs unchanged
- [ ] Documentation updated
- [ ] Team trained
- [ ] PR reviewed and merged

---

## Success Confirmation

When complete, verify:

```powershell
# 1. Check no Fulfillment references remain
grep -r "Fulfillment" src/ --include="*.cs" --exclude-dir=bin --exclude-dir=obj

# Should return: 0 results (except in comments if explaining history)

# 2. Check Shipment has all warehouse methods
grep -r "AllocateInventory\|StartPicking\|Pack\|Ship" src/ReSys.Core/Domain/Orders/Shipments/

# Should return: Methods defined on Shipment

# 3. Run all tests
dotnet test

# Should: All pass (or skip if needed)

# 4. Build solution
dotnet build

# Should: Build without warnings
```

---

## Done!

When all 4 weeks complete:
- ✅ Fulfillment domain removed
- ✅ Shipment expanded with warehouse workflow
- ✅ StockMovement for audit trail
- ✅ Spree-aligned architecture
- ✅ 40% less code
- ✅ Single state machine
- ✅ All tests pass
- ✅ Team trained

**Celebration time!** 🎉

---

## Quick Commands Reference

```powershell
# Start branch
git checkout -b feature/spree-aligned-refactor

# After each week
git add .
git commit -m "Week 1: Expand Shipment model" # etc.

# Run tests
dotnet test tests/Core.UnitTests/ -v m

# Build
dotnet build

# Create migration
dotnet ef migrations add [MigrationName] `
    --project src/ReSys.Infrastructure `
    --startup-project src/ReSys.API

# Apply migration
dotnet ef database update `
    --project src/ReSys.Infrastructure `
    --startup-project src/ReSys.API

# When done
git push origin feature/spree-aligned-refactor
# Create PR
```

---

**Ready to start? Go to Week 1!** 🚀
