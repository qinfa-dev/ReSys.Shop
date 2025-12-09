using FluentAssertions;
using ReSys.Core.Domain.Promotions.Actions;
using ReSys.Core.Domain.Promotions.Promotions;
// Added for GetPrivate extension method
using ReSys.Core.Common.Domain.Concerns; // Added for IHasMetadata

namespace Core.UnitTests.Domain.Promotions.Actions;

public class PromotionUsageTests
{
    [Fact]
    public void CreateOrderDiscount_ShouldCreateCorrectUsage_ForFixedAmount()
    {
        // Arrange
        var amount = 15.0m;

        // Act
        var result = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.FixedAmount, amount);

        // Assert
        result.IsError.Should().BeFalse();
        var usage = result.Value;
        usage.Type.Should().Be(Promotion.PromotionType.OrderDiscount);
        ((IHasMetadata)usage).GetPrivate<Promotion.DiscountType?>("discountType").Should().Be(Promotion.DiscountType.FixedAmount);
        ((IHasMetadata)usage).GetPrivate<decimal?>("value").Should().Be(amount);
    }

    [Fact]
    public void CreateOrderDiscount_ShouldCreateCorrectUsage_ForPercentage()
    {
        // Arrange
        var percentage = 0.2m; // 20%

        // Act
        var result = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.Percentage, percentage);

        // Assert
        result.IsError.Should().BeFalse();
        var usage = result.Value;
        usage.Type.Should().Be(Promotion.PromotionType.OrderDiscount);
        ((IHasMetadata)usage).GetPrivate<Promotion.DiscountType?>("discountType").Should().Be(Promotion.DiscountType.Percentage);
        ((IHasMetadata)usage).GetPrivate<decimal?>("value").Should().Be(percentage);
    }

    [Fact]
    public void CreateOrderDiscount_ShouldReturnError_ForInvalidPercentage()
    {
        // Arrange
        var invalidPercentage = 1.1m; // 110%

        // Act
        var result = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.Percentage, invalidPercentage);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PromotionUsage.Errors.InvalidPercentageValue.Code);
    }

    [Fact]
    public void CreateItemDiscount_ShouldCreateCorrectUsage_ForFixedAmount()
    {
        // Arrange
        var amount = 5.0m;

        // Act
        var result = PromotionUsage.CreateItemDiscount(Promotion.DiscountType.FixedAmount, amount);

        // Assert
        result.IsError.Should().BeFalse();
        var usage = result.Value;
        usage.Type.Should().Be(Promotion.PromotionType.ItemDiscount);
        usage.GetPrivate<Promotion.DiscountType?>("discountType").Should().Be(Promotion.DiscountType.FixedAmount);
        usage.GetPrivate<decimal?>("value").Should().Be(amount);
    }

    [Fact]
    public void CreateItemDiscount_ShouldReturnError_ForInvalidPercentage()
    {
        // Arrange
        var invalidPercentage = 1.1m; // 110%

        // Act
        var result = PromotionUsage.CreateItemDiscount(Promotion.DiscountType.Percentage, invalidPercentage);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PromotionUsage.Errors.InvalidPercentageValue.Code);
    }
}
