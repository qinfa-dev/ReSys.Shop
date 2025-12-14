using ReSys.Core.Domain.Catalog.Products.Images;

namespace ReSys.Core.Feature.Common.Storage.Models;

public sealed class UploadOptions
{
    public static readonly UploadOptions Default = new();

    /// <summary>
    /// Folder/path prefix where file will be stored (e.g., "users/123/avatars").
    /// </summary>
    public string? Folder { get; init; }

    /// <summary>
    /// Custom filename without extension. If null, uses timestamp or original (if PreserveFileName).
    /// </summary>
    public string? FileName { get; init; }

    /// <summary>
    /// Use the original uploaded filename (without path). Default: false.
    /// </summary>
    public bool PreserveOriginalFileName { get; init; } = false;

    /// <summary>
    /// Generate thumbnails for images.
    /// </summary>
    public bool GenerateThumbnails { get; init; }

    /// <summary>
    /// Desired thumbnail widths (height auto-scaled).
    /// </summary>
    public IReadOnlyCollection<int>? ThumbnailWidths { get; init; }

    /// <summary>
    /// Optimize/compress images and strip metadata. Default: true.
    /// </summary>
    public bool OptimizeImage { get; init; } = true;

    /// <summary>
    /// Image quality (1-100). Default: 85.
    /// </summary>
    public int Quality { get; init; } = 85;

    /// <summary>
    /// Convert to WebP format. Default: false.
    /// </summary>
    public bool ConvertToWebP { get; init; }

    /// <summary>
    /// Resize image if larger than this (maintains aspect ratio).
    /// </summary>
    public (int Width, int Height)? MaxDimensions { get; init; }
    
    /// <summary>
    ///  
    /// </summary>
    public bool AllowUpscale { get; init; }

    /// <summary>
    /// 
    /// </summary>
    public bool CropToFit { get; init; }

    /// <summary>
    /// Custom metadata key-value pairs.
    /// </summary>
    public IReadOnlyDictionary<string, string>? Metadata { get; init; }

    /// <summary>
    /// Overwrite existing file at the target path.
    /// </summary>
    public bool Overwrite { get; init; }

    public static UploadOptions FromDomainSpec(
        ProductImage.ProductImageType type,
        Guid? productId,
        Guid? variantId,
        string contentType)
    {
        var spec = ProductImage.GetSizeSpec(type);

        // Use folder getter, not full path
        var storageFolder = ProductImage.Constraints.GetStorageFolder(
            productId: productId,
            variantId: variantId,
            type: type.ToString());

        var storageFileName = ProductImage.Constraints.GetStorageFileName(
            type: type.ToString(),
            contentType: contentType);

        return new UploadOptions
        {
            Folder = storageFolder,  // Just the folder path
            FileName = Path.GetFileNameWithoutExtension(storageFileName), // Without extension
            Overwrite = false,
            OptimizeImage = true,
            ConvertToWebP = true,
            MaxDimensions = spec.TargetWidth.HasValue
                ? (spec.TargetWidth.Value, spec.TargetHeight ?? spec.TargetWidth.Value)
                : null,
            AllowUpscale = spec.AllowUpscale,
            CropToFit = spec.CropToFit,
            GenerateThumbnails = type switch
            {
                ProductImage.ProductImageType.Default => true,
                ProductImage.ProductImageType.Gallery => true,
                _ => false
            },
            ThumbnailWidths = type switch
            {
                ProductImage.ProductImageType.Default => new[] { 300, 600 },
                ProductImage.ProductImageType.Gallery => new[] { 400, 800 },
                _ => null
            }
        };
    }
}