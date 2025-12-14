using FluentAssertions;

using ReSys.Core.Domain.Catalog.OptionTypes;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Prices;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Stocks;

namespace Core.UnitTests.Domain.Catalog.Products.Variants;

public class VariantTests
{
    // Helper method to create a valid Product instance (copied from ProductTests for self-containment)
    private static Product CreateValidProduct(string name = "Test Product", string slug = "test-product")
    {
        var result = Product.Create(name: name, description: "Test Description", slug: slug); // Corrected
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper method to create a valid Variant instance
    private static Variant CreateValidVariant(Guid productId, bool isMaster = false, string sku = "SKU001", bool trackInventory = true)
    {
        var result = Variant.Create(productId: productId, isMaster: isMaster, sku: sku);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper to create a StockItem (dependency for AttachStockItem tests)
    private static StockItem CreateValidStockItem(Guid variantId, Guid stockLocationId, string sku = "SKU_ITEM", int quantity = 10)
    {
        var stockItemResult = ReSys.Core.Domain.Inventories.Stocks.StockItem.Create(variantId: variantId, stockLocationId: stockLocationId, sku: sku, quantityOnHand: quantity);
        stockItemResult.IsError.Should().BeFalse();
        return stockItemResult.Value;
    }

    // Helper to create an OptionValue (dependency for AddOptionValue tests)
    private static OptionValue CreateValidOptionValue(Guid optionTypeId, string name = "Color", string presentation = "Color")
    {
        var optionValueResult = OptionValue.Create(optionTypeId: optionTypeId, name: name, presentation: presentation);
        optionValueResult.IsError.Should().BeFalse();
        return optionValueResult.Value;
    }

    // Helper to create a VariantOptionValue (dependency for AddOptionValue tests)
    private static VariantOptionValue CreateValidVariantOptionValue(Guid variantId, Guid optionValueId)
    {
        var ovvResult = VariantOptionValue.Create(variantId: variantId, optionValueId: optionValueId);
        ovvResult.IsError.Should().BeFalse();
        return ovvResult.Value;
    }

    [Fact]
    public void Variant_Create_MasterVariant_ShouldReturnVariantAndRaiseCreatedEvent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var sku = "MASTER-SKU";

        // Act
        var result = Variant.Create(productId: productId, isMaster: true, sku: sku);

        // Assert
        result.IsError.Should().BeFalse();
        var variant = result.Value;
        variant.Should().NotBeNull();
        variant.ProductId.Should().Be(expected: productId);
        variant.IsMaster.Should().BeTrue();
        variant.Sku.Should().Be(expected: sku);
        variant.DomainEvents.Should().ContainSingle(predicate: e => e is Variant.Events.Created);
        variant.DomainEvents.Should().NotContain(predicate: e => e is Product.Events.VariantAdded);
    }

    [Fact]
    public void Variant_Create_NonMasterVariant_ShouldReturnVariantAndRaiseCreatedAndVariantAddedEvents()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var sku = "NONMASTER-SKU";

        // Act
        var result = Variant.Create(productId: productId, isMaster: false, sku: sku);

        // Assert
        result.IsError.Should().BeFalse();
        var variant = result.Value;
        variant.Should().NotBeNull();
        variant.ProductId.Should().Be(expected: productId);
        variant.IsMaster.Should().BeFalse();
        variant.Sku.Should().Be(expected: sku);
        variant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.Created);
        variant.DomainEvents.Should().Contain(predicate: e => e is Product.Events.VariantAdded);
    }

    [Fact]
    public void Variant_Create_ShouldReturnProductRequiredError_WhenProductIdIsEmpty()
    {
        // Arrange
        var productId = Guid.Empty;

        // Act
        var result = Variant.Create(productId: productId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.ProductRequired);
    }

    [Theory]
    [InlineData(data: "invalid-unit")]
    public void Variant_Create_ShouldReturnInvalidDimensionUnitError_WhenDimensionsUnitIsInvalid(string? invalidUnit)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var result = Variant.Create(productId: productId, dimensionsUnit: invalidUnit);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.InvalidDimensionUnit);
    }

    [Theory]
    [InlineData(data: "invalid-unit")]
    public void Variant_Create_ShouldReturnInvalidWeightUnitError_WhenWeightUnitIsInvalid(string? invalidUnit)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var result = Variant.Create(productId: productId, weightUnit: invalidUnit);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.InvalidWeightUnit);
    }

    [Theory]
    [InlineData(data: "XXX")] // Not a valid ISO currency code
    public void Variant_Create_ShouldReturnInvalidCurrencyError_WhenCostCurrencyIsInvalid(string? invalidCurrency)
    {
        // Arrange
        var productId = Guid.NewGuid();

        // Act
        var result = Variant.Create(productId: productId, costCurrency: invalidCurrency);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Price.Errors.InvalidCurrency);
    }

    [Fact]
    public void Variant_Update_ShouldUpdateSkuAndRaiseEvent()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId, isMaster: true);
        var newSku = "NEW-SKU";
        variant.ClearDomainEvents(); // Clear creation events

        // Act
        var result = variant.Update(sku: newSku);

        // Assert
        result.IsError.Should().BeFalse();
        variant.Sku.Should().Be(expected: newSku);
        variant.DomainEvents.Should().ContainSingle(predicate: e => e is Variant.Events.Updated);
        variant.DomainEvents.Should().NotContain(predicate: e => e is Product.Events.VariantUpdated); // Master variant, so no Product.Events.VariantUpdated
    }

    [Fact]
    public void Variant_Update_ShouldUpdateWeightAndDimensions()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);
        variant.ClearDomainEvents();

        // Act
        var result = variant.Update(weight: 10m, height: 5m, width: 3m, depth: 2m, dimensionsUnit: "cm", weightUnit: "kg");

        // Assert
        result.IsError.Should().BeFalse();
        variant.Weight.Should().Be(expected: 10m);
        variant.Height.Should().Be(expected: 5m);
        variant.Width.Should().Be(expected: 3m);
        variant.Depth.Should().Be(expected: 2m);
        variant.DimensionsUnit.Should().Be(expected: "cm");
        variant.WeightUnit.Should().Be(expected: "kg");
        variant.DomainEvents.Should().ContainSingle(predicate: e => e is Variant.Events.Updated);
    }

    [Fact]
    public void Variant_Update_ShouldSetClearStockItemsEvent_WhenTrackInventoryChangesToFalse()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId, sku: "SKU001", trackInventory: true);
        variant.ClearDomainEvents();

        // Act
        var result = variant.Update(trackInventory: false);

        // Assert
        result.IsError.Should().BeFalse();
        variant.TrackInventory.Should().BeFalse();
        variant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.ClearStockItems);
        variant.DomainEvents.Should().ContainSingle(predicate: e => e is Variant.Events.Updated);
    }

    [Fact]
    public void Variant_Update_ShouldReturnInvalidPriceError_WhenCostPriceIsNegative()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.Update(costPrice: -10m);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.InvalidPrice);
    }

    [Theory]
    [InlineData(data: "invalid-unit")]
    public void Variant_Update_ShouldReturnInvalidDimensionUnitError_WhenDimensionsUnitIsInvalid(string invalidUnit)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.Update(dimensionsUnit: invalidUnit);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.InvalidDimensionUnit);
    }

    [Theory]
    [InlineData(data: "invalid-unit")]
    public void Variant_Update_ShouldReturnInvalidWeightUnitError_WhenWeightUnitIsInvalid(string invalidUnit)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.Update(weightUnit: invalidUnit);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.InvalidWeightUnit);
    }

    [Theory]
    [InlineData(data: "XXX")]
    public void Variant_Update_ShouldReturnInvalidCurrencyError_WhenCostCurrencyIsInvalid(string invalidCurrency)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.Update(costCurrency: invalidCurrency);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Price.Errors.InvalidCurrency);
    }

    [Fact]
    public void Variant_SetPrice_ShouldAddNewPriceAndRaiseEvents()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);
        var amount = 100m;
        var currency = "USD";
        variant.ClearDomainEvents();

        // Act
        var result = variant.SetPrice(amount: amount, currency: currency);

        // Assert
        result.IsError.Should().BeFalse();
        var price = result.Value;
        price.Should().NotBeNull();
        price.Amount.Should().Be(expected: amount);
        price.Currency.Should().Be(expected: currency);
        variant.Prices.Should().Contain(expected: price);
        variant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.Updated);
        variant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.VariantPriceChanged);
    }

    [Fact]
    public void Variant_SetPrice_ShouldUpdateExistingPriceAndRaiseEvents()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);
        variant.SetPrice(amount: 100m, currency: "USD"); // Initial price
        var newAmount = 150m;
        var currency = "USD";
        variant.ClearDomainEvents();

        // Act
        var result = variant.SetPrice(amount: newAmount, currency: currency);

        // Assert
        result.IsError.Should().BeFalse();
        var price = result.Value;
        price.Should().NotBeNull();
        price.Amount.Should().Be(expected: newAmount);
        variant.Prices.Should().ContainSingle(predicate: p => p.Amount == newAmount);
        variant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.Updated);
        variant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.VariantPriceChanged);
    }

    [Fact]
    public void Variant_SetPrice_ShouldReturnInvalidPriceError_WhenAmountIsNegative()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.SetPrice(amount: -10m, currency: "USD");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.InvalidPrice);
    }

    [Theory]
    [InlineData(data: "")]
    public void Variant_SetPrice_ShouldReturnCurrencyRequiredError_WhenCurrencyIsNullOrEmpty(string invalidCurrency)
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.SetPrice(amount: 10m, currency: invalidCurrency);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Price.Errors.CurrencyRequired);
    }

    [Fact]
    public void Variant_SetPrice_ShouldReturnCurrencyTooLongError_WhenCurrencyIsTooLong()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.SetPrice(amount: 10m, currency: "TOO_LONG_CURRENCY");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Price.Errors.CurrencyTooLong);
    }

    [Fact]
    public void Variant_SetPrice_ShouldReturnInvalidCurrencyError_WhenCurrencyIsInvalid()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);

        // Act
        var result = variant.SetPrice(amount: 10m, currency: "XXX");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Price.Errors.InvalidCurrency);
    }




    [Fact]
    public void Variant_AddOptionValue_ShouldReturnError_WhenCalledOnMasterVariant()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var masterVariant = CreateValidVariant(productId: productId, isMaster: true);
        var optionTypeId = Guid.NewGuid();
        var optionValue = CreateValidOptionValue(optionTypeId: optionTypeId);
        masterVariant.ClearDomainEvents();

        // Act
        var result = masterVariant.AddOptionValue(optionValue: optionValue);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.MasterCannotHaveOptionValues);
        masterVariant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Variant_AddOptionValue_ShouldReturnError_WhenOptionValueIsNull()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var nonMasterVariant = CreateValidVariant(productId: productId, isMaster: false);
        nonMasterVariant.ClearDomainEvents();

        // Act
        var result = nonMasterVariant.AddOptionValue(optionValue: null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: OptionValue.Errors.NotFound(id: Guid.Empty)); // Expects Guid.Empty from Variant.AddOptionValue
        nonMasterVariant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Variant_AddOptionValue_ShouldAddOptionValueAndRaiseEvents_WhenValidAndNotMaster()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var nonMasterVariant = CreateValidVariant(productId: productId, isMaster: false);
        // Simulate Product.ProductOptionTypes being set up
        var product = CreateValidProduct();
        var optionTypeId = Guid.NewGuid();
        var productOptionType = ReSys.Core.Domain.Catalog.Products.OptionTypes.ProductOptionType.Create(productId: productId, optionTypeId: optionTypeId).Value;
        product.ProductOptionTypes.Add(item: productOptionType);
        nonMasterVariant.Product = product; // Link product to variant
        var optionValue = CreateValidOptionValue(optionTypeId: optionTypeId);
        nonMasterVariant.ClearDomainEvents();

        // Act
        var result = nonMasterVariant.AddOptionValue(optionValue: optionValue);

        // Assert
        result.IsError.Should().BeFalse();
        nonMasterVariant.VariantOptionValues.Should().ContainSingle(predicate: ovv => ovv.OptionValueId == optionValue.Id && ovv.VariantId == nonMasterVariant.Id);
        nonMasterVariant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.Updated);
        nonMasterVariant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.OptionAdded);
    }

    [Fact]
    public void Variant_AddOptionValue_ShouldNotAddDuplicateOptionValue()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var nonMasterVariant = CreateValidVariant(productId: productId, isMaster: false);
        var product = CreateValidProduct();
        var optionTypeId = Guid.NewGuid();
        var productOptionType = ReSys.Core.Domain.Catalog.Products.OptionTypes.ProductOptionType.Create(productId: productId, optionTypeId: optionTypeId).Value;
        product.ProductOptionTypes.Add(item: productOptionType);
        nonMasterVariant.Product = product;
        var optionValue = CreateValidOptionValue(optionTypeId: optionTypeId);
        nonMasterVariant.AddOptionValue(optionValue: optionValue); // Add once
        nonMasterVariant.ClearDomainEvents();

        // Act
        var result = nonMasterVariant.AddOptionValue(optionValue: optionValue); // Try to add again

        // Assert
        result.IsError.Should().BeFalse();
        nonMasterVariant.VariantOptionValues.Should().ContainSingle(predicate: ovv => ovv.OptionValueId == optionValue.Id);
        nonMasterVariant.DomainEvents.Should().BeEmpty(); // No new events should be raised
    }

    [Fact]
    public void Variant_AddOptionValue_ShouldReturnError_WhenOptionTypeNotAssociatedWithProduct()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var nonMasterVariant = CreateValidVariant(productId: productId, isMaster: false);
        // Product does not have this option type associated
        var product = CreateValidProduct();
        nonMasterVariant.Product = product;
        var optionTypeId = Guid.NewGuid();
        var optionValue = CreateValidOptionValue(optionTypeId: optionTypeId);
        nonMasterVariant.ClearDomainEvents();

        // Act
        var result = nonMasterVariant.AddOptionValue(optionValue: optionValue);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Variant.InvalidOptionValue");
        nonMasterVariant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Variant_Delete_ShouldReturnError_WhenCalledOnMasterVariant()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var masterVariant = CreateValidVariant(productId: productId, isMaster: true);
        masterVariant.ClearDomainEvents();

        // Act
        var result = masterVariant.Delete();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.MasterCannotBeDeleted);
        masterVariant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Variant_Delete_ShouldReturnError_WhenHasCompletedOrders()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var nonMasterVariant = CreateValidVariant(productId: productId, isMaster: false);

        // Ensure variant has a product and price for LineItem.Create to work
        var product = CreateValidProduct(name: "Test Product", slug: "test-product");
        nonMasterVariant.Product = product;
        nonMasterVariant.SetPrice(amount: 10m, currency: "USD");

        // Simulate completed order
        var orderId = Guid.NewGuid();
        // Correct Order.Create call
        var orderCreateResult = ReSys.Core.Domain.Orders.Order.Create(storeId: Guid.NewGuid(), currency: "USD", userId: "user@example.com");
        orderCreateResult.IsError.Should().BeFalse();
        var order = orderCreateResult.Value;
        order.Id = orderId; // Set the ID to match orderId

        order.CompletedAt = DateTimeOffset.UtcNow; // Mark order as completed

        // Correct LineItem.Create call
        var lineItemCreateResult = ReSys.Core.Domain.Orders.LineItems.LineItem.Create(orderId: order.Id, variant: nonMasterVariant, quantity: 1, currency: "USD");
        lineItemCreateResult.IsError.Should().BeFalse();
        var lineItem = lineItemCreateResult.Value;

        lineItem.Order = order; // Link order to line item
        nonMasterVariant.LineItems.Add(item: lineItem);
        nonMasterVariant.ClearDomainEvents();

        // Act
        var result = nonMasterVariant.Delete();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Variant.Errors.CannotDeleteWithCompleteOrders);
        nonMasterVariant.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Variant_Delete_ShouldSoftDeleteAndRaiseEvents_WhenNotMasterAndNoCompletedOrders()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var nonMasterVariant = CreateValidVariant(productId: productId, isMaster: false);
        nonMasterVariant.ClearDomainEvents();

        // Act
        var result = nonMasterVariant.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        nonMasterVariant.IsDeleted.Should().BeTrue();
        nonMasterVariant.DeletedAt.Should().NotBeNull();
        nonMasterVariant.DomainEvents.Should().Contain(predicate: e => e is Product.Events.VariantRemoved);
        nonMasterVariant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.RemoveFromIncompleteOrders);
        nonMasterVariant.DomainEvents.Should().Contain(predicate: e => e is Variant.Events.Deleted);
    }

    [Fact]
    public void Variant_Discontinue_ShouldSetDiscontinueOnAndRaiseEvent_WhenNotAlreadyDiscontinued()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);
        variant.ClearDomainEvents();

        // Act
        var result = variant.Discontinue();

        // Assert
        result.IsError.Should().BeFalse();
        variant.DiscontinueOn.Should().NotBeNull();
        variant.DomainEvents.Should().ContainSingle(predicate: e => e is Variant.Events.Updated);
    }

    [Fact]
    public void Variant_Discontinue_ShouldNotRaiseEvent_WhenAlreadyDiscontinued()
    {
        // Arrange
        var productId = Guid.NewGuid();
        var variant = CreateValidVariant(productId: productId);
        variant.Discontinue(); // Discontinue first
        variant.ClearDomainEvents();

        // Act
        var result = variant.Discontinue();

        // Assert
        result.IsError.Should().BeFalse();
        variant.DiscontinueOn.Should().NotBeNull(); // Should still be discontinued
        variant.DomainEvents.Should().BeEmpty(); // No new events
    }
}
