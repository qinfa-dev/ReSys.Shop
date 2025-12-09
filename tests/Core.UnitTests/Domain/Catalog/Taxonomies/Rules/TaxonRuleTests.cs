using FluentAssertions;

using ReSys.Core.Domain.Catalog.Taxonomies.Rules;

using static ReSys.Core.Domain.Catalog.Taxonomies.Rules.TaxonRule; // For TaxonRule.Errors

namespace Core.UnitTests.Domain.Catalog.Taxonomies.Rules;

public class TaxonRuleTests
{
    // Helper method to create a valid TaxonRule instance
    private static TaxonRule CreateValidTaxonRule(Guid taxonId, string type = "product_name", string value = "test", string matchPolicy = "contains", string? propertyName = null)
    {
        var result = TaxonRule.Create(taxonId: taxonId, type: type, value: value, matchPolicy: matchPolicy, propertyName: propertyName);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    [Fact]
    public void TaxonRule_Create_ShouldReturnTaxonRule_WhenValidParameters()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var type = "product_name";
        var value = "Test Product";
        var matchPolicy = "contains";

        // Act
        var result = TaxonRule.Create(taxonId: taxonId, type: type, value: value, matchPolicy: matchPolicy);

        // Assert
        result.IsError.Should().BeFalse();
        var rule = result.Value;
        rule.Should().NotBeNull();
        rule.TaxonId.Should().Be(expected: taxonId);
        rule.Type.Should().Be(expected: type);
        rule.Value.Should().Be(expected: value);
        rule.MatchPolicy.Should().Be(expected: matchPolicy);
        rule.PropertyName.Should().BeNull();
    }

    [Fact]
    public void TaxonRule_Create_ShouldReturnTaxonRule_WhenProductPropertyTypeAndPropertyNameProvided()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var type = "product_property";
        var value = "Color:Red";
        var matchPolicy = "is_equal_to";
        var propertyName = "Color";

        // Act
        var result = TaxonRule.Create(taxonId: taxonId, type: type, value: value, matchPolicy: matchPolicy, propertyName: propertyName);

        // Assert
        result.IsError.Should().BeFalse();
        var rule = result.Value;
        rule.Should().NotBeNull();
        rule.Type.Should().Be(expected: type);
        rule.Value.Should().Be(expected: value);
        rule.MatchPolicy.Should().Be(expected: matchPolicy);
        rule.PropertyName.Should().Be(expected: propertyName);
    }

    [Fact]
    public void TaxonRule_Create_ShouldReturnInvalidType_WhenTypeIsInvalid()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var type = "invalid_type";
        var value = "test";

        // Act
        var result = TaxonRule.Create(taxonId: taxonId, type: type, value: value);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.InvalidType);
    }

    [Fact]
    public void TaxonRule_Create_ShouldReturnInvalidMatchPolicy_WhenMatchPolicyIsInvalid()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var type = "product_name";
        var value = "test";
        var matchPolicy = "invalid_policy";

        // Act
        var result = TaxonRule.Create(taxonId: taxonId, type: type, value: value, matchPolicy: matchPolicy);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.InvalidMatchPolicy);
    }

    [Fact]
    public void TaxonRule_Create_ShouldReturnPropertyNameRequired_WhenProductPropertyTypeAndNoPropertyName()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var type = "product_property";
        var value = "test";
        var matchPolicy = "contains";

        // Act
        var result = TaxonRule.Create(taxonId: taxonId, type: type, value: value, matchPolicy: matchPolicy, propertyName: null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.PropertyNameRequired);
    }

    [Fact]
    public void TaxonRule_Update_ShouldUpdateProperties()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var rule = CreateValidTaxonRule(taxonId: taxonId);

        var newType = "product_sku";
        var newValue = "NEW-SKU";
        var newMatchPolicy = "is_equal_to";

        // Act
        var result = rule.Update(type: newType, value: newValue, matchPolicy: newMatchPolicy);

        // Assert
        result.IsError.Should().BeFalse();
        rule.Type.Should().Be(expected: newType);
        rule.Value.Should().Be(expected: newValue);
        rule.MatchPolicy.Should().Be(expected: newMatchPolicy);
        rule.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void TaxonRule_Update_ShouldReturnInvalidType_WhenTypeIsInvalid()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var rule = CreateValidTaxonRule(taxonId: taxonId);

        // Act
        var result = rule.Update(type: "invalid_type");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.InvalidType);
    }

    [Fact]
    public void TaxonRule_Update_ShouldReturnInvalidMatchPolicy_WhenMatchPolicyIsInvalid()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var rule = CreateValidTaxonRule(taxonId: taxonId);

        // Act
        var result = rule.Update(matchPolicy: "invalid_policy");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.InvalidMatchPolicy);
    }

    [Fact]
    public void TaxonRule_Update_ShouldReturnPropertyNameRequired_WhenProductPropertyTypeAndNoPropertyName()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var rule = CreateValidTaxonRule(taxonId: taxonId, type: "product_property", propertyName: "Color"); // Start with propertyName

        // Act
        var result = rule.Update(propertyName: ""); // Try to set propertyName to null/empty

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.PropertyNameRequired);
    }

    [Fact]
    public void TaxonRule_Update_ShouldNotChangeIfNoNewValues()
    {
        // Arrange
        var taxonId = Guid.NewGuid();
        var rule = CreateValidTaxonRule(taxonId: taxonId, type: "product_name", value: "test");


        var initialUpdatedAt = rule.UpdatedAt; // Should be null initially for new object

        // Act
        var result = rule.Update(type: "product_name", value: "test"); // Same values

        // Assert
        result.IsError.Should().BeFalse();
        rule.UpdatedAt.Should().Be(expected: initialUpdatedAt); // UpdatedAt should not change
    }
}
