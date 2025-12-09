using Scalar.AspNetCore;

namespace ReSys.API.OpenApi;

public static class ScalarConfiguration
{
    internal static IApplicationBuilder UseScalarWithUi(this WebApplication app)
    {

        app.MapScalarApiReference(configureOptions: options =>
        {
            options.WithOpenApiRoutePattern(pattern: "/openapi/v1.json");
            options.Theme = ScalarTheme.Laserwave;
            options.AddPreferredSecuritySchemes(preferredSchemes: "Bearer");
        });
        return app;
    }
}
