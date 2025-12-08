using ErrorOr;

using FluentStorage;
using FluentStorage.Blobs;

using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

using ReSys.Core.Feature.Common.Storage.Models;
using ReSys.Core.Feature.Common.Storage.Services;
using ReSys.Infrastructure.Storages.Options;

using Serilog;

using SixLabors.ImageSharp;

namespace ReSys.Infrastructure.Storages.Providers;

public sealed class LocalStorageService : IStorageService
{
    private readonly IBlobStorage _storage;
    private readonly StorageOptions _options;

    public LocalStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;

        try
        {
            Directory.CreateDirectory(path: _options.LocalPath);
            _storage = StorageFactory.Blobs.DirectoryFiles(directoryFullName: _options.LocalPath);
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to initialize local storage at path: {Path}",
                propertyValue: _options.LocalPath);
            throw;
        }
    }

    public async Task<ErrorOr<StorageFileInfo>> UploadFileAsync(
        IFormFile? file,
        string? path = null,
        CancellationToken cancellationToken = default)
    {
        if (file is null)
            return StorageErrors.FileEmpty;

        if (file.Length == 0)
            return StorageErrors.FileEmptyContent;

        if (file.Length > _options.MaxFileSizeBytes)
            return StorageErrors.FileTooLarge(maxSizeBytes: _options.MaxFileSizeBytes);

        string extension = Path.GetExtension(path: file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(value: extension,
                comparer: StringComparer.OrdinalIgnoreCase))
            return StorageErrors.FileInvalidType(extension: extension,
                allowedExtensions: _options.AllowedExtensions);

        try
        {
            string dateFolder = DateTimeOffset.UtcNow.ToString(format: "yyyy/MM/dd");
            string safeFileName = $"{DateTimeOffset.UtcNow.Ticks}_{Guid.NewGuid():N}{extension}";
            string blobPath = path != null
                ? Path.Combine(path1: path,
                    path2: dateFolder,
                    path3: safeFileName).Replace(oldValue: "\\",
                    newValue: "/")
                : Path.Combine(path1: dateFolder,
                    path2: safeFileName).Replace(oldValue: "\\",
                    newValue: "/");

            // Get image dimensions if it's an image
            int? width = null;
            int? height = null;
            if (file.ContentType.StartsWith(value: "image/", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                using var image = await Image.LoadAsync(stream: file.OpenReadStream(), cancellationToken: cancellationToken);
                width = image.Width;
                height = image.Height;
            }

            await using Stream stream = file.OpenReadStream();
            await _storage.WriteAsync(fullPath: blobPath,
                dataStream: stream,
                cancellationToken: cancellationToken);

            string url = Path.Combine(path1: _options.BaseUrl,
                path2: blobPath).Replace(oldValue: "\\",
                newValue: "/");

            return new StorageFileInfo
            {
                Url = url,
                ContentType = file.ContentType,
                Width = width,
                Height = height,
                Size = file.Length,
                LastModified = DateTimeOffset.UtcNow
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to upload file {FileName}",
                propertyValue: file.FileName);
            return StorageErrors.UploadFailed(reason: ex.Message);
        }
    }

    public async Task<ErrorOr<Success>> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value: fileUrl))
            return StorageErrors.InvalidUrl;

        try
        {
            string blobPath = GetBlobPath(fileUrl: fileUrl);

            if (!await _storage.ExistsAsync(fullPath: blobPath,
                    cancellationToken: cancellationToken))
                return StorageErrors.FileNotFound(path: blobPath);

            await _storage.DeleteAsync(fullPath: blobPath,
                cancellationToken: cancellationToken);
            return Result.Success;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to delete file at {FileUrl}",
                propertyValue: fileUrl);
            return StorageErrors.DeleteFailed(path: fileUrl);
        }
    }

    public async Task<ErrorOr<Stream>> GetFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value: fileUrl))
            return StorageErrors.InvalidUrl;

        try
        {
            string blobPath = GetBlobPath(fileUrl: fileUrl);

            if (!await _storage.ExistsAsync(fullPath: blobPath,
                    cancellationToken: cancellationToken))
                return StorageErrors.FileNotFound(path: blobPath);

            MemoryStream memoryStream = new();
            await _storage.ReadToStreamAsync(fullPath: blobPath,
                targetStream: memoryStream,
                cancellationToken: cancellationToken);
            memoryStream.Position = 0;

            return memoryStream;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to read file at {FileUrl}",
                propertyValue: fileUrl);
            return StorageErrors.ReadFailed(path: fileUrl);
        }
    }

    public async Task<ErrorOr<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(value: fileUrl))
            return StorageErrors.InvalidUrl;

        try
        {
            string blobPath = GetBlobPath(fileUrl: fileUrl);
            return await _storage.ExistsAsync(fullPath: blobPath,
                cancellationToken: cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to check if file exists at {FileUrl}",
                propertyValue: fileUrl);
            return StorageErrors.ExistsFailed;
        }
    }

    public async Task<ErrorOr<IReadOnlyList<StorageFileModel>>> ListFilesAsync(
        string? folder = null,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        try
        {
            IReadOnlyCollection<Blob>? blobs = await _storage.ListAsync(
                folderPath: folder,
                recurse: recursive,
                cancellationToken: cancellationToken);

            List<StorageFileModel> fileInfos = blobs
                .Where(predicate: b => !b.IsFolder)
                .Select(selector: blob => new StorageFileModel
                {
                    Path = blob.FullPath,
                    Size = blob.Size,
                    LastModifiedUtc = blob.LastModificationTime,
                    Url = Path.Combine(path1: _options.BaseUrl,
                        path2: blob.FullPath).Replace(oldValue: "\\",
                        newValue: "/")
                })
                .ToList();

            return fileInfos.AsReadOnly();
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to list files in folder {Folder}",
                propertyValue: folder ?? "root");
            return StorageErrors.ListFailed;
        }
    }

    private string GetBlobPath(string fileUrl)
    {
        return fileUrl
            .Replace(oldValue: _options.BaseUrl,
                newValue: "",
                comparisonType: StringComparison.OrdinalIgnoreCase)
            .TrimStart(trimChar: '/');
    }
}