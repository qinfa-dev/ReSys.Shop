using System.Text.Json;
using System.Text.Json.Serialization;

using ErrorOr;

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

using ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;
using ReSys.Infrastructure.Security.Authentication.Externals.Options;

namespace ReSys.Infrastructure.Security.Authentication.Externals.Services;

public sealed class FacebookTokenValidator : IExternalTokenValidator
{
    private readonly FacebookOption? _facebookOptions;
    private readonly ILogger<FacebookTokenValidator> _logger;
    private readonly HttpClient _httpClient;

    // Cache for app access token to avoid regenerating it frequently
    private readonly Lazy<string?> _appAccessToken;

    public FacebookTokenValidator(
        IOptions<FacebookOption> facebookSettings,
        ILogger<FacebookTokenValidator> logger,
        HttpClient httpClient)
    {
        _facebookOptions = facebookSettings.Value;
        _logger = logger;
        _httpClient = httpClient;

        _appAccessToken = new Lazy<string?>(valueFactory: () =>
        {
            if (string.IsNullOrWhiteSpace(value: _facebookOptions?.AppId) ||
                string.IsNullOrWhiteSpace(value: _facebookOptions?.AppSecret))
            {
                return null;
            }
            return $"{_facebookOptions.AppId}|{_facebookOptions.AppSecret}";
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
        if (!provider.Equals(value: "facebook",
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            return Error.Validation(code: "Provider.NotSupported",
                description: "This validator only supports Facebook");
        }

        // Validate configuration
        if (_facebookOptions == null || _appAccessToken.Value == null)
        {
            _logger.LogError(message: "Facebook configuration is not available or incomplete");
            return Error.NotFound(code: "Facebook.Configuration.Missing",
                description: "Facebook OAuth configuration is not available or incomplete");
        }

        try
        {
            // If we have an authorization code, exchange it for access token first
            if (!string.IsNullOrWhiteSpace(value: authorizationCode))
            {
                _logger.LogDebug(message: "Exchanging Facebook authorization code for access token");
                ErrorOr<string> tokenExchangeResult = await ExchangeAuthorizationCodeAsync(
                    authorizationCode: authorizationCode,
                    redirectUri: redirectUri,
                    cancellationToken: cancellationToken);
                if (tokenExchangeResult.IsError)
                {
                    return tokenExchangeResult.Errors;
                }

                accessToken = tokenExchangeResult.Value;
            }

            if (string.IsNullOrWhiteSpace(value: accessToken))
            {
                return Error.Validation(code: "Token.Required",
                    description: "Access token is required for Facebook validation");
            }

            // Validate and get user information in one step for better performance
            return await ValidateTokenAndGetUserInfoAsync(accessToken: accessToken,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            _logger.LogWarning(message: "Facebook token validation was cancelled");
            return Error.Failure(code: "Token.ValidationCancelled",
                description: "Facebook token validation was cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(exception: ex,
                message: "Error validating Facebook token");
            return Error.Failure(code: "Token.ValidationError",
                description: "Failed to validate Facebook token");
        }
    }

    private async Task<ErrorOr<string>> ExchangeAuthorizationCodeAsync(
        string authorizationCode,
        string? redirectUri,
        CancellationToken cancellationToken)
    {
        string? appId = _facebookOptions?.AppId;
        string? appSecret = _facebookOptions?.AppSecret;

        if (string.IsNullOrWhiteSpace(value: appId) || string.IsNullOrWhiteSpace(value: appSecret))
        {
            return Error.NotFound(code: "Facebook.Configuration.Missing",
                description: "Facebook OAuth configuration is incomplete");
        }

        Dictionary<string, string> tokenRequest = new()
        {
            [key: "grant_type"] = "authorization_code",
            [key: "client_id"] = appId,
            [key: "client_secret"] = appSecret,
            [key: "code"] = authorizationCode
        };

        if (!string.IsNullOrWhiteSpace(value: redirectUri))
        {
            tokenRequest[key: "redirect_uri"] = redirectUri;
        }

        try
        {
            HttpResponseMessage response = await _httpClient.PostAsync(
                requestUri: "https://graph.facebook.com/v18.0/oauth/access_token",
                content: new FormUrlEncodedContent(nameValueCollection: tokenRequest),
                cancellationToken: cancellationToken
            );

            string responseContent = await response.Content.ReadAsStringAsync(cancellationToken: cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning(message: "Facebook token exchange failed: {StatusCode} - {Content}",
                    args:
                    [
                        response.StatusCode,
                        responseContent
                    ]);
                return Error.Failure(code: "Facebook.TokenExchange.Failed",
                    description: "Failed to exchange authorization code with Facebook");
            }

            FacebookTokenResponse? tokenData = JsonSerializer.Deserialize<FacebookTokenResponse>(json: responseContent);

            if (tokenData == null || string.IsNullOrWhiteSpace(value: tokenData.AccessToken))
            {
                _logger.LogError(message: "Invalid token response from Facebook: {Content}",
                    args: responseContent);
                return Error.Failure(code: "Facebook.TokenExchange.InvalidResponse",
                    description: "Invalid token response from Facebook");
            }

            _logger.LogDebug(message: "Successfully exchanged Facebook authorization code for access token");
            return tokenData.AccessToken;
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(exception: ex,
                message: "Network error during Facebook token exchange");
            return Error.Failure(code: "Facebook.TokenExchange.NetworkError",
                description: "Network error during token exchange");
        }
        catch (JsonException ex)
        {
            _logger.LogError(exception: ex,
                message: "Error parsing Facebook token exchange response");
            return Error.Failure(code: "Facebook.TokenExchange.ParseError",
                description: "Error parsing token exchange response");
        }
    }

    private async Task<ErrorOr<ExternalUserTransfer>> ValidateTokenAndGetUserInfoAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        string appAccessToken = _appAccessToken.Value!;

        try
        {
            // First, validate the token using Facebook's debug endpoint for security
            HttpResponseMessage debugResponse = await _httpClient.GetAsync(
                requestUri: $"https://graph.facebook.com/debug_token?input_token={Uri.EscapeDataString(stringToEscape: accessToken)}&access_token={Uri.EscapeDataString(stringToEscape: appAccessToken)}",
                cancellationToken: cancellationToken
            );

            if (!debugResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(message: "Facebook token debug failed: {StatusCode}",
                    args: debugResponse.StatusCode);
                return Error.Unauthorized(code: "Facebook.Token.Invalid",
                    description: "Invalid Facebook token");
            }

            string debugContent = await debugResponse.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
            FacebookDebugResponse? debugInfo = JsonSerializer.Deserialize<FacebookDebugResponse>(json: debugContent);

            // Validate token debug response
            ErrorOr<Success> validationResult = ValidateTokenDebugInfo(debugInfo: debugInfo);
            if (validationResult.IsError)
            {
                return validationResult.Errors;
            }

            // Token is valid, now get user information
            return await GetUserInformationAsync(accessToken: accessToken,
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(exception: ex,
                message: "Network error during Facebook token validation");
            return Error.Failure(code: "Facebook.Token.NetworkError",
                description: "Network error during token validation");
        }
        catch (JsonException ex)
        {
            _logger.LogError(exception: ex,
                message: "Error parsing Facebook debug response");
            return Error.Failure(code: "Facebook.Debug.ParseError",
                description: "Error parsing Facebook debug response");
        }
    }

    private ErrorOr<Success> ValidateTokenDebugInfo(FacebookDebugResponse? debugInfo)
    {
        if (debugInfo?.Data == null)
        {
            _logger.LogWarning(message: "Facebook token debug response is null or invalid");
            return Error.Unauthorized(code: "Facebook.Token.Invalid",
                description: "Invalid Facebook token debug response");
        }

        if (!debugInfo.Data.IsValid)
        {
            _logger.LogWarning(message: "Facebook token validation failed: token is not valid");
            return Error.Unauthorized(code: "Facebook.Token.Invalid",
                description: "Facebook token validation failed");
        }

        if (debugInfo.Data.AppId != _facebookOptions?.AppId)
        {
            _logger.LogWarning(message: "Facebook token app ID mismatch: expected {ExpectedAppId}, got {ActualAppId}",
                args:
                [
                    _facebookOptions?.AppId,
                    debugInfo.Data.AppId
                ]);
            return Error.Unauthorized(code: "Facebook.Token.AppIdMismatch",
                description: "Facebook token app ID mismatch");
        }

        // Check token expiration if available
        if (debugInfo.Data.ExpiresAt.HasValue)
        {
            DateTimeOffset expiresAt = DateTimeOffset.FromUnixTimeSeconds(seconds: debugInfo.Data.ExpiresAt.Value);
            if (expiresAt <= DateTimeOffset.UtcNow.AddMinutes(minutes: 1)) // 1 minute buffer
            {
                _logger.LogWarning(message: "Facebook token has expired or expires very soon");
                return Error.Unauthorized(code: "Facebook.Token.Expired",
                    description: "Facebook token has expired");
            }
        }

        // Validate required scopes for e-commerce
        if (debugInfo.Data.Scopes != null)
        {
            string[] requiredScopes = ["email", "public_profile"];
            bool hasRequiredScopes = requiredScopes.All(predicate: scope =>
                debugInfo.Data.Scopes.Contains(value: scope,
                    comparer: StringComparer.OrdinalIgnoreCase));

            if (!hasRequiredScopes)
            {
                _logger.LogWarning(
                    message: "Facebook token missing required scopes. Required: {RequiredScopes}, Got: {ActualScopes}",
                    args:
                    [
                        string.Join(separator: ", ",
                            value: requiredScopes),
                        string.Join(separator: ", ",
                            value: debugInfo.Data.Scopes)
                    ]);
                return Error.Forbidden(code: "Facebook.Token.InsufficientScopes",
                    description: "Facebook token does not have required permissions for authentication");
            }
        }

        return Result.Success;
    }

    private async Task<ErrorOr<ExternalUserTransfer>> GetUserInformationAsync(
        string accessToken,
        CancellationToken cancellationToken)
    {
        try
        {
            // Get user information with essential fields only for e-commerce
            HttpResponseMessage userResponse = await _httpClient.GetAsync(
                requestUri: $"https://graph.facebook.com/v18.0/me?fields=id,email,first_name,last_name,name,picture.width(200).height(200),verified,locale&access_token={Uri.EscapeDataString(stringToEscape: accessToken)}",
                cancellationToken: cancellationToken
            );

            if (!userResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning(message: "Facebook user info request failed: {StatusCode}",
                    args: userResponse.StatusCode);
                return Error.Failure(code: "Facebook.UserInfo.Failed",
                    description: "Failed to get user information from Facebook");
            }

            string userContent = await userResponse.Content.ReadAsStringAsync(cancellationToken: cancellationToken);
            FacebookUserInfo? userInfo = JsonSerializer.Deserialize<FacebookUserInfo>(json: userContent);

            if (userInfo == null || string.IsNullOrWhiteSpace(value: userInfo.Id))
            {
                _logger.LogError(message: "Invalid user information from Facebook: {Content}",
                    args: userContent);
                return Error.Failure(code: "Facebook.UserInfo.Invalid",
                    description: "Invalid user information from Facebook");
            }

            // Handle email requirements for e-commerce
            string? email = userInfo.Email;
            bool emailVerified = !string.IsNullOrWhiteSpace(value: email);

            if (string.IsNullOrWhiteSpace(value: email))
            {
                // For e-commerce, we might want to require email
                // But Facebook doesn't always provide it, so we'll use a placeholder
                email = $"fb_{userInfo.Id}@facebook.local";
                emailVerified = false;
                _logger.LogInformation(message: "Facebook user {UserId} does not have email, using placeholder",
                    args: userInfo.Id);
            }

            // Additional validation for e-commerce security
            if (userInfo.Verified == false)
            {
                _logger.LogWarning(message: "Facebook user {UserId} account is not verified",
                    args: userInfo.Id);
                // For production e-commerce, you might want to return an error here
                // return Error.Forbidden("Facebook.Account.NotVerified", "Facebook account is not verified");
            }

            _logger.LogDebug(message: "Successfully retrieved Facebook user information for: {Email}",
                args: email);

            return new ExternalUserTransfer
            {
                ProviderId = userInfo.Id,
                Email = email,
                FirstName = NormalizeDisplayName(name: userInfo.FirstName),
                LastName = NormalizeDisplayName(name: userInfo.LastName),
                ProfilePictureUrl = userInfo.Picture?.Data?.Url,
                EmailVerified = emailVerified,
                AdditionalClaims = new Dictionary<string, string>
                {
                    [key: "name"] = NormalizeDisplayName(name: userInfo.Name) ?? "",
                    [key: "has_email"] = emailVerified.ToString().ToLowerInvariant(),
                    [key: "verified"] = (userInfo.Verified ?? false).ToString().ToLowerInvariant(),
                    [key: "locale"] = userInfo.Locale ?? "en_US",
                    [key: "provider_user_id"] = userInfo.Id,
                    [key: "provider"] = "facebook"
                }
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(exception: ex,
                message: "Network error during Facebook user info retrieval");
            return Error.Failure(code: "Facebook.UserInfo.NetworkError",
                description: "Network error during user info retrieval");
        }
        catch (JsonException ex)
        {
            _logger.LogError(exception: ex,
                message: "Error parsing Facebook user info response");
            return Error.Failure(code: "Facebook.UserInfo.ParseError",
                description: "Error parsing Facebook user info response");
        }
    }

    /// <summary>
    /// Normalizes display names to prevent XSS and ensure clean data
    /// </summary>
    private static string? NormalizeDisplayName(string? name)
    {
        if (string.IsNullOrWhiteSpace(value: name))
            return name;

        // Basic sanitization for display names
        return name.Trim()
            .Replace(oldValue: "<",
                newValue: "&lt;")
            .Replace(oldValue: ">",
                newValue: "&gt;")
            .Replace(oldValue: "&",
                newValue: "&amp;")
            .Replace(oldValue: "\"",
                newValue: "&quot;")
            .Replace(oldValue: "'",
                newValue: "&#x27;");
    }

    // Facebook API response models with proper validation and security considerations
    private sealed record FacebookTokenResponse
    {
        [JsonPropertyName(name: "access_token")]
        public string AccessToken { get; init; } = null!;

        [JsonPropertyName(name: "token_type")]
        public string TokenType { get; init; } = null!;

        [JsonPropertyName(name: "expires_in")]
        public int ExpiresIn { get; init; }

        [JsonPropertyName(name: "scope")]
        public string? Scope { get; init; }
    }

    private sealed record FacebookDebugResponse
    {
        [JsonPropertyName(name: "data")]
        public FacebookDebugData? Data { get; init; }
    }

    private sealed record FacebookDebugData
    {
        [JsonPropertyName(name: "app_id")]
        public string AppId { get; init; } = null!;

        [JsonPropertyName(name: "is_valid")]
        public bool IsValid { get; init; }

        [JsonPropertyName(name: "user_id")]
        public string UserId { get; init; } = null!;

        [JsonPropertyName(name: "expires_at")]
        public long? ExpiresAt { get; init; }

        [JsonPropertyName(name: "scopes")]
        public string[]? Scopes { get; init; }

        [JsonPropertyName(name: "type")]
        public string? Type { get; init; }

        [JsonPropertyName(name: "application")]
        public string? Application { get; init; }
    }

    private sealed record FacebookUserInfo
    {
        [JsonPropertyName(name: "id")]
        public string Id { get; init; } = null!;

        [JsonPropertyName(name: "email")]
        public string? Email { get; init; }

        [JsonPropertyName(name: "first_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName(name: "last_name")]
        public string? LastName { get; init; }

        [JsonPropertyName(name: "name")]
        public string? Name { get; init; }

        [JsonPropertyName(name: "picture")]
        public FacebookPicture? Picture { get; init; }

        [JsonPropertyName(name: "verified")]
        public bool? Verified { get; init; }

        [JsonPropertyName(name: "locale")]
        public string? Locale { get; init; }
    }

    private sealed record FacebookPicture
    {
        [JsonPropertyName(name: "data")]
        public FacebookPictureData? Data { get; init; }
    }

    private sealed record FacebookPictureData
    {
        [JsonPropertyName(name: "url")]
        public string? Url { get; init; }

        [JsonPropertyName(name: "width")]
        public int Width { get; init; }

        [JsonPropertyName(name: "height")]
        public int Height { get; init; }

        [JsonPropertyName(name: "is_silhouette")]
        public bool IsSilhouette { get; init; }
    }
}