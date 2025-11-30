# Plan Workflow: Vertical Slice ASP.NET Application

This document outlines the steps to set up a vertical slice ASP.NET application with minimal APIs using Carter, Scalar/Swagger for API documentation, MediatR, Result Pattern with ErrorOr, PostgreSQL with PgVector, EF Core with Unit of Work and Application DbContext, Identity with roles, users, claims, and external login with Google, Fluent Validation, and comprehensive testing.

## Todo List

### 1. Project Setup & Core Configuration (Pending)
- [ ] Initialize ASP.NET Core Web API project.
- [ ] Configure project structure for vertical slices (e.g., `Features` folder with subfolders for each feature).
- [ ] Add necessary NuGet packages for all planned technologies.

### 2. API Framework: Carter (Pending)
- [ ] Integrate Carter for defining minimal APIs.
- [ ] Create a basic Carter module for a sample endpoint.

### 3. API Documentation: Scalar/Swagger (Pending)
- [ ] Configure Swagger/OpenAPI for API documentation.
- [ ] Integrate Scalar for enhanced API documentation UI.

### 4. Database Setup: PostgreSQL & EF Core (Pending)
- [ ] Set up PostgreSQL database (locally or via Docker).
- [ ] Install and configure Npgsql.EntityFrameworkCore.PostgreSQL.
- [ ] Implement `ApplicationDbContext` inheriting from `DbContext`.
- [ ] Implement a generic `IUnitOfWork` interface and a concrete `UnitOfWork` wrapper, utilizing `IApplicationDbContext` directly to expose `DbSet<TEntity>` for data access.
- [ ] Configure connection string in `appsettings.json`.
- [ ] Set up EF Core Migrations.
- [ ] (Optional but recommended for later) Integrate PgVector extensions for vector embeddings (initial setup might just be enabling the extension in Postgres).

### 5. Application Patterns: MediatR & ErrorOr (Pending)
- [ ] Integrate MediatR for in-process command/query dispatching.
- [ ] Implement a basic command/query handler using MediatR.
- [ ] Introduce `ErrorOr` for handling results and errors.
- [ ] Create custom `Error` codes and messages (e.g., `TodoItem.NotFound`).
- [ ] Wrap MediatR responses with `ErrorOr<TResponse>`.

### 6. Authentication & Authorization: ASP.NET Core Identity (Pending)
- [ ] Configure ASP.NET Core Identity with EF Core for user management.
- [ ] Implement custom `ApplicationUser` and `ApplicationRole` classes.
- [ ] Set up role-based authorization.
- [ ] Implement claims-based authorization with permissions per endpoint.
- [ ] Configure external login with Google:
    - [ ] Register application with Google Developer Console to obtain Client ID and Client Secret.
    - [ ] Add Google authentication middleware.

### 7. Validation: FluentValidation (Pending)
- [ ] Integrate FluentValidation for request validation.
- [ ] Define validation rules for sample requests.
- [ ] Implement custom validation error codes (e.g., `TodoItem.Id.Required`).
- [ ] Integrate FluentValidation with MediatR pipeline for automatic validation.

### 8. Testing (Pending)
- [ ] Set up xUnit for unit and integration testing.
- [ ] Integrate Shouldly for assertion syntax (preferred over Fluent Assertions).
- [ ] Integrate NSubstitute for mocking.
- [ ] Implement parameterized tests where appropriate (e.g., using `[InlineData]` or `[MemberData]` with xUnit).
- [ ] Write unit tests for MediatR handlers, services, and validators.
- [ ] Write integration tests for API endpoints.

### 9. Examples & Documentation (Pending)
- [ ] Create a "TodoItem" feature with:
    - [ ] Minimal API endpoints (Create, Read, Update, Delete) using Carter.
    - [ ] MediatR commands/queries for each operation.
    - [ ] `ErrorOr` return types for all operations.
    - [ ] FluentValidation for input models, including custom error codes.
    - [ ] EF Core entity and direct `IApplicationDbContext` usage via Unit of Work.
    - [ ] Identity-based authorization for endpoints (e.g., only authenticated users can create, specific roles can delete).
    - [ ] Swagger/Scalar documentation for TodoItem endpoints.
    - [ ] Unit and integration tests for the TodoItem feature.
- [ ] Document how to run the application, migrations, and tests.

## Review Section

_This section will be populated upon completion of the task._
