using System.Text.Json;
using System.Text.Json.Serialization;

using ErrorOr;

using Google.Apis.Auth;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;
using ReSys.Infrastructure.Security.Authentication.Externals.Options;

namespace ReSys.Infrastructure.Security.Authentication.Externals.Services;

public sealed class GoogleTokenValidator : IExternalTokenValidator
{
    private readonly GoogleOption? _googleOptions;
    private readonly ILogger<GoogleTokenValidator> _logger;
    private readonly HttpClient _httpClient;

    // Cache for validation settings to avoid recreating
    private readonly Lazy<GoogleJsonWebSignature.ValidationSettings?> _validationSettings;

    public GoogleTokenValidator(
        IOptions<GoogleOption> googleSettings,
        ILogger<GoogleTokenValidator> logger,
        HttpClient httpClient)
    {
        _googleOptions = googleSettings.Value;
        _logger = logger;
        _httpClient = httpClient;

        _validationSettings = new Lazy<GoogleJsonWebSignature.ValidationSettings?>(valueFactory: () =>
        {
            if (string.IsNullOrWhiteSpace(value: _googleOptions?.ClientId))
                return null;

            return new GoogleJsonWebSignature.ValidationSettings
            {
                Audience = [_googleOptions.ClientId]
            };
        });
    }

    public async Task<ErrorOr<ExternalUserTransfer>> ValidateTokenAsync(
        string provider,
        string? accessToken,
        string? idToken,
        string? authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken = default)
    {
        if (!provider.Equals(value: "google",
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation(code: "Provider.NotSupported",
                description: "This validator only supports Google");
        }

        // Validate configuration
        if (_googleOptions == null)
        {
            _logger.LogError(message: "Google configuration is not available");
            return Error.NotFound(code: "Google.Configuration.Missing",
                description: "Google OAuth configuration is not available");
        }

        try
        {
            // If we have an authorization code, exchange it for tokens first
            if (!string.IsNullOrWhiteSpace(value: authorizationCode))
            {
                _logger.LogDebug(message: "Exchanging Google authorization code for tokens");
                ErrorOr<(string AccessToken, string? IdToken)> tokenExchangeResult = await ExchangeAuthorizationCodeAsync(
                    authorizationCode: authorizationCode,
                    redirectUri: redirectUri,
                    cancellationToken: cancellationToken);
                if (tokenExchangeResult.IsError)
                {
                    return tokenExchangeResult.Errors;
                }

                accessToken = tokenExchangeResult.Value.AccessToken;
                idToken = tokenExchangeResult.Value.IdToken;
            }

            // Prefer ID token validation (more secure and reliable)
            if (!string.IsNullOrWhiteSpace(value: idToken))
            {
                _logger.LogDebug(message: "Validating Google ID token");
                return await ValidateIdTokenWithSdkAsync(idToken: idToken,
                    cancellationToken: cancellationToken);
            }

            // Fallback to access token validation
            if (!string.IsNullOrWhiteSpace(value: accessToken))
            {
                _logger.LogDebug(message: "Validating Google access token");
                return await ValidateAccessTokenAsync(accessToken: accessToken,
                    cancellationToken: cancellationToken);
            }

            return Error.Validation(code: "Token.Invalid",
                description: "No valid token provided for Google authentication");
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(message: "Google token validation was cancelled");
            return Error.Failure(code: "Token.ValidationCancelled",
                description: "Google token validation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(exception: ex,
                message: "Error validating Google token");
            return Error.Failure(code: "Token.ValidationError",
                description: "Failed to validate Google token");
        }
    }

    private async Task<ErrorOr<ExternalUserTransfer>> ValidateIdTokenWithSdkAsync(string idToken, CancellationToken cancellationToken)
    {
        try
        {
            GoogleJsonWebSignature.ValidationSettings? validationSettings = _validationSettings.Value;
            if (validationSettings == null)
            {
                return Error.NotFound(code: "Google.Configuration.Missing",
                    description: "Google ClientId is not configured");
            }

            // Use Google.Apis.Auth SDK to validate ID token with proper signature verification
            GoogleJsonWebSignature.Payload? payload = await GoogleJsonWebSignature.ValidateAsync(jwt: idToken,
                validationSettings: validationSettings);

            // Additional security validations
            if (string.IsNullOrWhiteSpace(value: payload.Email))
            {
                _logger.LogWarning(message: "Google ID token does not contain email claim");
                return Error.Validation(code: "Google.IdToken.MissingEmail",
                    description: "Email is required for authentication");
            }

            // Ensure email is verified for security
            if (!payload.EmailVerified)
            {
                _logger.LogWarning(message: "Google account email is not verified: {Email}",
                    args: payload.Email);
                return Error.Unauthorized(code: "Google.Email.NotVerified",
                    description: "Email must be verified to authenticate");
            }

            _logger.LogDebug(message: "Successfully validated Google ID token for user: {Email}",
                args: payload.Email);

            return new ExternalUserTransfer
            {
                ProviderId = payload.Subject,
                Email = payload.Email,
                FirstName = payload.GivenName ?? "",
                LastName = payload.FamilyName ?? "",
                ProfilePictureUrl = payload.Picture,
                EmailVerified = payload.EmailVerified,
                AdditionalClaims = new Dictionary<string, string>
                {
                    [key: "locale"] = payload.Locale ?? "",
                    [key: "name"] = payload.Name ?? "",
                    [key: "iss"] = payload.Issuer ?? "",
                    [key: "aud"] = payload.Audience?.ToString() ?? ""
                }
            };
        }
        catch (InvalidJwtException ex)
        {
            _logger.LogWarning(message: "Invalid Google ID token: {Error}",
                args: ex.Message);
            return Error.Unauthorized(code: "Google.IdToken.Invalid",
                description: "Invalid Google ID token");
        }
        catch (Exception ex)
        {
            _logger.LogError(exception: ex,
                message: "Error parsing Google ID token");
            return Error.Failure(code: "Google.IdToken.ParseError",
                description: "Failed to parse Google ID token");
        }
    }

    private async Task<ErrorOr<(string AccessToken, string? IdToken)>> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken)
    {
        string? clientId = _googleOptions?.ClientId;
        string? clientSecret = _googleOptions?.ClientSecret;

        if (string.IsNullOrWhiteSpace(value: clientId) || string.IsNullOrWhiteSpace(value: clientSecret))
        {
            return Error.NotFound(code: "Google.Configuration.Missing",
                description: "Google OAuth configuration is incomplete");
        }

        Dictionary<string, string> tokenRequest = new()
        {
            [key: "grant_type"] = "authorization_code",
            [key: "client_id"] = clientId,
            [key: "client_secret"] = clientSecret,
            [key: "code"] = authorizationCode
        };

        if (!string.IsNullOrWhiteSpace(value: redirectUri))
        {
            tokenRequest[key: "redirect_uri"] = redirectUri;
        }

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(
                requestUri: "https://oauth2.googleapis.com/token",
                content: new FormUrlEncodedContent(nameValueCollection: tokenRequest),
                cancellationToken: cancellationToken
            );

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(message: "Google token exchange failed: {StatusCode} - {Content}",
                    args:
                    [
                        response.StatusCode,
                        responseContent
                    ]);
                return Error.Failure(code: "Google.TokenExchange.Failed",
                    description: "Failed to exchange authorization code with Google");
            }

            GoogleTokenResponse? tokenData = JsonSerializer.Deserialize<GoogleTokenResponse>(json: responseContent);

            if (tokenData == null || string.IsNullOrWhiteSpace(value: tokenData.AccessToken))
            {
                _logger.LogError(message: "Invalid token response from Google: {Content}",
                    args: responseContent);
                return Error.Failure(code: "Google.TokenExchange.InvalidResponse",
                    description: "Invalid token response from Google");
            }

            return (tokenData.AccessToken, tokenData.IdToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(exception: ex,
                message: "Network error during Google token exchange");
            return Error.Failure(code: "Google.TokenExchange.NetworkError",
                description: "Network error during token exchange");
        }
    }

    private async Task<ErrorOr<ExternalUserTransfer>> ValidateAccessTokenAsync(string accessToken, CancellationToken cancellationToken)
    {
        try
        {
            HttpResponseMessage response = await _httpClient.GetAsync(
                requestUri: $"https://www.googleapis.com/oauth2/v2/userinfo?access_token={Uri.EscapeDataString(stringToEscape: accessToken)}",
                cancellationToken: cancellationToken
            );

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(message: "Google access token validation failed: {StatusCode}",
                    args: response.StatusCode);
                return Error.Unauthorized(code: "Google.AccessToken.Invalid",
                    description: "Invalid Google access token");
            }

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
            GoogleUserInfo? userInfo = JsonSerializer.Deserialize<GoogleUserInfo>(json: responseContent);

            if (userInfo == null || string.IsNullOrWhiteSpace(value: userInfo.Email))
            {
                _logger.LogError(message: "Invalid user info from Google access token: {Content}",
                    args: responseContent);
                return Error.Unauthorized(code: "Google.AccessToken.InvalidUserInfo",
                    description: "Invalid user info from Google access token");
            }

            // Require verified email for security
            if (!userInfo.VerifiedEmail)
            {
                _logger.LogWarning(message: "Google account email is not verified: {Email}",
                    args: userInfo.Email);
                return Error.Unauthorized(code: "Google.Email.NotVerified",
                    description: "Email must be verified to authenticate");
            }

            _logger.LogDebug(message: "Successfully validated Google access token for user: {Email}",
                args: userInfo.Email);

            return new ExternalUserTransfer
            {
                ProviderId = userInfo.Id,
                Email = userInfo.Email,
                FirstName = userInfo.GivenName ?? "",
                LastName = userInfo.FamilyName ?? "",
                ProfilePictureUrl = userInfo.Picture,
                EmailVerified = userInfo.VerifiedEmail,
                AdditionalClaims = new Dictionary<string, string>
                {
                    [key: "locale"] = userInfo.Locale ?? "",
                    [key: "name"] = userInfo.Name ?? ""
                }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(exception: ex,
                message: "Network error during Google access token validation");
            return Error.Failure(code: "Google.AccessToken.NetworkError",
                description: "Network error during token validation");
        }
        catch (JsonException ex)
        {
            _logger.LogError(exception: ex,
                message: "Error parsing Google user info response");
            return Error.Failure(code: "Google.UserInfo.ParseError",
                description: "Error parsing user information");
        }
    }

    // Google API response models with proper validation
    private sealed record GoogleTokenResponse
    {
        [JsonPropertyName(name: "access_token")]
        public string AccessToken { get; init; } = null!;

        [JsonPropertyName(name: "id_token")]
        public string? IdToken { get; init; }

        [JsonPropertyName(name: "token_type")]
        public string TokenType { get; init; } = null!;

        [JsonPropertyName(name: "expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName(name: "refresh_token")]
        public string? RefreshToken { get; init; }

        [JsonPropertyName(name: "scope")]
        public string? Scope { get; init; }
    }

    private sealed record GoogleUserInfo
    {
        [JsonPropertyName(name: "id")]
        public string Id { get; init; } = null!;

        [JsonPropertyName(name: "email")]
        public string Email { get; init; } = null!;

        [JsonPropertyName(name: "verified_email")]
        public bool VerifiedEmail { get; init; }

        [JsonPropertyName(name: "given_name")]
        public string? GivenName { get; init; }

        [JsonPropertyName(name: "family_name")]
        public string? FamilyName { get; init; }

        [JsonPropertyName(name: "name")]
        public string? Name { get; init; }

        [JsonPropertyName(name: "picture")]
        public string? Picture { get; init; }

        [JsonPropertyName(name: "locale")]
        public string? Locale { get; init; }
    }
}
