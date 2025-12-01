using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Identity.Roles;

namespace ReSys.Core.Domain.Identity.Users.Roles;

/// <summary>
/// Represents the many-to-many relationship between User and Role.
/// Inherits from IdentityUserRole for ASP.NET Core Identity integration.
/// Implements IAssignable for auditable assignment tracking.
/// </summary>
public sealed class UserRole : IdentityUserRole<string>, IHasAssignable, IHasDomainEvents
{
    #region Constraints

    public static class Constraints
    {
        public const int MinRolePerUser = 1;
        public const int MaxRolePerUser = 10;
        public const int MinUsersPerRole = 0;
        public const int MaxUsersPerRole = 1000;
        public const string UserIdPattern = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error MaxRolesExceeded => Error.Validation(
            code: $"{nameof(UserRole)}.MaxRolesExceeded",
            description: $"User cannot have more than {Constraints.MaxRolePerUser} roles assigned");

        public static Error MaxUsersExceeded => Error.Validation(
            code: $"{nameof(UserRole)}.MaxUsersExceeded",
            description: $"Role cannot be assigned to more than {Constraints.MaxUsersPerRole} users");

        public static Error AlreadyAssigned(string roleName) => Error.Conflict(
            code: $"{nameof(UserRole)}.AlreadyAssigned",
            description: $"User already has role '{roleName}'");

        public static Error NotAssigned(string roleName) => Error.Conflict(
            code: $"{nameof(UserRole)}.NotAssigned",
            description: $"User does not have role '{roleName}'");

        public static Error AssignmentFailed(string roleName) => Error.Failure(
            code: $"{nameof(UserRole)}.AssignmentFailed",
            description: $"Failed to assign role '{roleName}' to user");

        public static Error RemovalFailed(string roleName) => Error.Failure(
            code: $"{nameof(UserRole)}.RemovalFailed",
            description: $"Failed to remove role '{roleName}' from user");

        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: $"{nameof(UserRole)}.UnexpectedError",
            description: $"An unexpected error occurred while {operation} the user");
    }

    #endregion

    #region Properties

    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? AssignedTo { get; set; }

    #endregion

    #region Relationships
    public Role Role { get; set; } = null!;
    public ApplicationUser ApplicationUser { get; set; } = null!;

    #endregion

    #region Factory Methods

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

    public ErrorOr<UserRole> Assign(ApplicationUser applicationUser, Role role, string? assignedBy = null)
    {
        // Check constraints
        if (applicationUser.UserRoles.Count >= Constraints.MaxRolePerUser)
            return Errors.MaxRolesExceeded;
        if (role.UserRoles.Count >= Constraints.MaxUsersPerRole)
            return Errors.MaxUsersExceeded;
        if (applicationUser.UserRoles.Any(predicate: ur => ur.RoleId == role.Id))
            return Errors.AlreadyAssigned(roleName: role.Name ?? "Unknown");

        UserId = applicationUser.Id;
        RoleId = role.Id;
        ApplicationUser = applicationUser;
        Role = role;

       this.MarkAsAssigned(assignedTo: applicationUser.Id,
            assignedBy: assignedBy);

        AddDomainEvent(domainEvent: new Events.Assigned(UserId: UserId,
            RoleId: RoleId));
        return this;
    }

    public ErrorOr<Deleted> Unassign()
    {
        string roleName = Role.Name ?? "Unknown";
        Unassign();

        AddDomainEvent(domainEvent: new Events.Unassigned(UserId: UserId,
            RoleId: RoleId,
            RoleName: roleName));
        return Result.Deleted;
    }

    #endregion

    #region Events

    public static class Events
    {
        public sealed record Assigned(string UserId, string RoleId) : DomainEvent;
        public sealed record Unassigned(string UserId, string RoleId, string RoleName) : DomainEvent;
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