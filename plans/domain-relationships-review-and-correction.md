# Feature Implementation Plan: Refactor Order & Inventory Relationships

## 📋 Todo Checklist
- [ ] Remove the unused `Fulfillment` bounded context.
- [ ] Remove the `StockLocationId` relationship from the `InventoryUnit` entity.
- [ ] Add a non-nullable `StockLocationId` relationship to the `Shipment` entity.
- [ ] Create and apply a new database migration for the schema changes.
- [ ] Update and run tests to verify the new model.

## 🔍 Analysis & Investigation

### Problem Summary
The user has identified that the relationship between `Inventories` and `Orders` is "a mess" and has requested that the domain model be corrected to align with Spree Commerce patterns, explicitly stating that the unused `Fulfillment` context should be removed.

### Codebase Analysis
1.  **`Fulfillment` Context**: The `src/ReSys.Core/Domain/Fulfillment` directory and its contents (`FulfillmentOrder`, `FulfillmentLineItem`) represent an incomplete and inactive feature. The EF Core configuration for it (`FulfillmentOrderConfiguration.cs`) is entirely commented out, confirming it's not in the database schema. Its presence is a source of confusion and technical debt.

2.  **`InventoryUnit` Entity**: The investigation of `src/ReSys.Core/Domain/Orders/Shipments/InventoryUnitConfiguration.cs` revealed that `InventoryUnit` has a **nullable `StockLocationId` foreign key**. This is the primary source of the "mess". In a standard fulfillment model, all items within a single shipment originate from the same location. Attaching the location to each individual unit instead of the parent `Shipment` is denormalized and incorrect.

3.  **Spree Commerce Model**: The provided Spree code shows that a `Shipment` is the entity that contains `InventoryUnits`. The `Shipment` itself is what's tied to a `StockLocation`. `InventoryUnits` implicitly belong to that location by virtue of being in the shipment. This is a much cleaner, normalized, and more effective design.

### Proposed Solution
The solution is to refactor the `ReSys.Shop` domain model to match the Spree Commerce pattern. This involves moving the responsibility for location from the `InventoryUnit` to the `Shipment`.

-   **Remove `Fulfillment`**: This is a straightforward deletion of dead code.
-   **Refactor `Shipment` and `InventoryUnit`**: The `StockLocationId` will be removed from `InventoryUnit` and added as a required foreign key to `Shipment`.

This change will make the model more logical, easier to understand, and correctly aligned with industry-standard e-commerce architecture.

## 📝 Implementation Plan

### Step-by-Step Implementation

1.  **Step 1: Delete the `Fulfillment` Bounded Context**
    -   **Action**: Delete the entire `src/ReSys.Core/Domain/Fulfillment` directory.
    -   **Reasoning**: Removes the unused and incomplete feature, simplifying the domain model immediately.

2.  **Step 2: Remove `StockLocationId` from `InventoryUnit`**
    -   **Files to modify**:
        -   `src/ReSys.Core/Domain/Orders/Shipments/InventoryUnit.cs`
        -   `src/ReSys.Core/Domain/Orders/Shipments/InventoryUnitConfiguration.cs`
    -   **Changes in `InventoryUnit.cs`**:
        -   Delete the `public Guid? StockLocationId { get; private set; }` property.
        -   Delete the `public StockLocation? StockLocation { get; private set; }` navigation property.
    -   **Changes in `InventoryUnitConfiguration.cs`**:
        -   Delete the property configuration for `StockLocationId`.
        -   Delete the `HasOne(e => e.StockLocation)` relationship configuration.
        -   Delete the index for `StockLocationId`.
    -   **Reasoning**: This removes the incorrect relationship from the child entity.

3.  **Step 3: Add `StockLocationId` to `Shipment`**
    -   **Files to modify**:
        -   `src/ReSys.Core/Domain/Orders/Shipments/Shipment.cs`
        -   `src/ReSys.Core/Domain/Orders/Shipments/ShipmentConfiguration.cs`
    -   **Changes in `Shipment.cs`**:
        -   Add a `public Guid StockLocationId { get; private set; }` property.
        -   Add a `public StockLocation StockLocation { get; private set; }` navigation property.
        -   Update the `Create` factory method to accept `stockLocationId` as a required parameter.
    -   **Changes in `ShipmentConfiguration.cs`**:
        -   Add property configuration for the new `StockLocationId` property, making it required.
        -   Add a `HasOne(s => s.StockLocation).WithMany().HasForeignKey(s => s.StockLocationId)` relationship. Use `.OnDelete(DeleteBehavior.Restrict)` to prevent a `StockLocation` from being deleted if it has associated shipments.
        -   Add an index on the new `StockLocationId` foreign key.
    -   **Reasoning**: This correctly places the location relationship on the parent `Shipment` entity, aligning the model with Spree and logical fulfillment practices.

4.  **Step 4: Create a New Database Migration**
    -   **Action**: From the project root, run the `dotnet ef migrations` command to generate a new migration. This migration will drop the foreign key from `inventory_units` and add a new, non-nullable foreign key to the `shipments` table.
      ```bash
      dotnet ef migrations add Refactor_ShipmentStockLocationRelationship --project src/ReSys.Infrastructure/ReSys.Infrastructure.csproj --startup-project src/ReSys.API/ReSys.API.csproj
      ```
    -   **Note**: Because we are adding a non-nullable column to an existing table (`shipments`), the migration may fail if there is existing data. It might need to be modified to provide a default value for existing rows or to first add the column as nullable, update the data, and then make it non-nullable.

5.  **Step 5: Apply the Database Migration**
    -   **Action**: Run the database update command after verifying the migration script.
      ```bash
      dotnet ef database update --project src/ReSys.Infrastructure/ReSys.Infrastructure.csproj --startup-project src/ReSys.API/ReSys.API.csproj
      ```

### Testing Strategy
-   **Unit Tests**: Update unit tests for `Shipment.Create` to pass the new `stockLocationId`. Any tests that relied on `InventoryUnit.StockLocationId` will need to be refactored to check `shipment.StockLocationId`.
-   **Integration Tests**: Modify integration tests for order processing and shipment creation. The logic that creates `Shipment` instances must now provide a valid `StockLocationId`. Create a new test to verify that an attempt to delete a `StockLocation` with active shipments fails, as per the `OnDelete.Restrict` behavior.

## 🎯 Success Criteria
-   The `Fulfillment` context is completely removed from the codebase.
-   The `StockLocationId` is removed from the `InventoryUnit` entity and database table.
-   The `Shipment` entity contains a required `StockLocationId` foreign key.
-   The application builds successfully, and a clean database migration is generated and applied.
-   All relevant unit and integration tests are updated and pass, confirming the new, cleaner data model works as expected.
