using FluentAssertions;

using ReSys.Core.Domain.Orders.Adjustments;

namespace Core.UnitTests.Domain.Orders.Adjustments;

public class OrderAdjustmentTests
{
    [Fact]
    public void Create_ShouldSetScopeCorrectly_WhenCreatingOrderScopedAdjustment()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = -1000;
        var description = "Test Discount";
        var scope = OrderAdjustment.AdjustmentScope.Order;

        // Act
        var result = OrderAdjustment.Create(orderId, amount, description, scope);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Scope.Should().Be(scope);
        result.Value.LineItemId.Should().BeNull();
    }

    [Fact]
    public void Create_ShouldSetScopeAndLineItemIdCorrectly_WhenCreatingLineItemScopedAdjustment()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var lineItemId = Guid.NewGuid();
        var amount = -500;
        var description = "Item Discount";
        var scope = OrderAdjustment.AdjustmentScope.LineItem;

        // Act
        var result = OrderAdjustment.Create(orderId, amount, description, scope, lineItemId: lineItemId);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Scope.Should().Be(scope);
        result.Value.LineItemId.Should().Be(lineItemId);
    }
    
    [Fact]
    public void Create_ShouldReturnError_WhenScopeIsLineItemButLineItemIdIsNull()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = -500;
        var description = "Item Discount";
        var scope = OrderAdjustment.AdjustmentScope.LineItem;

        // Act
        var result = OrderAdjustment.Create(orderId, amount, description, scope, lineItemId: null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("OrderAdjustment.LineItemIdRequired");
    }
}
