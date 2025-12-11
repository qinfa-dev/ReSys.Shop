using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.Roles;

namespace ReSys.Core.Domain.Identity.Users.Roles;

/// <summary>
/// Represents the explicit many-to-many relationship between a <see cref="User"/> and a <see cref="Role"/>
/// within the ASP.NET Core Identity system. This class extends the default <see cref="IdentityUserRole{TKey}"/>
/// to include additional auditing and assignment tracking capabilities, as well as domain event publishing.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>User Authorization</term>
/// <description>Links a user to one or more roles, which in turn define their permissions and capabilities.</description>
/// </item>
/// <item>
/// <term>Role-Based Access Control (RBAC)</term>
/// <description>Forms the foundation for RBAC by defining which roles a user belongs to.</description>
/// </item>
/// <item>
/// <term>Auditing</term>
/// <description>Tracks when a user was assigned to a role and by whom.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes (inherited or extended):</strong>
/// <list type="bullet">
/// <item>
/// <term>UserId</term>
/// <description>The ID of the <see cref="User"/> assigned to the role.</description>
/// </item>
/// <item>
/// <term>RoleId</term>
/// <description>The ID of the <see cref="Role"/> assigned to the user.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasAssignable</strong> - For tracking who assigned the role and when.</item>
/// <item><strong>IHasDomainEvents</strong> - For publishing domain events on assignment/unassignment.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class UserRole : IdentityUserRole<string>, IHasAssignable, IHasDomainEvents
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="UserRole"/> assignments.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Minimum number of roles a user must have (e.g., at least one role).</summary>
        public const int MinRolePerUser = 1;
        /// <summary>Maximum number of roles a single user can be assigned to.</summary>
        public const int MaxRolePerUser = 10;
        /// <summary>Minimum number of users a role must have.</summary>
        public const int MinUsersPerRole = 0; // A role can exist without users
        /// <summary>Maximum number of users that can be assigned to a single role.</summary>
        public const int MaxUsersPerRole = 1000;
        /// <summary>Regex pattern for validating user IDs (GUID format).</summary>
        public const string UserIdPattern = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="UserRole"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a user has exceeded the maximum allowed number of roles.
        /// </summary>
        public static Error MaxRolesExceeded => Error.Validation(
            code: $"{nameof(UserRole)}.MaxRolesExceeded",
            description: $"User cannot have more than {Constraints.MaxRolePerUser} roles assigned");

        /// <summary>
        /// Error indicating that a role has exceeded the maximum allowed number of assigned users.
        /// </summary>
        public static Error MaxUsersExceeded => Error.Validation(
            code: $"{nameof(UserRole)}.MaxUsersExceeded",
            description: $"Role cannot be assigned to more than {Constraints.MaxUsersPerRole} users");

        /// <summary>
        /// Error indicating that a user already has the specified role assigned.
        /// </summary>
        /// <param name="roleName">The name of the role that is already assigned.</param>
        public static Error AlreadyAssigned(string roleName) => Error.Conflict(
            code: $"{nameof(UserRole)}.AlreadyAssigned",
            description: $"User already has role '{roleName}'");

        /// <summary>
        /// Error indicating that a user does not have the specified role assigned.
        /// </summary>
        /// <param name="roleName">The name of the role that was expected to be assigned.</param>
        public static Error NotAssigned(string roleName) => Error.Conflict(
            code: $"{nameof(UserRole)}.NotAssigned",
            description: $"User does not have role '{roleName}'");

        /// <summary>
        /// Error indicating a general failure during role assignment to a user.
        /// </summary>
        /// <param name="roleName">The name of the role that failed to be assigned.</param>
        public static Error AssignmentFailed(string roleName) => Error.Failure(
            code: $"{nameof(UserRole)}.AssignmentFailed",
            description: $"Failed to assign role '{roleName}' to user");

        /// <summary>
        /// Error indicating a general failure during role removal from a user.
        /// </summary>
        /// <param name="roleName">The name of the role that failed to be removed.</param>
        public static Error RemovalFailed(string roleName) => Error.Failure(
            code: $"{nameof(UserRole)}.RemovalFailed",
            description: $"Failed to remove role '{roleName}' from user");

        /// <summary>
        /// Occurs when an unexpected error happens during a <see cref="UserRole"/> operation.
        /// </summary>
        /// <param name="operation">A descriptive string indicating the operation during which the error occurred.</param>
        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: $"{nameof(UserRole)}.UnexpectedError",
            description: $"An unexpected error occurred while {operation} the user");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the UTC timestamp when this user-role assignment was made.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that assigned this role to the user.
    /// </summary>
    public string? AssignedBy { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user (<see cref="UserId"/>) to whom this role was assigned.
    /// </summary>
    public string? AssignedTo { get; set; }

    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="Role"/> that is part of this assignment.
    /// </summary>
    public Role Role { get; set; } = null!;
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="User"/> that is part of this assignment.
    /// </summary>
    public User User { get; set; } = null!;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="UserRole"/> assignment instance.
    /// This method performs basic initialization and marks the assignment as created.
    /// </summary>
    /// <param name="userId">The unique identifier of the <see cref="User"/>.</param>
    /// <param name="roleId">The unique identifier of the <see cref="Role"/>.</param>
    /// <param name="assignedBy">Optional: The identifier of the user or system performing the assignment.</param>
    /// <returns>
    /// An <see cref="ErrorOr{UserRole}"/> result.
    /// Returns the newly created <see cref="UserRole"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method marks the assignment as assigned and adds an <see cref="Events.Assigned"/> domain event.
    /// Further validation (e.g., maximum roles per user, role existence) is typically handled by
    /// the <see cref="User"/> or <see cref="Role"/> aggregate, or an application service.
    /// </remarks>
    public static ErrorOr<UserRole> Create(string userId, string roleId, string? assignedBy = null)
    {
        UserRole userRole = new()
        {
            UserId = userId,
            RoleId = roleId
        };

        userRole.MarkAsAssigned(assignedTo: userId,
            assignedBy: assignedBy);

        userRole.AddDomainEvent(domainEvent: new Events.Assigned(UserId: userId,
            RoleId: roleId));
        return userRole;
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Assigns this <see cref="UserRole"/> to a specified <see cref="User"/> and <see cref="Role"/>.
    /// This method performs validation checks for constraints like maximum roles per user and maximum users per role.
    /// </summary>
    /// <param name="user">The <see cref="User"/> instance to which the role is being assigned.</param>
    /// <param name="role">The <see cref="Role"/> instance that is being assigned to the user.</param>
    /// <param name="assignedBy">Optional: The identifier of the user or system performing the assignment.</param>
    /// <returns>
    /// An <see cref="ErrorOr{UserRole}"/> result.
    /// Returns the updated <see cref="UserRole"/> instance on successful assignment.
    /// Returns <see cref="Errors.MaxRolesExceeded"/> if the user has too many roles.
    /// Returns <see cref="Errors.MaxUsersExceeded"/> if the role has too many users.
    /// Returns <see cref="Errors.AlreadyAssigned(string)"/> if the user already has this role.
    /// </returns>
    /// <remarks>
    /// This method updates the <c>UserId</c> and <c>RoleId</c> properties, and sets the navigation properties.
    /// It marks the assignment as assigned and adds an <see cref="Events.Assigned"/> domain event.
    /// </remarks>
    public ErrorOr<UserRole> Assign(User user, Role role, string? assignedBy = null)
    {
        // Check constraints
        if (user.UserRoles.Count >= Constraints.MaxRolePerUser)
            return Errors.MaxRolesExceeded;
        if (role.UserRoles.Count >= Constraints.MaxUsersPerRole)
            return Errors.MaxUsersExceeded;
        if (user.UserRoles.Any(predicate: ur => ur.RoleId == role.Id))
            return Errors.AlreadyAssigned(roleName: role.Name ?? "Unknown");

        UserId = user.Id;
        RoleId = role.Id;
        User = user;
        Role = role;

       this.MarkAsAssigned(assignedTo: user.Id,
            assignedBy: assignedBy);

        AddDomainEvent(domainEvent: new Events.Assigned(UserId: UserId,
            RoleId: RoleId));
        return this;
    }

    /// <summary>
    /// Unassigns this <see cref="UserRole"/> from its associated <see cref="User"/> and <see cref="Role"/>.
    /// This method signals that the user-role relationship should no longer exist.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful unassignment.
    /// </returns>
    /// <remarks>
    /// This method marks the assignment as unassigned. The actual removal from the collections
    /// of <see cref="User"/> and <see cref="Role"/> is typically handled by the respective aggregates
    /// or by an application service. An <see cref="Events.Unassigned"/> domain event is added.
    /// </remarks>
    public ErrorOr<Deleted> Unassign()
    {
        string roleName = Role.Name ?? "Unknown";
        this.MarkAsUnassigned();

        AddDomainEvent(domainEvent: new Events.Unassigned(UserId: UserId,
            RoleId: RoleId,
            RoleName: roleName));
        return Result.Deleted;
    }

    #endregion

    #region Events

    /// <summary>
    /// Defines domain events related to the assignment and unassignment of <see cref="UserRole"/>s.
    /// These events enable a decoupled architecture, allowing other services to react to changes
    /// in user-role relationships.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Raised when a <see cref="UserRole"/> is successfully assigned.
        /// Purpose: Notifies the system that a user has been granted a new role, potentially impacting
        /// authorization caches, UI updates, or audit logs.
        /// </summary>
        /// <param name="UserId">The unique identifier of the user involved in the assignment.</param>
        /// <param name="RoleId">The unique identifier of the role involved in the assignment.</param>
        public sealed record Assigned(string UserId, string RoleId) : DomainEvent;
        /// <summary>
        /// Raised when a <see cref="UserRole"/> is successfully unassigned.
        /// Purpose: Signals that a user has had a role removed, prompting updates to authorization data,
        /// UI changes, or audit logs.
        /// </summary>
        /// <param name="UserId">The unique identifier of the user involved in the unassignment.</param>
        /// <param name="RoleId">The unique identifier of the role involved in the unassignment.</param>
        /// <param name="RoleName">The name of the role that was unassigned (for logging/context).</param>
        public sealed record Unassigned(string UserId, string RoleId, string RoleName) : DomainEvent;
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