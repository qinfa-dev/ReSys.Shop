using ErrorOr;
using FluentStorage;
using FluentStorage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ReSys.Core.Feature.Common.Storage.Models;
using ReSys.Core.Feature.Common.Storage.Services;
using ReSys.Infrastructure.Storages.Helpers;
using ReSys.Infrastructure.Storages.Options;
using Serilog;

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
            Directory.CreateDirectory(_options.LocalPath);
            _storage = StorageFactory.Blobs.DirectoryFiles(_options.LocalPath);
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to initialize local storage");
            throw;
        }
    }

    // ======================================================
    // Upload
    // ======================================================
    public async Task<ErrorOr<StorageFileInfo>> UploadFileAsync(
        IFormFile? file,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= UploadOptions.Default;

        if (file is null)
            return StorageErrors.FileEmpty;

        if (file.Length == 0)
            return StorageErrors.FileEmptyContent;

        if (file.Length > _options.MaxFileSizeBytes)
            return StorageErrors.FileTooLarge(_options.MaxFileSizeBytes);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!_options.AllowedExtensions.Contains(ext))
            return StorageErrors.FileInvalidType(ext, _options.AllowedExtensions);

        try
        {
            var finalExt = options.ConvertToWebP ? ".webp" : ext;
            var path = options.BuildPath(file.FileName, finalExt);

            await using var input = file.OpenReadStream();

            Stream uploadStream = input;
            int? width = null;
            int? height = null;
            string contentType = file.ContentType;

            if (file.ContentType.StartsWith("image/", StringComparison.OrdinalIgnoreCase)
                && options.OptimizeImage)
            {
                var processed =
                    await ImageProcessingHelper.ProcessImageAsync(
                        input, options, cancellationToken);

                uploadStream = processed.ProcessedStream;
                width = processed.Width;
                height = processed.Height;
                contentType = processed.ContentType;
            }

            await _storage.WriteAsync(
                path,
                uploadStream,
                options.Overwrite,
                cancellationToken);

            var thumbnails = await UploadThumbnailsAsync(
                input,
                options,
                path,
                cancellationToken);

            return new StorageFileInfo
            {
                Path = path,
                Url = GetFileUrl(path),
                ContentType = contentType,
                Length = uploadStream.Length,
                Width = width,
                Height = height,
                Thumbnails = thumbnails,
                Metadata = options.Metadata,
                LastModified = DateTimeOffset.UtcNow
            };
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Local upload failed");
            return StorageErrors.UploadFailed(ex.Message);
        }
    }

    // ======================================================
    // Batch
    // ======================================================
    public async Task<ErrorOr<IReadOnlyList<StorageFileInfo>>> UploadBatchAsync(
        IEnumerable<IFormFile> files,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = new List<StorageFileInfo>();

        foreach (var file in files)
        {
            var result = await UploadFileAsync(file, options, cancellationToken);
            if (result.IsError)
                return result.Errors;

            list.Add(result.Value);
        }

        return list.AsReadOnly();
    }

    // ======================================================
    // Read
    // ======================================================
    public async Task<ErrorOr<Stream>> GetFileStreamAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        var path = GetBlobPath(fileUrl);

        if (!await _storage.ExistsAsync(path, cancellationToken))
            return StorageErrors.FileNotFound(path);

        var ms = new MemoryStream();
        await _storage.ReadToStreamAsync(path, ms, cancellationToken);
        ms.Position = 0;
        return ms;
    }

    // ======================================================
    // Delete
    // ======================================================
    public async Task<ErrorOr<Success>> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        var path = GetBlobPath(fileUrl);

        if (!await _storage.ExistsAsync(path, cancellationToken))
            return StorageErrors.FileNotFound(path);

        await _storage.DeleteAsync(path, cancellationToken);
        return Result.Success;
    }

    public async Task<ErrorOr<Success>> DeleteBatchAsync(
        IEnumerable<string> fileUrls,
        CancellationToken cancellationToken = default)
    {
        foreach (var url in fileUrls)
        {
            var r = await DeleteFileAsync(url, cancellationToken);
            if (r.IsError)
                return r.Errors;
        }

        return Result.Success;
    }

    // ======================================================
    // Exists
    // ======================================================
    public async Task<ErrorOr<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        return await _storage.ExistsAsync(
            GetBlobPath(fileUrl),
            cancellationToken);
    }

    // ======================================================
    // List
    // ======================================================
    public async Task<ErrorOr<IReadOnlyList<StorageFileMetadata>>> ListFilesAsync(
        string? prefix = null,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var blobs = await _storage.ListAsync(
            folderPath: prefix,
            recurse: recursive);

        return blobs
            .Where(b => !b.IsFolder)
            .Select(b => new StorageFileMetadata
            {
                Path = b.FullPath,
                Url = GetFileUrl(b.FullPath),
                Length = b.Size ?? 0,
                LastModified = b.LastModificationTime.GetValueOrDefault()
            })
            .ToList()
            .AsReadOnly();
    }

    // ======================================================
    // Copy / Move
    // ======================================================
    public async Task<ErrorOr<StorageFileInfo>> CopyFileAsync(
        string sourceUrl,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        var src = GetBlobPath(sourceUrl);

        if (!await _storage.ExistsAsync(src, cancellationToken))
            return StorageErrors.FileNotFound(src);

        await using var ms = new MemoryStream();
        await _storage.ReadToStreamAsync(src, ms, cancellationToken);
        ms.Position = 0;

        await _storage.WriteAsync(
            destinationPath,
            ms,
            overwrite,
            cancellationToken);

        return new StorageFileInfo
        {
            Path = destinationPath,
            Url = GetFileUrl(destinationPath),
            ContentType = "application/octet-stream",
            Length = ms.Length,
            LastModified = DateTimeOffset.UtcNow
        };
    }

    public async Task<ErrorOr<StorageFileInfo>> MoveFileAsync(
        string sourceUrl,
        string destinationPath,
        bool overwrite,
        CancellationToken cancellationToken = default)
    {
        var copy = await CopyFileAsync(
            sourceUrl,
            destinationPath,
            overwrite,
            cancellationToken);

        if (copy.IsError)
            return copy.Errors;

        await DeleteFileAsync(sourceUrl, cancellationToken);
        return copy.Value;
    }

    // ======================================================
    // Metadata (not supported)
    // ======================================================
    public Task<ErrorOr<StorageFileInfo>> GetMetadataAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        return Task.FromResult<ErrorOr<StorageFileInfo>>(
            StorageErrors.OperationFailed("Metadata", "Not supported"));
    }

    // ======================================================
    // Helpers
    // ======================================================
    private async Task<IReadOnlyDictionary<int, string>?> UploadThumbnailsAsync(
        Stream original,
        UploadOptions options,
        string basePath,
        CancellationToken ct)
    {
        if (!options.GenerateThumbnails || options.ThumbnailWidths == null)
            return null;

        var dict = new Dictionary<int, string>();

        foreach (var w in options.ThumbnailWidths)
        {
            original.Position = 0;

            using var thumb =
                await ImageProcessingHelper.GenerateThumbnailAsync(
                    original, w, options.Quality, ct);

            var thumbPath = basePath.Replace(".", $"_{w}.");

            await _storage.WriteAsync(
                thumbPath,
                thumb,
                options.Overwrite,
                ct);

            dict[w] = GetFileUrl(thumbPath);
        }

        return dict;
    }

    private string GetFileUrl(string path)
        => $"{_options.BaseUrl.TrimEnd('/')}/{path}";

    private string GetBlobPath(string url)
    {
        return url
            .Replace(_options.BaseUrl, "", StringComparison.OrdinalIgnoreCase)
            .Trim('/');
    }
}
