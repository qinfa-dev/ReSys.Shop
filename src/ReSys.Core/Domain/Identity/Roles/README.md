# Identity.Roles Bounded Context

This document describes the `Identity.Roles` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the definition and lifecycle of application roles, which are fundamental for grouping users and assigning permissions within the system. It integrates seamlessly with ASP.NET Core Identity, providing robust mechanisms for creating, updating, and deleting roles, while enforcing critical business rules related to system-defined and default roles to maintain system integrity and security.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Roles` bounded context.

-   **Application Role**: A named collection of permissions that can be assigned to users, simplifying access control management. It represents a specific set of responsibilities or privileges within the application. Represented by the `ApplicationRole` aggregate.
-   **Role Name**: The unique, normalized identifier for a role (e.g., "ADMINISTRATOR", "CUSTOMER"). This is typically used for internal system logic and lookups.
-   **Display Name**: A human-readable name for the role (e.g., "Administrator", "Customer"), used for presentation in user interfaces.
-   **Description**: A detailed explanation of the role's purpose, responsibilities, or the permissions it encompasses.
-   **Is Default**: A boolean flag indicating if this role is automatically assigned to new users upon registration or creation.
-   **Priority**: An integer value indicating the precedence or order of roles, useful for display or conflict resolution in complex permission scenarios.
-   **Is System Role**: A boolean flag indicating if this role is critical to the system's core operation. System roles often have restrictions on modification or deletion to prevent system instability.
-   **User Role**: The explicit association between an `ApplicationUser` and an `ApplicationRole`, indicating that a user is a member of that role.
-   **Role Claim**: A key-value pair associated with a role, often used for fine-grained authorization policies or to convey additional information about the role's capabilities.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`ApplicationRole`**: This is the Aggregate Root for roles. It extends `IdentityRole` from ASP.NET Core Identity, encapsulating all role-related properties, business logic, and managing its relationships with users and claims. It is responsible for maintaining the consistency and integrity of role definitions.
    -   **Entities**:
        -   `ApplicationUserRole` (owned by `ApplicationRole`): Represents the many-to-many relationship between an `ApplicationRole` and an `ApplicationUser` (from `Identity.Users`). This entity explicitly links users to roles.
        -   `ApplicationRoleClaim` (owned by `ApplicationRole`): Represents claims associated with the role, providing a mechanism for adding custom authorization data or metadata to roles.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `DisplayName`, `Description`, `IsDefault`, `Priority`, and `IsSystemRole` act as intrinsic attributes of the `ApplicationRole` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   `ApplicationUser` (from `Core.Domain.Identity.Users`): Referenced by `ApplicationUserRole`, but managed by its own aggregate.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. All role-related business logic is encapsulated within the `ApplicationRole` aggregate itself.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Roles` bounded context.

-   Role names (`Name`) must be unique across the system to ensure unambiguous identification.
-   Default roles (`IsDefault = true`) cannot be deleted. This prevents the system from being left without a default role for new users.
-   Default roles cannot be modified (except for their `IsDefault` flag, which would require careful handling to ensure at least one default role exists in the system). This protects the integrity of core system roles.
-   An `ApplicationRole` cannot be deleted if it is currently assigned to one or more users (`UserRoles.Count > 0`). This prevents orphaned user-role relationships.
-   The `Priority` of a role must fall within a defined range (0 to 100), ensuring valid ordering.
-   `ApplicationRole` instances track their creation and update timestamps, and the user who performed these actions (`CreatedAt`, `UpdatedAt`, `CreatedBy`, `UpdatedBy`), adhering to auditing requirements.
-   `ApplicationRole` includes a `Version` property (`IHasVersion`) to support optimistic concurrency control, preventing conflicting updates.

---

## 🤝 Relationships & Dependencies

-   **`ApplicationRole` to `ApplicationUserRole`**: One-to-many composition. The `ApplicationRole` aggregate is responsible for managing its associations with users through `ApplicationUserRole` entities.
-   **`ApplicationRole` to `ApplicationRoleClaim`**: One-to-many composition. The `ApplicationRole` aggregate manages its associated claims through `ApplicationRoleClaim` entities.
-   **ASP.NET Core Identity Integration**: The `ApplicationRole` aggregate inherits from `Microsoft.AspNetCore.Identity.IdentityRole`, providing seamless integration with the ASP.NET Core Identity framework for core identity functionalities.
-   **Shared Kernel**: Implements `IHasVersion`, `IHasDomainEvents`, and `IHasAuditable` from `SharedKernel.Domain.Attributes` and `SharedKernel.Domain.Events`, leveraging common patterns for versioning, domain event publishing, and auditing.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Application Role**: Instantiate a new `ApplicationRole` with a unique name, optional display name, description, priority, and flags indicating if it's a system or default role.
-   **Update Application Role Details**: Modify the `DisplayName`, `Description`, `Priority`, `IsSystemRole`, or `IsDefault` flags of an existing `ApplicationRole`, subject to business rules (e.g., cannot modify system/default roles).
-   **Delete Application Role**: Remove an `ApplicationRole` from the system, with built-in checks to prevent deletion of default roles or roles currently assigned to users.
-   **Manage Domain Events**: Publish domain events (`Created`, `Updated`, `Deleted`) to signal significant state changes in the role's lifecycle, enabling a decoupled architecture where other parts of the system can react asynchronously.

---

## 📝 Considerations / Notes

-   The `ApplicationRole` aggregate is designed to be robust against invalid operations, particularly concerning default and system roles, which are critical for application stability.
-   The integration with ASP.NET Core Identity means that many underlying identity operations (e.g., role storage, user-role assignment mechanisms) are handled by the framework, while this aggregate focuses on the domain-specific business logic and invariants of roles.
-   Domain Events are a key mechanism for communicating changes in role state to other parts of the system, promoting loose coupling and extensibility.
-   The `UserRoles` and `RoleClaims` collections are managed internally by the `ApplicationRole` aggregate, ensuring that all related data is consistent with the role's state.