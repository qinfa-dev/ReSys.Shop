using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;

namespace ReSys.Core.Domain.Auditing;

public sealed class AuditLog : AuditableEntity<Guid>
{
    public static class Constraints
    {
        public const int EntityNameMaxLength = 100;
        public const int ActionMaxLength = 50;
        public const int UserNameMaxLength = 255;
        public const int UserEmailMaxLength = CommonInput.Constraints.Email.MaxLength;
        public const int IpAddressMaxLength = CommonInput.Constraints.Network.IpV6MaxLength; 
        public const int UserAgentMaxLength = 500;
    }

    public static class Errors
    {
        public static Error EntityNameRequired => CommonInput.Errors.Required(prefix: nameof(AuditLog), field: nameof(EntityName));
        public static Error ActionRequired => CommonInput.Errors.Required(prefix: nameof(AuditLog), field: nameof(Action));
        public static Error EntityIdRequired => CommonInput.Errors.Required(prefix: nameof(AuditLog), field: nameof(EntityId));
        public static Error NotFound(Guid id) => CommonInput.Errors.NotFound(prefix: nameof(AuditLog), field: id.ToString());
    }

    // Core Fields
    public Guid EntityId { get; set; }
    public string EntityName { get; set; } = string.Empty;
    public string Action { get; set; } = string.Empty;
    public DateTimeOffset Timestamp { get; set; }

    // User Context
    public string? UserId { get; set; }
    public string? UserName { get; set; }
    public string? UserEmail { get; set; }

    // Change Tracking
    public string? OldValues { get; set; }
    public string? NewValues { get; set; }
    public string? ChangedProperties { get; set; }

    // Request Context
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestId { get; set; }

    // Additional Context
    public string? Reason { get; set; }
    public string? AdditionalData { get; set; }
    public AuditSeverity Severity { get; set; } = AuditSeverity.Information;

    private AuditLog() { } // Private constructor for EF Core

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

public enum AuditSeverity
{
    Information = 0,
    Warning = 1,
    Error = 2,
    Critical = 3
}