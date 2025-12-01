using System.Security.Cryptography;
using System.Text;

using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Domain.Identity.Tokens;

/// <summary>
/// Represents a refresh token for a user, used to obtain new JWTs.
/// </summary>
public sealed class RefreshToken : AuditableEntity<Guid>, IHasAssignable
{
    #region Constraints

    public static class Constraints
    {
        // FIXED: Increased token length for better security (512 bits = 64 bytes)
        public const int TokenBytes = 64;
        public const int IpAddressLength = 45;
        public const string IpAddressAllowedPattern = @"^(([0-9]{1,3}\.){3}[0-9]{1,3}|([a-fA-F0-9:]+))$";
    }

    #endregion

    #region Errors

    public static class Errors
    {
        public static Error RefreshTokenNotFound => Error.NotFound(code: "RefreshToken.NotFound",
            description: "Refresh token not found");
        public static Error Expired => Error.Validation(code: "RefreshToken.Expired",
            description: "Refresh token has expired");
        public static Error Revoked => Error.Validation(code: "RefreshToken.Revoked",
            description: "Refresh token has been revoked");
        public static Error GenerationFailed => Error.Failure(code: "RefreshToken.GenerationFailed",
            description: "Failed to generate refresh token");
        public static Error RotationFailed => Error.Failure(code: "RefreshToken.RotationFailed",
            description: "Failed to rotate refresh token");
        public static Error RevocationFailed => Error.Failure(code: "RefreshToken.RevocationFailed",
            description: "Failed to revoke refresh token");
        public static Error ValidationFailed => Error.Failure(code: "RefreshToken.ValidationFailed",
            description: "Failed to validate refresh token");
    }

    #endregion

    #region Properties
    public string UserId { get; set; } = string.Empty;
    public string TokenHash { get; set; } = string.Empty;
    public DateTimeOffset ExpiresAt { get; set; }
    public string CreatedByIp { get; set; } = string.Empty;
    public DateTimeOffset? RevokedAt { get; set; }
    public string? RevokedByIp { get; set; }
    public string? RevokedReason { get; set; }

    // ADDED: Token family tracking for enhanced security
    public string? TokenFamily { get; set; }

    #region IAssignable Properties
    public DateTimeOffset? AssignedAt { get; set; }
    public string? AssignedBy { get; set; }
    public string? AssignedTo { get; set; }
    #endregion

    #endregion

    #region Relationships

    public ApplicationUser ApplicationUser { get; set; } = null!;

    #endregion

    #region Computed Properties

    public bool IsExpired => DateTimeOffset.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;

    #endregion

    #region Factory Methods

    public static ErrorOr<RefreshToken> Create(
        ApplicationUser applicationUser,
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
                UserId = applicationUser.Id,
                TokenHash = Hash(rawToken: rawToken),
                CreatedAt = now,
                CreatedBy = assignedBy,
                CreatedByIp = ipAddress.Trim(),
                ExpiresAt = now.Add(timeSpan: lifetime),
                ApplicationUser = applicationUser,
                TokenFamily = tokenFamily ?? Guid.NewGuid().ToString() // Generate new family if not provided
            };

            refreshToken.MarkAsAssigned(assignedTo: applicationUser.Id, assignedBy: assignedBy);

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

    public static string Hash(string rawToken)
    {
        using SHA512 sha = SHA512.Create();
        byte[] bytes = sha.ComputeHash(buffer: Encoding.UTF8.GetBytes(s: rawToken));
        return Convert.ToBase64String(inArray: bytes);
    }

    #endregion
}