using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;

using static ReSys.Core.Domain.Orders.Shipments.InventoryUnit;

namespace Core.UnitTests.Domain.Inventories.Stocks;

public class InventoryUnitTests
{
    private Variant CreateTestVariant(Guid id, Guid productId, string sku)
    {
        var productResult = Product.Create(
            name: "Test Product Name",
            slug: $"test-product-{Guid.NewGuid()}",
            isDigital: false);
        productResult.IsError.Should().BeFalse();
        var product = productResult.Value;

        var variantResult = Variant.Create(
            productId: productId,
            sku: sku,
            trackInventory: true,
            isMaster: false);
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Id = id;
        variant.Product = product;
        var priceResult = variant.SetPrice(amount: 1000m, currency: "USD");
        priceResult.IsError.Should().BeFalse(because: $"Failed to set price for variant: {priceResult.FirstError.Description}");
        return variant;
    }

    private StockLocation CreateTestStockLocation(Guid id, string name = "Test Location")
    {
        var result = StockLocation.Create(name: name);
        result.Value.Id = id;
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // --- Create Factory Method Tests ---
    [Fact]
    public void Create_ShouldCreateSingleUnitWithCorrectQuantity_WithValidParameters()
    {
        // Arrange
        Guid variantId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        Guid lineItemId = Guid.NewGuid();
        Guid shipmentId = Guid.NewGuid(); // New required parameter
        int quantity = 3;

        // Act
        var result = Create(variantId: variantId, orderId: orderId, lineItemId: lineItemId, shipmentId: shipmentId, quantity: quantity);

        // Assert
        result.IsError.Should().BeFalse();
        var unit = result.Value;
        unit.VariantId.Should().Be(expected: variantId);
        unit.OrderId.Should().Be(expected: orderId);
        unit.LineItemId.Should().Be(expected: lineItemId);
        unit.ShipmentId.Should().Be(expected: shipmentId); // Assert new parameter
        unit.Quantity.Should().Be(expected: quantity);
        unit.State.Should().Be(expected: InventoryUnitState.OnHand);
        unit.DomainEvents.Should().ContainSingle(predicate: e => e.GetType() == typeof(Events.Created) && ((Events.Created)e).Quantity == quantity);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenQuantityIsZeroOrNegative()
    {
        // Arrange
        Guid variantId = Guid.NewGuid();
        Guid orderId = Guid.NewGuid();
        Guid lineItemId = Guid.NewGuid();
        Guid shipmentId = Guid.NewGuid(); // New required parameter
        int quantity = 0; // Invalid

        // Act
        var result = Create(variantId: variantId, orderId: orderId, lineItemId: lineItemId, shipmentId: shipmentId, quantity: quantity);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "InventoryUnit.InvalidQuantity");
    }

    // --- State Transition Tests ---
    [Fact]
    public void FillBackorder_ShouldTransitionFromBackorderedToOnHand()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.State = InventoryUnitState.Backordered;
        unit.ClearDomainEvents();

        // Act
        var result = unit.FillBackorder();

        // Assert
        result.IsError.Should().BeFalse();
        unit.State.Should().Be(expected: InventoryUnitState.OnHand);
        unit.DomainEvents.Should().ContainSingle(predicate: e => e is Events.BackorderFilled);
    }

    [Fact]
    public void FillBackorder_ShouldBeIdempotent_WhenAlreadyOnHand()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.State = InventoryUnitState.OnHand;
        unit.ClearDomainEvents();

        // Act
        var result = unit.FillBackorder();

        // Assert
        result.IsError.Should().BeFalse();
        unit.State.Should().Be(expected: InventoryUnitState.OnHand);
        unit.DomainEvents.Should().BeEmpty(); // No event raised
    }

    [Fact]
    public void FillBackorder_ShouldReturnError_WhenNotInBackorderedState()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.State = InventoryUnitState.Shipped; // Invalid state
        unit.ClearDomainEvents();

        // Act
        var result = unit.FillBackorder();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: Errors.InvalidStateTransition(from: InventoryUnitState.Shipped, to: InventoryUnitState.OnHand).Code);
        unit.State.Should().Be(expected: InventoryUnitState.Shipped); // State should not change
    }

    [Fact]
    public void TransitionToShipped_ShouldTransitionFromOnHandToShipped()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.StockLocationId = Guid.NewGuid(); // Needed for successful shipment event
        unit.ClearDomainEvents();

        // Act
        var result = unit.TransitionToShipped();

        // Assert
        result.IsError.Should().BeFalse();
        unit.State.Should().Be(expected: InventoryUnitState.Shipped);
        unit.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Shipped);
    }

    [Fact]
    public void TransitionToShipped_ShouldReturnError_WhenNotInOnHandState()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.State = InventoryUnitState.Backordered; // Invalid state
        unit.ClearDomainEvents();

        // Act
        var result = unit.TransitionToShipped();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: Errors.InvalidStateTransition(InventoryUnitState.Backordered, InventoryUnitState.Shipped).Code);
        unit.State.Should().Be(expected: InventoryUnitState.Backordered); // State should not change
    }

    [Fact]
    public void Return_ShouldTransitionFromShippedToReturned()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.State = InventoryUnitState.Shipped; // Must be shipped to return
        unit.ClearDomainEvents();

        // Act
        var result = unit.Return();

        // Assert
        result.IsError.Should().BeFalse();
        unit.State.Should().Be(expected: InventoryUnitState.Returned);
        unit.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Returned);
    }

    [Fact]
    public void Return_ShouldReturnError_WhenNotShipped()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.State = InventoryUnitState.OnHand; // Invalid state
        unit.ClearDomainEvents();

        // Act
        var result = unit.Return();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: Errors.CannotReturnFromNonShipped.Code);
        unit.State.Should().Be(expected: InventoryUnitState.OnHand); // State should not change
    }

    [Fact]
    public void Return_ShouldReturnError_WhenAlreadyReturned()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        unit.State = InventoryUnitState.Returned; // Already returned
        unit.ClearDomainEvents();

        // Act
        var result = unit.Return();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: Errors.AlreadyReturned.Code);
        unit.State.Should().Be(expected: InventoryUnitState.Returned); // State should not change
    }

    // --- SetStockLocation Tests ---
    [Fact]
    public void SetStockLocation_ShouldUpdateStockLocationId()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        var newStockLocation = CreateTestStockLocation(id: Guid.NewGuid());
        unit.ClearDomainEvents();

        // Act
        var result = unit.SetStockLocation(stockLocation: newStockLocation);

        // Assert
        result.IsError.Should().BeFalse();
        unit.StockLocationId.Should().Be(expected: newStockLocation.Id);
        unit.DomainEvents.Should().ContainSingle(predicate: e => e is Events.StockLocationAssigned);
    }

    [Fact]
    public void SetStockLocation_ShouldBeIdempotent_WhenSameLocationAssigned()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        var stockLocation = CreateTestStockLocation(id: Guid.NewGuid());
        unit.StockLocationId = stockLocation.Id; // Pre-set
        unit.ClearDomainEvents();

        // Act
        var result = unit.SetStockLocation(stockLocation: stockLocation);

        // Assert
        result.IsError.Should().BeFalse();
        unit.StockLocationId.Should().Be(expected: stockLocation.Id);
        unit.DomainEvents.Should().BeEmpty();
    }
    
    // --- Query Tests ---
    [Fact]
    public void IsInTerminalState_ShouldReturnTrue_ForReturned()
    {
        // Arrange
        var returnedUnit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        returnedUnit.State = InventoryUnitState.Returned;

        var onHandUnit = Create(variantId: Guid.NewGuid(), orderId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), quantity: 1).Value;
        onHandUnit.State = InventoryUnitState.OnHand;

        // Assert
        returnedUnit.IsInTerminalState.Should().BeTrue();
        onHandUnit.IsInTerminalState.Should().BeFalse();
    }
}