using Microsoft.AspNetCore.Http;

using ReSys.Core.Feature.Common.Storage.Models;

namespace ReSys.Core.Feature.Common.Storage.Services;

public interface IStorageService
{
    /// <summary>
    /// Upload a single file with optional processing and configuration.
    /// </summary>
    Task<ErrorOr<StorageFileInfo>> UploadFileAsync(
        IFormFile file,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Upload multiple files with shared options.
    /// </summary>
    Task<ErrorOr<IReadOnlyList<StorageFileInfo>>> UploadBatchAsync(
        IEnumerable<IFormFile> files,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file by its URL or path.
    /// </summary>
    Task<ErrorOr<Success>> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete multiple files.
    /// </summary>
    Task<ErrorOr<Success>> DeleteBatchAsync(
        IEnumerable<string> fileUrls,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a readable stream for the file.
    /// </summary>
    Task<ErrorOr<Stream>> GetFileStreamAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists.
    /// </summary>
    Task<ErrorOr<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List files in a folder (prefix).
    /// </summary>
    Task<ErrorOr<IReadOnlyList<StorageFileMetadata>>> ListFilesAsync(
        string? prefix = null,
        bool recursive = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Copy a file to a new path.
    /// </summary>
    Task<ErrorOr<StorageFileInfo>> CopyFileAsync(
        string sourceUrl,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Move (rename) a file.
    /// </summary>
    Task<ErrorOr<StorageFileInfo>> MoveFileAsync(
        string sourceUrl,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get metadata for a file without downloading content.
    /// </summary>
    Task<ErrorOr<StorageFileInfo>> GetMetadataAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);
}