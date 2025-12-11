using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Feature.Accounts.Auth.Externals;

public static partial class ExternalModule
{
    public static class GetExternalProviders
    {
        internal const string Name = "Account.Authentication.External.Providers.Get";
        internal const string Summary = "Get available external authentication providers";

        internal const string Description =
            "Returns list of configured external authentication providers with login URLs";

        public sealed record Query : IQuery<List<Result>>;

        public sealed record Result : ExternalProvider;

        public record ExternalProvider
        {
            public string Provider { get; init; } = null!;
            public string DisplayName { get; init; } = null!;
            public string LoginUrl { get; init; } = null!;
            public string? IconUrl { get; init; }
            public bool IsEnabled { get; init; } = true;
            public string[] RequiredScopes { get; init; } = [];
            public string ConfigurationUrl { get; init; } = null!;
        }

        public sealed class CommandHandler(
            SignInManager<User> signInManager,
            IConfiguration configuration,
            IHttpContextAccessor httpContextAccessor,
            ILogger<CommandHandler> logger
        ) : IQueryHandler<Query, List<Result>>
        {
            // Supported providers for production e-commerce (removed Microsoft)
            private static readonly HashSet<string> SupportedProviders =
                new(comparer: StringComparer.OrdinalIgnoreCase) { "google", "facebook" };

            public async Task<ErrorOr<List<Result>>> Handle(Query request, CancellationToken cancellationToken)
            {
                try
                {
                    IEnumerable<AuthenticationScheme> schemes =
                        await signInManager.GetExternalAuthenticationSchemesAsync();
                    string baseUrl = GetBaseUrl();
                    string externalRoute = Route;

                    List<Result> providers = [];

                    foreach (AuthenticationScheme scheme in schemes)
                    {
                        string providerName = scheme.Name.ToLowerInvariant();

                        // Security: Only include supported providers
                        if (!SupportedProviders.Contains(item: providerName))
                        {
                            logger.LogDebug(message: "Skipping unsupported provider: {Provider}",
                                args: scheme.Name);
                            continue;
                        }

                        if (!IsProviderConfigured(providerName: scheme.Name))
                        {
                            logger.LogDebug(message: "Skipping unconfigured provider: {Provider}",
                                args: scheme.Name);
                            continue;
                        }

                        Result provider = new Result
                        {
                            Provider = providerName,
                            DisplayName = GetProviderDisplayName(providerName: providerName),
                            LoginUrl = BuildTokenExchangeUrl(baseUrl: baseUrl,
                                externalRoute: externalRoute,
                                providerName: providerName),
                            IconUrl = GetProviderIconUrl(providerName: providerName),
                            RequiredScopes = GetProviderRequiredScopes(providerName: providerName),
                            ConfigurationUrl = BuildConfigurationUrl(baseUrl: baseUrl,
                                externalRoute: externalRoute,
                                providerName: providerName),
                            IsEnabled = true
                        };

                        providers.Add(item: provider);
                    }

                    logger.LogInformation(
                        message: "Retrieved {Count} configured external authentication providers: {Providers}",
                        args:
                        [
                            providers.Count,
                            string.Join(separator: ", ",
                                values: providers.Select(selector: p => p.Provider))
                        ]);

                    return providers;
                }
                catch (Exception ex)
                {
                    logger.LogError(exception: ex,
                        message: "Failed to retrieve external authentication providers");
                    return Error.Failure(
                        code: "ExternalProviders.RetrievalFailed",
                        description: "Failed to retrieve external authentication providers"
                    );
                }
            }

            private string GetBaseUrl()
            {
                string? configuredBaseUrl = configuration[key: "App:BaseUrl"];
                if (!string.IsNullOrWhiteSpace(value: configuredBaseUrl))
                {
                    return configuredBaseUrl.TrimEnd(trimChar: '/');
                }

                HttpContext? context = httpContextAccessor.HttpContext;
                if (context != null)
                {
                    HttpRequest request = context.Request;
                    string scheme = request.Scheme;
                    string? host = request.Host.Value;
                    return $"{scheme}://{host}";
                }

                logger.LogWarning(
                    message: "No base URL configured and no HTTP context available. Using localhost fallback.");
                return "https://localhost:5001";
            }

            private static string BuildTokenExchangeUrl(string baseUrl, string externalRoute, string providerName)
            {
                return $"{baseUrl}{externalRoute}/token/exchange/{providerName}";
            }

            private static string BuildConfigurationUrl(string baseUrl, string externalRoute, string providerName)
            {
                return $"{baseUrl}{externalRoute}/config/{providerName}";
            }

            private static string GetProviderDisplayName(string providerName) =>
                providerName switch
                {
                    "google" => "Google",
                    "facebook" => "Facebook",
                    _ => char.ToUpperInvariant(c: providerName[index: 0]) + providerName[1..].ToLowerInvariant()
                };

            private static string? GetProviderIconUrl(string providerName) =>
                providerName switch
                {
                    "google" => "https://developers.google.com/identity/images/g-logo.png",
                    "facebook" => "https://upload.wikimedia.org/wikipedia/commons/5/51/Facebook_f_logo_%282019%29.svg",
                    _ => null
                };

            private static string[] GetProviderRequiredScopes(string providerName) =>
                providerName switch
                {
                    "google" => ["openid", "email", "profile"],
                    "facebook" => ["email", "public_profile"],
                    _ => []
                };

            private bool IsProviderConfigured(string providerName)
            {
                try
                {
                    string normalizedName = providerName.ToLowerInvariant();

                    // Only check configuration for supported providers
                    if (!SupportedProviders.Contains(item: normalizedName))
                    {
                        return false;
                    }

                    IConfigurationSection section = configuration.GetSection(key: $"Authentication:{providerName}");
                    if (!section.Exists())
                    {
                        logger.LogDebug(message: "Configuration section not found for provider: {Provider}",
                            args: providerName);
                        return false;
                    }

                    bool isConfigured = normalizedName switch
                    {
                        "google" => HasRequiredGoogleConfig(section: section),
                        "facebook" => HasRequiredFacebookConfig(section: section),
                        _ => false
                    };

                    if (!isConfigured)
                    {
                        logger.LogDebug(message: "Provider {Provider} is not properly configured",
                            args: providerName);
                    }

                    return isConfigured;
                }
                catch (Exception ex)
                {
                    logger.LogWarning(exception: ex,
                        message: "Error checking configuration for provider {Provider}",
                        args: providerName);
                    return false;
                }
            }

            private static bool HasRequiredGoogleConfig(IConfigurationSection section)
            {
                string? clientId = section[key: "ClientId"];
                string? clientSecret = section[key: "ClientSecret"];
                return !string.IsNullOrWhiteSpace(value: clientId) && !string.IsNullOrWhiteSpace(value: clientSecret);
            }

            private static bool HasRequiredFacebookConfig(IConfigurationSection section)
            {
                string? appId = section[key: "AppId"];
                string? appSecret = section[key: "AppSecret"];
                return !string.IsNullOrWhiteSpace(value: appId) && !string.IsNullOrWhiteSpace(value: appSecret);
            }
        }
    }
}

public static partial class ExternalModule
{
}