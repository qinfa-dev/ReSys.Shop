# Authorization Claims

This directory defines custom claim types used throughout the application's authorization system. These custom claims provide a standardized and strongly-typed way to represent various authorization-related attributes within a user's identity, such as roles, permissions, and policies.

---

## Modules

### 1. `CustomClaim.cs`

This file defines a static class containing constant strings for custom claim types.

-   **Purpose**: To centralize the definition of custom claim types, ensuring consistency and preventing magic strings across the codebase when dealing with authorization logic. These claims are typically added to a user's `ClaimsPrincipal` during the authentication process and then evaluated by authorization handlers.
-   **Key Features**:
    -   `Role`: Represents a user's assigned role (e.g., "Administrator", "User").
    -   `Permission`: Represents a specific permission granted to a user (e.g., "Admin.User.Create", "Product.View").
    -   `Policy`: Represents a named authorization policy that a user satisfies.
    -   `ScopeDomain`: Represents the domain or scope within which a user's claims are valid.
    -   `Issuer`: Represents the entity that issued the claim.

---

## Purpose

The components within this directory are designed to:

-   **Standardize Claim Types**: Provide a consistent set of identifiers for custom claims used in the application's security context.
-   **Improve Readability**: Make authorization code more understandable by using named constants instead of raw strings for claim types.
-   **Facilitate Authorization Logic**: Serve as the foundation for building granular authorization rules based on roles, permissions, and policies.
-   **Enhance Maintainability**: Centralize claim definitions, making it easier to manage and update them as the application's authorization requirements evolve.
