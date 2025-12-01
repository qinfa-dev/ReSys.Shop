using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.Roles.Claims;
using ReSys.Core.Domain.Identity.Users.Roles;

namespace ReSys.Core.Domain.Identity.Roles;

/// <summary>
/// Role aggregate root - manages roles for ASP.NET Core Identity.
/// Inherits from IdentityRole for integration and IAuditable for tracking.
/// </summary>
public sealed class Role : IdentityRole, IHasVersion, IHasDomainEvents, IHasAuditable
{
    #region Constraints
    public static class Constraints
    {
        public const int MinPriority = 0;
        public const int MaxPriority = 100;
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error RoleNotFound => Error.NotFound(
            code: "Role.RoleNotFound",
            description: $"Role is not found.");

        public static Error DefaultRoleNotFound => Error.NotFound(
            code: "Role.DefaultRoleNotFound",
            description: "The default user role is not configured in the system.");

        public static Error RoleAlreadyExists(string roleName) => Error.Conflict(
            code: "Role.RoleAlreadyExists",
            description: $"A role with the name '{roleName}' already exists.");

        public static Error CannotDeleteDefaultRole(string roleName) => Error.Validation(
            code: "Role.CannotDeleteDefaultRole",
            description: $"Cannot delete default role '{roleName}'.");

        public static Error CannotModifyDefaultRole(string roleName) => Error.Validation(
            code: "Role.CannotModifyDefaultRole",
            description: $"Cannot modify default role '{roleName}'.");

        public static Error RoleInUse(string roleName) => Error.Validation(
            code: "Role.RoleInUse",
            description: $"Cannot delete role '{roleName}' because it is assigned to one or more users.");

        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: "Role.UnexpectedError",
            description: $"An unexpected error occurred while {operation} the role");
    }

    #endregion

    #region Properties
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public bool IsDefault { get; set; }
    public int Priority { get; set; }
    public bool IsSystemRole { get; set; }

    public DateTimeOffset CreatedAt { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public string? UpdatedBy { get; set; }

    public long Version { get; set; }

    #endregion

    #region Relationships

    public ICollection<UserRole> UserRoles { get; set; } = [];
    public ICollection<RoleClaim> RoleClaims { get; set; } = [];

    #endregion

    #region Computed Properties

    public bool IsActive => !IsSystemRole || UserRoles.Any();
    #endregion

    #region Factory Methods

    public static ErrorOr<Role> Create(
        string name,
        string? displayName = null,
        string? description = null,
        int priority = 0,
        bool isSystemRole = false,
        bool isDefault = false,
        string? createdBy = null)
    {
        string trimmedName = name.Trim();

        Role role = new()
        {
            Name = trimmedName,
            NormalizedName = trimmedName.ToUpperInvariant(),
            DisplayName = displayName,
            Description = description?.Trim(),
            Priority = priority,
            IsSystemRole = isSystemRole,
            IsDefault = isDefault,
            CreatedAt = DateTimeOffset.UtcNow,
            CreatedBy = createdBy
        };

        role.AddDomainEvent(domainEvent: new Events.Created(RoleId: role.Id,
            Name: role.Name));
        return role;
    }

    #endregion

    #region Business Logic

    public ErrorOr<Role> Update(
        string? name = null,
        string? displayName = null,
        string? description = null,
        int? priority = null,
        bool? isSystemRole = null,
        bool? isDefault = null,
        string? updatedBy = null)
    {
        if (IsDefault)
            return Errors.CannotModifyDefaultRole(roleName: Name ?? "Unknown");

        bool changed = false;

        // Update name
        if (!string.IsNullOrWhiteSpace(value: name) && name.Trim() != Name)
        {
            string trimmedName = name.Trim();
            Name = trimmedName;
            NormalizedName = trimmedName.ToUpperInvariant();
            changed = true;
        }

        // Update display name
        if (displayName != null && displayName.Trim() != DisplayName)
        {
            string trimmedDisplayName = displayName.Trim();
            DisplayName = trimmedDisplayName;
            changed = true;
        }

        // Update description
        if (description != null && description != Description)
        {
            Description = description.Trim();
            changed = true;
        }

        // Update priority
        if (priority.HasValue && priority != Priority)
        {
            Priority = priority.Value;
            changed = true;
        }

        // Update system role flag
        if (isSystemRole.HasValue && isSystemRole != IsSystemRole)
        {
            IsSystemRole = isSystemRole.Value;
            changed = true;
        }

        // Update default flag
        if (isDefault.HasValue && isDefault != IsDefault)
        {
            IsDefault = isDefault.Value;
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            UpdatedBy = updatedBy;
            AddDomainEvent(domainEvent: new Events.Updated(RoleId: Id,
                Name: Name ?? "Unknown"));
        }

        return this;
    }

    public ErrorOr<Deleted> Delete()
    {
        string roleName = Name ?? "Unknown";
        if (IsDefault)
            return Errors.CannotDeleteDefaultRole(roleName: roleName);

        if (UserRoles.Count > 0)
            return Errors.RoleInUse(roleName: roleName);

        AddDomainEvent(domainEvent: new Events.Deleted(RoleId: Id,
            Name: roleName));
        return Result.Deleted;
    }
    #endregion

    #region Events

    public static class Events
    {
        /// <summary>
        /// Domain event raised when a new application role is created.
        /// Purpose: Notifies the system that a new role is available, potentially impacting user assignment, permissions, or auditing.
        /// </summary>
        public sealed record Created(string RoleId, string Name) : DomainEvent;

        /// <summary>
        /// Domain event raised when an existing application role is updated.
        /// Purpose: Signals that a role's details have changed, prompting dependent services to re-evaluate permissions, user assignments, or audit logs.
        /// </summary>
        public sealed record Updated(string RoleId, string Name) : DomainEvent;

        /// <summary>
        /// Domain event raised when an application role is deleted.
        /// Purpose: Indicates a role has been removed, requiring cleanup, invalidation of user assignments, or logging of the deletion in related services.
        /// </summary>
        public sealed record Deleted(string RoleId, string Name) : DomainEvent;
    }

    #endregion

    #region Domain Event Helpers

    private readonly List<IDomainEvent> _domainEvents = [];
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(item: domainEvent);
    }

    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    #endregion
}