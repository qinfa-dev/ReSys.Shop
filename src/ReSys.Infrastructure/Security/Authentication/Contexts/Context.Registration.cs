using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

using Serilog;

namespace ReSys.Infrastructure.Security.Authentication.Contexts;

/// <summary>
/// Registers the authentication-related context & token services.
/// </summary>
internal static class ContextRegistration
{
    public static IServiceCollection AddAuthenticationContext(this IServiceCollection services)
    {
        // HttpContextAccessor
        services.AddHttpContextAccessor();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "HttpContextAccessor",
            propertyValue1: "Singleton");

        // User context
        services.AddScoped<IUserContext, UserContext>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "IUserContext",
            propertyValue1: "Scoped");

        Log.Information(messageTemplate: "Authentication context services registered");
        return services;
    }
}