using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
using ReSys.Infrastructure.Security.Authentication.Externals.Options;
using ReSys.Infrastructure.Security.Authentication.Externals.Services;

using Serilog;

namespace ReSys.Infrastructure.Security.Authentication.Externals;

public static class ExternalRegistration
{
    public static IServiceCollection AddExternalAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            // Bind authentication options (Google, Facebook)
            AddOptions(services);

            // Register external token validators
            AddTokenValidator(services);

            // Register external authentication services
            services.AddScoped<IExternalUserService, ExternalUserService>();

            sw.Stop();
            Log.Debug(messageTemplate: "External authentication registered in {Duration:0.0000}ms",
                propertyValue: sw.Elapsed.TotalMilliseconds);
            return services;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Error(exception: ex,
                messageTemplate: LogTemplates.OperationFailed,
                propertyValue0: "AddExternalAuthentication",
                propertyValue1: ex.Message);
            throw;
        }
    }

    public static AuthenticationBuilder ConfigureExternalAuthentication(this AuthenticationBuilder auth, IConfiguration configuration)
    {
        GoogleOption? googleOptions = GetOptionalOptions<GoogleOption>(configuration: configuration,
            sectionName: GoogleOption.Section);

        // Google OAuth (if configured)
        if (googleOptions != null && IsProviderConfigurationValid(id: googleOptions.ClientId,
                secret: googleOptions.ClientSecret))
        {
            auth.AddGoogle(configureOptions: opts =>
            {
                opts.ClientId = googleOptions.ClientId;
                opts.ClientSecret = googleOptions.ClientSecret;
                opts.Scope.Clear();
                opts.Scope.Add(item: "openid");
                opts.Scope.Add(item: "profile");
                opts.Scope.Add(item: "email");
                opts.SignInScheme = IdentityConstants.ExternalScheme;
                opts.SaveTokens = false;
                opts.UsePkce = true;
                opts.CallbackPath = "/signin-google";
                opts.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    Log.Debug(messageTemplate: "Redirecting to Google authorization endpoint");
                    context.Response.Redirect(location: context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            Log.Information(messageTemplate: LogTemplates.ConfigLoaded,
                propertyValue0: "GoogleOAuth",
                propertyValue1: new
                {
                    Scopes = new[]
                    {
                        "openid",
                        "profile",
                        "email"
                    },
                    UsePkce = true
                });
        }
        else
        {
            Log.Debug(messageTemplate: "Google OAuth not configured (missing credentials)");
        }

        FacebookOption? facebookOptions = GetOptionalOptions<FacebookOption>(configuration: configuration,
            sectionName: FacebookOption.Section);

        // Facebook OAuth (if configured)
        if (facebookOptions != null && IsProviderConfigurationValid(id: facebookOptions.AppId,
                secret: facebookOptions.AppSecret))
        {
            auth.AddFacebook(configureOptions: opts =>
            {
                opts.AppId = facebookOptions.AppId;
                opts.AppSecret = facebookOptions.AppSecret;
                opts.Scope.Clear();
                opts.Scope.Add(item: "email");
                opts.Scope.Add(item: "public_profile");
                opts.SignInScheme = IdentityConstants.ExternalScheme;
                opts.SaveTokens = false;
                opts.CallbackPath = "/signin-facebook";
                opts.Fields.Clear();
                opts.Fields.Add(item: "id");
                opts.Fields.Add(item: "email");
                opts.Fields.Add(item: "first_name");
                opts.Fields.Add(item: "last_name");
                opts.Fields.Add(item: "name");
                opts.Fields.Add(item: "picture.width(200).height(200)");
                opts.Events.OnRedirectToAuthorizationEndpoint = context =>
                {
                    Log.Debug(messageTemplate: "Redirecting to Facebook authorization endpoint");
                    context.Response.Redirect(location: context.RedirectUri);
                    return Task.CompletedTask;
                };
            });

            Log.Information(messageTemplate: LogTemplates.ConfigLoaded,
                propertyValue0: "FacebookOAuth",
                propertyValue1: new
                {
                    Scopes = new[]
                    {
                            "email",
                            "public_profile"
                    },
                    Fields = new[]
                    {
                            "id",
                            "email",
                            "name",
                            "picture"
                    }
                });
        }
        else
        {
            Log.Debug(messageTemplate: "Facebook OAuth not configured (missing credentials)");
        }
        return auth;
    }

    private static void AddTokenValidator(IServiceCollection services)
    {
        // Google token validator http client
        services.AddHttpClient<GoogleTokenValidator>(configureClient: client =>
        {
            client.Timeout = TimeSpan.FromSeconds(seconds: 15);
            client.DefaultRequestHeaders.Add(name: "User-Agent",
                value: "ReSys.Shop/1.0");
            client.DefaultRequestHeaders.Add(name: "Accept",
                value: "application/json");
        });

        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "GoogleTokenValidator HttpClient",
            propertyValue1: "Transient");

        services.AddHttpClient<FacebookTokenValidator>(configureClient: client =>
        {
            client.Timeout = TimeSpan.FromSeconds(seconds: 15);
            client.DefaultRequestHeaders.Add(name: "User-Agent",
                value: "ReSys.Shop/1.0");
            client.DefaultRequestHeaders.Add(name: "Accept",
                value: "application/json");
        });


        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "FacebookTokenValidator HttpClient",
            propertyValue1: "Transient");

        services.AddScoped<GoogleTokenValidator>();
        services.AddScoped<FacebookTokenValidator>();
        services.AddScoped<IExternalTokenValidator, CompositeExternalTokenValidator>();
    }

    private static void AddOptions(IServiceCollection services)
    {
        services.AddOptions<GoogleOption>()
            .BindConfiguration(configSectionPath: GoogleOption.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddOptions<FacebookOption>()
            .BindConfiguration(configSectionPath: FacebookOption.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    private static T? GetOptionalOptions<T>(IConfiguration configuration, string sectionName)
        where T : class, new()
    {
        var section = configuration.GetSection(key: sectionName);
        return section.Exists() ? section.Get<T>() : null;
    }

    private static bool IsProviderConfigurationValid(string? id, string? secret)
        => !string.IsNullOrWhiteSpace(value: id) && !string.IsNullOrWhiteSpace(value: secret);
}

