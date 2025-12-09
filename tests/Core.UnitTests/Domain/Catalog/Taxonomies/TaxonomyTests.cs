using Core.UnitTests.Domain.Catalog.Taxonomies.Taxa;

using ErrorOr;

using FluentAssertions;

using ReSys.Core.Domain.Catalog.Taxonomies;

using static ReSys.Core.Domain.Catalog.Taxonomies.Taxonomy; // For Taxonomy.Errors and Taxonomy.Events

namespace Core.UnitTests.Domain.Catalog.Taxonomies;

public class TaxonomyTests
{
    // Helper method to create a valid Taxonomy instance
    private static Taxonomy CreateValidTaxonomy(Guid storeId, string name = "test-taxonomy", string presentation = "Test Taxonomy")
    {
        var result = Taxonomy.Create(storeId: storeId, name: name, presentation: presentation);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    [Fact]
    public void Taxonomy_Create_ShouldReturnTaxonomy_WhenValidParameters()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var name = "product-categories";
        var presentation = "Product Categories";

        // Act
        var result = Taxonomy.Create(storeId: storeId, name: name, presentation: presentation);

        // Assert
        result.IsError.Should().BeFalse();
        var taxonomy = result.Value;
        taxonomy.Should().NotBeNull();
        taxonomy.StoreId.Should().Be(expected: storeId);
        taxonomy.Name.Should().Be(expected: name); // Name is slugified by NormalizeParams
        taxonomy.Presentation.Should().Be(expected: presentation);
        taxonomy.Position.Should().Be(expected: 0); // Default position
        taxonomy.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Fact]
    public void Taxonomy_Update_ShouldUpdateNameAndPresentationAndRaiseEvent()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        taxonomy.ClearDomainEvents();

        var newName = "updated-categories";
        var newPresentation = "Updated Categories";
        var newPosition = 50;

        // Act
        var result = taxonomy.Update(name: newName, presentation: newPresentation, position: newPosition);

        // Assert
        result.IsError.Should().BeFalse();
        taxonomy.Name.Should().Be(expected: newName); // Slugified
        taxonomy.Presentation.Should().Be(expected: newPresentation);
        taxonomy.Position.Should().Be(expected: newPosition);
        taxonomy.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxonomy.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.NameChanged.Should().BeTrue();
    }

    [Fact]
    public void Taxonomy_Update_ShouldUpdateOnlyPresentationAndRaiseEvent_WhenNameNotChanged()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId, name: "my-taxonomy", presentation: "My Taxonomy");
        taxonomy.ClearDomainEvents();

        var newPresentation = "My Fancy Taxonomy";

        // Act
        var result = taxonomy.Update(presentation: newPresentation);

        // Assert
        result.IsError.Should().BeFalse();
        taxonomy.Name.Should().Be(expected: "my-taxonomy"); // Should remain unchanged
        taxonomy.Presentation.Should().Be(expected: newPresentation);
        taxonomy.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxonomy.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.NameChanged.Should().BeTrue(); // Presentation change implies name change for event purposes
    }

    [Fact]
    public void Taxonomy_Update_ShouldNotRaiseEvent_WhenNoChanges()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId, name: "my-taxonomy", presentation: "My Taxonomy");
        taxonomy.ClearDomainEvents();

        // Act
        var result = taxonomy.Update(name: "my-taxonomy", presentation: "My Taxonomy"); // No actual change

        // Assert
        result.IsError.Should().BeFalse();
        taxonomy.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxonomy_Delete_ShouldReturnDeletedResultAndRaiseEvent_WhenNoTaxons()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        taxonomy.ClearDomainEvents();

        // Act
        var result = taxonomy.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Deleted);
        taxonomy.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Deleted);
    }

    [Fact]
    public void Taxonomy_Delete_ShouldReturnHasTaxonsError_WhenHasTaxons()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        // Simulate adding a taxon to the taxonomy
        var taxon = TaxonTests.CreateValidTaxon(taxonomyId: taxonomy.Id);
        taxonomy.Taxons.Add(item: taxon);
        taxonomy.Taxons.Add(item: TaxonTests.CreateValidTaxon(taxonomyId: taxonomy.Id)); // Add a second one to ensure Taxons.Count > 1 
        taxonomy.ClearDomainEvents();

        // Act
        var result = taxonomy.Delete();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.HasTaxons);
        taxonomy.DomainEvents.Should().BeEmpty();
    }
}
