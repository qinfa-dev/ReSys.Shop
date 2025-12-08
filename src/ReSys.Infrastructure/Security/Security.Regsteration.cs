using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Constants;
using ReSys.Infrastructure.Security.Authentication;
using ReSys.Infrastructure.Security.Authentication.Identity;
using ReSys.Infrastructure.Security.Authorization;

using Serilog;

namespace ReSys.Infrastructure.Security;

public static class SecurityConfiguration
{
    public static IServiceCollection AddSecurity(this IServiceCollection services, IConfiguration configuration)
    {
        // Identity management (user/role stores, user manager, sign-in manager)
        services.AddShopIdentityCore();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "Identity",
            propertyValue1: "Scoped");

        // Register: Authentication
        services.AddAuthenticationInternal(configuration: configuration);

        // Register: Authorization
        services.AddAuthorizationInternal(configuration: configuration);

        return services;
    }

    public static IApplicationBuilder UseSecurity(this IApplicationBuilder app)
    {
        // Use: Authentication
        app.UseAuthenticationInternal();

        // Use: Authorization
        app.UseAuthorizationInternal();

        return app;
    }
}