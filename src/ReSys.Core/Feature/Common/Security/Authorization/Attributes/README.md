# Authorization Attributes

This directory contains custom authorization attributes and extension methods designed to provide a flexible and robust authorization mechanism within the application. It extends ASP.NET Core's built-in authorization capabilities by allowing granular control over permissions, roles, and policies, with a focus on reusability and ease of configuration.

---

## Modules

### 1. `RequestAuthorize.Attribute.cs`

This file defines a custom `RequestAuthorizeAttribute` that inherits from `Microsoft.AspNetCore.Authorization.AuthorizeAttribute`. This attribute is the core component for applying authorization rules to controllers, actions, or Minimal API endpoints.

-   **Purpose**: To offer a highly configurable authorization attribute that can enforce access based on a combination of permissions, roles, and policies, thereby centralizing authorization logic and improving code clarity.
-   **Key Features**:
    -   **Flexible Authorization**: Supports specifying required `Permissions`, `Roles`, and `Policies` either individually or in combination.
    -   **Constructor Overloads**: Allows initialization with comma-separated strings or arrays for each authorization type.
    -   **Static Factory Methods**: Provides convenient `WithPermissions`, `WithRoles`, `WithPolicies`, and `WithAnyPermission` methods for common use cases.
    -   **Policy Building**: Internally constructs a policy string that the ASP.NET Core authorization system can interpret, using custom claim types (`CustomClaim.Permission`, `CustomClaim.Policy`, `CustomClaim.Role`).
    -   **Validation**: Includes validation to ensure that at least one authorization parameter is provided and that claim values are not empty.
    -   **Performance**: Uses lazy initialization for policy building.

### 2. `RequestAuthorize.Extensions.cs`

This file provides a set of extension methods for `Microsoft.AspNetCore.Builder.IEndpointConventionBuilder`, simplifying the application of authorization rules to endpoints, especially in Minimal APIs.

-   **Purpose**: To offer a fluent and expressive API for configuring authorization requirements on endpoints, reducing boilerplate and making authorization setup more intuitive.
-   **Key Features**:
    -   **Fluent API**: Methods like `RequirePermission`, `RequirePermissions`, `RequireRole`, `RequireRoles`, `RequirePolicy`, and `RequirePolicies` allow for chaining authorization rules.
    -   **Granular Control**: Supports requiring single or multiple permissions, roles, or policies.
    -   **`RequireAccessPermission` / `RequireAccessPermissions`**: Integrates directly with `Domain.Identity.Permissions.AccessPermission` entities for type-safe permission requirements.
    -   **`RequireCustomAuthorization`**: A general-purpose method for combining permissions, policies, and roles with custom string configurations.
    -   **`RequireAdminAccess`**: A convenience method for applying common administrative access rules.
    -   **Caching**: Uses a `ConcurrentDictionary` to cache `RequestAuthorizeAttribute` instances, improving performance by avoiding repeated object creation.
    -   **Cache Management**: Includes `ClearCache` and `GetCacheSize` methods for testing and monitoring.

### 3. `AuthorizationBuilder.cs`

This file defines a `AuthorizationBuilder` class that provides a fluent API for programmatically constructing authorization requirements.

-   **Purpose**: To enable the programmatic construction of complex authorization requirements (permissions, roles, policies) in a clear and readable manner, which can then be used to create `RequestAuthorizeAttribute` instances.
-   **Key Features**:
    -   **Fluent API**: Methods like `RequirePermission`, `RequirePermissions`, `RequireRole`, `RequireRoles`, `RequirePolicy`, and `RequirePolicies` allow for chaining authorization rules to build up a set of requirements.
    -   **`Build()` Method**: Creates a `RequestAuthorizeAttribute` instance based on the configured requirements.
    -   **Static Factory Methods**: Provides convenient methods like `Create()`, `ForAdmin()`, `ForUserManagement()`, and `ForRoleManagement()` to pre-configure common authorization scenarios.
    -   **`GetPolicyString()`**: Returns the raw policy string that would be generated, useful for debugging or advanced scenarios.
    -   **`Reset()`**: Allows the builder to be reused by clearing its current configuration.

---

## Purpose

The components within this directory are designed to:

-   **Centralize Authorization Logic**: Provide a single, consistent mechanism for defining and enforcing authorization rules across the application.
-   **Enhance Readability and Maintainability**: Offer a declarative and programmatic way to specify authorization requirements, making code easier to understand and manage.
-   **Improve Developer Productivity**: Simplify the process of applying complex authorization rules to API endpoints through fluent APIs and builder patterns.
-   **Promote Reusability**: Allow authorization rules to be defined once and applied consistently across different parts of the application.
-   **Integrate with ASP.NET Core Authorization**: Leverage the existing ASP.NET Core authorization framework while extending its capabilities to meet specific application needs.