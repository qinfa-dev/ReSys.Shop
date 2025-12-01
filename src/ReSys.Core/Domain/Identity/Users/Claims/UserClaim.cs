using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Domain.Identity.Users.Claims;

/// <summary>
/// Represents a claim assigned to a user in ASP.NET Core Identity.
/// Inherits from IdentityUserClaim for integration and IAssignable for tracking.
/// </summary>
public class UserClaim : IdentityUserClaim<string>, IHasAssignable
{
    #region Constraints

    public static class Constraints
    {
        public const int MaxClaimsPerUser = 100;
        public const int MinClaimTypeLength = 1;
        public const int MaxClaimTypeLength = 256;
        public const int MaxClaimValueLength = 1000;
    }
    #endregion

    #region Errors

    public static class Errors
    {
        public static Error MaxClaimsExceeded => Error.Validation(
            code: "UserClaim.MaxClaimsExceeded",
            description: $"User cannot have more than {Constraints.MaxClaimsPerUser} claims assigned");

        public static Error AlreadyAssigned(string claimType) => Error.Conflict(
            code: "UserClaim.AlreadyAssigned",
            description: $"Claim type '{claimType}' is already assigned to the user");

        public static Error NotAssigned(string claimType) => Error.Conflict(
            code: "UserClaim.NotAssigned",
            description: $"Claim type '{claimType}' is not assigned to the user");

        public static Error AssignmentFailed => Error.Failure(
            code: "UserClaim.AssignmentFailed",
            description: $"Failed to assign claims' to user");

        public static Error RemovalFailed(string claimType) => Error.Failure(
            code: "UserClaim.RemovalFailed",
            description: $"Failed to remove claim '{claimType}' from user");

    }

    #endregion

    #region Properties
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? AssignedTo { get; set; }
    #endregion

    #region Relationships

    public ApplicationUser ApplicationUser { get; set; } = null!;

    #endregion

    #region Factory Methods

    public static ErrorOr<UserClaim> Create(string userId, string claimType, string? claimValue = null, string? assignedBy = null)
    {
        string trimmedClaimType = claimType.Trim();

        UserClaim userClaim = new()
        {
            UserId = userId,
            ClaimType = trimmedClaimType,
            ClaimValue = claimValue?.Trim()
        };

        userClaim.MarkAsAssigned(
            assignedTo: userId,
            assignedBy: assignedBy);

        return userClaim;
    }

    #endregion

    #region Business Logic

    public ErrorOr<UserClaim> Assign(ApplicationUser applicationUser, string claimType, string? claimValue = null, string? assignedBy = null)
    {
        string trimmedClaimType = claimType.Trim();

        // Check constraints
        if (applicationUser.Claims.Count >= Constraints.MaxClaimsPerUser)
            return Errors.MaxClaimsExceeded;
        if (applicationUser.Claims.Any(predicate: uc => uc.ClaimType == trimmedClaimType))
            return Errors.AlreadyAssigned(claimType: trimmedClaimType);

        UserId = applicationUser.Id;
        ClaimType = trimmedClaimType;
        ClaimValue = claimValue?.Trim();
        ApplicationUser = applicationUser;

        this.MarkAsAssigned(assignedTo: applicationUser.Id,
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