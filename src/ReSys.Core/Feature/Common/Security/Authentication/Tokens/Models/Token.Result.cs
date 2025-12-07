namespace ReSys.Core.Feature.Common.Security.Authentication.Tokens.Models;
public record TokenResult
{
    public string Token { get; init; } = string.Empty;
    public long ExpiresAt { get; init; }
}

