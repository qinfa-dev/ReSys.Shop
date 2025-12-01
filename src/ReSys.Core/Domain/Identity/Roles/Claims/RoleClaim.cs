using System.Text.RegularExpressions;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Domain.Identity.Roles.Claims;

/// <summary>
/// Represents a claim assigned to a role in ASP.NET Core Identity.
/// Inherits from IdentityRoleClaim for integration and IAssignable for tracking.
/// </summary>
public sealed class RoleClaim : IdentityRoleClaim<string>, IHasAssignable
{
    #region Constraints

    public static class Constraints
    {
        public const int MaxClaimsPerRole = 100;
        public const int MinClaimTypeLength = CommonInput.Constraints.Text.MinLength;
        public const int MaxClaimTypeLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        public const int MaxClaimValueLength = CommonInput.Constraints.Text.MediumTextMaxLength;
        public const string ClaimTypePattern = @"^[a-zA-Z0-9:_-]{1,256}$";
        public const string RoleIdPattern = CommonInput.Constraints.Identifiers.GuidPattern;

        public const int MaxUsersAffectedByPermissionChange = 1000;
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error MaxClaimsExceeded => Error.Validation(
            code: $"{nameof(RoleClaim)}.MaxClaimsExceeded",
            description: $"Role cannot have more than {Constraints.MaxClaimsPerRole} claims assigned");

        public static Error AlreadyAssigned(string claimType) => Error.Conflict(
            code: $"{nameof(RoleClaim)}.AlreadyAssigned",
            description: $"Claim type '{claimType}' is already assigned to the role");

        public static Error NotAssigned(string claimType) => Error.Conflict(
            code: $"{nameof(RoleClaim)}.NotAssigned",
            description: $"Claim type '{claimType}' is not assigned to the role");

        public static Error AssignmentFailed(string claimType) => Error.Failure(
            code: $"{nameof(RoleClaim)}.AssignmentFailed",
            description: $"Failed to assign claim '{claimType}' to role");

        public static Error RemovalFailed(string claimType) => Error.Failure(
            code: $"{nameof(RoleClaim)}.RemovalFailed",
            description: $"Failed to remove claim '{claimType}' from role");

        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: $"{nameof(RoleClaim)}.UnexpectedError",
            description: $"An unexpected error occurred while {operation} the role");
    }

    #endregion

    #region Properties

    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? AssignedTo { get; set; }

    #endregion

    #region Relationships
    public Role Role { get; set; } = null!;

    #endregion

    #region Factory Methods

    public static ErrorOr<RoleClaim> Create(string roleId, string claimType, string? claimValue = null, string? assignedBy = null)
    {
        // Validate role ID
        if (string.IsNullOrWhiteSpace(value: roleId))
            return CommonInput.Errors.Required(prefix: nameof(RoleClaim),
                field: nameof(RoleId));
        if (!Regex.IsMatch(input: roleId,
                pattern: Constraints.RoleIdPattern))
            return CommonInput.Errors.InvalidGuid(prefix: nameof(RoleClaim),
                field: nameof(RoleId));

        // Validate claim type
        if (string.IsNullOrWhiteSpace(value: claimType))
            return CommonInput.Errors.Required(prefix: nameof(RoleClaim),
                field: nameof(ClaimType));
        string trimmedClaimType = claimType.Trim();
        if (trimmedClaimType.Length < Constraints.MinClaimTypeLength)
            return CommonInput.Errors.TooShort(prefix: nameof(RoleClaim),
                field: nameof(ClaimType),
                minLength: Constraints.MinClaimTypeLength);
        if (trimmedClaimType.Length > Constraints.MaxClaimTypeLength)
            return CommonInput.Errors.TooLong(prefix: nameof(RoleClaim),
                field: nameof(ClaimType),
                maxLength: Constraints.MaxClaimTypeLength);
        if (!Regex.IsMatch(input: trimmedClaimType,
                pattern: Constraints.ClaimTypePattern))
            return Errors.UnexpectedError(operation: "invalid claim type format");

        // Validate claim value
        if (!string.IsNullOrWhiteSpace(value: claimValue) && claimValue.Length > Constraints.MaxClaimValueLength)
            return CommonInput.Errors.TooLong(prefix: nameof(RoleClaim),
                field: nameof(ClaimValue),
                maxLength: Constraints.MaxClaimValueLength);

        RoleClaim roleClaim = new()
        {
            RoleId = roleId,
            ClaimType = trimmedClaimType,
            ClaimValue = claimValue?.Trim()
        };

        roleClaim.MarkAsAssigned(assignedTo: roleId,
            assignedBy: assignedBy);

        return roleClaim;
    }

    #endregion

    #region Business Logic

    public ErrorOr<RoleClaim> Assign(Role? role, string claimType, string? claimValue = null, string? assignedBy = null)
    {
        // Validate role
        if (role == null)
            return Errors.UnexpectedError(operation: "assigning claim to null role");

        // Validate claim type
        if (string.IsNullOrWhiteSpace(value: claimType))
            return CommonInput.Errors.Required(prefix: nameof(RoleClaim),
                field: nameof(ClaimType));
        string trimmedClaimType = claimType.Trim();
        if (trimmedClaimType.Length < Constraints.MinClaimTypeLength)
            return CommonInput.Errors.TooShort(prefix: nameof(RoleClaim),
                field: nameof(ClaimType),
                minLength: Constraints.MinClaimTypeLength);
        if (trimmedClaimType.Length > Constraints.MaxClaimTypeLength)
            return CommonInput.Errors.TooLong(prefix: nameof(RoleClaim),
                field: nameof(ClaimType),
                maxLength: Constraints.MaxClaimTypeLength);
        if (!Regex.IsMatch(input: trimmedClaimType,
                pattern: Constraints.ClaimTypePattern))
            return Errors.UnexpectedError(operation: "invalid claim type format");

        // Validate claim value
        if (!string.IsNullOrWhiteSpace(value: claimValue) && claimValue.Length > Constraints.MaxClaimValueLength)
            return CommonInput.Errors.TooLong(prefix: nameof(RoleClaim),
                field: nameof(ClaimValue),
                maxLength: Constraints.MaxClaimValueLength);

        // Check constraints
        if (role.RoleClaims.Count >= Constraints.MaxClaimsPerRole)
            return Errors.MaxClaimsExceeded;
        if (role.RoleClaims.Any(predicate: rc => rc.ClaimType == trimmedClaimType))
            return Errors.AlreadyAssigned(claimType: trimmedClaimType);

        RoleId = role.Id;
        ClaimType = trimmedClaimType;
        ClaimValue = claimValue?.Trim();
        Role = role;

        this.MarkAsAssigned(assignedTo: role.Id,
            assignedBy: assignedBy);

        return this;
    }

    public ErrorOr<Deleted> Remove()
    {
        this.MarkAsUnassigned();

        return Result.Deleted;
    }
    #endregion
}