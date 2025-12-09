using FluentAssertions;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Catalog.Products.Variants;
// Added for Product
using ReSys.Core.Domain.Stores.StockLocations;
using ReSys.Core.Domain.Stores;

using System.Reflection; // For reflection

namespace Core.UnitTests.Domain.Inventories.Locations;

public class StockLocationInvariantTests
{
    private StockLocation CreateTestStockLocation(string name = "Test Location")
    {
        var result = StockLocation.Create(name: name);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

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
    private StoreStockLocation CreateTestStoreStockLocation(StockLocation stockLocation, Store store, int priority = 1, bool canFulfillOrders = true)
    {
        var result = StoreStockLocation.Create(stockLocationId: stockLocation.Id, storeId: store.Id, priority: priority, canFulfillOrders: canFulfillOrders);
        result.IsError.Should().BeFalse(because: $"StoreStockLocation.Create should not fail for valid inputs: {result.FirstError.Description}");
        
        var storeStockLocation = result.Value;
        storeStockLocation.Should().NotBeNull(because: "StoreStockLocation object should not be null after successful creation."); // Explicit null check

        // Removed direct assignment of navigation properties
        // storeStockLocation.StockLocation = stockLocation;
        // storeStockLocation.Store = store; 
        
        return storeStockLocation;
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnSuccess_WhenStockLocationIsInConsistentState()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var productId1 = Guid.NewGuid();
        var variant1 = CreateTestVariant(productId: productId1, sku: "SKU1", trackInventory: true);
        var productId2 = Guid.NewGuid();
        var variant2 = CreateTestVariant(productId: productId2, sku: "SKU2", trackInventory: false);

        stockLocation.StockItems.Add(item: CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant1, quantityOnHand: 10, quantityReserved: 5, backorderable: true));
        stockLocation.StockItems.Add(item: CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant2, quantityOnHand: 5, quantityReserved: 2, backorderable: false));

        // Act
        var result = stockLocation.ValidateInvariants();

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenStockItemHasReservedExceedingOnHandAndNotBackorderable()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", trackInventory: false); // Not backorderable
        stockLocation.StockItems.Add(item: CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 5, quantityReserved: 10, backorderable: false)); // Inconsistent

        // Act
        var result = stockLocation.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockLocation.InvalidStockItemState");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenStockItemHasNegativeQuantityOnHand()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1");
        // Create a valid stock item then set QuantityOnHand to negative via reflection for testing invariant
        var stockItem = CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 10, quantityReserved: 0, backorderable: true);
        
        var quantityOnHandProperty = typeof(StockItem).GetProperty(name: nameof(StockItem.QuantityOnHand), bindingAttr: BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.NonPublic);
        quantityOnHandProperty.Should().NotBeNull(); // Ensure property is found
        quantityOnHandProperty.SetValue(obj: stockItem, value: -1);
        
        stockLocation.StockItems.Add(item: stockItem);

        // Act
        var result = stockLocation.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockLocation.NegativeQuantityOnHand");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenStockItemHasNegativeQuantityReserved()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1");
        // Create a valid stock item then set QuantityReserved to negative via reflection for testing invariant
        var stockItem = CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 10, quantityReserved: 5, backorderable: true);
        
        var quantityReservedProperty = typeof(StockItem).GetProperty(name: nameof(StockItem.QuantityReserved), bindingAttr: BindingFlags.Public | BindingFlags.Instance | BindingFlags.SetProperty | BindingFlags.NonPublic);
        quantityReservedProperty.Should().NotBeNull(); // Ensure property is found
        quantityReservedProperty.SetValue(obj: stockItem, value: -5);
        
        stockLocation.StockItems.Add(item: stockItem);

        // Act
        var result = stockLocation.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockLocation.NegativeQuantityReserved");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenStoreLinkageIsInvalid()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var storeResult = Store.Create(name: "Test Store", mailFromAddress: "test@store.com", customerSupportEmail: "test@store.com");
        storeResult.IsError.Should().BeFalse();
        var store = storeResult.Value;

        // Directly create an invalid StoreStockLocation
        var invalidStoreStockLocationResult = StoreStockLocation.Create(stockLocationId: Guid.NewGuid(), storeId: store.Id);
        invalidStoreStockLocationResult.IsError.Should().BeFalse();
        var invalidStoreStockLocation = invalidStoreStockLocationResult.Value;
        // invalidStoreStockLocation.StockLocation = stockLocation; // Removed to fix NRE
        // invalidStoreStockLocation.Store = store; // Removed to fix NRE

        stockLocation.StoreStockLocations.Add(item: invalidStoreStockLocation);

        // Act
        var result = stockLocation.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockLocation.InvalidStoreLinkage");
    }
}