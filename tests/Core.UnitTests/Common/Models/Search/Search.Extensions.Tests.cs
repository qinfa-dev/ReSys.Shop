using FluentAssertions;

using Core.UnitTests.Common.Models.Shared;

using ReSys.Core.Common.Models.Search;

namespace Core.UnitTests.Common.Models.Search;

public sealed class SearchParamsExtensionsTests
{
    private readonly List<TestEntity> _testData;

    public SearchParamsExtensionsTests()
    {
        _testData = TestEntity.GetTestData();
    }

    private IQueryable<TestEntity> GetQueryableTestData() => _testData.AsQueryable();

    #region ApplySearch with SearchParams Tests

    [Fact]
    public void ApplySearch_WithNullSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: null);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(3);
        result[0].StringProperty.Should().Be("Apple");
        result[1].StringProperty.Should().Be("Banana");
        result[2].StringProperty.Should().Be("Blueberry");
    }

    [Fact]
    public void ApplySearch_WithEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void ApplySearch_WithWhitespaceSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "   ");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void ApplySearch_FullTextSearch_SearchesAllStringProperties()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "apple");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void ApplySearch_CaseInsensitiveByDefault_FindsMatches()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "APPLE");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void ApplySearch_WithCaseSensitiveOption_RespectsCasing()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "APPLE", CaseSensitive: true); // Pass CaseSensitive directly

        // Act
        var result = query.ApplySearch(searchParams).ToList(); // No 'options' parameter

        // Assert
        result.Count.Should().Be(0); // No matches with case sensitivity
    }

    [Fact]
    public void ApplySearch_WithExactMatchOption_FindsExactMatches()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Apple", ExactMatch: true); // Pass ExactMatch directly

        // Act
        var result = query.ApplySearch(searchParams).ToList(); // No 'options' parameter

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void ApplySearch_WithStartsWithOption_FindsStartsWithMatches()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Blue", StartsWith: true); // Pass StartsWith directly

        // Act
        var result = query.ApplySearch(searchParams).ToList(); // No 'options' parameter

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Blueberry");
    }

    [Fact]
    public void ApplySearch_WithNullableStringProperty_HandlesNullsGracefully()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "HasValue");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(1);
        result[0].NullableStringProperty.Should().Be("HasValue");
    }

    #endregion

    #region ApplySearch with SearchFields Tests

    [Fact]
    public void ApplySearch_WithSpecificSearchFields_SearchesOnlyThoseFields()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Apple", SearchFields: ["StringProperty"]);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void ApplySearch_WithMultipleSearchFields_SearchesAllSpecifiedFields()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Value", SearchFields: ["StringProperty", "NullableStringProperty"]);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(2); // "HasValue" and "AnotherValue"
        result.Should().Contain(e => e.Id == 1 && e.NullableStringProperty == "HasValue");
        result.Should().Contain(e => e.Id == 3 && e.NullableStringProperty == "AnotherValue");
    }

    [Fact]
    public void ApplySearch_WithInvalidSearchField_IgnoresInvalidField()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Apple", SearchFields: ["InvalidField", "StringProperty"]);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void ApplySearch_WithNonStringSearchField_IgnoresNonStringField()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "10", SearchFields: ["IntProperty", "StringProperty"]);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        // Should only search StringProperty, ignoring IntProperty
        result.Should().NotBeNull();
        result.Count.Should().Be(0); // "10" is not contained in any StringProperty values
    }

    [Fact]
    public void ApplySearch_WithEmptySearchFields_PerformsFullTextSearch()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Apple", SearchFields: []);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void ApplySearch_WithNullSearchFields_PerformsFullTextSearch()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Apple", SearchFields: null);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }





    #endregion

    #region ApplySearch with Expressions Tests

    [Fact]
    public void ApplySearch_WithSingleExpression_SearchesSpecifiedProperty()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.ApplySearch("Apple", e => e.StringProperty).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void ApplySearch_WithMultipleExpressions_SearchesAllSpecifiedProperties()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.ApplySearch("Value",
            e => e.StringProperty,
            e => e.NullableStringProperty!).ToList();

        // Assert
        result.Count.Should().Be(2);
        result.Should().Contain(e => e.NullableStringProperty == "HasValue");
        result.Should().Contain(e => e.NullableStringProperty == "AnotherValue");
    }

    [Fact]
    public void ApplySearch_WithExpressionsEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.ApplySearch("", e => e.StringProperty).ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void ApplySearch_WithExpressionsNullSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.ApplySearch(null!, e => e.StringProperty).ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void ApplySearch_WithNoExpressions_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.ApplySearch("Apple").ToList();

        // Assert
        result.Count.Should().Be(3); // No expressions provided, returns all
    }

    [Fact]
    public void ApplySearch_WithNullableStringExpression_HandlesNullsGracefully()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.ApplySearch("NonExistentValue", e => e.NullableStringProperty!).ToList();

        // Assert
        result.Count.Should().Be(0);
    }

    [Fact]
    public void ApplySearch_WithExpressions_AlwaysCaseInsensitive()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.ApplySearch("APPLE", e => e.StringProperty).ToList();

        // Assert
        // Expression-based search always converts to lowercase, so this should find "Apple"
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    #endregion

    #region SearchIn Tests

    [Fact]
    public void SearchIn_WithValidSearchTerm_ReturnsMatchingResults()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.SearchIn("Banana", e => e.StringProperty).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Banana");
    }

    [Fact]
    public void SearchIn_WithEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.SearchIn("", e => e.StringProperty).ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void SearchIn_WithNullSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.SearchIn(null!, e => e.StringProperty).ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void SearchIn_WithWhitespaceSearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.SearchIn("   ", e => e.StringProperty).ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void SearchIn_AlwaysCaseInsensitive()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.SearchIn("BANANA", e => e.StringProperty).ToList();

        // Assert
        // SearchIn uses expression-based search which is always case-insensitive
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Banana");
    }

    #endregion

    #region Search Builder Tests

    [Fact]
    public void Search_CreatesSearchBuilder()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var builder = query.Search("Apple");

        // Assert
        builder.Should().NotBeNull();
        builder.Should().BeOfType<SearchBuilder<TestEntity>>();
    }

    [Fact]
    public void SearchBuilder_WithSingleField_SearchesCorrectly()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("Apple")
            .In(e => e.StringProperty)
            .Execute()
            .ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void SearchBuilder_WithMultipleFields_SearchesCorrectly()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("Value")
            .In(e => e.StringProperty, e => e.NullableStringProperty!)
            .Execute()
            .ToList();

        // Assert
        result.Count.Should().Be(2);
        result.Should().Contain(e => e.NullableStringProperty == "HasValue");
        result.Should().Contain(e => e.NullableStringProperty == "AnotherValue");
    }

    [Fact]
    public void SearchBuilder_WithCaseSensitive_DoesNotRespectCasingSensitivityWhenUsingFields()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("APPLE")
            .In(e => e.StringProperty)
            .CaseSensitive()
            .Execute()
            .ToList();

        // Assert
        // When using fields, SearchBuilder calls expression-based ApplySearch which ignores case sensitivity options
        result.Count.Should().Be(1); // Still finds match because expression search is always case insensitive
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void SearchBuilder_WithCaseSensitive_RespectsOptionsWhenNoFieldsSpecified()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("APPLE")
            .CaseSensitive()
            .Execute()
            .ToList();

        // Assert
        // When no fields specified, it uses SearchParams-based search which respects options
        result.Count.Should().Be(0); // No matches with case sensitivity
    }

    [Fact]
    public void SearchBuilder_WithExactMatch_FindsExactMatches()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("Apple")
            .In(e => e.StringProperty)
            .ExactMatch()
            .Execute()
            .ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void SearchBuilder_WithStartsWith_FindsStartsWithMatches()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("Blue")
            .In(e => e.StringProperty)
            .StartsWith()
            .Execute()
            .ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Blueberry");
    }

    [Fact]
    public void SearchBuilder_WithChainedOptions_AppliesAllOptions()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("blue")
            .In(e => e.StringProperty)
            .StartsWith()
            .CaseSensitive(false) // Explicitly set case insensitive
            .Execute()
            .ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Blueberry");
    }

    [Fact]
    public void SearchBuilder_WithEmptySearchTerm_ReturnsOriginalQuery()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("")
            .In(e => e.StringProperty)
            .Execute()
            .ToList();

        // Assert
        result.Count.Should().Be(3);
    }

    [Fact]
    public void SearchBuilder_WithNoFields_PerformsFullTextSearch()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query.Search("Apple")
            .Execute()
            .ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    [Fact]
    public void SearchBuilder_ImplicitConversion_Works()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        Func<IQueryable<TestEntity>> searchFunc = query.Search("Apple").In(e => e.StringProperty);
        var result = searchFunc().ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
    }

    #endregion



    #region Edge Cases and Error Handling Tests

    [Fact]
    public void ApplySearch_WithComplexSearchTerm_HandlesCorrectly()
    {
        // Arrange
        var query = GetQueryableTestData();
        var searchParams = new SearchParams(SearchTerm: "Has Value");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        // Should not find anything since "Has Value" (with space) is not in the data
        result.Count.Should().Be(0);
    }

    [Fact]
    public void ApplySearch_WithSpecialCharacters_HandlesCorrectly()
    {
        // Arrange
        var testDataWithSpecialChars = new List<TestEntity>
        {
            new TestEntity
            {
                Id = 99,
                StringProperty = "Test@Email.com",
                NullableStringProperty = "Special-Character_Value",
                Status = TestStatus.Active
            }
        };
        var query = testDataWithSpecialChars.AsQueryable();
        var searchParams = new SearchParams(SearchTerm: "@Email");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Test@Email.com");
    }

    [Fact]
    public void ApplySearch_CombinedWithOtherLinqOperations_WorksCorrectly()
    {
        // Arrange
        var query = GetQueryableTestData()
            .Where(e => e.Status == TestStatus.Active)
            .OrderBy(e => e.Id);
        var searchParams = new SearchParams(SearchTerm: "Apple");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].StringProperty.Should().Be("Apple");
        result[0].Status.Should().Be(TestStatus.Active);
    }

    [Fact]
    public void ApplySearch_WithLargeDataset_PerformsEfficiently()
    {
        // Arrange
        var largeDataset = new List<TestEntity>();
        for (int i = 0; i < 1000; i++)
        {
            largeDataset.Add(new TestEntity
            {
                Id = i,
                StringProperty = i % 2 == 0 ? $"Even{i}" : $"Odd{i}",
                NullableStringProperty = i % 10 == 0 ? null : $"Value{i}",
                Status = TestStatus.Active
            });
        }
        var query = largeDataset.AsQueryable();
        var searchParams = new SearchParams(SearchTerm: "Even");

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(500); // Half should match "Even"
        result.All(e => e.StringProperty.Contains("Even")).Should().BeTrue();
    }

    #endregion

    #region Integration Tests with TestEntity Properties

    [Fact]
    public void ApplySearch_WorksWithTestEntityComplexScenario()
    {
        // Arrange
        var query = GetQueryableTestData()
            .Where(e => e.Status != TestStatus.Inactive);
        var searchParams = new SearchParams(SearchTerm: "Value", SearchFields: ["NullableStringProperty"]);

        // Act
        var result = query.ApplySearch(searchParams).ToList();

        // Assert
        result.Count.Should().Be(1); // Only the Active entity with "HasValue"
        result[0].Id.Should().Be(1);
        result[0].NullableStringProperty.Should().Be("HasValue");
        result[0].Status.Should().Be(TestStatus.Active);
    }

    [Fact]
    public void SearchBuilder_WorksWithTestEntityPropertiesChaining()
    {
        // Arrange
        var query = GetQueryableTestData();

        // Act
        var result = query
            .Where(e => e.IntProperty >= 20)
            .Search("another")
            .In(e => e.NullableStringProperty!)
            .CaseSensitive(false)
            .Execute()
            .OrderBy(e => e.Id)
            .ToList();

        // Assert
        result.Count.Should().Be(1);
        result[0].Id.Should().Be(3);
        result[0].NullableStringProperty.Should().Be("AnotherValue");
        result[0].IntProperty.Should().Be(30);
    }

    #endregion
}