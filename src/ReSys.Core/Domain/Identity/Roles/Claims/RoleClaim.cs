using System.Text.RegularExpressions;

using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Domain.Identity.Roles.Claims;

/// <summary>
/// Represents a claim (a piece of information) that is assigned to a specific role within the
/// ASP.NET Core Identity system. This class extends the default <see cref="IdentityRoleClaim{TKey}"/>
/// to include additional auditing and assignment tracking capabilities.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Fine-grained Authorization</term>
/// <description>Claims can be used to define specific permissions or attributes granted to users who possess a certain role.</description>
/// </item>
/// <item>
/// <term>Policy-Based Authorization</term>
/// <description>Enables dynamic authorization checks based on the presence and value of claims, rather than static role names.</description>
/// </item>
/// <item>
/// <term>Extensibility</term>
/// <description>Allows for custom data to be associated with roles, beyond just the role name.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>RoleId</term>
/// <description>The ID of the <see cref="Role"/> to which this claim belongs.</description>
/// </item>
/// <item>
/// <term>ClaimType</term>
/// <description>The type of claim (e.g., "Permission", "Department").</description>
/// </item>
/// <item>
/// <term>ClaimValue</term>
/// <description>The value of the claim (e.g., "admin.users.create", "HR").</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasAssignable</strong> - For tracking who assigned the claim and when.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class RoleClaim : IdentityRoleClaim<string>, IHasAssignable
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="RoleClaim"/> properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Maximum number of claims that can be assigned to a single role.</summary>
        public const int MaxClaimsPerRole = 100;
        /// <summary>Minimum length allowed for a claim type string.</summary>
        public const int MinClaimTypeLength = CommonInput.Constraints.Text.MinLength;
        /// <summary>Maximum length allowed for a claim type string.</summary>
        public const int MaxClaimTypeLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        /// <summary>Maximum length allowed for a claim value string.</summary>
        public const int MaxClaimValueLength = CommonInput.Constraints.Text.MediumTextMaxLength;
        /// <summary>Regex pattern for validating claim types.</summary>
        public const string ClaimTypePattern = @"^[a-zA-Z0-9:_-]{1,256}$";
        /// <summary>Regex pattern for validating RoleId (GUID format).</summary>
        public const string RoleIdPattern = CommonInput.Constraints.Identifiers.GuidPattern;

        /// <summary>
        /// Maximum number of users that are expected to be affected by a single permission change.
        /// This is a heuristic value that might influence event processing or UI warnings.
        /// </summary>
        public const int MaxUsersAffectedByPermissionChange = 1000;
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="RoleClaim"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a role has exceeded the maximum allowed number of claims.
        /// </summary>
        public static Error MaxClaimsExceeded => Error.Validation(
            code: $"{nameof(RoleClaim)}.MaxClaimsExceeded",
            description: $"Role cannot have more than {Constraints.MaxClaimsPerRole} claims assigned");

        /// <summary>
        /// Error indicating that a claim of the specified type is already assigned to the role.
        /// </summary>
        /// <param name="claimType">The type of claim that is already assigned.</param>
        public static Error AlreadyAssigned(string claimType) => Error.Conflict(
            code: $"{nameof(RoleClaim)}.AlreadyAssigned",
            description: $"Claim type '{claimType}' is already assigned to the role");

        /// <summary>
        /// Error indicating that a claim of the specified type is not assigned to the role.
        /// </summary>
        /// <param name="claimType">The type of claim that was expected to be assigned.</param>
        public static Error NotAssigned(string claimType) => Error.Conflict(
            code: $"{nameof(RoleClaim)}.NotAssigned",
            description: $"Claim type '{claimType}' is not assigned to the role");

        /// <summary>
        /// Error indicating a general failure during claim assignment.
        /// </summary>
        /// <param name="claimType">The type of claim that failed to be assigned.</param>
        public static Error AssignmentFailed(string claimType) => Error.Failure(
            code: $"{nameof(RoleClaim)}.AssignmentFailed",
            description: $"Failed to assign claim '{claimType}' to role");

        /// <summary>
        /// Error indicating a general failure during claim removal.
        /// </summary>
        /// <param name="claimType">The type of claim that failed to be removed.</param>
        public static Error RemovalFailed(string claimType) => Error.Failure(
            code: $"{nameof(RoleClaim)}.RemovalFailed",
            description: $"Failed to remove claim '{claimType}' from role");

        /// <summary>
        /// Occurs when an unexpected error happens during a <see cref="RoleClaim"/> operation.
        /// </summary>
        /// <param name="operation">A descriptive string indicating the operation during which the error occurred.</param>
        public static Error UnexpectedError(string operation) => Error.Unexpected(
            code: $"{nameof(RoleClaim)}.UnexpectedError",
            description: $"An unexpected error occurred while {operation} the role");
    }

    #endregion

    #region Properties

    /// <summary>
    /// Gets or sets the UTC timestamp when this claim was assigned to the role.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that assigned this claim.
    /// </summary>
    public string? AssignedBy { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the role to which this claim is assigned. (Redundant with RoleId property from IdentityRoleClaim)
    /// </summary>
    public string? AssignedTo { get; set; }

    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="Role"/> to which this claim is assigned.
    /// </summary>
    public Role Role { get; set; } = null!;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="RoleClaim"/> instance.
    /// This method performs validation on the provided <paramref name="roleId"/>, <paramref name="claimType"/>, and <paramref name="claimValue"/>.
    /// </summary>
    /// <param name="roleId">The unique identifier of the <see cref="Role"/> to which the claim will be assigned.</param>
    /// <param name="claimType">The type of the claim (e.g., "Permission", "Department").</param>
    /// <param name="claimValue">The value of the claim (e.g., "admin.users.create", "HR"). Optional.</param>
    /// <param name="assignedBy">The identifier of the user or system assigning this claim. Optional.</param>
    /// <returns>
    /// An <see cref="ErrorOr{RoleClaim}"/> result.
    /// Returns the newly created <see cref="RoleClaim"/> instance on success.
    /// Returns one of the <see cref="CommonInput.Errors"/> or internal <see cref="Errors"/> if validation fails.
    /// </returns>
    /// <remarks>
    /// This method ensures the provided identifiers and claim data adhere to defined constraints.
    /// It automatically marks the claim as assigned with the current UTC timestamp.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// string roleId = Guid.NewGuid().ToString(); // Placeholder for an existing Role ID
    /// var roleClaimResult = RoleClaim.Create(
    ///     roleId: roleId,
    ///     claimType: "Permission",
    ///     claimValue: "catalog.products.view",
    ///     assignedBy: "adminUser123");
    /// 
    /// if (roleClaimResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating RoleClaim: {roleClaimResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newRoleClaim = roleClaimResult.Value;
    ///     Console.WriteLine($"RoleClaim '{newRoleClaim.ClaimType}' created for Role {newRoleClaim.RoleId}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static ErrorOr<RoleClaim> Create(string roleId, string claimType, string? claimValue = null, string? assignedBy = null)
    {
        // Validate roleId
        if (string.IsNullOrWhiteSpace(roleId) || !Regex.IsMatch(roleId, Constraints.RoleIdPattern))
            return Error.Validation(code: "RoleClaim.InvalidRoleId", description: "Invalid Role ID format.");

        // Validate claimType
        if (string.IsNullOrWhiteSpace(claimType))
            return CommonInput.Errors.Required(prefix: nameof(RoleClaim), field: nameof(ClaimType));
        
        string trimmedClaimType = claimType.Trim();
        if (trimmedClaimType.Length < Constraints.MinClaimTypeLength)
            return CommonInput.Errors.TooShort(prefix: nameof(RoleClaim), field: nameof(ClaimType), minLength: Constraints.MinClaimTypeLength);
        if (trimmedClaimType.Length > Constraints.MaxClaimTypeLength)
            return CommonInput.Errors.TooLong(prefix: nameof(RoleClaim), field: nameof(ClaimType), maxLength: Constraints.MaxClaimTypeLength);
        if (!Regex.IsMatch(trimmedClaimType, Constraints.ClaimTypePattern))
            return Error.Validation(code: "RoleClaim.InvalidClaimType", description: "Invalid Claim Type format.");

        // Validate claimValue
        if (!string.IsNullOrWhiteSpace(claimValue) && claimValue.Length > Constraints.MaxClaimValueLength)
            return CommonInput.Errors.TooLong(prefix: nameof(RoleClaim), field: nameof(ClaimValue), maxLength: Constraints.MaxClaimValueLength);

        var roleClaim = new RoleClaim
        {
            RoleId = roleId,
            ClaimType = trimmedClaimType,
            ClaimValue = claimValue?.Trim(),
            AssignedAt = DateTimeOffset.UtcNow,
            AssignedBy = assignedBy
        };
        
        roleClaim.MarkAsAssigned(roleId, assignedBy);

        return roleClaim;
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Assigns this <see cref="RoleClaim"/> to a specified <see cref="Role"/>.
    /// This method performs validation checks before assigning the claim.
    /// </summary>
    /// <param name="role">The <see cref="Role"/> instance to which the claim is being assigned.</param>
    /// <param name="claimType">The type of the claim (e.g., "Permission").</param>
    /// <param name="claimValue">The value of the claim (e.g., "catalog.products.view"). Optional.</param>
    /// <param name="assignedBy">The identifier of the user or system assigning this claim. Optional.</param>
    /// <returns>
    /// An <see cref="ErrorOr{RoleClaim}"/> result.
    /// Returns the updated <see cref="RoleClaim"/> instance on successful assignment.
    /// Returns <see cref="Errors.UnexpectedError(string)"/> if the <paramref name="role"/> is null.
    /// Returns <see cref="CommonInput.Errors.Required"/> if <paramref name="claimType"/> is missing.
    /// Returns <see cref="Errors.MaxClaimsExceeded"/> if the role already has too many claims.
    /// Returns <see cref="Errors.AlreadyAssigned(string)"/> if the claim type is already assigned.
    /// </returns>
    /// <remarks>
    /// This method ensures that the role does not exceed its maximum claim limit and that the claim type is not duplicated within the role.
    /// It updates the claim's <c>RoleId</c>, <c>ClaimType</c>, <c>ClaimValue</c>, and marks it as assigned.
    /// </remarks>
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

    /// <summary>
    /// Removes this <see cref="RoleClaim"/> from its associated role.
    /// This method signals that the claim should no longer be assigned.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful removal.
    /// </returns>
    /// <remarks>
    /// This method updates the claim's assignment status. The actual removal from the
    /// role's collection is typically handled by the parent <see cref="Role"/> aggregate.
    /// </remarks>
    public ErrorOr<Deleted> Remove()
    {
        this.MarkAsUnassigned();

        return Result.Deleted;
    }
    #endregion
}