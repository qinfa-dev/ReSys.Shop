# Auditing Bounded Context

This document describes the `Auditing` bounded context, outlining its purpose, ubiquitous language, core components, and key behaviors.

---

## 🎯 Purpose

This domain is responsible for creating and managing audit logs, which record significant changes and actions performed within the system. It captures detailed information about who did what, when, to which entity, and from where, providing a comprehensive historical record for security, compliance, and debugging purposes.

---

## 🗣️ Ubiquitous Language

This section defines the key terms and concepts used within the `Auditing` bounded context.

-   **Audit Log**: A record of an event that occurs within the system, detailing the action performed, the entity affected, and contextual information about the user and request. Represented by the <see cref="AuditLog"/> entity.
-   **Entity ID**: The unique identifier of the business entity that was the subject of the audit action.
-   **Entity Name**: The name or type of the business entity that was audited.
-   **Action**: A predefined verb describing the operation performed (e.g., "Created", "Updated", "OrderPlaced"). Defined by the static class <see cref="AuditAction"/>.
-   **Timestamp**: The exact date and time when the audit event occurred.
-   **User Context**: Information about the user who initiated the action, including <c>UserId</c>, <c>UserName</c>, and <c>UserEmail</c>.
-   **Change Tracking**: Details about the data that was modified, including <c>OldValues</c>, <c>NewValues</c>, and <c>ChangedProperties</c> (often stored as JSON).
-   **Request Context**: Information about the request environment, such as <c>IpAddress</c>, <c>UserAgent</c>, and <c>RequestId</c>.
-   **Severity**: The criticality level of the audit log entry (e.g., <see cref="AuditSeverity.Information"/>, <see cref="AuditSeverity.Warning"/>, <see cref="AuditSeverity.Error"/>, <see cref="AuditSeverity.Critical"/>). Defined by the <see cref="AuditSeverity"/> enumeration.
-   **Reason**: An optional explanation for why the action was performed.

---

## 🏛️ Core Components (Aggregates, Entities, Value Objects)

This domain is composed of the following core building blocks:

### Aggregates

-   **`AuditLog`**: This is the Aggregate Root. It encapsulates all information related to a single audit event and is responsible for its creation and ensuring its data integrity.
    -   **Entities**: None directly owned by <see cref="AuditLog"/>.
    -   **Value Objects**: None explicitly defined as separate classes. Properties like <c>EntityId</c>, <c>EntityName</c>, <c>Action</c>, <c>Timestamp</c>, <c>UserId</c>, <c>UserName</c>, <c>OldValues</c>, <c>NewValues</c>, <c>ChangedProperties</c>, <c>IpAddress</c>, <c>UserAgent</c>, <c>RequestId</c>, <c>Reason</c>, <c>AdditionalData</c>, and <c>Severity</c> are intrinsic attributes of the <see cref="AuditLog"/> aggregate.

### Entities (not part of an Aggregate Root, if any)

-   None.

### Value Objects (standalone, if any)

-   **<see cref="AuditAction"/>**: A static class providing a comprehensive list of standardized action names (e.g., <c>Created</c>, <c>OrderPlaced</c>, <c>PaymentCaptured</c>) to ensure consistency across audit logs. While not a traditional "value object" in the class sense, its constants serve as distinct, descriptive values in the domain.
-   **<see cref="AuditSeverity"/>**: An enumeration defining the levels of criticality for audit log entries (<c>Information</c>, <c>Warning</c>, <c>Error</c>, <c>Critical</c>).

---

## ⚙️ Domain Services (if any)

-   None explicitly defined as separate classes. The `Create` method within the `AuditLog` aggregate handles the construction and initial validation of an audit log entry.

---

## 📜 Business Rules / Invariants

This section outlines the critical business rules and invariants enforced within the `Auditing` bounded context.

-   An <see cref="AuditLog"/> entry must always have an <c>EntityId</c>, <c>EntityName</c>, and <c>Action</c>. These fields are validated as required by the <see cref="AuditLog.Create"/> method.
-   <c>EntityName</c> and <c>Action</c> must adhere to defined maximum length constraints (<see cref="AuditLog.Constraints.EntityNameMaxLength"/>, <see cref="AuditLog.Constraints.ActionMaxLength"/>), validated during creation.
-   The <c>EntityId</c> cannot be an empty <see cref="Guid"/>.
-   The <c>Timestamp</c> of an <see cref="AuditLog"/> entry is automatically set to <c>DateTimeOffset.UtcNow</c> upon creation, ensuring an accurate record of when the event occurred.
-   Metadata fields like <c>OldValues</c>, <c>NewValues</c>, <c>ChangedProperties</c>, and <c>AdditionalData</c> are designed to be flexible, often stored as JSONB to accommodate varying data structures for detailed change tracking.

---

## 🤝 Relationships & Dependencies

-   `AuditLog` is a standalone aggregate. It records information about other entities and aggregates but does not directly own or manage their lifecycle.
-   **Shared Kernel**: `AuditLog` inherits from `AuditableEntity<Guid>` (from `SharedKernel.Domain.Entities`), providing common base properties and auditing concerns. It leverages `ErrorOr` for a functional approach to error handling during creation.

---

## 🚀 Key Use Cases / Behaviors

-   **Create Audit Log Entry**: Generate a new <see cref="AuditLog"/> record using <see cref="AuditLog.Create(Guid, string, string, string?, string?, string?, string?, string?, string?, string?, AuditSeverity)"/> to capture a significant event, providing details about the affected entity, the action, and relevant context. This method performs initial validation and sets the timestamp.
-   **Track Entity Changes**: Record <c>OldValues</c>, <c>NewValues</c>, and <c>ChangedProperties</c> (typically JSON serialized) to provide a granular view of data modifications over time.
-   **Capture User and Request Context**: Store <c>UserId</c>, <c>UserName</c>, <c>IpAddress</c>, and <c>UserAgent</c> to identify who performed the action and from where.
-   **Categorize Audit Events**: Assign a <see cref="AuditSeverity"/> level to audit entries for easier filtering and analysis, allowing for quick identification of critical events.

---

## 📝 Usage Example

Here is an example of how to create an <see cref="AuditLog"/> entry within an application service or domain event handler when a <see cref="Product"/>'s name is updated. This demonstrates capturing the change, the user context, and persisting the log.

```csharp
// Using System.Text.Json for serialization
using System.Text.Json;

// ... within an an Application Service or Domain Event Handler

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
    // Assuming product.Update(name: newName) is a method on the Product aggregate
    product.Update(name: newName); // This would internally update product.Name

    // 4. Capture the state after the change
    var newValues = new { Name = product.Name };

    // 5. Create the AuditLog entry
    var auditLogEntryResult = AuditLog.Create(
        entityId: product.Id,
        entityName: nameof(Product),
        action: AuditAction.Updated,
        userId: userId,
        userName: userEmail, // Assuming userEmail can be used as userName for logging purposes
        oldValues: JsonSerializer.Serialize(oldValues),
        newValues: JsonSerializer.Serialize(newValues),
        changedProperties: JsonSerializer.Serialize(new[] { "Name" }),
        reason: "User updated product name via admin panel.",
        severity: AuditSeverity.Information
    );

    // Handle potential errors during audit log creation
    if (auditLogEntryResult.IsError)
    {
        // Log the error for audit log creation failure, but don't prevent product update
        _logger.LogError($"Failed to create audit log for Product update: {auditLogEntryResult.FirstError.Description}");
    }
    else
    {
        await _auditLogRepository.AddAsync(auditLogEntryResult.Value);
    }
    
    // This assumes an explicit update method for product in the repository
    await _productRepository.UpdateAsync(product);

    // This assumes a Unit of Work pattern to save all changes
    await _unitOfWork.SaveChangesAsync();
}
```

---

---

## 📝 Considerations / Notes

-   The `Auditing` domain is primarily a write-only or append-only system, focusing on reliable data capture rather than complex business logic or state changes.
-   The use of `ErrorOr` in the `Create` method ensures that invalid audit log entries are prevented at the domain level.
-   The `AuditLog` is designed to be highly flexible, especially regarding change tracking and additional data, often relying on JSONB columns in the database.
-   This domain is typically integrated into application services or domain event handlers in other bounded contexts to automatically create audit entries upon significant state changes.
