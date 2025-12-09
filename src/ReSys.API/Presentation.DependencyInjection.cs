using System.Diagnostics;

using Carter;

using ReSys.API.Configurations;
using ReSys.API.Middlewares.Exceptions;
using ReSys.API.Middlewares.Paramesters;
using ReSys.API.OpenApi;
using ReSys.Core.Common.Constants;
using ReSys.Infrastructure.Security;

using Serilog;

namespace ReSys.API;

/// <summary>
/// Configures presentation layer services including endpoints, documentation,
/// error handling, CORS, and health checks for ASP.NET Core Web API.
/// </summary>
public static class DependencyInjection
{
    #region Service Registration

    /// <summary>
    /// Registers all presentation-layer services including Carter endpoints,
    /// documentation, error handling, CORS, and health checks.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="builderEnvironment"></param>
    /// <returns>The configured service collection for method chaining</returns>
    public static IServiceCollection AddPresentation(this IServiceCollection services,
        IConfiguration configuration, IWebHostEnvironment builderEnvironment)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Information(messageTemplate: LogTemplates.ModuleRegistered,
                propertyValue0: "Presentation",
                propertyValue1: 0);

            var serviceCount = 0;

            // Register: Carter for minimal API endpoints
            services.AddCarter();
            serviceCount++;
            Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "Carter",
                propertyValue1: "Singleton");

            // Configure: JSON serialization options
            services.AddJsonConfig();
            serviceCount++;
            Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "JsonOptions",
                propertyValue1: "Singleton");

            // Register: API explorer for endpoint discovery
            services.AddEndpointsExplorer();
            serviceCount++;

            // Register: Global error handling
            services.AddErrorHandlers();
            serviceCount++;

            // Register: Session, CORS, and data protection
            services.AddSessionAndCors(configuration: configuration);
            serviceCount++;

            stopwatch.Stop();

            Log.Information(
                messageTemplate: LogTemplates.ModuleRegistered,
                propertyValue0: "Presentation",
                propertyValue1: serviceCount);

            Log.Debug(
                messageTemplate: "Presentation layer configured in {Duration:0.0000}ms",
                propertyValue: stopwatch.Elapsed.TotalMilliseconds);

            return services;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Fatal(
                exception: ex,
                messageTemplate: LogTemplates.ComponentStartupFailed,
                propertyValue0: "Presentation",
                propertyValue1: ex.Message);

            throw;
        }
    }

    #endregion

    #region Middleware Pipeline

    /// <summary>
    /// Configures the middleware pipeline for the presentation layer including
    /// documentation, error handling, routing, authentication, and endpoints.
    /// </summary>
    /// <param name="app">The web application builder</param>
    /// <returns>The configured application builder for method chaining</returns>
    public static IApplicationBuilder UsePresentation(this WebApplication app)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Information(messageTemplate: "Configuring presentation middleware pipeline");

            var middlewareCount = 0;

            // Early: Exception handling (before any other middleware)
            app.UseExceptionHandler();

            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "ExceptionHandler",
                propertyValue1: middlewareCount);

            // Dev-only: API documentation
            if (app.Environment.IsDevelopment())
            {
                app.UseEndpointsExplorer();
                middlewareCount++;
                Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                    propertyValue0: "ApiDocumentation",
                    propertyValue1: middlewareCount);
            }

            // Security redirects
            app.UseHttpsRedirection();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "HttpsRedirection",
                propertyValue1: middlewareCount);

            // Static files
            app.UseStaticFiles();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "StaticFiles",
                propertyValue1: middlewareCount);

            // Routing (early in pipeline)
            app.UseRouting();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "Routing",
                propertyValue1: middlewareCount);

            // Custom middleware (e.g., query normalization)
            app.UseMiddleware<QueryKeyNormalizationMiddleware>();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "QueryKeyNormalization",
                propertyValue1: middlewareCount);

            // Session management (after routing)
            app.UseSession();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "Session",
                propertyValue1: middlewareCount);

            // CORS (after routing, before endpoints)
            app.UseCors();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "CORS",
                propertyValue1: middlewareCount);

            // Security (from infrastructure security)
            app.UseSecurity();
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "Authorization",
                propertyValue1: 1);
            middlewareCount++;

            // Global error handling (after auth)
            app.UseErrorHandlers();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "ErrorHandling",
                propertyValue1: middlewareCount);

            // Endpoints mapping
            app.MapCarter();
            middlewareCount++;
            Log.Debug(messageTemplate: LogTemplates.MiddlewareAdded,
                propertyValue0: "CarterEndpoints",
                propertyValue1: middlewareCount);

            stopwatch.Stop();

            // Log: Application ready
            string hostUrl = app.Urls.FirstOrDefault() ?? "http://localhost";
            Log.Information(messageTemplate: LogTemplates.AppReady,
                propertyValue: hostUrl);

            Log.Debug(
                messageTemplate: "Presentation middleware configured ({MiddlewareCount} components) in {Duration:0.0000}ms",
                propertyValue0: middlewareCount,
                propertyValue1: stopwatch.Elapsed.TotalMilliseconds);

            return app;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Error(
                exception: ex,
                messageTemplate: "Presentation middleware configuration failed: {ErrorMessage}",
                propertyValue: ex.Message);

            throw;
        }
    }

    #endregion

    #region Error Handling Configuration

    /// <summary>
    /// Registers centralized exception handling and problem details customization.
    /// </summary>
    private static void AddErrorHandlers(this IServiceCollection services)
    {
        // Register: Global exception handler
        services.AddExceptionHandler<GlobalExceptionHandler>();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "GlobalExceptionHandler",
            propertyValue1: "Singleton");

        // Configure: Problem details customization
        services.AddProblemDetails(configure: ConfigureProblemDetails);
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "ProblemDetails",
            propertyValue1: "Singleton");
    }

    /// <summary>
    /// Configures global error handling middleware in the pipeline.
    /// </summary>
    private static void UseErrorHandlers(this IApplicationBuilder app)
    {
        // Add: Custom error handling middleware (if separate from UseExceptionHandler)
        // app.UseMiddleware<GlobalErrorHandlingMiddleware>(); // If needed
    }

    /// <summary>
    /// Customizes problem details to include trace ID and user agent.
    /// </summary>
    private static void ConfigureProblemDetails(ProblemDetailsOptions options)
    {
        // Customize: Add diagnostic information to problem details
        options.CustomizeProblemDetails = context =>
        {
            // Add: Trace ID for distributed tracing
            context.ProblemDetails.Extensions[key: "trace_id"] =
                context.HttpContext.TraceIdentifier;

            // Add: User agent for client identification
            context.ProblemDetails.Extensions[key: "user_agent"] =
                context.HttpContext.Request.Headers.UserAgent.ToString();
        };
    }

    #endregion

    #region Session & CORS Configuration

    /// <summary>
    /// Registers session management, CORS policies, and data protection services.
    /// </summary>
    private static void AddSessionAndCors(this IServiceCollection services,
        IConfiguration configuration)
    {

        // Configure: CORS policy (customize for production; use config-driven origins)
        //services.AddCors(setupAction: options =>
        //{
        //    options.AddDefaultPolicy(configurePolicy: builder => builder
        //        .WithOrigins(origins: configuration.GetValue<string>(key: "Cors:AllowedOrigins")?.Split(separator: ',') ?? new[] { "http://localhost:4200" })
        //        .AllowAnyHeader()
        //        .AllowAnyMethod()
        //        .AllowCredentials());
        //});
        services.AddCors(setupAction: options =>
        {
            options.AddDefaultPolicy(configurePolicy: builder => builder
                .AllowAnyOrigin()
                .AllowAnyHeader()
                .AllowAnyMethod());
        });


        Log.Debug(
            messageTemplate: LogTemplates.ConfigLoaded,
            propertyValue0: "CORS",
            propertyValue1: new { Policy = "Default", Environment = "Config-Driven" });

        // Configure: Secure session management
        services.AddSession(configure: options =>
        {
            // Set: Session timeout
            options.IdleTimeout = TimeSpan.FromMinutes(minutes: 10);

            // Enforce: Secure cookie settings
            options.Cookie.HttpOnly = true;
            options.Cookie.IsEssential = true;
            options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
            options.Cookie.SameSite = SameSiteMode.Lax;
        });

        Log.Debug(
            messageTemplate: LogTemplates.ConfigLoaded,
            propertyValue0: "Session",
            propertyValue1: new { Timeout = "10min", Secure = true });

        //// Configure: Data protection with key persistence
        //services.AddDataProtection()
        //    .PersistKeysToFileSystem(directory: new DirectoryInfo(path: "./keys"))
        //    .SetApplicationName(applicationName: "ReSys.Shop")
        //    .SetDefaultKeyLifetime(lifetime: TimeSpan.FromDays(days: 14));

        //Log.Debug(
        //    messageTemplate: LogTemplates.ConfigLoaded,
        //    propertyValue0: "DataProtection",
        //    propertyValue1: new { KeyLocation = "./keys", KeyLifetime = "14days" });
    }

    #endregion

    #region Documentation Configuration

    /// <summary>
    /// Registers OpenAPI/Swagger documentation with authentication integration.
    /// </summary>
    private static void AddEndpointsExplorer(this IServiceCollection services)
    {
        // Register: Endpoints API explorer
        services.AddEndpointsApiExplorer();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "EndpointsApiExplorer",
            propertyValue1: "Singleton");

        // Register: OpenAPI specification with authentication
        services.AddOpenApiWithAuth();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "OpenApi",
            propertyValue1: "Singleton");

        // Register: Swagger UI with authentication
        services.AddSwaggerWithAuth();
        Log.Debug(messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "Swagger",
            propertyValue1: "Singleton");
    }

    /// <summary>
    /// Configures API documentation middleware (OpenAPI, Swagger, Scalar).
    /// </summary>
    private static void UseEndpointsExplorer(this WebApplication app)
    {
        // Map: OpenAPI specification endpoint
        app.MapOpenApi();

        // Configure: Swagger UI
        app.UseSwaggerWithUi();

        // Configure: Scalar API documentation
        app.UseScalarWithUi();

        Log.Information(messageTemplate: "API documentation available at /swagger and /scalar");
    }

    #endregion
}