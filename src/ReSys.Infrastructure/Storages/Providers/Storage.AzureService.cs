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

public sealed class AzureStorageService : IStorageService
{
    private readonly IBlobStorage _storage;
    private readonly StorageOptions _options;

    public AzureStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;

        try
        {
            (string accountName, string accountKey) = ParseConnectionString(connectionString: _options.AzureConnectionString!);
            _storage = StorageFactory.Blobs.AzureBlobStorageWithSharedKey(
                accountName: accountName,
                key: accountKey);
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to initialize Azure storage");
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
            await using Stream stream = file.OpenReadStream();

            // Get image dimensions if it's an image
            int? width = null;
            int? height = null;
            if (file.ContentType.StartsWith(value: "image/", comparisonType: StringComparison.OrdinalIgnoreCase))
            {
                using var image = await Image.LoadAsync(stream: stream, cancellationToken: cancellationToken);
                width = image.Width;
                height = image.Height;
                stream.Position = 0; // Reset stream position for upload
            }

            string dateFolder = DateTimeOffset.UtcNow.ToString(format: "yyyy/MM/dd");
            string safeFileName = $"{DateTimeOffset.UtcNow.Ticks}_{Guid.NewGuid():N}{extension}";
            string blobPath = path != null
                ? $"{_options.AzureContainerName}/{path}/{dateFolder}/{safeFileName}"
                : $"{_options.AzureContainerName}/{dateFolder}/{safeFileName}";

            await _storage.WriteAsync(fullPath: blobPath,
                dataStream: stream,
                cancellationToken: cancellationToken);

            var fileUrl = GetFileUrl(blobPath: blobPath);
            var now = DateTimeOffset.UtcNow;

            return new StorageFileInfo
            {
                Url = fileUrl,
                ContentType = file.ContentType,
                Width = width,
                Height = height,
                Size = file.Length,
                LastModified = now
            };
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            Log.Error(exception: ex,
                messageTemplate: "Failed to upload file {FileName} to Azure",
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
                messageTemplate: "Failed to delete file at {FileUrl} from Azure",
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
                messageTemplate: "Failed to read file at {FileUrl} from Azure",
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
                messageTemplate: "Failed to check if file exists at {FileUrl} in Azure",
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
            string folderPath = folder != null
                ? $"{_options.AzureContainerName!}/{folder}"
                : _options.AzureContainerName!;

            IReadOnlyCollection<Blob>? blobs = await _storage.ListAsync(
                folderPath: folderPath,
                recurse: recursive,
                cancellationToken: cancellationToken);

            List<StorageFileModel> fileInfos = blobs
                .Where(predicate: b => !b.IsFolder)
                .Select(selector: blob => new StorageFileModel
                {
                    Path = blob.FullPath,
                    Size = blob.Size,
                    LastModifiedUtc = blob.LastModificationTime,
                    Url = GetFileUrl(blobPath: blob.FullPath)
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
                messageTemplate: "Failed to list files in Azure folder {Folder}",
                propertyValue: folder ?? "root");
            return StorageErrors.ListFailed;
        }
    }

    private string GetBlobPath(string fileUrl)
    {
        string? baseUrl = !string.IsNullOrEmpty(value: _options.AzureCdnUrl)
            ? _options.AzureCdnUrl
            : $"https://{GetStorageAccount()}.blob.core.windows.net";

        return fileUrl
            .Replace(oldValue: baseUrl,
                newValue: "",
                comparisonType: StringComparison.OrdinalIgnoreCase)
            .TrimStart(trimChar: '/');
    }

    private string GetFileUrl(string blobPath)
    {
        return !string.IsNullOrEmpty(value: _options.AzureCdnUrl)
            ? $"{_options.AzureCdnUrl.TrimEnd(trimChar: '/')}/{blobPath.TrimStart(trimChar: '/')}"
            : $"https://{GetStorageAccount()}.blob.core.windows.net/{blobPath}";
    }

    private (string accountName, string accountKey) ParseConnectionString(string connectionString)
    {
        string[] parts = connectionString.Split(separator: ';',
            options: StringSplitOptions.RemoveEmptyEntries);

        string? accountName = null;
        string? accountKey = null;

        foreach (string part in parts)
        {
            if (part.StartsWith(value: "AccountName="))
                accountName = part[12..];
            else if (part.StartsWith(value: "AccountKey="))
                accountKey = part[11..];
        }

        if (string.IsNullOrEmpty(value: accountName) || string.IsNullOrEmpty(value: accountKey))
            throw new InvalidOperationException(message: "Invalid Azure storage connection string format");

        return (accountName, accountKey);
    }

    private string GetStorageAccount()
    {
        (string accountName, _) = ParseConnectionString(connectionString: _options.AzureConnectionString!);
        return accountName;
    }
}