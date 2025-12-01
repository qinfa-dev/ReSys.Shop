# Authentication Tokens Context

This directory centralizes the interfaces and models for managing authentication tokens, specifically JSON Web Tokens (JWTs) and Refresh Tokens. It provides a clear separation of concerns for token-related operations, including generation, validation, rotation, and revocation, ensuring secure and efficient user authentication and session management.

---

## Modules

### 1. JWT (`Jwt/`)

This subdirectory focuses on the management and validation of JSON Web Tokens (JWTs), which are primarily used for access control.

-   **`IJwtTokenService.cs`**:
    -   **Functionality**: Defines the contract for a service responsible for all JWT access token operations. This includes:
        -   Generating new JWTs for authenticated users.
        -   Extracting `ClaimsPrincipal` from a token.
        -   Determining the remaining time until a token expires.
        -   Validating the format and full integrity of a JWT.
        -   Parsing a JWT into a `JwtSecurityToken` object.
        -   Extracting all claims from a token.
-   **`JwtTokenValidationResult.cs`**:
    -   **Functionality**: A record that encapsulates the outcome of a JWT validation process. It provides details such as whether the token is `IsValid`, the `ClaimsIdentity` extracted, the `SecurityToken` object, the `Issuer`, and any `Exception` that occurred during validation.

### 2. Models (`Models/`)

This subdirectory contains common data transfer objects (DTOs) used across the token management services.

-   **`ActiveSessionResult.cs`**:
    -   **Functionality**: A record representing detailed information about an active user session. It includes the `TokenId`, `CreatedAt`, `ExpiresAt`, `CreatedByIp`, a flag `IsCurrentSession`, and a computed `RemainingTime` property. This model is useful for displaying active sessions to users or for administrative purposes.
-   **`AuthenticationResult.cs`**:
    -   **Functionality**: A record that aggregates the results of a successful authentication attempt. It typically contains the `AccessToken`, `RefreshToken`, their respective `AccessTokenExpiresAt` and `RefreshTokenExpiresAt` timestamps, `TokenType` (e.g., "Bearer"), and a computed `ExpiresIn` property.
-   **`TokenResult.cs`**:
    -   **Functionality**: A generic record used to return a token string and its expiration timestamp. It serves as a common return type for token generation methods.

### 3. Refresh Token (`RefreshToken/`)

This subdirectory is dedicated to the management of refresh tokens, which are used to obtain new access tokens without requiring re-authentication.

-   **`IRefreshTokenService.cs`**:
    -   **Functionality**: Defines the contract for a service that handles refresh token lifecycle management. This includes:
        -   Generating new refresh tokens for users, with an option for "remember me" functionality.
        -   Validating a raw refresh token and retrieving the associated `RefreshToken` entity and `ApplicationUser`.
        -   Rotating refresh tokens, which involves revoking an old token and issuing a new one.
        -   Revoking specific refresh tokens or all tokens for a given user.
        -   Cleaning up expired and aged revoked tokens from the storage.
-   **`RefreshTokenValidationResult.cs`**:
    -   **Functionality**: A record that holds the result of a refresh token validation. It contains the validated `Domain.Identity.Tokens.RefreshToken` entity and the `ApplicationUser` associated with it.

---

## Purpose

The components within this directory are designed to:

-   **Secure Authentication Flows**: Provide the necessary interfaces and models to implement robust and secure JWT and refresh token-based authentication.
-   **Decouple Token Logic**: Separate the concerns of JWT and refresh token management from the core authentication logic, promoting modularity and maintainability.
-   **Standardize Token Operations**: Define clear contracts for token generation, validation, and lifecycle management.
-   **Support Session Management**: Enable features like "remember me," active session tracking, and forced logout by revoking tokens.
-   **Enhance Security**: Facilitate token rotation and cleanup to mitigate security risks associated with long-lived tokens.
