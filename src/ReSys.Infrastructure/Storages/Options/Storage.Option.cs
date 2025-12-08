using Microsoft.Extensions.Options;

namespace ReSys.Infrastructure.Storages.Options;

public sealed class StorageOptions : IValidateOptions<StorageOptions>
{
    public const string Section = "Storage";

    // Local storage settings (Development)
    public string LocalPath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";

    // Azure storage settings (Production)
    public string? AzureConnectionString { get; set; }
    public string? AzureContainerName { get; set; }
    public string? AzureCdnUrl { get; set; }
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB
    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".gif", ".pdf", ".doc", ".docx", ".xls", ".xlsx"
    ];

    // Performance settings
    public int UploadTimeoutSeconds { get; set; } = 300; // 5 minutes
    public bool EnableCompression { get; set; } = true;
    public int MaxConcurrentUploads { get; set; } = 10;

    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        var failures = new List<string>();

        // Required checks
        if (string.IsNullOrWhiteSpace(options.LocalPath))
            failures.Add("LocalPath must be provided.");

        if (string.IsNullOrWhiteSpace(options.BaseUrl))
            failures.Add("BaseUrl must be provided.");

        // File size and extensions
        if (options.MaxFileSizeBytes <= 0)
            failures.Add("MaxFileSizeBytes must be greater than 0.");

        if (options.AllowedExtensions is not { Length: > 0 })
            failures.Add("At least one AllowedExtension must be provided.");

        foreach (var ext in options.AllowedExtensions)
        {
            if (!AllowedExtensions.Contains(ext.ToLowerInvariant()))
                failures.Add($"Unsupported file extension: {ext}");
        }

        // Azure-specific rules
        bool azureConfigured = !string.IsNullOrEmpty(options.AzureConnectionString) ||
                               !string.IsNullOrEmpty(options.AzureContainerName);

        if (azureConfigured)
        {
            if (string.IsNullOrEmpty(options.AzureConnectionString))
                failures.Add("AzureConnectionString is required when using Azure storage.");
            if (string.IsNullOrEmpty(options.AzureContainerName))
                failures.Add("AzureContainerName is required when using Azure storage.");
        }

        // Performance settings
        if (options.UploadTimeoutSeconds <= 0)
            failures.Add("UploadTimeoutSeconds must be greater than 0.");

        if (options.MaxConcurrentUploads <= 0)
            failures.Add("MaxConcurrentUploads must be greater than 0.");

        return failures.Count > 0
            ? ValidateOptionsResult.Fail(failures)
            : ValidateOptionsResult.Success;
    }
}