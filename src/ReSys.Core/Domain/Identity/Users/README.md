# Identity.Users Bounded Context

This document describes the `Identity.Users` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the core user identity within the application, serving as the central aggregate for all user-related information and operations. It encompasses user registration, authentication, profile management, and robust associations with roles, claims, external logins, authentication tokens, refresh tokens, and user addresses. It integrates seamlessly with ASP.NET Core Identity to provide a secure, extensible, and comprehensive user management system.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users` bounded context.

-   **Application User**: A registered individual or entity interacting with the system. This is the central <see cref="User"/> aggregate root of this bounded context.
-   **User Name**: A unique identifier for login purposes, often distinct from the user's email (<see cref="User.UserName"/>).
-   **Email**: The user's primary email address, frequently used for login, communication, and account recovery (<see cref="User.Email"/>).
-   **Phone Number**: The user's contact phone number (<see cref="User.PhoneNumber"/>).
-   **First Name / Last Name**: The user's personal given and family names, forming their <see cref="User.FullName"/> (<see cref="User.FirstName"/>, <see cref="User.LastName"/>).
-   **Date of Birth**: The user's birth date, used for age verification or personalized experiences (<see cref="User.DateOfBirth"/>).
-   **Profile Image Path**: The URL or file path to the user's profile picture or avatar (<see cref="User.ProfileImagePath"/>).
-   **Email Confirmed / Phone Number Confirmed**: Boolean flags indicating whether the user's email address or phone number has been verified (<see cref="User.EmailConfirmed"/>, <see cref="User.PhoneNumberConfirmed"/>).
-   **Last Sign In At / Current Sign In At / IP**: Timestamps and IP addresses tracking the user's login activity, crucial for security auditing (<see cref="User.LastSignInAt"/>, <see cref="User.CurrentSignInAt"/>, <see cref="User.LastSignInIp"/>, <see cref="User.CurrentSignInIp"/>).
-   **Sign In Count**: A counter for the total number of times the user has successfully signed into the application (<see cref="User.SignInCount"/>).
-   **Refresh Token**: A long-lived token used to obtain new, short-lived access tokens without requiring re-authentication. (Owned by <see cref="User"/>).
-   **User Claim**: A piece of information (key-value pair) about the user, used for authorization and personalization. (Owned by <see cref="User"/>).
-   **User Login**: Information about external login providers (e.g., Google, Facebook) linked to the user's account. (Owned by <see cref="User"/>).
-   **User Token**: Generic tokens associated with the user, used for various purposes like password resets or email confirmations. (Owned by <see cref="User"/>).
-   **User Role**: The explicit association between an <see cref="User"/> and an <see cref="Role"/> (from `Identity.Roles`), defining the user's permissions and access level. (Owned by <see cref="User"/>).
-   **User Address**: A physical address (shipping, billing) associated with the user. (Owned by <see cref="User"/>).
-   **Order**: A record of a user's purchase or transaction within the system. (Owned by <see cref="User"/>).
-   **Locked Out**: A state where a user's account is temporarily or permanently blocked from logging in due to security reasons (e.g., too many failed login attempts) (<see cref="User.LockoutEnabled"/>, <see cref="User.LockoutEnd"/>).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`ApplicationUser`**: This is the Aggregate Root. It extends <see cref="IdentityUser"/> from ASP.NET Core Identity, encapsulating all user-related properties, business logic, and managing its relationships with various owned entities. It is responsible for maintaining the consistency and integrity of the user's identity and associated data.
    -   **Entities**:
        -   <see cref="RefreshToken"/> (owned by <see cref="User"/>): Represents a refresh token issued to this user, managing its lifecycle (creation, revocation).
        -   <see cref="UserClaim"/> (owned by <see cref="User"/>): Represents claims (key-value pairs) associated with this user, used for authorization and personalization.
        -   <see cref="UserLogin"/> (owned by <see cref="User"/>): Stores information about external login providers (e.g., Google, Facebook) linked to the user's account.
        -   <see cref="UserToken"/> (owned by <see cref="User"/>): Stores generic tokens for this user, such as those used for password resets or email confirmations.
        -   <see cref="UserRole"/> (owned by <see cref="User"/>): Represents the many-to-many relationship between this user and an <see cref="Role"/> (from `Identity.Roles`), defining the user's membership in roles.
        -   <see cref="UserAddress"/> (owned by <see cref="User"/>): Represents a physical address (shipping, billing) associated with this user.
        -   <c>Order</c> (owned by <see cref="User"/>): Represents an order placed by this user, linking the user to their purchase history.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>FirstName</c>, <c>LastName</c>, <c>DateOfBirth</c>, <c>ProfileImagePath</c>, <c>LastSignInAt</c>, <c>CurrentSignInAt</c>, <c>SignInCount</c> (from <see cref="User"/>), and auditing/versioning properties (<c>CreatedAt</c>, <c>UpdatedAt</c>, <c>CreatedBy</c>, <c>UpdatedBy</c>, <c>Version</c>) are intrinsic attributes of the <see cref="User"/> aggregate.

### Entities (not part of an Aggregate Root, if any)

-   <see cref="Role"/> (from `Core.Domain.Identity.Roles`): Referenced by <see cref="UserRole"/>, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All user-related business logic is encapsulated within the `ApplicationUser` aggregate itself, or handled by the underlying ASP.NET Core Identity framework.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users` bounded context.

-   Usernames (<c>UserName</c>) and email addresses (<c>Email</c>) must be unique across the system to ensure distinct user identities. This is typically enforced by the underlying ASP.NET Core Identity system.
-   Email and phone numbers can be confirmed, indicating their verification status (<see cref="User.EmailConfirmed"/>, <see cref="User.PhoneNumberConfirmed"/>).
-   User accounts can be locked out, preventing login for a specified period or indefinitely (<see cref="User.LockoutEnabled"/>, <see cref="User.LockoutEnd"/>).
-   A user cannot be deleted if they have active (unrevoked) refresh tokens. The <see cref="User.Delete()"/> method enforces this by returning <see cref="User.Errors.HasActiveTokens"/>.
-   A user cannot be deleted if they have assigned roles. The <see cref="User.Delete()"/> method enforces this by returning <see cref="User.Errors.HasActiveRoles"/>.
-   <see cref="User"/> instances track their creation and update timestamps, and the user who performed these actions (<c>CreatedAt</c>, <c>UpdatedAt</c>, <c>CreatedBy</c>, <c>UpdatedBy</c>), adhering to auditing requirements via <see cref="IHasAuditable"/>.
-   <see cref="User"/> includes a <c>Version</c> property (<see cref="IHasVersion"/>) to support optimistic concurrency control, preventing conflicting updates.
-   Constraints are applied to credential lengths (e.g., username, email, phone number) to ensure data integrity and prevent overflow (<see cref="User.Constraints.MinCredentialLength"/>, <see cref="User.Constraints.MaxCredentialLength"/>).
-   User sign-in activity (timestamps, IP addresses, count) is meticulously recorded for security and analytics (<see cref="User.LastSignInAt"/>, <see cref="User.CurrentSignInAt"/>, <see cref="User.SignInCount"/>, etc., updated by <see cref="User.RecordSignIn(string?)"/>).
-   Users can designate default billing and shipping addresses from their collection of <see cref="UserAddress"/>es (<see cref="User.DefaultBillingAddress"/>, <see cref="User.DefaultShippingAddress"/>).

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

-   **Register New User**: Create a new <see cref="User"/> account using <see cref="User.Create(string?, string?, string?, string?, DateTimeOffset?, string?, string?, bool, bool)"/> with essential details like email, username, and optional profile information.
-   **Update User Profile**: Modify personal details such as <c>FirstName</c>, <c>LastName</c>, <c>DateOfBirth</c>, and <c>ProfileImagePath</c> using <see cref="User.UpdateProfile(string?, string?, DateTimeOffset?, string?)"/>. A more comprehensive update can be done via <see cref="User.Update(string?, string?, string?, string?, DateTimeOffset?, string?, string?, bool, bool)"/>.
-   **Update Contact Information**: Change the user's <c>Email</c> using <see cref="User.UpdateEmail(string)"/> or <c>PhoneNumber</c> using <see cref="User.UpdatePhoneNumber(string)"/>, triggering re-confirmation processes.
-   **Confirm Email/Phone Number**: Mark the user's email as verified using <see cref="User.ConfirmEmail()"/> or phone number as verified using <see cref="User.ConfirmPhoneNumber()"/>.
-   **Record Sign-In Activity**: Update sign-in related timestamps and increment the sign-in count using <see cref="User.RecordSignIn(string?)"/> upon successful user login.
-   **Manage Account Lockout**: Lock a user's account using <see cref="User.LockAccount(DateTimeOffset?)"/> or unlock it using <see cref="User.UnlockAccount()"/>, preventing or restoring login access.
-   **Delete User Account**: Remove an <see cref="User"/> from the system using <see cref="User.Delete()"/>, with built-in checks to prevent deletion if active tokens or roles are associated.
-   **Manage User Addresses**: Add new <see cref="UserAddress"/>es to the user's collection using <see cref="User.AddAddress(UserAddress)"/>. Individual address updates are handled by the <see cref="UserAddress"/> aggregate.
-   **Manage User Roles and Claims**: Associate or disassociate <see cref="Role"/>s and <see cref="UserClaim"/>s with the user. (Specific methods are in <see cref="UserRole"/> and <see cref="UserClaim"/> entities, orchestrated by the <see cref="User"/> aggregate or application services).
-   **Publish Domain Events**: <see cref="User"/> publishes various events (e.g., <see cref="User.Events.UserCreated"/>, <see cref="User.Events.UserUpdated"/>, <see cref="User.Events.EmailChanged"/>, <see cref="User.Events.AccountLocked"/>) to signal significant state changes in the user's lifecycle, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   The `ApplicationUser` aggregate is a highly central and complex aggregate root, orchestrating a significant portion of the application's identity and user-related data.
-   The integration with ASP.NET Core Identity means that many underlying identity operations (e.g., password hashing, token generation for password reset) are handled by the framework, while this aggregate focuses on the domain-specific business logic and invariants of the user.
-   Domain Events are extensively used to communicate changes in user state to other parts of the system, promoting loose coupling and extensibility.
-   The owned entities (`RefreshToken`, `ApplicationUserClaim`, etc.) are managed directly by the `ApplicationUser` aggregate, ensuring their lifecycle and consistency are tied to the user.
-   Security and data integrity are paramount, with built-in checks for deletion, unique identifiers, and tracking of sensitive activities.