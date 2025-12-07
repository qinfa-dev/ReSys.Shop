# Feature Implementation Plan: Review and Correct Domain Configurations

## 📋 Todo Checklist
- [ ] Standardize Primary Key column naming to `id` across all entities.
- [ ] Apply `HasQueryFilter` for soft deletion on all `ISoftDeletable` entities.
- [ ] Review and enforce consistent relationship configurations (explicitness, delete behaviors).
- [ ] Correct the `StockItem` to `InventoryUnit` relationship.
- [ ] Ensure consistent application of configuration extension methods (e.g., `ConfigureAuditable`).
- [ ] Analyze and optimize database indexes for redundancy and performance.
- [ ] Generate a new EF Core migration to validate all changes.
- [ ] Final Review and Testing.

## 🔍 Analysis & Investigation

### Codebase Structure
The investigation revealed 57 Entity Framework Core configuration files located under `src/ReSys.Core/Domain/`. These files inherit from `IEntityTypeConfiguration<T>` and are responsible for mapping domain entities to the database schema. The structure is consistent, using regions to separate configuration aspects like keys, properties, and relationships. There is heavy reliance on custom extension methods (e.g., `ConfigureAuditable`, `ConfigureName`) to standardize common property settings.

### Inspected Files
- `src/ReSys.Core/Domain/Catalog/Products/ProductConfiguration.cs`
- `src/ReSys.Core/Domain/Orders/OrderConfiguration.cs`
- `src/ReSys.Core/Domain/Inventories/Stocks/StockItemConfiguration.cs`
- `src/ReSys.Core/Domain/Identity/Users/UserConfiguration.cs`
- `src/ReSys.Core/Domain/Stores/StoreConfiguration.cs`
- A full list of 57 files was obtained via `glob`.

### Current Architecture & Key Findings
The architecture uses EF Core with a Code-First approach. The analysis of the sample files uncovered several systemic inconsistencies:

1.  **Inconsistent Primary Key Naming:** Some configurations explicitly name the primary key column as `id` (e.g., `ProductConfiguration`), while others (e.g., `OrderConfiguration`, `UserConfiguration`) do not, letting it default to `Id`. The project convention appears to favor lowercase names for columns.
2.  **Missing Soft Delete Filters:** Entities that implement `ISoftDeletable` (like `Product`) have the necessary properties (`IsDeleted`, `DeletedAt`), but their configurations are missing the `HasQueryFilter(e => !e.IsDeleted)`. This is a critical omission that leads to soft-deleted data being returned in queries. `StoreConfiguration` is a notable exception where the filter is correctly applied.
3.  **Relationship Misconfiguration:** A potentially critical bug was found in `StockItemConfiguration.cs`. The relationship `BackorderedInventoryUnits` appears to be misconfigured with a foreign key pointing to `StockLocationId` instead of `StockItemId`. This would lead to incorrect data associations.
4.  **Inconsistent Relationship Explicitness:** The requirement of relationships is not always explicitly stated. For example, some `HasMany` relationships lack an `IsRequired()` call, which can lead to ambiguity.
5.  **Inconsistent Extension Method Usage:** The application of custom configuration extensions (e.g., `ConfigureUniqueName` vs. `ConfigureParameterizableName`) varies, suggesting a need for a systematic review to ensure each entity's configuration matches the interfaces it implements.
6.  **Potentially Redundant Indexes:** Some configurations define indexes on single columns that are also the leading column in a composite index (e.g., `StockItemConfiguration`). This can be redundant and should be reviewed for optimization.

## 📝 Implementation Plan

### Prerequisites
- A development environment with the .NET 9 SDK installed.
- Access to the project codebase.

### Step-by-Step Implementation

1.  **Step 1: Standardize Primary Key Column Naming**
    - **Action:** Iterate through all 57 entity configuration files found by the `glob` command. For each file, ensure the primary key configuration includes `.HasColumnName("id")`.
    - **Files to modify:** All `*Configuration.cs` files in `src/ReSys.Core/Domain/`.
    - **Example Change (in `OrderConfiguration.cs`):**
      ```csharp
      // From
      builder.Property(propertyExpression: o => o.Id)
          .ValueGeneratedNever()
          .HasComment(comment: "Id: Unique identifier for the order. Value generated never.");

      // To
      builder.Property(propertyExpression: o => o.Id)
          .HasColumnName("id") // <-- Add this
          .ValueGeneratedNever()
          .HasComment(comment: "Id: Unique identifier for the order. Value generated never.");
      ```

2.  **Step 2: Apply Soft Delete Query Filters**
    - **Action:** Identify all domain entities that implement the `ISoftDeletable` interface. In their corresponding configuration files, add a global query filter to automatically exclude soft-deleted records.
    - **Files to modify:** All configuration files for entities implementing `ISoftDeletable` (e.g., `ProductConfiguration.cs`).
    - **Example Change (in `ProductConfiguration.cs`):**
      ```csharp
      // Add this line within the Configure method, typically after properties are set.
      builder.HasQueryFilter(p => !p.IsDeleted);
      ```

3.  **Step 3: Correct `StockItem` Relationship**
    - **Action:** In `StockItemConfiguration.cs`, correct the foreign key for the `BackorderedInventoryUnits` relationship. It should point to `StockItemId`.
    - **Files to modify:** `src/ReSys.Core/Domain/Inventories/Stocks/StockItemConfiguration.cs`
    - **Example Change:**
      ```csharp
      // From
      builder.HasMany(navigationExpression: si => si.BackorderedInventoryUnits)
          .WithOne()
          .HasForeignKey(e => e.StockLocationId) // <-- Incorrect
          .IsRequired(false)
          .OnDelete(deleteBehavior: DeleteBehavior.NoAction);

      // To
      builder.HasMany(navigationExpression: si => si.BackorderedInventoryUnits)
          .WithOne()
          .HasForeignKey(e => e.StockItemId) // <-- Corrected
          .IsRequired(false)
          .OnDelete(deleteBehavior: DeleteBehavior.NoAction);
      ```

4.  **Step 4: Full Review of All Configurations**
    - **Action:** Perform a comprehensive review of all 57 configuration files, paying special attention to the following:
        - **Relationships:** Ensure `IsRequired()` is explicitly set. Verify delete behaviors (`Cascade`, `Restrict`, `SetNull`) are appropriate for each relationship.
        - **Extension Methods:** Check that entities implementing interfaces like `IHasAuditable`, `IHasMetadata`, `IHasUniqueName` have the corresponding `Configure...()` methods called in their configuration.
        - **Indexes:** Remove redundant indexes. For example, if a composite index `(A, B)` exists, the individual index on `A` is often not needed.
    - **Files to modify:** Potentially all `*Configuration.cs` files. This step requires careful, systematic review of each file against the domain entity's definition.

### Testing Strategy
1.  **EF Core Migration:** After applying all the corrections, the primary validation step is to generate a new EF Core migration.
    - Run the command: `dotnet ef migrations add StandardizeDomainConfigurations --project src/ReSys.Infrastructure --startup-project src/ReSys.API`
    - The command should execute without any errors. This proves that the EF Core model is valid and can be translated into a database schema.
2.  **Migration Review:** Carefully inspect the generated migration file. It should reflect the intended changes:
    - Renaming of `Id` columns to `id`.
    - Corrections to foreign key constraints.
    - Addition or removal of indexes.
    - No changes related to query filters should appear, as they are a model-level concern, not a schema change.
3.  **Regression Testing:** Run all existing unit and integration tests to ensure that the changes have not broken any existing functionality.

## 🎯 Success Criteria
- All 57 domain configuration files are internally consistent and adhere to the defined project conventions.
- An EF Core migration can be successfully generated without errors, validating the correctness of the entity mappings.
- Existing tests pass, indicating no regressions were introduced.
- A manual review of the generated migration script confirms that the schema changes align with the corrections made.
