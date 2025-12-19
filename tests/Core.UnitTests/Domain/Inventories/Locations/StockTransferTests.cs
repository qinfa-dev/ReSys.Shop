using ErrorOr;

using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.Stocks;
using ReSys.Core.Domain.Inventories.StockTransfers;

namespace Core.UnitTests.Domain.Inventories.Locations;

public class StockTransferTests
{
    private StockLocation CreateTestStockLocation(Guid id, string name = "Test Location")
    {
        var result = StockLocation.Create(name: name);
        result.Value.Id = id; // Set specific ID for testing
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    private Variant CreateTestVariant(Guid id, Guid productId, string sku) // Removed backorderable
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
            isMaster: false); // Removed backorderable from call
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Id = id; // Set specific ID for testing
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
            backorderable: backorderable);
        result.IsError.Should().BeFalse(); // Ensure creation is successful for helper
        var stockItem = result.Value;
        stockItem.Variant = variant; // Assign the variant navigation property
        return stockItem;
    }

    // --- Create Factory Method Tests ---
    [Fact]
    public void Create_ShouldReturnStockTransfer_ForValidTransfer()
    {
        // Arrange
        Guid sourceId = Guid.NewGuid();
        Guid destinationId = Guid.NewGuid();
        string reference = "PO123";

        // Act
        var result = StockTransfer.Create(destinationLocationId: destinationId, sourceLocationId: sourceId, reference: reference);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.SourceLocationId.Should().Be(expected: sourceId);
        result.Value.DestinationLocationId.Should().Be(expected: destinationId);
        result.Value.Reference.Should().Be(expected: reference);
        result.Value.Number.Should().StartWith(expected: "T");
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockTransfer.Events.StockTransferCreated);
        var createdEvent = (StockTransfer.Events.StockTransferCreated)result.Value.DomainEvents.First();
        createdEvent.SourceLocationId.Should().Be(expected: sourceId);
        createdEvent.DestinationLocationId.Should().Be(expected: destinationId);
    }

    [Fact]
    public void Create_ShouldReturnStockTransfer_ForValidReceipt()
    {
        // Arrange
        Guid destinationId = Guid.NewGuid();

        // Act
        var result = StockTransfer.Create(destinationLocationId: destinationId, sourceLocationId: null);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SourceLocationId.Should().BeNull();
        result.Value.DestinationLocationId.Should().Be(expected: destinationId);
        result.Value.Number.Should().StartWith(expected: "T");
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockTransfer.Events.StockTransferCreated);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenSourceEqualsDestination()
    {
        // Arrange
        Guid locationId = Guid.NewGuid();

        // Act
        var result = StockTransfer.Create(destinationLocationId: locationId, sourceLocationId: locationId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockTransfer.Errors.SourceEqualsDestination.Code);
    }

    // --- Update Method Tests ---
    [Fact]
    public void Update_ShouldUpdateProperties_WhenValidParametersProvided()
    {
        // Arrange
        var initialSource = Guid.NewGuid();
        var initialDestination = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: initialDestination, sourceLocationId: initialSource).Value;
        stockTransfer.ClearDomainEvents();

        var newSource = Guid.NewGuid();
        var newDestination = Guid.NewGuid();
        var newReference = "NEWREF";

        // Act
        var result = stockTransfer.Update(destinationLocationId: newDestination, sourceLocationId: newSource, reference: newReference);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.SourceLocationId.Should().Be(expected: newSource);
        result.Value.DestinationLocationId.Should().Be(expected: newDestination);
        result.Value.Reference.Should().Be(expected: newReference);
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is StockTransfer.Events.StockTransferUpdated);
    }

    [Fact]
    public void Update_ShouldReturnError_WhenNewSourceEqualsNewDestination()
    {
        // Arrange
        var initialSource = Guid.NewGuid();
        var initialDestination = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: initialDestination, sourceLocationId: initialSource).Value;

        var newLocation = Guid.NewGuid();

        // Act
        var result = stockTransfer.Update(destinationLocationId: newLocation, sourceLocationId: newLocation);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockTransfer.Errors.SourceEqualsDestination.Code);
    }

    [Fact]
    public void Update_ShouldNotRaiseEvent_WhenNoChangesOccur()
    {
        // Arrange
        var initialSource = Guid.NewGuid();
        var initialDestination = Guid.NewGuid();
        var initialReference = "REF1";
        var stockTransfer = StockTransfer.Create(destinationLocationId: initialDestination, sourceLocationId: initialSource, reference: initialReference).Value;
        stockTransfer.ClearDomainEvents();

        // Act
        var result = stockTransfer.Update(destinationLocationId: initialDestination, sourceLocationId: initialSource, reference: initialReference);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.DomainEvents.Should().BeEmpty();
    }

    // --- Transfer Method Tests ---
    [Fact]
    public void Transfer_ShouldSucceed_WithSufficientStock()
    {
        // Arrange
        var sourceLocationId = Guid.NewGuid();
        var destinationLocationId = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: destinationLocationId, sourceLocationId: sourceLocationId).Value;
        stockTransfer.ClearDomainEvents(); // Clear creation event

        var sourceLocation = CreateTestStockLocation(id: sourceLocationId, name: "Source");
        var destinationLocation = CreateTestStockLocation(id: destinationLocationId, name: "Destination");

        var variant1 = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "V1");
        var initialStockItem1 = CreateTestStockItem(stockLocationId: sourceLocationId, variant: variant1, quantityOnHand: 10, quantityReserved: 0, backorderable: false);
        sourceLocation.StockItems.Add(item: initialStockItem1);

        var variantsByQuantity = new Dictionary<Variant, int>
                {
                    { variant1, 5 }
                };

        // Act
        var result = stockTransfer.Transfer(sourceLocation: sourceLocation, destinationLocation: destinationLocation, variantsByQuantity: variantsByQuantity);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Success);

        // Verify source stock changes
        var sourceItem1 = sourceLocation.StockItems.First(predicate: si => si.VariantId == variant1.Id);
        sourceItem1.QuantityOnHand.Should().Be(expected: 5);

        // Verify destination stock changes
        var destItem1 = destinationLocation.StockItems.First(predicate: si => si.VariantId == variant1.Id);
        destItem1.QuantityOnHand.Should().Be(expected: 5);

        stockTransfer.DomainEvents.Should().ContainSingle(predicate: e => e is StockTransfer.Events.StockTransferred);
        var transferredEvent = stockTransfer.DomainEvents.OfType<StockTransfer.Events.StockTransferred>().Single();
        transferredEvent.SourceLocationId.Should().Be(expected: sourceLocationId);
        transferredEvent.DestinationLocationId.Should().Be(expected: destinationLocationId);
    }

    [Fact]
    public void Transfer_ShouldReturnError_WhenInsufficientStock_NonBackorderable()
    {
        // Arrange
        var sourceLocationId = Guid.NewGuid();
        var destinationLocationId = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: destinationLocationId, sourceLocationId: sourceLocationId).Value;

        var sourceLocation = CreateTestStockLocation(id: sourceLocationId, name: "Source");
        var destinationLocation = CreateTestStockLocation(id: destinationLocationId, name: "Destination");

        var variant1 = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "V1"); // Not backorderable
        sourceLocation.StockItems.Add(item: CreateTestStockItem(stockLocationId: sourceLocationId, variant: variant1, quantityOnHand: 5, quantityReserved: 0, backorderable: false));

        var variantsByQuantity = new Dictionary<Variant, int>
        {
            { variant1, 10 } // Request more than available
        };

        // Act
        var result = stockTransfer.Transfer(sourceLocation: sourceLocation, destinationLocation: destinationLocation, variantsByQuantity: variantsByQuantity);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockTransfer.InsufficientStock"); // From StockLocation.Unstock

        var sourceItem1 = sourceLocation.StockItems.First(predicate: si => si.VariantId == variant1.Id);
        sourceItem1.QuantityOnHand.Should().Be(expected: 5); // No change
        destinationLocation.StockItems.Should().BeEmpty(); // No stock added
    }

    [Fact]
    public void Transfer_ShouldReturnError_WhenQuantityIsZeroOrNegative()
    {
        // Arrange
        var sourceLocationId = Guid.NewGuid();
        var destinationLocationId = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: destinationLocationId, sourceLocationId: sourceLocationId).Value;

        var sourceLocation = CreateTestStockLocation(id: sourceLocationId, name: "Source");
        var destinationLocation = CreateTestStockLocation(id: destinationLocationId, name: "Destination");

        var variant1 = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "V1");
        sourceLocation.StockItems.Add(item: CreateTestStockItem(stockLocationId: sourceLocationId, variant: variant1, quantityOnHand: 10, quantityReserved: 0, backorderable: false));

        var variantsByQuantity = new Dictionary<Variant, int>
        {
            { variant1, 0 } // Invalid quantity
        };

        // Act
        var result = stockTransfer.Transfer(sourceLocation: sourceLocation, destinationLocation: destinationLocation, variantsByQuantity: variantsByQuantity);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockTransfer.InvalidQuantity");
    }

    [Fact]
    public void Transfer_ShouldReturnError_WhenSourceLocationDoesNotMatchTransferSourceId()
    {
        // Arrange
        var sourceLocationId = Guid.NewGuid();
        var destinationLocationId = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: destinationLocationId, sourceLocationId: sourceLocationId).Value;

        var incorrectSourceLocation = CreateTestStockLocation(id: Guid.NewGuid(), name: "Incorrect Source"); // Wrong ID
        var destinationLocation = CreateTestStockLocation(id: destinationLocationId, name: "Destination");

        var variant = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "V1");
        var variantsByQuantity = new Dictionary<Variant, int> { { variant, 1 } };

        // Act
        var result = stockTransfer.Transfer(sourceLocation: incorrectSourceLocation, destinationLocation: destinationLocation, variantsByQuantity: variantsByQuantity);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockTransfer.Errors.StockLocationNotFound(locationId: incorrectSourceLocation.Id).Code);
    }

    // --- Receive Method Tests ---
    [Fact]
    public void Receive_ShouldSucceed_WithValidVariantsAndQuantities()
    {
        // Arrange
        var destinationLocationId = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: destinationLocationId).Value; // Receipt (no source)
        stockTransfer.ClearDomainEvents(); // Clear creation event

        var destinationLocation = CreateTestStockLocation(id: destinationLocationId, name: "Destination");
        destinationLocation.StockItems.Clear(); // Ensure StockItems is empty for isolated test

        var variant1 = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "RV1");
        var variant2 = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "RV2");

        var variantsByQuantity = new Dictionary<Variant, int>
        {
            { variant1, 10 },
            { variant2, 20 }
        };

        // Act
        var result = stockTransfer.Receive(destinationLocation: destinationLocation, variantsByQuantity: variantsByQuantity);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Success);

        // Verify destination stock changes
        destinationLocation.StockItems.Should().ContainSingle(predicate: si => si.VariantId == variant1.Id && si.QuantityOnHand == 10);
        destinationLocation.StockItems.Should().ContainSingle(predicate: si => si.VariantId == variant2.Id && si.QuantityOnHand == 20);

        stockTransfer.DomainEvents.Should().ContainSingle(predicate: e => e is StockTransfer.Events.StockReceived);
        var receivedEvent = stockTransfer.DomainEvents.OfType<StockTransfer.Events.StockReceived>().Single();
        receivedEvent.DestinationLocationId.Should().Be(expected: destinationLocationId);
    }

    [Fact]
    public void Receive_ShouldReturnError_WhenQuantityIsZeroOrNegative()
    {
        // Arrange
        var destinationLocationId = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: destinationLocationId).Value;

        var destinationLocation = CreateTestStockLocation(id: destinationLocationId, name: "Destination");

        var variant1 = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "RV1");

        var variantsByQuantity = new Dictionary<Variant, int>
        {
            { variant1, -5 } // Invalid quantity
        };

        // Act
        var result = stockTransfer.Receive(destinationLocation: destinationLocation, variantsByQuantity: variantsByQuantity);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "StockTransfer.InvalidQuantity");
    }

    [Fact]
    public void Receive_ShouldReturnError_WhenDestinationLocationDoesNotMatchTransferDestinationId()
    {
        // Arrange
        var destinationLocationId = Guid.NewGuid();
        var stockTransfer = StockTransfer.Create(destinationLocationId: destinationLocationId).Value;

        var incorrectDestinationLocation = CreateTestStockLocation(id: Guid.NewGuid(), name: "Incorrect Destination"); // Wrong ID

        var variant = CreateTestVariant(id: Guid.NewGuid(), productId: Guid.NewGuid(), sku: "V1");
        var variantsByQuantity = new Dictionary<Variant, int> { { variant, 1 } };

        // Act
        var result = stockTransfer.Receive(destinationLocation: incorrectDestinationLocation, variantsByQuantity: variantsByQuantity);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: StockTransfer.Errors.StockLocationNotFound(locationId: incorrectDestinationLocation.Id).Code);
    }

    // --- Delete Method Tests ---
    [Fact]
    public void Delete_ShouldReturnDeletedResultAndRaiseEvent()
    {
        // Arrange
        var stockTransfer = StockTransfer.Create(destinationLocationId: Guid.NewGuid()).Value;
        stockTransfer.ClearDomainEvents();

        // Act
        var result = stockTransfer.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Deleted);
        stockTransfer.DomainEvents.Should().ContainSingle(predicate: e => e is StockTransfer.Events.StockTransferDeleted);
    }
}