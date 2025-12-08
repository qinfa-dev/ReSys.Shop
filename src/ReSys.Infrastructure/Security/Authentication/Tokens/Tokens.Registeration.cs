using System.Text;

using Ardalis.GuardClauses;

using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Security.Authentication.Tokens.Interfaces;
using ReSys.Infrastructure.Security.Authentication.Tokens.Options;
using ReSys.Infrastructure.Security.Authentication.Tokens.Services;

using Serilog;

namespace ReSys.Infrastructure.Security.Authentication.Tokens;

public static class TokensRegistration
{
    public static IServiceCollection AddTokensAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            AddOptions(services);
            AddServices(services);

            sw.Stop();
            Log.Debug(messageTemplate: "Token authentication registered in {Duration:0.0000}ms",
                propertyValue: sw.Elapsed.TotalMilliseconds);
            return services;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log.Error(exception: ex,
                messageTemplate: LogTemplates.OperationFailed,
                propertyValue0: "AddTokenAuthentication",
                propertyValue1: ex.Message);
            throw;
        }
    }

    private static void AddServices(IServiceCollection services)
    {
        // Token services
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IRefreshTokenService, RefreshTokenService>();
    }

    private static void AddOptions(IServiceCollection services)
    {
        // Bind authentication options (Jwt)
        services.AddOptions<JwtOptions>()
            .BindConfiguration(configSectionPath: JwtOptions.Section)
            .ValidateDataAnnotations()
            .ValidateOnStart();
    }

    public static AuthenticationBuilder ConfigureTokensAuthentication(this AuthenticationBuilder auth,
        IConfiguration configuration)
    {
        // Load required options
        JwtOptions jwtOptions = GetRequiredOptions<JwtOptions>(configuration: configuration,
            sectionName: JwtOptions.Section);

        // JWT bearer config
        auth.AddJwtBearer(configureOptions: options =>
        {
            options.RequireHttpsMetadata = false;// NOT RECOMMENDED FOR PRODUCTION
            options.SaveToken = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = jwtOptions.Issuer,
                ValidAudience = jwtOptions.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(key: Encoding.UTF8.GetBytes(s: jwtOptions.Secret)),
                ClockSkew = TimeSpan.FromMinutes(minutes: 2),
                RequireExpirationTime = true,
                RequireSignedTokens = true
            };

            // Events for logging

            options.Events = new JwtBearerEvents
            {
                OnMessageReceived = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    var header = context.Request.Headers[key: "Authorization"].ToString();
                    logger?.LogDebug(message: "OnMessageReceived Authorization header: {Header}", args: header);
                    return Task.CompletedTask;
                },
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    logger?.LogWarning(
                        message: LogTemplates.AuthFailed,
                        args:
                        [
                            "JWT",
                                context.Exception.Message,
                                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                        ]);

                    context.Response.Headers[key: "Token-Expired"] = "true";
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    string userId = context.Principal?.FindFirst(type: "sub")?.Value ?? "Unknown";
                    logger?.LogDebug(message: LogTemplates.UserAuthenticated,
                        args:
                        [
                            userId,
                                "JWT",
                                context.HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
                        ]);
                    return Task.CompletedTask;
                },
                OnChallenge = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetService<ILogger<JwtBearerEvents>>();
                    logger?.LogInformation(message: "JWT authentication challenge triggered");
                    return Task.CompletedTask;
                }
            };
        });

        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "JwtBearer",
            propertyValue1: "Singleton");

        Log.Information(messageTemplate: LogTemplates.ConfigLoaded,
            propertyValue0: "JwtBearer",
            propertyValue1: new
            {
                jwtOptions.Issuer,
                jwtOptions.Audience,
                ClockSkew = "2min"
            });

        return auth;
    }
    private static T GetRequiredOptions<T>(IConfiguration configuration, string sectionName)
        where T : class, new()
    {
        T? options = configuration.GetSection(key: sectionName).Get<T>();
        Guard.Against.Null(input: options,
            message: $"{typeof(T).Name} options must be configured in appsettings at section '{sectionName}'.");
        return options;
    }
}
