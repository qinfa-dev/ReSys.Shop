using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

using ErrorOr;

using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

using ReSys.Core.Domain.Identity.Tokens;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Models;
using ReSys.Infrastructure.Security.Authentication.Tokens.Options;

namespace ReSys.Infrastructure.Security.Authentication.Tokens.Services;

public sealed class JwtTokenService : IJwtTokenService
{
    private readonly JwtOptions _jwtOptions;
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly TokenValidationParameters _validationParameters;

    public JwtTokenService(IOptions<JwtOptions>? jwtSettings)
    {
        _jwtOptions = jwtSettings?.Value ?? throw new ArgumentNullException(paramName: nameof(jwtSettings));
        _tokenHandler = new JwtSecurityTokenHandler();

        // Pre-configure validation parameters for better performance
        _validationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            RequireSignedTokens = true,
            ClockSkew = TimeSpan.FromMinutes(minutes: 5), // Allow 5 minutes clock skew
            ValidIssuer = _jwtOptions.Issuer,
            ValidAudience = _jwtOptions.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(s: _jwtOptions.Secret))
        };
    }

    public Task<ErrorOr<TokenResult>> GenerateAccessTokenAsync(
        User? applicationUser,
        CancellationToken cancellationToken = default)
    {
        try
        {
            if (applicationUser is null || string.IsNullOrWhiteSpace(value: applicationUser.Id))
                return Task.FromResult<ErrorOr<TokenResult>>(result: Jwt.Errors.InvalidUser);

            DateTimeOffset now = DateTimeOffset.UtcNow;
            DateTimeOffset expires = now.AddMinutes(minutes: _jwtOptions.AccessTokenLifetimeMinutes);

            // Essential claims only - avoid PII in JWT
            List<Claim> claims =
            [
                new(type: JwtRegisteredClaimNames.Sub,
                    value: applicationUser.Id),
                new(type: JwtRegisteredClaimNames.Jti,
                    value: Guid.NewGuid()
                        .ToString()),
                new(type: JwtRegisteredClaimNames.Iat,
                    value: now.ToUnixTimeSeconds()
                        .ToString(),
                    valueType: ClaimValueTypes.Integer64)
            ];

            // Add: username only if needed (avoid email in JWT for privacy)
            if (!string.IsNullOrEmpty(value: applicationUser.UserName))
            {
                claims.Add(item: new Claim(type: JwtRegisteredClaimNames.UniqueName,
                    value: applicationUser.UserName));
            }

            // Add essential security claims
            if (applicationUser.EmailConfirmed)
            {
                claims.Add(item: new Claim(type: "email_verified",
                    value: "true"));
            }

            SymmetricSecurityKey key = new(key: Encoding.UTF8.GetBytes(s: _jwtOptions.Secret));
            SigningCredentials credentials = new(key: key,
                algorithm: SecurityAlgorithms.HmacSha256);

            JwtSecurityToken token = new(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: now.UtcDateTime,
                expires: expires.UtcDateTime,
                signingCredentials: credentials
            );

            string? tokenString = _tokenHandler.WriteToken(token: token);

            return Task.FromResult<ErrorOr<TokenResult>>(result: new TokenResult
            {
                Token = tokenString,
                ExpiresAt = expires.ToUnixTimeSeconds()
            });
        }
        catch (SecurityTokenException)
        {
            return Task.FromResult<ErrorOr<TokenResult>>(result: Jwt.Errors.SecurityTokenError);
        }
        catch (Exception)
        {
            return Task.FromResult<ErrorOr<TokenResult>>(result: Jwt.Errors.GenerationFailed);
        }
    }

    public ErrorOr<ClaimsPrincipal> GetPrincipalFromToken(string token)
    {
        try
        {
            // Read token without validation for expired tokens
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token: token);
            IEnumerable<Claim>? claims = jwtToken.Claims;
            ClaimsIdentity identity = new(claims: claims,
                authenticationType: "JWT",
                nameType: JwtRegisteredClaimNames.UniqueName,
                roleType: ClaimTypes.Role);
            return new ClaimsPrincipal(identity: identity);
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(code: Jwt.Errors.InvalidFormat.Code,
                description: $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.PrincipalExtraction;
        }
    }

    public ErrorOr<TimeSpan> GetTokenRemainingTime(string token)
    {
        try
        {
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token: token);
            long? exp = jwtToken.Payload.Expiration;

            if (!exp.HasValue)
            {
                return Jwt.Errors.NoExpiration;
            }

            DateTimeOffset expiration = DateTimeOffset.FromUnixTimeSeconds(seconds: exp.Value);
            TimeSpan remaining = expiration - DateTimeOffset.UtcNow;

            return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(code: Jwt.Errors.InvalidFormat.Code,
                description: $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.SecurityTokenError;
        }
    }

    public ErrorOr<bool> ValidateTokenFormat(string token)
    {
        try
        {
            // Basic format validation - should have 3 parts separated by dots
            string[] parts = token.Split(separator: '.');
            if (parts.Length != Jwt.Constraints.TokenParts)
            {
                return Error.Validation(code: Jwt.Errors.InvalidFormat.Code,
                    description: "JWT must have exactly 3 parts separated by dots");
            }

            // Try to read the token structure
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token: token);

            // Basic header validation
            if (string.IsNullOrEmpty(value: jwtToken.Header.Alg))
            {
                return Jwt.Errors.MissingAlgorithm;
            }

            // Basic payload validation
            if (jwtToken.Payload.Count == 0)
            {
                return Jwt.Errors.EmptyPayload;
            }

            return true;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(code: Jwt.Errors.InvalidFormat.Code,
                description: $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.InvalidFormat;
        }
    }

    public ErrorOr<JwtSecurityToken> ParseToken(string token)
    {
        try
        {
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token: token);
            return jwtToken;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(code: Jwt.Errors.InvalidFormat.Code,
                description: $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.ParseFailed;
        }
    }

    public ErrorOr<JwtTokenValidationResult> ValidateToken(
        string token,
        bool validateLifetime = true)
    {
        try
        {
            // Clone validation parameters to modify lifetime validation
            TokenValidationParameters? validationParams = _validationParameters.Clone();
            validationParams.ValidateLifetime = validateLifetime;

            ClaimsPrincipal? principal = _tokenHandler.ValidateToken(token: token,
                validationParameters: validationParams,
                validatedToken: out SecurityToken? validatedToken);

            JwtTokenValidationResult result = new()
            {
                IsValid = true,
                ClaimsIdentity = principal.Identities.FirstOrDefault(),
                SecurityToken = validatedToken,
                Issuer = validatedToken.Issuer
            };

            return result;
        }
        catch (SecurityTokenExpiredException ex)
        {
            JwtTokenValidationResult result = new()
            {
                IsValid = false,
                Exception = ex
            };
            return result;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return Jwt.Errors.InvalidSignature;
        }
        catch (SecurityTokenValidationException)
        {
            return Jwt.Errors.ValidationFailed;
        }
        catch (Exception)
        {
            return Jwt.Errors.InvalidFormat;
        }
    }

    public ErrorOr<Dictionary<string, object>> GetTokenClaims(string token)
    {
        try
        {
            JwtSecurityToken? jwtToken = _tokenHandler.ReadJwtToken(token: token);
            Dictionary<string, object> claims = jwtToken.Claims.ToDictionary(keySelector: c => c.Type,
                elementSelector: c => (object)c.Value);
            return claims;
        }
        catch (ArgumentException ex)
        {
            return Error.Validation(code: Jwt.Errors.InvalidFormat.Code,
                description: $"Invalid token format: {ex.Message}");
        }
        catch (Exception)
        {
            return Jwt.Errors.ClaimsExtraction;
        }
    }
}