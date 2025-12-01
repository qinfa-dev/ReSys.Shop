# Identity.Tokens Bounded Context

This document describes the `Identity.Tokens` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the generation, validation, and lifecycle of authentication tokens, specifically JSON Web Tokens (JWTs) for access and Refresh Tokens for session management. It provides the foundational mechanisms for secure user authentication, authorization, and maintaining user sessions without requiring frequent re-authentication, thereby enhancing both security and user experience.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Tokens` bounded context.

-   **JSON Web Token (JWT)**: A compact, URL-safe means of representing claims to be transferred between two parties. JWTs are typically short-lived and used for access control to protected resources. Represented conceptually by the `Jwt` static helper class.
-   **Refresh Token**: A long-lived, cryptographically secure token used to obtain new, short-lived JWTs without requiring the user to re-enter their credentials. It is central to maintaining persistent user sessions. Represented by the `RefreshToken` entity.
-   **Token Hash**: A cryptographic hash of the raw refresh token string, stored in the database instead of the raw token itself to enhance security against data breaches.
-   **Expires At**: The precise timestamp when a token (either JWT or Refresh Token) becomes invalid and can no longer be used.
-   **Revoked At**: The timestamp when a Refresh Token was explicitly invalidated by the system or user, rendering it unusable before its natural expiration.
-   **Created By IP / Revoked By IP**: The IP addresses associated with the creation or revocation of a Refresh Token, used for auditing and security analysis.
-   **Token Family**: A mechanism to group related Refresh Tokens (e.g., when a Refresh Token is rotated, the new token belongs to the same family as the old one). This enables strategies like immediate invalidation of all old tokens in a family upon rotation.
-   **Is Expired**: A computed property indicating whether a token's `ExpiresAt` timestamp is in the past.
-   **Is Revoked**: A computed property indicating whether a Refresh Token has been explicitly invalidated.
-   **Claims**: Key-value pairs of information (assertions) about a subject (typically a user) that are encoded within a JWT (e.g., user ID, roles, permissions).
-   **Secret**: A cryptographic key used to sign and verify the integrity of JWTs, ensuring they have not been tampered with.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`RefreshToken`**: This is the Aggregate Root. It represents a single refresh token issued to a user and is responsible for managing its entire lifecycle, including creation, expiration, and explicit revocation. It ensures the integrity and consistent state of the refresh token.
    -   **Entities**: None directly owned by `RefreshToken`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `TokenHash`, `ExpiresAt`, `CreatedByIp`, `RevokedAt`, `RevokedByIp`, `RevokedReason`, and `TokenFamily` are intrinsic attributes of the `RefreshToken` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `ApplicationUser` (from `Core.Domain.Identity.Users`): Referenced by `RefreshToken` to establish ownership, but `ApplicationUser` is managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`Jwt`**: This is a static class that serves as a collection of helper methods and constraints related to JSON Web Tokens. It encapsulates logic for JWT structure, validation rules, and potentially generation/parsing, but does not represent an entity or value object itself.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The static methods within `Jwt` (e.g., for token generation, validation, parsing) and `RefreshToken` (e.g., `GenerateRandomToken`, `Hash`) encapsulate domain-specific logic that could conceptually be part of a dedicated `TokenService` if the complexity warranted it. However, for now, they are tightly coupled to their respective token types.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Tokens` bounded context.

-   Refresh tokens must have a defined `lifetime` and an `ExpiresAt` timestamp, after which they are considered invalid.
-   Refresh tokens can be explicitly `Revoked` by the system or user, rendering them immediately unusable regardless of their `ExpiresAt` value.
-   A refresh token is considered invalid if its `IsExpired` or `IsRevoked` computed properties are true.
-   Raw refresh token values are never stored directly; instead, a cryptographically secure `TokenHash` is stored to protect against data breaches.
-   IP addresses (`CreatedByIp`, `RevokedByIp`) are recorded for auditing and to detect suspicious activity related to token usage.
-   Refresh tokens are associated with a `TokenFamily` to enable advanced security features like automatic invalidation of all previous tokens in a family upon successful token rotation.
-   JWTs must adhere to a specific, well-defined format (`header.payload.signature`) and require a cryptographic `Secret` for signing and verification to ensure authenticity and integrity.
-   JWTs should always contain an expiration claim (`exp`) to limit their validity period.

---

## 🤝 Relationships & Dependencies

-   **`RefreshToken` to `ApplicationUser`**: A many-to-one relationship. Each `RefreshToken` instance is owned by and associated with a single `ApplicationUser` (from the `Identity.Users` bounded context).
-   **Shared Kernel**: The `RefreshToken` aggregate inherits from `AuditableEntity<Guid>` and implements `IHasAssignable` (from `SharedKernel.Domain.Entities`), providing common auditing fields and assignment tracking capabilities. It also leverages `CommonInput.Constraints` (from `SharedKernel.Validations`) for consistent validation logic.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Refresh Token**: Generate a new, cryptographically secure `RefreshToken` for a given `ApplicationUser`, specifying its lifetime and recording the IP address of creation.
-   **Revoke Refresh Token**: Explicitly invalidate an active `RefreshToken`, recording the revocation timestamp, IP address, and an optional reason.
-   **Generate Random Token**: Produce cryptographically secure random strings suitable for use as raw refresh token values.
-   **Hash Token**: Compute a secure hash of a raw token string for storage and comparison.
-   **Check Token Status**: Determine if a `RefreshToken` is `IsExpired` or `IsRevoked`.
-   **(Conceptual for JWT)**: Generate JWTs with specific claims, validate the integrity and authenticity of received JWTs, and parse JWTs to extract their claims.

---

## 📝 Considerations / Notes

-   The `RefreshToken` aggregate focuses on the state and lifecycle of the refresh token itself, while the actual process of *issuing* new JWTs using a refresh token is typically handled by an application service or authentication provider.
-   The `Jwt` static class serves as a utility for JWT-related operations, keeping the domain model clean while providing necessary cryptographic and parsing functionalities.
-   The `TokenFamily` concept is a powerful security feature that helps mitigate replay attacks and ensures that only the most recently issued refresh token in a sequence remains valid.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   Security is paramount in this domain, with measures like token hashing, IP tracking, and explicit revocation built into the `RefreshToken`'s design.
