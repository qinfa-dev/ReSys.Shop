# Identity.Users Bounded Context

This document describes the `Identity.Users` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the core user identity within the application, serving as the central aggregate for all user-related information and operations. It encompasses user registration, authentication, profile management, and robust associations with roles, claims, external logins, authentication tokens, refresh tokens, and user addresses. It integrates seamlessly with ASP.NET Core Identity to provide a secure, extensible, and comprehensive user management system.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users` bounded context.

-   **Application User**: A registered individual or entity interacting with the system. This is the central aggregate root of this bounded context.
-   **User Name**: A unique identifier for login purposes, often distinct from the user's email.
-   **Email**: The user's primary email address, frequently used for login, communication, and account recovery.
-   **Phone Number**: The user's contact phone number.
-   **First Name / Last Name**: The user's personal given and family names, forming their `FullName`.
-   **Date of Birth**: The user's birth date, used for age verification or personalized experiences.
-   **Profile Image Path**: The URL or file path to the user's profile picture or avatar.
-   **Email Confirmed / Phone Number Confirmed**: Boolean flags indicating whether the user's email address or phone number has been verified.
-   **Last Sign In At / Current Sign In At / IP**: Timestamps and IP addresses tracking the user's login activity, crucial for security auditing.
-   **Sign In Count**: A counter for the total number of times the user has successfully signed into the application.
-   **Refresh Token**: A long-lived token used to obtain new, short-lived access tokens without requiring re-authentication. (Owned by `ApplicationUser`).
-   **User Claim**: A piece of information (key-value pair) about the user, used for authorization and personalization. (Owned by `ApplicationUser`).
-   **User Login**: Information about external login providers (e.g., Google, Facebook) linked to the user's account. (Owned by `ApplicationUser`).
-   **User Token**: Generic tokens associated with the user, used for various purposes like password resets or email confirmations. (Owned by `ApplicationUser`).
-   **User Role**: The explicit association between an `ApplicationUser` and an `ApplicationRole` (from `Identity.Roles`), defining the user's permissions and access level. (Owned by `ApplicationUser`).
-   **User Address**: A physical address (shipping, billing) associated with the user. (Owned by `ApplicationUser`).
-   **Order**: A record of a user's purchase or transaction within the system. (Owned by `ApplicationUser`).
-   **Locked Out**: A state where a user's account is temporarily or permanently blocked from logging in due to security reasons (e.g., too many failed login attempts).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`ApplicationUser`**: This is the Aggregate Root. It extends `IdentityUser` from ASP.NET Core Identity, encapsulating all user-related properties, business logic, and managing its relationships with various owned entities. It is responsible for maintaining the consistency and integrity of the user's identity and associated data.
    -   **Entities**:
        -   `RefreshToken` (owned by `ApplicationUser`): Represents a refresh token issued to this user, managing its lifecycle (creation, revocation).
        -   `ApplicationUserClaim` (owned by `ApplicationUser`): Represents claims (key-value pairs) associated with this user, used for authorization and personalization.
        -   `ApplicationUserLogin` (owned by `ApplicationUser`): Stores information about external login providers (e.g., Google, Facebook) linked to the user's account.
        -   `ApplicationUserToken` (owned by `ApplicationUser`): Stores generic tokens for this user, such as those used for password resets or email confirmations.
        -   `ApplicationUserRole` (owned by `ApplicationUser`): Represents the many-to-many relationship between this user and an `ApplicationRole` (from `Identity.Roles`), defining the user's membership in roles.
        -   `UserAddress` (owned by `ApplicationUser`): Represents a physical address (shipping, billing) associated with this user.
        -   `Order` (owned by `ApplicationUser`): Represents an order placed by this user, linking the user to their purchase history.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `FirstName`, `LastName`, `DateOfBirth`, `ProfileImagePath`, `LastSignInAt`, `CurrentSignInAt`, `SignInCount` are intrinsic attributes of the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `ApplicationRole` (from `Core.Domain.Identity.Roles`): Referenced by `ApplicationUserRole`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All user-related business logic is encapsulated within the `ApplicationUser` aggregate itself, or handled by the underlying ASP.NET Core Identity framework.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users` bounded context.

-   Usernames and email addresses must be unique across the system to ensure distinct user identities.
-   Email and phone numbers can be confirmed, indicating their verification status.
-   User accounts can be locked out, preventing login for a specified period or indefinitely.
-   A user cannot be deleted if they have active (unrevoked) refresh tokens. (Enforced by `ApplicationUser.Delete()`)
-   A user cannot be deleted if they have assigned roles. (Enforced by `ApplicationUser.Delete()`)
-   `ApplicationUser` instances track their creation and update timestamps, and the user who performed these actions (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`), adhering to auditing requirements.
-   `ApplicationUser` includes a `Version` property (`IHasVersion`) to support optimistic concurrency control, preventing conflicting updates.
-   Constraints are applied to credential lengths (e.g., username, email, phone number) to ensure data integrity and prevent overflow.
-   User sign-in activity (timestamps, IP addresses, count) is meticulously recorded for security and analytics.
-   Users can designate default billing and shipping addresses from their collection of `UserAddress`es.

---

## 🤝 Relationships & Dependencies

-   **`ApplicationUser` to `RefreshToken`**: One-to-many composition. `ApplicationUser` is the aggregate root for its `RefreshToken`s.
-   **`ApplicationUser` to `ApplicationUserClaim`**: One-to-many composition. `ApplicationUser` is the aggregate root for its `ApplicationUserClaim`s.
-   **`ApplicationUser` to `ApplicationUserLogin`**: One-to-many composition. `ApplicationUser` is the aggregate root for its `ApplicationUserLogin`s.
-   **`ApplicationUser` to `ApplicationUserToken`**: One-to-many composition. `ApplicationUser` is the aggregate root for its `ApplicationUserToken`s.
-   **`ApplicationUser` to `ApplicationUserRole`**: One-to-many composition. `ApplicationUser` is the aggregate root for its `ApplicationUserRole`s, which link to `ApplicationRole` (from `Identity.Roles`).
-   **`ApplicationUser` to `UserAddress`**: One-to-many composition. `ApplicationUser` is the aggregate root for its `UserAddress`es.
-   **`ApplicationUser` to `Order`**: One-to-many composition. `ApplicationUser` is the aggregate root for its `Order`s.
-   **External Aggregates**: References `ApplicationRole` (from `Identity.Roles`) for role management.
-   **ASP.NET Core Identity Integration**: The `ApplicationUser` aggregate inherits from `Microsoft.AspNetCore.Identity.IdentityUser`, providing seamless integration with the ASP.NET Core Identity framework for core identity functionalities.
-   **Shared Kernel**: Implements `IHasVersion`, `IHasDomainEvents`, and `IHasAuditable` from `SharedKernel.Domain.Attributes` and `SharedKernel.Domain.Events`, leveraging common patterns for versioning, domain event publishing, and auditing.

---

## 🚀 Key Use Cases / Behaviors

-   **Register New User**: Create a new `ApplicationUser` account with essential details like email, username, and optional profile information.
-   **Update User Profile**: Modify personal details such as `FirstName`, `LastName`, `DateOfBirth`, and `ProfileImagePath`.
-   **Update Contact Information**: Change the user's `Email` or `PhoneNumber`, triggering re-confirmation processes.
-   **Confirm Email/Phone Number**: Mark the user's email or phone number as verified.
-   **Record Sign-In Activity**: Update `LastSignInAt`, `CurrentSignInAt`, `CurrentSignInIp`, and `SignInCount` upon successful user login.
-   **Manage Account Lockout**: Lock or unlock a user's account, preventing or restoring login access.
-   **Delete User Account**: Remove an `ApplicationUser` from the system, with built-in checks to prevent deletion if active tokens or roles are associated.
-   **Manage User Addresses**: Add, update, or remove `UserAddress`es associated with the user.
-   **Manage User Roles and Claims**: Associate or disassociate `ApplicationRole`s and `ApplicationUserClaim`s with the user.
-   **Publish Domain Events**: Emit domain events (`UserCreated`, `UserUpdated`, `EmailChanged`, `AccountLocked`, etc.) to signal significant state changes in the user's lifecycle, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `ApplicationUser` aggregate is a highly central and complex aggregate root, orchestrating a significant portion of the application's identity and user-related data.
-   The integration with ASP.NET Core Identity means that many underlying identity operations (e.g., password hashing, token generation for password reset) are handled by the framework, while this aggregate focuses on the domain-specific business logic and invariants of the user.
-   Domain Events are extensively used to communicate changes in user state to other parts of the system, promoting loose coupling and extensibility.
-   The owned entities (`RefreshToken`, `ApplicationUserClaim`, etc.) are managed directly by the `ApplicationUser` aggregate, ensuring their lifecycle and consistency are tied to the user.
-   Security and data integrity are paramount, with built-in checks for deletion, unique identifiers, and tracking of sensitive activities.