using FluentAssertions;
using NSubstitute;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Orders.LineItems;
using ReSys.Core.Common.Domain.Entities; // Added for Aggregate

using System;
using System.Linq;
using Xunit;

namespace Core.UnitTests.Domain.Orders.LineItems;

public class LineItemTests
{
    private Variant CreateTestVariant(Guid productId, string sku, decimal price, string currency)
    {
        var productResult = Product.Create(
            name: "Test Product Name",
            slug: $"test-product-{Guid.NewGuid()}",
            isDigital: false); // Default to physical product for simplicity
        productResult.IsError.Should().BeFalse();
        var product = productResult.Value;

        var variantResult = Variant.Create(
            productId: productId,
            sku: sku,
            trackInventory: true, // Default to tracking inventory
            isMaster: false);
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Product = product;
        
        var priceResult = variant.SetPrice(amount: price, currency: currency);
        priceResult.IsError.Should().BeFalse(because: $"Failed to set price for variant: {priceResult.FirstError.Description}");
        
        return variant;
    }

    [Fact]
    public void Create_ShouldReturnLineItem_WhenValidInputs()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD");
        var quantity = 2;
        var currency = "USD";

        // Act
        var result = LineItem.Create(orderId: orderId, variant: variant, quantity: quantity, currency: currency);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().Be(expected: orderId);
        result.Value.VariantId.Should().Be(expected: variant.Id);
        result.Value.Quantity.Should().Be(expected: quantity);
        result.Value.PriceCents.Should().Be(expected: 1000); // 10.0m * 100
        result.Value.Currency.Should().Be(expected: currency);
        result.Value.CapturedName.Should().Be(expected: variant.DescriptiveName);
        result.Value.CapturedSku.Should().Be(expected: variant.Sku);
        result.Value.CreatedAt.Should().BeCloseTo(nearbyTime: DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(seconds: 5));
    }

    [Fact]
    public void Create_ShouldReturnError_WhenVariantIsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        Variant? variant = null;
        var quantity = 1;
        var currency = "USD";

        // Act
        var result = LineItem.Create(orderId: orderId, variant: variant, quantity: quantity, currency: currency);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "LineItem.VariantRequired");
    }

    [Fact]
    public void Create_ShouldReturnError_WhenQuantityIsInvalid()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD");
        var quantity = 0; // Invalid quantity

        // Act
        var result = LineItem.Create(orderId: orderId, variant: variant, quantity: quantity, currency: "USD");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "LineItem.InvalidQuantity");
    }

    [Fact]
    public void Create_ShouldReturnError_WhenCurrencyIsNullOrEmpty()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD");
        var quantity = 1;
        var emptyCurrency = string.Empty;

        // Act
        var result = LineItem.Create(orderId: orderId, variant: variant, quantity: quantity, currency: emptyCurrency);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "LineItem.Currency.Required");
    }

    [Fact]
    public void Create_ShouldReturnError_WhenVariantIsNotPricedInCurrency()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        // Create variant without price for "USD"
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 0m, currency: "EUR"); 
        variant.Prices.Clear(); // Manually clear prices to make it unpurchasable for USD
        var quantity = 1;
        var requestedCurrency = "USD"; // Requesting USD, but variant only has EUR price

        // Act
        var result = LineItem.Create(orderId: orderId, variant: variant, quantity: quantity, currency: requestedCurrency);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "LineItem.VariantNotPriced");
    }

    [Fact]
    public void UpdateQuantity_ShouldUpdateQuantity_WhenValidInput()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD");
        var lineItem = LineItem.Create(orderId: orderId, variant: variant, quantity: 1, currency: "USD").Value;
        var newQuantity = 5;

        // Act
        var result = lineItem.UpdateQuantity(quantity: newQuantity);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Quantity.Should().Be(expected: newQuantity);
        result.Value.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void UpdateQuantity_ShouldReturnError_WhenQuantityIsInvalid()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD");
        var lineItem = LineItem.Create(orderId: orderId, variant: variant, quantity: 1, currency: "USD").Value;
        var newQuantity = 0; // Invalid quantity

        // Act
        var result = lineItem.UpdateQuantity(quantity: newQuantity);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "LineItem.InvalidQuantity");
    }

    [Fact]
    public void ComputedProperties_ShouldCalculateCorrectly()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD"); // 1000 cents
        var lineItem = LineItem.Create(orderId: orderId, variant: variant, quantity: 3, currency: "USD").Value;

        // Assert
        lineItem.PriceCents.Should().Be(expected: 1000);
        lineItem.UnitPrice.Should().Be(expected: 10.0m);
        lineItem.SubtotalCents.Should().Be(expected: 3000); // 1000 * 3
        lineItem.Subtotal.Should().Be(expected: 30.0m);
        lineItem.TotalCents.Should().Be(expected: 3000); // Now equal to SubtotalCents
        lineItem.Total.Should().Be(expected: 30.0m);
    }
}
