using ErrorOr;

using Microsoft.AspNetCore.Http;

using ReSys.Core.Feature.Common.Storage.Models;

namespace ReSys.Core.Feature.Common.Storage.Services;

public interface IStorageService
{
    /// <summary>
    /// Upload a file to storage.
    /// Returns the file information including URL and metadata.
    /// </summary>
    Task<ErrorOr<StorageFileInfo>> UploadFileAsync(
        IFormFile file,
        string? path = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Delete a file from storage.
    /// </summary>
    Task<ErrorOr<Success>> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Get a file stream from storage.
    /// </summary>
    Task<ErrorOr<Stream>> GetFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Check if a file exists in storage.
    /// </summary>
    Task<ErrorOr<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// List all files in a given folder (recursively if required).
    /// </summary>
    Task<ErrorOr<IReadOnlyList<StorageFileModel>>> ListFilesAsync(
        string? folder = null,
        bool recursive = false,
        CancellationToken cancellationToken = default);
}