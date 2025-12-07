using FluentAssertions;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Inventories.Stocks; // Added namespace for StockItem
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.Shipments;
using ReSys.Core.Domain.Orders.LineItems;
using ReSys.Core.Domain.Promotions.Actions; // Corrected namespace
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.ShippingMethods;

namespace Core.UnitTests.Domain.Orders;

public class OrderTests
{
    // NEW: Helper to create a StockItem
    private StockItem CreateTestStockItem(Guid variantId, string sku, int quantityOnHand = 10)
    {
        var stockLocationResult = StockLocation.Create(name: "Test Warehouse");
        stockLocationResult.IsError.Should().BeFalse();
        var stockLocation = stockLocationResult.Value;

        var stockItemResult = StockItem.Create(
            variantId: variantId,
            stockLocationId: stockLocation.Id,
            sku: sku,
            quantityOnHand: quantityOnHand);
        stockItemResult.IsError.Should().BeFalse();
        return stockItemResult.Value;
    }

    // Modified CreateTestVariant to ensure purchasable variant by default
    private Variant CreateTestVariant(Guid productId, string sku, decimal price, string currency, bool hasPrice = true, bool isDigital = false, bool trackInventory = true)
    {
        var productResult = Product.Create(
            name: "Test Product Name",
            slug: $"test-product-{Guid.NewGuid()}",
            isDigital: isDigital);
        productResult.IsError.Should().BeFalse();
        var product = productResult.Value;

        var variantResult = Variant.Create(
            productId: productId,
            sku: sku,
            trackInventory: !isDigital && trackInventory,
            isMaster: false);
        variantResult.IsError.Should().BeFalse();
        var variant = variantResult.Value;
        variant.Product = product;
        if (hasPrice)
        {
            var priceResult = variant.SetPrice(price, currency: currency);
            priceResult.IsError.Should().BeFalse($"Failed to set price for variant: {priceResult.FirstError.Code} - {priceResult.FirstError.Description}");
        }
        else
        {
            variant.Prices.Clear(); // Clear prices if hasPrice is false
        }

        // NEW: Add a StockItem if it's a physical product and tracks inventory
        if (trackInventory && !isDigital)
        {
            var stockItem = CreateTestStockItem(variantId: variant.Id, sku: sku, quantityOnHand: 10);
            variant.StockItems.Add(stockItem);
        }

        return variant;
    }

    private Order CreateTestOrder(Guid storeId, string currency = "USD")
    {
        var orderResult = Order.Create(storeId, currency);
        orderResult.IsError.Should().BeFalse();
        return orderResult.Value;
    }

    // Modified CreateTestLineItem to set Variant navigation property
    private LineItem CreateTestLineItem(Guid orderId, Variant variant, int quantity, string currency = "USD")
    {
        var lineItemResult = LineItem.Create(orderId, variant, quantity, currency);
        lineItemResult.IsError.Should().BeFalse();
        var lineItem = lineItemResult.Value;
        lineItem.Variant = variant; // NEW: Set the Variant navigation property
        return lineItem;
    }

    // --- Invariant Tests (from initial setup) ---
    [Fact]
    public void ValidateInvariants_ShouldReturnSuccess_WhenOrderIsInConsistentState()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD");
        var lineItem = CreateTestLineItem(order.Id, variant, 1);
        order.LineItems.Add(lineItem);
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
        var order = CreateTestOrder(Guid.NewGuid());
        var productId = Guid.NewGuid();
        var variant = CreateTestVariant(productId: productId, sku: "SKU1", price: 10.0m, currency: "USD");
        var lineItem = CreateTestLineItem(order.Id, variant, 1);
        order.LineItems.Add(lineItem);
        // Deliberately set an inconsistent item total
        order.ItemTotalCents = lineItem.SubtotalCents + 100;
        order.TotalCents = order.ItemTotalCents;

        // Act
        var result = order.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.InconsistentItemTotal");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenCompletedOrderHasNoCompletionTimestamp()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        order.State = Order.OrderState.Complete;
        order.CompletedAt = null; // Deliberately set to null

        // Act
        var result = order.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.MissingCompletionTimestamp");
    }

    [Fact]
    public void ValidateInvariants_ShouldReturnError_WhenCanceledOrderHasNoCancellationTimestamp()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        order.State = Order.OrderState.Canceled;
        order.CanceledAt = null; // Deliberately set to null

        // Act
        var result = order.ValidateInvariants();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.MissingCancellationTimestamp");
    }

    // --- Create Factory Tests ---
    [Fact]
    public void Create_ShouldReturnOrder_WhenValidInputs()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var currency = "USD";

        // Act
        var result = Order.Create(storeId, currency);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.StoreId.Should().Be(storeId);
        result.Value.Currency.Should().Be(currency);
        result.Value.State.Should().Be(Order.OrderState.Cart);
        result.Value.Number.Should().StartWith("R");
        result.Value.CreatedAt.Should().BeCloseTo(DateTimeOffset.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Create_ShouldGenerateUniqueOrderNumber()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var currency = "USD";

        // Act
        var order1 = Order.Create(storeId, currency).Value;
        var order2 = Order.Create(storeId, currency).Value;

        // Assert
        order1.Number.Should().NotBe(order2.Number);
    }

    // --- State Transition Tests ---
    [Fact]
    public void Next_ShouldTransitionFromCartToAddress_WhenLineItemsExist()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1); // Add a line item
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        //order.LineItems.Count.Should().Be(1); // Debug assertion

        // Act
        var result = order.Next();

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        result.Value.State.Should().Be(Order.OrderState.Address);
    }

    [Fact]
    public void Next_ShouldReturnError_WhenTransitionFromCartToAddressWithEmptyCart()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid()); // Empty cart

        // Act
        var result = order.Next();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.EmptyCart");
    }

    [Fact]
    public void Next_ShouldTransitionFromAddressToDelivery_WhenPhysicalOrderHasAddresses()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.Next(); // Cart -> Address

        // Create real UserAddress instances
        var shipAddressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        shipAddressResult.IsError.Should().BeFalse();
        var shipAddress = shipAddressResult.Value;

        var billAddressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        billAddressResult.IsError.Should().BeFalse();
        var billAddress = billAddressResult.Value;

        order.SetShippingAddress(shipAddress);
        order.SetBillingAddress(billAddress);

        // Act
        var result = order.Next();

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        result.Value.State.Should().Be(Order.OrderState.Delivery);
    }

    [Fact]
    public void Next_ShouldReturnError_WhenPhysicalOrderMissingAddressesForDeliveryTransition()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        var updateResult = order.Next(); // Cart -> Address
        updateResult.IsError.Should().BeFalse();

        // Act
        var result = order.Next(); // Address -> Delivery without addresses

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.Address.Required");
    }

    [Fact]
    public void Next_ShouldTransitionFromDeliveryToPayment_WhenPhysicalOrderHasShippingMethod()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.Next(); // Cart -> Address

        var shipAddressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        shipAddressResult.IsError.Should().BeFalse();
        var shipAddress = shipAddressResult.Value;

        var billAddressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        billAddressResult.IsError.Should().BeFalse();
        var billAddress = billAddressResult.Value;

        order.SetShippingAddress(shipAddress);
        order.SetBillingAddress(billAddress);
        order.Next(); // Address -> Delivery

        // Create a real ShippingMethod instance
        var shippingMethodResult = ShippingMethod.Create(
            name: "Standard Shipping",
            presentation: "STD",
            type: ShippingMethod.ShippingType.Standard,
            baseCost: 5.0m); shippingMethodResult.IsError.Should().BeFalse();
        var shippingMethod = shippingMethodResult.Value;

        order.SetShippingMethod(shippingMethod);

        // Act
        var result = order.Next();

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        result.Value.State.Should().Be(Order.OrderState.Payment);
        // Shipments are no longer created directly by order.Next(), but by an Application Service.
        // result.Value.Shipments.Should().ContainSingle(); // Removed assertion
    }
    [Fact]
    public void Next_ShouldReturnError_WhenPhysicalOrderMissingShippingMethodForPaymentTransition()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.Next(); // Cart -> Address

        var shipAddressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        shipAddressResult.IsError.Should().BeFalse();
        var shipAddress = shipAddressResult.Value;

        var billAddressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        billAddressResult.IsError.Should().BeFalse();
        var billAddress = billAddressResult.Value;

        order.SetShippingAddress(shipAddress);
        order.SetBillingAddress(billAddress);
        order.Next(); // Address -> Delivery

        var shippingMethodResult = ShippingMethod.Create(
            name: "Standard Shipping",
            presentation: "STD",
            type: ShippingMethod.ShippingType.Standard,
            baseCost: 5.0m);

                shippingMethodResult.IsError.Should().BeFalse();
                var shippingMethod = shippingMethodResult.Value;
                
        
        // Act
        var result = order.Next(); // Delivery -> Payment without shipping method

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.ShippingMethodRequired");
    }

    [Fact]
    public void Next_ShouldTransitionFromPaymentToConfirm_WhenPaymentsCoverTotal()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.ItemTotalCents = 1000; // Set a total for payment
        order.TotalCents = 1000;

        // Progress to Payment state
        order.State = Order.OrderState.Payment;
        order.AddPayment(1000, Guid.NewGuid(), "CreditCard");


        // Act
        var result = order.Next(); // Payment -> Confirm

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        result.Value.State.Should().Be(Order.OrderState.Confirm);
    }

    [Fact]
    public void Next_ShouldReturnError_WhenPaymentsDoNotCoverTotalForConfirmTransition()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.ItemTotalCents = 1000; // Set a total for payment
        order.TotalCents = 1000;

        // Progress to Payment state
        order.State = Order.OrderState.Payment;
        order.AddPayment(500, Guid.NewGuid(), "CreditCard"); // Insufficient payment


        // Act
        var result = order.Next(); // Payment -> Confirm

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.InsufficientPayment");
    }

    [Fact]
    public void Next_ShouldTransitionFromConfirmToComplete_WhenPaymentsAreCompleted()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.ItemTotalCents = 1000;
        order.TotalCents = 1000;

        // Simulate full transition to Payment state 
        order.Next(); // Cart -> Address
        var shipAddressResult = UserAddress.Create(
            firstName: "John", lastName: "Doe", userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(), address1: "123 Main St", city: "Anytown", zipcode: "12345", phone: "555-1234");
        shipAddressResult.IsError.Should().BeFalse();
        order.SetShippingAddress(shipAddressResult.Value);
        order.SetBillingAddress(shipAddressResult.Value); // Same for simplicity
        order.Next(); // Address -> Delivery

        var shippingMethodResult = ShippingMethod.Create(
            name: "Standard Shipping", presentation: "STD", type: ShippingMethod.ShippingType.Standard, baseCost: 5.0m);
        shippingMethodResult.IsError.Should().BeFalse();
        order.SetShippingMethod(shippingMethodResult.Value);
        order.Next(); // Delivery -> Payment

        // Simulate shipment creation and inventory unit allocation for physical orders
        if (!order.IsFullyDigital)
        {
            var stockLocationId = Guid.NewGuid(); // Dummy stock location
            var shipmentResult = ReSys.Core.Domain.Orders.Shipments.Shipment.Create(
                orderId: order.Id,
                stockLocationId: stockLocationId);
            shipmentResult.IsError.Should().BeFalse($"Shipment creation failed: {shipmentResult.FirstError.Description}");
            var shipment = shipmentResult.Value;
            shipment.Ready(); // Transition to Ready state to satisfy Order.Complete() validation

            foreach (var lineItem in order.LineItems)
            {
                for (int i = 0; i < lineItem.Quantity; i++)
                {
                    var inventoryUnitResult = ReSys.Core.Domain.Orders.Shipments.InventoryUnit.Create(
                        variantId: lineItem.VariantId,
                        orderId: order.Id,
                        lineItemId: lineItem.Id,
                        shipmentId: shipment.Id,
                        quantity: 1); // Each unit represents 1 quantity
                    inventoryUnitResult.IsError.Should().BeFalse($"InventoryUnit creation failed: {inventoryUnitResult.FirstError.Description}");
                    var inventoryUnit = inventoryUnitResult.Value;
                    
                    // Manually add to line item's inventory units (as EF Core would)
                    lineItem.InventoryUnits.Add(inventoryUnit);
                }
            }
            order.Shipments.Add(shipment);
        }

        order.AddPayment(1500, Guid.NewGuid(), "CreditCard");
        var payment = order.Payments.First(); // Get the payment from the collection
        payment.Authorize("auth123", "code123"); // Authorize the payment first
        payment.Capture("transactionId"); // Mark payment as completed
        payment.IsCompleted.Should().BeTrue(); // Explicitly check the state

        order.Next(); // Payment -> Confirm

        // Act
        var result = order.Next(); // Confirm -> Complete


        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        var completedOrder = (Order)result.Value; // Explicit cast
        completedOrder.Should().NotBeNull(); // Explicit null check (redundant but for safety)
        completedOrder.State.Should().Be(Order.OrderState.Complete);
        completedOrder.CompletedAt.Should().NotBeNull();
        completedOrder.DomainEvents.OfType<Order.Events.FinalizeInventory>().Should().ContainSingle();
    }

    [Fact]
    public void Next_ShouldReturnError_WhenPaymentsAreNotCompletedForCompleteTransition()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.ItemTotalCents = 1000;
        order.TotalCents = 1000;

        // Progress to Confirm state
        order.State = Order.OrderState.Confirm;
        order.AddPayment(1000, Guid.NewGuid(), "CreditCard"); // Payment not captured

        // Act
        var result = order.Next(); // Confirm -> Complete

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.PaymentNotCompleted");
    }

    [Fact]
    public void Cancel_ShouldTransitionToCanceledState_WhenOrderIsNotComplete()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.State = Order.OrderState.Payment; // Not a complete state

        // Act
        var result = order.Cancel();

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        result.Value.State.Should().Be(Order.OrderState.Canceled);
        result.Value.CanceledAt.Should().NotBeNull();
        ((Aggregate)order).DomainEvents.OfType<Order.Events.ReleaseInventory>().Should().ContainSingle();
    }

    [Fact]
    public void Cancel_ShouldReturnError_WhenOrderIsAlreadyComplete()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        order.State = Order.OrderState.Complete;

        // Act
        var result = order.Cancel();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.CannotCancelCompleted");
    }

    [Fact]
    public void Cancel_ShouldBeIdempotent_WhenOrderIsAlreadyCanceled()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        order.State = Order.OrderState.Canceled;
        order.CanceledAt = DateTimeOffset.UtcNow.AddMinutes(-5); // Already canceled

        // Act
        var result = order.Cancel();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.State.Should().Be(Order.OrderState.Canceled);
        // CanceledAt should not be updated if already canceled
        result.Value.CanceledAt.Should().BeCloseTo(DateTimeOffset.UtcNow.AddMinutes(-5), TimeSpan.FromSeconds(1));
    }

    // --- Line Item Management Tests ---
    [Fact]
    public void AddLineItem_ShouldAddNewLineItem_WhenVariantNotAlreadyInOrder()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var initialItemTotal = order.ItemTotalCents;
        var initialTotal = order.TotalCents;

        // Act
        var result = order.AddLineItem(variant, 2);

        // Assert
        var usdPrice = variant.PriceIn("USD");
        usdPrice.Should().NotBeNull();
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.LineItems.Should().ContainSingle(li => li.VariantId == variant.Id && li.Quantity == 2);
        order.ItemTotalCents.Should().Be(initialItemTotal + (usdPrice.Value * 100 * 2));
        order.TotalCents.Should().Be(initialTotal + (usdPrice.Value * 100 * 2));
    }

    [Fact]
    public void AddLineItem_ShouldUpdateQuantity_WhenVariantAlreadyInOrder()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 2);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        var initialItemTotal = order.ItemTotalCents;
        var initialTotal = order.TotalCents;

        // Act
        var result = order.AddLineItem(variant, 3); // Add more of the same variant

        // Assert
        var usdPrice = variant.PriceIn("USD");
        usdPrice.Should().NotBeNull();
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.LineItems.Should().ContainSingle(li => li.VariantId == variant.Id && li.Quantity == 5);
        order.ItemTotalCents.Should().Be(initialItemTotal + (usdPrice.Value * 100 * 3));
        order.TotalCents.Should().Be(initialTotal + (usdPrice.Value * 100 * 3));
    }

    [Fact]
    public void AddLineItem_ShouldReturnError_WhenQuantityIsInvalid()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");

        // Act
        var result = order.AddLineItem(variant, 0);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.LineItem.Quantity.TooFewItems");
    }

    [Fact]
    public void AddLineItem_ShouldReturnError_WhenVariantIsNotPurchasable()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        // Create unpurchasable variant by explicitly setting hasPrice to false
        var unpurchasableVariant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU_UNPURCHASABLE", price: 10.0m, currency: "USD", hasPrice: false);

        // Act
        var result = order.AddLineItem(unpurchasableVariant, 1);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.VariantNotPurchasable");
    }

    [Fact]
    public void AddLineItem_ShouldReturnError_WhenOrderIsNotInCartState()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        order.State = Order.OrderState.Address; // Move out of Cart state

        // Act
        var result = order.AddLineItem(CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU2", price: 10.0m, currency: "USD"), 1);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.CannotModifyAfterCart");
    }

    [Fact]
    public void RemoveLineItem_ShouldRemoveExistingLineItem()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant1 = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var variant2 = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU2", price: 10.0m, currency: "USD");
        var addLineItemResult1 = order.AddLineItem(variant1, 1);
        addLineItemResult1.IsError.Should().BeFalse($"Expected AddLineItem1 to succeed, but got error: {addLineItemResult1.FirstError.Code} - {addLineItemResult1.FirstError.Description}");
        var addLineItemResult2 = order.AddLineItem(variant2, 1);
        addLineItemResult2.IsError.Should().BeFalse($"Expected AddLineItem2 to succeed, but got error: {addLineItemResult2.FirstError.Code} - {addLineItemResult2.FirstError.Description}");
        var lineItemIdToRemove = order.LineItems.First(li => li.VariantId == variant1.Id).Id;
        var initialItemTotal = order.ItemTotalCents;
        var initialTotal = order.TotalCents;
        var lineItemToRemoveSubtotal = order.LineItems.First(li => li.Id == lineItemIdToRemove).SubtotalCents;

        // Act
        var result = order.RemoveLineItem(lineItemIdToRemove);

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.LineItems.Should().NotContain(li => li.Id == lineItemIdToRemove);
        order.LineItems.Should().ContainSingle(li => li.VariantId == variant2.Id);
        order.ItemTotalCents.Should().Be(initialItemTotal - lineItemToRemoveSubtotal);
        order.TotalCents.Should().Be(initialTotal - lineItemToRemoveSubtotal);
    }

    [Fact]
    public void UpdateLineItemQuantity_ShouldUpdateQuantityAndRecalculateTotals()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        var lineItemToUpdate = order.LineItems.First();
        var oldQuantity = lineItemToUpdate.Quantity;
        var newQuantity = 5;
        var oldItemTotal = order.ItemTotalCents;
        var oldTotal = order.TotalCents;

        // Act
        var result = order.UpdateLineItemQuantity(lineItemToUpdate.Id, newQuantity);

        // Assert
        var usdPrice = variant.PriceIn("USD");
        usdPrice.Should().NotBeNull();
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        lineItemToUpdate.Quantity.Should().Be(newQuantity);
        order.ItemTotalCents.Should().Be(oldItemTotal - (usdPrice.Value * 100 * oldQuantity) + (usdPrice.Value * 100 * newQuantity));
        order.TotalCents.Should().Be(oldTotal - (usdPrice.Value * 100 * oldQuantity) + (usdPrice.Value * 100 * newQuantity));
    }

    [Fact]
    public void UpdateLineItemQuantity_ShouldReturnError_WhenQuantityIsInvalid()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");
        var lineItemToUpdate = order.LineItems.First();

        // Act
        var result = order.UpdateLineItemQuantity(lineItemToUpdate.Id, 0);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.LineItem.Quantity.TooFewItems");
    }

    [Fact]
    public void UpdateLineItemQuantity_ShouldReturnError_WhenLineItemDoesNotExist()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());

        // Act
        var result = order.UpdateLineItemQuantity(Guid.NewGuid(), 1);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.LineItemNotFound");
    }

    // --- Promotion Management Tests ---
    [Fact]
    public void ApplyPromotion_ShouldApplyPromotionAndRecalculateTotals()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 2);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}"); // Total 2000 cents

        var promotionUsageResult = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.FixedAmount, 10.0m);
        promotionUsageResult.IsError.Should().BeFalse();
        var promotionUsage = promotionUsageResult.Value;

        var promotionResult = Promotion.Create("Test Promo", promotionUsage);
        promotionResult.IsError.Should().BeFalse();
        var promotion = promotionResult.Value;

        // Act
        var result = order.ApplyPromotion(promotion);

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.PromotionId.Should().Be(promotion.Id);
        order.Adjustments.Should().NotBeEmpty(); // Check if any adjustment was added
        order.AdjustmentTotalCents.Should().BeLessThan(0); // Should be a discount
        order.TotalCents.Should().Be(order.ItemTotalCents + order.AdjustmentTotalCents); // Check recalculation
    }

    [Fact]
    public void ApplyPromotion_ShouldReturnError_WhenSecondDifferentPromotionApplied()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");

        var promotionUsage1Result = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.FixedAmount, 1.0m);
        promotionUsage1Result.IsError.Should().BeFalse();
        var promotionUsage1 = promotionUsage1Result.Value;
        var promotion1Result = Promotion.Create("Promo1", promotionUsage1);
        promotion1Result.IsError.Should().BeFalse();
        var promotion1 = promotion1Result.Value;
        order.ApplyPromotion(promotion1); // Apply first promotion

        var promotionUsage2Result = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.FixedAmount, 2.0m);
        promotionUsage2Result.IsError.Should().BeFalse();
        var promotionUsage2 = promotionUsage2Result.Value;
        var promotion2Result = Promotion.Create("Promo2", promotionUsage2);
        promotion2Result.IsError.Should().BeFalse();
        var promotion2 = promotion2Result.Value;

        // Act
        var result = order.ApplyPromotion(promotion2);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.PromotionAlreadyApplied");
    }

    [Fact]
    public void RemovePromotion_ShouldRemoveAdjustmentsAndRecalculateTotals()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 2);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");

        var promotionUsageResult = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.FixedAmount, 2.0m);
        promotionUsageResult.IsError.Should().BeFalse();
        var promotionUsage = promotionUsageResult.Value;
        var promotionResult = Promotion.Create("Promo Discount", promotionUsage);
        promotionResult.IsError.Should().BeFalse();
        var promotion = promotionResult.Value;
        order.ApplyPromotion(promotion, code: "TESTCODE"); // Apply the promotion to create adjustments and set PromotionId/PromoCode

        // Ensure these are set by the ApplyPromotion method
        order.PromotionId.Should().Be(promotion.Id);
        order.PromoCode.Should().NotBeNullOrEmpty();
        order.ItemTotalCents.Should().Be(2000); // 2 * 1000 cents
        order.AdjustmentTotalCents.Should().Be(-200); // -2.0m * 100
        order.TotalCents.Should().Be(1800); // Recalculated total

        // Act
        var result = order.RemovePromotion();

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.PromotionId.Should().BeNull();
        order.PromoCode.Should().BeNull();
        order.Adjustments.Should().BeEmpty(); // Promotion adjustments cleared
        order.AdjustmentTotalCents.Should().Be(0);
        order.TotalCents.Should().Be(2000); // Total should revert to ItemTotal
    }

    // --- Address Management Tests ---
    [Fact]
    public void SetShippingAddress_ShouldSetAddressAndRaiseEvent_WhenValidAddressProvided()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var addressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        addressResult.IsError.Should().BeFalse();
        var address = addressResult.Value;

        // Act
        var result = order.SetShippingAddress(address);

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.ShipAddress.Should().Be(address);
        ((Aggregate)order).DomainEvents.OfType<Order.Events.ShippingAddressSet>().Should().ContainSingle();
    }

    [Fact]
    public void SetShippingAddress_ShouldReturnError_WhenNullAddressProvided()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());

        // Act
        var result = order.SetShippingAddress(null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.Address.Required");
    }

    [Fact]
    public void SetShippingAddress_ShouldReturnError_WhenDigitalOrder()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var digitalVariant = CreateTestVariant(productId: Guid.NewGuid(), sku: "DIGITAL_SKU", price: 10.0m, currency: "USD", isDigital: true);
        var addLineItemResult = order.AddLineItem(digitalVariant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");

        var addressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        addressResult.IsError.Should().BeFalse();
        var address = addressResult.Value;

        // Act
        var result = order.SetShippingAddress(address);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.DigitalOrderNoShipping");
    }

    [Fact]
    public void SetBillingAddress_ShouldSetAddressAndRaiseEvent_WhenValidAddressProvided()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var addressResult = UserAddress.Create(
            firstName: "John",
            lastName: "Doe",
            userId: Guid.NewGuid().ToString(),
            countryId: Guid.NewGuid(),
            address1: "123 Main St",
            city: "Anytown",
            zipcode: "12345",
            phone: "555-1234");
        addressResult.IsError.Should().BeFalse();
        var address = addressResult.Value;

        // Act
        var result = order.SetBillingAddress(address);

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.BillAddress.Should().Be(address);
        ((Aggregate)order).DomainEvents.OfType<Order.Events.BillingAddressSet>().Should().ContainSingle();
    }

    [Fact]
    public void SetBillingAddress_ShouldReturnError_WhenNullAddressProvided()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());

        // Act
        var result = order.SetBillingAddress(null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.Address.Required");
    }

    [Fact]
    public void SetShippingMethod_ShouldSetMethodRecalculateTotalsAndRaiseEvent_WhenValidMethodProvided()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");

        var shippingMethodResult = ShippingMethod.Create(
            name: "Standard Shipping",
            presentation: "STD",
            type: ShippingMethod.ShippingType.Standard,
            baseCost: 5.0m);
        shippingMethodResult.IsError.Should().BeFalse();
        var shippingMethod = shippingMethodResult.Value;

        // Set state to Delivery for valid transition
        order.State = Order.OrderState.Delivery;

        // Act
        var result = order.SetShippingMethod(shippingMethod);

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        order.ShippingMethodId.Should().Be(shippingMethod.Id);
        order.ShipmentTotalCents.Should().Be(500); // 5.0m * 100
        order.TotalCents.Should().Be(1500); // 1000 (ItemTotal) + 500 (ShipmentTotal)
        ((Aggregate)order).DomainEvents.OfType<Order.Events.ShippingMethodSelected>().Should().ContainSingle();
    }

    [Fact]
    public void SetShippingMethod_ShouldReturnError_WhenNullMethodProvided()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());

        // Act
        var result = order.SetShippingMethod(null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.Shipping method.Required");
    }

    [Fact]
    public void SetShippingMethod_ShouldReturnError_WhenDigitalOrder()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var digitalVariant = CreateTestVariant(productId: Guid.NewGuid(), sku: "DIGITAL_SKU", price: 10.0m, currency: "USD", isDigital: true);
        var addLineItemResult = order.AddLineItem(digitalVariant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");

        var shippingMethodResult = ShippingMethod.Create(
            name: "Standard Shipping",
            presentation: "STD",
            type: ShippingMethod.ShippingType.Standard,
            baseCost: 5.0m);
        shippingMethodResult.IsError.Should().BeFalse();
        var shippingMethod = shippingMethodResult.Value;

        // Set state to Delivery for valid transition
        order.State = Order.OrderState.Delivery;

        // Act
        var result = order.SetShippingMethod(shippingMethod);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.DigitalOrderNoShipping");
    }

    [Fact]
    public void SetShippingMethod_ShouldReturnError_WhenOrderIsInInvalidState()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var variant = CreateTestVariant(productId: Guid.NewGuid(), sku: "SKU1", price: 10.0m, currency: "USD");
        var addLineItemResult = order.AddLineItem(variant, 1);
        addLineItemResult.IsError.Should().BeFalse($"Expected AddLineItem to succeed, but got error: {addLineItemResult.FirstError.Code} - {addLineItemResult.FirstError.Description}");

        var shippingMethodResult = ShippingMethod.Create(
            name: "Standard Shipping",
            presentation: "STD",
            type: ShippingMethod.ShippingType.Standard,
            baseCost: 5.0m);
        shippingMethodResult.IsError.Should().BeFalse();
        var shippingMethod = shippingMethodResult.Value;

        order.State = Order.OrderState.Complete; // Invalid state

        // Act
        var result = order.SetShippingMethod(shippingMethod);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.InvalidStateForShipping");
    }

    // --- Payment Management Tests ---
    [Fact]
    public void AddPayment_ShouldCreateAndReturnPayment_WhenValidInputs()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var paymentMethodId = Guid.NewGuid();
        var amountCents = 1000L;
        var paymentMethodType = "CreditCard";

        // Act
        var result = order.AddPayment(amountCents, paymentMethodId, paymentMethodType);

        // Assert
        result.IsError.Should().BeFalse($"Expected success, but got error: {result.FirstError.Code} - {result.FirstError.Description}");
        result.Value.Should().NotBeNull();
        order.Payments.Should().ContainSingle(p => p.AmountCents == amountCents && p.PaymentMethodId == paymentMethodId);
    }

    [Fact]
    public void AddPayment_ShouldReturnError_WhenInvalidAmountCents()
    {
        // Arrange
        var order = CreateTestOrder(Guid.NewGuid());
        var paymentMethodId = Guid.NewGuid();
        var amountCents = -100L;
        var paymentMethodType = "CreditCard";

        // Act
        var result = order.AddPayment(amountCents, paymentMethodId, paymentMethodType);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Order.Amount cents.TooFewItems");
    }


}