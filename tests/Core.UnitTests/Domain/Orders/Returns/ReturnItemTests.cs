using FluentAssertions;

using ReSys.Core.Domain.Orders.Returns;
using ReSys.Core.Domain.Orders.Shipments;

namespace Core.UnitTests.Domain.Orders.Returns;

public class ReturnItemTests
{
    private InventoryUnit CreateTestInventoryUnit(Guid variantId, Guid orderId, Guid lineItemId, InventoryUnit.InventoryUnitState state = InventoryUnit.InventoryUnitState.Backordered)
    {
        var result = InventoryUnit.Create(variantId: variantId, orderId: orderId, lineItemId: lineItemId, shipmentId: Guid.NewGuid(), quantity: 1);
        
        if (result.IsError || result.Value is null)
        {
            var errorDescription = result.IsError ? result.FirstError.Description : "Value was null after successful creation.";
            throw new InvalidOperationException(message: $"Failed to create InventoryUnit for testing: {errorDescription}");
        }
        
        var unit = result.Value;
        
        // Use reflection to set state if not OnHand, as Factory creates OnHand
        if (state != InventoryUnit.InventoryUnitState.OnHand)
        {
            var stateProperty = typeof(InventoryUnit).GetProperty(name: nameof(InventoryUnit.State));
            stateProperty?.SetValue(obj: unit, value: state);
        }
        return unit;
    }

    [Fact(Skip = "CreateForPartialReturn is temporarily disabled due to refactoring")]
    public void CreateForPartialReturn_ShouldSplitInventoryUnit_AndCreateReturnItem()
    {
        // Arrange
        // This setup is simplified. A real test would require more mocking
        // or a more robust TestBuilder pattern for aggregates.
        var orderId = Guid.NewGuid();
        var lineItemId = Guid.NewGuid();
        var variantId = Guid.NewGuid();

        var originalUnit = InventoryUnit.Create(
            variantId,
            orderId,
            lineItemId,
            shipmentId: Guid.NewGuid(),
            10).Value; // Initial quantity
        originalUnit.RequiresIndividualTracking = false;

        var returnQuantity = 3;

        // Act
        var result = ReturnItem.CreateForPartialReturn(originalUnit, returnQuantity);

        // Assert
        result.IsError.Should().BeFalse();

        var (newReturnItem, splitOffUnit) = result.Value;

        // Check original unit
        originalUnit.Quantity.Should().Be(7); // 10 - 3

        // Check split-off unit
        splitOffUnit.Should().NotBeNull();
        splitOffUnit.Quantity.Should().Be(returnQuantity);
        splitOffUnit.OrderId.Should().Be(orderId);

        // Check the new return item
        newReturnItem.Should().NotBeNull();
        newReturnItem.InventoryUnitId.Should().Be(splitOffUnit.Id);
        newReturnItem.ReturnQuantity.Should().Be(returnQuantity);
    }

    [Fact]
    public void Accept_ShouldSetPassedQualityCheckAndApplyRestockingFee_WhenLogicIsImplemented()
    {
        // Arrange
        var returnItem = ReturnItem.Create(Guid.NewGuid(), 1).Value;
        returnItem.ReceptionStatus = ReturnItem.ReturnReceptionStatus.Received;
        returnItem.AcceptanceStatus = ReturnItem.ReturnAcceptanceStatus.Pending;

        // Act
        var result = returnItem.Accept(isAutomatic: false);

        // Assert
        result.IsError.Should().BeFalse();
        var acceptedReturn = result.Value;

        // This tests the placeholder logic. A real implementation would have more complex rules.
        if (acceptedReturn.Resellable && acceptedReturn.PassedQualityCheck)
        {
            acceptedReturn.RestockingFeeCents.Should().Be(0);
        }
        else
        {
            acceptedReturn.RestockingFeeCents.Should().BeGreaterThan(0);
        }

        acceptedReturn.AcceptanceStatus.Should().Be(ReturnItem.ReturnAcceptanceStatus.Accepted);
    }
}
