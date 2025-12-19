using System.Diagnostics;
using System.Reflection;

using Mapster;

using MapsterMapper;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Feature.Admin.Catalog.Taxons;
using ReSys.Core.Feature.Common.Behaviors;
using ReSys.Core.Domain.Inventories.FulfillmentStrategies;

using Serilog;

namespace ReSys.Core;

public static class DependencyInjection
{
    #region Service Registration

    /// <summary>
    /// Registers all use cases layer services including MediatR, FluentValidation, and Mapster.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <returns>The configured service collection for method chaining</returns>
    public static IServiceCollection AddCore(
        this IServiceCollection services)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Information(messageTemplate: LogTemplates.ModuleRegistered,
                propertyValue0: "UseCases",
                propertyValue1: 0);

            int serviceCount = 0;

            // Register: CQRS pattern with MediatR
            services.AddCqrs();
            serviceCount++;

            // Register: FluentValidation pipeline
            services.AddValidations();
            serviceCount++;

            // Register: Mapster object mapping
            services.AddMappings();
            serviceCount++;

            stopwatch.Stop();

            Log.Information(
                messageTemplate: LogTemplates.ModuleRegistered,
                propertyValue0: "UseCases",
                propertyValue1: serviceCount);

            Log.Debug(
                messageTemplate: "UseCases layer configured in {Duration:0.0000}ms",
                propertyValue: stopwatch.Elapsed.TotalMilliseconds);

            return services;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Fatal(
                exception: ex,
                messageTemplate: LogTemplates.ComponentStartupFailed,
                propertyValue0: "UseCases",
                propertyValue1: ex.Message);

            throw;
        }
    }

    /// <summary>
    /// Configures use cases middleware (currently no middleware required).
    /// Reserved for future use case-specific middleware.
    /// </summary>
    /// <param name="app">The application builder</param>
    /// <returns>The configured application builder for method chaining</returns>
    public static IApplicationBuilder UseCore(this IApplicationBuilder app)
    {
        Log.Debug(messageTemplate: "UseCases middleware pipeline (no middleware configured)");
        return app;
    }

    #endregion

    #region CQRS Configuration

    /// <summary>
    /// Registers MediatR for CQRS pattern with validation pipeline behavior.
    /// Scans the executing assembly for command/query handlers.
    /// </summary>
    private static void AddCqrs(this IServiceCollection services)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Get: Assembly for handler scanning
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            // Register: MediatR with assembly scanning
            services.AddMediatR(configuration: cfg =>
            {
                cfg.RegisterServicesFromAssembly(assembly: executingAssembly);

                // Add: Validation pipeline behavior
                cfg.AddBehavior(serviceType: typeof(IPipelineBehavior<,>),
                    implementationType: typeof(ValidationBehavior<,>));
            });

            // Register: Domain services (factories, strategies, etc.)
            services.AddDomainServices();

            stopwatch.Stop();

            Log.Debug(
                messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "MediatR",
                propertyValue1: "Transient");

            Log.Debug(
                messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "ValidationBehavior",
                propertyValue1: "Transient");

            Log.Information(
                messageTemplate: LogTemplates.ConfigLoaded,
                propertyValue0: "MediatR",
                propertyValue1: new
                {
                    Assembly = executingAssembly.GetName().Name,
                    PipelineBehaviors = new[] { typeof(ValidationBehavior<,>) }
                });

            Log.Debug(
                messageTemplate: "CQRS services registered in {Duration:0.0000}ms",
                propertyValue: stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Error(
                exception: ex,
                messageTemplate: LogTemplates.OperationFailed,
                propertyValue0: "AddCqrs",
                propertyValue1: ex.Message);

            throw;
        }
    }

    #endregion

    #region Validation Configuration

    /// <summary>
    /// Registers FluentValidation validators from the executing assembly.
    /// Validators are automatically discovered and registered as scoped services.
    /// </summary>
    private static void AddValidations(this IServiceCollection services)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Get: Assembly for validator scanning
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            // Register: All validators from assembly
            services.AddValidatorsFromAssembly(assembly: executingAssembly);

            // Discover: Validator types for logging
            List<Type> validatorTypes = executingAssembly
                .GetTypes()
                .Where(predicate: t => t.GetInterfaces()
                    .Any(predicate: i => i.IsGenericType &&
                                         i.GetGenericTypeDefinition() == typeof(IValidator<>)))
                .ToList();

            stopwatch.Stop();

            Log.Debug(
                messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "FluentValidation",
                propertyValue1: "Scoped");

            Log.Information(
                messageTemplate: LogTemplates.ConfigLoaded,
                propertyValue0: "FluentValidation",
                propertyValue1: new
                {
                    Assembly = executingAssembly.GetName().Name,
                    ValidatorCount = validatorTypes.Count,
                    Validators = validatorTypes.Select(selector: t => t.Name).Take(count: 5).ToArray()
                });

            Log.Debug(
                messageTemplate: "Validation services registered in {Duration:0.0000}ms",
                propertyValue: stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Error(
                exception: ex,
                messageTemplate: LogTemplates.OperationFailed,
                propertyValue0: "AddValidations",
                propertyValue1: ex.Message);

            throw;
        }
    }

    #endregion

    #region Object Mapping Configuration

    /// <summary>
    /// Registers Mapster for high-performance object mapping with global configuration.
    /// Configuration is scanned from the executing assembly and registered as singleton.
    /// </summary>
    private static void AddMappings(this IServiceCollection services)
    {
        Stopwatch stopwatch = Stopwatch.StartNew();

        try
        {
            // Get: Assembly for mapping configuration scanning
            Assembly executingAssembly = Assembly.GetExecutingAssembly();

            // Create: Global Mapster configuration
            TypeAdapterConfig config = TypeAdapterConfig.GlobalSettings;

            // Scan: Assembly for mapping configurations
            config.Scan(assemblies: executingAssembly);

            // Register: Global configuration as singleton
            services.AddSingleton(implementationInstance: config);

            Log.Debug(
                messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "TypeAdapterConfig",
                propertyValue1: "Singleton");

            // Register: Mapper service as scoped
            services.AddScoped<IMapper, ServiceMapper>();

            Log.Debug(
                messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "IMapper",
                propertyValue1: "Scoped");

            // Compute: Mapping statistics
            int mappingCount = config.RuleMap.Count;

            stopwatch.Stop();

            Log.Information(
                LogTemplates.ConfigLoaded,
                "Master",
                new
                {
                    Assembly = executingAssembly.GetName().Name,
                    MappingRules = mappingCount,
                    ConfigurationType = "Global",
                });

            foreach (var kvp in config.RuleMap)
            {
                var typeTuple = kvp.Key; // TypeTuple
                var sourceType = typeTuple.Source;
                var destinationType = typeTuple.Destination;

                Log.Debug(
                    "Mapping configured: {Source} -> {Destination}",
                    sourceType.FullName,
                    destinationType.FullName
                );
            }
            Log.Debug(
                messageTemplate: "Object mapping services registered in {Duration:0.0000}ms",
                propertyValue: stopwatch.Elapsed.TotalMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Error(
                exception: ex,
                messageTemplate: LogTemplates.OperationFailed,
                propertyValue0: "AddMappings",
                propertyValue1: ex.Message);

            throw;
        }
    }

    #endregion

    #region Domain Services Configuration

    /// <summary>
    /// Registers domain services including factories, strategies, and other domain-specific services.
    /// </summary>
    private static void AddDomainServices(this IServiceCollection services)
    {
        // Register: Taxon Hierarchy services
        services.AddScoped<TaxonModule.Services.IHierarchy, TaxonModule.Services.HierarchyService>();
        services.AddScoped<TaxonModule.Services.IRegeneration, TaxonModule.Services.RegenerationService>();

        Log.Debug(
            messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "TaxonModule.Services.HierarchyService",
            propertyValue1: "Scoped");

        Log.Debug(
            messageTemplate: LogTemplates.ServiceRegistered,
            propertyValue0: "TaxonModule.Services.RegenerationService",
            propertyValue1: "Scoped");


        // Register: Fulfillment Strategy Factory
        services.AddScoped<FulfillmentStrategyFactory>();
        services.AddScoped<IFulfillmentPlanner, FulfillmentPlanner>();
    }

    #endregion
}
