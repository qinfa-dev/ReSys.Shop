using ErrorOr;
using Google.Cloud.Storage.V1;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;
using ReSys.Core.Feature.Common.Storage.Models;
using ReSys.Core.Feature.Common.Storage.Services;
using ReSys.Infrastructure.Storages.Helpers;
using ReSys.Infrastructure.Storages.Options;
using Serilog;

namespace ReSys.Infrastructure.Storages.Providers;

public sealed class GoogleCloudStorageService : IStorageService
{
    private readonly StorageClient _client;
    private readonly StorageOptions _options;

    public GoogleCloudStorageService(IOptions<StorageOptions> options)
    {
        _options = options.Value;

        if (!string.IsNullOrEmpty(_options.GoogleCredentialsPath))
        {
            Environment.SetEnvironmentVariable(
                "GOOGLE_APPLICATION_CREDENTIALS",
                _options.GoogleCredentialsPath);
        }

        _client = StorageClient.Create();
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
                    await ImageProcessingHelper.ProcessImageAsync(input, options, cancellationToken);

                uploadStream = processed.ProcessedStream;
                width = processed.Width;
                height = processed.Height;
                contentType = processed.ContentType;
            }

            await _client.UploadObjectAsync(
                _options.GoogleBucketName!,
                path,
                contentType,
                uploadStream,
                cancellationToken: cancellationToken);

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
            Log.Error(ex, "Google Cloud upload failed");
            return StorageErrors.UploadFailed(ex.Message);
        }
    }

    // ======================================================
    // Batch upload
    // ======================================================
    public async Task<ErrorOr<IReadOnlyList<StorageFileInfo>>> UploadBatchAsync(
        IEnumerable<IFormFile> files,
        UploadOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var results = new List<StorageFileInfo>();

        foreach (var file in files)
        {
            var r = await UploadFileAsync(file, options, cancellationToken);
            if (r.IsError)
                return r.Errors;

            results.Add(r.Value);
        }

        return results.AsReadOnly();
    }

    // ======================================================
    // Delete
    // ======================================================
    public async Task<ErrorOr<Success>> DeleteFileAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        var path = ExtractPath(fileUrl);

        try
        {
            await _client.DeleteObjectAsync(
                _options.GoogleBucketName!,
                path,
                cancellationToken: cancellationToken);

            return Result.Success;
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 404)
        {
            return StorageErrors.FileNotFound(path);
        }
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
    // Read
    // ======================================================
    public async Task<ErrorOr<Stream>> GetFileStreamAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        var path = ExtractPath(fileUrl);
        var ms = new MemoryStream();

        try
        {
            await _client.DownloadObjectAsync(
                _options.GoogleBucketName!,
                path,
                ms,
                cancellationToken: cancellationToken);

            ms.Position = 0;
            return ms;
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 404)
        {
            return StorageErrors.FileNotFound(path);
        }
    }

    // ======================================================
    // Exists
    // ======================================================
    public async Task<ErrorOr<bool>> ExistsAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        var path = ExtractPath(fileUrl);

        try
        {
            await _client.GetObjectAsync(
                _options.GoogleBucketName!,
                path,
                cancellationToken: cancellationToken);

            return true;
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 404)
        {
            return false;
        }
    }

    // ======================================================
    // List
    // ======================================================
    public Task<ErrorOr<IReadOnlyList<StorageFileMetadata>>> ListFilesAsync(
        string? prefix = null,
        bool recursive = false,
        CancellationToken cancellationToken = default)
    {
        var results = new List<StorageFileMetadata>();

        var objects = _client.ListObjects(
            _options.GoogleBucketName!,
            prefix);

        foreach (var obj in objects)
        {
            if (!recursive && obj.Name.Contains('/'))
                continue;

            results.Add(new StorageFileMetadata
            {
                Path = obj.Name,
                Url = GetFileUrl(obj.Name),
                Length = (long)(obj.Size ?? 0UL),
                LastModified = obj.UpdatedDateTimeOffset ?? DateTimeOffset.UtcNow,
                ContentType = obj.ContentType
            });
        }

        return Task.FromResult<ErrorOr<IReadOnlyList<StorageFileMetadata>>>(
            results.AsReadOnly());
    }


    // ======================================================
    // Copy / Move
    // ======================================================
    public async Task<ErrorOr<StorageFileInfo>> CopyFileAsync(
        string sourceUrl,
        string destinationPath,
        bool overwrite = false,
        CancellationToken cancellationToken = default)
    {
        var sourcePath = ExtractPath(sourceUrl);

        try
        {
            var obj = await _client.CopyObjectAsync(
                _options.GoogleBucketName!,
                sourcePath,
                _options.GoogleBucketName!,
                destinationPath,
                cancellationToken: cancellationToken);

            return new StorageFileInfo
            {
                Path = destinationPath,
                Url = GetFileUrl(destinationPath),
                ContentType = obj.ContentType ?? "application/octet-stream",
                Length = (long)(obj.Size ?? 0UL),
                LastModified = obj.UpdatedDateTimeOffset ?? DateTimeOffset.UtcNow
            };
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 404)
        {
            return StorageErrors.FileNotFound(sourcePath);
        }
    }

    public async Task<ErrorOr<StorageFileInfo>> MoveFileAsync(
        string sourceUrl,
        string destinationPath,
        bool overwrite = false,
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
    // Metadata
    // ======================================================
    public async Task<ErrorOr<StorageFileInfo>> GetMetadataAsync(
        string fileUrl,
        CancellationToken cancellationToken = default)
    {
        var path = ExtractPath(fileUrl);

        try
        {
            var obj = await _client.GetObjectAsync(
                _options.GoogleBucketName!,
                path,
                cancellationToken: cancellationToken);

            return new StorageFileInfo
            {
                Path = obj.Name,
                Url = GetFileUrl(obj.Name),
                ContentType = obj.ContentType ?? "application/octet-stream",
                Length = (long)(obj.Size ?? 0UL),
                LastModified = obj.UpdatedDateTimeOffset ?? DateTimeOffset.UtcNow
            };
        }
        catch (Google.GoogleApiException ex) when (ex.Error?.Code == 404)
        {
            return StorageErrors.FileNotFound(path);
        }
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

            await _client.UploadObjectAsync(
                _options.GoogleBucketName!,
                thumbPath,
                "image/webp",
                thumb,
                cancellationToken: ct);

            dict[w] = GetFileUrl(thumbPath);
        }

        return dict;
    }

    private string GetFileUrl(string path)
    {
        if (!string.IsNullOrEmpty(_options.GoogleBaseUrl))
            return $"{_options.GoogleBaseUrl.TrimEnd('/')}/{path}";

        return $"https://storage.googleapis.com/{_options.GoogleBucketName}/{path}";
    }

    private string ExtractPath(string fileUrl)
    {
        if (!string.IsNullOrEmpty(_options.GoogleBaseUrl))
            return fileUrl.Replace(_options.GoogleBaseUrl, "").Trim('/');

        return fileUrl
            .Replace($"https://storage.googleapis.com/{_options.GoogleBucketName}/", "")
            .Trim('/');
    }
}
