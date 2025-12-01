# Identity.Users.Tokens Bounded Context

This document describes the `Identity.Users.Tokens` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages generic tokens associated with individual user accounts. These tokens are typically used for single-use or short-lived authentication mechanisms such as password reset links, email confirmation links, or multi-factor authentication setup. It provides a flexible way to issue and manage secure, temporary credentials for various user management workflows.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users.Tokens` bounded context.

-   **User Token**: A temporary, secure credential associated with a user for specific, short-term purposes. Represented by the `UserToken` entity.
-   **User**: The application user to whom the token is issued. (Referenced from `Identity.Users` Bounded Context).
-   **Login Provider**: The authentication provider that issued the token. For internal tokens, this might be the application itself.
-   **Token Name**: A logical name for the token's purpose (e.g., "PasswordReset", "EmailConfirmation", "AuthenticatorKey").
-   **Token Value**: The actual cryptographic string of the token.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `UserToken` is an entity that is owned by the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`UserToken`**: This is the central entity of this bounded context. It represents a single generic token associated with a user and inherits from `IdentityUserToken<string>` for ASP.NET Core Identity integration.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `UserId`, `LoginProvider`, `Name`, and `Value` are intrinsic attributes of the `UserToken` entity, inherited or directly managed.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users.Tokens` bounded context.

-   A `UserToken` must always be associated with a valid `UserId`.
-   The combination of `UserId`, `LoginProvider`, and `Name` must be unique for each `UserToken`, ensuring that only one token of a specific type from a specific provider exists for a user at a time.
-   Tokens typically have an expiration or are single-use, although their lifecycle management (e.g., expiration, invalidation after use) is often handled by application services that interact with this entity.

---

## 🤝 Relationships & Dependencies

-   **`UserToken` to `ApplicationUser`**: Many-to-one relationship. `UserToken` is owned by `ApplicationUser` (from `Identity.Users`).
-   **ASP.NET Core Identity Integration**: Inherits from `IdentityUserToken<string>`, integrating seamlessly with the framework's token management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create User Token**: Generate and store a new token for a user for a specific purpose (e.g., password reset).
-   **Retrieve User Token**: Find a token by its user, login provider, and name for validation.
-   **Remove User Token**: Delete a token after it has been used or has expired.

---

## 📝 Considerations / Notes

-   `UserToken` acts as a child entity within the `ApplicationUser` aggregate, and its lifecycle is managed by the `ApplicationUser` aggregate or by the ASP.NET Core Identity framework.
-   This domain provides the persistence mechanism for various temporary tokens used in identity-related workflows.
-   The actual token generation logic (e.g., cryptographic strength, uniqueness) and validation logic (e.g., checking expiration) are typically implemented in application services or helper classes that utilize this entity.
