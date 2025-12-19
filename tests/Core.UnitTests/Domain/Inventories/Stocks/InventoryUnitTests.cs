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

    [Fact]
    public void Create_ShouldCreateSingleUnitWithCorrectQuantity_WithValidParameters()
    {
        // Arrange
        Guid variantId = Guid.NewGuid();
        Guid lineItemId = Guid.NewGuid();
        Guid shipmentId = Guid.NewGuid();

        // Act
        var result = Create(variantId: variantId, lineItemId: lineItemId, shipmentId: shipmentId);

        // Assert
        result.IsError.Should().BeFalse();
        var unit = result.Value;
        unit.VariantId.Should().Be(expected: variantId);
        unit.LineItemId.Should().Be(expected: lineItemId);
        unit.ShipmentId.Should().Be(expected: shipmentId); // Assert new parameter
        unit.State.Should().Be(expected: InventoryUnitState.OnHand);
        unit.DomainEvents.Should().ContainSingle(predicate: e => e.GetType() == typeof(Events.Created));
    }

    // --- State Transition Tests ---
    [Fact]
    public void FillBackorder_ShouldTransitionFromBackorderedToOnHand()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid(), initialState: InventoryUnitState.Backordered).Value;
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
        var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid()).Value;
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
        var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid()).Value;
        unit.Ship(); // Transition to shipped state
        unit.ClearDomainEvents();

        // Act
        var result = unit.FillBackorder();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: Errors.InvalidStateTransition(from: InventoryUnitState.Shipped, to: InventoryUnitState.OnHand).Code);
        unit.State.Should().Be(expected: InventoryUnitState.Shipped); // State should not change
    }

    [Fact]
    public void Ship_ShouldTransitionFromOnHandToShipped()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid()).Value;
        unit.ClearDomainEvents();

        // Act
        var result = unit.Ship();

        // Assert
        result.IsError.Should().BeFalse();
        unit.State.Should().Be(expected: InventoryUnitState.Shipped);
        unit.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Shipped);
    }

    [Fact]
    public void Ship_ShouldReturnError_WhenNotInShippableState()
    {
        // Arrange
        var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid()).Value;
        unit.Ship();
        unit.Cancel();
        //unit.Return(); // now in Returned state
        unit.ClearDomainEvents();

        // Act
        var result = unit.Ship();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: Errors.InvalidStateTransition(InventoryUnitState.Canceled, InventoryUnitState.Shipped).Code);
        unit.State.Should().Be(expected: InventoryUnitState.Canceled); // State should not change
    }

    //[Fact]
    //public void Return_ShouldTransitionFromShippedToReturned()
    //{
    //    // Arrange
    //    var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid()).Value;
    //    unit.Ship(); // Must be shipped to return
    //    unit.ClearDomainEvents();

    //    // Act
    //    var result = unit.Return();

    //    // Assert
    //    result.IsError.Should().BeFalse();
    //    unit.State.Should().Be(expected: InventoryUnitState.Returned);
    //    unit.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Returned);
    //}

    //[Fact]
    //public void Return_ShouldReturnError_WhenNotShipped()
    //{
    //    // Arrange
    //    var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid()).Value;
    //    unit.ClearDomainEvents();

    //    // Act
    //    var result = unit.Return();

    //    // Assert
    //    result.IsError.Should().BeTrue();
    //    result.FirstError.Code.Should().Be(expected: Errors.CannotReturnNonShipped.Code);
    //    unit.State.Should().Be(expected: InventoryUnitState.OnHand); // State should not change
    //}

    //[Fact]
    //public void Return_ShouldReturnError_WhenAlreadyReturned()
    //{
    //    // Arrange
    //    var unit = Create(variantId: Guid.NewGuid(), lineItemId: Guid.NewGuid(), shipmentId: Guid.NewGuid()).Value;
    //    unit.Ship();
    //    unit.Return(); // Already returned
    //    unit.ClearDomainEvents();

    //    // Act
    //    var result = unit.Return();

    //    // Assert
    //    result.IsError.Should().BeTrue();
    //    result.FirstError.Code.Should().Be(expected: Errors.AlreadyReturned.Code);
    //    unit.State.Should().Be(expected: InventoryUnitState.Returned); // State should not change
    //}

    // --- Query Tests ---

}