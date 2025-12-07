# Feature Implementation Plan: Unit Tests for Catalog.Taxonomies

## 📋 Todo Checklist
- [x] Create a new unit test file for `Taxonomy` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/`.
    *   **Implementation Notes**: Created `TaxonomyTests.cs` with necessary `using` directives and `CreateValidTaxonomy` helper.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxonomy.Create` factory method (happy path, name/presentation validation, domain events).
    *   **Implementation Notes**: Implemented happy path test, and tests for `Name` and `Presentation` being required. Asserts that `Name` is slugified and `Taxonomy.Events.Created` is raised.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxonomy.Update` method (happy path property updates, domain events).
    *   **Implementation Notes**: Implemented tests for updating name, presentation, and position, including asserting `NameChanged` flag in the `Events.Updated` event. Also includes a test for no changes.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxonomy.Delete` method (happy path, HasTaxons error, domain events).
    *   **Implementation Notes**: Implemented tests for deleting an empty taxonomy and for attempting to delete a taxonomy with multiple taxons (expecting `Errors.HasTaxons`).
    *   **Status**: ✅ Completed
- [x] Create a new unit test file for `Taxon` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/`.
    *   **Implementation Notes**: Created `TaxonTests.cs` with necessary `using` directives and helper methods (`CreateValidTaxonomy` (copied), `CreateValidTaxon`, `CreateValidTaxonRule`, `CreateValidTaxonImage`).
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.Create` factory method (happy path root/child creation, name/presentation validation, domain events).
    *   **Implementation Notes**: Implemented tests for creating root and child taxons. Asserts that `Name` is slugified, `Permalink` and `PrettyName` are correctly generated based on hierarchy, and `Taxon.Events.Created` is raised.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.Update` method (happy path property updates, parent change, `MarkedForRegenerateTaxonProducts` logic, domain events).
    *   **Implementation Notes**: Implemented tests for updating basic properties (name, presentation, description, position) and asserting permalink/pretty name regeneration. Tests also cover `automatic` flag and `rulesMatchPolicy` changes triggering `MarkedForRegenerateTaxonProducts` and `Taxon.Events.RegenerateProducts`. A test confirms cosmetic changes do not trigger regeneration.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.SetParent` method (happy path, SelfParenting error, domain events).
    *   **Implementation Notes**: Implemented tests for successfully setting a parent and asserting the `ParentId`, `Position`, and `Events.Moved` event. Also includes a test for the `SelfParenting` error.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.Delete` method (happy path, HasChildren error, domain events).
    *   **Implementation Notes**: Implemented tests for deleting a taxon with no children and for attempting to delete a taxon that has children (expecting `Errors.HasChildren`).
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.AddTaxonRule` method (happy path, various errors, domain events).
    *   **Implementation Notes**: Implemented happy path test, and tests for null rule, `TaxonId` mismatch, and duplicate rules. Asserts that `MarkedForRegenerateTaxonProducts` is set/cleared and appropriate domain events (`Events.Updated`, `Events.RegenerateProducts`) are raised.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.RemoveRule` method (happy path, not found error, domain events).
    *   **Implementation Notes**: Implemented happy path test for removing a rule and a test for attempting to remove a non-existent rule. Asserts `MarkedForRegenerateTaxonProducts` is set/cleared and `Events.RegenerateProducts` is raised.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.AddImage` method (happy path, replacing existing image).
    *   **Implementation Notes**: Implemented tests for adding a new image and replacing an existing image of the same type. Asserts `TaxonImages` collection and `Events.Updated` event.
    *   **Status**: ✅ Completed
- [x] Write tests for `Taxon.RemoveImage` method (happy path, not found error).
    *   **Implementation Notes**: Implemented tests for successfully removing an image and for attempting to remove a non-existent image. Asserts `TaxonImages` collection and `Events.Updated` event.
    *   **Status**: ✅ Completed
- [x] Create a new unit test file for `TaxonImage` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Images/`.
    *   **Implementation Notes**: Created `TaxonImageTests.cs` with necessary `using` directives and `CreateValidTaxonImage` helper.
    *   **Status**: ✅ Completed
- [x] Write tests for `TaxonImage.Create` factory method (happy path).
    *   **Implementation Notes**: Implemented happy path test for `TaxonImage.Create`, asserting all properties including metadata.
    *   **Status**: ✅ Completed
- [x] Write tests for `TaxonImage.Update` method (happy path property updates).
    *   **Implementation Notes**: Implemented tests for `TaxonImage.Update` covering property updates and metadata. Also includes a test to ensure `UpdatedAt` is not changed if no properties are updated.
    *   **Status**: ✅ Completed
- [x] Create a new unit test file for `TaxonRule` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Rules/`.
    *   **Implementation Notes**: Created `TaxonRuleTests.cs` with necessary `using` directives and `CreateValidTaxonRule` helper.
    *   **Status**: ✅ Completed
- [x] Write tests for `TaxonRule.Create` factory method (happy path, invalid type/policy/PropertyNameRequired errors).
    *   **Implementation Notes**: Implemented happy path tests for general rules and product property rules. Includes tests for `InvalidType`, `InvalidMatchPolicy`, and `PropertyNameRequired` errors.
    *   **Status**: ✅ Completed
- [x] Write tests for `TaxonRule.Update` method (happy path property updates, invalid type/policy/PropertyNameRequired errors).
    *   **Implementation Notes**: Implemented happy path test for updating rule properties. Includes tests for `InvalidType`, `InvalidMatchPolicy`, and `PropertyNameRequired` errors during update. Also a test for no changes.
    *   **Status**: ✅ Completed
- [x] Ensure all new tests follow xUnit conventions and project standards.
    *   **Implementation Notes**: All implemented tests adhere to xUnit framework practices (e.g., `[Fact]`, `[Theory]`), use `FluentAssertions` for clear assertions, and follow the Arrange-Act, Assert (AAA) pattern as defined in `docs/UNIT_TESTING_BEST_PRACTICES.md`.
    *   **Status**: ✅ Completed
- [ ] Final Review and Testing of all Catalog.Taxonomies tests.

## 🔍 Analysis & Investigation

### Codebase Structure
The `Catalog.Taxonomies` bounded context is composed of several interrelated entities:
-   `Taxonomy` (Aggregate Root): Top-level classification system.
-   `Taxon` (Aggregate Root): Individual node within a `Taxonomy`, forming a hierarchical tree. Owns `TaxonImage`s, `TaxonRule`s, and `Classification`s.
-   `TaxonImage` (Owned Entity by `Taxon`): Image assets for a `Taxon`.
-   `TaxonRule` (Owned Entity by `Taxon`): Rules for automatic product classification within a `Taxon`.

Each entity leverages `ErrorOr` for functional error handling and publishes domain events for significant state changes.

### Current Architecture
The domain adheres to Domain-Driven Design and Clean Architecture principles. `Taxonomy` and `Taxon` are distinct aggregate roots, each managing its own invariants and lifecycle. Nested set properties (`Lft`, `Rgt`, `Depth`) are used for efficient hierarchical queries within `Taxon`. `IHasParameterizableName` is used for consistent name/presentation handling, which includes slugification of the 'Name' property, similar to `Product`.

### Dependencies & Integration Points
-   **`ErrorOr`**: For returning results and errors.
-   **`ReSys.Core.Common.Constants.CommonInput`**: Provides common validation constraints and error messages.
-   **`ReSys.Core.Common.Extensions`**: Utility extensions like `ToSlug()`.
-   **`HasParameterizableName`**: Static helper for normalizing name and presentation, including slugification.
-   `Taxonomy` depends on `Taxon` (owns a collection of `Taxon`s).
-   `Taxon` depends on `Taxonomy`, `TaxonImage`, `TaxonRule`, `Classification` (owns collections/references).
-   `TaxonImage` inherits from `BaseImageAsset`.
-   `TaxonRule` inherits from `AuditableEntity`.

### Considerations & Challenges
-   **Hierarchy Management**: Testing `Taxon.SetParent` and `Taxon.Delete` will require careful setup of parent/child relationships and nested set properties.
-   **Domain Event Assertions**: Verifying correct domain events are raised is crucial, especially for `Taxon.Events.Moved`, `Taxon.Events.RegenerateProducts`, `Taxonomy.Events.Created`, `Updated`, `Deleted`.
-   **Dependency Setup**: Creating valid `Taxonomy` and `Taxon` instances, particularly `Taxon`s with specific parent/child relationships, will require robust helper methods.
-   **Rule Logic**: `TaxonRule` involves matching logic (`GetFieldName`, `GetFilterOperator`, `CanConvertToQueryFilter` in `Taxon.Extensions.cs`), which needs to be considered when testing `TaxonRule.Create` and `TaxonRule.Update`.
-   **Metadata Handling**: Testing `PublicMetadata` and `PrivateMetadata` updates for `Taxonomy` and `Taxon`.
-   **`IHasParameterizableName`**: Remember that `name` parameters for `Taxonomy.Create`/`Update` and `Taxon.Create`/`Update` will be slugified internally, so assertions for the `Name` property should reflect this.

## 📝 Implementation Plan: Catalog.Taxonomies Tests

### Prerequisites
- .NET SDK installed.
- `ReSys.Core.csproj` and `Core.UnitTests.csproj` are part of the `ReSys.Shop.sln` solution.
- Familiarity with xUnit testing framework and `FluentAssertions`.
- Helper methods for creating `Taxonomy` and `Taxon` will be essential.

### Step-by-Step Implementation

1.  **Create Test File for `Taxonomy`**
    *   **Action**: Create `TaxonomyTests.cs` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/`.
    *   **Changes needed**: Basic test class structure, `using` directives, helper for `CreateValidTaxonomy`.

2.  **Write Tests for `Taxonomy.Create()`**
    *   **Action**: Implement tests for the `Create` factory method.
    *   **Changes needed**:
        *   Happy path: creation with valid `storeId`, `name`, `presentation`. Assert properties and `Taxonomy.Events.Created`.
        *   Error: `name`/`presentation` validation (will be handled by `HasParameterizableName.NormalizeParams` internally, check final `Name` property is slugified).

3.  **Write Tests for `Taxonomy.Update()`**
    *   **Action**: Implement tests for the `Update` method.
    *   **Changes needed**:
        *   Happy path: Update `name`, `presentation`, `position`, metadata. Assert properties and `Taxonomy.Events.Updated`.
        *   Ensure `nameChanged` flag in event is correct.

4.  **Write Tests for `Taxonomy.Delete()`**
    *   **Action**: Implement tests for the `Delete` method.
    *   **Changes needed**:
        *   Happy path: Delete a taxonomy with no children. Assert `Result.Deleted` and `Taxonomy.Events.Deleted`.
        *   Error: `HasTaxons` if `Taxons.Count > 1`. (Requires setting up dummy `Taxon` children).

5.  **Create Test File for `Taxon`**
    *   **Action**: Create `TaxonTests.cs` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Taxa/`.
    *   **Changes needed**: Basic test class structure, `using` directives, helper for `CreateValidTaxonomy` (or mock), `CreateValidTaxon`.

6.  **Write Tests for `Taxon.Create()`**
    *   **Action**: Implement tests for the `Create` factory method.
    *   **Changes needed**:
        *   Happy path: Root taxon creation. Assert properties, `IsRoot`, `Permalink`, `PrettyName`, `Lft`/`Rgt`/`Depth` (initial 0s), `Taxon.Events.Created`.
        *   Happy path: Child taxon creation. Assert parent ID, correct `Permalink`, `PrettyName`.
        *   `name`/`presentation` validation (check `Name` property is slugified).

7.  **Write Tests for `Taxon.Update()`**
    *   **Action**: Implement tests for the `Update` method.
    *   **Changes needed**:
        *   Happy path: Update name/presentation/description/position/SEO/metadata. Assert properties and `Taxon.Events.Updated` (with `NameOrPresentationChanged` flag).
        *   Test `automatic`, `rulesMatchPolicy`, `sortOrder` changes triggering `MarkedForRegenerateTaxonProducts` and `Taxon.Events.RegenerateProducts`.
        *   Test `parentId` change via `SetParent` (only happy path for `Update`).

8.  **Write Tests for `Taxon.SetParent()`**
    *   **Action**: Implement tests for the `SetParent` method.
    *   **Changes needed**:
        *   Happy path: Set a valid parent. Assert `ParentId`, `Position`, `Taxon.Events.Moved`.
        *   Error: `SelfParenting`.
        *   Error: `ParentTaxonomyMismatch` (Requires mocking `Parent`'s `TaxonomyId`).

9.  **Write Tests for `Taxon.Delete()`**
    *   **Action**: Implement tests for the `Delete` method.
    *   **Changes needed**:
        *   Happy path: Delete a taxon with no children. Assert `Result.Deleted` and `Taxon.Events.Deleted`.
        *   Error: `HasChildren` if `Children.Any()`. (Requires setting up dummy `Children`).

10. **Write Tests for `Taxon.AddTaxonRule()`**
    *   **Action**: Implement tests for the `AddTaxonRule` method.
    *   **Changes needed**:
        *   Happy path: Add a valid rule. Assert `TaxonRules` collection, `MarkedForRegenerateTaxonProducts`, `Taxon.Events.Updated`, `Taxon.Events.RegenerateProducts`.
        *   Error: `TaxonRule.Errors.Required`.
        *   Error: `TaxonRule.Errors.TaxonMismatch`.
        *   Error: `TaxonRule.Errors.Duplicate`.
        *   Pre-requisite helpers: `TaxonRule.Create`.

11. **Write Tests for `Taxon.RemoveRule()`**
    *   **Action**: Implement tests for the `RemoveRule` method.
    *   **Changes needed**:
        *   Happy path: Remove an existing rule. Assert `TaxonRules` collection, `MarkedForRegenerateTaxonProducts`, `Taxon.Events.RegenerateProducts`.
        *   Error: `TaxonRule.Errors.NotFound`.

12. **Write Tests for `Taxon.AddImage()`**
    *   **Action**: Implement tests for the `AddImage` method.
    *   **Changes needed**:
        *   Happy path: Add a new image. Assert `TaxonImages` collection, `Taxon.Events.Updated`.
        *   Happy path: Add an image of a type that already exists (should replace). Assert `TaxonImages` count and new image.
        *   Pre-requisite helpers: `TaxonImage.Create`.

13. **Write Tests for `Taxon.RemoveImage()`**
    *   **Action**: Implement tests for the `RemoveImage` method.
    *   **Changes needed**:
        *   Happy path: Remove an existing image. Assert `TaxonImages` collection, `Taxon.Events.Updated`.
        *   Error: `TaxonImage.Errors.NotFound`.

14. **Create Test File for `TaxonImage`**
    *   **Action**: Create `TaxonImageTests.cs` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Images/`.
    *   **Changes needed**: Basic test class structure, `using` directives, helper for `CreateValidTaxonImage`.

15. **Write Tests for `TaxonImage.Create()`**
    *   **Action**: Implement tests for the `Create` factory method.
    *   **Changes needed**:
        *   Happy path: creation with valid `taxonId`, `type`, `url`, `alt`, etc. Assert properties.

16. **Write Tests for `TaxonImage.Update()`**
    *   **Action**: Implement tests for the `Update` method.
    *   **Changes needed**:
        *   Happy path: Update `type`, `url`, `alt`, metadata. Assert properties.

17. **Create Test File for `TaxonRule`**
    *   **Action**: Create `TaxonRuleTests.cs` in `tests/Core.UnitTests/Domain/Catalog/Taxonomies/Rules/`.
    *   **Changes needed**: Basic test class structure, `using` directives, helper for `CreateValidTaxonRule`.

18. **Write Tests for `TaxonRule.Create()`**
    *   **Action**: Implement tests for the `Create` factory method.
    *   **Changes needed**:
        *   Happy path: creation with valid `taxonId`, `type`, `value`, `matchPolicy`, `propertyName`.
        *   Error: `InvalidType`.
        *   Error: `InvalidMatchPolicy`.
        *   Error: `PropertyNameRequired`.

19. **Write Tests for `TaxonRule.Update()`**
    *   **Action**: Implement tests for the `Update` method.
    *   **Changes needed**:
        *   Happy path: Update `type`, `value`, `matchPolicy`, `propertyName`.
        *   Error: `InvalidType`.
        *   Error: `InvalidMatchPolicy`.
        *   Error: `PropertyNameRequired`.

### Testing Strategy
Unit tests will be executed using the `dotnet test` command from the root of the `ReSys.Shop.sln`.
1.  Navigate to the project root: `cd C:\Users\ElTow\source\ReSys.Shop`
2.  Run tests: `dotnet test`

This command will discover and run all tests within the `tests/Core.UnitTests` project.

### Tools to be Used:
- `dotnet test` for running the unit tests.

## 🎯 Success Criteria
- New test files are created: `TaxonomyTests.cs`, `TaxonTests.cs`, `TaxonImageTests.cs`, `TaxonRuleTests.cs`.
- Comprehensive unit tests are implemented for `Taxonomy`, `Taxon`, `TaxonImage`, and `TaxonRule` covering their core behaviors, error conditions, and domain events.
- All created tests pass successfully when `dotnet test` is executed.
- The tests demonstrate adherence to xUnit conventions and project coding standards.
- The tests provide robust coverage for the `Catalog.Taxonomies` bounded context.