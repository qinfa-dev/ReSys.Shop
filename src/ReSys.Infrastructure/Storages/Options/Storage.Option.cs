using Microsoft.Extensions.Options;

namespace ReSys.Infrastructure.Storages.Options;

public enum StorageProvider
{
    Local,
    Azure,
    GoogleCloud
}

public sealed class StorageOptions : IValidateOptions<StorageOptions>
{
    public const string Section = "Storage";

    // =========================
    // Provider selection
    // =========================
    public StorageProvider Provider { get; set; } = StorageProvider.Local;

    // =========================
    // Local storage
    // =========================
    public string LocalPath { get; set; } = "wwwroot/uploads";
    public string BaseUrl { get; set; } = "/uploads";

    // =========================
    // Azure Blob Storage
    // =========================
    public string? AzureConnectionString { get; set; }
    public string? AzureContainerName { get; set; }
    public string? AzureCdnUrl { get; set; }

    // =========================
    // Google Cloud Storage
    // =========================
    public string? GoogleProjectId { get; set; }
    public string? GoogleBucketName { get; set; }

    /// <summary>
    /// Optional path to service-account JSON file.
    /// If null, GOOGLE_APPLICATION_CREDENTIALS will be used.
    /// </summary>
    public string? GoogleCredentialsPath { get; set; }

    /// <summary>
    /// Optional CDN or public base URL.
    /// Example: https://storage.googleapis.com/my-bucket
    /// </summary>
    public string? GoogleBaseUrl { get; set; }

    // =========================
    // Upload constraints
    // =========================
    public long MaxFileSizeBytes { get; set; } = 10 * 1024 * 1024; // 10MB

    public string[] AllowedExtensions { get; set; } =
    [
        ".jpg", ".jpeg", ".png", ".gif",
        ".pdf", ".doc", ".docx", ".xls", ".xlsx"
    ];

    // =========================
    // Performance
    // =========================
    public int UploadTimeoutSeconds { get; set; } = 300;
    public int MaxConcurrentUploads { get; set; } = 10;

    // =========================
    // Validation
    // =========================
    public ValidateOptionsResult Validate(string? name, StorageOptions options)
    {
        var failures = new List<string>();

        if (options.MaxFileSizeBytes <= 0)
            failures.Add("MaxFileSizeBytes must be greater than 0.");

        if (options.AllowedExtensions is not { Length: > 0 })
            failures.Add("At least one AllowedExtension must be provided.");

        foreach (var ext in options.AllowedExtensions)
        {
            if (string.IsNullOrWhiteSpace(ext) || !ext.StartsWith("."))
                failures.Add($"Invalid extension '{ext}'.");
        }

        switch (options.Provider)
        {
            case StorageProvider.Local:
                if (string.IsNullOrWhiteSpace(options.LocalPath))
                    failures.Add("LocalPath is required for Local provider.");
                if (string.IsNullOrWhiteSpace(options.BaseUrl))
                    failures.Add("BaseUrl is required for Local provider.");
                break;

            case StorageProvider.Azure:
                if (string.IsNullOrWhiteSpace(options.AzureConnectionString))
                    failures.Add("AzureConnectionString is required.");
                if (string.IsNullOrWhiteSpace(options.AzureContainerName))
                    failures.Add("AzureContainerName is required.");
                break;

            case StorageProvider.GoogleCloud:
                if (string.IsNullOrWhiteSpace(options.GoogleBucketName))
                    failures.Add("GoogleBucketName is required.");
                if (string.IsNullOrWhiteSpace(options.GoogleProjectId))
                    failures.Add("GoogleProjectId is required.");
                break;
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
