using FluentAssertions;
using ReSys.Core.Domain.Promotions.Rules;
using System;
using Xunit;

namespace Core.UnitTests.Domain.Promotions.Rules;

public class PromotionRuleTests
{
    [Fact]
    public void Create_ShouldReturnRule_WhenValidInputs()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var type = PromotionRule.RuleType.FirstOrder;
        var value = "true";

        // Act
        var result = PromotionRule.Create(promotionId, type, value);

        // Assert
        result.IsError.Should().BeFalse();
        var rule = result.Value;
        rule.Should().NotBeNull();
        rule.PromotionId.Should().Be(promotionId);
        rule.Type.Should().Be(type);
        rule.Value.Should().Be(value);
    }
    
    [Fact]
    public void Create_ShouldReturnError_WhenValueIsInvalid()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        var type = PromotionRule.RuleType.FirstOrder;

        // Act - Empty value
        var resultEmpty = PromotionRule.Create(promotionId, type, "");

        // Assert
        resultEmpty.IsError.Should().BeTrue();
        resultEmpty.FirstError.Code.Should().Be(PromotionRule.Errors.ValueRequired.Code);

        // Act - Too long value
        var longValue = new string('a', PromotionRule.Constraints.ValueMaxLength + 1);
        var resultTooLong = PromotionRule.Create(promotionId, type, longValue);

        // Assert
        resultTooLong.IsError.Should().BeTrue();
        resultTooLong.FirstError.Code.Should().Be(PromotionRule.Errors.ValueTooLong.Code);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenTypeIsInvalid()
    {
        // Arrange
        var promotionId = Guid.NewGuid();
        // Cast an invalid int to RuleType to simulate an invalid enum value
        var invalidType = (PromotionRule.RuleType)999; 
        var value = "true";

        // Act
        var result = PromotionRule.Create(promotionId, invalidType, value);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(PromotionRule.Errors.InvalidRuleType.Code);
    }
}