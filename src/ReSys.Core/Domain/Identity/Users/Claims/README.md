# Identity.Users.Claims Bounded Context

This document describes the `Identity.Users.Claims` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the assignment of claims to individual users within the identity system. It allows for associating specific key-value pairs of information with users, which can then be used for fine-grained authorization policies, personalizing user experiences, or for integration with external systems that rely on claims-based security.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users.Claims` bounded context.

-   **User Claim**: A specific key-value pair of information that is assigned to a `User`. Represented by the `UserClaim` entity.
-   **User**: The application user to whom the claim is assigned. (Referenced from `Identity.Users` Bounded Context).
-   **Claim Type**: The key or name of the claim (e.g., "permission", "favorite_color", "department").
-   **Claim Value**: The value associated with the `Claim Type` (e.g., "catalog.product.view", "blue", "Marketing").
-   **Assigned At / By / To**: Auditing fields tracking when, by whom, and to what the claim was assigned.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `UserClaim` is an entity that is owned by the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`UserClaim`**: This is the central entity of this bounded context. It represents a single claim associated with a user and inherits from `IdentityUserClaim<string>` for ASP.NET Core Identity integration. It implements `IHasAssignable` for auditing purposes.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `ClaimType`, `ClaimValue`, and the `IAssignable` properties are intrinsic attributes of the `UserClaim` entity.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users.Claims` bounded context.

-   A `UserClaim` must always be associated with a valid `UserId`, `ClaimType`, and `ClaimValue`.
-   There is a maximum limit to the number of claims that can be assigned to a single user.
-   A specific `Claim Type` can only be assigned once to a user to prevent duplicates.
-   `Claim Type` and `Claim Value` must adhere to predefined length constraints.
-   `UserClaim` instances track their assignment details (`AssignedAt`, `AssignedBy`, `AssignedTo`), adhering to auditing requirements.

---

## 🤝 Relationships & Dependencies

-   **`UserClaim` to `ApplicationUser`**: Many-to-one relationship. `UserClaim` is owned by `ApplicationUser` (from `Identity.Users`).
-   **Shared Kernel**: `UserClaim` implements `IHasAssignable` (from `SharedKernel.Domain.Concerns`), leveraging common patterns for tracking assignment. It uses `ErrorOr` for a functional approach to error handling.
-   **ASP.NET Core Identity Integration**: Inherits from `IdentityUserClaim<string>`, integrating seamlessly with the framework's claim management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create User Claim**: Instantiate a new `UserClaim` for a user, specifying its type and value.
-   **Assign User Claim**: Associate a new claim with an `ApplicationUser`, subject to constraints like maximum claims per user and uniqueness.
-   **Remove User Claim**: Disassociate an existing claim from an `ApplicationUser`.

---

## 📝 Considerations / Notes

-   `UserClaim` acts as a child entity within the `ApplicationUser` aggregate, and its lifecycle is managed by the `ApplicationUser` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   This domain is crucial for implementing personalized experiences and fine-grained access control based on user attributes.
