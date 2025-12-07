# Identity.Users.Claims Bounded Context

This document describes the `Identity.Users.Claims` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the assignment of claims to individual users within the identity system. It allows for associating specific key-value pairs of information with users, which can then be used for fine-grained authorization policies, personalizing user experiences, or for integration with external systems that rely on claims-based security.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Users.Claims` bounded context.

-   **User Claim**: A specific key-value pair of information that is assigned to a <see cref="User"/>. Represented by the <see cref="UserClaim"/> entity.
-   **User**: The application user to whom the claim is assigned. (Referenced from `Identity.Users` Bounded Context).
-   **Claim Type**: The key or name of the claim (e.g., "permission", "favorite_color", "department"). Validated against constraints like <see cref="UserClaim.Constraints.MinClaimTypeLength"/> and <see cref="UserClaim.Constraints.MaxClaimTypeLength"/>.
-   **Claim Value**: The value associated with the <c>Claim Type</c> (e.g., "catalog.product.view", "blue", "Marketing"). Validated against constraints like <see cref="UserClaim.Constraints.MaxClaimValueLength"/>.
-   **Assigned At / By / To**: Auditing fields tracking when, by whom, and to what the claim was assigned, derived from the <see cref="IHasAssignable"/> concern.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `UserClaim` is an entity that is owned by the `ApplicationUser` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`UserClaim`**: This is the central entity of this bounded context. It represents a single claim associated with a user and inherits from <see cref="IdentityUserClaim{TKey}"/> for ASP.NET Core Identity integration. It implements <see cref="IHasAssignable"/> for auditing purposes.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>ClaimType</c>, <c>ClaimValue</c> (from <see cref="IdentityUserClaim{TKey}"/>), and the <see cref="IHasAssignable"/> properties (<c>AssignedAt</c>, <c>AssignedBy</c>, <c>AssignedTo</c>) are intrinsic attributes of the <see cref="UserClaim"/> entity.

### Value Objects (standalone, if any)

-   None.

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Users.Claims` bounded context.

-   A <see cref="UserClaim"/> must always be associated with a valid <c>UserId</c>, <c>ClaimType</c>, and <c>ClaimValue</c> (if applicable).
-   There is a maximum limit (<see cref="UserClaim.Constraints.MaxClaimsPerUser"/>) to the number of claims that can be assigned to a single user, enforced by <see cref="UserClaim.Errors.MaxClaimsExceeded"/>.
-   A specific <c>Claim Type</c> can only be assigned once to a user to prevent duplicates. Attempts to assign an already present claim type will result in <see cref="UserClaim.Errors.AlreadyAssigned(string)"/>.
-   <c>Claim Type</c> and <c>Claim Value</c> must adhere to predefined length constraints (<see cref="UserClaim.Constraints.MinClaimTypeLength"/>, <see cref="UserClaim.Constraints.MaxClaimTypeLength"/>, <see cref="UserClaim.Constraints.MaxClaimValueLength"/>).
-   <see cref="UserClaim"/> instances track their assignment details (<c>AssignedAt</c>, <c>AssignedBy</c>, <c>AssignedTo</c>), adhering to auditing requirements via the <see cref="IHasAssignable"/> concern.

---

## 🤝 Relationships & Dependencies

-   **`UserClaim` to `ApplicationUser`**: Many-to-one relationship. `UserClaim` is owned by `ApplicationUser` (from `Identity.Users`).
-   **Shared Kernel**: `UserClaim` implements `IHasAssignable` (from `SharedKernel.Domain.Concerns`), leveraging common patterns for tracking assignment. It uses `ErrorOr` for a functional approach to error handling.
-   **ASP.NET Core Identity Integration**: Inherits from `IdentityUserClaim<string>`, integrating seamlessly with the framework's claim management.

---

## 🚀 Key Use Cases / Behaviors

-   **Create User Claim**: Instantiate a new <see cref="UserClaim"/> using <see cref="UserClaim.Create(string, string, string?, string?)"/> for a user, specifying its type and value. This method performs initial validation and assignment tracking.
-   **Assign User Claim**: Explicitly assign a <see cref="UserClaim"/> to an <see cref="ApplicationUser"/> using <see cref="UserClaim.Assign(User, string, string?, string?)"/>. This method enforces constraints like maximum claims per user (<see cref="UserClaim.Constraints.MaxClaimsPerUser"/>) and uniqueness of claim types, returning appropriate errors (<see cref="UserClaim.Errors.MaxClaimsExceeded"/>, <see cref="UserClaim.Errors.AlreadyAssigned(string)"/>).
-   **Remove User Claim**: Disassociate an existing claim from an <see cref="ApplicationUser"/> using <see cref="UserClaim.Remove()"/>. This method marks the claim as unassigned. The actual removal from the user's collection is typically handled by the parent <see cref="ApplicationUser"/> aggregate.

---

## 📝 Considerations / Notes

-   `UserClaim` acts as a child entity within the `ApplicationUser` aggregate, and its lifecycle is managed by the `ApplicationUser` aggregate.
-   The use of `ErrorOr` for return types promotes a functional approach to error handling, making business rule violations explicit.
-   This domain is crucial for implementing personalized experiences and fine-grained access control based on user attributes.
