# Identity.Roles.Claims Bounded Context

This document describes the `Identity.Roles.Claims` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the assignment of claims to application roles within the identity system. It allows for associating specific key-value pairs of information with roles, which can then be used for fine-grained authorization policies, to convey additional context about a role's capabilities, or for integration with external systems that rely on claims-based security.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Roles.Claims` bounded context.

-   **Role Claim**: A specific key-value pair of information that is assigned to a <see cref="Role"/>. Represented by the <see cref="RoleClaim"/> entity.
-   **Role**: The application role to which the claim is assigned. (Referenced from `Identity.Roles` Bounded Context).
-   **Claim Type**: The key or name of the claim (e.g., "permission", "country", "department"). Validated against <see cref="RoleClaim.Constraints.ClaimTypePattern"/> and length constraints.
-   **Claim Value**: The value associated with the <c>Claim Type</c> (e.g., "admin.product.create", "USA", "Sales"). Validated against length constraints.
-   **Assigned At / By / To**: Auditing fields tracking when, by whom, and to what the claim was assigned, derived from the <see cref="IHasAssignable"/> concern.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `RoleClaim` is an entity that is owned by the `ApplicationRole` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`RoleClaim`**: This is the central entity of this bounded context. It represents a single claim associated with a role and inherits from <see cref="IdentityRoleClaim{TKey}"/> for ASP.NET Core Identity integration. It implements <see cref="IHasAssignable"/> for auditing purposes.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>ClaimType</c>, <c>ClaimValue</c> (from <see cref="IdentityRoleClaim{TKey}"/>), and the <see cref="IHasAssignable"/> properties (<c>AssignedAt</c>, <c>AssignedBy</c>, <c>AssignedTo</c>) are intrinsic attributes of the <see cref="RoleClaim"/> entity.

### Value Objects (standalone, if any)

-   None.

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Roles.Claims` bounded context.

-   A `RoleClaim` must always be associated with a valid `RoleId`, `ClaimType`, and `ClaimValue`.
-   There is a maximum limit to the number of claims that can be assigned to a single role.
-   A specific `Claim Type` can only be assigned once to a role to prevent duplicates.
-   `Claim Type` and `Claim Value` must adhere to predefined length constraints.
-   `Claim Type` must follow a specific pattern (`Constraints.ClaimTypePattern`).
-   `RoleClaim` instances track their assignment details (`AssignedAt`, `AssignedBy`, `AssignedTo`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`RoleClaim` to `ApplicationRole`**: Many-to-one relationship. `RoleClaim` is owned by `ApplicationRole` (from `Identity.Roles`).
-   **Shared Kernel**: `RoleClaim` implements `IHasAssignable` (from `SharedKernel.Domain.Concerns`), leveraging common patterns for tracking assignment. It uses `ErrorOr` for a functional approach to error handling.
-   **ASP.NET Core Identity Integration**: Inherits from `IdentityRoleClaim<string>`, integrating seamlessly with the framework's claim management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Role Claim**: Instantiate a new `RoleClaim` for a role, specifying its type and value.
-   **Assign Role Claim**: Associate a new claim with an `ApplicationRole`, subject to constraints like maximum claims per role and uniqueness.
-   **Remove Role Claim**: Disassociate an existing claim from an `ApplicationRole`.

---

## 📝 Considerations / Notes

-   `RoleClaim` acts as a child entity within the `ApplicationRole` aggregate, and its lifecycle is managed by the `ApplicationRole` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   This domain is crucial for implementing claims-based authorization, allowing flexible and extensible access control policies.
