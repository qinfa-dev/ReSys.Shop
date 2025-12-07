# Identity.Users.Logins Bounded Context

This document describes the `Identity.Users.Logins` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the association of external login providers (such as Google, Facebook, or other OAuth providers) with user accounts. It enables users to authenticate and sign in to the application using their credentials from these external services, thereby simplifying the registration and login process and enhancing user convenience.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users.Logins` bounded context.

-   **User Login**: A record linking a user's account to an external authentication provider. Represented by the <see cref="UserLogin"/> entity.
-   **User**: The application user who is linked to the external login. (Referenced from `Identity.Users` Bounded Context).
-   **Login Provider**: The name of the external service (e.g., "Google", "Facebook", "Microsoft Account") that authenticated the user (<see cref="IdentityUserLogin{TKey}.LoginProvider"/>).
-   **Provider Key**: A unique identifier for the user obtained from the external <c>Login Provider</c> (<see cref="IdentityUserLogin{TKey}.ProviderKey"/>). This key is specific to the external service.
-   **Provider Display Name**: A user-friendly name for the <c>Login Provider</c>, often shown in the UI (<see cref="IdentityUserLogin{TKey}.ProviderDisplayName"/>).

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `UserLogin` is an entity that is owned by the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`UserLogin`**: This is the central entity of this bounded context. It represents a single external login associated with a user and inherits from <see cref="IdentityUserLogin{TKey}"/> for ASP.NET Core Identity integration.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>LoginProvider</c>, <c>ProviderKey</c>, <c>ProviderDisplayName</c>, and <c>UserId</c> (from <see cref="IdentityUserLogin{TKey}"/>) are intrinsic attributes of the <see cref="UserLogin"/> entity.

### Value Objects (standalone, if any)

-   None.

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users.Logins` bounded context.

-   A <see cref="UserLogin"/> must always be associated with a valid <c>UserId</c> (<see cref="IdentityUserLogin{TKey}.UserId"/>).
-   The combination of <c>Login Provider</c> (<see cref="IdentityUserLogin{TKey}.LoginProvider"/>) and <c>Provider Key</c> (<see cref="IdentityUserLogin{TKey}.ProviderKey"/>) must be unique across all <see cref="UserLogin"/> entries, ensuring that a single external identity maps to a single user account.

---

## 🤝 Relationships & Dependencies

-   **`UserLogin` to `ApplicationUser`**: Many-to-one relationship. `UserLogin` is owned by `ApplicationUser` (from `Identity.Users`).
-   **ASP.NET Core Identity Integration**: Inherits from `IdentityUserLogin<string>`, integrating seamlessly with the framework's external login management.

---

## 🚀 Key Use Cases / Behaviors

-   **Add External Login**: Link an external authentication provider's credentials to an existing user account. This is typically orchestrated by an application service that leverages ASP.NET Core Identity's <c>UserManager</c> and <c>SignInManager</c> to add a new <see cref="UserLogin"/> record.
-   **Remove External Login**: Disassociate an external login from a user account. This is also typically managed by an application service interacting with ASP.NET Core Identity.
-   **Retrieve User by External Login**: Find a user account based on their external login details (i.e., <c>LoginProvider</c> and <c>ProviderKey</c>). ASP.NET Core Identity provides mechanisms for this lookup.

---

## 📝 Considerations / Notes

-   `UserLogin` acts as a child entity within the `ApplicationUser` aggregate, and its lifecycle is managed by the `ApplicationUser` aggregate or by the ASP.NET Core Identity framework.
-   This domain is fundamental for supporting "Login with Google", "Login with Facebook", and similar single sign-on (SSO) features.
-   The actual authentication flow with external providers is handled by the ASP.NET Core Identity middleware and external authentication handlers, while this domain focuses on representing the persistent link.
