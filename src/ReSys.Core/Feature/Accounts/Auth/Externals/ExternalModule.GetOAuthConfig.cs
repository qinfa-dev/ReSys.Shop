using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace ReSys.Core.Feature.Accounts.Auth.Externals;

public static partial class ExternalModule
{
    public static class GetOAuthConfig
    {
        #region Constants
        internal const string Name = "Account.Authentication.External.OAuthConfig.Get";
        internal const string Summary = "Get OAuth configuration for frontend";
        internal const string Description =
            "Returns OAuth configuration needed for frontend applications to handle OAuth flow.";
        #endregion

        public sealed record Query(string? Provider) : IQuery<Result>;

        public sealed record Result
        {
            public string Provider { get; init; } = null!;
            public string ClientId { get; init; } = null!;
            public string AuthorizationUrl { get; init; } = null!;
            public string TokenUrl { get; init; } = null!;
            public string[] Scopes { get; init; } = [];
            public string ResponseType { get; init; } = "code";
            public Dictionary<string, string> AdditionalParameters { get; init; } = new();
            public string TokenExchangeUrl { get; init; } = null!;
            public string ProviderName { get; init; } = null!;
            public bool RequiresPKCE { get; init; } = true;
        }

        public sealed class CommandHandler(
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CommandHandler> logger
        ) : IQueryHandler<Query, Result>
        {
            private static readonly HashSet<string> SupportedProviders = new(comparer: StringComparer.OrdinalIgnoreCase)
            {
                "google",
                "facebook"
            };

            public async Task<ErrorOr<Result>> Handle(Query request, CancellationToken cancellationToken)
            {
                string? provider = request.Provider?.ToLowerInvariant().Trim();
                if (string.IsNullOrWhiteSpace(value: provider))
                {
                    logger.LogWarning(message: "No provider specified in GetOAuthConfig command");
                    return Error.Validation(code: "Provider.Required",
                        description: "Provider is required");
                }

                if (!SupportedProviders.Contains(item: provider))
                {
                    logger.LogWarning(message: "Unsupported provider requested: {Provider}",
                        args: provider);
                    return Error.NotFound(
                        code: "Provider.NotSupported",
                        description: $"Provider '{provider}' is not supported. Supported providers: {string.Join(separator: ", ", values: SupportedProviders)}");
                }

                IConfigurationSection configSection = configuration.GetSection(key: $"Authentication:{provider}");
                if (!configSection.Exists())
                {
                    logger.LogWarning(message: "Configuration not found for provider: {Provider}",
                        args: provider);
                    return Error.NotFound(code: "Provider.NotConfigured",
                        description: $"Provider '{provider}' is not configured");
                }

                string baseUrl = GetBaseUrl();

                try
                {
                    ErrorOr<Result> result = provider switch
                    {
                        "google" => await GetGoogleConfigAsync(config: configSection,
                            baseUrl: baseUrl),
                        "facebook" => await GetFacebookConfigAsync(config: configSection,
                            baseUrl: baseUrl),
                        _ => Error.NotFound(code: "Provider.NotSupported",
                            description: $"Provider '{provider}' is not supported")
                    };

                    return result;
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex,
                        message: "Error getting OAuth config for provider: {Provider}",
                        args: provider);
                    return Error.Failure(code: "Provider.ConfigError",
                        description: $"Failed to retrieve configuration for provider '{provider}'");
                }
            }

            private async Task<ErrorOr<Result>> GetGoogleConfigAsync(IConfigurationSection config, string baseUrl)
            {
                await Task.CompletedTask;

                string? clientId = config[key: "ClientId"];
                if (string.IsNullOrWhiteSpace(value: clientId))
                    return Error.Validation(code: "Google.ClientId.Missing",
                        description: "Google ClientId is not configured");

                string? clientSecret = config[key: "ClientSecret"];
                if (string.IsNullOrWhiteSpace(value: clientSecret))
                    return Error.Validation(code: "Google.ClientSecret.Missing",
                        description: "Google ClientSecret is not configured");

                return new Result
                {
                    Provider = "google",
                    ProviderName = "Google",
                    ClientId = clientId,
                    AuthorizationUrl = "https://accounts.google.com/o/oauth2/v2/auth",
                    TokenUrl = "https://oauth2.googleapis.com/token",
                    Scopes = ["openid", "email", "profile"],
                    ResponseType = "code",
                    RequiresPKCE = true,
                    AdditionalParameters = new()
                    {
                        [key: "access_type"] = "offline",
                        [key: "prompt"] = "consent",
                        [key: "include_granted_scopes"] = "true"
                    },
                    TokenExchangeUrl = $"{baseUrl}/api/account/auth/external/token/exchange/google"
                };
            }

            private async Task<ErrorOr<Result>> GetFacebookConfigAsync(IConfigurationSection config, string baseUrl)
            {
                await Task.CompletedTask;

                string? appId = config[key: "AppId"];
                if (string.IsNullOrWhiteSpace(value: appId))
                    return Error.Validation(code: "Facebook.AppId.Missing",
                        description: "Facebook AppId is not configured");

                string? appSecret = config[key: "AppSecret"];
                if (string.IsNullOrWhiteSpace(value: appSecret))
                    return Error.Validation(code: "Facebook.AppSecret.Missing",
                        description: "Facebook AppSecret is not configured");

                return new Result
                {
                    Provider = "facebook",
                    ProviderName = "Facebook",
                    ClientId = appId,
                    AuthorizationUrl = "https://www.facebook.com/v18.0/dialog/oauth",
                    TokenUrl = "https://graph.facebook.com/v18.0/oauth/access_token",
                    Scopes = ["email", "public_profile"],
                    ResponseType = "code",
                    RequiresPKCE = false,
                    AdditionalParameters = new()
                    {
                        [key: "display"] = "popup",
                        [key: "auth_type"] = "rerequest"
                    },
                    TokenExchangeUrl = $"{baseUrl}/api/account/auth/external/token/exchange/facebook"
                };
            }

            private string GetBaseUrl()
            {
                string? configuredBaseUrl = configuration[key: "App:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(value: configuredBaseUrl))
                    return configuredBaseUrl.TrimEnd(trimChar: '/');

                var context = httpContextAccessor.HttpContext;
                if (context != null)
                    return $"{context.Request.Scheme}://{context.Request.Host}";

                logger.LogWarning(message: "No base URL configured and no HTTP context available. Using localhost fallback.");
                return "https://localhost:5001";
            }
        }
    }
}