# Feature Implementation Plan: Domain Model Configuration

## 📋 Todo Checklist
- [ ] Create `Configuration` bounded context and domain entity.
- [ ] Implement EF Core entity type configuration for `Configuration`.
- [ ] Add `DbSet` to `ApplicationDbContext` and create a database migration.
- [ ] Implement CQRS commands and queries for managing configurations.
- [ ] Create a database seeder for default configuration values.
- [ ] Implement API endpoints for configuration management.
- [ ] Add Unit Tests for the new domain entity and CQRS handlers.

## 🔍 Analysis & Investigation

### Codebase Structure
- The project follows a clean architecture with `ReSys.Core` (Domain/Application), `ReSys.Infrastructure`, and `ReSys.API` (Presentation).
- The domain is organized into Bounded Contexts (e.g., `Catalog`, `Orders`). The new configuration logic does not fit into any existing context and will be placed in its own `Configuration` bounded context for clarity and separation of concerns.
- Domain entities, like `Product.cs`, are designed as aggregate roots using DDD patterns. They include factory methods (`Create`), private constructors, nested `Errors` and `Constraints` classes, and use the `ErrorOr` library for functional error handling.

### Current Architecture
- The system uses a vertical slice architecture.
- **Domain:** DDD patterns are prevalent. Aggregates inherit from a base `Aggregate` class which provides `Id` and domain event capabilities.
- **Persistence:** Entity Framework Core is used for data access. Entity configurations are managed via `IEntityTypeConfiguration<T>` implementations, which are discovered and applied from the `ReSys.Core` assembly. The main context is `ApplicationDbContext` in `ReSys.Infrastructure`.
- **Application Logic:** CQRS is implemented using `MediatR`. Each feature typically consists of a Command/Query, a Handler, and a FluentValidation `Validator`.
- **API:** The API layer (`ReSys.API`) uses `Carter` to define minimal API endpoints, which delegate work to the `MediatR` pipeline.

### Dependencies & Integration Points
- **MediatR:** For CQRS pattern implementation.
- **FluentValidation:** For validating commands.
- **ErrorOr:** For consistent, functional error handling.
- **Entity Framework Core:** For persistence. New entity requires a new `DbSet` in `ApplicationDbContext` and an `IEntityTypeConfiguration`.
- **Carter:** For exposing the new functionality via RESTful endpoints.

### Considerations & Challenges
- The configuration values have different data types (boolean, integer, string). The plan proposes storing all values as strings in the database and adding a `ValueType` property to the entity. The application layer (CQRS handlers) will be responsible for parsing/casting the string value to the correct type. This provides flexibility at the cost of requiring careful handling in the application logic.
- A seeder is necessary to populate the initial 17 configuration values provided in the prompt. This seeder must be called from the existing `Seeder.Orchestrator`.

## 📝 Implementation Plan

### Prerequisites
- No new external packages are required. All necessary tools (EF Core, MediatR, Carter) are already in use.

### Step-by-Step Implementation

1.  **Create the Configuration Domain Model**
    - **Files to create:**
      - `src/ReSys.Core/Domain/Configurations/Configuration.cs`
      - `src/ReSys.Core/Domain/Configurations/ValueType.cs`
    - **Changes needed in `Configuration.cs`:**
      - Define the `Configuration` class as a public sealed class inheriting from `Aggregate`.
      - Add properties: `Key` (string), `Value` (string), `Description` (string), `DefaultValue` (string), `ValueType` (enum).
      - Implement a static `Create` factory method that returns `ErrorOr<Configuration>` and performs validation (e.g., key is required, value matches type).
      - Add a private constructor for EF Core.
      - Add an `Update` method for changing the `Value`.
      - Define nested static classes for `Constraints` and `Errors` following the pattern in `Product.cs`.
    - **Changes needed in `ValueType.cs`:**
      - Create a public enum `ConfigurationValueType` with values `String`, `Boolean`, `Integer`.

2.  **Implement EF Core Persistence**
    - **File to create:** `src/ReSys.Core/Domain/Configurations/ConfigurationConfiguration.cs`
    - **Changes needed:**
      - Implement `IEntityTypeConfiguration<Configuration>`.
      - Configure the table name to `Configurations`.
      - Set `Id` as the primary key.
      - Create a unique index on the `Key` property.
      - Set max length for string properties (`Key`, `Value`, `Description`).
    - **File to modify:** `src/ReSys.Infrastructure/Persistence/Persistence.DbContext.cs`
    - **Changes needed:**
      - Add a new `DbSet` property: `public DbSet<Configuration> Configurations { get; set; }`

3.  **Create Database Migration**
    - **Action:**
      - From the root directory, run the following shell command to create a new EF Core migration. (This step is for the implementer, not to be run by the planner).
      ```bash
      dotnet ef migrations add AddConfigurationEntity --project src/ReSys.Infrastructure --startup-project src/ReSys.API
      ```

4.  **Implement CQRS Features for Configuration**
    - **Files to create:**
      - `src/ReSys.Core/Features/Configurations/List/ListConfigurations.cs`
      - `src/ReSys.Core/Features/Configurations/Get/GetConfiguration.cs`
      - `src/ReSys.Core/Features/Configurations/Update/UpdateConfiguration.cs`
    - **Changes needed:**
      - **`ListConfigurations.cs`**: Define `ListConfigurationsQuery` and a `Handler` that retrieves all configurations from the database.
      - **`GetConfiguration.cs`**: Define `GetConfigurationQuery` (with `Key` property) and a `Handler` that retrieves a single configuration by its key.
      - **`UpdateConfiguration.cs`**: Define `UpdateConfigurationCommand` (with `Key` and `Value`), a `Validator` to ensure the value is not empty, and a `Handler` that finds the configuration, updates its value using the domain method, and persists the change.

5.  **Create Database Seeder**
    - **File to create:** `src/ReSys.Infrastructure/Seeders/Seeder.3.Configuration.cs`
    - **Changes needed:**
      - Create a class `ConfigurationSeeder` that implements `ISeeder`.
      - In the `SeedAsync` method, check if any configurations already exist.
      - If not, create and add all 17 default configurations from the prompt using the `Configuration.Create` factory method and add them to the `DbContext`.
    - **File to modify:** `src/ReSys.Infrastructure/Seeders/Seeder.Orchestrator.cs`
    - **Changes needed:**
      - Inject `ConfigurationSeeder` in the constructor.
      - Call `await _configurationSeeder.SeedAsync(cancellationToken);` in the `SeedAsync` method after the existing seeders.

6.  **Expose API Endpoints**
    - **File to create:** `src/ReSys.API/Endpoints/ConfigurationEndpoints.cs`
    - **Changes needed:**
      - Create a class `ConfigurationEndpoints` that implements `ICarterModule`.
      - Define three endpoints:
        - `GET /api/configurations`: Maps to the `ListConfigurationsQuery`.
        - `GET /api/configurations/{key}`: Maps to the `GetConfigurationQuery`.
        - `PUT /api/configurations/{key}`: Maps to the `UpdateConfigurationCommand`.
      - Use `MapToApi` extension methods to send requests to MediatR and handle responses.

### Testing Strategy
- **Unit Tests:**
  - Create `tests/Core.UnitTests/Domain/Configurations/ConfigurationTests.cs`.
  - Test the `Configuration.Create` factory method for valid and invalid inputs (e.g., missing key).
  - Test the `Configuration.Update` method.
- **Feature Tests:**
  - Create tests for the CQRS handlers (`ListConfigurationsHandlerTests`, `GetConfigurationHandlerTests`, `UpdateConfigurationHandlerTests`) using a mocked `DbContext` or an in-memory database.
  - Verify that handlers return correct data or errors.
  - Verify that the `UpdateConfiguration` validator prevents invalid updates.

## 🎯 Success Criteria
- The `Configurations` table is created in the database and populated with the 17 default values.
- The new API endpoints (`GET /api/configurations`, `GET /api/configurations/{key}`, `PUT /api/configurations/{key}`) are functional and correctly manage configuration settings.
- All new business logic is covered by unit and feature tests.
- The implementation adheres strictly to the existing architectural patterns (DDD, CQRS, ErrorOr, Carter).
