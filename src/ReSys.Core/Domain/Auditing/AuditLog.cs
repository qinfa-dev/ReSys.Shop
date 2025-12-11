using ReSys.Core.Common.Domain.Entities;

namespace ReSys.Core.Domain.Auditing;

/// <summary>
/// Represents an immutable audit log entry, capturing significant events and changes within the application.
/// Audit logs provide a historical record of operations performed on entities, including who performed them,
/// when they occurred, and what data was changed.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Auditing Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Accountability</term>
/// <description>Tracks user actions and system events for accountability.</description>
/// </item>
/// <item>
/// <term>Troubleshooting</term>
/// <description>Aids in debugging and understanding system behavior over time.</description>
/// </item>
/// <item>
/// <term>Compliance</term>
/// <description>Helps meet regulatory and internal compliance requirements.</description>
/// </item>
/// <item>
/// <term>Security Monitoring</term>
/// <description>Identifies suspicious activities or unauthorized access attempts.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Information Captured:</strong>
/// <list type="bullet">
/// <item>
/// <term>Entity Identity</term>
/// <description>ID and name of the entity affected.</description>
/// </item>
/// <item>
/// <term>Action Performed</term>
/// <description>Type of operation (e.g., Created, Updated, Deleted, OrderPlaced).</description>
/// </item>
/// <item>
/// <term>Timestamp</term>
/// <description>When the event occurred.</description>
/// </item>
/// <item>
/// <term>User Context</term>
/// <description>Who initiated the action (User ID, name, email).</description>
/// </item>
/// <item>
/// <term>Change Details</term>
/// <description>Snapshot of old and new values, and changed properties (often JSON serialized).</description>
/// </item>
/// <item>
/// <term>Request Context</term>
/// <description>Origin of the request (IP address, user agent).</description>
/// </item>
/// <term>Severity</term>
/// <description>The criticality level of the audit event.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class AuditLog : AuditableEntity<Guid>
{
    public static class Constraints
    {
        /// <summary>Maximum length for the name of the entity being audited.</summary>
        public const int EntityNameMaxLength = 100;
        /// <summary>Maximum length for the action string (e.g., "Created", "OrderPlaced").</summary>
        public const int ActionMaxLength = 50;
        /// <summary>Maximum length for the username in the audit log.</summary>
        public const int UserNameMaxLength = 255;
        /// <summary>Maximum length for the user's email in the audit log.</summary>
        public const int UserEmailMaxLength = CommonInput.Constraints.Email.MaxLength; 
        /// <summary>Maximum length for the IP address recorded in the audit log (supports IPv6).</summary>
        public const int IpAddressMaxLength = CommonInput.Constraints.Network.IpV6MaxLength; 
        /// <summary>Maximum length for the user agent string recorded in the audit log.</summary>
        public const int UserAgentMaxLength = 500;
    }

    public static class Errors
    {
        /// <summary>Error indicating that the entity name is required.</summary>
        public static Error EntityNameRequired => CommonInput.Errors.Required(prefix: nameof(AuditLog), field: nameof(EntityName));
        /// <summary>Error indicating that the action is required.</summary>
        public static Error ActionRequired => CommonInput.Errors.Required(prefix: nameof(AuditLog), field: nameof(Action));
        /// <summary>Error indicating that the entity ID is required.</summary>
        public static Error EntityIdRequired => CommonInput.Errors.Required(prefix: nameof(AuditLog), field: nameof(EntityId));
        /// <summary>
        /// Error indicating that a requested audit log entry could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="AuditLog"/> entry that was not found.</param>
        public static Error NotFound(Guid id) => CommonInput.Errors.NotFound(prefix: nameof(AuditLog), field: id.ToString());
    }

    // Core Fields
    /// <summary>
    /// Gets or sets the unique identifier of the entity that the audit log entry refers to.
    /// </summary>
    public Guid EntityId { get; set; }
    /// <summary>
    /// Gets or sets the name of the entity type being audited (e.g., "Product", "User").
    /// </summary>
    public string EntityName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the action performed (e.g., "Created", "Updated", "OrderPlaced").
    /// Uses constants from <see cref="AuditAction"/>.
    /// </summary>
    public string Action { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the UTC timestamp when the audit event occurred.
    /// </summary>
    public DateTimeOffset Timestamp { get; set; }

    // User Context
    /// <summary>
    /// Gets or sets the unique identifier of the user who initiated the action.
    /// Nullable if the action was performed by the system or an unauthenticated user.
    /// </summary>
    public string? UserId { get; set; }
    /// <summary>
    /// Gets or sets the username of the user who initiated the action.
    /// </summary>
    public string? UserName { get; set; }
    /// <summary>
    /// Gets or sets the email address of the user who initiated the action.
    /// </summary>
    public string? UserEmail { get; set; }

    // Change Tracking
    /// <summary>
    /// Gets or sets a JSON string representing the values of the entity's properties *before* the action.
    /// </summary>
    public string? OldValues { get; set; }
    /// <summary>
    /// Gets or sets a JSON string representing the values of the entity's properties *after* the action.
    /// </summary>
    public string? NewValues { get; set; }
    /// <summary>
    /// Gets or sets a JSON string indicating which properties were changed, typically an array of property names.
    /// </summary>
    public string? ChangedProperties { get; set; }

    // Request Context
    /// <summary>
    /// Gets or sets the IP address from which the action originated.
    /// </summary>
    public string? IpAddress { get; set; }
    /// <summary>
    /// Gets or sets the user agent string of the client that initiated the action.
    /// </summary>
    public string? UserAgent { get; set; }
    /// <summary>
    /// Gets or sets a unique identifier for the request during which the action occurred.
    /// </summary>
    public string? RequestId { get; set; }

    // Additional Context
    /// <summary>
    /// Gets or sets a human-readable reason for the audited action (e.g., "Admin override", "Customer request").
    /// </summary>
    public string? Reason { get; set; }
    /// <summary>
    /// Gets or sets additional arbitrary data related to the audit event, stored as a JSON string.
    /// </summary>
    public string? AdditionalData { get; set; }
    /// <summary>
    /// Gets or sets the <see cref="AuditSeverity"/> of this log entry.
    /// Defaults to <see cref="AuditSeverity.Information"/>.
    /// </summary>
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private AuditLog() { }

    /// <summary>
    /// Factory method to create a new <see cref="AuditLog"/> instance.
    /// Performs basic validation for mandatory fields.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity affected by the action.</param>
    /// <param name="entityName">The name of the entity type (e.g., "Product", "User").</param>
    /// <param name="action">The action performed (e.g., "Created", "OrderPlaced").</param>
    /// <param name="userId">Optional: The unique identifier of the user who initiated the action.</param>
    /// <param name="userName">Optional: The username of the user who initiated the action.</param>
    /// <param name="oldValues">Optional: JSON string of old property values before the change.</param>
    /// <param name="newValues">Optional: JSON string of new property values after the change.</param>
    /// <param name="changedProperties">Optional: JSON string of property names that were changed.</param>
    /// <param name="ipAddress">Optional: IP address from which the action originated.</param>
    /// <param name="reason">Optional: Human-readable reason for the action.</param>
    /// <param name="severity">The <see cref="AuditSeverity"/> of the log entry. Defaults to <see cref="AuditSeverity.Information"/>.</param>
    /// <returns>
    /// An <see cref="ErrorOr{AuditLog}"/> result.
    /// Returns the newly created <see cref="AuditLog"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation for mandatory fields fails.
    /// </returns>
    /// <remarks>
    /// This method initializes the audit log entry with a new <c>Id</c> and <c>Timestamp</c>.
    /// It performs basic validation to ensure core fields are provided and within length constraints.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var auditLogResult = AuditLog.Create(
    ///     entityId: Guid.Parse("some-product-id"),
    ///     entityName: "Product",
    ///     action: AuditAction.Created,
    ///     userId: "admin-user-id",
    ///     userName: "Admin",
    ///     ipAddress: "192.168.1.1",
    ///     reason: "New product onboarding",
    ///     severity: AuditSeverity.Information);
    /// 
    /// if (auditLogResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating audit log: {auditLogResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newAuditLog = auditLogResult.Value;
    ///     Console.WriteLine($"Audit log created for action '{newAuditLog.Action}' on entity '{newAuditLog.EntityName}'.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static ErrorOr<AuditLog> Create(
        Guid entityId,
        string entityName,
        string action,
        string? userId = null,
        string? userName = null,
        string? oldValues = null,
        string? newValues = null,
        string? changedProperties = null,
        string? ipAddress = null,
        string? reason = null,
        AuditSeverity severity = AuditSeverity.Information)
    {
        var errors = Validate(entityId: entityId, entityName: entityName, action: action);
        if (errors.Any()) return errors.First();

        return new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityId = entityId,
            EntityName = entityName.Trim(),
            Action = action.Trim(),
            Timestamp = DateTimeOffset.UtcNow,
            UserId = userId,
            UserName = userName?.Trim(),
            OldValues = oldValues,
            NewValues = newValues,
            ChangedProperties = changedProperties,
            IpAddress = ipAddress?.Trim(),
            Reason = reason?.Trim(),
            Severity = severity
        };
    }

    /// <summary>
    /// Performs internal validation for mandatory fields of an <see cref="AuditLog"/> entry.
    /// This method is primarily used by the <see cref="Create"/> factory method.
    /// </summary>
    /// <param name="entityId">The unique identifier of the entity.</param>
    /// <param name="entityName">The name of the entity type.</param>
    /// <param name="action">The action performed.</param>
    /// <returns>An array of <see cref="Error"/> objects if any validation rules are violated; otherwise, an empty array.</returns>
    private static Error[] Validate(Guid entityId, string entityName, string action)
    {
        var errors = new List<Error>();
        if (entityId == Guid.Empty) errors.Add(item: Errors.EntityIdRequired);
        if (string.IsNullOrWhiteSpace(value: entityName)) errors.Add(item: Errors.EntityNameRequired);
        else if (entityName.Length > Constraints.EntityNameMaxLength)
            errors.Add(item: CommonInput.Errors.TooLong(prefix: nameof(AuditLog), field: nameof(EntityName), maxLength: Constraints.EntityNameMaxLength));
        if (string.IsNullOrWhiteSpace(value: action)) errors.Add(item: Errors.ActionRequired);
        else if (action.Length > Constraints.ActionMaxLength)
            errors.Add(item: CommonInput.Errors.TooLong(prefix: nameof(AuditLog), field: nameof(Action), maxLength: Constraints.ActionMaxLength));
        return errors.ToArray();
    }
}

/// <summary>
/// Defines the severity level of an audit log entry.
/// This helps in prioritizing and filtering audit events.
/// </summary>
public enum AuditSeverity
{
    /// <summary>Informational audit event, typically for routine operations.</summary>
    Information = 0,
    /// <summary>Warning audit event, indicating a non-critical issue or unusual activity.</summary>
    Warning = 1,
    /// <summary>Error audit event, indicating a failure or a problematic operation.</summary>
    Error = 2,
    /// <summary>Critical audit event, indicating a severe issue or a security breach attempt.</summary>
    Critical = 3
}