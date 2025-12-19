using ErrorOr;

using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Movements;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Orders.Shipments; 

namespace Core.UnitTests.Domain.Inventories.Stocks;

public class StockItemTests
{
    private Variant CreateTestVariant(Guid productId, string sku, bool trackInventory = true) // Removed backorderable
    {
        var productResult = Product.Create(
            name: "Test Product Name",
            slug: $"test-product-{Guid.NewGuid()}",
            isDigital: !trackInventory);
        productResult.IsError.Should().BeFalse();
        var product = productResult.Value;

        var variantResult = Variant.Create(
            productId: productId,
            sku: sku,
            trackInventory: trackInventory,
            isMaster: false); // Removed backorderable from call
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Product = product;
        var priceResult = variant.SetPrice(amount: 1000m, currency: "USD");
        priceResult.IsError.Should().BeFalse(because: $"Failed to set price for variant: {priceResult.FirstError.Description}");
        return variant;
    }

    private InventoryUnit CreateTestInventoryUnit(Guid variantId, Guid lineItemId, InventoryUnit.InventoryUnitState state = InventoryUnit.InventoryUnitState.Backordered)
    {
        var result = InventoryUnit.Create(variantId: variantId, lineItemId: lineItemId, shipmentId: Guid.NewGuid());
        
        if (result.IsError || result.Value is null)
        {
            var errorDescription = result.IsError ? result.FirstError.Description : "Value was null after successful creation.";
            throw new InvalidOperationException(message: $"Failed to create InventoryUnit for testing: {errorDescription}");
        }
        
        var unit = result.Value;
        
        // Use reflection to set state if not OnHand, as Factory creates OnHand
        if (state != InventoryUnit.InventoryUnitState.OnHand)
        {
            var stateProperty = typeof(InventoryUnit).GetProperty(name: nameof(InventoryUnit.State));
            stateProperty?.SetValue(obj: unit, value: state);
        }
        return unit;
    }

    // --- Create Factory Method Tests ---
    [Fact]
    public void Create_ShouldReturnStockItem_WithValidParameters()
    {
        // Arrange
        Guid variantId = Guid.NewGuid();
        Guid stockLocationId = Guid.NewGuid();
        string sku = "TESTSKU001";
        int quantityOnHand = 10;
        int quantityReserved = 5;
        bool backorderable = true;

        // Act
        var result = StockItem.Create(variantId: variantId, stockLocationId: stockLocationId, sku: sku, quantityOnHand: quantityOnHand, quantityReserved: quantityReserved, backorderable: backorderable);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.VariantId.Should().Be(expected: variantId);
        result.Value.StockLocationId.Should().Be(expected: stockLocationId);
        result.Value.Sku.Should().Be(expected: sku);
        result.Value.QuantityOnHand.Should().Be(expected: quantityOnHand);
        result.Value.QuantityReserved.Should().Be(expected: quantityReserved);
        result.Value.Backorderable.Should().Be(expected: backorderable);
        result.Value.CountAvailable.Should().Be(expected: quantityOnHand - quantityReserved);
        result.Value.InStock.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockItemCreated);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenQuantityOnHandIsNegative()
    {
        // Arrange
        Guid variantId = Guid.NewGuid();
        Guid stockLocationId = Guid.NewGuid();
        string sku = "TESTSKU002";
        int quantityOnHand = -5; // Invalid

        // Act
        var result = StockItem.Create(variantId: variantId, stockLocationId: stockLocationId, sku: sku, quantityOnHand: quantityOnHand);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockItem.Errors.InvalidQuantity.Code);
    }

    // --- Adjust Method Tests ---
    [Fact]
    public void Adjust_ShouldIncreaseQuantityOnHand_ForPositiveAdjustment()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_ADJ_POS");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: true).Value;
        stockItem.ClearDomainEvents();
        int adjustment = 5;

        // Act
        var result = stockItem.Adjust(quantity: adjustment, originator: StockMovement.MovementOriginator.Adjustment, reason: "Restock");

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.QuantityOnHand.Should().Be(expected: 15);
        stockItem.StockMovements.Should().ContainSingle(predicate: sm => sm.Quantity == adjustment);
        stockItem.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockAdjusted);
        var adjustedEvent = (StockItem.Events.StockAdjusted)stockItem.DomainEvents.First();
        adjustedEvent.Quantity.Should().Be(expected: adjustment);
        adjustedEvent.NewCount.Should().Be(expected: 15);
    }

    [Fact]
    public void Adjust_ShouldDecreaseQuantityOnHand_ForNegativeAdjustment()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_ADJ_NEG");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: true).Value;
        stockItem.ClearDomainEvents();
        int adjustment = -3;

        // Act
        var result = stockItem.Adjust(quantity: adjustment, originator: StockMovement.MovementOriginator.Adjustment, reason: "Damage");

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.QuantityOnHand.Should().Be(expected: 7);
        stockItem.StockMovements.Should().ContainSingle(predicate: sm => sm.Quantity == adjustment);
        stockItem.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockAdjusted);
    }

    [Fact]
    public void Adjust_ShouldReturnError_WhenResultingQuantityOnHandIsNegative()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_ADJ_ERROR");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 5, quantityReserved: 0, backorderable: true).Value;
        stockItem.ClearDomainEvents();
        int adjustment = -10;

        // Act
        var result = stockItem.Adjust(quantity: adjustment, originator: StockMovement.MovementOriginator.Adjustment, reason: "Loss");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockItem.Errors.InsufficientStock(available: 0,requested: 0).Code);
        stockItem.QuantityOnHand.Should().Be(expected: 5); // Quantity should not change
        stockItem.StockMovements.Should().BeEmpty();
        stockItem.DomainEvents.Should().BeEmpty();
    }


    // --- Reserve Method Tests ---
    [Fact]
    public void Reserve_ShouldIncreaseQuantityReserved_WhenSufficientAvailableStock()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_RES_OK");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: false).Value; // Not backorderable
        stockItem.ClearDomainEvents();
        int quantityToReserve = 5;
        var orderId = Guid.NewGuid();

        // Act
        var result = stockItem.Reserve(quantity: quantityToReserve, orderId: orderId);

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.QuantityReserved.Should().Be(expected: 5);
        stockItem.QuantityOnHand.Should().Be(expected: 10); // Should not change
        stockItem.CountAvailable.Should().Be(expected: 5);
        stockItem.StockMovements.Should().ContainSingle(predicate: sm => sm.Quantity == -quantityToReserve && sm.Action == StockMovement.MovementAction.Reserved);
        stockItem.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockReserved);
    }

    [Fact]
    public void Reserve_ShouldBeIdempotent_WhenSameQuantityReservedForSameOrder()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_RES_IDEMPOTENT");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: false).Value;
        stockItem.ClearDomainEvents();
        int quantityToReserve = 5;
        var orderId = Guid.NewGuid();

        // Act
        var firstReserveResult = stockItem.Reserve(quantity: quantityToReserve, orderId: orderId);
        stockItem.ClearDomainEvents(); // Clear events from first reserve
        var secondReserveResult = stockItem.Reserve(quantity: quantityToReserve, orderId: orderId);

        // Assert
        firstReserveResult.IsError.Should().BeFalse();
        secondReserveResult.IsError.Should().BeFalse();
        stockItem.QuantityReserved.Should().Be(expected: 5); // Should not increase
        stockItem.CountAvailable.Should().Be(expected: 5);
        stockItem.StockMovements.Should().ContainSingle(predicate: sm => sm.Quantity == -quantityToReserve); // Only one movement
        stockItem.DomainEvents.Should().BeEmpty(); // No new events for idempotent call
    }

    [Fact]
    public void Reserve_ShouldReturnError_WhenDifferentQuantityReservedForSameOrder()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_RES_CONFLICT");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: false).Value;
        stockItem.ClearDomainEvents();
        var orderId = Guid.NewGuid();

        // Act
        var firstReserveResult = stockItem.Reserve(quantity: 5, orderId: orderId);
        stockItem.ClearDomainEvents();
        var secondReserveResult = stockItem.Reserve(quantity: 3, orderId: orderId); // Different quantity

        // Assert
        firstReserveResult.IsError.Should().BeFalse();
        secondReserveResult.IsError.Should().BeTrue();
        secondReserveResult.FirstError.Code.Should().Be(expected: StockItem.Errors.DuplicateReservation(orderId, 5, 3).Code);
        stockItem.QuantityReserved.Should().Be(expected: 5); // Quantity should not change from first reserve
        stockItem.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reserve_ShouldReturnError_WhenInsufficientAvailableStockAndNotBackorderable()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_RES_ERROR"); // Removed backorderable
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 5, quantityReserved: 0, backorderable: false).Value; // Not backorderable
        stockItem.ClearDomainEvents();
        int quantityToReserve = 10;
        var orderId = Guid.NewGuid();

        // Act
        var result = stockItem.Reserve(quantity: quantityToReserve, orderId: orderId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockItem.Errors.InsufficientStock(available: 0,requested: 0).Code);
        stockItem.QuantityReserved.Should().Be(expected: 0);
        stockItem.CountAvailable.Should().Be(expected: 5);
        stockItem.StockMovements.Should().BeEmpty();
        stockItem.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Reserve_ShouldSucceed_WhenInsufficientAvailableStockButBackorderable()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_RES_BACKORDER"); // Removed backorderable
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 5, quantityReserved: 0, backorderable: true).Value; // Backorderable
        stockItem.ClearDomainEvents();
        int quantityToReserve = 10;
        var orderId = Guid.NewGuid();

        // Act
        var result = stockItem.Reserve(quantity: quantityToReserve, orderId: orderId);

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.QuantityReserved.Should().Be(expected: 10);
        stockItem.QuantityOnHand.Should().Be(expected: 5);
        stockItem.CountAvailable.Should().Be(expected: 0); // CountAvailable is Math.Max(0, ...) so it cannot be negative
        stockItem.StockMovements.Should().ContainSingle(predicate: sm => sm.Quantity == -quantityToReserve);
        stockItem.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockReserved);
    }

    // --- Release Method Tests ---
    [Fact]
    public void Release_ShouldDecreaseQuantityReserved_WhenValidQuantity()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_REL_OK");
        var orderId = Guid.NewGuid();
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 5, backorderable: true).Value;
        stockItem.ClearDomainEvents();
        int quantityToRelease = 3;

        // Act
        var result = stockItem.Release(quantity: quantityToRelease, orderId: orderId);

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.QuantityReserved.Should().Be(expected: 2);
        stockItem.CountAvailable.Should().Be(expected: 8);
        stockItem.StockMovements.Should().ContainSingle(predicate: sm => sm.Quantity == quantityToRelease && sm.Action == StockMovement.MovementAction.Released);
        stockItem.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockReleased);
    }

    [Fact]
    public void Release_ShouldReturnError_WhenQuantityExceedsReserved()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_REL_ERROR");
        var orderId = Guid.NewGuid();
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 5, backorderable: true).Value;
        stockItem.ClearDomainEvents();
        int quantityToRelease = 10;

        // Act
        var result = stockItem.Release(quantity: quantityToRelease, orderId: orderId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockItem.Errors.InvalidRelease(reserved: 5,releaseRequested: 10).Code);
        stockItem.QuantityReserved.Should().Be(expected: 5); // No change
        stockItem.DomainEvents.Should().BeEmpty();
    }

    // --- ConfirmShipment Method Tests ---
    [Fact]
    public void ConfirmShipment_ShouldDecreaseQuantityOnHandAndReserved_WhenValid()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_SHIP_OK");
        var orderId = Guid.NewGuid();
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 5, backorderable: true).Value;
        stockItem.ClearDomainEvents();
        int quantityToShip = 3;

        // Act
        var result = stockItem.ConfirmShipment(quantity: quantityToShip, shipmentId: Guid.NewGuid(), orderId: orderId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Deleted); // ConfirmShipment returns Result.Deleted on success
        stockItem.QuantityOnHand.Should().Be(expected: 7);
        stockItem.QuantityReserved.Should().Be(expected: 2);
        stockItem.StockMovements.Should().ContainSingle(predicate: sm => sm.Quantity == -quantityToShip && sm.Action == StockMovement.MovementAction.Sold);
        stockItem.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockShipped);
    }

    [Fact]
    public void ConfirmShipment_ShouldReturnError_WhenQuantityExceedsReserved()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_SHIP_ERROR");
        var orderId = Guid.NewGuid();
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 5, backorderable: true).Value;
        stockItem.ClearDomainEvents();
        int quantityToShip = 10;

        // Act
        var result = stockItem.ConfirmShipment(quantity: quantityToShip, shipmentId: Guid.NewGuid(), orderId: orderId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockItem.Errors.InvalidShipment(reserved: 5,shipRequested: 10).Code);
        stockItem.QuantityOnHand.Should().Be(expected: 10); // No change
        stockItem.QuantityReserved.Should().Be(expected: 5); // No change
        stockItem.DomainEvents.Should().BeEmpty();
    }

    // --- Lifecycle Delete Method Tests ---
    [Fact]
    public void Delete_ShouldReturnDeletedResultAndRaiseEvent()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_DEL");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 1, quantityReserved: 0, backorderable: true).Value;
        stockItem.ClearDomainEvents();

        // Act
        var result = stockItem.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Deleted);
        stockItem.DomainEvents.Should().ContainSingle(predicate: e => e is StockItem.Events.StockItemDeleted);
    }
    
    // --- Update Method Tests ---
    [Fact]
    public void Update_ShouldUpdatePropertiesAndAdjustQuantities_WhenQuantitiesChanged()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_UPD_QTY");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 5, backorderable: true).Value;
        stockItem.ClearDomainEvents();

        // Act
        var result = stockItem.Update(
            variantId: variant.Id,
            stockLocationId: stockItem.StockLocationId,
            sku: "NEW_SKU",
            backorderable: false,
            quantityOnHand: 12, // Increased by 2
            quantityReserved: 4); // Decreased by 1

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.Sku.Should().Be(expected: "NEW_SKU");
        stockItem.Backorderable.Should().BeFalse();
        stockItem.QuantityOnHand.Should().Be(expected: 12);
        stockItem.QuantityReserved.Should().Be(expected: 4);
        stockItem.DomainEvents.Should().Contain(predicate: e => e is StockItem.Events.StockItemUpdated);
        stockItem.DomainEvents.Should().Contain(predicate: e => e is StockItem.Events.StockAdjusted);
        stockItem.DomainEvents.Should().Contain(predicate: e => e is StockItem.Events.StockReleased); // For decreased reserved
    }

    [Fact]
    public void Update_ShouldNotRaiseEvent_WhenNoEffectiveChanges()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_UPD_NO_CHANGE");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 5, backorderable: true).Value;
        stockItem.ClearDomainEvents();

        // Act
        var result = stockItem.Update(
            variantId: variant.Id,
            stockLocationId: stockItem.StockLocationId,
            sku: variant.Sku ?? string.Empty,
            backorderable: true,
            quantityOnHand: 10,
            quantityReserved: 5);

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.DomainEvents.Should().BeEmpty();
    }
}
