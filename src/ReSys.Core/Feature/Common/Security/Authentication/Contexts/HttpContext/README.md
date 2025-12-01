# HTTP Context User Authentication Context

This directory provides abstractions and utility methods for extracting and managing user authentication context directly from the HTTP request's `ClaimsPrincipal`. It simplifies access to common user information such as ID, username, and authentication status, making it easier to implement security-related logic throughout the application.

---

## Modules

### 1. `ClaimsPrincipalExtensions.cs`

This file contains extension methods for the `System.Security.Claims.ClaimsPrincipal` class. These extensions provide a convenient way to access frequently used user claims.

-   **Purpose**: To simplify the retrieval of core user identity information from the `ClaimsPrincipal` object, reducing boilerplate code and improving readability in security-sensitive operations.
-   **Key Features**:
    -   `GetUserId(this ClaimsPrincipal user)`: Retrieves the user's unique identifier (typically from `ClaimTypes.NameIdentifier`).
    -   `GetUserName(this ClaimsPrincipal user)`: Retrieves the user's name (typically from `ClaimTypes.Name`).
    -   `IsAuthenticated(this ClaimsPrincipal user)`: Checks if the user's identity is authenticated.

### 2. `IUserContext.cs`

This file defines an interface that represents the current user's context within the application.

-   **Purpose**: To provide a clear contract for accessing authenticated user information, allowing for dependency injection and easier testing of components that rely on the current user's identity. It abstracts away the underlying mechanism of how user information is obtained from the HTTP context.
-   **Key Features**:
    -   `UserId`: Property to get the unique identifier of the authenticated user.
    -   `UserName`: Property to get the name of the authenticated user.
    -   `IsAuthenticated`: Property to check if the current user is authenticated.

---

## Purpose

The components within this directory are designed to:

-   **Simplify User Context Access**: Provide a straightforward and consistent way to retrieve authenticated user details from the HTTP request.
-   **Promote Testability**: By using the `IUserContext` interface, components that depend on user information can be easily tested with mock implementations.
-   **Decouple from HTTP Specifics**: Abstract away the direct interaction with `ClaimsPrincipal` and HTTP context, making the application logic cleaner and more portable.
-   **Enhance Security Logic**: Facilitate the implementation of authorization and personalization features by providing readily available user identity information.
