# Identity.Users.Roles Bounded Context

This document describes the `Identity.Users.Roles` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the explicit association between individual users and application roles. It establishes the many-to-many relationship that defines which roles a user belongs to, thereby determining their permissions and access levels within the system. This centralizes role assignment and ensures consistency in how user privileges are managed.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users.Roles` bounded context.

-   **User Role**: The explicit link between an `ApplicationUser` and an `ApplicationRole`. Represented by the `UserRole` entity.
-   **User**: The individual assigned to a role. (Referenced from `Identity.Users` Bounded Context).
-   **Role**: The application role assigned to a user. (Referenced from `Identity.Roles` Bounded Context).
-   **Assigned At / By / To**: Auditing fields tracking when, by whom, and to what the role was assigned.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `UserRole` is an entity that is owned by the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`UserRole`**: This is the central entity of this bounded context. It represents the explicit many-to-many relationship between `ApplicationUser` and `Role`, and inherits from `IdentityUserRole<string>` for ASP.NET Core Identity integration. It implements `IHasAssignable` for auditing purposes and `IHasDomainEvents` to publish events related to assignment.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `UserId`, `RoleId`, and the `IAssignable` properties are intrinsic attributes of the `UserRole` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users.Roles` bounded context.

-   A `UserRole` must always be associated with a valid `UserId` and `RoleId`.
-   A user cannot be assigned to more than a predefined maximum number of roles.
-   A role cannot be assigned to more than a predefined maximum number of users.
-   A specific role can only be assigned once to a user to prevent duplicates.
-   `UserRole` instances track their assignment details (`AssignedAt`, `AssignedBy`, `AssignedTo`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`UserRole` to `ApplicationUser`**: Many-to-one relationship. `UserRole` is owned by `ApplicationUser` (from `Identity.Users`).
-   **`UserRole` to `ApplicationRole`**: Many-to-one relationship. `UserRole` links to `ApplicationRole` (from `Identity.Roles`).
-   **Shared Kernel**: `UserRole` implements `IHasAssignable` and `IHasDomainEvents` (from `SharedKernel.Domain`), leveraging common patterns for tracking assignment and event publishing. It uses `ErrorOr` for a functional approach to error handling.
-   **ASP.NET Core Identity Integration**: Inherits from `IdentityUserRole<string>`, integrating seamlessly with the framework's user-role management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create User Role**: Establish a new link between a user and a role.
-   **Assign Role to User**: Associate an `ApplicationRole` with an `ApplicationUser`, subject to constraints like maximum roles per user.
-   **Unassign Role from User**: Disassociate an `ApplicationRole` from an `ApplicationUser`.
-   **Publish Domain Events**: Emit events (`Assigned`, `Unassigned`) to signal changes in user-role assignments, enabling a decoupled architecture.

---

## 📝 Considerations / Notes

-   `UserRole` acts as a child entity within the `ApplicationUser` aggregate, and its lifecycle is managed by the `ApplicationUser` aggregate or an application service coordinating with it.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   This domain is fundamental for implementing role-based access control (RBAC).
