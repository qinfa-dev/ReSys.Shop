using FluentAssertions;
using ReSys.Core.Domain.Inventories;

namespace Core.UnitTests.Domain.Inventories;

public class NumberGeneratorTests
{
    [Fact]
    public void Generate_ShouldReturnStringWithCorrectPrefix()
    {
        // Arrange
        string prefix = "INV";

        // Act
        string generatedNumber = NumberGenerator.Generate(prefix: prefix);

        // Assert
        generatedNumber.Should().StartWith(expected: prefix);
    }

    [Fact]
    public void Generate_ShouldReturnDifferentNumbersOnSubsequentCalls()
    {
        // Arrange
        string prefix = "ORD";

        // Act
        string number1 = NumberGenerator.Generate(prefix: prefix);
        string number2 = NumberGenerator.Generate(prefix: prefix);
        string number3 = NumberGenerator.Generate(prefix: prefix);

        // Assert
        number1.Should().NotBe(unexpected: number2);
        number2.Should().NotBe(unexpected: number3);
        number1.Should().NotBe(unexpected: number3);
    }

    [Fact]
    public void Generate_ShouldIncludeCurrentDateInFormat()
    {
        // Arrange
        string prefix = "REF";
        string expectedDateFormat = DateTime.UtcNow.ToString(format: "yyMMdd");

        // Act
        string generatedNumber = NumberGenerator.Generate(prefix: prefix);

        // Assert
        generatedNumber.Should().Contain(expected: expectedDateFormat);
    }
}
