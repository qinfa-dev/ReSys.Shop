using FluentAssertions;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Catalog.Products.Variants;

using System.Reflection; // Added for BindingFlags

namespace Core.UnitTests.Domain.Inventories.Stocks;

public class StockItemInvariantTests
{
    private Variant CreateTestVariant(Guid productId, string sku, bool trackInventory = true)
    {
        // Create a real Product instance
        var productResult = ReSys.Core.Domain.Catalog.Products.Product.Create(
            name: "Test Product Name",
            slug: $"test-product-{Guid.NewGuid()}",
            isDigital: !trackInventory);
        productResult.IsError.Should().BeFalse();
        var product = productResult.Value;

        var variantResult = Variant.Create(
            productId: productId,
            sku: sku,
            trackInventory: trackInventory,
            isMaster: false); // Assuming non-master for these tests
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Product = product; // Assign the created product
        var priceResult = variant.SetPrice(amount: 1000m, currency: "USD");
        priceResult.IsError.Should().BeFalse(because: $"Failed to set price for variant: {priceResult.FirstError.Description}");
        return variant;
    }

    private StockItem CreateTestStockItem(
        Guid stockLocationId, 
        Variant variant, 
        int quantityOnHand, 
        int quantityReserved, 
        bool backorderable)
    {
        var result = StockItem.Create(
            variantId: variant.Id, 
            stockLocationId: stockLocationId, 
            sku: variant.Sku ?? string.Empty,
            quantityOnHand: quantityOnHand, 
            quantityReserved: quantityReserved, 
            backorderable: backorderable);
        result.IsError.Should().BeFalse(); // Ensure creation is successful for helper
        var stockItem = result.Value;
        stockItem.Variant = variant; // Assign the variant navigation property
        return stockItem;
    }
    [Fact]
    public void ValidateInvariants_ShouldReturnSuccess_WhenStockItemIsInConsistentState()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", trackInventory: true);
        var stockItem = CreateTestStockItem(stockLocationId: Guid.NewGuid(), variant: variant, quantityOnHand: 10, quantityReserved: 5, backorderable: true);
        // Act
        var result = stockItem.ValidateInvariants();

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenQuantityReservedIsNegative()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1");
        // Create a valid stock item then set QuantityReserved to negative via reflection for testing invariant
        var stockItem = CreateTestStockItem(stockLocationId: Guid.NewGuid(), variant: variant, quantityOnHand: 10, quantityReserved: 5, backorderable: true);
        
        var quantityReservedProperty = typeof(StockItem).GetProperty(name: nameof(StockItem.QuantityReserved), bindingAttr: BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.NonPublic);
        quantityReservedProperty.Should().NotBeNull();
        quantityReservedProperty.SetValue(obj: stockItem, value: -1); // Deliberately inconsistent, corrected call
        
        // Act
        var result = stockItem.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockItem.NegativeReserved");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenReservedExceedsOnHandAndNotBackorderable()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", trackInventory: false); // Not backorderable
        var stockItem = CreateTestStockItem(stockLocationId: Guid.NewGuid(), variant: variant, quantityOnHand: 5, quantityReserved: 10, backorderable: false); // Inconsistent
        // Act
        var result = stockItem.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockItem.ReservedExceedsOnHand");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnSuccess_WhenReservedExceedsOnHandAndIsBackorderable()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", trackInventory: true); // Backorderable
        var stockItem = CreateTestStockItem(stockLocationId: Guid.NewGuid(), variant: variant, quantityOnHand: 5, quantityReserved: 10, backorderable: true); // Inconsistent but allowed for backorderable
        // Act
        var result = stockItem.ValidateInvariants();

        // Assert
        result.IsError.Should().BeFalse();
    }
}