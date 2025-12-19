using FluentAssertions;

using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.Adjustments;

namespace Core.UnitTests.Domain.Orders.Adjustments;

public class OrderTotalsTests
{
    private Variant CreateTestVariant(Guid productId, string sku, decimal price, string currency)
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
            trackInventory: false,
            isMaster: false);
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Product = product;

        var priceResult = variant.SetPrice(amount: price, currency: currency);
        priceResult.IsError.Should().BeFalse();

        return variant;
    }

    [Fact]
    public void RecalculateTotals_IncludesEligibleLineItemAdjustments_ExcludesIneligible()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var orderResult = Order.Create(storeId: storeId, currency: "USD");
        orderResult.IsError.Should().BeFalse(because: orderResult.IsError ? orderResult.FirstError.Description : string.Empty);
        var order = orderResult.Value;

        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU-1", price: 10.0m, currency: "USD");

        // Add line item (2 × $10.00 = 2000 cents)
        var addResult = order.AddLineItem(variant: variant, quantity: 2);
        addResult.IsError.Should().BeFalse(because: addResult.IsError ? addResult.FirstError.Description : string.Empty);
        var lineItem = order.LineItems.First();

        // Create adjustments
        var eligibleLineAdj = LineItemAdjustment.Create(lineItem.Id, amountCents: -500, description: "Eligible item discount", promotionId: null, eligible: true);
        var ineligibleLineAdj = LineItemAdjustment.Create(lineItem.Id, amountCents: -300, description: "Ineligible item discount", promotionId: null, eligible: false);

        var eligibleOrderAdj = OrderAdjustment.Create(order.Id, amountCents: -200, description: "Eligible order discount", OrderAdjustment.AdjustmentScope.Order, promotionId: null, eligible: true, mandatory: false);
        var ineligibleOrderAdj = OrderAdjustment.Create(order.Id, amountCents: -100, description: "Ineligible order discount", OrderAdjustment.AdjustmentScope.Order, promotionId: null, eligible: false, mandatory: false);

        eligibleLineAdj.IsError.Should().BeFalse(because: eligibleLineAdj.IsError ? eligibleLineAdj.FirstError.Description : string.Empty);
        ineligibleLineAdj.IsError.Should().BeFalse(because: ineligibleLineAdj.IsError ? ineligibleLineAdj.FirstError.Description : string.Empty);
        eligibleOrderAdj.IsError.Should().BeFalse(because: eligibleOrderAdj.IsError ? eligibleOrderAdj.FirstError.Description : string.Empty);
        ineligibleOrderAdj.IsError.Should().BeFalse(because: ineligibleOrderAdj.IsError ? ineligibleOrderAdj.FirstError.Description : string.Empty);

        // Add adjustments to order
        lineItem.Adjustments.Add(eligibleLineAdj.Value);
        lineItem.Adjustments.Add(ineligibleLineAdj.Value);
        order.OrderAdjustments.Add(eligibleOrderAdj.Value);
        order.OrderAdjustments.Add(ineligibleOrderAdj.Value);

        // Act: trigger recalculation by updating line item quantity (no-op change)
        var updateResult = order.UpdateLineItemQuantity(lineItem.Id, lineItem.Quantity);
        updateResult.IsError.Should().BeFalse(because: updateResult.IsError ? updateResult.FirstError.Description : string.Empty);

        // Assert
        // Base subtotal: 2000
        // Eligible line-item adj: -500 => ItemTotal = 1500
        order.ItemTotalCents.Should().Be(1500m);

        // Adjustment total should include only eligible order-level adj: -200
        order.AdjustmentTotalCents.Should().Be(-200m);

        // Grand total = ItemTotal (1500) + ShipmentTotal (0) + AdjustmentTotal (-200) = 1300
        order.TotalCents.Should().Be(1300m);
    }
}
