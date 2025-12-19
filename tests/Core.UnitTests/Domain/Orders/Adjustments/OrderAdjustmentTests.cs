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
    }

    [Fact]
    public void Create_ShouldSetScopeCorrectly_WhenCreatingShippingScopedAdjustment()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amount = -100;
        var description = "Shipping Discount";
        var scope = OrderAdjustment.AdjustmentScope.Shipping;

        // Act
        var result = OrderAdjustment.Create(orderId, amount, description, scope);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Scope.Should().Be(scope);
    }
}
