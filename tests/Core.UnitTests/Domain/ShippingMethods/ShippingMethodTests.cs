using ErrorOr;

using FluentAssertions;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Settings.ShippingMethods;

using static ReSys.Core.Domain.Settings.ShippingMethods.ShippingMethod;

namespace Core.UnitTests.Domain.ShippingMethods;

public class ShippingMethodTests
{
    // Record type for Create_ShouldReturnError_WhenInvalidInputs test data
    public record InvalidCreateInputData(
        string? Name,
        string? Presentation,
        ShippingType Type,
        decimal BaseCost,
        int? EstimatedDaysMin,
        int? EstimatedDaysMax,
        int Position,
        string ExpectedErrorCode);

    // Record type for Update_ShouldReturnError_WhenInvalidNameOrPresentation test data
    public record InvalidUpdateInputData(
        string? Name,
        string? Presentation,
        string ExpectedErrorCode);

    private static ShippingMethod CreateValidShippingMethod(
        string name = "Standard Shipping",
        string presentation = "Standard Ground (3-5 days)",
        ShippingType type = ShippingType.Standard,
        decimal baseCost = 5.00m,
        string? description = null,
        bool active = true,
        int? estimatedDaysMin = 3,
        int? estimatedDaysMax = 5,
        int position = 0,

        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null,
        DisplayOn displayOn = DisplayOn.Both)
    {
        var result = ShippingMethod.Create(
            name: name,
            presentation: presentation,
            type: type,
            baseCost: baseCost,
            description: description,
            active: active,
            estimatedDaysMin: estimatedDaysMin,
            estimatedDaysMax: estimatedDaysMax,
            position: position,

            publicMetadata: publicMetadata,
            privateMetadata: privateMetadata,
            displayOn: displayOn);

        result.IsError.Should().BeFalse();
        return result.Value;
    }
    [Fact]
    public void Create_ShouldReturnShippingMethod_WhenValidInputs()
    {
        // Arrange
        var name = "Express Air";
        var presentation = "Express Air (1-2 days)";
        var type = ShippingType.Express;
        var baseCost = 15.00m;
        var estimatedDaysMin = 1;
        var estimatedDaysMax = 2;

        // Act
        var result = ShippingMethod.Create(
            name: name,
            presentation: presentation,
            type: type,
            baseCost: baseCost,
            estimatedDaysMin: estimatedDaysMin,
            estimatedDaysMax: estimatedDaysMax);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.Name.Should().Be(expected: name.ToSlug());
        result.Value.Presentation.Should().Be(expected: presentation);
        result.Value.Type.Should().Be(expected: type);
        result.Value.BaseCost.Should().Be(expected: baseCost);
        result.Value.EstimatedDaysMin.Should().Be(expected: estimatedDaysMin);
        result.Value.EstimatedDaysMax.Should().Be(expected: estimatedDaysMax);
        result.Value.CreatedAt.Should().BeCloseTo(nearbyTime: DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(seconds: 5));
        result.Value.Active.Should().BeTrue();
        result.Value.Position.Should().Be(expected: 0);
        result.Value.PublicMetadata.Should().BeEmpty();
        result.Value.PrivateMetadata.Should().BeEmpty();
        result.Value.DomainEvents.Should().ContainSingle(predicate: e => e is ShippingMethod.Events.Created);
    }

    public static IEnumerable<object[]> GetInvalidCreateInputs()
    {
        yield return [new InvalidCreateInputData(Name: "", Presentation: "Presentation", Type: ShippingType.Standard, BaseCost: 10.00m, EstimatedDaysMin: null, EstimatedDaysMax: null, Position: 0, ExpectedErrorCode: "ShippingMethod.Name.Required")
        ];
        yield return
        [
            new InvalidCreateInputData(
            Name: "TooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongName", Presentation: "Presentation", Type: ShippingType.Standard, BaseCost: 10.00m, EstimatedDaysMin: null, EstimatedDaysMax: null, Position: 0, ExpectedErrorCode: "ShippingMethod.Name.TooLong")
        ];
        yield return [new InvalidCreateInputData(Name: "Name", Presentation: "", Type: ShippingType.Standard, BaseCost: 10.00m, EstimatedDaysMin: null, EstimatedDaysMax: null, Position: 0, ExpectedErrorCode: "ShippingMethod.Presentation.Required")
        ];
        yield return [new InvalidCreateInputData(Name: "Name", Presentation: "Presentation", Type: (ShippingType)99, BaseCost: 10.00m, EstimatedDaysMin: null, EstimatedDaysMax: null, Position: 0, ExpectedErrorCode: "ShippingMethod.Type.InvalidEnumValue")
        ];
        yield return [new InvalidCreateInputData(Name: "Name", Presentation: "Presentation", Type: ShippingType.Standard, BaseCost: -5.00m, EstimatedDaysMin: null, EstimatedDaysMax: null, Position: 0, ExpectedErrorCode: "ShippingMethod.BaseCost.InvalidRange")
        ];
        yield return [new InvalidCreateInputData(Name: "Name", Presentation: "Presentation", Type: ShippingType.Standard, BaseCost: 5.00m, EstimatedDaysMin: 5, EstimatedDaysMax: 3, Position: 0, ExpectedErrorCode: "ShippingMethod.EstimatedDays.InvalidRange")
        ]; // Min > Max
        yield return [new InvalidCreateInputData(Name: "Name", Presentation: "Presentation", Type: ShippingType.Standard, BaseCost: 5.00m, EstimatedDaysMin: -1, EstimatedDaysMax: 3, Position: 0, ExpectedErrorCode: "ShippingMethod.EstimatedDays.InvalidRange")
        ]; // Min negative
        yield return [new InvalidCreateInputData(Name: "Name", Presentation: "Presentation", Type: ShippingType.Standard, BaseCost: 5.00m, EstimatedDaysMin: 3, EstimatedDaysMax: -1, Position: 0, ExpectedErrorCode: "ShippingMethod.EstimatedDays.InvalidRange")
        ]; // Max negative
        yield return [new InvalidCreateInputData(Name: "Name", Presentation: "Presentation", Type: ShippingType.Standard, BaseCost: 10.00m, EstimatedDaysMin: null, EstimatedDaysMax: null, Position: -1, ExpectedErrorCode: "ShippingMethod.Position.InvalidRange")
        ]; // Position negative
    }

    [Theory]
    [MemberData(memberName: nameof(GetInvalidCreateInputs))]
    public void Create_ShouldReturnError_WhenInvalidInputs(InvalidCreateInputData data)
    {
        // Act
        var result = ShippingMethod.Create(
            name: data.Name ?? string.Empty, presentation: data.Presentation ?? string.Empty, type: data.Type, baseCost: data.BaseCost,
            estimatedDaysMin: data.EstimatedDaysMin, estimatedDaysMax: data.EstimatedDaysMax, position: data.Position);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain(expected: data.ExpectedErrorCode);
    }

    [Fact]
    public void Create_ShouldSetDefaultValues_WhenOptionalParametersAreOmitted()
    {
        // Arrange
        var name = "Default Test";
        var presentation = "Default Presentation";
        var type = ShippingType.Standard;
        var baseCost = 0m;

        // Act
        var result = ShippingMethod.Create(name: name, presentation: presentation, type: type, baseCost: baseCost);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Active.Should().BeTrue();
        result.Value.Description.Should().BeNull();
        result.Value.EstimatedDaysMin.Should().BeNull();
        result.Value.EstimatedDaysMax.Should().BeNull();
        result.Value.MaxWeight.Should().BeNull();
        result.Value.Position.Should().Be(expected: 0);
        result.Value.PublicMetadata.Should().BeEmpty();
        result.Value.PrivateMetadata.Should().BeEmpty();
        result.Value.DisplayOn.Should().Be(expected: DisplayOn.Both);
    }

    [Fact]
    public void Create_ShouldNormalizeNameAndPresentation()
    {
        // Arrange
        var name = "  Padded Name  ";
        var presentation = "  Padded Presentation  ";
        var type = ShippingType.Standard;
        var baseCost = 10m;

        // Act
        var result = ShippingMethod.Create(name: name, presentation: presentation, type: type, baseCost: baseCost);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Name.Should().Be(expected: name.Trim().ToSlug());
        result.Value.Presentation.Should().Be(expected: presentation.Trim());
    }

    [Fact]
    public void Update_ShouldUpdateAllProperties_WhenValid()
    {
        // Arrange
        var shippingMethod = CreateValidShippingMethod();
        var newName = "Updated Name";
        var newPresentation = "Updated Presentation";
        var newDescription = "New Description";
        var newBaseCost = 12.00m;
        var newActive = false;
        var newEstimatedDaysMin = 2;
        var newEstimatedDaysMax = 4;
        var newMaxWeight = 50.0m;
        var newPosition = 1;
        var newPublicMetadata = new Dictionary<string, object?> { { "key1", "value1" } };
        var newPrivateMetadata = new Dictionary<string, object?> { { "internalKey", 123 } };

        // Act
        var result = shippingMethod.Update(
            name: newName,
            presentation: newPresentation,
            description: newDescription,
            baseCost: newBaseCost,
            active: newActive,
            estimatedDaysMin: newEstimatedDaysMin,
            estimatedDaysMax: newEstimatedDaysMax,
            maxWeight: newMaxWeight,
            position: newPosition,
            publicMetadata: newPublicMetadata,
            privateMetadata: newPrivateMetadata);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Updated);
        shippingMethod.Name.Should().Be(expected: newName.ToSlug());
        shippingMethod.Presentation.Should().Be(expected: newPresentation);
        shippingMethod.Description.Should().Be(expected: newDescription);
        shippingMethod.BaseCost.Should().Be(expected: newBaseCost);
        shippingMethod.Active.Should().Be(expected: newActive);
        shippingMethod.EstimatedDaysMin.Should().Be(expected: newEstimatedDaysMin);
        shippingMethod.EstimatedDaysMax.Should().Be(expected: newEstimatedDaysMax);
        shippingMethod.MaxWeight.Should().Be(expected: newMaxWeight);
        shippingMethod.Position.Should().Be(expected: newPosition);
        shippingMethod.PublicMetadata.Should().ContainKey(expected: "key1").And.ContainValue(expected: "value1");
        shippingMethod.PrivateMetadata.Should().ContainKey(expected: "internalKey").And.ContainValue(expected: 123);
        shippingMethod.UpdatedAt.Should().BeCloseTo(nearbyTime: DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(seconds: 5));
        shippingMethod.DomainEvents.Should().ContainSingle(predicate: e => e is ShippingMethod.Events.Updated);
    }

    [Fact]
    public void Update_ShouldNotUpdateIfNoChanges()
    {
        // Arrange
        var shippingMethod = CreateValidShippingMethod(name: "Original Name", presentation: "Original Presentation");
        var originalUpdatedAt = shippingMethod.UpdatedAt; // Assuming UpdatedAt is initialized in Create or a default

        // Act
        var result = shippingMethod.Update(name: "Original Name", presentation: "Original Presentation");

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Updated); // Still returns Updated, but no actual change

        // Assert that UpdatedAt has not changed if no properties were effectively changed
        // This requires careful handling of when UpdateAt is set inside the Update method
        shippingMethod.UpdatedAt.Should().Be(expected: originalUpdatedAt);

        shippingMethod.DomainEvents.Should().NotContain(predicate: e => e is ShippingMethod.Events.Updated);
    }

    public static IEnumerable<object[]> GetInvalidUpdateNameOrPresentationInputs()
    {
        yield return [new InvalidUpdateInputData(Name: "", Presentation: "Presentation", ExpectedErrorCode: "ShippingMethod.Name.Required")];
        yield return
        [
            new InvalidUpdateInputData(
            Name: "TooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongNameTooLongName", Presentation: "Presentation", ExpectedErrorCode: "ShippingMethod.Name.TooLong")
        ];
        yield return [new InvalidUpdateInputData(Name: "Name", Presentation: "", ExpectedErrorCode: "ShippingMethod.Presentation.Required")];
    }

    [Theory]
    [MemberData(memberName: nameof(GetInvalidUpdateNameOrPresentationInputs))]
    public void Update_ShouldReturnError_WhenInvalidNameOrPresentation(InvalidUpdateInputData data)
    {
        // Arrange
        var shippingMethod = CreateValidShippingMethod();

        // Act
        var result = shippingMethod.Update(name: data.Name, presentation: data.Presentation);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain(expected: data.ExpectedErrorCode);
    }

    [Fact]
    public void Update_ShouldReturnError_WhenBaseCostIsNegative()
    {
        // Arrange
        var shippingMethod = CreateValidShippingMethod();

        // Act
        var result = shippingMethod.Update(baseCost: -1.0m);

        // Assert
        result.IsError.Should().BeTrue();
        result.Errors.Any(predicate: m => m.Code.Contains(value: "ShippingMethod.BaseCost.InvalidRange")).Should().BeTrue();
    }

    [Fact]
    public void Update_ShouldReturnError_WhenMaxWeightIsNegative()
    {
        // Arrange
        var shippingMethod = CreateValidShippingMethod();

        // Act
        var result = shippingMethod.Update(maxWeight: -1.0m);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain(expected: "InvalidRange"); // From CommonInput.Errors.InvalidRange
        result.FirstError.Code.Should().Contain(expected: "MaxWeight");
    }

    [Fact]
    public void Update_ShouldReturnError_WhenPositionIsNegative()
    {
        // Arrange
        var shippingMethod = CreateValidShippingMethod();

        // Act
        var result = shippingMethod.Update(position: -1);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Contain(expected: "ShippingMethod.Position.InvalidRange");
    }

    [Fact]
    public void CalculateCost_ShouldReturnZeroForFreeShipping()
    {
        // Arrange
        var freeShippingMethod = CreateValidShippingMethod(type: ShippingType.FreeShipping, baseCost: 10.0m);
        var zeroCostMethod = CreateValidShippingMethod(type: ShippingType.Standard, baseCost: 0.0m);

        // Act
        var cost1 = freeShippingMethod.CalculateCost(orderWeight: 10.0m, orderTotal: 100.0m);
        var cost2 = zeroCostMethod.CalculateCost(orderWeight: 10.0m, orderTotal: 100.0m);

        // Assert
        cost1.Should().Be(expected: 0.0m);
        cost2.Should().Be(expected: 0.0m);
    }

    [Fact]
    public void CalculateCost_ShouldApplySurchargeForOverweight()
    {
        // Arrange
        var shippingMethodResult = ShippingMethod.Create(name: "Test", presentation: "Test", type: ShippingType.Standard, baseCost: 10.0m);
        shippingMethodResult.IsError.Should().BeFalse();
        var shippingMethod = shippingMethodResult.Value;
        shippingMethod.MaxWeight = 20.0m; // Set MaxWeight directly

        // Act
        var cost = shippingMethod.CalculateCost(orderWeight: 25.0m, orderTotal: 100.0m);

        // Assert
        cost.Should().Be(expected: 15.0m); // 10.0 * 1.5
    }

    [Fact]
    public void CalculateCost_ShouldReturnBaseCostForNormalWeight()
    {
        // Arrange
        var shippingMethodResult = ShippingMethod.Create(name: "Test", presentation: "Test", type: ShippingType.Standard, baseCost: 10.0m);
        shippingMethodResult.IsError.Should().BeFalse();
        var shippingMethod = shippingMethodResult.Value;
        shippingMethod.MaxWeight = 20.0m; // Set MaxWeight directly

        // Act
        var cost1 = shippingMethod.CalculateCost(orderWeight: 15.0m, orderTotal: 100.0m);
        var cost2 = shippingMethod.CalculateCost(orderWeight: 20.0m, orderTotal: 100.0m); // Exactly max weight

        // Assert
        cost1.Should().Be(expected: 10.0m);
        cost2.Should().Be(expected: 10.0m);
    }

    [Fact]
    public void Delete_ShouldPublishDeletedEvent()
    {
        // Arrange
        var shippingMethod = CreateValidShippingMethod();

        // Act
        var result = shippingMethod.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Deleted);
        shippingMethod.DomainEvents.Should().ContainSingle(predicate: e => e is ShippingMethod.Events.Deleted);
    }

    [Fact]
    public void IsFreeShipping_ShouldReturnCorrectValue()
    {
        // Arrange
        var freeType = CreateValidShippingMethod(type: ShippingType.FreeShipping, baseCost: 10.0m);
        var zeroCost = CreateValidShippingMethod(type: ShippingType.Standard, baseCost: 0.0m);
        var paidType = CreateValidShippingMethod(type: ShippingType.Standard, baseCost: 5.0m);

        // Assert
        freeType.IsFreeShipping.Should().BeTrue();
        zeroCost.IsFreeShipping.Should().BeTrue();
        paidType.IsFreeShipping.Should().BeFalse();
    }

    [Fact]
    public void IsExpressShipping_ShouldReturnCorrectValue()
    {
        // Arrange
        var expressType = CreateValidShippingMethod(type: ShippingType.Express);
        var overnightType = CreateValidShippingMethod(type: ShippingType.Overnight);
        var standardType = CreateValidShippingMethod(type: ShippingType.Standard);

        // Assert
        expressType.IsExpressShipping.Should().BeTrue();
        overnightType.IsExpressShipping.Should().BeTrue();
        standardType.IsExpressShipping.Should().BeFalse();
    }

    [Fact]
    public void EstimatedDelivery_ShouldReturnFormattedString()
    {
        // Arrange
        var methodWithEstimates = CreateValidShippingMethod(estimatedDaysMin: 3, estimatedDaysMax: 5);

        // Assert
        methodWithEstimates.EstimatedDelivery.Should().Be(expected: "3-5 days");
    }

    [Fact]
    public void EstimatedDelivery_ShouldReturnDefaultString_WhenEstimatesAreNull()
    {
        // Arrange
        var methodWithoutEstimates = CreateValidShippingMethod(estimatedDaysMin: null, estimatedDaysMax: null);

        // Assert
        methodWithoutEstimates.EstimatedDelivery.Should().Be(expected: "Standard delivery");
    }
}