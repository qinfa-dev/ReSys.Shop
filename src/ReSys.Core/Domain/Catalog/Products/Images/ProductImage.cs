using ErrorOr;

using Pgvector;
using Pgvector.EntityFrameworkCore;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Catalog.Products.Images;

public sealed class ProductImage : BaseImageAsset
{
    #region Constraints
    public static class Constraints
    {
        public const int UrlMaxLength = CommonInput.Constraints.UrlAndUri.UrlMaxLength;
        public const int AltMaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        public const int EmbeddingDimension = 1024;

        public const string PathPrefix = "catalog";
        public const string ImageFolder = "images";
        public static readonly string[] ValidTypes =
        [
            nameof(ProductImageType.Default),
            nameof(ProductImageType.Square),
            nameof(ProductImageType.Thumbnail),
            nameof(ProductImageType.Gallery)
        ];

        public static readonly string[] ValidContentTypes =
        [
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        ];

        public static readonly string[] ValidDimensionUnits =
        [
            "mm",
            "cm",
            "in",
            "ft"
        ];

        public static string GetStoragePath(Guid? productId, Guid? variantId, string type, string contentType)
        {
            var timestamp = DateTimeOffset.UtcNow.ToString(format: "yyyyMMddHHmmss");
            var fileType = contentType.Split(separator: '/')[1];
            var entityType = variantId.HasValue ? "variants" : "products";
            var id = variantId ?? productId;

            return $"{PathPrefix}/{ImageFolder}/{entityType}/{id}/{type.ToLower()}/{timestamp}_{type.ToLower()}.{fileType}";
        }
    }

    public enum ProductImageType
    {
        None = 0,
        Default = 1,
        Square = 2,
        Thumbnail = 3,
        Gallery = 4
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error Required =>
            Error.Validation(code: "ProductImage.Required", description: "At least one product image is required.");

        public static Error AlreadyExists(Guid productId, Guid? variantId, string type) =>
            Error.Conflict(code: "ProductImage.AlreadyExists", description: $"Product image of type '{type}' for product '{productId}'{(variantId.HasValue ? $" and variant '{variantId}'" : string.Empty)} already exists.");

        public static Error NotFound(Guid id) =>
            Error.NotFound(code: "ProductImage.NotFound", description: $"Product image with ID '{id}' was not found.");

        public static Error InvalidType =>
            Error.Validation(code: "ProductImage.InvalidType", description: $"Type must be one of: {string.Join(separator: ", ", value: Constraints.ValidTypes)}.");

        public static Error InvalidContentType =>
            Error.Validation(code: "ProductImage.InvalidContentType", description: $"Content type must be one of: {string.Join(separator: ", ", value: Constraints.ValidContentTypes)}.");

        public static Error InvalidDimensionUnit =>
            Error.Validation(code: "ProductImage.InvalidDimensionUnit", description: $"Dimension unit must be one of: {string.Join(separator: ", ", value: Constraints.ValidDimensionUnits)}.");

        public static Error InvalidUrl =>
            Error.Validation(code: "ProductImage.InvalidUrl", description: $"URL must not exceed {Constraints.UrlMaxLength} characters and cannot be empty.");

        public static Error InvalidEmbedding =>
            Error.Validation(code: "ProductImage.InvalidEmbedding", description: $"Embedding must be {Constraints.EmbeddingDimension}-dimensional.");

        public static Error EmbeddingNotGenerated =>
            Error.Validation(code: "ProductImage.EmbeddingNotGenerated", description: "Image embedding has not been generated yet.");
    }
    #endregion

    #region Core Properties
    public string ContentType { get; set; } = "image/jpeg";
    public int? Width { get; set; }
    public int? Height { get; set; }
    public string? DimensionsUnit { get; set; }

    /// <summary>
    /// 1024-dimensional embedding from OpenCLIP ViT-H/14 (LAION-2B)
    /// </summary>
    public Vector? Embedding { get; set; }

    /// <summary>
    /// Model version that generated the embedding
    /// </summary>
    public string? EmbeddingModel { get; set; } = "openclip-vit-h-14-laion2b";

    /// <summary>
    /// When the embedding was generated
    /// </summary>
    public DateTimeOffset? EmbeddingGeneratedAt { get; set; }
    #endregion

    #region Relationships
    public Guid? ProductId { get; set; }
    public Guid? VariantId { get; set; }
    public Product? Product { get; set; }
    public Variant? Variant { get; set; }
    #endregion

    #region Computed Properties
    public new bool IsDefault => Type == nameof(ProductImageType.Default);
    public bool IsSquare => Type == nameof(ProductImageType.Square);
    public bool IsThumbnail => Type == nameof(ProductImageType.Thumbnail);
    public string AspectRatio => Width.HasValue && Height.HasValue && Height > 0 ? $"{Width}:{Height}" : "unknown";
    public bool HasEmbedding => Embedding != null;
    #endregion

    #region Constructors
    private ProductImage() { }
    #endregion

    #region Factory
    public static ErrorOr<ProductImage> Create(
        string url,
        Guid? productId = null,
        Guid? variantId = null,
        string? alt = null,
        int position = 0,
        string type = nameof(ProductImageType.Default),
        string contentType = "image/jpeg",
        int? width = null,
        int? height = null,
        string? dimensionsUnit = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        if (string.IsNullOrWhiteSpace(value: url) || url.Length > Constraints.UrlMaxLength)
            return Errors.InvalidUrl;

        if (!Constraints.ValidContentTypes.Contains(value: contentType))
            return Errors.InvalidContentType;

        if (dimensionsUnit is not null && !Constraints.ValidDimensionUnits.Contains(value: dimensionsUnit))
            return Errors.InvalidDimensionUnit;

        if (!Constraints.ValidTypes.Contains(value: type))
            return Errors.InvalidType;

        var image = new ProductImage
        {
            Id = Guid.NewGuid(),
            Url = url.Trim(),
            ProductId = productId,
            VariantId = variantId,
            Alt = alt?.Trim(),
            Position = Math.Max(val1: 0, val2: position),
            Type = type,
            ContentType = contentType,
            Width = width,
            Height = height,
            DimensionsUnit = dimensionsUnit,
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata ?? new Dictionary<string, object?>()),
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata ?? new Dictionary<string, object?>()),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return image;
    }
    #endregion

    #region Business Logic
    public ErrorOr<Deleted> Delete() => Result.Deleted;

    public ErrorOr<ProductImage> Update(
        string? url = null,
        string? alt = null,
        int? position = null,
        string? type = null,
        string? contentType = null,
        int? width = null,
        int? height = null,
        string? dimensionsUnit = null,
        IDictionary<string, object?>? publicMetadata = null,
        IDictionary<string, object?>? privateMetadata = null)
    {
        if (contentType is not null && !Constraints.ValidContentTypes.Contains(value: contentType))
            return Errors.InvalidContentType;

        if (dimensionsUnit is not null && !Constraints.ValidDimensionUnits.Contains(value: dimensionsUnit))
            return Errors.InvalidDimensionUnit;

        bool changed = false;

        if (url is { Length: > 0 } && url != Url)
        {
            Url = url.Trim();
            // Clear embedding when URL changes
            Embedding = null;
            EmbeddingGeneratedAt = null;
            changed = true;
        }

        if (alt != null && alt != Alt)
        {
            Alt = alt.Trim();
            changed = true;
        }

        if (position.HasValue && position != Position)
        {
            Position = Math.Max(val1: 0, val2: position.Value);
            changed = true;
        }

        if (type != null && type != Type)
        {
            Type = type;
            changed = true;
        }

        if (contentType != null && contentType != ContentType)
        {
            ContentType = contentType;
            changed = true;
        }

        if (width.HasValue && width != Width)
        {
            Width = width > 0 ? width : null;
            changed = true;
        }

        if (height.HasValue && height != Height)
        {
            Height = height > 0 ? height : null;
            changed = true;
        }

        if (dimensionsUnit != null && dimensionsUnit != DimensionsUnit)
        {
            DimensionsUnit = dimensionsUnit;
            changed = true;
        }

        if (publicMetadata != null && !PublicMetadata.MetadataEquals(dict2: publicMetadata))
        {
            PublicMetadata = new Dictionary<string, object?>(dictionary: publicMetadata);
            changed = true;
        }

        if (privateMetadata != null && !PrivateMetadata.MetadataEquals(dict2: privateMetadata))
        {
            PrivateMetadata = new Dictionary<string, object?>(dictionary: privateMetadata);
            changed = true;
        }

        if (changed)
            UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    public ErrorOr<ProductImage> SetEmbedding(float[] embedding, string? modelVersion = null)
    {
        if (embedding.Length != Constraints.EmbeddingDimension)
            return Errors.InvalidEmbedding;

        Embedding = new Vector(v: embedding);
        EmbeddingModel = modelVersion ?? "openclip-vit-h-14-laion2b";
        EmbeddingGeneratedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        return this;
    }

    public double CalculateSimilarity(ProductImage other)
    {
        if (Embedding == null || other.Embedding == null)
            throw new InvalidOperationException(message: "Both images must have embeddings to calculate similarity");

        return 1 - Embedding.CosineDistance(b: other.Embedding);
    }
    #endregion
}