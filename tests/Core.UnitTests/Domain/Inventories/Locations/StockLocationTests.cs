using ErrorOr;
using FluentAssertions;
using NSubstitute;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Domain.Stores.StockLocations;
using System.Reflection;
using Xunit;
using Humanizer; // Added for .ToSlug() and .Humanize()
using ReSys.Core.Common.Extensions; // Added for .ToSlug() and .Humanize()

namespace Core.UnitTests.Domain.Inventories.Locations;

public class StockLocationTests
{
    private StockLocation CreateTestStockLocation(string name = "Test Location", bool isDefault = false)
    {
        var result = StockLocation.Create(name: name, isDefault: isDefault);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    private Variant CreateTestVariant(Guid productId, string sku, bool trackInventory = true)
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
            isMaster: false);
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Product = product;
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
            backorderable: backorderable);result.IsError.Should().BeFalse();
        var stockItem = result.Value;
        stockItem.Variant = variant; // Assign the variant navigation property
        return stockItem;
    }

    private Store CreateTestStore(string name = "Test Store")
    {
        var result = Store.Create(name: name, mailFromAddress: "test@store.com", customerSupportEmail: "test@store.com");
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // --- Create Factory Method Tests ---
    [Fact]
    public void Create_ShouldReturnStockLocation_WithValidParameters()
    {
        // Arrange
        string name = "Warehouse A";

        // Act
        var result = StockLocation.Create(name: name);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(expected: name.Trim().ToSlug()); // Assert slugified name
        result.Value.Presentation.Should().Be(expected: name.Trim().ToSlug().Humanize(casing: LetterCasing.Title)); // Assert humanized form of slug
        result.Value.Active.Should().BeTrue();
        result.Value.Default.Should().BeFalse();
        result.Value.Id.Should().NotBeEmpty();
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockLocation.Events.Created);
    }

    [Fact]
    public void Create_ShouldReturnStockLocation_WithOptionalParameters()
    {
        // Arrange
        string name = "Warehouse B";
        string presentation = "B Warehouse";
        bool active = false;
        bool isDefault = true;
        Guid countryId = Guid.NewGuid();
        string address1 = "123 Test St";
        string city = "Testville";

        // Act
        var result = StockLocation.Create(
            name: name,
            presentation: presentation,
            active: active,
            isDefault: isDefault,
            countryId: countryId,
            address1: address1,
            city: city);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(expected: name.Trim().ToSlug()); // Assert slugified name
        result.Value.Presentation.Should().Be(expected: presentation.Trim()); // Presentation is directly trimmed, not humanized if provided
        result.Value.Active.Should().Be(expected: active);
        result.Value.Default.Should().Be(expected: isDefault);
        result.Value.CountryId.Should().Be(expected: countryId);
        result.Value.Address1.Should().Be(expected: address1);
        result.Value.City.Should().Be(expected: city);
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockLocation.Events.Created);
    }

    // --- Update Method Tests ---
    [Fact]
    public void Update_ShouldUpdateProperties_WhenValidParametersProvided()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        string newName = "New Warehouse Name";
        string newCity = "New City";

        // Act
        var result = stockLocation.Update(name: newName, city: newCity, active: false);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(expected: newName.Trim().ToSlug()); // Assert slugified name
        result.Value.City.Should().Be(expected: newCity); // City is not normalized
        result.Value.Active.Should().BeFalse();
        result.Value.DomainEvents.Should().Contain(predicate: e => e is StockLocation.Events.Updated);
    }

    [Fact]
    public void Update_ShouldNotChangeProperties_WhenNullParametersProvided()
    {
        // Arrange
        var initialName = "Original Name";
        var initialCity = "Original City";
        var stockLocation = StockLocation.Create(name: initialName, city: initialCity).Value;
        stockLocation.ClearDomainEvents(); // Clear initial creation event
        
        // Act
        var result = stockLocation.Update(name: null, city: null);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(expected: initialName.Trim().ToSlug()); // Assert slugified name
        result.Value.City.Should().Be(expected: initialCity.Trim()); // City should retain its original value
        result.Value.DomainEvents.Should().NotContain(predicate: e => e is StockLocation.Events.Updated);
    }

    [Fact]
    public void Update_ShouldNotRaiseEvent_WhenNoChangesOccur()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation(name: "Same Name");
        stockLocation.ClearDomainEvents(); // Clear initial creation event

        // Act
        var result = stockLocation.Update(name: "Same Name");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.DomainEvents.Should().BeEmpty();
    }
    
    // --- MakeDefault Method Tests ---
    [Fact]
    public void MakeDefault_ShouldSetDefaultToTrue_WhenNotAlreadyDefault()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation(isDefault: false);
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.MakeDefault();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Default.Should().BeTrue();
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockLocation.Events.StockLocationMadeDefault);
    }

    [Fact]
    public void MakeDefault_ShouldNotRaiseEvent_WhenAlreadyDefault()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation(isDefault: true);
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.MakeDefault();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Default.Should().BeTrue();
        result.Value.DomainEvents.Should().BeEmpty();
    }

    // --- Delete and Restore Method Tests ---
    [Fact]
    public void Delete_ShouldSetIsDeletedTrue_WhenNoDependenciesExist()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Delete(hasPendingShipments: false, hasActiveStockTransfers: false, hasBackorderedInventoryUnits: false);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Deleted);
        stockLocation.IsDeleted.Should().BeTrue();
        stockLocation.DeletedAt.Should().NotBeNull();
        stockLocation.DomainEvents.Should().ContainSingle(predicate: e => e is StockLocation.Events.Deleted);
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenHasPendingShipments()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Delete(hasPendingShipments: true, hasActiveStockTransfers: false, hasBackorderedInventoryUnits: false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockLocation.Errors.HasPendingShipments.Code);
        stockLocation.IsDeleted.Should().BeFalse();
        stockLocation.DeletedAt.Should().BeNull();
        stockLocation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenHasActiveStockTransfers()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Delete(hasPendingShipments: false, hasActiveStockTransfers: true, hasBackorderedInventoryUnits: false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockLocation.Errors.HasActiveStockTransfers.Code);
        stockLocation.IsDeleted.Should().BeFalse();
        stockLocation.DeletedAt.Should().BeNull();
        stockLocation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenHasBackorderedInventoryUnits()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Delete(hasPendingShipments: false, hasActiveStockTransfers: false, hasBackorderedInventoryUnits: true);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockLocation.Errors.HasBackorderedInventoryUnits.Code);
        stockLocation.IsDeleted.Should().BeFalse();
        stockLocation.DeletedAt.Should().BeNull();
        stockLocation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenStockItemsExist()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1");
        stockLocation.StockItems.Add(item: CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 10, quantityReserved: 0, backorderable: true));
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Delete(hasPendingShipments: false, hasActiveStockTransfers: false, hasBackorderedInventoryUnits: false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockLocation.Errors.HasStockItems.Code);
        stockLocation.IsDeleted.Should().BeFalse();
        stockLocation.DeletedAt.Should().BeNull();
        stockLocation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Delete_ShouldReturnError_WhenReservedStockExists()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1");
        stockLocation.StockItems.Add(item: CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 10, quantityReserved: 5, backorderable: true));
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Delete(hasPendingShipments: false, hasActiveStockTransfers: false, hasBackorderedInventoryUnits: false);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockLocation.Errors.HasReservedStock.Code);
        stockLocation.IsDeleted.Should().BeFalse();
        stockLocation.DeletedAt.Should().BeNull();
        stockLocation.DomainEvents.Should().BeEmpty();
    }


    [Fact]
    public void Restore_ShouldSetIsDeletedFalse_WhenDeleted()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        // The delete method itself no longer directly sets these, it expects the caller to know.
        // For testing purposes, we manually set the deleted state here
        stockLocation.DeletedAt = DateTimeOffset.UtcNow;
        stockLocation.IsDeleted = true;
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Restore();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.DeletedAt.Should().BeNull();
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockLocation.Events.Restored);
    }

    [Fact]
    public void Restore_ShouldNotRaiseEvent_WhenNotDeleted()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.Restore();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.IsDeleted.Should().BeFalse();
        result.Value.DeletedAt.Should().BeNull();
        result.Value.DomainEvents.Should().BeEmpty();
    }

    // --- StockItemOrCreate Tests ---
    [Fact]
    public void StockItemOrCreate_ShouldReturnExistingStockItem_WhenVariantAlreadyHasStockItem()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_EXIST");
        var existingStockItem = CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 10, quantityReserved: 0, backorderable: true);
        stockLocation.StockItems.Add(item: existingStockItem);

        // Act
        var result = stockLocation.StockItemOrCreate(variant: variant);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().BeSameAs(expected: existingStockItem);
    }

    [Fact]
    public void StockItemOrCreate_ShouldCreateNewStockItem_WhenVariantDoesNotHaveStockItem()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_NEW");

        // Act
        var result = stockLocation.StockItemOrCreate(variant: variant);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.VariantId.Should().Be(expected: variant.Id);
        result.Value.StockLocationId.Should().Be(expected: stockLocation.Id);
        stockLocation.StockItems.Should().Contain(expected: result.Value);
    }

    // --- Restock and Unstock Tests ---
    [Fact]
    public void Restock_ShouldIncreaseStockItemQuantity_WhenSuccessful()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_RESTOCK");
        var stockItem = CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 5, quantityReserved: 0, backorderable: true);
        stockLocation.StockItems.Add(item: stockItem);
        stockLocation.ClearDomainEvents();

        int quantityToRestock = 5;

        // Act
        var result = stockLocation.Restock(variant: variant, quantity: quantityToRestock, originator: StockMovement.MovementOriginator.Adjustment);

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.QuantityOnHand.Should().Be(expected: 10);
        stockItem.StockMovements.Should().Contain(predicate: sm => sm.Quantity == quantityToRestock);
    }

    [Fact]
    public void Restock_ShouldCreateStockItemAndIncreaseQuantity_WhenVariantDoesNotExist()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_NEW_RESTOCK");
        stockLocation.ClearDomainEvents();

        int quantityToRestock = 7;

        // Act
        var result = stockLocation.Restock(variant: variant, quantity: quantityToRestock, originator: StockMovement.MovementOriginator.Supplier);

        // Assert
        result.IsError.Should().BeFalse();
        stockLocation.StockItems.Should().ContainSingle(predicate: si => si.VariantId == variant.Id);
        stockLocation.StockItems.First().QuantityOnHand.Should().Be(expected: quantityToRestock);
        stockLocation.StockItems.First().StockMovements.Should().Contain(predicate: sm => sm.Quantity == quantityToRestock);
    }

    [Fact]
    public void Unstock_ShouldDecreaseStockItemQuantity_WhenSuccessful()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_UNSTOCK");
        var stockItem = CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 10, quantityReserved: 0, backorderable: true);
        stockLocation.StockItems.Add(item: stockItem);
        stockLocation.ClearDomainEvents();

        int quantityToUnstock = 3;

        // Act
        var result = stockLocation.Unstock(variant: variant, quantity: quantityToUnstock, originator: StockMovement.MovementOriginator.Order);

        // Assert
        result.IsError.Should().BeFalse();
        stockItem.QuantityOnHand.Should().Be(expected: 7);
        stockItem.StockMovements.Should().Contain(predicate: sm => sm.Quantity == -quantityToUnstock);
    }

    [Fact]
    public void Unstock_ShouldReturnError_WhenInsufficientStockAndNotBackorderable()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_NO_BACKORDER"); // Removed backorderable
        var stockItem = CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 5, quantityReserved: 0, backorderable: false);
        stockLocation.StockItems.Add(item: stockItem);
        stockLocation.ClearDomainEvents();

        int quantityToUnstock = 10;

        // Act
        var result = stockLocation.Unstock(variant: variant, quantity: quantityToUnstock, originator: StockMovement.MovementOriginator.Order);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockLocation.UnstockWouldViolateReservations");
        stockItem.QuantityOnHand.Should().Be(expected: 5); // Quantity should not change
    }

    [Fact]
    public void Unstock_ShouldReturnError_WhenUnstockingMoreThanOnHandEvenIfBackorderableAndQuantityGoesNegative()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_BACKORDERABLE"); // Removed backorderable
        var stockItem = CreateTestStockItem(stockLocationId: stockLocation.Id, variant: variant, quantityOnHand: 5, quantityReserved: 0, backorderable: true);
        stockLocation.StockItems.Add(item: stockItem);
        stockLocation.ClearDomainEvents();

        int quantityToUnstock = 10;

        // Act
        var result = stockLocation.Unstock(variant: variant, quantity: quantityToUnstock, originator: StockMovement.MovementOriginator.Order);

        // Assert
        result.IsError.Should().BeTrue(); // Unstock will return an error because Adjust prevents negative QuantityOnHand
        result.FirstError.Code.Should().Be(expected: StockItem.Errors.InsufficientStock(available: 0,requested: 0).Code); // Error from Adjust method
        stockItem.QuantityOnHand.Should().Be(expected: 5); // Quantity should not change
        stockItem.StockMovements.Should().BeEmpty();
    }
    // --- LinkStore and UnlinkStore Tests ---
    [Fact]
    public void LinkStore_ShouldLinkStore_WhenNotAlreadyLinked()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var store = CreateTestStore();
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.LinkStore(store: store);

        // Assert
        result.IsError.Should().BeFalse();
        stockLocation.StoreStockLocations.Should().ContainSingle(predicate: ssl => ssl.StoreId == store.Id);
        stockLocation.DomainEvents.Should().ContainSingle(predicate: e => e is StockLocation.Events.LinkedToStockLocation);
    }

    [Fact]
    public void LinkStore_ShouldReturnError_WhenAlreadyLinked()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var store = CreateTestStore();
        stockLocation.LinkStore(store: store); // Link once
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.LinkStore(store: store); // Link again

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockLocation.StoreAlreadyLinked");
        stockLocation.StoreStockLocations.Should().ContainSingle(predicate: ssl => ssl.StoreId == store.Id); // Still only one
        stockLocation.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void UnlinkStore_ShouldUnlinkStore_WhenLinked()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var store = CreateTestStore();
        stockLocation.LinkStore(store: store); // Link first
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.UnlinkStore(store: store);

        // Assert
        result.IsError.Should().BeFalse();
        stockLocation.StoreStockLocations.Should().BeEmpty();
        stockLocation.DomainEvents.Should().ContainSingle(predicate: e => e is StockLocation.Events.UnlinkedFromStockLocation);
    }

    [Fact]
    public void UnlinkStore_ShouldReturnError_WhenNotLinked()
    {
        // Arrange
        var stockLocation = CreateTestStockLocation();
        var store = CreateTestStore();
        stockLocation.ClearDomainEvents();

        // Act
        var result = stockLocation.UnlinkStore(store: store);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockLocation.StoreNotLinked");
        stockLocation.DomainEvents.Should().BeEmpty();
    }
}