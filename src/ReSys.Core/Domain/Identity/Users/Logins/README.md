# Identity.Users.Logins Bounded Context

This document describes the `Identity.Users.Logins` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the association of external login providers (such as Google, Facebook, or other OAuth providers) with user accounts. It enables users to authenticate and sign in to the application using their credentials from these external services, thereby simplifying the registration and login process and enhancing user convenience.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users.Logins` bounded context.

-   **User Login**: A record linking a user's account to an external authentication provider. Represented by the `UserLogin` entity.
-   **User**: The application user who is linked to the external login. (Referenced from `Identity.Users` Bounded Context).
-   **Login Provider**: The name of the external service (e.g., "Google", "Facebook", "Microsoft Account") that authenticated the user.
-   **Provider Key**: A unique identifier for the user obtained from the external `Login Provider`. This key is specific to the external service.
-   **Provider Display Name**: A user-friendly name for the `Login Provider`, often shown in the UI.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `UserLogin` is an entity that is owned by the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`UserLogin`**: This is the central entity of this bounded context. It represents a single external login associated with a user and inherits from `IdentityUserLogin<string>` for ASP.NET Core Identity integration.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `LoginProvider`, `ProviderKey`, `ProviderDisplayName`, and `UserId` are intrinsic attributes of the `UserLogin` entity, inherited or directly managed.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users.Logins` bounded context.

-   A `UserLogin` must always be associated with a valid `UserId`.
-   The combination of `Login Provider` and `Provider Key` must be unique across all `UserLogin` entries, ensuring that a single external identity maps to a single user account.

---

## 🤝 Relationships & Dependencies

-   **`UserLogin` to `ApplicationUser`**: Many-to-one relationship. `UserLogin` is owned by `ApplicationUser` (from `Identity.Users`).
-   **ASP.NET Core Identity Integration**: Inherits from `IdentityUserLogin<string>`, integrating seamlessly with the framework's external login management.

---

## 🚀 Key Use Cases / Behaviors

-   **Add External Login**: Link an external authentication provider's credentials to an existing user account.
-   **Remove External Login**: Disassociate an external login from a user account.
-   **Retrieve User by External Login**: Find a user account based on their external login details.

---

## 📝 Considerations / Notes

-   `UserLogin` acts as a child entity within the `ApplicationUser` aggregate, and its lifecycle is managed by the `ApplicationUser` aggregate or by the ASP.NET Core Identity framework.
-   This domain is fundamental for supporting "Login with Google", "Login with Facebook", and similar single sign-on (SSO) features.
-   The actual authentication flow with external providers is handled by the ASP.NET Core Identity middleware and external authentication handlers, while this domain focuses on representing the persistent link.
