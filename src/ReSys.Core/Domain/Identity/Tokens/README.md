# Identity.Tokens Bounded Context

This document describes the `Identity.Tokens` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the generation, validation, and lifecycle of authentication tokens, specifically JSON Web Tokens (JWTs) for access and Refresh Tokens for session management. It provides the foundational mechanisms for secure user authentication, authorization, and maintaining user sessions without requiring frequent re-authentication, thereby enhancing both security and user experience.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Tokens` bounded context.

-   **JSON Web Token (JWT)**: A compact, URL-safe means of representing claims to be transferred between two parties. JWTs are typically short-lived and used for access control to protected resources. Constraints and errors are defined in the static class <see cref="Jwt"/>.
-   **Refresh Token**: A long-lived, cryptographically secure token used to obtain new, short-lived JWTs without requiring the user to re-enter their credentials. It is central to maintaining persistent user sessions. Represented by the <see cref="RefreshToken"/> entity.
-   **Token Hash**: A cryptographic hash of the raw refresh token string, stored in the database instead of the raw token itself to enhance security against data breaches. (<see cref="RefreshToken.TokenHash"/>).
-   **Expires At**: The precise UTC timestamp when a token (either JWT or <see cref="RefreshToken"/>) becomes invalid and can no longer be used (<see cref="RefreshToken.ExpiresAt"/>).
-   **Revoked At**: The UTC timestamp when a <see cref="RefreshToken"/> was explicitly invalidated by the system or user, rendering it unusable before its natural expiration (<see cref="RefreshToken.RevokedAt"/>).
-   **Created By IP / Revoked By IP**: The IP addresses associated with the creation (<see cref="RefreshToken.CreatedByIp"/>) or revocation (<see cref="RefreshToken.RevokedByIp"/>) of a <see cref="RefreshToken"/>, used for auditing and security analysis.
-   **Token Family**: A mechanism to group related <see cref="RefreshToken"/>s (e.g., when a <see cref="RefreshToken"/> is rotated, the new token belongs to the same family as the old one). This enables strategies like immediate invalidation of all old tokens in a family upon rotation (<see cref="RefreshToken.TokenFamily"/>).
-   **Is Expired**: A computed property indicating whether a token's <c>ExpiresAt</c> timestamp is in the past (<see cref="RefreshToken.IsExpired"/>).
-   **Is Revoked**: A computed property indicating whether a <see cref="RefreshToken"/> has been explicitly invalidated (<see cref="RefreshToken.IsRevoked"/>).
-   **Claims**: Key-value pairs of information (assertions) about a subject (typically a user) that are encoded within a JWT (e.g., user ID, roles, permissions).
-   **Secret**: A cryptographic key used to sign and verify the integrity of JWTs, ensuring they have not been tampered with.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`RefreshToken`**: This is the Aggregate Root. It represents a single refresh token issued to a user and is responsible for managing its entire lifecycle, including creation, expiration, and explicit revocation. It ensures the integrity and consistent state of the refresh token.
    -   **Entities**: None directly owned by <see cref="RefreshToken"/>.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>UserId</c>, <c>TokenHash</c>, <c>ExpiresAt</c>, <c>CreatedByIp</c>, <c>RevokedAt</c>, <c>RevokedByIp</c>, <c>RevokedReason</c>, and <c>TokenFamily</c> are intrinsic attributes of the <see cref="RefreshToken"/> aggregate. Auditing properties (<c>CreatedAt</c>, <c>AssignedAt</c>, <c>AssignedBy</c>, <c>AssignedTo</c>) are inherited via <see cref="AuditableEntity"/> and <see cref="IHasAssignable"/>.

### Entities (not part of an Aggregate Root, if any)

-   <see cref="ApplicationUser"/> (from `Core.Domain.Identity.Users`): Referenced by <see cref="RefreshToken"/> to establish ownership, but <see cref="ApplicationUser"/> is managed by its own aggregate.

### Value Objects (standalone, if any)

-   **`Jwt`**: This is a static class that serves as a collection of helper methods and constraints related to JSON Web Tokens. It encapsulates logic for JWT structure, validation rules, and potentially generation/parsing, but does not represent an entity or value object itself.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. However, static helper methods within <see cref="Jwt"/> (e.g., for token generation, validation, parsing, though not directly implemented in the <see cref="Jwt"/> static class itself, it represents the concept) and <see cref="RefreshToken"/> (e.g., <see cref="RefreshToken.GenerateRandomToken()"/>, <see cref="RefreshToken.Hash(string)"/>) encapsulate domain-specific logic. This design keeps the domain model clean while providing necessary cryptographic and parsing functionalities.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Tokens` bounded context.

-   <see cref="RefreshToken"/>s must have a defined <c>lifetime</c> and an <c>ExpiresAt</c> timestamp, after which they are considered invalid (<see cref="RefreshToken.IsExpired"/>).
-   <see cref="RefreshToken"/>s can be explicitly <c>Revoked</c> by the system or user using <see cref="RefreshToken.Revoke(string, string?)"/>, rendering them immediately unusable regardless of their <c>ExpiresAt</c> value (<see cref="RefreshToken.IsRevoked"/>).
-   Raw refresh token values are never stored directly; instead, a cryptographically secure <c>TokenHash</c> is stored to protect against data breaches. The <see cref="RefreshToken.Hash(string)"/> method is used for this purpose.
-   IP addresses (<c>CreatedByIp</c>, <c>RevokedByIp</c>) are recorded for auditing and to detect suspicious activity related to token usage.
-   <see cref="RefreshToken"/>s are associated with a <c>TokenFamily</c> to enable advanced security features like automatic invalidation of all previous tokens in a family upon successful token rotation.
-   JWTs must adhere to a specific, well-defined format (<c>header.payload.signature</c>) and require a cryptographic <c>Secret</c> for signing and verification to ensure authenticity and integrity. <see cref="Jwt.Constraints.TokenPattern"/> defines the expected format.
-   JWTs should always contain an expiration claim (<c>exp</c>) to limit their validity period (<see cref="Jwt.Errors.NoExpiration"/>).

---

## 🤝 Relationships & Dependencies

-   **`RefreshToken` to `ApplicationUser`**: A many-to-one relationship. Each `RefreshToken` instance is owned by and associated with a single `ApplicationUser` (from the `Identity.Users` bounded context).
-   **Shared Kernel**: The `RefreshToken` aggregate inherits from `AuditableEntity<Guid>` and implements `IHasAssignable` (from `SharedKernel.Domain.Entities`), providing common auditing fields and assignment tracking capabilities. It also leverages `CommonInput.Constraints` (from `SharedKernel.Validations`) for consistent validation logic.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Refresh Token**: Generate a new, cryptographically secure <see cref="RefreshToken"/> for a given <see cref="ApplicationUser"/> using <see cref="RefreshToken.Create(User, string, TimeSpan, string, string?, string?)"/>. This method specifies its lifetime, records the IP address of creation, and hashes the token for secure storage.
-   **Revoke Refresh Token**: Explicitly invalidate an active <see cref="RefreshToken"/> using <see cref="RefreshToken.Revoke(string, string?)"/>. This method records the revocation timestamp, IP address, and an optional reason.
-   **Generate Random Token**: Produce cryptographically secure random strings suitable for use as raw refresh token values via <see cref="RefreshToken.GenerateRandomToken()"/>.
-   **Hash Token**: Compute a secure SHA512 hash of a raw token string for storage and comparison using <see cref="RefreshToken.Hash(string)"/>.
-   **Check Token Status**: Determine if a <see cref="RefreshToken"/> is expired (<see cref="RefreshToken.IsExpired"/>) or revoked (<see cref="RefreshToken.IsRevoked"/>) using its computed properties.
-   **(Conceptual for JWT)**: While not directly part of the <see cref="Jwt"/> static class, the domain concept supports generating JWTs with specific claims, validating the integrity and authenticity of received JWTs, and parsing JWTs to extract their claims. These operations would be handled by an application service using the definitions from this domain.

---

## 📝 Considerations / Notes

-   The `RefreshToken` aggregate focuses on the state and lifecycle of the refresh token itself, while the actual process of *issuing* new JWTs using a refresh token is typically handled by an application service or authentication provider.
-   The `Jwt` static class serves as a utility for JWT-related operations, keeping the domain model clean while providing necessary cryptographic and parsing functionalities.
-   The `TokenFamily` concept is a powerful security feature that helps mitigate replay attacks and ensures that only the most recently issued refresh token in a sequence remains valid.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit and easier to manage.
-   Security is paramount in this domain, with measures like token hashing, IP tracking, and explicit revocation built into the `RefreshToken`'s design.
