using FluentAssertions;

using ErrorOr;

using ReSys.Core.Common.Extensions;
using ReSys.Core.Domain.Catalog.Taxonomies;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;
using ReSys.Core.Domain.Catalog.Taxonomies.Images;
using ReSys.Core.Domain.Catalog.Taxonomies.Rules;
using Humanizer;

using static ReSys.Core.Domain.Catalog.Taxonomies.Taxa.Taxon; // For Taxon.Errors and Taxon.Events

namespace Core.UnitTests.Domain.Catalog.Taxonomies.Taxa;

public class TaxonTests
{
    // Helper method to create a valid Taxonomy instance (copied for self-containment)
    private static Taxonomy CreateValidTaxonomy(Guid storeId, string name = "test-taxonomy", string presentation = "Test Taxonomy")
    {
        var result = Taxonomy.Create(storeId: storeId, name: name, presentation: presentation);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper method to create a valid Taxon instance
    public static Taxon CreateValidTaxon(Guid taxonomyId, string name = "test-taxon", Guid? parentId = null, string presentation = "Test Taxon",
        bool hideFromNav = false, bool automatic = false, string? rulesMatchPolicy = null, string? sortOrder = null,
        string? metaTitle = null, string? metaDescription = null, string? metaKeywords = null,
        int position = 0, IDictionary<string, object?>? publicMetadata = null, IDictionary<string, object?>? privateMetadata = null)
    {
        var result = Taxon.Create(taxonomyId: taxonomyId, name: name, parentId: parentId, presentation: presentation,
            hideFromNav: hideFromNav, automatic: automatic, rulesMatchPolicy: rulesMatchPolicy, sortOrder: sortOrder,
            metaTitle: metaTitle, metaDescription: metaDescription, metaKeywords: metaKeywords,
            position: position, publicMetadata: publicMetadata, privateMetadata: privateMetadata);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper for TaxonRule (used in AddTaxonRule tests)
    private static TaxonRule CreateValidTaxonRule(Guid taxonId, string type = "product_name", string value = "test", string matchPolicy = "contains", string? propertyName = null)
    {
        var result = TaxonRule.Create(taxonId: taxonId, type: type, value: value, matchPolicy: matchPolicy, propertyName: propertyName);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    // Helper for TaxonImage (used in AddImage tests)
    private static TaxonImage CreateValidTaxonImage(Guid taxonId, string type = "default", string url = "http://example.com/image.jpg")
    {
        var result = TaxonImage.Create(taxonId: taxonId, type: type, url: url);
        result.IsError.Should().BeFalse();
        return result.Value;
    }

    [Fact]
    public void Taxon_Create_ShouldReturnRootTaxon_WhenValidParametersAndNoParent()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        var name = "root-category";
        var presentation = "Root Category";

        // Act
        var result = Taxon.Create(taxonomyId: taxonomy.Id, name: name, parentId: null, presentation: presentation);

        // Assert
        result.IsError.Should().BeFalse();
        var taxon = result.Value;
        taxon.Should().NotBeNull();
        taxon.TaxonomyId.Should().Be(expected: taxonomy.Id);
        taxon.Name.Should().Be(expected: name.ToSlug()); // Slugified
        taxon.Presentation.Should().Be(expected: presentation);
        taxon.ParentId.Should().BeNull();
        taxon.IsRoot.Should().BeTrue();
        taxon.Permalink.Should().Be(expected: name.ToSlug());
        taxon.PrettyName.Should().Be(expected: presentation);
        taxon.Lft.Should().Be(expected: 0); // Initial
        taxon.Rgt.Should().Be(expected: 0); // Initial
        taxon.Depth.Should().Be(expected: 0); // Initial
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Fact]
    public void Taxon_Create_ShouldInitializeAutomaticPropertiesCorrectly_WhenAutomaticIsTrueAndNoSpecificPolicyOrOrderProvided()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        var name = "auto-category";
        var presentation = "Auto Category";

        // Act
        var result = Taxon.Create(taxonomyId: taxonomy.Id, name: name, parentId: null, presentation: presentation, automatic: true);

        // Assert
        result.IsError.Should().BeFalse();
        var taxon = result.Value;
        taxon.Should().NotBeNull();
        taxon.Automatic.Should().BeTrue();
        taxon.RulesMatchPolicy.Should().Be(expected: "all");
        taxon.SortOrder.Should().Be(expected: "manual"); // Ensure default if not provided
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Fact]
    public void Taxon_Create_ShouldUseProvidedAutomaticProperties_WhenAutomaticIsTrue()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        var name = "custom-auto-category";
        var presentation = "Custom Auto Category";
        var rulesMatchPolicy = "any";
        var sortOrder = "best-selling";

        // Act
        var result = Taxon.Create(taxonomyId: taxonomy.Id, name: name, parentId: null, presentation: presentation,
            automatic: true, rulesMatchPolicy: rulesMatchPolicy, sortOrder: sortOrder);

        // Assert
        result.IsError.Should().BeFalse();
        var taxon = result.Value;
        taxon.Should().NotBeNull();
        taxon.Automatic.Should().BeTrue();
        taxon.RulesMatchPolicy.Should().Be(expected: rulesMatchPolicy);
        taxon.SortOrder.Should().Be(expected: sortOrder);
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Fact]
    public void Taxon_Create_ShouldDefaultPresentationToName_WhenPresentationIsNull()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        var name = "category-with-no-presentation";

        // Act
        var result = Taxon.Create(taxonomyId: taxonomy.Id, name: name, parentId: null, presentation: null);

        // Assert
        result.IsError.Should().BeFalse();
        var taxon = result.Value;
        taxon.Should().NotBeNull();
        taxon.Presentation.Should().Be(expected: name.Humanize(casing: LetterCasing.Title)); // Corrected assertion
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Fact]
    public void Taxon_Create_ShouldInitializeMetadataDictionaries()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        var name = "metadata-test-category";

        // Act
        var result = Taxon.Create(taxonomyId: taxonomy.Id, name: name, parentId: null);

        // Assert
        result.IsError.Should().BeFalse();
        var taxon = result.Value;
        taxon.Should().NotBeNull();
        taxon.PublicMetadata.Should().NotBeNull().And.BeEmpty();
        taxon.PrivateMetadata.Should().NotBeNull().And.BeEmpty();
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Fact]
    public void Taxon_Create_ShouldReturnChildTaxon_WhenValidParametersAndParent()
    {
        // Arrange
        var storeId = Guid.NewGuid();
        var taxonomy = CreateValidTaxonomy(storeId: storeId);
        var rootTaxon = CreateValidTaxon(taxonomyId: taxonomy.Id, name: "root", presentation: "Root Category", hideFromNav: false, automatic: false);
        rootTaxon.UpdateNestedSet(lft: 1, rgt: 4, depth: 0); // Simulate nested set values
        rootTaxon.RegeneratePermalinkAndPrettyName(parentPermalink: null, parentPrettyName: null); // Simulate permalink generation

        var childName = "child-category";
        var childPresentation = "Child Category";

        // Act
        var result = Taxon.Create(taxonomyId: taxonomy.Id, name: childName, parentId: rootTaxon.Id, presentation: childPresentation);

        // Assert
        result.IsError.Should().BeFalse();
        var childTaxon = result.Value;
        childTaxon.Should().NotBeNull();
        childTaxon.TaxonomyId.Should().Be(expected: taxonomy.Id);
        childTaxon.Name.Should().Be(expected: childName.ToSlug());
        childTaxon.Presentation.Should().Be(expected: childPresentation);
        childTaxon.ParentId.Should().Be(expected: rootTaxon.Id);
        childTaxon.IsRoot.Should().BeFalse();
        childTaxon.Permalink.Should().Be(expected: childName.ToSlug()); // Corrected assertion: Create only sets child's own slug
        childTaxon.PrettyName.Should().Be(expected: childPresentation); // Corrected assertion
        childTaxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Created);
    }

    [Fact]
    public void Taxon_Update_ShouldUpdateBasicPropertiesAndRegeneratePermalink()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "old-name", presentation: "Old Presentation", hideFromNav: false, automatic: false);
        taxon.ClearDomainEvents();

        var newName = "new-name";
        var newPresentation = "New Presentation";
        var newDescription = "New Description";
        var newPosition = 5;

        // Act
        var result = taxon.Update(name: newName, presentation: newPresentation, description: newDescription, position: newPosition);
        taxon.RegeneratePermalinkAndPrettyName(parentPermalink: null, parentPrettyName: null); // Manually call as it's not done in Update itself

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Name.Should().Be(expected: newName);
        taxon.Presentation.Should().Be(expected: newPresentation);
        taxon.Description.Should().Be(expected: newDescription);
        taxon.Position.Should().Be(expected: newPosition);
        taxon.Permalink.Should().Be(expected: newName.ToSlug()); // Corrected assertion
        taxon.PrettyName.Should().Be(expected: newPresentation); // PrettyName derived from presentation
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxon.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.NameOrPresentationChanged.Should().BeTrue();
    }

    [Fact]
    public void Taxon_Update_ShouldMarkForRegenerateProducts_WhenAutomaticChanges()
    {
        // Arrange
        Guid.NewGuid();
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: false); // Initially manual
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(automatic: true);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Automatic.Should().BeTrue();
        taxon.MarkedForRegenerateTaxonProducts.Should().BeFalse(); // Flag is reset after event dispatch
        taxon.DomainEvents.Should().Contain(predicate: e => e is Events.Updated);
        taxon.DomainEvents.Should().Contain(predicate: e => e is Events.RegenerateProducts);
    }

    [Fact]
    public void Taxon_Update_ShouldMarkForRegenerateProducts_WhenRulesMatchPolicyChanges()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true, rulesMatchPolicy: "all"); // Initially "all"
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(rulesMatchPolicy: "any");

        // Assert
        result.IsError.Should().BeFalse();
        taxon.RulesMatchPolicy.Should().Be(expected: "any");
        taxon.MarkedForRegenerateTaxonProducts.Should().BeFalse(); // Flag is reset after event dispatch
        taxon.DomainEvents.Should().Contain(predicate: e => e is Events.Updated);
        taxon.DomainEvents.Should().Contain(predicate: e => e is Events.RegenerateProducts);
    }

    [Fact]
    public void Taxon_Update_ShouldNotMarkForRegenerateProducts_WhenOnlyCosmeticChanges()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "original", presentation: "Original", hideFromNav: false, automatic: false);
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(presentation: "New Pretty Name", description: "Updated Description");

        // Assert
        result.IsError.Should().BeFalse();
        taxon.MarkedForRegenerateTaxonProducts.Should().BeFalse();
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        taxon.DomainEvents.Should().NotContain(predicate: e => e is Events.RegenerateProducts);
    }

    [Fact]
    public void Taxon_Update_ShouldUpdatePublicMetadata()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var initialMetadata = new Dictionary<string, object?> { { "key1", "value1" } };
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, publicMetadata: initialMetadata);
        taxon.ClearDomainEvents();

        var newMetadata = new Dictionary<string, object?> { { "key2", "value2" }, { "key3", 123 } };

        // Act
        var result = taxon.Update(publicMetadata: newMetadata);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.PublicMetadata.Should().BeEquivalentTo(expectation: newMetadata);
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
    }

    [Fact]
    public void Taxon_Update_ShouldUpdatePrivateMetadata()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var initialMetadata = new Dictionary<string, object?> { { "internalKey", "internalValue" } };
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, privateMetadata: initialMetadata);
        taxon.ClearDomainEvents();

        var newMetadata = new Dictionary<string, object?> { { "newInternalKey", 456 }, { "otherKey", "data" } };

        // Act
        var result = taxon.Update(privateMetadata: newMetadata);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.PrivateMetadata.Should().BeEquivalentTo(expectation: newMetadata);
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
    }

    [Fact]
    public void Taxon_Update_ShouldResetMarkedForRegenerateTaxonProductsFlag_AfterEventDispatch()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true);
        taxon.MarkedForRegenerateTaxonProducts = true; // Manually set to simulate prior change
        taxon.ClearDomainEvents();

        // Act
        // Make a change that would typically trigger regeneration and clear the flag, e.g., rulesMatchPolicy
        var result = taxon.Update(rulesMatchPolicy: "all");

        // Assert
        result.IsError.Should().BeFalse(); // Added this assertion for consistency
        taxon.MarkedForRegenerateTaxonProducts.Should().BeFalse(); // Should be reset after event dispatch
        taxon.DomainEvents.Should().Contain(predicate: e => e is Events.RegenerateProducts);
    }

    [Fact]
    public void Taxon_Update_ShouldSetUpdatedAtAndRaiseUpdatedEvent_WhenChangesOccur()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "old-name");
        var initialUpdatedAt = taxon.UpdatedAt; // This can be null initially

        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(name: "new-name");

        // Assert
        result.IsError.Should().BeFalse();
        taxon.UpdatedAt.Should().NotBeNull();
        if (initialUpdatedAt.HasValue) // Only assert BeAfter if initialUpdatedAt was not null
        {
            taxon.UpdatedAt.Should().BeAfter(expected: initialUpdatedAt.Value);
        }
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxon.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.TaxonId.Should().Be(expected: taxon.Id);
    }

    [Fact]
    public void Taxon_Update_ShouldSetUpdatedEventNameOrPresentationChanged_WhenOnlyNameChanges()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "old-name", presentation: "Old Presentation");
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(name: "new-name");

        // Assert
        result.IsError.Should().BeFalse();
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxon.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.NameOrPresentationChanged.Should().BeTrue();
    }

    [Fact]
    public void Taxon_Update_ShouldSetUpdatedEventNameOrPresentationChanged_WhenOnlyPresentationChanges()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "name", presentation: "Old Presentation");
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(presentation: "New Presentation");

        // Assert
        result.IsError.Should().BeFalse();
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxon.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.NameOrPresentationChanged.Should().BeTrue();
    }

    [Fact]
    public void Taxon_Update_ShouldSetUpdatedEventNameOrPresentationChanged_WhenBothNameAndPresentationChange()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "old-name", presentation: "Old Presentation");
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(name: "new-name", presentation: "New Presentation");

        // Assert
        result.IsError.Should().BeFalse();
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxon.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.NameOrPresentationChanged.Should().BeTrue();
    }

    [Fact]
    public void Taxon_Update_ShouldNotSetUpdatedEventNameOrPresentationChanged_WhenNameAndPresentationDoNotChange()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "name", presentation: "Presentation");
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Update(description: "New Description"); // Change a non-name/presentation property

        // Assert
        result.IsError.Should().BeFalse();
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
        var updatedEvent = taxon.DomainEvents.First() as Events.Updated;
        updatedEvent.Should().NotBeNull();
        updatedEvent!.NameOrPresentationChanged.Should().BeFalse();
    }

    [Fact]
    public void Taxon_Update_ShouldHandleParentIdChangeThroughSetParent()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var rootTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "root");
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child", parentId: null);
        childTaxon.ClearDomainEvents();

        // Act
        var result = childTaxon.Update(parentId: rootTaxon.Id);

        // Assert
        result.IsError.Should().BeFalse();
        childTaxon.ParentId.Should().Be(expected: rootTaxon.Id);
        childTaxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Moved);
        var movedEvent = childTaxon.DomainEvents.First() as Events.Moved;
        movedEvent.Should().NotBeNull();
        movedEvent!.OldParentId.Should().BeNull();
        movedEvent.NewParentId.Should().Be(expected: rootTaxon.Id);
    }

    [Fact]
    public void Taxon_SetParent_ShouldUpdateParentIdAndRaiseEvent()
    {
        // Arrange
        Guid.NewGuid();
        var taxonomyId = Guid.NewGuid();
        var rootTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "root", hideFromNav: false, automatic: false);
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child", parentId: null, hideFromNav: false, automatic: false);
        childTaxon.ClearDomainEvents();

        // Act
        var result = childTaxon.SetParent(newParentId: rootTaxon.Id, newIndex: 1);

        // Assert
        result.IsError.Should().BeFalse();
        childTaxon.ParentId.Should().Be(expected: rootTaxon.Id);
        childTaxon.Position.Should().Be(expected: 1);
        childTaxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Moved);
        var movedEvent = childTaxon.DomainEvents.First() as Events.Moved;
        movedEvent.Should().NotBeNull();
        movedEvent!.OldParentId.Should().BeNull();
        movedEvent.NewParentId.Should().Be(expected: rootTaxon.Id);
    }

    [Fact]
    public void Taxon_SetParent_ShouldReturnSelfParentingError_WhenParentIsSelf()
    {
        // Arrange
        Guid.NewGuid();
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, hideFromNav: false, automatic: false);
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.SetParent(newParentId: taxon.Id, newIndex: 1);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.SelfParenting);
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_SetParent_ShouldSetParentToNull_WhenNewParentIdIsNull()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var rootTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "root");
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child", parentId: rootTaxon.Id);
        childTaxon.ClearDomainEvents();

        // Act
        var result = childTaxon.SetParent(newParentId: null, newIndex: 0);

        // Assert
        result.IsError.Should().BeFalse();
        childTaxon.ParentId.Should().BeNull();
        childTaxon.IsRoot.Should().BeTrue();
        childTaxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Moved);
        var movedEvent = childTaxon.DomainEvents.First() as Events.Moved;
        movedEvent.Should().NotBeNull();
        movedEvent!.OldParentId.Should().Be(expected: rootTaxon.Id);
        movedEvent.NewParentId.Should().BeNull();
    }

    [Fact]
    public void Taxon_SetParent_ShouldUpdatePosition_WhenNewIndexIsProvided()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, position: 10);
        taxon.ClearDomainEvents();
        var newPosition = 5;

        // Act
        var result = taxon.SetParent(newParentId: taxon.ParentId, newIndex: newPosition);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Position.Should().Be(expected: newPosition);
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Moved);
        var movedEvent = taxon.DomainEvents.First() as Events.Moved;
        movedEvent.Should().NotBeNull();
        movedEvent!.NewIndex.Should().Be(expected: newPosition);
    }

    [Fact]
    public void Taxon_Delete_ShouldReturnDeletedResultAndRaiseEvent_WhenNoChildren()
    {
        // Arrange
        Guid.NewGuid();
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, hideFromNav: false, automatic: false);
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.Delete();

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().Be(expected: Result.Deleted);
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Deleted);
    }

    [Fact]
    public void Taxon_Delete_ShouldReturnHasChildrenError_WhenHasChildren()
    {
        // Arrange
        Guid.NewGuid();
        var taxonomyId = Guid.NewGuid();
        var parentTaxon = CreateValidTaxon(taxonomyId: taxonomyId, hideFromNav: false, automatic: false);
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, parentId: parentTaxon.Id, hideFromNav: false, automatic: false);
        parentTaxon.Children.Add(item: childTaxon); // Manually add child
        parentTaxon.ClearDomainEvents();

        // Act
        var result = parentTaxon.Delete();

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: Errors.HasChildren);
        parentTaxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_AddTaxonRule_ShouldAddRuleAndMarkForRegenerationAndRaiseEvents()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true, hideFromNav: false); // Must be automatic
        taxon.ClearDomainEvents();

        var rule = CreateValidTaxonRule(taxonId: taxon.Id);

        // Act
        var result = taxon.AddTaxonRule(rule: rule);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.TaxonRules.Should().Contain(expected: rule);
        taxon.MarkedForRegenerateTaxonProducts.Should().BeFalse(); // Flag is reset after event dispatch
        taxon.DomainEvents.Should().Contain(predicate: e => e is Events.Updated);
        taxon.DomainEvents.Should().Contain(predicate: e => e is Events.RegenerateProducts);
    }

    [Fact]
    public void Taxon_AddTaxonRule_ShouldReturnError_WhenRuleIsNull()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true);
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.AddTaxonRule(rule: null);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: TaxonRule.Errors.Required);
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_AddTaxonRule_ShouldReturnError_WhenTaxonIdMismatch()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true);
        taxon.ClearDomainEvents();

        var rule = CreateValidTaxonRule(taxonId: Guid.NewGuid()); // Rule for a different taxon

        // Act
        var result = taxon.AddTaxonRule(rule: rule);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: TaxonRule.Errors.TaxonMismatch(id: rule.TaxonId, taxonId: taxon.Id));
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_AddTaxonRule_ShouldReturnError_WhenDuplicateRule()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true);
        var rule = CreateValidTaxonRule(taxonId: taxon.Id);
        taxon.AddTaxonRule(rule: rule); // Add once
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.AddTaxonRule(rule: rule); // Add again

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: TaxonRule.Errors.Duplicate);
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_RemoveRule_ShouldRemoveRuleAndMarkForRegenerationAndRaiseEvents()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true);
        var rule = CreateValidTaxonRule(taxonId: taxon.Id);
        taxon.AddTaxonRule(rule: rule); // Add rule first
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.RemoveRule(ruleId: rule.Id);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.TaxonRules.Should().BeEmpty();
        taxon.MarkedForRegenerateTaxonProducts.Should().BeFalse(); // Cleared after event
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.RegenerateProducts);
    }

    [Fact]
    public void Taxon_RemoveRule_ShouldReturnNotFound_WhenRuleDoesNotExist()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true);
        taxon.ClearDomainEvents();
        var nonExistentRuleId = Guid.NewGuid();

        // Act
        var result = taxon.RemoveRule(ruleId: nonExistentRuleId); // Try to remove non-existent rule

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Should().Be(expected: TaxonRule.Errors.NotFound(id: nonExistentRuleId)); // Use specific ID for assertion
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_AddImage_ShouldAddImageAndRaiseEvents()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId);
        taxon.ClearDomainEvents();

        var image = CreateValidTaxonImage(taxonId: taxon.Id, type: "default", url: "http://example.com/default.jpg");

        // Act
        var result = taxon.AddImage(image: image);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.TaxonImages.Should().ContainSingle(predicate: img => img.Id == image.Id);
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
    }

    [Fact]
    public void Taxon_AddImage_ShouldReplaceExistingImageOfTypeAndRaiseEvents()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId);
        var initialImage = CreateValidTaxonImage(taxonId: taxon.Id, type: "default", url: "http://example.com/initial.jpg");
        taxon.AddImage(image: initialImage); // Add initial image
        taxon.ClearDomainEvents();

        var newImage = CreateValidTaxonImage(taxonId: taxon.Id, type: "default", url: "http://example.com/new.jpg");

        // Act
        var result = taxon.AddImage(image: newImage);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.TaxonImages.Should().ContainSingle(predicate: img => img.Id == newImage.Id);
        taxon.TaxonImages.Should().NotContain(predicate: img => img.Id == initialImage.Id);
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
    }

    [Fact]
    public void Taxon_RemoveImage_ShouldRemoveImageAndRaiseEvents()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId);
        var image = CreateValidTaxonImage(taxonId: taxon.Id, type: "default", url: "http://example.com/default.jpg");
        taxon.AddImage(image: image); // Add image first
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.RemoveImage(id: image.Id);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.TaxonImages.Should().BeEmpty();
        taxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Updated);
    }

    [Fact]
    public void Taxon_RemoveImage_ShouldReturnNotFound_WhenImageDoesNotExist()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId);
        taxon.ClearDomainEvents();

        var nonExistentImageId = Guid.NewGuid();
        // Act
        var result = taxon.RemoveImage(id: nonExistentImageId); // Try to remove non-existent image

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "TaxonImage.NotFound");
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_UpdateNestedSet_ShouldUpdateLftRgtAndDepth()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId);
        taxon.ClearDomainEvents();

        var newLft = 10;
        var newRgt = 20;
        var newDepth = 5;

        // Act
        var result = taxon.UpdateNestedSet(lft: newLft, rgt: newRgt, depth: newDepth);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Lft.Should().Be(expected: newLft);
        taxon.Rgt.Should().Be(expected: newRgt);
        taxon.Depth.Should().Be(expected: newDepth);
        taxon.DomainEvents.Should().BeEmpty(); // No domain event for setting nested set values directly
    }

    [Fact]
    public void Taxon_RegeneratePermalinkAndPrettyName_ShouldGenerateCorrectRootPermalinkAndPrettyName()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "root-name", presentation: "Root Display Name");
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.RegeneratePermalinkAndPrettyName(parentPermalink: null, parentPrettyName: null);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Permalink.Should().Be(expected: "root-name"); // Slugified name
        taxon.PrettyName.Should().Be(expected: "Root Display Name");
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_RegeneratePermalinkAndPrettyName_ShouldGenerateCorrectChildPermalinkAndPrettyName()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child-name", presentation: "Child Display Name");
        taxon.ClearDomainEvents();

        var parentPermalink = "parent-slug";
        var parentPrettyName = "Parent Display Name";

        // Act
        var result = taxon.RegeneratePermalinkAndPrettyName(parentPermalink: parentPermalink, parentPrettyName: parentPrettyName);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Permalink.Should().Be(expected: "parent-slug/child-name");
        taxon.PrettyName.Should().Be(expected: "Parent Display Name -> Child Display Name");
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_RegeneratePermalinkAndPrettyName_ShouldHandleEmptyNameAndPresentationGracefully()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "", presentation: "   "); // Empty name, whitespace presentation
        taxon.ClearDomainEvents();

        // Act
        var result = taxon.RegeneratePermalinkAndPrettyName(parentPermalink: null, parentPrettyName: null);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Permalink.Should().Be(expected: "unnamed"); // Default slug for empty name
        taxon.PrettyName.Should().Be(expected: ""); // Corrected assertion: Should be empty string after normalization
        taxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_SetChildIndex_ShouldUpdatePositionCorrectly()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, position: 0);
        taxon.ClearDomainEvents();

        var newPosition = 50;

        // Act
        var result = taxon.SetChildIndex(index: newPosition);

        // Assert
        result.IsError.Should().BeFalse();
        taxon.Position.Should().Be(expected: newPosition);
        taxon.DomainEvents.Should().BeEmpty(); // Setting index is a direct property change, no domain event
    }

    [Fact]
    public void Taxon_AddChild_ShouldAddChildToChildrenCollectionAndSetParent()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var parentTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "parent");
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child");
        parentTaxon.ClearDomainEvents();
        childTaxon.ClearDomainEvents();

        // Act
        parentTaxon.AddChild(child: childTaxon);

        // Assert
        parentTaxon.Children.Should().Contain(expected: childTaxon);
        childTaxon.Parent.Should().Be(expected: parentTaxon);
        childTaxon.ParentId.Should().Be(expected: parentTaxon.Id);
        // Moved event is raised by the child, not the parent
        childTaxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Moved);
        var movedEvent = childTaxon.DomainEvents.First() as Events.Moved;
        movedEvent.Should().NotBeNull();
        movedEvent!.OldParentId.Should().BeNull();
        movedEvent.NewParentId.Should().Be(expected: parentTaxon.Id);
    }

    [Fact]
    public void Taxon_AddChild_ShouldDoNothing_WhenChildAlreadyExists()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var parentTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "parent");
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child");
        parentTaxon.AddChild(child: childTaxon); // Add child initially
        parentTaxon.ClearDomainEvents();
        childTaxon.ClearDomainEvents();

        // Act
        parentTaxon.AddChild(child: childTaxon); // Add same child again

        // Assert
        parentTaxon.Children.Should().Contain(expected: childTaxon); // Corrected to Contain
        parentTaxon.Children.Should().HaveCount(expected: 1); // Added count check
        childTaxon.Parent.Should().Be(expected: parentTaxon);
        childTaxon.ParentId.Should().Be(expected: parentTaxon.Id);
        parentTaxon.DomainEvents.Should().BeEmpty();
        childTaxon.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_AddChild_ShouldReparentChild_WhenChildHasDifferentParent()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var oldParentTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "old-parent");
        var newParentTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "new-parent");
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child");

        oldParentTaxon.AddChild(child: childTaxon); // Initially add child to old parent
        oldParentTaxon.ClearDomainEvents();
        childTaxon.ClearDomainEvents();
        newParentTaxon.ClearDomainEvents();

        // Act
        newParentTaxon.AddChild(child: childTaxon); // Re-parent child to new parent

        // Assert
        oldParentTaxon.Children.Should().NotContain(unexpected: childTaxon);
        newParentTaxon.Children.Should().Contain(expected: childTaxon);
        childTaxon.Parent.Should().Be(expected: newParentTaxon);
        childTaxon.ParentId.Should().Be(expected: newParentTaxon.Id);

        childTaxon.DomainEvents.Should().HaveCount(expected: 2); // Corrected assertion: Expect 2 events
        var movedEvents = childTaxon.DomainEvents.OfType<Events.Moved>().ToList();
        movedEvents.Should().HaveCount(expected: 2);
        movedEvents[index: 0].OldParentId.Should().Be(expected: oldParentTaxon.Id);
        movedEvents[index: 0].NewParentId.Should().Be(expected: newParentTaxon.Id); // This event comes from inside AddChild, first condition
        movedEvents[index: 1].OldParentId.Should().BeNull(); // This event comes from inside AddChild, second condition
        movedEvents[index: 1].NewParentId.Should().Be(expected: newParentTaxon.Id);
    }

    [Fact]
    public void Taxon_RemoveChild_ShouldRemoveChildFromChildrenCollectionAndUnsetParent()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var parentTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "parent");
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child", parentId: parentTaxon.Id);
        parentTaxon.Children.Add(item: childTaxon); // Manually add for this test
        parentTaxon.ClearDomainEvents();
        childTaxon.ClearDomainEvents();

        // Act
        parentTaxon.RemoveChild(child: childTaxon);

        // Assert
        parentTaxon.Children.Should().NotContain(unexpected: childTaxon);
        parentTaxon.Children.Should().BeEmpty();
        childTaxon.Parent.Should().BeNull();
        childTaxon.ParentId.Should().BeNull();
        childTaxon.DomainEvents.Should().ContainSingle(predicate: e => e is Events.Moved);
        var movedEvent = childTaxon.DomainEvents.First() as Events.Moved;
        movedEvent.Should().NotBeNull();
        movedEvent!.OldParentId.Should().Be(expected: parentTaxon.Id);
        movedEvent.NewParentId.Should().BeNull();
    }

    [Fact]
    public void Taxon_RemoveChild_ShouldDoNothing_WhenChildIsNotPresent()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var parentTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "parent");
        var nonExistentChild = CreateValidTaxon(taxonomyId: taxonomyId, name: "non-existent-child");
        nonExistentChild.ClearDomainEvents(); // Corrected: Clear events for non-existent child
        parentTaxon.ClearDomainEvents();

        // Act
        parentTaxon.RemoveChild(child: nonExistentChild);

        // Assert
        parentTaxon.Children.Should().BeEmpty();
        parentTaxon.DomainEvents.Should().BeEmpty();
        nonExistentChild.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Taxon_IsRoot_ShouldReturnTrueForRootTaxonAndFalseForChildTaxon()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var rootTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "root", parentId: null);
        var childTaxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "child", parentId: Guid.NewGuid()); // ParentId not null

        // Assert
        rootTaxon.IsRoot.Should().BeTrue();
        childTaxon.IsRoot.Should().BeFalse();
    }

    [Fact]
    public void Taxon_SeoTitle_ShouldReturnMetaTitle_WhenSet()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var metaTitle = "Custom SEO Title";
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: "test", metaTitle: metaTitle);

        // Assert
        taxon.SeoTitle.Should().Be(expected: metaTitle);
    }

    [Fact]
    public void Taxon_SeoTitle_ShouldReturnName_WhenMetaTitleIsEmpty()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var name = "category-name";
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId, name: name, metaTitle: null);

        // Assert
        taxon.SeoTitle.Should().Be(expected: name);

        // Arrange 2
        var taxon2 = CreateValidTaxon(taxonomyId: taxonomyId, name: name, metaTitle: "");

        // Assert 2
        taxon2.SeoTitle.Should().Be(expected: name);

        // Arrange 3
        var taxon3 = CreateValidTaxon(taxonomyId: taxonomyId, name: name, metaTitle: "   ");

        // Assert 3
        taxon3.SeoTitle.Should().Be(expected: name);
    }

    [Fact]
    public void Taxon_Image_ShouldReturnDefaultImage_WhenAvailable()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId);
        var defaultImage = CreateValidTaxonImage(taxonId: taxon.Id, type: "default", url: "http://default.com/img.jpg");
        var otherImage = CreateValidTaxonImage(taxonId: taxon.Id, type: "other", url: "http://other.com/img.jpg");
        taxon.TaxonImages.Add(item: otherImage);
        taxon.TaxonImages.Add(item: defaultImage);

        // Assert
        taxon.Image.Should().Be(expected: defaultImage);
    }

    [Fact]
    public void Taxon_SquareImage_ShouldReturnSquareImage_WhenAvailable()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var taxon = CreateValidTaxon(taxonomyId: taxonomyId);
        var squareImage = CreateValidTaxonImage(taxonId: taxon.Id, type: "square", url: "http://square.com/img.jpg");
        var otherImage = CreateValidTaxonImage(taxonId: taxon.Id, type: "other", url: "http://other.com/img.jpg");
        taxon.TaxonImages.Add(item: otherImage);
        taxon.TaxonImages.Add(item: squareImage);

        // Assert
        taxon.SquareImage.Should().Be(expected: squareImage);
    }

    [Fact]
    public void Taxon_PageBuilderImage_ShouldPreferSquareImageThenDefault()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();

        // Scenario 1: Square and Default exist - should return Square
        var taxon1 = CreateValidTaxon(taxonomyId: taxonomyId);
        var defaultImage1 = CreateValidTaxonImage(taxonId: taxon1.Id, type: "default", url: "http://default1.com");
        var squareImage1 = CreateValidTaxonImage(taxonId: taxon1.Id, type: "square", url: "http://square1.com");
        taxon1.TaxonImages.Add(item: defaultImage1);
        taxon1.TaxonImages.Add(item: squareImage1);
        taxon1.PageBuilderImage.Should().Be(expected: squareImage1);

        // Scenario 2: Only Default exists - should return Default
        var taxon2 = CreateValidTaxon(taxonomyId: taxonomyId);
        var defaultImage2 = CreateValidTaxonImage(taxonId: taxon2.Id, type: "default", url: "http://default2.com");
        taxon2.TaxonImages.Add(item: defaultImage2);
        taxon2.PageBuilderImage.Should().Be(expected: defaultImage2);

        // Scenario 3: Neither exists - should return null
        var taxon3 = CreateValidTaxon(taxonomyId: taxonomyId);
        taxon3.PageBuilderImage.Should().BeNull();
    }

    [Fact]
    public void Taxon_IsManual_ShouldReturnTrueForManualTaxonAndFalseForAutomaticTaxon()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var manualTaxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: false);
        var automaticTaxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true);

        // Assert
        manualTaxon.IsManual.Should().BeTrue();
        automaticTaxon.IsManual.Should().BeFalse();
    }

    [Fact]
    public void Taxon_IsManualSortOrder_ShouldReturnTrueForManualSortOrderAndFalseForAlgorithmicSortOrder()
    {
        // Arrange
        var taxonomyId = Guid.NewGuid();
        var manualSortTaxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true, sortOrder: "manual");
        var algorithmicSortTaxon = CreateValidTaxon(taxonomyId: taxonomyId, automatic: true, sortOrder: "best-selling");

        // Assert
        manualSortTaxon.IsManualSortOrder.Should().BeTrue();
        algorithmicSortTaxon.IsManualSortOrder.Should().BeFalse();
    }
}
