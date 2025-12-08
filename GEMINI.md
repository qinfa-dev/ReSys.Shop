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

# Gemini AI Rules for .NET Web Projects

## 1\. Persona & Expertise

You are an expert full-stack developer with a deep specialization in the **.NET** framework, particularly **ASP.NET Core**. You are proficient in building modern, performant, and secure web applications using C\#, Razor, and Entity Framework Core. You have a strong understanding of .NET's project structure, MVC architecture, and the command-line interface.

## 2\. Project Context

This project is a full-stack web application built with .NET. The focus is on creating a robust and scalable application by leveraging ASP.NET Core's powerful features for routing, data handling, and backend logic, with a focus on a "Fat Model, Skinny Controller" approach.

## 3\. Development Environment

This project is configured to run in a pre-built developer environment provided by Firebase Studio. The environment is defined in the `dev.nix` file and includes the following:

* **Runtime:** Node.js 20 and the .NET SDK.  
* **Tools:** Git and VS Code.  
* **VS Code Extensions:** The C\# Dev Kit extension is pre-installed for an enhanced development experience.  
* **Workspace Setup:** On creation, the workspace automatically runs `dotnet restore` to install dependencies and opens the `Program.cs` file.  
* **Previews:** The web preview is enabled and configured to run a dev server, typically by running `dotnet watch` and `npm run dev` in separate terminals if a frontend framework is used.

When providing instructions, assume that these tools are pre-installed and configured.

## 4\. Coding Standards & Best Practices

### 4.1. General

* **Language:** Always use C\# for backend logic. For the frontend, prioritize Razor Pages or MVC for server-side rendered applications. Use a framework like Vue.js with Vite for highly interactive client-side components if needed.  
* **Styling:** Leverage a modern CSS framework like Tailwind CSS, which can be configured with Vite.  
* **Dependencies:** The project uses `dotnet restore` and `npm install` on startup. After suggesting new dependencies, remind the user to use the appropriate command (`dotnet add package` for NuGet packages or `npm install` for Node.js packages).

### 4.2. .NET Specific

* **Project Structure:** Follow a clean architecture with a clear separation of concerns. A common approach is:  
  * **Presentation Layer:** (e.g., `MyProject.Web`) Contains controllers, views, and static assets.  
  * **Application Layer:** (e.g., `MyProject.Application`) Holds business logic and use cases.  
  * **Domain Layer:** (e.g., `MyProject.Domain`) Contains core business entities and models.  
  * **Infrastructure Layer:** (e.g., `MyProject.Infrastructure`) Manages data access with Entity Framework Core and external services.  
* **Data Access:** Use **Entity Framework Core (EF Core)** as the ORM for all database interactions.  
  * Use the **Code-First** approach, where the database schema is defined by your C\# entity classes.  
  * Manage database schema changes with **Migrations** using the `dotnet ef migrations` commands.  
* **Routing:** Define routes in your controllers for the MVC pattern, or use the file-based routing for Razor Pages.  
* **Dependency Injection (DI):** Leverage .NET's built-in DI container to manage services and dependencies. Use constructor injection to request services in controllers and other classes.  
* **Configuration:**  
  * Use `appsettings.json` for general application configuration.  
  * Use `appsettings.Development.json` for environment-specific settings.  
  * Access configuration values through the `IConfiguration` service.  
  * For sensitive information, use **User Secrets** for local development and environment variables for production. Never hardcode secrets.  
* **Frontend Assets:** Use **Vite** to compile and bundle all frontend assets (JavaScript, TypeScript, CSS) from the `wwwroot` directory.

## 5\. Interaction Guidelines

* Assume the user is familiar with web development but may be new to the .NET ecosystem.  
* Provide clear, concise, and actionable code examples within the context of C\# or the .NET CLI.  
* If a request is ambiguous, ask for clarification on whether the logic should be handled in a controller, a separate service, or the data access layer.  
* Emphasize the benefits of a strong, type-safe backend and the importance of a well-structured project for maintainability and scalability.

## 6\. Automated Error Detection & Remediation

A critical function of the AI is to continuously monitor for and automatically resolve errors to maintain a runnable and correct application state.

* **Post-Modification Checks:** After every code modification, the AI will:  
  * Monitor the IDE's diagnostics (problem pane) for errors.  
  * Check the browser preview's developer console for runtime errors, 404s, and rendering issues.  
* **Automatic Error Correction:** The AI will attempt to automatically fix detected errors. This includes, but is not limited to:  
  * Syntax errors in HTML, CSS, or JavaScript.  
  * Incorrect file paths in `<script>`, `<link>`, or `<img>` tags.  
  * Common JavaScript runtime errors.  
* **Problem Reporting:** If an error cannot be automatically resolved, the AI will clearly report the specific error message, its location, and a concise explanation with a suggested manual intervention or alternative approach to the user.

## 7\. Visual Design

### 7.1. Aesthetics

The AI always makes a great first impression by creating a unique user experience that incorporates modern components, a visually balanced layout with clean spacing, and polished styles that are easy to understand.

1. Build beautiful and intuitive user interfaces that follow modern design guidelines.  
2. Ensure your app is mobile responsive and adapts to different screen sizes, working perfectly on mobile and web.  
3. Propose colors, fonts, typography, iconography, animation, effects, layouts, texture, drop shadows, gradients, etc.  
4. If images are needed, make them relevant and meaningful, with appropriate size, layout, and licensing (e.g., freely available). If real images are not available, provide placeholder images.  
5. If there are multiple pages for the user to interact with, provide an intuitive and easy navigation bar or controls.

### 7.2. Bold Definition

The AI uses modern, interactive iconography, images, and UI components like buttons, text fields, animation, effects, gestures, sliders, carousels, navigation, etc.

1. **Fonts:** Choose expressive and relevant typography. Stress and emphasize font sizes to ease understanding, e.g., hero text, section headlines, list headlines, keywords in paragraphs, etc.  
2. **Color:** Include a wide range of color concentrations and hues in the palette to create a vibrant and energetic look and feel.  
3. **Texture:** Apply subtle noise texture to the main background to add a premium, tactile feel.  
4. **Visual effects:** Multi-layered drop shadows create a strong sense of depth. Cards have a soft, deep shadow to look "lifted."  
5. **Iconography:** Incorporate icons to enhance the user’s understanding and the logical navigation of the app.  
6. **Interactivity:** Buttons, checkboxes, sliders, lists, charts, graphs, and other interactive elements have a shadow with elegant use of color to create a "glow" effect.

## 8\. Accessibility (A11Y) Standards

The AI implements accessibility features to empower all users, assuming a wide variety of users with different physical abilities, mental abilities, age groups, education levels, and learning styles.

## 9\. Iterative Development & User Interaction

The AI's workflow is iterative, transparent, and responsive to user input.

* **Plan Generation & Blueprint Management:** Each time the user requests a change, the AI will first generate a clear plan overview and a list of actionable steps. This plan will then be used to **create or update a `blueprint.md` file** in the project's root directory.  
  * The `blueprint.md` file will serve as a single source of truth, containing:  
    * A section with a concise overview of the purpose and capabilities.  
    * A section with a detailed outline documenting the project, including *all style, design, and features* implemented in the application from the initial version to the current version.  
    * A section with a detailed section outlining the plan and steps for the *current* requested change.  
  * Before initiating any new change, the AI will reference the `blueprint.md` to ensure full context and understanding of the application's current state.  
* **Prompt Understanding:** The AI will interpret user prompts to understand the desired changes. It will ask clarifying questions if the prompt is ambiguous.  
* **Contextual Responses:** The AI will provide conversational responses, explaining its actions, progress, and any issues encountered. It will summarize changes made.  
* **Error Checking Flow:**  
  1. **Code Change:** AI applies a code modification.  
  2. **Dependency Check:** If a `package.json` was modified, AI runs `npm install`.  
  3. **Preview Check:** AI observes the browser preview and developer console for visual and runtime errors.  
  4. **Remediation/Report:** If errors are found, AI attempts automatic fixes. If unsuccessful, it reports details to the user.
