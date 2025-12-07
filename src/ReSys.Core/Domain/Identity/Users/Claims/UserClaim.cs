using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Common.Domain.Concerns;

namespace ReSys.Core.Domain.Identity.Users.Claims;

/// <summary>
/// Represents a claim (a piece of information) that is assigned to a specific user within the
/// ASP.NET Core Identity system. This class extends the default <see cref="IdentityUserClaim{TKey}"/>
/// to include additional auditing and assignment tracking capabilities.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Fine-grained Authorization</term>
/// <description>Claims can be used to define specific permissions or attributes directly granted to a user, independent of their roles.</description>
/// </item>
/// <item>
/// <term>Policy-Based Authorization</term>
/// <description>Enables dynamic authorization checks based on the presence and value of claims associated with the authenticated user.</description>
/// </item>
/// <item>
/// <term>User Customization</term>
/// <description>Allows for custom data to be associated with a user, beyond their basic profile information (e.g., preferences, external system IDs).</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>UserId</term>
/// <description>The ID of the <see cref="User"/> to whom this claim belongs.</description>
/// </item>
/// <item>
/// <term>ClaimType</term>
/// <description>The type of claim (e.g., "FavoriteColor", "LastLoginIP").</description>
/// </item>
/// <item>
/// <term>ClaimValue</term>
/// <description>The value of the claim (e.g., "blue", "192.168.1.1").</description>
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
public class UserClaim : IdentityUserClaim<string>, IHasAssignable
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="UserClaim"/> properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Maximum number of claims that can be assigned to a single user.</summary>
        public const int MaxClaimsPerUser = 100;
        /// <summary>Minimum length allowed for a claim type string.</summary>
        public const int MinClaimTypeLength = 1;
        /// <summary>Maximum length allowed for a claim type string.</summary>
        public const int MaxClaimTypeLength = 256;
        /// <summary>Maximum length allowed for a claim value string.</summary>
        public const int MaxClaimValueLength = 1000;
    }
    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="UserClaim"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a user has exceeded the maximum allowed number of claims.
        /// </summary>
        public static Error MaxClaimsExceeded => Error.Validation(
            code: "UserClaim.MaxClaimsExceeded",
            description: $"User cannot have more than {Constraints.MaxClaimsPerUser} claims assigned");

        /// <summary>
        /// Error indicating that a claim of the specified type is already assigned to the user.
        /// </summary>
        /// <param name="claimType">The type of claim that is already assigned.</param>
        public static Error AlreadyAssigned(string claimType) => Error.Conflict(
            code: "UserClaim.AlreadyAssigned",
            description: $"Claim type '{claimType}' is already assigned to the user");

        /// <summary>
        /// Error indicating that a claim of the specified type is not assigned to the user.
        /// </summary>
        /// <param name="claimType">The type of claim that was expected to be assigned.</param>
        public static Error NotAssigned(string claimType) => Error.Conflict(
            code: "UserClaim.NotAssigned",
            description: $"Claim type '{claimType}' is not assigned to the user");

        /// <summary>
        /// Error indicating a general failure during claim assignment to a user.
        /// </summary>
        public static Error AssignmentFailed => Error.Failure(
            code: "UserClaim.AssignmentFailed",
            description: $"Failed to assign claims' to user");

        /// <summary>
        /// Error indicating a general failure during claim removal from a user.
        /// </summary>
        /// <param name="claimType">The type of claim that failed to be removed.</param>
        public static Error RemovalFailed(string claimType) => Error.Failure(
            code: "UserClaim.RemovalFailed",
            description: $"Failed to remove claim '{claimType}' from user");

    }

    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the UTC timestamp when this claim was assigned to the user.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that assigned this claim.
    /// </summary>
    public string? AssignedBy { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the entity to which this claim is assigned (i.e., the UserId).
    /// </summary>
    public string? AssignedTo { get; set; }
    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the navigation property to the <see cref="User"/> to whom this claim is assigned.
    /// </summary>
    public User User { get; set; } = null!;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="UserClaim"/> instance.
    /// This method performs basic trimming of claim type and value.
    /// </summary>
    /// <param name="userId">The unique identifier of the <see cref="User"/> to whom the claim will be assigned.</param>
    /// <param name="claimType">The type of the claim (e.g., "FavoriteColor").</param>
    /// <param name="claimValue">The value of the claim (e.g., "blue"). Optional.</param>
    /// <param name="assignedBy">The identifier of the user or system assigning this claim. Optional.</param>
    /// <returns>
    /// An <see cref="ErrorOr{UserClaim}"/> result.
    /// Returns the newly created <see cref="UserClaim"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method ensures the provided identifiers and claim data adhere to defined constraints implicitly through trimming.
    /// It automatically marks the claim as assigned with the current UTC timestamp.
    /// Further validation (e.g., claim type length, uniqueness per user) is typically handled at a higher level (e.g., a domain service or application service)
    /// before persistence or during assignment to a user aggregate.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// string userId = Guid.NewGuid().ToString(); // Placeholder for an existing User ID
    /// var userClaimResult = UserClaim.Create(
    ///     userId: userId,
    ///     claimType: "LanguagePreference",
    ///     claimValue: "en-US",
    ///     assignedBy: "system_init");
    /// 
    /// if (userClaimResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating UserClaim: {userClaimResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newUserClaim = userClaimResult.Value;
    ///     Console.WriteLine($"UserClaim '{newUserClaim.ClaimType}' created for User {newUserClaim.UserId}.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
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

    /// <summary>
    /// Assigns this <see cref="UserClaim"/> to a specified <see cref="User"/>.
    /// This method performs validation checks to ensure the claim can be assigned to the user.
    /// </summary>
    /// <param name="user">The <see cref="User"/> instance to which the claim is being assigned.</param>
    /// <param name="claimType">The type of the claim (e.g., "FavoriteColor").</param>
    /// <param name="claimValue">The value of the claim (e.g., "blue"). Optional.</param>
    /// <param name="assignedBy">The identifier of the user or system assigning this claim. Optional.</param>
    /// <returns>
    /// An <see cref="ErrorOr{UserClaim}"/> result.
    /// Returns the updated <see cref="UserClaim"/> instance on successful assignment.
    /// Returns <see cref="Errors.MaxClaimsExceeded"/> if the user already has too many claims.
    /// Returns <see cref="Errors.AlreadyAssigned(string)"/> if the claim type is already assigned to the user.
    /// </returns>
    /// <remarks>
    /// This method ensures that the user does not exceed their maximum claim limit and that the claim type is not duplicated for the user.
    /// It updates the claim's <c>UserId</c>, <c>ClaimType</c>, <c>ClaimValue</c>, and marks it as assigned.
    /// </remarks>
    public ErrorOr<UserClaim> Assign(User user, string claimType, string? claimValue = null, string? assignedBy = null)
    {
        string trimmedClaimType = claimType.Trim();

        // Check constraints
        if (user.Claims.Count >= Constraints.MaxClaimsPerUser)
            return Errors.MaxClaimsExceeded;
        if (user.Claims.Any(predicate: uc => uc.ClaimType == trimmedClaimType))
            return Errors.AlreadyAssigned(claimType: trimmedClaimType);

        UserId = user.Id;
        ClaimType = trimmedClaimType;
        ClaimValue = claimValue?.Trim();
        User = user;

        this.MarkAsAssigned(assignedTo: user.Id,
            assignedBy: assignedBy);

        return this;
    }

    /// <summary>
    /// Removes this <see cref="UserClaim"/> from its associated user.
    /// This method signals that the claim should no longer be assigned.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Returns <see cref="Result.Deleted"/> on successful removal.
    /// </returns>
    /// <remarks>
    /// This method updates the claim's assignment status. The actual removal from the
    /// user's collection is typically handled by the parent <see cref="User"/> aggregate.
    /// </remarks>
    public ErrorOr<Deleted> Remove()
    {
        this.MarkAsUnassigned();
        return Result.Deleted;
    }

    #endregion


}