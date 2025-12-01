# Authorization Providers

This directory defines the interfaces and data structures necessary for providing and managing user authorization data within the application. It establishes a contract for retrieving a user's permissions, roles, and policies, and for invalidating cached authorization information, ensuring that authorization decisions are always based on up-to-date data.

---

## Modules

### 1. `IUserAuthorizationProvider.cs`

This file defines the interface for any service that can provide user-specific authorization data.

-   **Purpose**: To abstract the source and retrieval mechanism of a user's authorization information. This allows different implementations (e.g., fetching from a database, an external identity service, or a cache) to be plugged in without affecting the core authorization logic.
-   **Key Features**:
    -   `GetUserAuthorizationAsync(string userId)`: Asynchronously retrieves the `UserAuthorizationData` for a given user ID. Returns `null` if the user or their authorization data is not found.
    -   `InvalidateUserAuthorizationAsync(string userId)`: Asynchronously invalidates any cached authorization data for a specific user, forcing a fresh retrieval on the next request.

### 2. `UserAuthorizationData.cs`

This file defines a record that encapsulates all relevant authorization information for a user.

-   **Purpose**: To provide a structured and immutable representation of a user's permissions, roles, and policies, which can be easily passed around the application for making authorization decisions.
-   **Key Features**:
    -   **Properties**:
        -   `UserId`: The unique identifier of the user.
        -   `UserName`: The username of the user.
        -   `Email`: The email address of the user.
        -   `Permissions`: A read-only list of permissions assigned to the user.
        -   `Roles`: A read-only list of roles assigned to the user.
        -   `Policies`: A read-only list of policies applicable to the user.
    -   **JSON Serialization Attributes**: Includes `JsonPropertyName` attributes to control how the properties are serialized to JSON, ensuring consistent naming conventions (e.g., `user_id`, `user_name`).

---

## Purpose

The components within this directory are designed to:

-   **Decouple Authorization Data Retrieval**: Separate the logic for obtaining user authorization data from the actual authorization enforcement mechanisms.
-   **Standardize Authorization Data**: Provide a consistent format for representing a user's permissions, roles, and policies.
-   **Support Caching Strategies**: Enable the implementation of caching for user authorization data, improving performance by reducing redundant data fetches.
-   **Facilitate Dynamic Authorization**: Allow for dynamic updates and invalidation of user authorization data, ensuring that changes to permissions or roles are reflected promptly.
-   **Enhance Testability**: Make it easier to test authorization logic by providing a clear interface for mocking user authorization data.
