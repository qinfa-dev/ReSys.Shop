using FluentAssertions;
using ReSys.Core.Domain.Promotions.Actions;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.Promotions.Rules;

namespace Core.UnitTests.Domain.Promotions.Promotions;

public class PromotionTests
{
    private PromotionUsage Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage()
    {
        var usageResult = PromotionUsage.CreateOrderDiscount(Promotion.DiscountType.FixedAmount, 10.0m);
        usageResult.IsError.Should().BeFalse();
        return usageResult.Value;
    }

    [Fact]
    public void Create_ShouldReturnPromotion_WhenValidInputs()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();

        // Act
        var result = Promotion.Create("Test Promo", usage, "TEST-CODE", "A test promo", 100, 50, DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddDays(1), 100, true);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be("Test Promo");
        result.Value.PromotionCode.Should().Be("TEST-CODE");
        result.Value.IsActive.Should().BeTrue();
    }

    [Fact]
    public void Create_ShouldReturnError_WhenNameIsInvalid()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();

        // Act
        var result = Promotion.Create("", usage);

        // Assert
        result.IsError.Should().BeTrue();
        // Assuming a validation error code, adjust as per actual implementation
        // result.FirstError.Code.Should().Be("Promotion.Name.Required"); 
    }

    [Fact]
    public void Create_ShouldReturnError_WhenNumericParametersAreInvalid()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();

        // Act
        var result = Promotion.Create("Test Promo", usage, minimumOrderAmount: -1);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(Promotion.Errors.InvalidMinimumOrderAmount.Code);
    }
    
    [Fact]
    public void Update_ShouldUpdateProperties_WhenValidParametersProvided()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Original Name", usage).Value;
        var newName = "New Name";

        // Act
        var result = promotion.Update(name: newName);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.Name.Should().Be(newName);
    }

    [Fact]
    public void Activate_ShouldSetIsActive_WhenNotExpired()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage, expiresAt: DateTimeOffset.UtcNow.AddDays(1)).Value;
        promotion.Deactivate();
        promotion.Active.Should().BeFalse();
        
        // Act
        var result = promotion.Activate();

        // Assert
        result.IsError.Should().BeFalse();
        promotion.Active.Should().BeTrue();
    }

    [Fact]
    public void Deactivate_ShouldSetIsInactive()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage).Value;
        promotion.Active.Should().BeTrue();

        // Act
        var result = promotion.Deactivate();

        // Assert
        result.IsError.Should().BeFalse();
        promotion.Active.Should().BeFalse();
    }
    
    [Fact]
    public void AddRule_ShouldAddRule_WhenRuleIsValid()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage).Value;
        var rule = PromotionRule.Create(promotion.Id, PromotionRule.RuleType.FirstOrder, "true").Value;

        // Act
        var result = promotion.AddRule(rule);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.PromotionRules.Should().Contain(rule);
    }
    
    [Fact]
    public void AddRule_ShouldReturnError_WhenRuleIsDuplicate()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage).Value;
        var rule = PromotionRule.Create(promotion.Id, PromotionRule.RuleType.FirstOrder, "true").Value;
        promotion.AddRule(rule);

        // Act
        var result = promotion.AddRule(rule);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Promotion.DuplicateRule");
    }

    [Fact]
    public void RemoveRule_ShouldRemoveRule_WhenRuleExists()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage).Value;
        var rule = PromotionRule.Create(promotion.Id, PromotionRule.RuleType.FirstOrder, "true").Value;
        promotion.AddRule(rule);
        promotion.PromotionRules.Should().HaveCount(1);

        // Act
        var result = promotion.RemoveRule(rule.Id);

        // Assert
        result.IsError.Should().BeFalse();
        promotion.PromotionRules.Should().BeEmpty();
    }

    [Fact]
    public void IncrementUsage_ShouldIncreaseUsageCount()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage).Value;
        var initialUsage = promotion.UsageCount;

        // Act
        var result = promotion.IncrementUsage();

        // Assert
        result.IsError.Should().BeFalse();
        promotion.UsageCount.Should().Be(initialUsage + 1);
    }
    
    [Fact]
    public void Validate_ShouldReturnSuccess_WhenPromotionIsConsistent()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage, "CODE", expiresAt: DateTimeOffset.UtcNow.AddDays(1), requiresCouponCode: true).Value;

        // Act
        var result = promotion.Validate();

        // Assert
        result.IsError.Should().BeFalse();
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenDateRangeIsInvalid()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage, startsAt: DateTimeOffset.UtcNow.AddDays(1), expiresAt: DateTimeOffset.UtcNow).Value;

        // Act
        var result = promotion.Validate();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Promotion.InvalidDateRange");
    }

    [Fact]
    public void Validate_ShouldReturnError_WhenCodeIsRequiredButMissing()
    {
        // Arrange
        var usage = Create_ShouldReturnPromotion_WhenValidInputs_CreateTestUsage();
        var promotion = Promotion.Create("Test Promo", usage, requiresCouponCode: true, code: null).Value;

        // Act
        var result = promotion.Validate();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be("Promotion.CodeRequired");
    }
}
