# Gemini Instructions for ReSys.Shop

This document provides a comprehensive overview of the `ReSys.Shop` project, intended to serve as instructional context for Gemini. It synthesizes information from various project documentation, including `README.md`, `COPILOT_INSTRUCTIONS_SUMMARY.md`, `README_REVIEW_GUIDE.md`, and `API_SPECIFICATION.md`.

## Project Overview

`ReSys.Shop` is a modern, full-stack e-commerce application designed as a fashion recommendation system. It leverages a microservice-ready architecture with a .NET 9 backend, Vue 17+ frontends (sales and admin), and a Python-based visual similarity recommendation engine. The project emphasizes scalability, real-time product suggestions, and cloud-ready REST APIs. The API design largely follows Spree Commerce API v2 patterns and uses the JSON:API specification for responses.

As of November 30, 2025, a comprehensive review indicates a robust domain model with 42 aggregates/entities and over 150 documented API endpoints. The project is currently focused on achieving 100% domain coverage by implementing 38 identified missing API endpoints, a task estimated to take approximately 3 weeks with parallel teams.

## Architecture

The project adheres to a Clean Architecture pattern, primarily consisting of:

*   **ReSys.API (Presentation Layer):** An ASP.NET Core Web API project (`net9.0`) serving as the entry point. It uses `Carter` for minimal APIs, `Swashbuckle.AspNetCore` for API documentation, `Microsoft.AspNetCore.Authentication.JwtBearer` for authentication, and `Serilog` for logging. API responses strictly follow the JSON:API specification.
*   **ReSys.Core (Domain/Application Layer):** A .NET Standard library (`net9.0`) encapsulating the core business logic and domain entities. It extensively uses:
    *   **MediatR:** For implementing CQRS (Command Query Responsibility Segregation) patterns and in-process messaging, with a `ValidationBehavior` for pipeline validation.
    *   **FluentValidation:** For defining and executing validation rules for commands and queries.
    *   **Mapster:** For high-performance object mapping between domain entities and DTOs.
    *   **ErrorOr:** For functional error handling (Railway-Oriented Programming).
    *   **Microsoft.AspNetCore.Identity.EntityFrameworkCore:** For identity management.
    *   **Microsoft.EntityFrameworkCore, Pgvector, Pgvector.EntityFrameworkCore:** Indicating data persistence with PostgreSQL, leveraging vector capabilities for visual similarity model integration.
*   **ReSys.Infrastructure (Infrastructure Layer):** A .NET Standard library (`net9.0`) intended for external concerns such as data access, external service integrations, and other infrastructure-specific implementations.

The internal feature architecture within the Core layer typically follows a three-layer pattern: `Command/Query` → `Validator` → `Handler` → `Mapper`. The project also employs a **Bounded Contexts** approach, with identified contexts such as Catalog, Orders, Inventories, Promotions, and Identity.

## Technologies Used

*   **Backend:** .NET 9 (C#)
*   **Frontends:** Vue 17+ (Sales and Admin UIs)
*   **Recommendation Engine:** Python 3.8+ (Deep learning for visual similarity, integrated via RESTful APIs)
*   **Database:** PostgreSQL (with `Pgvector` extension)
*   **API Framework:** ASP.NET Core, Carter (minimal APIs)
*   **API Specification:** JSON:API
*   **Messaging:** MediatR (CQRS, Event-Driven Architecture)
*   **Validation:** FluentValidation
*   **Mapping:** Mapster
*   **Logging:** Serilog
*   **Authentication:** JWT Bearer (RS256)
*   **ORM:** Entity Framework Core

## Building and Running

### Prerequisites

*   .NET 9 SDK
*   Node.js & Vue CLI (for frontends, if working on frontend)
*   Python 3.8+ (for the recommendation model, if working on the model)
*   Docker/PostgreSQL (for database setup)

### Build the Project

From the root directory:

```bash
dotnet build ReSys.Shop.sln
```

### Run the API

From the root directory:

```bash
dotnet run --project src/ReSys.API/ReSys.API.csproj
```

Alternatively, navigate to `src/ReSys.API` and run:

```bash
dotnet run
```

### Run Tests

Assuming test projects are set up in the `tests/` directory (e.g., xUnit, NUnit patterns):

From the root directory:

```bash
dotnet test
```

## Development Conventions

The project maintains strong development conventions and architectural patterns, extensively documented in `.github/copilot-instructions.md` (which serves as the primary detailed guide for AI agents). Key conventions include:

*   **Layered Architecture:** Clear separation of concerns (API, Core, Infrastructure).
*   **CQRS & MediatR:** Extensive use of MediatR for commands, queries, and domain events, promoting a clean, maintainable, and testable codebase with a defined pipeline behavior including validation.
*   **Domain-Driven Design (DDD):** Implementation of patterns like:
    *   **Aggregates:** Enforcing consistency boundaries and managing state changes.
    *   **ErrorOr:** For robust and functional error handling, avoiding exceptions for control flow.
    *   **Factory Methods:** For safe and controlled creation of domain objects.
    *   **Domain Events:** For decoupled communication within the domain.
*   **FluentValidation:** All incoming requests (commands/queries) are expected to be validated using FluentValidation, integrated into the MediatR pipeline. Centralized constraint definitions are used.
*   **Mapster:** Used for high-performance object-to-object mapping between various layers (e.g., DTOs to domain entities), reducing boilerplate.
*   **Serilog:** Centralized, structured logging across the application.
*   **JSON:API:** All API responses strictly adhere to the JSON:API specification.
*   **Testing Philosophy:** Emphasizes unit and integration tests, with patterns for various test scenarios.
*   **Common Development Workflows:** Defined processes for adding new features (e.g., 5-step process for Commands/Queries), database migrations, and general project structure.

## Key Learnings & Considerations for the Agent

*   **Primary Agent Instruction Document:** For detailed architectural guidance, domain model patterns, workflows, and anti-patterns, refer to `.github/copilot-instructions.md`.
*   **Initial `Program.cs` Observations:** The `src/ReSys.API/Program.cs` file, while functional for basic setup, currently appears to be missing explicit service registrations for `ReSys.Core` and `ReSys.Infrastructure` layers (e.g., `builder.Services.AddCore();` or `builder.Services.AddInfrastructure();`). If asked to add or modify services within these layers, the first step may involve adding the appropriate extension method calls in `Program.cs` or verifying existing ones.
*   **Infrastructure Layer Configuration:** The `ReSys.Infrastructure` project's `DependencyInjection.cs` is intended for registering infrastructure-specific services (e.g., database contexts, external API clients, custom repositories). If this file is empty, new infrastructure services should be registered here, and then an `AddInfrastructure()` extension method would be called from `Program.cs`.
*   **Database Configuration:** There is no explicit database connection string in `appsettings.json` or `appsettings.Development.json`. Database configuration might be environment-variable-driven or set up in a different, currently undiscovered, manner (e.g., `User Secrets`, cloud provider configuration).
*   **Missing API Endpoints:** The project has identified 38 missing API endpoints crucial for 100% domain coverage. Specifications for these are available in `docs/MISSING_ENDPOINTS_SPEC.md`. When implementing new features, prioritize addressing these gaps.
*   **Frontend and Python Model:** This `GEMINI.md` primarily focuses on the .NET backend. The Vue frontends and the Python visual similarity model are integral parts of the system, integrated via RESTful APIs. Interactions with these components would require consulting their specific documentation or API specifications.

## Comprehensive Documentation Reference

The project includes several detailed documentation files critical for understanding specific aspects of the system:

*   **`.github/copilot-instructions.md`**: The definitive guide for AI agents, covering architectural patterns, domain models, common pitfalls, and development workflows in detail.
*   **`COMPREHENSIVE_DOMAIN_API_REVIEW.md`**: Provides an executive summary of the API coverage, inventory of domain models, and a high-level implementation roadmap.
*   **`DOMAIN_API_ALIGNMENT_REVIEW.md`**: Offers a detailed gap analysis matrix for missing features and provides timeline estimates.
*   **`docs/MISSING_ENDPOINTS_SPEC.md`**: Contains 38 complete, production-ready specifications for identified missing API endpoints, ready for implementation.
*   **`ENDPOINT_BREAKDOWN.md`**: An ASCII tree and visual reference of all documented API endpoints.
*   **`DOMAIN_STRUCTURE_SUMMARY.json`**: A JSON inventory of all 42 domain models, including properties, constraints, relationships, and business rules.
*   **`docs/API_SPECIFICATION.md`**: Exhaustive documentation of all API endpoints for both storefront and admin platforms, including request/response examples and authentication details.
