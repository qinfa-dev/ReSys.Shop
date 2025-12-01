# External Authentication Context

This directory provides the interfaces, models, and utilities necessary for integrating external identity providers (like Google, Facebook, etc.) into the application's authentication system. It standardizes the process of validating external tokens, normalizing user information from different providers, and managing external logins within the application's `Identity` framework.

---

## Modules

### 1. `ExternalUserInfo.cs`

This file defines a standardized data model for user information retrieved from any external identity provider.

-   **Purpose**: To normalize the diverse data structures returned by various OAuth providers into a single, consistent format, simplifying user provisioning and profile management within the application.
-   **Key Features**:
    -   **Properties**: Includes `ProviderId` (unique ID from the external provider), `Email`, `FirstName`, `LastName`, `ProfilePictureUrl`, `EmailVerified` status, `ProviderName`, and `AdditionalClaims` (for extra provider-specific data).
    -   **`DisplayName`**: A computed property that provides a user-friendly display name, prioritizing first/last names or falling back to the email.
    -   **`Create` Method**: A static factory method for creating validated instances of `ExternalUserInfo`, ensuring essential fields are present.

### 2. `IExternalTokenValidator.cs`

This interface defines the contract for validating tokens received from external identity providers.

-   **Purpose**: To abstract the provider-specific logic required to verify the authenticity and integrity of tokens (e.g., access tokens, ID tokens, authorization codes) issued by external OAuth services.
-   **Key Features**:
    -   `ValidateTokenAsync`: A method that takes provider-specific tokens and returns a normalized `ExternalUserInfo` object upon successful validation, or an `ErrorOr` error if validation fails.

### 3. `IExternalUserService.cs`

This interface defines the contract for managing user accounts that are linked to external identity providers.

-   **Purpose**: To handle the application's internal `Identity` operations related to external logins, such as linking external accounts to existing users, creating new users from external logins, and managing the lifecycle of these associations.
-   **Key Features**:
    -   `FindOrCreateUserWithExternalLoginAsync`: Finds an existing user or creates a new one based on external user information, handling the association with the external login.
    -   `HasExternalLoginAsync`: Checks if a user already has an external login for a specific provider.
    -   `GetExternalLoginsAsync`: Retrieves all external login details associated with a user.
    -   `RemoveExternalLoginAsync`: Removes an external login association from a user's account, with safety checks.

### 4. `OAuthProvider.cs`

This enumeration lists the external OAuth providers supported by the application.

-   **Purpose**: To provide a strongly typed way to refer to different external authentication services, improving code clarity and maintainability.
-   **Values**: Includes common providers like `Google`, `Facebook`, `Twitter`, and `GitHub`.

---

## Purpose

The components within this directory are designed to:

-   **Facilitate External Logins**: Enable users to authenticate using their existing accounts from popular identity providers.
-   **Standardize External Data**: Provide a consistent way to handle user data coming from various external sources.
-   **Decouple Provider-Specific Logic**: Abstract away the complexities of interacting with different OAuth APIs, making the system more extensible.
-   **Integrate with Internal Identity**: Seamlessly connect external authentication flows with the application's core user management system.
-   **Enhance User Experience**: Offer convenient and familiar login options for users.
