namespace ReSys.Core.Feature.Common.Storage.Models;

public static class StorageErrors
{
    public static Error FileNotFound(string path) =>
        Error.NotFound("Storage.FileNotFound", $"File not found: {path}");

    public static Error FileAlreadyExists(string path) =>
        Error.Conflict("Storage.FileAlreadyExists", $"File already exists: {path}");

    public static Error UploadFailed(string reason) =>
        Error.Failure("Storage.UploadFailed", $"File upload failed: {reason}");

    public static Error DeleteFailed(string reason) =>
        Error.Failure("Storage.DeleteFailed", $"File deletion failed: {reason}");

    public static Error ReadFailed(string reason) =>
        Error.Failure("Storage.ReadFailed", $"File read failed: {reason}");

    public static Error CopyFailed(string reason) =>
        Error.Failure("Storage.CopyFailed", $"File copy failed: {reason}");

    public static Error MoveFailed(string reason) =>
        Error.Failure("Storage.MoveFailed", $"File move failed: {reason}");

    public static Error FileEmpty =>
        Error.Validation("File.Empty", "No file was provided");

    public static Error FileEmptyContent =>
        Error.Validation("File.EmptyContent", "The file has no content");

    public static Error FileTooLarge(long maxSizeBytes) =>
        Error.Validation("File.TooLarge",
            $"File exceeds maximum size of {maxSizeBytes / 1024 / 1024}MB");

    public static Error FileInvalidType(string extension, IEnumerable<string> allowed) =>
        Error.Validation("File.InvalidType",
            $"File type '{extension}' not allowed. Allowed: {string.Join(", ", allowed)}");

    public static Error InvalidUrl =>
        Error.Validation("File.InvalidUrl", "File URL is invalid or empty");

    public static Error InvalidFileName =>
        Error.Validation("File.InvalidFileName", "File name contains invalid characters");

    public static Error ImageProcessingFailed(string reason) =>
        Error.Failure("Image.ProcessingFailed", $"Image processing failed: {reason}");

    public static Error OperationFailed(string operation, string reason) =>
        Error.Failure($"Storage.{operation}Failed", reason);
}