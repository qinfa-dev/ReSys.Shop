using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Stocks;

namespace Core.UnitTests.Domain.Inventories;

public class BackorderAllocationTests
{
    private Variant CreateTestVariant(Guid productId, string sku, bool trackInventory = true)
    {
        var productResult = Product.Create(
            name: "Test Product",
            slug: $"test-{Guid.NewGuid()}",
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
        priceResult.IsError.Should().BeFalse();
        return variant;
    }

    [Fact]
    public void Reserve_AllAvailableUnits_Succeeds()
    {
        var variant = CreateTestVariant(Guid.NewGuid(), "SKU_FULL_1");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 5, quantityReserved: 0, backorderable: false).Value;
        stockItem.ClearDomainEvents();

        var orderId = Guid.NewGuid();
        var r1 = stockItem.Reserve(3, orderId);
        r1.IsError.Should().BeFalse();
        stockItem.QuantityReserved.Should().Be(3);
        stockItem.CountAvailable.Should().Be(2);

        var r2 = stockItem.Reserve(2, orderId: Guid.NewGuid());
        r2.IsError.Should().BeFalse();
        stockItem.QuantityReserved.Should().Be(5);
        stockItem.CountAvailable.Should().Be(0);
    }

    [Fact]
    public void Reserve_PartialAvailable_Backorderable_AllowsReservationBeyondOnHand()
    {
        var variant = CreateTestVariant(Guid.NewGuid(), "SKU_PARTIAL_1");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 2, quantityReserved: 0, backorderable: true).Value;
        stockItem.ClearDomainEvents();

        var orderId = Guid.NewGuid();
        var r1 = stockItem.Reserve(3, orderId);
        r1.IsError.Should().BeFalse();
        stockItem.QuantityReserved.Should().Be(3);
        stockItem.QuantityOnHand.Should().Be(2);
        stockItem.CountAvailable.Should().Be(0); // cannot be negative
    }

    [Fact]
    public void Reserve_NotBackorderable_Insufficient_ReportsError()
    {
        var variant = CreateTestVariant(Guid.NewGuid(), "SKU_NOBACK_1");
        var stockItem = StockItem.Create(variantId: variant.Id, stockLocationId: Guid.NewGuid(), sku: variant.Sku ?? string.Empty, quantityOnHand: 1, quantityReserved: 0, backorderable: false).Value;
        stockItem.ClearDomainEvents();

        var res = stockItem.Reserve(2, Guid.NewGuid());
        res.IsError.Should().BeTrue();
        res.FirstError.Code.Should().Be(StockItem.Errors.InsufficientStock(available: stockItem.QuantityOnHand, requested: 2).Code);
    }
}
