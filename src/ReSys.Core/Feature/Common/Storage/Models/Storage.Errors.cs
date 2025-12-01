using ErrorOr;

namespace ReSys.Core.Feature.Common.Storage.Models;

public static class StorageErrors
{
    // File not found
    public static Error FileNotFound(string path) =>
        Error.NotFound(code: "Storage.FileNotFound",
            description: $"File not found: {path}");

    // Upload errors
    public static Error UploadFailed(string reason) =>
        Error.Failure(code: "Storage.UploadFailed",
            description: $"File upload failed: {reason}");

    public static Error FileEmpty =>
        Error.Validation(code: "File.Empty",
            description: "No file was provided");

    public static Error FileEmptyContent =>
        Error.Validation(code: "File.Empty",
            description: "The file is empty");

    public static Error FileTooLarge(long maxSizeBytes) =>
        Error.Validation(code: "File.TooLarge",
            description: $"File size exceeds the maximum allowed size of {maxSizeBytes / 1024 / 1024}MB");

    public static Error FileInvalidType(string extension, string[] allowedExtensions) =>
        Error.Validation(code: "File.InvalidType",
            description: $"File type {extension} is not allowed. Allowed types: {string.Join(separator: ", ", value: allowedExtensions)}");

    // Delete errors
    public static Error DeleteFailed(string path) =>
        Error.Failure(code: "Storage.DeleteFailed",
            description: $"Failed to delete file: {path}");

    // Read errors
    public static Error ReadFailed(string path) =>
        Error.Failure(code: "Storage.ReadFailed",
            description: $"Failed to read file: {path}");

    // General validation errors
    public static Error InvalidUrl =>
        Error.Validation(code: "File.InvalidUrl",
            description: "File URL cannot be empty");

    // Operation errors
    public static Error ExistsFailed =>
        Error.Failure(code: "File.ExistsFailed",
            description: "An error occurred while checking file existence");

    public static Error ListFailed =>
        Error.Failure(code: "File.ListFailed",
            description: "An error occurred while listing files");
}