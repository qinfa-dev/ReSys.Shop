# Feature Implementation Plan: unit_test_taxon

## 📋 Todo Checklist
- [x] Review existing `TaxonTests.cs` for testing patterns and coverage.
- [x] Implement remaining unit tests for `Taxon.Create` scenarios.
- [x] Implement remaining unit tests for `Taxon.Update` scenarios.
- [x] Implement remaining unit tests for `Taxon.SetParent` scenarios.
- [x] Implement unit tests for `Taxon.UpdateNestedSet` method.
- [x] Implement remaining unit tests for `Taxon.RegeneratePermalinkAndPrettyName` method.
- [x] Implement unit tests for `Taxon.SetChildIndex` method.
- [x] Implement unit tests for `Taxon.AddChild` method.
- [x] Implement unit tests for `Taxon.RemoveChild` method.
- [x] Implement unit tests for computed properties (`IsRoot`, `SeoTitle`, `Image`, `SquareImage`, `PageBuilderImage`, `IsManual`, `IsManualSortOrder`).
- [x] Ensure all domain events are correctly raised and verified.
- [x] Final Review and Testing

## 🔍 Analysis & Investigation

### Codebase Structure
- `src/ReSys.Core/Domain/Catalog/Taxonomies/Taxa/Taxon.cs`: This is the aggregate root entity for a hierarchical taxonomy structure. It contains core properties, nested set model properties (`Lft`, `Rgt`, `Depth`), automatic taxon configuration, SEO properties, metadata, and relationships to other entities like `Taxonomy`, `TaxonImage`, `Classification`, `TaxonRule`, and `PromotionRuleTaxon`. It utilizes `ErrorOr` for functional error handling and `MediatR` for domain events.
- `src/ReSys.Core/Common/Domain/Entities/Aggregate.cs`: Base class for `Taxon`, providing common aggregate functionalities like domain event management.
- `src/ReSys.Core/Common/Extensions/MetadataExtensions.cs`: Used for comparing metadata dictionaries.
- `src/ReSys.Core/Domain/Catalog/Taxonomies/TaxonRule.cs`, `TaxonImage.cs`: Related entities used by `Taxon`.
- `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`: Existing unit test file for `Taxon`, using `Xunit` and `FluentAssertions`. It provides helper methods for creating test instances of `Taxonomy`, `Taxon`, `TaxonRule`, and `TaxonImage`.

### Current Architecture
The project adheres to a Clean Architecture pattern, with `ReSys.Core` being the Domain/Application layer. `Taxon` is a domain entity that is also an aggregate root, meaning it encapsulates its own state and business logic, and is responsible for maintaining its invariants. It uses functional error handling (`ErrorOr`) and dispatches domain events (`MediatR`) to signal changes to other parts of the system.

### Dependencies & Integration Points
- `ErrorOr`: Used for returning success or error results from methods, promoting railway-oriented programming.
- `MediatR`: Used for dispatching domain events (e.g., `Events.Created`, `Events.Updated`, `Events.Deleted`, `Events.Moved`, `Events.RegenerateProducts`). Unit tests should verify that these events are correctly raised.
- `Taxonomy`, `TaxonRule`, `TaxonImage`: `Taxon` has relationships with these entities. Helper methods in the existing test file facilitate their creation.
- `MetadataExtensions`: Used for comparing `IDictionary<string, object?>` instances.

### Considerations & Challenges
- **Complexity of `Taxon`**: `Taxon.cs` is a large and complex class with many properties and methods covering various concerns (hierarchy, automation, SEO, metadata, images, rules). This requires a thorough and systematic approach to testing.
- **Domain Events**: Ensuring that the correct domain events are raised at the appropriate times, with the correct payload.
- **Nested Set Model**: While the `Lft`, `Rgt`, `Depth` properties are updated by `UpdateNestedSet`, the actual nested set logic (re-indexing the tree) is likely handled at a higher level (e.g., application service or infrastructure layer) after a `Taxon` is saved. The unit tests for `Taxon` should primarily focus on ensuring these properties can be set, and not on the complex tree re-indexing logic itself.
- **`ErrorOr` Return Types**: All methods returning `ErrorOr` should be tested for both success and error paths.
- **Slugification/Normalization**: `Name` and `Presentation` normalization (e.g., `ToSlug()`) and how they affect `Permalink` and `PrettyName` should be tested.
- **Helper Methods**: Leveraging the existing helper methods in `TaxonTests.cs` (`CreateValidTaxonomy`, `CreateValidTaxon`, `CreateValidTaxonRule`, `CreateValidTaxonImage`) to maintain consistency and reduce test setup boilerplate.
- **State Management**: When testing methods that modify the `Taxon` instance, ensure that the state changes are correctly reflected and that `ClearDomainEvents()` is used between `Arrange` and `Act` phases to isolate events for each test.

## 📝 Implementation Plan

### Prerequisites
- The existing `TaxonTests.cs` file is available and contains the helper methods and initial tests.
- .NET 9 SDK is installed.
- Xunit and FluentAssertions are configured in the test project.

### Step-by-Step Implementation

1.  **Review existing `TaxonTests.cs`**:
    - Familiarize with the existing test structure, naming conventions, and assertion style.

2.  **Add `Create` method tests**:
    - **Test Case**: `Taxon_Create_ShouldInitializeAutomaticPropertiesCorrectly_WhenAutomaticIsTrueAndNoSpecificPolicyOrOrderProvided`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Assert `Automatic` is true, `RulesMatchPolicy` defaults to "all", `SortOrder` defaults to "manual".
    - **Test Case**: `Taxon_Create_ShouldUseProvidedAutomaticProperties_WhenAutomaticIsTrue`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Assert `RulesMatchPolicy` and `SortOrder` use provided values when `automatic` is true.
    - **Test Case**: `Taxon_Create_ShouldDefaultPresentationToName_WhenPresentationIsNull`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Assert `Presentation` equals `Name` when `presentation` parameter is null.
    - **Test Case**: `Taxon_Create_ShouldInitializeMetadataDictionaries`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Assert `PublicMetadata` and `PrivateMetadata` are initialized as empty dictionaries if not provided.

3.  **Add `Update` method tests**:
    - **Test Case**: `Taxon_Update_ShouldUpdateHideFromNavMetaPropertiesAndNotRegenerateProducts`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test updating `HideFromNav`, `MetaTitle`, `MetaDescription`, `MetaKeywords` and assert `MarkedForRegenerateTaxonProducts` is false and no `RegenerateProducts` event is raised.
    - **Test Case**: `Taxon_Update_ShouldUpdatePublicMetadata`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test updating `PublicMetadata` and ensure deep equality check.
    - **Test Case**: `Taxon_Update_ShouldUpdatePrivateMetadata`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test updating `PrivateMetadata` and ensure deep equality check.
    - **Test Case**: `Taxon_Update_ShouldResetMarkedForRegenerateTaxonProductsFlag_AfterEventDispatch`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify that `MarkedForRegenerateTaxonProducts` is set to false after `RegenerateProducts` event is dispatched.
    - **Test Case**: `Taxon_Update_ShouldResetAutomaticProperties_WhenAutomaticChangesToFalse`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `automatic = false` resetting `RulesMatchPolicy` to "all" and `SortOrder` to "manual".
    - **Test Case**: `Taxon_Update_ShouldSetUpdatedAtAndRaiseUpdatedEvent_WhenChangesOccur`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Assert `UpdatedAt` is set and `Updated` event is raised when any property changes.
    - **Test Case**: `Taxon_Update_ShouldSetUpdatedEventNameOrPresentationChanged_WhenOnlyNameChanges`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify `NameOrPresentationChanged` in `Updated` event is true.
    - **Test Case**: `Taxon_Update_ShouldSetUpdatedEventNameOrPresentationChanged_WhenOnlyPresentationChanges`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify `NameOrPresentationChanged` in `Updated` event is true.
    - **Test Case**: `Taxon_Update_ShouldSetUpdatedEventNameOrPresentationChanged_WhenBothNameAndPresentationChange`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify `NameOrPresentationChanged` in `Updated` event is true.
    - **Test Case**: `Taxon_Update_ShouldNotSetUpdatedEventNameOrPresentationChanged_WhenNameAndPresentationDoNotChange`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify `NameOrPresentationChanged` in `Updated` event is false.
    - **Test Case**: `Taxon_Update_ShouldHandleParentIdChangeThroughSetParent`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test the flow where `parentId` is updated via the `Update` method, which internally calls `SetParent`.

4.  **Add `SetParent` method tests**:
    - **Test Case**: `Taxon_SetParent_ShouldSetParentToNull_WhenNewParentIdIsNull`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test setting `ParentId` to `null` (making it a root).
    - **Test Case**: `Taxon_SetParent_ShouldUpdatePosition_WhenNewIndexIsProvided`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify `Position` is updated.

5.  **Add `UpdateNestedSet` method tests**:
    - **Test Case**: `Taxon_UpdateNestedSet_ShouldUpdateLftRgtAndDepth`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Assert `Lft`, `Rgt`, `Depth` are correctly updated.

6.  **Add `RegeneratePermalinkAndPrettyName` method tests**:
    - **Test Case**: `Taxon_RegeneratePermalinkAndPrettyName_ShouldGenerateCorrectRootPermalinkAndPrettyName`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test with `null` parent permalink and pretty name.
    - **Test Case**: `Taxon_RegeneratePermalinkAndPrettyName_ShouldGenerateCorrectChildPermalinkAndPrettyName`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test with non-null parent permalink and pretty name, including slugification.
    - **Test Case**: `Taxon_RegeneratePermalinkAndPrettyName_ShouldHandleEmptyNameAndPresentationGracefully`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test how it handles empty strings for `Name` and `Presentation`.

7.  **Add `SetChildIndex` method tests**:
    - **Test Case**: `Taxon_SetChildIndex_ShouldUpdatePositionCorrectly`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Assert `Position` is updated.

8.  **Add `AddChild` method tests**:
    - **Test Case**: `Taxon_AddChild_ShouldAddChildToChildrenCollectionAndSetParent`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify child is added, and its `Parent` and `ParentId` are set.
    - **Test Case**: `Taxon_AddChild_ShouldDoNothing_WhenChildAlreadyExists`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify no change and no new event if child is already present.
    - **Test Case**: `Taxon_AddChild_ShouldReparentChild_WhenChildHasDifferentParent`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test re-parenting, ensure old parent's `Children` collection is updated and `Moved` event is raised for both old and new parent context.

9.  **Add `RemoveChild` method tests**:
    - **Test Case**: `Taxon_RemoveChild_ShouldRemoveChildFromChildrenCollectionAndUnsetParent`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify child is removed, and its `Parent` and `ParentId` are unset.
    - **Test Case**: `Taxon_RemoveChild_ShouldDoNothing_WhenChildIsNotPresent`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Verify no change and no new event if child is not present.

10. **Add Computed Properties tests**:
    - **Test Case**: `Taxon_IsRoot_ShouldReturnTrueForRootTaxonAndFalseForChildTaxon`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `IsRoot` property.
    - **Test Case**: `Taxon_SeoTitle_ShouldReturnMetaTitle_WhenSet`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `SeoTitle` returns `MetaTitle`.
    - **Test Case**: `Taxon_SeoTitle_ShouldReturnName_WhenMetaTitleIsEmpty`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `SeoTitle` falls back to `Name`.
    - **Test Case**: `Taxon_Image_ShouldReturnDefaultImage_WhenAvailable`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `Image` property.
    - **Test Case**: `Taxon_SquareImage_ShouldReturnSquareImage_WhenAvailable`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `SquareImage` property.
    - **Test Case**: `Taxon_PageBuilderImage_ShouldPreferSquareImageThenDefault`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `PageBuilderImage` property for different scenarios (square exists, only default exists, neither exists).
    - **Test Case**: `Taxon_IsManual_ShouldReturnTrueForManualTaxonAndFalseForAutomaticTaxon`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `IsManual` property.
    - **Test Case**: `Taxon_IsManualSortOrder_ShouldReturnTrueForManualSortOrderAndFalseForAlgorithmicSortOrder`
        - **Files to modify**: `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`
        - **Changes needed**: Test `IsManualSortOrder` property.

### Testing Strategy
- **Unit Tests**: All new tests will be unit tests, isolated to the `Taxon` aggregate itself. Dependencies (like `TaxonRule`, `TaxonImage`, `Taxonomy`) will be mocked or created using existing helper methods as "valid" instances to focus on `Taxon`'s logic.
- **Arrange-Act-Assert (AAA) Pattern**: Each test will clearly follow the AAA pattern.
- **Fluent Assertions**: Continue to use `FluentAssertions` for clear and readable assertions.
- **ErrorOr Verification**: For methods returning `ErrorOr`, tests will check both `IsError` and `IsSuccess` paths, and verify the specific `Error` object when applicable.
- **Domain Event Verification**: For methods that raise domain events, tests will assert the presence, type, and content of these events using `taxon.DomainEvents.Should().ContainSingle(...)` and similar patterns. `taxon.ClearDomainEvents()` will be used before each `Act` to ensure only events from the current operation are tested.
- **Edge Cases**: Pay attention to edge cases such as null parameters, empty collections, and boundary conditions (e.g., `automatic` flag influencing defaults).

## 🎯 Success Criteria
- All identified test cases for `Taxon.cs` are implemented in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/TaxonTests.cs`.
- All new tests pass successfully when `dotnet test` is run in the `tests/Core.UnitTests` project.
- The new tests adhere to the existing conventions and style of `TaxonTests.cs`.
- Code coverage for `Taxon.cs` is significantly improved.
- No new warnings or errors are introduced.
