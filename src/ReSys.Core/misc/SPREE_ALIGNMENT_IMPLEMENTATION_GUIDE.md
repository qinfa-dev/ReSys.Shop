# Spree Alignment: Detailed Implementation Guide

This guide provides concrete code examples for aligning ReSys.Shop with Spree's proven architecture.

---

## 1. Create InventoryUnit Domain Model

**File**: `src/ReSys.Core/Domain/Orders/InventoryUnits/InventoryUnit.cs`

```csharp
using ErrorOr;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Orders.InventoryUnits;

/// <summary>
/// Represents an individual inventory unit tied to a line item and shipment.
/// Each unit tracks its individual state through the fulfillment lifecycle.
/// 
/// Aligns with Spree's InventoryUnit pattern for simplified inventory tracking.
/// </summary>
public sealed class InventoryUnit : Entity
{
    /// <summary>
    /// States an inventory unit progresses through.
    /// </summary>
    public enum InventoryUnitState
    {
        OnHand,       // Item is in stock
        Backordered,  // Item not yet in stock
        Shipped,      // Item left warehouse (on carrier)
        Received      // Customer received item
    }

    #region Properties
    /// <summary>Foreign key to the LineItem this unit belongs to.</summary>
    public Guid LineItemId { get; set; }

    /// <summary>Foreign key to the Variant of this unit.</summary>
    public Guid VariantId { get; set; }

    /// <summary>Foreign key to the Shipment this unit is assigned to (nullable until shipment created).</summary>
    public Guid? ShipmentId { get; set; }

    /// <summary>Foreign key to the Order this unit belongs to.</summary>
    public Guid OrderId { get; set; }

    /// <summary>Current state of this inventory unit.</summary>
    public InventoryUnitState State { get; set; } = InventoryUnitState.OnHand;

    /// <summary>Timestamp when unit was shipped (nullable until shipped).</summary>
    public DateTimeOffset? ShippedAt { get; set; }

    /// <summary>Timestamp when unit was delivered (nullable until delivered).</summary>
    public DateTimeOffset? ReceivedAt { get; set; }
    #endregion

    #region Relationships
    public LineItem LineItem { get; set; } = null!;
    public Variant Variant { get; set; } = null!;
    public Shipment? Shipment { get; set; }
    #endregion

    #region Constructors
    private InventoryUnit() { }
    #endregion

    #region Factory Methods
    public static InventoryUnit Create(Guid lineItemId, Guid variantId, Guid orderId, bool backordered = false)
    {
        return new InventoryUnit
        {
            Id = Guid.NewGuid(),
            LineItemId = lineItemId,
            VariantId = variantId,
            OrderId = orderId,
            State = backordered ? InventoryUnitState.Backordered : InventoryUnitState.OnHand,
            CreatedAt = DateTimeOffset.UtcNow
        };
    }
    #endregion

    #region Business Logic
    /// <summary>Transitions unit to Shipped state when shipment leaves warehouse.</summary>
    public ErrorOr<InventoryUnit> Ship(Guid shipmentId, string? trackingNumber = null)
    {
        if (State == InventoryUnitState.Received)
            return Error.Validation("InventoryUnit.AlreadyReceived", "Cannot reship delivered unit.");

        State = InventoryUnitState.Shipped;
        ShipmentId = shipmentId;
        ShippedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    /// <summary>Transitions unit to Received state when customer accepts delivery.</summary>
    public ErrorOr<InventoryUnit> Receive()
    {
        if (State != InventoryUnitState.Shipped)
            return Error.Validation("InventoryUnit.NotShipped", "Unit must be shipped before delivery.");

        State = InventoryUnitState.Received;
        ReceivedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    /// <summary>Backorder a unit that came back in stock.</summary>
    public ErrorOr<InventoryUnit> Backorder()
    {
        if (State != InventoryUnitState.OnHand)
            return Error.Validation("InventoryUnit.InvalidState", "Only on-hand units can be backordered.");

        State = InventoryUnitState.Backordered;
        UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    /// <summary>Restore a backordered unit to on-hand.</summary>
    public ErrorOr<InventoryUnit> RestoreToOnHand()
    {
        if (State != InventoryUnitState.Backordered)
            return Error.Validation("InventoryUnit.NotBackordered", "Unit must be backordered to restore.");

        State = InventoryUnitState.OnHand;
        UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }
    #endregion
}
```

**File**: `src/ReSys.Core/Domain/Orders/InventoryUnits/InventoryUnitConfiguration.cs`

```csharp
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReSys.Core.Domain.Orders.InventoryUnits;

public sealed class InventoryUnitConfiguration : IEntityTypeConfiguration<InventoryUnit>
{
    public void Configure(EntityTypeBuilder<InventoryUnit> builder)
    {
        builder.HasKey(x => x.Id);

        builder.Property(x => x.LineItemId).IsRequired();
        builder.Property(x => x.VariantId).IsRequired();
        builder.Property(x => x.OrderId).IsRequired();
        builder.Property(x => x.ShipmentId).IsRequired(false);
        builder.Property(x => x.State).HasConversion<string>().IsRequired();
        builder.Property(x => x.ShippedAt).IsRequired(false);
        builder.Property(x => x.ReceivedAt).IsRequired(false);

        builder.HasIndex(x => x.OrderId);
        builder.HasIndex(x => x.LineItemId);
        builder.HasIndex(x => x.ShipmentId);
        builder.HasIndex(x => new { x.OrderId, x.State });

        builder.HasOne(x => x.LineItem)
            .WithMany(x => x.InventoryUnits)
            .HasForeignKey(x => x.LineItemId);

        builder.HasOne(x => x.Variant)
            .WithMany()
            .HasForeignKey(x => x.VariantId);

        builder.HasOne(x => x.Shipment)
            .WithMany(x => x.InventoryUnits)
            .HasForeignKey(x => x.ShipmentId)
            .IsRequired(false);

        builder.ToTable("InventoryUnits");
    }
}
```

---

## 2. Expand Shipment Model with Warehouse Workflow

**File**: `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs` (Replace warehouse methods)

```csharp
// Add to existing Shipment class:

#region Warehouse Workflow Methods
/// <summary>
/// Allocates inventory for this shipment.
/// Transitions from Pending to Ready when all inventory units are on-hand.
/// </summary>
public ErrorOr<Shipment> AllocateInventory()
{
    if (State != ShipmentState.Pending)
        return Error.Validation("Shipment.InvalidStateForAllocation", "Shipment must be in Pending state.");

    // Verify we have inventory units
    if (!InventoryUnits.Any())
        return Error.Validation("Shipment.NoInventoryUnits", "Shipment has no inventory units.");

    // Check if all units are on-hand (not backordered)
    var backordered = InventoryUnits.Where(u => u.State == InventoryUnit.InventoryUnitState.Backordered).ToList();
    if (backordered.Any())
        return Error.Validation("Shipment.BackorderedItems", $"{backordered.Count} items are backordered.");

    State = ShipmentState.Ready;
    UpdatedAt = DateTimeOffset.UtcNow;
    AddDomainEvent(new Events.InventoryAllocated(Id, OrderId, StockLocationId));

    return this;
}

/// <summary>
/// Ships this shipment with tracking number.
/// Transitions to Shipped state and updates all inventory units.
/// </summary>
public ErrorOr<Shipment> Ship(string trackingNumber)
{
    if (State != ShipmentState.Ready)
        return Error.Validation("Shipment.NotReady", "Shipment must be in Ready state to ship.");

    if (string.IsNullOrWhiteSpace(trackingNumber))
        return Error.Validation("Shipment.TrackingNumberRequired", "Tracking number is required.");

    // Transition all inventory units to Shipped
    foreach (var unit in InventoryUnits)
    {
        var result = unit.Ship(Id, trackingNumber);
        if (result.IsError) return result.FirstError;
    }

    State = ShipmentState.Shipped;
    TrackingNumber = trackingNumber;
    ShippedAt = DateTimeOffset.UtcNow;
    UpdatedAt = DateTimeOffset.UtcNow;

    AddDomainEvent(new Events.Shipped(Id, OrderId, StockLocationId, trackingNumber));

    return this;
}

/// <summary>
/// Marks shipment as delivered.
/// Transitions inventory units to Received state.
/// </summary>
public ErrorOr<Shipment> Deliver()
{
    if (State != ShipmentState.Shipped)
        return Error.Validation("Shipment.NotShipped", "Shipment must be shipped before delivery.");

    // Transition all inventory units to Received
    foreach (var unit in InventoryUnits)
    {
        var result = unit.Receive();
        if (result.IsError) return result.FirstError;
    }

    State = ShipmentState.Delivered;
    DeliveredAt = DateTimeOffset.UtcNow;
    UpdatedAt = DateTimeOffset.UtcNow;

    AddDomainEvent(new Events.Delivered(Id, OrderId, StockLocationId));

    return this;
}

/// <summary>
/// Cancels this shipment and releases inventory.
/// </summary>
public ErrorOr<Shipment> Cancel()
{
    if (State == ShipmentState.Shipped || State == ShipmentState.Delivered)
        return Error.Validation("Shipment.CannotCancelShipped", "Cannot cancel shipped/delivered shipment.");

    if (State == ShipmentState.Canceled)
        return this; // Idempotent

    State = ShipmentState.Canceled;
    CanceledAt = DateTimeOffset.UtcNow;
    UpdatedAt = DateTimeOffset.UtcNow;

    AddDomainEvent(new Events.Canceled(Id, OrderId, StockLocationId));

    return this;
}
#endregion

#region Relationships (Updated)
/// <summary>
/// Inventory units in this shipment.
/// Each represents an individual item being shipped.
/// </summary>
public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
#endregion
```

---

## 3. Update LineItem to Create InventoryUnits

**File**: `src/ReSys.Core/Domain/Orders/LineItems/LineItem.cs` (Add)

```csharp
// Add to existing LineItem class:

#region Relationships (Updated)
/// <summary>
/// Individual inventory units representing each item quantity.
/// Created when line item is added; deleted when removed.
/// </summary>
public ICollection<InventoryUnit> InventoryUnits { get; set; } = new List<InventoryUnit>();
#endregion

#region Methods
/// <summary>
/// Creates inventory units for this line item.
/// Called after line item is added to order.
/// </summary>
public void CreateInventoryUnits(bool backordered = false)
{
    for (int i = 0; i < Quantity; i++)
    {
        var unit = InventoryUnit.Create(
            lineItemId: Id,
            variantId: VariantId,
            orderId: OrderId,
            backordered: backordered
        );
        InventoryUnits.Add(unit);
    }
}

/// <summary>
/// Removes excess inventory units when quantity is reduced.
/// </summary>
public void TrimInventoryUnits(int newQuantity)
{
    var toRemove = InventoryUnits
        .Where(u => u.State == InventoryUnit.InventoryUnitState.OnHand)
        .Take(InventoryUnits.Count - newQuantity)
        .ToList();

    foreach (var unit in toRemove)
    {
        InventoryUnits.Remove(unit);
    }
}
#endregion
```

---

## 4. Simplify Order.Complete() Method

**File**: `src/ReSys.Core/Domain/Orders/Order.cs` (Replace Complete method)

```csharp
/// <summary>
/// Completes the order - simplified Spree-aligned version.
/// </summary>
private ErrorOr<Order> Complete()
{
    // ✓ Check all payments are completed
    var completedPayments = Payments.Where(p => p.IsCompleted).Sum(p => p.AmountCents);
    if (completedPayments < TotalCents)
    {
        return Error.Validation(
            code: "Order.InsufficientPayment",
            description: $"Order requires ${TotalCents / 100m:F2}. Received: ${completedPayments / 100m:F2}");
    }

    // ✓ For physical orders, verify shipments exist and are at least Ready
    if (!IsFullyDigital)
    {
        if (!Shipments.Any())
            return Error.Validation("Order.NoShipments", "Physical orders require at least one shipment.");

        if (Shipments.Any(s => s.State == Shipment.ShipmentState.Pending))
            return Error.Validation("Order.ShipmentsPending", "All shipments must be ready or shipped.");
    }

    // ✓ Set completion timestamp and state
    CompletedAt = DateTimeOffset.UtcNow;
    State = OrderState.Complete;
    UpdatedAt = DateTimeOffset.UtcNow;

    // ✓ Publish events
    AddDomainEvent(new Events.Completed(OrderId: Id, StoreId: StoreId));
    AddDomainEvent(new Events.FinalizeInventory(OrderId: Id, StoreId: StoreId));

    if (HasPromotion && PromotionId.HasValue)
    {
        AddDomainEvent(new Promotion.Events.Used(PromotionId: PromotionId!.Value, OrderId: Id));
    }

    return this;
}

// Update error definition in Errors class:
public static class Errors
{
    // Remove old fulfillment-related errors
    // public static Error FulfillmentLocationRequired => ... // ❌ REMOVE
    // public static Error OrderFulfillmentDomainService => ... // ❌ REMOVE

    // Add new/updated errors
    public static Error NoShipments => 
        Error.Validation("Order.NoShipments", "Physical orders require at least one shipment.");

    public static Error ShipmentsPending => 
        Error.Validation("Order.ShipmentsPending", "All shipments must be ready or shipped before completing order.");
}
```

---

## 5. Remove Fulfillment Domain References

**File**: `src/ReSys.Core/Domain/Orders/Order.cs` (Remove imports)

```csharp
// Remove these lines:
// using ReSys.Core.Domain.Fulfillment; ❌
// using ReSys.Core.Domain.Orders.OrderFulfillmentDomainService; ❌

// Remove these properties/methods:
public Guid? FulfillmentLocationId { get; set; } // ❌
public StockLocation? FulfillmentLocation { get; set; } // ❌

public ErrorOr<Order> SetFulfillmentLocation(StockLocation? location) { } // ❌

// Remove this event
public sealed record FulfillmentLocationSelected(...) : DomainEvent; // ❌
```

---

## 6. Update Shipment Creation

**File**: Application service that creates shipments (e.g., CreateShipmentsService)

```csharp
using ErrorOr;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Orders;

namespace ReSys.Core.Feature.Orders.Services;

/// <summary>
/// Creates shipments for an order during Delivery state transition.
/// Replaces OrderFulfillmentDomainService.
/// 
/// Spree-aligned: Coordinator pattern (simplified).
/// </summary>
public sealed class CreateShipmentsService
{
    private readonly IApplicationDbContext _dbContext;

    public CreateShipmentsService(IApplicationDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    /// <summary>
    /// Creates proposed shipments for an order.
    /// Groups line items by warehouse (stock location).
    /// </summary>
    public async Task<ErrorOr<List<Shipment>>> CreateProposedShipments(Order order, CancellationToken ct)
    {
        // Validate prerequisites
        if (order.IsFullyDigital)
            return Error.Validation("Order.DigitalOrder", "Digital orders don't need shipments.");

        if (!order.ShippingMethodId.HasValue)
            return Error.Validation("Order.NoShippingMethod", "Shipping method must be selected.");

        if (order.LineItems.Count == 0)
            return Error.Validation("Order.EmptyCart", "Cannot create shipments for empty order.");

        // Group line items by warehouse
        // For simplicity: use single default warehouse
        // For advanced: implement splitter logic for multi-warehouse
        var warehouse = await _dbContext.StockLocations
            .FirstAsync(sl => sl.Active, cancellationToken: ct);

        if (warehouse == null)
            return Error.NotFound("StockLocation.NotFound", "No active warehouse available.");

        // Create single shipment (can be extended for multiple warehouses)
        var shipmentResult = Shipment.Create(
            orderId: order.Id,
            shippingMethodId: order.ShippingMethodId.Value,
            stockLocationId: warehouse.Id
        );

        if (shipmentResult.IsError)
            return shipmentResult.FirstError;

        var shipment = shipmentResult.Value;

        // Create inventory units for each line item
        foreach (var lineItem in order.LineItems)
        {
            // Check stock availability
            var hasStock = await _dbContext.StockItems
                .AnyAsync(si =>
                    si.StockLocationId == warehouse.Id &&
                    si.VariantId == lineItem.VariantId &&
                    si.CountOnHand >= lineItem.Quantity,
                    cancellationToken: ct);

            var backordered = !hasStock;

            // Create inventory units
            lineItem.CreateInventoryUnits(backordered);

            // Add units to shipment
            foreach (var unit in lineItem.InventoryUnits)
            {
                shipment.InventoryUnits.Add(unit);
            }
        }

        return new List<Shipment> { shipment };
    }
}
```

---

## 7. Update DbContext

**File**: `src/ReSys.Infrastructure/Persistence/IApplicationDbContext.cs`

```csharp
// Add InventoryUnit DbSet
public DbSet<InventoryUnit> InventoryUnits { get; set; }

// Remove Fulfillment DbSet
// public DbSet<FulfillmentOrder> FulfillmentOrders { get; set; } // ❌ REMOVE
// public DbSet<FulfillmentLineItem> FulfillmentLineItems { get; set; } // ❌ REMOVE
```

**File**: `src/ReSys.Infrastructure/Persistence/ApplicationDbContext.cs`

```csharp
// In OnModelCreating:

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    // Add InventoryUnit configuration
    modelBuilder.ApplyConfiguration(new InventoryUnitConfiguration());

    // Remove Fulfillment configurations
    // modelBuilder.ApplyConfiguration(new FulfillmentOrderConfiguration()); // ❌
    // modelBuilder.ApplyConfiguration(new FulfillmentLineItemConfiguration()); // ❌

    // ... rest of configurations
}
```

---

## 8. Update Event Handlers

**Delete**: `src/ReSys.Core/Feature/Fulfillment/EventHandlers/ShipmentCreatedEventHandler.cs` ❌

**File**: `src/ReSys.Core/Feature/Orders/EventHandlers/ShipmentReadyEventHandler.cs` (New)

```csharp
using MediatR;

namespace ReSys.Core.Feature.Orders.EventHandlers;

/// <summary>
/// When Shipment reaches Ready state, notify warehouse system.
/// Replaces FulfillmentOrderCreatedEventHandler.
/// </summary>
public sealed class ShipmentReadyEventHandler : INotificationHandler<Shipment.Events.InventoryAllocated>
{
    private readonly ILogger<ShipmentReadyEventHandler> _logger;

    public ShipmentReadyEventHandler(ILogger<ShipmentReadyEventHandler> logger)
    {
        _logger = logger;
    }

    public async Task Handle(Shipment.Events.InventoryAllocated notification, CancellationToken ct)
    {
        _logger.LogInformation(
            "Shipment {ShipmentId} inventory allocated, ready for warehouse processing",
            notification.ShipmentId);

        // Send to warehouse management system
        // POST to /warehouse/queue/{shipmentId}
        // Or publish to message queue: "shipment.ready"
    }
}
```

---

## 9. Database Migration

**File**: `src/ReSys.Infrastructure/Migrations/AddInventoryUnitsRemoveFulfillment.cs`

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

public partial class AddInventoryUnitsRemoveFulfillment : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        // Create InventoryUnits table
        migrationBuilder.CreateTable(
            name: "InventoryUnits",
            columns: table => new
            {
                Id = table.Column<Guid>(nullable: false),
                LineItemId = table.Column<Guid>(nullable: false),
                VariantId = table.Column<Guid>(nullable: false),
                ShipmentId = table.Column<Guid>(nullable: true),
                OrderId = table.Column<Guid>(nullable: false),
                State = table.Column<string>(nullable: false), // OnHand, Backordered, Shipped, Received
                ShippedAt = table.Column<DateTimeOffset>(nullable: true),
                ReceivedAt = table.Column<DateTimeOffset>(nullable: true),
                CreatedAt = table.Column<DateTimeOffset>(nullable: false),
                UpdatedAt = table.Column<DateTimeOffset>(nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_InventoryUnits", x => x.Id);
                table.ForeignKey("FK_InventoryUnits_LineItems", x => x.LineItemId, "LineItems", "Id", ReferentialAction.Cascade);
                table.ForeignKey("FK_InventoryUnits_Variants", x => x.VariantId, "Variants", "Id");
                table.ForeignKey("FK_InventoryUnits_Shipments", x => x.ShipmentId, "Shipments", "Id");
            }
        );

        // Add indices
        migrationBuilder.CreateIndex("IX_InventoryUnits_OrderId", "InventoryUnits", "OrderId");
        migrationBuilder.CreateIndex("IX_InventoryUnits_LineItemId", "InventoryUnits", "LineItemId");
        migrationBuilder.CreateIndex("IX_InventoryUnits_ShipmentId", "InventoryUnits", "ShipmentId");
        migrationBuilder.CreateIndex("IX_InventoryUnits_OrderId_State", "InventoryUnits", new[] { "OrderId", "State" });

        // Drop Fulfillment tables
        migrationBuilder.DropTable(name: "FulfillmentLineItems");
        migrationBuilder.DropTable(name: "FulfillmentOrders");

        // Update Shipment columns if needed
        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "ShippedAt",
            table: "Shipments",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "DeliveredAt",
            table: "Shipments",
            nullable: true);

        migrationBuilder.AddColumn<DateTimeOffset>(
            name: "CanceledAt",
            table: "Shipments",
            nullable: true);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        // Reverse operations...
    }
}
```

---

## 10. Testing Example

**File**: `tests/Core.UnitTests/Domain/Orders/ShipmentTests.cs`

```csharp
[TestFixture]
public class ShipmentWorkflowTests
{
    [Test]
    public void Shipment_FullWorkflow_PendingToDelivered_Succeeds()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var shipmentResult = Shipment.Create(
            orderId: orderId,
            shippingMethodId: Guid.NewGuid(),
            stockLocationId: Guid.NewGuid()
        );
        var shipment = shipmentResult.Value;

        // Create inventory units
        var unit1 = InventoryUnit.Create(
            lineItemId: Guid.NewGuid(),
            variantId: Guid.NewGuid(),
            orderId: orderId,
            backordered: false
        );
        shipment.InventoryUnits.Add(unit1);

        // Act & Assert: Allocate
        var allocateResult = shipment.AllocateInventory();
        Assert.That(allocateResult.IsError, Is.False);
        Assert.That(shipment.State, Is.EqualTo(Shipment.ShipmentState.Ready));

        // Act & Assert: Ship
        var shipResult = shipment.Ship("1Z999AA10123456784");
        Assert.That(shipResult.IsError, Is.False);
        Assert.That(shipment.State, Is.EqualTo(Shipment.ShipmentState.Shipped));
        Assert.That(unit1.State, Is.EqualTo(InventoryUnit.InventoryUnitState.Shipped));

        // Act & Assert: Deliver
        var deliverResult = shipment.Deliver();
        Assert.That(deliverResult.IsError, Is.False);
        Assert.That(shipment.State, Is.EqualTo(Shipment.ShipmentState.Delivered));
        Assert.That(unit1.State, Is.EqualTo(InventoryUnit.InventoryUnitState.Received));
    }
}
```

---

## Summary of Changes

| Old Spree ReSys | New Spree-Aligned |
|---|---|
| Order + Shipment + FulfillmentOrder (3 aggregates) | Order + Shipment with InventoryUnits (2 aggregates) |
| FulfillmentLocationId on Order | Stock location determined during Shipment creation |
| OrderFulfillmentDomainService | CreateShipmentsService |
| Shipment.Events.Created → FulfillmentOrder.Created | Shipment.Events.InventoryAllocated (already in Shipment) |
| Complex state validation in Order.Complete() | Simple: Check payments + shipments ready |
| Tax complexity | Removed (or simplified to single TaxTotal) |
| Complex shipping calculators | Simple flat-rate pattern |

This aligns your codebase with Spree's battle-tested architecture while maintaining your DDD principles.
