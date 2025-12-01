# Common Security Authentication Use Cases

This directory serves as the central hub for all authentication-related use cases and abstractions within the application's security layer. It provides a structured approach to managing user identity, handling various authentication mechanisms (local and external), and ensuring secure access to application resources. The modules within this directory are designed to be flexible, extensible, and adhere to security best practices.

---

## Modules

### 1. Contexts (`Contexts/`)

This subdirectory focuses on providing contextual information about the authenticated user and managing authentication tokens.

-   **HTTP Context (`HttpContext/`)**:
    -   **Purpose**: Offers abstractions and utility methods for extracting and managing user authentication context directly from the HTTP request's `ClaimsPrincipal`. It simplifies access to common user information.
    -   **Key Files**:
        -   `ClaimsPrincipalExtensions.cs`: Provides extension methods for `ClaimsPrincipal` to easily retrieve `UserId`, `UserName`, and `IsAuthenticated` status.
        -   `IUserContext.cs`: Defines an interface for accessing current user information, promoting testability and decoupling from HTTP specifics.
-   **Tokens (`Tokens/`)**:
    -   **Purpose**: Centralizes interfaces and models for managing authentication tokens, specifically JSON Web Tokens (JWTs) and Refresh Tokens. It covers token generation, validation, rotation, and revocation.
    -   **Key Files**:
        -   `Jwt/IJwtTokenService.cs`: Defines the interface for JWT access token management.
        -   `Jwt/JwtTokenValidationResult.cs`: Model for JWT validation results.
        -   `Models/ActiveSessionResult.cs`: Model for active user session information.
        -   `Models/AuthenticationResult.cs`: Model for the result of an authentication operation.
        -   `Models/TokenResult.cs`: Generic model for token strings and expiration times.
        -   `RefreshToken/IRefreshTokenService.cs`: Defines the interface for refresh token lifecycle management.
        -   `RefreshToken/RefreshTokenValidationResult.cs`: Model for refresh token validation results.

### 2. Externals (`Externals/`)

This subdirectory handles the integration with external identity providers for authentication.

-   **Purpose**: Provides interfaces, models, and utilities for standardizing the process of validating external tokens, normalizing user information from different providers, and managing external logins within the application's `Identity` framework.
-   **Key Files**:
    -   `ExternalUserInfo.cs`: A standardized data model for user information retrieved from external identity providers (e.g., Google, Facebook).
    -   `IExternalTokenValidator.cs`: Defines the interface for validating tokens received from external identity providers.
    -   `IExternalUserService.cs`: Defines the interface for managing user accounts linked to external identity providers.
    -   `OAuthProvider.cs`: An enumeration listing the supported external OAuth providers.

---

## Purpose

The overall purpose of the `Authentication` directory is to:

-   **Provide a Unified Authentication Layer**: Offer a consistent and secure way to handle user authentication across the application, regardless of the underlying authentication mechanism.
-   **Support Multiple Authentication Methods**: Accommodate both local (e.g., username/password) and external (e.g., OAuth providers) authentication flows.
-   **Manage User Sessions**: Implement robust session management through JWTs and refresh tokens, including features like token rotation, revocation, and active session tracking.
-   **Abstract Security Complexities**: Decouple the application's business logic from the intricate details of authentication protocols and token management.
-   **Enhance Security and User Experience**: Ensure secure authentication processes while providing a smooth and flexible login experience for users.
