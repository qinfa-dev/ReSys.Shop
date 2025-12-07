# Identity.Roles Bounded Context

This document describes the `Identity.Roles` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain manages the definition and lifecycle of application roles, which are fundamental for grouping users and assigning permissions within the system. It integrates seamlessly with ASP.NET Core Identity, providing robust mechanisms for creating, updating, and deleting roles, while enforcing critical business rules related to system-defined and default roles to maintain system integrity and security.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Roles` bounded context.

-   **Application Role**: A named collection of permissions that can be assigned to users, simplifying access control management. It represents a specific set of responsibilities or privileges within the application. Represented by the <see cref="Role"/> aggregate.
-   **Role Name**: The unique, normalized identifier for a role (e.g., "ADMINISTRATOR", "CUSTOMER"). This is typically used for internal system logic and lookups.
-   **Display Name**: A human-readable name for the role (e.g., "Administrator", "Customer"), used for presentation in user interfaces.
-   **Description**: A detailed explanation of the role's purpose, responsibilities, or the permissions it encompasses.
-   **Is Default**: A boolean flag indicating if this role is automatically assigned to new users upon registration or creation.
-   **Priority**: An integer value indicating the precedence or order of roles, useful for display or conflict resolution in complex permission scenarios. It is constrained by <see cref="Role.Constraints.MinPriority"/> and <see cref="Role.Constraints.MaxPriority"/>.
-   **Is System Role**: A boolean flag indicating if this role is critical to the system's core operation. System roles often have restrictions on modification or deletion to prevent system instability.
-   **User Role**: The explicit association between an <see cref="ApplicationUser"/> and an <see cref="Role"/>, indicating that a user is a member of that role.
-   **Role Claim**: A key-value pair associated with a role, often used for fine-grained authorization policies or to convey additional information about the role's capabilities. Represented by the <see cref="RoleClaim"/> entity.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`ApplicationRole`**: This is the Aggregate Root for roles. It extends <see cref="IdentityRole"/> from ASP.NET Core Identity, encapsulating all role-related properties, business logic, and managing its relationships with users and claims. It is responsible for maintaining the consistency and integrity of role definitions.
    -   **Entities**:
        -   <see cref="UserRole"/> (owned by <see cref="Role"/>): Represents the many-to-many relationship between an <see cref="Role"/> and an <see cref="ApplicationUser"/> (from `Identity.Users`). This entity explicitly links users to roles.
        -   <see cref="RoleClaim"/> (owned by <see cref="Role"/>): Represents claims associated with the role, providing a mechanism for adding custom authorization data or metadata to roles.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>DisplayName</c>, <c>Description</c>, <c>IsDefault</c>, <c>Priority</c>, <c>IsSystemRole</c> (from <see cref="Role"/>), and auditing/versioning properties (<c>CreatedAt</c>, <c>UpdatedAt</c>, <c>CreatedBy</c>, <c>UpdatedBy</c>, <c>Version</c>) act as intrinsic attributes of the <see cref="Role"/> aggregate.

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

-   Role names (<c>Name</c>) must be unique across the system to ensure unambiguous identification. This is typically enforced by the underlying ASP.NET Core Identity system.
-   Default roles (<c>IsDefault</c> = <c>true</c>) cannot be deleted. The <see cref="Role.Delete()"/> method enforces this by returning <see cref="Role.Errors.CannotDeleteDefaultRole(string)"/>, preventing the system from being left without a default role for new users.
-   Default roles cannot be modified (except for their <c>IsDefault</c> flag, which would require careful handling to ensure at least one default role exists in the system). The <see cref="Role.Update(string?, string?, string?, int?, bool?, bool?, string?)"/> method enforces this by returning <see cref="Role.Errors.CannotModifyDefaultRole(string)"/>. This protects the integrity of core system roles.
-   An <see cref="Role"/> cannot be deleted if it is currently assigned to one or more users (<c>UserRoles.Count > 0</c>). The <see cref="Role.Delete()"/> method enforces this by returning <see cref="Role.Errors.RoleInUse(string)"/>, preventing orphaned user-role relationships.
-   The <c>Priority</c> of a role must fall within a defined range (<see cref="Role.Constraints.MinPriority"/> to <see cref="Role.Constraints.MaxPriority"/>), ensuring valid ordering and display.
-   <see cref="Role"/> instances track their creation and update timestamps, and the user who performed these actions (<c>CreatedAt</c>, <c>UpdatedAt</c>, <c>CreatedBy</c>, <c>UpdatedBy</c>), adhering to auditing requirements via <see cref="IHasAuditable"/>.
-   <see cref="Role"/> includes a <c>Version</c> property (<see cref="IHasVersion"/>) to support optimistic concurrency control, preventing conflicting updates in a multi-user environment.

---

## 🤝 Relationships & Dependencies

-   **`<see cref="Role"/> to <see cref="UserRole"/>`**: One-to-many composition. The <see cref="Role"/> aggregate is responsible for managing its associations with users through <see cref="UserRole"/> entities.
-   **`<see cref="Role"/> to <see cref="RoleClaim"/>`**: One-to-many composition. The <see cref="Role"/> aggregate manages its associated claims through <see cref="RoleClaim"/> entities.
-   **ASP.NET Core Identity Integration**: The <see cref="Role"/> aggregate inherits from <c>Microsoft.AspNetCore.Identity.IdentityRole</c>, providing seamless integration with the ASP.NET Core Identity framework for core identity functionalities.
-   **Shared Kernel**: Implements <see cref="IHasVersion"/>, <see cref="IHasDomainEvents"/>, and <see cref="IHasAuditable"/> from <c>SharedKernel.Domain.Attributes</c> and <c>SharedKernel.Domain.Events</c>, leveraging common patterns for versioning, domain event publishing, and auditing.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Application Role**: Instantiate a new <see cref="Role"/> using <see cref="Role.Create(string, string?, string?, int, bool, bool, string?)"/> with a unique name, optional display name, description, priority, and flags indicating if it's a system or default role.
-   **Update Application Role Details**: Modify the <c>DisplayName</c>, <c>Description</c>, <c>Priority</c>, <c>IsSystemRole</c>, or <c>IsDefault</c> flags of an existing <see cref="Role"/> using <see cref="Role.Update(string?, string?, string?, int?, bool?, bool?, string?)"/>. This method enforces business rules to prevent modification of default roles.
-   **Delete Application Role**: Remove an <see cref="Role"/> from the system using <see cref="Role.Delete()"/>, with built-in checks to prevent deletion of default roles or roles currently assigned to users.
-   **Manage Domain Events**: <see cref="Role"/> publishes events such as <see cref="Role.Events.Created"/>, <see cref="Role.Events.Updated"/>, and <see cref="Role.Events.Deleted"/> to signal significant state changes in the role's lifecycle, enabling a decoupled architecture where other parts of the system can react asynchronously.

---

## 📝 Considerations / Notes

-   The `ApplicationRole` aggregate is designed to be robust against invalid operations, particularly concerning default and system roles, which are critical for application stability.
-   The integration with ASP.NET Core Identity means that many underlying identity operations (e.g., role storage, user-role assignment mechanisms) are handled by the framework, while this aggregate focuses on the domain-specific business logic and invariants of roles.
-   Domain Events are a key mechanism for communicating changes in role state to other parts of the system, promoting loose coupling and extensibility.
-   The `UserRoles` and `RoleClaims` collections are managed internally by the `ApplicationRole` aggregate, ensuring that all related data is consistent with the role's state.