using System.Diagnostics;

using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using ReSys.Core.Common.Constants;
using ReSys.Core.Feature.Common.Storage.Services;
using ReSys.Infrastructure.Storages.Options;
using ReSys.Infrastructure.Storages.Providers;

using Serilog;

namespace ReSys.Infrastructure.Storages;

/// <summary>
/// Configures file storage services with environment-specific providers.
/// Uses local file system for development, Azure Blob Storage for production.
/// </summary>
public static class StorageConfiguration
{
    #region Service Registration

    /// <summary>
    /// Registers storage services with environment-specific implementations
    /// and validates configuration options on startup.
    /// </summary>
    /// <param name="services">The service collection to configure</param>
    /// <param name="configuration">Application configuration</param>
    /// <param name="environment">Web host environment</param>
    /// <returns>The configured service collection for method chaining</returns>
    public static IServiceCollection AddStorageServices(
        this IServiceCollection services,
        IConfiguration configuration,
        IWebHostEnvironment environment)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            Log.Information(messageTemplate: "Configuring storage services");

            // Register: Storage options with validation
            services.Configure<StorageOptions>(config: configuration.GetSection(key: StorageOptions.Section))
                .AddOptionsWithValidateOnStart<StorageOptions>()
                .Validate(
                    validation: options => ValidateStorageOptions(options: options,
                        environment: environment),
                    failureMessage: "Storage configuration is invalid");

            Log.Debug(
                messageTemplate: LogTemplates.ConfigLoaded,
                propertyValue0: "StorageOptions",
                propertyValue1: new { StorageOptions.Section });

            // Register: Environment-specific storage service
            if (environment.IsDevelopment())
            {
                // Development: Local file system storage
                services.AddScoped<IStorageService, LocalStorageService>();

                Log.Information(
                    messageTemplate: LogTemplates.ConfigLoaded,
                    propertyValue0: "StorageProvider",
                    propertyValue1: new
                    {
                        Environment = "Development",
                        Provider = "LocalFileSystem"
                    });
            }
            else
            {
                // Production/Staging: Azure Blob Storage
                services.AddScoped<IStorageService, AzureStorageService>();

                Log.Information(
                    messageTemplate: LogTemplates.ConfigLoaded,
                    propertyValue0: "StorageProvider",
                    propertyValue1: new
                    {
                        Environment = environment.EnvironmentName,
                        Provider = "AzureBlobStorage"
                    });
            }

            Log.Debug(
                messageTemplate: LogTemplates.ServiceRegistered,
                propertyValue0: "IStorageService",
                propertyValue1: "Scoped");

            stopwatch.Stop();

            Log.Information(
                messageTemplate: "Storage services configured in {Duration:0.0000}ms",
                propertyValue: stopwatch.Elapsed.TotalMilliseconds);

            return services;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            Log.Fatal(
                exception: ex,
                messageTemplate: LogTemplates.ComponentStartupFailed,
                propertyValue0: "Storage",
                propertyValue1: ex.Message);

            throw;
        }
    }

    #endregion

    #region Validation

    /// <summary>
    /// Validates storage options based on environment-specific requirements.
    /// </summary>
    private static bool ValidateStorageOptions(
        StorageOptions options,
        IWebHostEnvironment environment)
    {
        try
        {
            bool isValid = environment.IsDevelopment()
                ? ValidateLocalStorageOptions(options: options)
                : ValidateAzureStorageOptions(options: options);

            if (isValid)
            {
                Log.Debug(
                    messageTemplate: LogTemplates.ConfigLoaded,
                    propertyValue0: "StorageValidation",
                    propertyValue1: new
                    {
                        Environment = environment.EnvironmentName,
                        Valid = true
                    });
            }
            else
            {
                Log.Error(
                    messageTemplate: LogTemplates.ValidationFailed,
                    propertyValue0: "StorageOptions",
                    propertyValue1: new { Environment = environment.EnvironmentName });
            }

            return isValid;
        }
        catch (Exception ex)
        {
            Log.Error(
                exception: ex,
                messageTemplate: LogTemplates.ValidationFailed,
                propertyValue0: "StorageOptions",
                propertyValue1: new { ErrorMessage = ex.Message });

            return false;
        }
    }

    /// <summary>
    /// Validates local file system storage configuration.
    /// Ensures local path, base URL, and file constraints are properly set.
    /// </summary>
    private static bool ValidateLocalStorageOptions(StorageOptions options)
    {
        // Validate: Local path must be specified
        if (string.IsNullOrWhiteSpace(value: options.LocalPath))
        {
            Log.Error(messageTemplate: "Storage validation failed: LocalPath is required for development");
            return false;
        }

        // Validate: Base URL must be specified
        if (string.IsNullOrWhiteSpace(value: options.BaseUrl))
        {
            Log.Error(messageTemplate: "Storage validation failed: BaseUrl is required");
            return false;
        }

        // Validate: Max file size must be positive
        if (options.MaxFileSizeBytes <= 0)
        {
            Log.Error(
                messageTemplate: "Storage validation failed: MaxFileSizeBytes must be positive (got {MaxSize})",
                propertyValue: options.MaxFileSizeBytes);
            return false;
        }

        // Validate: Allowed extensions must exist
        if (options.AllowedExtensions.Length == 0)
        {
            Log.Error(messageTemplate: "Storage validation failed: AllowedExtensions must contain at least one extension");
            return false;
        }

        Log.Debug(
            messageTemplate: "Local storage validation passed: Path={LocalPath}, MaxSize={MaxSize}MB, Extensions={Extensions}",
            propertyValue0: options.LocalPath,
            propertyValue1: options.MaxFileSizeBytes / 1024 / 1024,
            propertyValue2: string.Join(separator: ", ",
                value: options.AllowedExtensions));

        return true;
    }

    /// <summary>
    /// Validates Azure Blob Storage configuration.
    /// Ensures connection string, container name, and file constraints are properly set.
    /// </summary>
    private static bool ValidateAzureStorageOptions(StorageOptions options)
    {
        // Validate: Azure connection string must be specified
        if (string.IsNullOrWhiteSpace(value: options.AzureConnectionString))
        {
            Log.Error(messageTemplate: "Storage validation failed: AzureConnectionString is required for production");
            return false;
        }

        // Validate: Azure container name must be specified
        if (string.IsNullOrWhiteSpace(value: options.AzureContainerName))
        {
            Log.Error(messageTemplate: "Storage validation failed: AzureContainerName is required for production");
            return false;
        }

        // Validate: Max file size must be positive
        if (options.MaxFileSizeBytes <= 0)
        {
            Log.Error(
                messageTemplate: "Storage validation failed: MaxFileSizeBytes must be positive (got {MaxSize})",
                propertyValue: options.MaxFileSizeBytes);
            return false;
        }

        // Validate: Allowed extensions must exist
        if (options.AllowedExtensions.Length == 0)
        {
            Log.Error(messageTemplate: "Storage validation failed: AllowedExtensions must contain at least one extension");
            return false;
        }

        Log.Debug(
            messageTemplate: "Azure storage validation passed: Container={Container}, MaxSize={MaxSize}MB, Extensions={Extensions}",
            propertyValue0: options.AzureContainerName,
            propertyValue1: options.MaxFileSizeBytes / 1024 / 1024,
            propertyValue2: string.Join(separator: ", ",
                value: options.AllowedExtensions));

        return true;
    }

    #endregion
}