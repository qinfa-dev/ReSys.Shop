using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Orders;

namespace Core.UnitTests.Domain.Inventories.Stocks;

public class StockMovementTests
{
    private Variant CreateTestVariant(Guid productId, string sku)
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
        variant.Product = product;
        var priceResult = variant.SetPrice(amount: 1000m, currency: "USD");
        priceResult.IsError.Should().BeFalse(because: $"Failed to set price for variant: {priceResult.FirstError.Description}");
        return variant;
    }

    private StockItem CreateTestStockItem(
        Guid variantId,
        Guid stockLocationId,
        string sku,
        int quantityOnHand = 0,
        int quantityReserved = 0,
        bool backorderable = true)
    {
        var result = StockItem.Create(variantId: variantId, stockLocationId: stockLocationId, sku: sku, quantityOnHand: quantityOnHand, quantityReserved: quantityReserved, backorderable: backorderable);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // --- Create Factory Method Tests ---
    [Fact]
    public void Create_ShouldReturnStockMovement_WithValidParameters()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_SM_001");
        var stockItem = CreateTestStockItem(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: true);
        int quantity = 5;
        var originator = StockMovement.MovementOriginator.Adjustment;
        var action = StockMovement.MovementAction.Received;
        string reason = "Initial stock";
        Guid? originatorId = Guid.NewGuid();

        // Act
        var result = StockMovement.Create(stockItemId: stockItem.Id, quantity: quantity, originator: originator, action: action, reason: reason, originatorId: originatorId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.StockItemId.Should().Be(expected: stockItem.Id);
        result.Value.Quantity.Should().Be(expected: quantity);
        result.Value.Originator.Should().Be(expected: originator);
        result.Value.Action.Should().Be(expected: action);
        result.Value.Reason.Should().Be(expected: reason);
        result.Value.OriginatorId.Should().Be(expected: originatorId);
        result.Value.IsIncrease.Should().BeTrue();
        result.Value.IsDecrease.Should().BeFalse();
    }

    [Fact]
    public void Create_ShouldReturnStockMovement_ForNegativeQuantity()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_SM_002");
        var stockItem = CreateTestStockItem(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: true);
        int quantity = -3; // Negative quantity
        var originator = StockMovement.MovementOriginator.Damage;
        var action = StockMovement.MovementAction.Damaged;

        // Act
        var result = StockMovement.Create(stockItemId: stockItem.Id, quantity: quantity, originator: originator, action: action);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Quantity.Should().Be(expected: quantity);
        result.Value.IsIncrease.Should().BeFalse();
        result.Value.IsDecrease.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldReturnError_WhenQuantityIsZero()
    {
        // Arrange
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_SM_003");
        var stockItem = CreateTestStockItem(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 10, quantityReserved: 0, backorderable: true);
        int quantity = 0; // Invalid quantity
        var originator = StockMovement.MovementOriginator.Adjustment;
        var action = StockMovement.MovementAction.Adjustment;

        // Act
        var result = StockMovement.Create(stockItemId: stockItem.Id, quantity: quantity, originator: originator, action: action);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockMovement.Errors.InvalidQuantity.Code);
    }
}
