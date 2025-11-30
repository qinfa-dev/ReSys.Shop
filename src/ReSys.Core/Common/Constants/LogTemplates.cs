namespace SharedKernel.Logging;

/// <summary>
/// Provides predefined log message templates for consistent structured logging across
/// ASP.NET Core Web APIs using Serilog best practices.
/// <para>
/// <b>Design Principles:</b>
/// <list type="bullet">
/// <item>PascalCase property names (Serilog convention)</item>
/// <item>Destructuring (@) for complex objects, avoid for primitives</item>
/// <item>Performance tracking with TimeSpan/Duration</item>
/// <item>Correlation IDs for distributed tracing</item>
/// <item>NO sensitive data (passwords, tokens, full user claims)</item>
/// <item>Appropriate log levels to prevent noise</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Quick Setup (Program.cs):</b>
/// <code>
/// using Serilog;
/// using Serilog.Events;
/// 
/// Log.Logger = new LoggerConfiguration()
///     .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
///     .Enrich.FromLogContext()
///     .Enrich.WithMachineName()
///     .Enrich.WithEnvironmentName()
///     .WriteTo.Console()
///     .WriteTo.File("logs/app-.log", 
///         rollingInterval: RollingInterval.Day,
///         retainedFileCountLimit: 30)
///     .CreateLogger();
/// 
/// try
/// {
///     var builder = WebApplication.CreateBuilder(args);
///     builder.Host.UseSerilog();
///     
///     var app = builder.Build();
///     
///     app.UseSerilogRequestLogging(opts =>
///     {
///         opts.MessageTemplate => "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000}ms";
///         opts.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
///         {
///             diagnosticContext.Set("RequestHost", httpContext.Request.Host.Value);
///             diagnosticContext.Set("RequestScheme", httpContext.Request.Scheme);
///             diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress);
///             diagnosticContext.Set("UserAgent", httpContext.Request.Headers["User-Agent"].ToString());
///         };
///     });
///     
///     app.Run();
///     Log.Information("Application stopped cleanly");
/// }
/// catch (Exception ex)
/// {
///     Log.Fatal(ex, "Application terminated unexpectedly");
/// }
/// finally
/// {
///     Log.CloseAndFlush();
/// }
/// </code>
/// </para>
/// 
/// <para>
/// <b>Recommended NuGet Packages:</b>
/// <list type="bullet">
/// <item>Serilog.AspNetCore (includes core + ASP.NET integration)</item>
/// <item>Serilog.Sinks.Console</item>
/// <item>Serilog.Sinks.File</item>
/// <item>Serilog.Enrichers.Environment</item>
/// <item>Serilog.Expressions (for filtering)</item>
/// <item>Serilog.Sinks.Seq (for centralized logging - optional)</item>
/// </list>
/// </para>
/// </summary>
public static class LogTemplates
{
    #region Application Lifecycle (Information Level)

    /// <summary>
    /// Application starting with environment and version info.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>Log.Information(LogTemplates.AppStarting, env.EnvironmentName, version);</c></para>
    /// </summary>
    public static string AppStarting => "Application starting in {Environment} environment (v{Version})";

    /// <summary>
    /// Application ready to accept requests.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>Log.Information(LogTemplates.AppReady, "https://localhost:5001");</c></para>
    /// </summary>
    public static string AppReady => "Application ready and listening on {Url}";

    /// <summary>
    /// Graceful shutdown initiated.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>Log.Information(LogTemplates.AppStopping);</c></para>
    /// </summary>
    public static string AppStopping => "Application shutdown initiated";

    /// <summary>
    /// Fatal startup error.
    /// <para><b>Level:</b> Fatal</para>
    /// <para><b>Usage:</b> <c>Log.Fatal(ex, LogTemplates.AppStartupFailed, ex.Message);</c></para>
    /// </summary>
    public static string AppStartupFailed => "Application failed to start: {ErrorMessage}";

    /// <summary>
    /// Component or module failed to start.
    /// <para><b>Level:</b> Fatal/Error</para>
    /// <para><b>Usage:</b> <c>Log.Fatal(ex, LogTemplates.ComponentStartupFailed, "DatabaseService", ex.Message);</c></para>
    /// </summary>
    public static string ComponentStartupFailed => "Component {ComponentName} failed to start: {ErrorMessage}";

    #endregion

    #region Dependency Injection & Configuration (Debug/Information Level)

    /// <summary>
    /// Service registered in DI container.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.ServiceRegistered, nameof(IUserService), "Scoped");</c></para>
    /// </summary>
    public static string ServiceRegistered => "Service {ServiceType} registered with {Lifetime} lifetime";

    /// <summary>
    /// Multiple services registered for a module/feature.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>Log.Information(LogTemplates.ModuleRegistered, "Authentication Management", 5);</c></para>
    /// </summary>
    public static string ModuleRegistered => "{ModuleName} module registered ({ServiceCount} services)";

    /// <summary>
    /// Configuration section loaded.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.ConfigLoaded, "JwtSettings", new { Issuer, Audience });</c></para>
    /// </summary>
    public static string ConfigLoaded => "Configuration loaded: {SectionName} {@Settings}";

    /// <summary>
    /// Middleware configured in pipeline.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.MiddlewareAdded, "CORS", 2);</c></para>
    /// </summary>
    public static string MiddlewareAdded => "Middleware {MiddlewareName} added at position {Order}";

    #endregion

    #region HTTP Request/Response (Information Level - handled by UseSerilogRequestLogging)

    /// <summary>
    /// Custom request started (use only if NOT using UseSerilogRequestLogging).
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.RequestStarted, method, path, correlationId);</c></para>
    /// </summary>
    public static string RequestStarted => "Request {Method} {Path} started (CorrelationId: {CorrelationId})";

    /// <summary>
    /// Custom request completed (use only if NOT using UseSerilogRequestLogging).
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.RequestCompleted, method, path, statusCode, elapsed);</c></para>
    /// </summary>
    public static string RequestCompleted => "Request {Method} {Path} completed with {StatusCode} in {Elapsed:0.0000}ms";

    /// <summary>
    /// Route matched to endpoint.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.RouteMatched, "/api/users/{id}", "GetUserById");</c></para>
    /// </summary>
    public static string RouteMatched => "Route {RouteTemplate} matched to endpoint {EndpointName}";

    #endregion

    #region Business Operations (Information/Debug Level)

    /// <summary>
    /// Operation started with parameters.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.OperationStarted, "CreateUser", new { Email, Role });</c></para>
    /// </summary>
    public static string OperationStarted => "Operation {OperationName} started {@Parameters}";

    /// <summary>
    /// Operation completed successfully with timing.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.OperationCompleted, "CreateUser", userId, elapsed);</c></para>
    /// </summary>
    public static string OperationCompleted => "Operation {OperationName} completed successfully (Result: {ResultId}, Duration: {Duration:0.0000}ms)";

    /// <summary>
    /// Operation failed with error details.
    /// <para><b>Level:</b> Error</para>
    /// <para><b>Usage:</b> <c>_logger.LogError(ex, LogTemplates.OperationFailed, "CreateUser", ex.Message);</c></para>
    /// </summary>
    public static string OperationFailed => "Operation {OperationName} failed: {ErrorMessage}";

    /// <summary>
    /// Slow operation exceeding threshold.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.SlowOperation, "GetUsers", elapsed, 1000);</c></para>
    /// </summary>
    public static string SlowOperation => "Slow operation detected: {OperationName} took {Duration:0.0000}ms (threshold: {ThresholdMs}ms)";

    #endregion

    #region Database Operations (Debug Level)

    /// <summary>
    /// Database query executed.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.DbQuery, "Users", "GetById", userId);</c></para>
    /// </summary>
    public static string DbQuery => "DB Query: {Table}.{Operation} (Entity: {EntityId})";

    /// <summary>
    /// Database command executed (Insert/Update/Delete).
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.DbCommand, "Insert", "Users", 1, elapsed);</c></para>
    /// </summary>
    public static string DbCommand => "DB {CommandType}: {Table} ({RowsAffected} rows in {Duration:0.00}ms)";

    /// <summary>
    /// Database connection established.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.DbConnected, "Primary", dbName);</c></para>
    /// </summary>
    public static string DbConnected => "Database connection established: {ConnectionName} to {DatabaseName}";

    /// <summary>
    /// Database operation failed.
    /// <para><b>Level:</b> Error</para>
    /// <para><b>Usage:</b> <c>_logger.LogError(ex, LogTemplates.DbError, "Query", "Users", ex.Message);</c></para>
    /// </summary>
    public static string DbError => "Database {OperationType} failed on {Table}: {ErrorMessage}";

    #endregion

    #region External Services & APIs (Information Level)

    /// <summary>
    /// External API call started.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.ExternalCallStarted, "PaymentService", "POST", "/api/payments");</c></para>
    /// </summary>
    public static string ExternalCallStarted => "Calling external service {ServiceName} {Method} {Endpoint}";

    /// <summary>
    /// External API call completed.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.ExternalCallCompleted, "PaymentService", 200, elapsed);</c></para>
    /// </summary>
    public static string ExternalCallCompleted => "External service {ServiceName} responded {StatusCode} in {Duration:0.0000}ms";

    /// <summary>
    /// External API call failed.
    /// <para><b>Level:</b> Error</para>
    /// <para><b>Usage:</b> <c>_logger.LogError(ex, LogTemplates.ExternalCallFailed, "PaymentService", ex.Message);</c></para>
    /// </summary>
    public static string ExternalCallFailed => "External service {ServiceName} call failed: {ErrorMessage}";

    /// <summary>
    /// External service timeout.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.ExternalCallTimeout, "PaymentService", 30000, elapsed);</c></para>
    /// </summary>
    public static string ExternalCallTimeout => "External service {ServiceName} timed out (Timeout: {TimeoutMs}ms, Elapsed: {Duration:0.0000}ms)";

    #endregion

    #region Caching (Debug Level)

    /// <summary>
    /// Cache hit.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.CacheHit, "user-profile-123");</c></para>
    /// </summary>
    public static string CacheHit => "Cache HIT: {CacheKey}";

    /// <summary>
    /// Cache miss - data will be fetched from source.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.CacheMiss, "user-profile-123");</c></para>
    /// </summary>
    public static string CacheMiss => "Cache MISS: {CacheKey} - fetching from source";

    /// <summary>
    /// Cache entry set.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.CacheSet, "user-profile-123", expiration);</c></para>
    /// </summary>
    public static string CacheSet => "Cache SET: {CacheKey} (Expiration: {Expiration})";

    /// <summary>
    /// Cache entry removed.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.CacheRemoved, "user-profile-123");</c></para>
    /// </summary>
    public static string CacheRemoved => "Cache REMOVED: {CacheKey}";

    #endregion

    #region Authentication & Authorization (Information/Warning Level)

    /// <summary>
    /// User successfully authenticated.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.UserAuthenticated, userId, "JWT", clientIp);</c></para>
    /// </summary>
    public static string UserAuthenticated => "User {UserId} authenticated via {AuthScheme} from {ClientIp}";

    /// <summary>
    /// Authentication failed.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.AuthFailed, "JWT", "Invalid signature", clientIp);</c></para>
    /// </summary>
    public static string AuthFailed => "Authentication failed: {AuthScheme} - {Reason} from {ClientIp}";

    /// <summary>
    /// Authorization check succeeded.
    /// <para><b>Level:</b> Debug</para>
    /// <para><b>Usage:</b> <c>_logger.LogDebug(LogTemplates.AuthorizedAccess, userId, resource, policy);</c></para>
    /// </summary>
    public static string AuthorizedAccess => "User {UserId} authorized to access {Resource} via policy {PolicyName}";

    /// <summary>
    /// Authorization denied.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.AccessDenied, userId, resource, policy);</c></para>
    /// </summary>
    public static string AccessDenied => "Access denied: User {UserId} to {Resource} - Policy {PolicyName} not satisfied";

    /// <summary>
    /// User logged out.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.UserLoggedOut, userId);</c></para>
    /// </summary>
    public static string UserLoggedOut => "User {UserId} logged out";

    #endregion

    #region Validation (Warning Level)

    /// <summary>
    /// Model validation failed.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.ValidationFailed, "CreateUser", errors);</c></para>
    /// </summary>
    public static string ValidationFailed => "Validation failed for {OperationName}: {@ValidationErrors}";

    /// <summary>
    /// Business rule violation.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.BusinessRuleViolation, "CreateOrder", "Insufficient stock");</c></para>
    /// </summary>
    public static string BusinessRuleViolation => "Business rule violation in {OperationName}: {RuleViolation}";

    #endregion

    #region Background Services & Jobs (Information Level)

    /// <summary>
    /// Background service started.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.BackgroundServiceStarted, nameof(EmailSenderService));</c></para>
    /// </summary>
    public static string BackgroundServiceStarted => "Background service {ServiceName} started";

    /// <summary>
    /// Background service stopped.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.BackgroundServiceStopped, nameof(EmailSenderService));</c></para>
    /// </summary>
    public static string BackgroundServiceStopped => "Background service {ServiceName} stopped";

    /// <summary>
    /// Scheduled job executed.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.JobExecuted, "CleanupOldLogs", elapsed, itemsProcessed);</c></para>
    /// </summary>
    public static string JobExecuted => "Job {JobName} completed in {Duration:0.0000}ms (Processed: {ItemCount} items)";

    /// <summary>
    /// Scheduled job failed.
    /// <para><b>Level:</b> Error</para>
    /// <para><b>Usage:</b> <c>_logger.LogError(ex, LogTemplates.JobFailed, "CleanupOldLogs", ex.Message);</c></para>
    /// </summary>
    public static string JobFailed => "Job {JobName} failed: {ErrorMessage}";

    #endregion

    #region Health Checks & Monitoring (Information/Warning/Error Level)

    /// <summary>
    /// Health check passed.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.HealthCheckPassed, "Database", elapsed);</c></para>
    /// </summary>
    public static string HealthCheckPassed => "Health check {CheckName} passed in {Duration:0.00}ms";

    /// <summary>
    /// Health check degraded.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.HealthCheckDegraded, "Redis", reason);</c></para>
    /// </summary>
    public static string HealthCheckDegraded => "Health check {CheckName} degraded: {Reason}";

    /// <summary>
    /// Health check failed.
    /// <para><b>Level:</b> Error</para>
    /// <para><b>Usage:</b> <c>_logger.LogError(LogTemplates.HealthCheckFailed, "Database", reason);</c></para>
    /// </summary>
    public static string HealthCheckFailed => "Health check {CheckName} failed: {Reason}";

    /// <summary>
    /// High resource usage detected.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.HighResourceUsage, "CPU", 85.5, 80);</c></para>
    /// </summary>
    public static string HighResourceUsage => "High {ResourceType} usage: {CurrentUsage:0.00}% (Threshold: {Threshold}%)";

    #endregion

    #region Rate Limiting & Security (Warning Level)

    /// <summary>
    /// Rate limit exceeded.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.RateLimitExceeded, clientIp, endpoint);</c></para>
    /// </summary>
    public static string RateLimitExceeded => "Rate limit exceeded: {ClientIp} on {Endpoint}";

    /// <summary>
    /// Suspicious activity detected.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.SuspiciousActivity, activityType, clientIp, details);</c></para>
    /// </summary>
    public static string SuspiciousActivity => "Suspicious activity detected: {ActivityType} from {ClientIp} - {Details}";

    /// <summary>
    /// CORS policy violation.
    /// <para><b>Level:</b> Warning</para>
    /// <para><b>Usage:</b> <c>_logger.LogWarning(LogTemplates.CorsViolation, origin, method);</c></para>
    /// </summary>
    public static string CorsViolation => "CORS policy violation: Origin {Origin} attempted {Method}";

    #endregion

    #region Message Queue & Events (Information Level)

    /// <summary>
    /// Message published to queue.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.MessagePublished, "OrderCreated", "orders-queue", messageId);</c></para>
    /// </summary>
    public static string MessagePublished => "Message published: {MessageType} to {QueueName} (MessageId: {MessageId})";

    /// <summary>
    /// Message consumed from queue.
    /// <para><b>Level:</b> Information</para>
    /// <para><b>Usage:</b> <c>_logger.LogInformation(LogTemplates.MessageConsumed, "OrderCreated", "orders-queue", messageId, elapsed);</c></para>
    /// </summary>
    public static string MessageConsumed => "Message consumed: {MessageType} from {QueueName} (MessageId: {MessageId}, Duration: {Duration:0.0000}ms)";

    /// <summary>
    /// Message processing failed.
    /// <para><b>Level:</b> Error</para>
    /// <para><b>Usage:</b> <c>_logger.LogError(ex, LogTemplates.MessageFailed, "OrderCreated", messageId, retryCount, ex.Message);</c></para>
    /// </summary>
    public static string MessageFailed => "Message processing failed: {MessageType} (MessageId: {MessageId}, Retry: {RetryCount}) - {ErrorMessage}";

    #endregion

    #region Feature & Provider Management (Warning Level)

    /// <summary>
    /// Feature or capability has been explicitly disabled via configuration or feature flag.
    /// <para><b>Level:</b> Warning</para>
    /// <para>
    /// <b>Usage:</b> 
    /// <c>_logger.LogWarning(LogTemplates.FeatureDisabled, "Email Notifications");</c>
    /// </para>
    /// </summary>
    /// <remarks>
    /// Useful for detecting when a feature is intentionally turned off to avoid confusion in production logs.
    /// Avoid logging sensitive feature names (e.g., internal flags or experimental features).
    /// </remarks>
    public static string FeatureDisabled => "Feature disabled: {FeatureName}";

    /// <summary>
    /// Unknown or unsupported service provider detected during configuration or runtime initialization.
    /// <para><b>Level:</b> Warning</para>
    /// <para>
    /// <b>Usage:</b> 
    /// <c>_logger.LogWarning(LogTemplates.UnknownProvider, "Email", "CustomMailProvider");</c>
    /// </para>
    /// </summary>
    /// <remarks>
    /// This template should be used when a configured provider name does not match any known or supported implementation,
    /// such as an unrecognized email or SMS provider.
    /// Helps identify misconfiguration or typos in application settings.
    /// </remarks>
    public static string UnknownProvider => "Unknown {ServiceType} provider: {Provider}";

    #endregion
}