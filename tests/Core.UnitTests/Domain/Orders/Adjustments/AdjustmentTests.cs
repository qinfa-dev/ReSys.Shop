using FluentAssertions;

using ReSys.Core.Domain.Orders.Adjustments;

namespace Core.UnitTests.Domain.Orders.Adjustments;

public class LineItemAdjustmentTests
{
    [Fact]
    public void Create_ShouldCreateLineItemAdjustment_WithValidParameters()
    {
        // Arrange
        var lineItemId = Guid.NewGuid();
        var amount = -500;
        var description = "Item Discount";

        // Act
        var result = LineItemAdjustment.Create(lineItemId, amount, description);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.LineItemId.Should().Be(lineItemId);
        result.Value.AmountCents.Should().Be(amount);
        result.Value.Description.Should().Be(description);
    }
    
    [Fact]
    public void Create_ShouldReturnError_WhenDescriptionIsEmpty()
    {
        // Arrange
        var lineItemId = Guid.NewGuid();
        var amount = -500;
        var description = ""; // Invalid

        // Act
        var result = LineItemAdjustment.Create(lineItemId, amount, description);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(LineItemAdjustment.Errors.DescriptionRequired.Code);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenDescriptionIsTooLong()
    {
        // Arrange
        var lineItemId = Guid.NewGuid();
        var amount = -500;
        var description = new string('a', LineItemAdjustment.Constraints.DescriptionMaxLength + 1); // Invalid

        // Act
        var result = LineItemAdjustment.Create(lineItemId, amount, description);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(LineItemAdjustment.Errors.DescriptionTooLong.Code);
    }
}
