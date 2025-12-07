using ErrorOr;
using FluentAssertions;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.LineItems;
using ReSys.Core.Domain.Orders.Payments;
using ReSys.Core.Domain.Orders.Shipments;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Catalog.Products;
using System;
using System.Linq;
using Xunit;
using NSubstitute; // For mocking Variant and Promotion

namespace Core.UnitTests.Domain.Orders;

public class OrderInvariantTests
{
    private Variant CreateTestVariant(Guid productId, string sku, bool trackInventory = true)
    {
        // Create a real Product instance
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
            isMaster: false); // Assuming non-master for these tests
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Product = product; // Assign the created product
        var priceResult = variant.SetPrice(amount: 1000m, currency: "USD");
        priceResult.IsError.Should().BeFalse(because: $"Failed to set price for variant: {priceResult.FirstError.Description}");
        return variant;
    }

    private Order CreateTestOrder(Guid storeId, string currency = "USD")
    {
        var orderResult = Order.Create(storeId: storeId, currency: currency);
        orderResult.IsError.Should().BeFalse();
        return orderResult.Value;
    }

    private LineItem CreateTestLineItem(Guid orderId, Variant variant, int quantity, string currency = "USD")
    {
        var lineItemResult = LineItem.Create(orderId: orderId, variant: variant, quantity: quantity, currency: currency);
        lineItemResult.IsError.Should().BeFalse();
        return lineItemResult.Value;
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnSuccess_WhenOrderIsInConsistentState()
    {
        // Arrange
        var order = CreateTestOrder(storeId: Guid.NewGuid());
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1");
        var lineItem = CreateTestLineItem(orderId: order.Id, variant: variant, quantity: 1);
        order.LineItems.Add(item: lineItem);
        order.ItemTotalCents = lineItem.SubtotalCents;
        order.TotalCents = order.ItemTotalCents;

        // Act
        var result = order.ValidateInvariants();

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenItemTotalIsInconsistent()
    {
        // Arrange
        var order = CreateTestOrder(storeId: Guid.NewGuid());
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1");
        var lineItem = CreateTestLineItem(orderId: order.Id, variant: variant, quantity: 1);
        order.LineItems.Add(item: lineItem);
        // Deliberately set an inconsistent item total
        order.ItemTotalCents = lineItem.SubtotalCents + 100; 
        order.TotalCents = order.ItemTotalCents;

        // Act
        var result = order.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Order.InconsistentItemTotal");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenCompletedOrderHasNoCompletionTimestamp()
    {
        // Arrange
        var order = CreateTestOrder(storeId: Guid.NewGuid());
        order.State = Order.OrderState.Complete;
        order.CompletedAt = null; // Deliberately set to null

        // Act
        var result = order.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Order.MissingCompletionTimestamp");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenCanceledOrderHasNoCancellationTimestamp()
    {
        // Arrange
        var order = CreateTestOrder(storeId: Guid.NewGuid());
        order.State = Order.OrderState.Canceled;
        order.CanceledAt = null; // Deliberately set to null

        // Act
        var result = order.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Order.MissingCancellationTimestamp");
    }

    // Add more tests for various scenarios as needed
}
