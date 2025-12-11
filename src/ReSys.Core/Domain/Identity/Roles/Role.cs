using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.Roles.Claims;
using ReSys.Core.Domain.Identity.Users.Roles;

namespace ReSys.Core.Domain.Identity.Roles;

/// <summary>
/// Represents an application role within the ASP.NET Core Identity system, serving as an aggregate root
/// for managing role-specific properties, constraints, and associated claims and user assignments.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>User Grouping</term>
/// <description>Groups users for simplified permission management.</description>
/// </item>
/// <item>
/// <term>Permission Assignment</term>
/// <description>Claims can be assigned to roles, granting specific permissions to all members of the role.</description>
/// </item>
/// <item>
/// <term>Lifecycle Management</term>
/// <description>Manages creation, update, and deletion of roles, including special handling for default and system roles.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>Name</term>
/// <description>Unique internal name of the role (e.g., "Administrator", "Customer").</description>
/// </item>
/// <item>
/// <term>DisplayName</term>
/// <description>Human-readable name for UI display.</description>
/// </item>
/// <item>
/// <term>IsDefault</term>
/// <description>Indicates if the role is assigned to new users by default.</description>
/// </item>
/// <item>
/// <term>IsSystemRole</term>
/// <description>Indicates if the role is critical and protected from certain modifications/deletions.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasVersion</strong> - For optimistic concurrency control.</item>
/// <item><strong>IHasDomainEvents</strong> - For publishing domain events on state changes.</item>
/// <item><strong>IHasAuditable</strong> - For tracking creation and update timestamps and by whom.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class Role : IdentityRole, IHasVersion, IHasDomainEvents, IHasAuditable
{
    #region Constraints
    /// <summary>
    /// Defines constraints and constant values specific to <see cref="Role"/> properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Minimum allowed value for role priority.</summary>
        public const int MinPriority = 0;
        /// <summary>Maximum allowed value for role priority.</summary>
        public const int MaxPriority = 100;
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="Role"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>Error indicating that a requested role could not be found.</summary>
        public static Error RoleNotFound => Error.NotFound(
            code: "Role.RoleNotFound",
            description: $"Role is not found.");

        /// <summary>Error indicating that the default user role is not configured in the system.</summary>
        public static Error DefaultRoleNotFound => Error.NotFound(
            code: "Role.DefaultRoleNotFound",
            description: "The default user role is not configured in the system.");

        /// <summary>
        /// Error indicating that a role with the specified name already exists.
        /// </summary>
        /// <param name="roleName">The name of the conflicting role.</param>
        public static Error RoleAlreadyExists(string roleName) => Error.Conflict(
            code: "Role.RoleAlreadyExists",
            description: $"A role with the name '{roleName}' already exists.");

        /// <summary>
        /// Error indicating that an attempt was made to delete a default role.
        /// Default roles are protected from deletion to maintain system integrity.
        /// </summary>
        /// <param name="roleName">The name of the default role that cannot be deleted.</param>
        public static Error CannotDeleteDefaultRole(string roleName) => Error.Validation(
            code: "Role.CannotDeleteDefaultRole",
            description: $"Cannot delete default role '{roleName}'.");

        /// <summary>
        /// Error indicating that an attempt was made to modify a default role.
        /// Default roles are protected from certain modifications to maintain system integrity.
        /// </summary>
        /// <param name="roleName">The name of the default role that cannot be modified.</param>
        public static Error CannotModifyDefaultRole(string roleName) => Error.Validation(
            code: "Role.CannotModifyDefaultRole",
            description: $"Cannot modify default role '{roleName}'.");

        /// <summary>
        /// Error indicating that a role cannot be deleted because it is currently assigned to one or more users.
        /// </summary>
        /// <param name="roleName">The name of the role that is in use.</param>
        public static Error RoleInUse(string roleName) => Error.Validation(
            code: "Role.RoleInUse",
            description: $"Cannot delete role '{roleName}' because it is assigned to one or more users.");

        /// <summary>
        /// Occurs when an unexpected error happens during a <see cref="Role"/> operation.
        /// </summary>
        /// <param name="operation">A descriptive string indicating the operation during which the error occurred.</param>
        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: "Role.UnexpectedError",
            description: $"An unexpected error occurred while {operation} the role");
    }

    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the human-readable display name for the role.
    /// This is used for presentation in user interfaces and can differ from the unique <c>Name</c>.
    /// </summary>
    public string? DisplayName { get; set; }
    /// <summary>
    /// Gets or sets a detailed description of the role's purpose, responsibilities, or the permissions it encompasses.
    /// </summary>
    public string? Description { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this role is automatically assigned to new users upon registration or creation.
    /// Default roles often have special protection from deletion.
    /// </summary>
    public bool IsDefault { get; set; }
    /// <summary>
    /// Gets or sets an integer value indicating the priority or precedence of the role.
    /// Higher priority roles might override permissions of lower priority roles, or influence display order.
    /// Must be within the range defined by <see cref="Constraints.MinPriority"/> and <see cref="Constraints.MaxPriority"/>.
    /// </summary>
    public int Priority { get; set; }
    /// <summary>
    /// Gets or sets a value indicating whether this role is a system-critical role.
    /// System roles are often protected from modification or deletion to prevent system instability.
    /// </summary>
    public bool IsSystemRole { get; set; }

    /// <summary>
    /// Gets or sets the UTC timestamp when the role was created.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public DateTimeOffset CreatedAt { get; set; }
    /// <summary>
    /// Gets or sets the UTC timestamp when the role was last updated.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public DateTimeOffset? UpdatedAt { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that created the role.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public string? CreatedBy { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that last updated the role.
    /// Inherited from <see cref="IHasAuditable"/>.
    /// </summary>
    public string? UpdatedBy { get; set; }

    /// <summary>
    /// Gets or sets the concurrency token used for optimistic concurrency control.
    /// Inherited from <see cref="IHasVersion"/>.
    /// </summary>
    public long Version { get; set; }

    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the collection of <see cref="UserRole"/> entities that link users to this role.
    /// Represents the many-to-many relationship between users and roles.
    /// </summary>
    public ICollection<UserRole> UserRoles { get; set; } = [];
    /// <summary>
    /// Gets or sets the collection of <see cref="RoleClaim"/> entities that define permissions or attributes assigned to this role.
    /// </summary>
    public ICollection<RoleClaim> RoleClaims { get; set; } = [];

    #endregion

    #region Computed Properties

    /// <summary>
    /// Indicates if the role is currently considered active.
    /// A role is active if it is not a system role, or if it has at least one user assigned to it.
    /// This property might be used for UI filtering or to determine if a role is "in use".
    /// </summary>
    public bool IsActive => !IsSystemRole || UserRoles.Any();
    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="Role"/> instance.
    /// Performs basic initialization and adds a <see cref="Events.Created"/> domain event.
    /// </summary>
    /// <param name="name">The unique internal name of the role (e.g., "Administrator").</param>
    /// <param name="displayName">Optional: Human-readable name for display. If null, derived from <paramref name="name"/>.</param>
    /// <param name="description">Optional: Detailed explanation of the role.</param>
    /// <param name="priority">The priority of the role, between <see cref="Constraints.MinPriority"/> and <see cref="Constraints.MaxPriority"/>.</param>
    /// <param name="isSystemRole">Indicates if this is a critical system role. Defaults to false.</param>
    /// <param name="isDefault">Indicates if this role is assigned to new users by default. Defaults to false.</param>
    /// <param name="createdBy">Optional: The identifier of the user or system that created this role.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Role}"/> result.
    /// Returns the newly created <see cref="Role"/> instance on success.
    /// Basic validation for name uniqueness and format is typically handled by the Identity framework or application layer.
    /// </returns>
    /// <remarks>
    /// This method sets the <c>Name</c> and <c>NormalizedName</c> (uppercase version of name for comparisons)
    /// and initializes auditing fields.
    /// </remarks>
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
            Id = Guid.NewGuid().ToString(), // IdentityRole expects string ID
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

    /// <summary>
    /// Updates the mutable properties of the <see cref="Role"/>.
    /// This method performs validation to prevent modification of default roles and updates role details.
    /// </summary>
    /// <param name="name">The new unique internal name of the role. If null, the existing name is retained.</param>
    /// <param name="displayName">The new human-readable display name. If null, the existing display name is retained.</param>
    /// <param name="description">The new detailed description. If null, the existing description is retained.</param>
    /// <param name="priority">The new priority of the role. If null, the existing priority is retained.</param>
    /// <param name="isSystemRole">The new flag indicating if this is a system role. If null, the existing flag is retained.</param>
    /// <param name="isDefault">The new flag indicating if this is a default role. If null, the existing flag is retained.</param>
    /// <param name="updatedBy">The identifier of the user or system that updated this role.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Role}"/> result.
    /// Returns the updated <see cref="Role"/> instance on success.
    /// Returns <see cref="Errors.CannotModifyDefaultRole(string)"/> if attempting to modify a default role.
    /// </returns>
    /// <remarks>
    /// This method ensures that system and default roles are protected from unauthorized or accidental changes.
    /// It updates the role's properties and recalculates <c>NormalizedName</c> if the <paramref name="name"/> changes.
    /// The <c>UpdatedAt</c> timestamp and <c>UpdatedBy</c> fields are automatically updated if any changes occur.
    /// A <see cref="Events.Updated"/> domain event is added.
    /// </remarks>
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

    /// <summary>
    /// Deletes the <see cref="Role"/> from the system.
    /// This operation is subject to several constraints to maintain system integrity.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful deletion.
    /// Returns <see cref="Errors.CannotDeleteDefaultRole(string)"/> if attempting to delete a default role.
    /// Returns <see cref="Errors.RoleInUse(string)"/> if the role is currently assigned to users.
    /// </returns>
    /// <remarks>
    /// This method enforces that:
    /// <list type="bullet">
    /// <item><term>Default roles</term><description>cannot be deleted to ensure system stability.</description></item>
    /// <item><term>Roles assigned to users</term><description>cannot be deleted until all user assignments are removed.</description></item>
    /// </list>
    /// A <see cref="Events.Deleted"/> domain event is added upon successful deletion.
    /// </remarks>
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

    /// <summary>
    /// Defines domain events related to the lifecycle and state changes of a <see cref="Role"/>.
    /// These events are crucial for enabling a decoupled, event-driven architecture, allowing
    /// other services or bounded contexts to react to role-related changes.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Domain event raised when a new application role is created.
        /// Purpose: Notifies the system that a new role is available, potentially impacting user assignment, permissions, or auditing.
        /// </summary>
        /// <param name="RoleId">The unique identifier (string) of the newly created role.</param>
        /// <param name="Name">The name of the newly created role.</param>
        public sealed record Created(string RoleId, string Name) : DomainEvent;

        /// <summary>
        /// Domain event raised when an existing application role is updated.
        /// Purpose: Signals that a role's details have changed, prompting dependent services to re-evaluate permissions, user assignments, or audit logs.
        /// </summary>
        /// <param name="RoleId">The unique identifier (string) of the updated role.</param>
        /// <param name="Name">The updated name of the role.</param>
        public sealed record Updated(string RoleId, string Name) : DomainEvent;

        /// <summary>
        /// Domain event raised when an application role is deleted.
        /// Purpose: Indicates a role has been removed, requiring cleanup, invalidation of user assignments, or logging of the deletion in related services.
        /// </summary>
        /// <param name="RoleId">The unique identifier (string) of the deleted role.</param>
        /// <param name="Name">The name of the deleted role.</param>
        public sealed record Deleted(string RoleId, string Name) : DomainEvent;
    }

    #endregion

    #region Domain Event Helpers

    /// <summary>
    /// A private list to store domain events that have been added to this aggregate.
    /// These events are typically dispatched after the aggregate's state changes are persisted.
    /// </summary>
    private readonly List<IDomainEvent> _domainEvents = [];
    /// <summary>
    /// Gets a read-only collection of domain events associated with this aggregate.
    /// </summary>
    public IReadOnlyList<IDomainEvent> DomainEvents => _domainEvents.AsReadOnly();

    /// <summary>
    /// Adds a domain event to the aggregate's internal list.
    /// </summary>
    /// <param name="domainEvent">The <see cref="IDomainEvent"/> to add.</param>
    public void AddDomainEvent(IDomainEvent domainEvent)
    {
        _domainEvents.Add(item: domainEvent);
    }

    /// <summary>
    /// Clears all domain events from the aggregate's internal list.
    /// This method is typically called after events have been successfully dispatched.
    /// </summary>
    public void ClearDomainEvents()
    {
        _domainEvents.Clear();
    }

    #endregion
}