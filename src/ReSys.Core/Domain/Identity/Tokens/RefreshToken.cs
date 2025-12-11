using System.Security.Cryptography;
using System.Text;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Domain.Identity.Tokens;

/// <summary>
/// Represents a long-lived refresh token used to obtain new, short-lived JSON Web Tokens (JWTs)
/// without requiring the user to re-authenticate with their credentials.
/// This entity is crucial for maintaining persistent user sessions and enhancing security.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Session Management</term>
/// <description>Allows users to remain logged in across sessions without repeatedly entering credentials.</description>
/// </item>
/// <item>
/// <term>Security Enhancement</term>
/// <description>Decouples the long-lived refresh token from the short-lived access token (JWT),
/// reducing the impact of compromised access tokens.</description>
/// </item>
/// <item>
/// <term>Token Rotation</term>
/// <description>Supports refreshing both access and refresh tokens, invalidating old ones.</description>
/// </item>
/// <item>
/// <term>Auditing</term>
/// <description>Tracks creation, expiration, and revocation details including IP addresses.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>TokenHash</term>
/// <description>A cryptographic hash of the raw token, never storing the raw token itself.</description>
/// </item>
/// <item>
/// <term>ExpiresAt</term>
/// <description>The UTC time when the token becomes invalid.</description>
/// </item>
/// <item>
/// <term>RevokedAt</term>
/// <description>The UTC time when the token was explicitly invalidated.</description>
/// </item>
/// <item>
/// <term>TokenFamily</term>
/// <description>Used to group related refresh tokens (for rotation scenarios).</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Concerns Implemented:</strong>
/// <list type="bullet">
/// <item><strong>IHasAssignable</strong> - For tracking who assigned the token and when.</item>
/// </list>
/// </para>
/// </remarks>
public sealed class RefreshToken : AuditableEntity<Guid>, IHasAssignable
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="RefreshToken"/> properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// The number of random bytes used to generate a refresh token.
        /// Increased to 64 bytes (512 bits) for better cryptographic security.
        /// </summary>
        public const int TokenBytes = 64;
        /// <summary>Maximum allowed length for an IP address string.</summary>
        public const int IpAddressLength = 45; // Supports IPv4 (max 15 chars) and IPv6 (max 45 chars)
        /// <summary>Regex pattern for validating IP address format (IPv4 or IPv6).</summary>
        public const string IpAddressAllowedPattern = @"^(([0-9]{1,3}\.){3}[0-9]{1,3}|([a-fA-F0-9:]+))$";
    }

    #endregion

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="RefreshToken"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>Error indicating that a requested refresh token could not be found.</summary>
        public static Error RefreshTokenNotFound => Error.NotFound(code: "RefreshToken.NotFound",
            description: "Refresh token not found");
        /// <summary>Error indicating that the refresh token has expired.</summary>
        public static Error Expired => Error.Validation(code: "RefreshToken.Expired",
            description: "Refresh token has expired");
        /// <summary>Error indicating that the refresh token has been revoked.</summary>
        public static Error Revoked => Error.Validation(code: "RefreshToken.Revoked",
            description: "Refresh token has been revoked");
        /// <summary>Error indicating a general failure during refresh token generation.</summary>
        public static Error GenerationFailed => Error.Failure(code: "RefreshToken.GenerationFailed",
            description: "Failed to generate refresh token");
        /// <summary>Error indicating a failure during refresh token rotation.</summary>
        public static Error RotationFailed => Error.Failure(code: "RefreshToken.RotationFailed",
            description: "Failed to rotate refresh token");
        /// <summary>Error indicating a failure during refresh token revocation.</summary>
        public static Error RevocationFailed => Error.Failure(code: "RefreshToken.RevocationFailed",
            description: "Failed to revoke refresh token");
        /// <summary>Error indicating a general failure during refresh token validation.</summary>
        public static Error ValidationFailed => Error.Failure(code: "RefreshToken.ValidationFailed",
            description: "Failed to validate refresh token");
    }

    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the unique identifier of the user to whom this refresh token belongs.
    /// </summary>
    public string UserId { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the cryptographic hash of the raw refresh token string.
    /// The raw token is never stored directly, only its hash for security.
    /// </summary>
    public string TokenHash { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the UTC timestamp when this refresh token expires.
    /// </summary>
    public DateTimeOffset ExpiresAt { get; set; }
    /// <summary>
    /// Gets or sets the IP address from which this refresh token was created.
    /// </summary>
    public string CreatedByIp { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the UTC timestamp when this refresh token was explicitly revoked.
    /// Null if the token is still active.
    /// </summary>
    public DateTimeOffset? RevokedAt { get; set; }
    /// <summary>
    /// Gets or sets the IP address from which this refresh token was revoked.
    /// </summary>
    public string? RevokedByIp { get; set; }
    /// <summary>
    /// Gets or sets the reason for the token's revocation.
    /// </summary>
    public string? RevokedReason { get; set; }

    /// <summary>
    /// Gets or sets the identifier for a token family.
    /// This is used to group related refresh tokens, enabling advanced security scenarios like automatic
    /// invalidation of old tokens in a family upon rotation.
    /// </summary>
    public string? TokenFamily { get; set; }

    #region IAssignable Properties
    /// <summary>
    /// Gets or sets the UTC timestamp when this token was assigned.
    /// </summary>
    public DateTimeOffset? AssignedAt { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the user or system that assigned this token.
    /// </summary>
    public string? AssignedBy { get; set; }
    /// <summary>
    /// Gets or sets the identifier of the entity to which this token was assigned (i.e., the UserId).
    /// </summary>
    public string? AssignedTo { get; set; }
    #endregion

    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the navigation property to the <see cref="User"/> who owns this refresh token.
    /// </summary>
    public User User { get; set; } = null!;

    #endregion

    #region Computed Properties

    /// <summary>
    /// Indicates whether the refresh token has expired based on its <see cref="ExpiresAt"/> timestamp.
    /// </summary>
    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    /// <summary>
    /// Indicates whether the refresh token has been explicitly revoked (i.e., <see cref="RevokedAt"/> has a value).
    /// </summary>
    public bool IsRevoked => RevokedAt.HasValue;

    #endregion

    #region Factory Methods

    /// <summary>
    /// Factory method to create a new <see cref="RefreshToken"/> instance.
    /// It generates a cryptographically secure token hash and sets expiration details.
    /// </summary>
    /// <param name="user">The <see cref="User"/> to whom this refresh token will be issued.</param>
    /// <param name="token">The raw token string. If null or empty, a random token will be generated.</param>
    /// <param name="lifetime">The duration for which the refresh token will be valid from its creation time.</param>
    /// <param name="ipAddress">The IP address from which the token creation request originated.</param>
    /// <param name="assignedBy">Optional: The identifier of the user or system that assigned this token.</param>
    /// <param name="tokenFamily">Optional: An identifier to group related tokens for rotation. If null, a new family ID is generated.</param>
    /// <returns>
    /// An <see cref="ErrorOr{RefreshToken}"/> result.
    /// Returns the newly created <see cref="RefreshToken"/> instance on success.
    /// Returns <see cref="Errors.GenerationFailed"/> if an unexpected error occurs during token generation.
    /// </returns>
    /// <remarks>
    /// The raw token is hashed before storage; only the <see cref="TokenHash"/> is persisted.
    /// The token is marked as assigned (<see cref="IHasAssignable"/> concern) with the current UTC timestamp.
    /// </remarks>
    public static ErrorOr<RefreshToken> Create(
        User user,
        string token,
        TimeSpan lifetime,
        string ipAddress,
        string? assignedBy = null,
        string? tokenFamily = null)
    {
        try
        {
            string rawToken = string.IsNullOrEmpty(value: token) ? GenerateRandomToken() : token;
            DateTimeOffset now = DateTimeOffset.UtcNow;

            RefreshToken refreshToken = new()
            {
                Id = Guid.NewGuid(),
                UserId = user.Id,
                TokenHash = Hash(rawToken: rawToken),
                CreatedAt = now,
                CreatedBy = assignedBy,
                CreatedByIp = ipAddress.Trim(),
                ExpiresAt = now.Add(timeSpan: lifetime),
                User = user,
                TokenFamily = tokenFamily ?? Guid.NewGuid().ToString() // Generate new family if not provided
            };

            refreshToken.MarkAsAssigned(assignedTo: user.Id, assignedBy: assignedBy);

            return refreshToken;
        }
        catch (Exception)
        {
            return Errors.GenerationFailed;
        }
    }

    #endregion

    #region Business Logic

    // REMOVED: Rotate method - rotation should be handled by service layer
    // This ensures proper transaction handling and token family management

    /// <summary>
    /// Explicitly revokes this refresh token, making it unusable even if not expired.
    /// </summary>
    /// <param name="ipAddress">The IP address from which the revocation request originated.</param>
    /// <param name="reason">Optional: A reason for the revocation (e.g., "compromised", "user logout").</param>
    /// <returns>
    /// An <see cref="ErrorOr{RefreshToken}"/> result.
    /// Returns the updated <see cref="RefreshToken"/> instance on successful revocation.
    /// Returns the current token if it is already revoked (idempotent).
    /// Returns <see cref="Errors.RevocationFailed"/> if an unexpected error occurs during revocation.
    /// </returns>
    /// <remarks>
    /// Revocation marks the token with a <see cref="RevokedAt"/> timestamp, <see cref="RevokedByIp"/> address,
    /// and optional <see cref="RevokedReason"/>.
    /// </remarks>
    public ErrorOr<RefreshToken> Revoke(string ipAddress, string? reason = null)
    {
        if (IsRevoked)
            return this; // FIXED: Return success for idempotency instead of error

        try
        {
            RevokedAt = DateTimeOffset.UtcNow;
            RevokedByIp = ipAddress.Trim();
            RevokedReason = reason?.Trim();

            return this;
        }
        catch (Exception)
        {
            return Errors.RevocationFailed;
        }
    }

    // REMOVED: Validate method - validation should be in service layer
    // Domain entities should not perform hash comparisons

    #endregion

    #region Static Helpers

    /// <summary>
    /// Generates a cryptographically secure random token string suitable for use as a refresh token.
    /// The token is Base64url encoded for safe transmission.
    /// </summary>
    /// <returns>A randomly generated, URL-safe Base64 string.</returns>
    public static string GenerateRandomToken()
    {
        // FIXED: Use cryptographically secure random bytes and proper encoding
        byte[] bytes = RandomNumberGenerator.GetBytes(count: Constraints.TokenBytes);
        // Use URL-safe Base64 encoding
        return Convert.ToBase64String(inArray: bytes)
            .TrimEnd(trimChar: '=')
            .Replace(oldChar: '+',
                newChar: '-')
            .Replace(oldChar: '/',
                newChar: '_');
    }

    /// <summary>
    /// Hashes a raw token string using SHA512 algorithm.
    /// This hash is stored in the database instead of the raw token for security purposes.
    /// </summary>
    /// <param name="rawToken">The unhashed, plain-text token string.</param>
    /// <returns>The Base64 encoded SHA512 hash of the raw token.</returns>
    public static string Hash(string rawToken)
    {
        using SHA512 sha = SHA512.Create();
        byte[] bytes = sha.ComputeHash(buffer: Encoding.UTF8.GetBytes(s: rawToken));
        return Convert.ToBase64String(inArray: bytes);
    }

    #endregion
}