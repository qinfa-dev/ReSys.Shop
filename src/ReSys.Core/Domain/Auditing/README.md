# Auditing Bounded Context

This document describes the `Auditing` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain is responsible for creating and managing audit logs, which record significant changes and actions performed within the system. It captures detailed information about who did what, when, to which entity, and from where, providing a comprehensive historical record for security, compliance, and debugging purposes.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Auditing` bounded context.

-   **Audit Log**: A record of an event that occurs within the system, detailing the action performed, the entity affected, and contextual information about the user and request. Represented by the `AuditLog` aggregate.
-   **Entity ID**: The unique identifier of the business entity that was the subject of the audit action.
-   **Entity Name**: The name or type of the business entity that was audited.
-   **Action**: A predefined verb describing the operation performed (e.g., "Created", "Updated", "OrderPlaced"). Defined by `AuditAction`.
-   **Timestamp**: The exact date and time when the audit event occurred.
-   **User Context**: Information about the user who initiated the action, including `UserId`, `UserName`, and `UserEmail`.
-   **Change Tracking**: Details about the data that was modified, including `OldValues`, `NewValues`, and `ChangedProperties` (often stored as JSON).
-   **Request Context**: Information about the request environment, such as `IpAddress`, `UserAgent`, and `RequestId`.
-   **Severity**: The criticality level of the audit log entry (e.g., `Information`, `Warning`, `Error`, `Critical`). Defined by `AuditSeverity`.
-   **Reason**: An optional explanation for why the action was performed.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`AuditLog`**: This is the Aggregate Root. It encapsulates all information related to a single audit event and is responsible for its creation and ensuring its data integrity.
    -   **Entities**: None directly owned by `AuditLog`.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like `EntityId`, `EntityName`, `Action`, `Timestamp`, `UserId`, `UserName`, `OldValues`, `NewValues`, `ChangedProperties`, `IpAddress`, `UserAgent`, `RequestId`, `Reason`, `AdditionalData`, and `Severity` are intrinsic attributes of the `AuditLog` aggregate.

### Entities (not part of an Aggregate Root, if any)

-   None.

### Value Objects (standalone, if any)

-   **`AuditAction`**: A static class providing a comprehensive list of standardized action names (e.g., `Created`, `OrderPlaced`, `PaymentCaptured`) to ensure consistency across audit logs.
-   **`AuditSeverity`**: An enumeration defining the levels of criticality for audit log entries (`Information`, `Warning`, `Error`, `Critical`).

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `Create` method within the `AuditLog` aggregate handles the construction and initial validation of an audit log entry.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Auditing` bounded context.

-   An `AuditLog` entry must always have an `EntityId`, `EntityName`, and `Action`.
-   `EntityName` and `Action` must adhere to defined maximum length constraints (`AuditLog.Constraints.EntityNameMaxLength`, `AuditLog.Constraints.ActionMaxLength`).
-   The `EntityId` cannot be an empty GUID.
-   The `Timestamp` of an `AuditLog` entry is automatically set to `DateTimeOffset.UtcNow` upon creation.
-   Metadata fields like `OldValues`, `NewValues`, `ChangedProperties`, and `AdditionalData` are often stored as JSONB to accommodate flexible schema changes and detailed change tracking.

---

## 🤝 Relationships & Dependencies

-   `AuditLog` is a standalone aggregate. It records information about other entities and aggregates but does not directly own or manage their lifecycle.
-   **Shared Kernel**: `AuditLog` inherits from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), providing common base properties and auditing concerns. It leverages `ErrorOr` for a functional approach to error handling during creation.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Audit Log Entry**: Generate a new `AuditLog` record to capture a significant event, providing details about the affected entity, the action, and relevant context.
-   **Track Entity Changes**: Record `OldValues`, `NewValues`, and `ChangedProperties` to provide a granular view of data modifications.
-   **Capture User and Request Context**: Store `UserId`, `UserName`, `IpAddress`, and `UserAgent` to identify who performed the action and from where.
-   **Categorize Audit Events**: Assign a `Severity` level to audit entries for easier filtering and analysis.

---

## 📝 Usage Example

Here is an example of how to create an `AuditLog` entry within an application service or domain event handler when a `Product`'s name is updated. This demonstrates capturing the change, the user context, and persisting the log.

```csharp
// Using System.Text.Json for serialization
using System.Text.Json;

// ... within an Application Service or Domain Event Handler

public async Task UpdateProductNameAsync(Guid productId, string newName, string userId, string userEmail)
{
    // 1. Fetch the entity to be updated
    var product = await _productRepository.GetByIdAsync(productId);
    if (product is null)
    {
        // Handle error: product not found
        return;
    }

    // 2. Capture the state before the change
    var oldValues = new { Name = product.Name };

    // 3. Perform the update
    product.Update(name: newName);

    // 4. Capture the state after the change
    var newValues = new { Name = product.Name };

    // 5. Create the AuditLog entry
    var auditLogEntry = AuditLog.Create(
        entityId: product.Id,
        entityName: nameof(Product),
        action: AuditAction.Updated,
        userId: userId,
        userEmail: userEmail, // Pass user's email
        oldValues: JsonSerializer.Serialize(oldValues),
        newValues: JsonSerializer.Serialize(newValues),
        changedProperties: JsonSerializer.Serialize(new[] { "Name" }),
        reason: "User updated product name via admin panel.",
        severity: AuditSeverity.Information
    );

    // 6. Persist the audit log and the updated entity
    if (!auditLogEntry.IsError)
    {
        await _auditLogRepository.AddAsync(auditLogEntry.Value);
    }
    
    await _productRepository.UpdateAsync(product);

    // This assumes a Unit of Work pattern to save all changes
    await _unitOfWork.SaveChangesAsync();
    await _productRepository.UpdateAsync(product);

    // This assumes a Unit of Work pattern to save all changes
    await _unitOfWork.SaveChangesAsync();
}
```

---

## 📝 Considerations / Notes

-   The `Auditing` domain is primarily a write-only or append-only system, focusing on reliable data capture rather than complex business logic or state changes.
-   The use of `ErrorOr` in the `Create` method ensures that invalid audit log entries are prevented at the domain level.
-   The `AuditLog` is designed to be highly flexible, especially regarding change tracking and additional data, often relying on JSONB columns in the database.
-   This domain is typically integrated into application services or domain event handlers in other bounded contexts to automatically create audit entries upon significant state changes.
