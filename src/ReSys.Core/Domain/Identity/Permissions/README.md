# Identity.Permissions Bounded Context

This document describes the `Identity.Permissions` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain defines and manages granular access permissions within the system. It provides a structured, standardized way to represent permissions using a `{area}.{resource}.{action}` format, generate human-readable names and descriptions, and categorize them for flexible assignment to users or roles, thereby enforcing security and access control policies.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Identity.Permissions` bounded context.

-   **Access Permission**: A specific authorization rule in the system, structured as `{area}.{resource}.{action}` (e.g., `admin.user.create`). It grants or denies the ability to perform a certain operation on a resource within an area. Represented by the `AccessPermission` entity.
-   **Area**: The top-level segment of an `Access Permission`, indicating a functional module, bounded context, or administrative context (e.g., "admin", "catalog", "order").
-   **Resource**: The middle segment of an `Access Permission`, indicating the specific entity, aggregate, or feature being accessed or manipulated (e.g., "user", "product", "order").
-   **Action**: The last segment of an `Access Permission`, indicating the specific operation being performed (e.g., "create", "view", "edit", "delete", "manage").
-   **Display Name**: A human-readable, user-friendly name for an `Access Permission` (e.g., "Create User"), automatically generated if not provided.
-   **Description**: A detailed explanation of what an `Access Permission` allows or restricts, automatically generated if not provided.
-   **Value**: A simplified string representation of an `Access Permission` (e.g., "admin:user:create"), often used for internal assignment or policy evaluation.
-   **Permission Category**: An enumeration (`PermissionCategory`) indicating whether a permission is primarily intended for assignment to individual users, roles (groups of users), both, or none.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots within this domain. `AccessPermission` acts as a standalone entity whose lifecycle is managed by the system, and it is referenced by other aggregates (e.g., `User`, `Role`) for assignment.

### Entities (not part of an Aggregate Root, if any)

-   **`AccessPermission`**: This is the central entity of this bounded context. It represents a single, immutable (in terms of its core `Name`) access permission. It is an `AuditableEntity`, tracking creation and update details.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `Name`, `Area`, `Resource`, `Action`, `DisplayName`, `Description`, `Value`, and `Category` are intrinsic attributes of the `AccessPermission` entity.

### Value Objects (standalone, if any)

-   **`PermissionCategory`**: An enumeration that categorizes permissions based on their applicability (e.g., `User`, `Role`, `Both`, `None`). This helps in filtering and presenting permissions appropriately in UIs.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. Static helper methods within the `AccessPermission` entity (e.g., `GenerateDisplayName`, `GenerateDescription`, `IsValidAccessPermissionName`, `ParseAccessPermissionName`) provide utility functions for formatting, parsing, and validating permission names and their components. These methods encapsulate logic directly related to the `AccessPermission` entity's structure and representation.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Identity.Permissions` bounded context.

-   `AccessPermission` names must strictly adhere to the `{area}.{resource}.{action}` format, consisting of exactly three segments separated by dots.
-   Each segment (`area`, `resource`, `action`) must conform to specific length constraints (1-64 characters) and a defined regex pattern (`^[a-z0-9]+(?:[-_][a-z0-9]+)*$`), ensuring consistency and validity.
-   `DisplayName`, `Description`, and `Value` properties have maximum length constraints to prevent excessive data storage and ensure UI compatibility.
-   The `Value` property, if provided, must adhere to a specific regex pattern (`^[a-zA-Z0-9:_./-]+$`).
-   `Area`, `Resource`, and `Action` segments are mandatory for the creation of a valid `AccessPermission`.
-   `DisplayName` and `Description` are automatically generated based on the `area`, `resource`, and `action` segments if not explicitly provided during creation, ensuring a default human-readable representation.

---

## 🤝 Relationships & Dependencies

-   `AccessPermission` is a standalone entity. It is designed to be referenced by other entities or aggregates (e.g., `User` from `Identity.Users`, `Role` from `Identity.Roles`) for the purpose of assigning permissions. This implies a many-to-many relationship with `User` and `Role` aggregates, typically managed through junction entities or direct collections within those aggregates.
-   **Shared Kernel**: The `AccessPermission` entity inherits from `AuditableEntity` (from `SharedKernel.Domain.Entities`), providing common auditing fields. It also leverages `CommonInput.Constraints` and `CommonInput.Errors` (from `SharedKernel.Validations`) for consistent validation logic.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Access Permission**: Instantiate a new `AccessPermission` either by providing its `area`, `resource`, and `action` segments, or by providing a full `{area}.{resource}.{action}` name string.
-   **Update Access Permission Details**: Modify the `DisplayName`, `Description`, `Value`, or `PermissionCategory` of an existing `AccessPermission`. The core `Name` (area.resource.action) is immutable after creation.
-   **Validate Permission Name Format**: Check if a given string conforms to the required `{area}.{resource}.{action}` permission naming convention.
-   **Parse Permission Name**: Extract the `area`, `resource`, and `action` segments from a full `AccessPermission` name string.
-   **Generate Human-Readable Names**: Automatically create `DisplayName`s and `Description`s for permissions based on their segments, facilitating user interface presentation.

---

## 📝 Considerations / Notes

-   The immutability of the `AccessPermission.Name` after creation is a key design choice, ensuring that the fundamental identifier of a permission remains stable.
-   The structured naming convention (`area.resource.action`) is crucial for organizing permissions, making them discoverable, and enabling programmatic access control checks.
-   The `PermissionCategory` enum helps in designing user interfaces for permission management, allowing for filtering and grouping of permissions relevant to users or roles.
-   This domain focuses solely on the definition and management of permissions themselves, with the actual assignment and enforcement of these permissions being handled by other related identity domains (e.g., `Identity.Users`, `Identity.Roles`).