using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Feature.Common.Security.Authentication.Contexts.Tokens.RefreshToken;

/// <summary>
/// Result of refresh token validation.
/// </summary>
public sealed record RefreshTokenValidationResult
{
    public Domain.Identity.Tokens.RefreshToken RefreshToken { get; init; } = null!;
    public ApplicationUser ApplicationUser { get; init; } = null!;
}
