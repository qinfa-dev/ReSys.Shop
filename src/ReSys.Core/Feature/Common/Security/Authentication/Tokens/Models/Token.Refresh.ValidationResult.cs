using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Feature.Common.Security.Authentication.Tokens.Models;

/// <summary>
/// Result of refresh token validation.
/// </summary>
public sealed record RefreshTokenValidationResult
{
    public Domain.Identity.Tokens.RefreshToken RefreshToken { get; init; } = null!;
    public User User { get; init; } = null!;
}
