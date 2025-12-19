//using FluentAssertions;

//using ReSys.Core.Domain.Orders.Shipments;

//namespace Core.UnitTests.Domain.Orders.Returns;

//public class ReturnItemTests
//{
//    private InventoryUnit CreateTestInventoryUnit(Guid variantId, Guid lineItemId, InventoryUnit.InventoryUnitState state = InventoryUnit.InventoryUnitState.Backordered)
//    {
//        var result = InventoryUnit.Create(variantId: variantId, lineItemId: lineItemId, shipmentId: Guid.NewGuid());

//        if (result.IsError || result.Value is null)
//        {
//            var errorDescription = result.IsError ? result.FirstError.Description : "Value was null after successful creation.";
//            throw new InvalidOperationException(message: $"Failed to create InventoryUnit for testing: {errorDescription}");
//        }

//        var unit = result.Value;

//        // Use reflection to set state if not OnHand, as Factory creates OnHand
//        if (state != InventoryUnit.InventoryUnitState.OnHand)
//        {
//            var stateProperty = typeof(InventoryUnit).GetProperty(name: nameof(InventoryUnit.State));
//            stateProperty?.SetValue(obj: unit, value: state);
//        }
//        return unit;
//    }



//    //[Fact]
//    //public void Accept_ShouldSetPassedQualityCheckAndApplyRestockingFee_WhenLogicIsImplemented()
//    //{
//    //    // Arrange
//    //    var inventoryUnit = CreateTestInventoryUnit(Guid.NewGuid(), Guid.NewGuid());
//    //    inventoryUnit.Ship();
//    //    var returnItem = ReturnItem.Create(inventoryUnit.Id, 1).Value;
//    //    returnItem.InventoryUnit = inventoryUnit;
//    //    returnItem.Receive();

//    //    // Act
//    //    var result = returnItem.Accept(isAutomatic: false);

//    //    // Assert
//    //    result.IsError.Should().BeFalse();
//    //    var acceptedReturn = result.Value;

//    //    // This tests the placeholder logic. A real implementation would have more complex rules.
//    //    if (acceptedReturn.Resellable && acceptedReturn.PassedQualityCheck)
//    //    {
//    //        acceptedReturn.RestockingFeeCents.Should().Be(0);
//    //    }
//    //    else
//    //    {
//    //        acceptedReturn.RestockingFeeCents.Should().BeGreaterThan(0);
//    //    }

//    //    acceptedReturn.AcceptanceStatus.Should().Be(ReturnItem.ReturnAcceptanceStatus.Accepted);
//    //}
//}
