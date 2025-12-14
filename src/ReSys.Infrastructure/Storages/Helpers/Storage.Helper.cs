using ReSys.Core.Feature.Common.Storage.Models;

using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats;
using SixLabors.ImageSharp.Processing;
using SixLabors.ImageSharp.Formats.Webp;
using SixLabors.ImageSharp.Formats.Jpeg;

namespace ReSys.Infrastructure.Storages.Helpers;

internal static class ImageProcessingHelper
{
    public static string BuildPath(this UploadOptions options, string originalName, string ext)
    {
        // Determine the filename (without extension)
        string fileNameWithoutExt;

        if (options.FileName != null)
        {
            // Use the provided filename
            fileNameWithoutExt = options.FileName;
        }
        else if (options.PreserveOriginalFileName)
        {
            // Use original filename
            fileNameWithoutExt = Path.GetFileNameWithoutExtension(originalName);
        }
        else
        {
            // Generate a unique filename
            fileNameWithoutExt = $"{DateTimeOffset.UtcNow.Ticks}_{Guid.NewGuid():N}";
        }

        // Combine folder + filename + extension
        var folder = options.Folder?.TrimEnd('/', '\\') ?? string.Empty;
        var fullPath = Path.Combine(folder, $"{fileNameWithoutExt}{ext}");

        return fullPath.Replace('\\', '/');
    }
    public static async Task<(Stream ProcessedStream, int Width, int Height, string ContentType)> ProcessImageAsync(
        Stream originalStream,
        UploadOptions options,
        CancellationToken ct)
    {
        originalStream.Position = 0;
        using var image = await Image.LoadAsync(originalStream, ct);

        int width = image.Width;
        int height = image.Height;

        // Process if max dimensions specified
        if (options.MaxDimensions.HasValue)
        {
            var (targetW, targetH) = options.MaxDimensions.Value;

            bool needsResize = width > targetW || height > targetH;
            bool shouldUpscale = options.AllowUpscale && (width < targetW || height < targetH);

            if (needsResize || shouldUpscale)
            {
                ResizeMode mode = options.CropToFit ? ResizeMode.Crop : ResizeMode.Max;

                image.Mutate(x => x.Resize(new ResizeOptions
                {
                    Size = new Size(targetW, targetH),
                    Mode = mode,
                    Position = AnchorPositionMode.Center
                }));

                width = image.Width;
                height = image.Height;
            }
        }

        // Strip EXIF metadata if optimizing
        if (options.OptimizeImage)
        {
            image.Mutate(x => x.AutoOrient());
        }

        // Choose encoder
        var encoder = options.ConvertToWebP
            ? (IImageEncoder)new WebpEncoder { Quality = options.Quality }
            : new JpegEncoder { Quality = options.Quality };

        var memoryStream = new MemoryStream();
        await image.SaveAsync(memoryStream, encoder, ct);
        memoryStream.Position = 0;

        string contentType = options.ConvertToWebP ? "image/webp" : "image/jpeg";

        return (memoryStream, width, height, contentType);
    }

    public static async Task<MemoryStream> GenerateThumbnailAsync(
        Stream originalStream,
        int targetWidth,
        int quality,
        CancellationToken ct)
    {
        originalStream.Position = 0;
        using var image = await Image.LoadAsync(originalStream, ct);

        // Thumbnails should be square (crop to fit)
        image.Mutate(ctx => ctx.Resize(new ResizeOptions
        {
            Size = new Size(targetWidth, targetWidth),
            Mode = ResizeMode.Crop,
            Position = AnchorPositionMode.Center
        }));

        var stream = new MemoryStream();
        await image.SaveAsWebpAsync(stream, new WebpEncoder { Quality = quality }, ct);
        stream.Position = 0;
        return stream;
    }
}