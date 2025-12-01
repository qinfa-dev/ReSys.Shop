# Promotions.Audits Bounded Context

This document describes the `Promotions.Audits` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain is responsible for creating and managing audit log entries specifically for promotions. It tracks significant events in a promotion's lifecycle, including creation, updates, activation, deactivation, rule changes, and usage. This provides a comprehensive historical record for accountability, compliance, and analysis of promotional activities.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Promotions.Audits` bounded context.

-   **Promotion Audit Log**: A record of an event related to a `Promotion`, detailing the action performed, the changes made, and contextual information about the user and request. Represented by the `PromotionAuditLog` entity.
-   **Promotion**: The parent promotion to which this audit log entry refers. (Referenced from `Promotions.Promotions` Bounded Context).
-   **Action**: A predefined verb describing the operation performed (e.g., "Created", "Updated", "Activated", "Used").
-   **Description**: A detailed explanation of the audit event.
-   **User Context**: Information about the user who initiated the action, including `UserId` and `UserEmail`.
-   **Request Context**: Information about the request environment, such as `IpAddress` and `UserAgent`.
-   **Changes Before / After**: Snapshots of the promotion's state before and after the action, typically stored as dictionaries.
-   **Metadata**: Additional contextual information for the audit entry.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   None explicitly defined as Aggregate Roots. `PromotionAuditLog` is an entity that is owned by the `Promotion` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   **`PromotionAuditLog`**: This is the central entity of this bounded context. It represents a single audit event for a promotion and is an `AuditableEntity`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `PromotionId`, `Action`, `Description`, `UserId`, `UserEmail`, `IpAddress`, `UserAgent`, `ChangesBefore`, `ChangesAfter`, and `Metadata` are intrinsic attributes.

### Value Objects (standalone, if any)

-   None.

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Promotions.Audits` bounded context.

-   A `PromotionAuditLog` entry must always be associated with a valid `PromotionId`.
-   `Action` and `Description` are required fields and must adhere to maximum length constraints.
-   `PromotionAuditLog` instances track their creation timestamps (`CreatedAt`), adhering to auditing requirements.
-   `ChangesBefore`, `ChangesAfter`, and `Metadata` are stored as dictionaries (often JSONB in database) for flexible schema.

---

## 🤝 Relationships & Dependencies

-   **`PromotionAuditLog` to `Promotion`**: Many-to-one relationship. `PromotionAuditLog` is owned by `Promotion` (from `Promotions.Promotions`).
-   **Shared Kernel**: `PromotionAuditLog` inherits from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), providing common auditing fields.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Promotion Audit Log Entry**: Generate a new `PromotionAuditLog` record to capture a significant event related to a promotion, including user and request context, and before/after state changes.

---

## 📝 Considerations / Notes

-   `PromotionAuditLog` acts as a child entity within the `Promotion` aggregate, and its lifecycle is managed by the `Promotion` aggregate.
-   This domain is primarily a write-only system, focused on reliable data capture for historical and analytical purposes.
-   The use of dictionaries for change tracking provides flexibility without requiring schema changes for every new attribute being audited.
