using Pgvector;
using Pgvector.EntityFrameworkCore;

using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Catalog.Products.Images;

/// <summary>
/// Represents an image asset associated with a product or a specific product variant.
/// This entity extends <see cref="BaseImageAsset"/> and incorporates advanced features like visual embeddings
/// for similarity calculations.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Catalog Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Visual Representation</term>
/// <description>Provides the visual content for products and variants in the storefront.</description>
/// </item>
/// <item>
/// <term>Categorization</term>
/// <description>Images can have different types (e.g., Default, Thumbnail, Gallery) for various display purposes.</description>
/// </item>
/// <item>
/// <term>Search & Discovery</term>
/// <description>Supports visual similarity search through <c>Embedding</c> vector data.</description>
/// </item>
/// <item>
/// <term>Auditability</term>
/// <description>Inherits auditing properties like creation and update timestamps.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>Url</term>
/// <description>The direct link to the image file.</description>
/// </item>
/// <item>
/// <term>Type</term>
/// <description>A classification for the image's purpose (e.g., 'Default', 'Square').</description>
/// </item>
/// <item>
/// <term>Embedding</term>
/// <description>A vector representation of the image for similarity analysis.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class ProductImage : BaseImageAsset, IHasIdentity<Guid>
{
    #region Constraints

    /// <summary>
    /// Defines constraints and constant values specific to <see cref="ProductImage"/> operations and properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum allowed length for the image URL.
        /// </summary>
        public const int UrlMaxLength = CommonInput.Constraints.UrlAndUri.UrlMaxLength;

        /// <summary>
        /// Maximum allowed length for the image alt text.
        /// </summary>
        public const int AltMaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;

        /// <summary>
        /// The base path prefix for catalog-related storage.
        /// </summary>
        public const string PathPrefix = "catalog";

        /// <summary>
        /// The folder name for image assets within the storage path.
        /// </summary>
        public const string ImageFolder = "images";

        /// <summary>
        /// A collection of valid image types (e.g., "Default", "Square", "Thumbnail", "Gallery").
        /// </summary>
        public static readonly string[] ValidTypes =
        [
            nameof(ProductImageType.Default),
            nameof(ProductImageType.Square),
            nameof(ProductImageType.Thumbnail),
            nameof(ProductImageType.Gallery),
            nameof(ProductImageType.Search)
        ];

        /// <summary>
        /// A collection of valid content (MIME) types for image files.
        /// </summary>
        public static readonly string[] ValidContentTypes =
        [
            "image/jpeg",
            "image/png",
            "image/gif",
            "image/webp"
        ];

        /// <summary>
        /// A collection of valid units for image dimensions (e.g., "mm", "cm", "in").
        /// </summary>
        public static readonly string[] ValidDimensionUnits =
        [
            "mm",
            "cm",
            "in",
            "ft"
        ];

        #endregion

        /// <summary>
        /// Generates a standardized storage path for a product image based on its associated entities and type.
        /// </summary>
        /// <param name="productId">Optional: The ID of the product the image belongs to.</param>
        /// <param name="variantId">Optional: The ID of the variant the image belongs to.</param>
        /// <param name="type">The <see cref="ProductImageType"/> of the image.</param>
        /// <returns>A formatted string representing the suggested storage path for the image file.</returns>
        public static string GetStorageFolder(Guid? productId, Guid? variantId, string type)
        {
            //var entityType = variantId.HasValue ? "variants" : "products";
            //var id = variantId ?? productId;
            var entityType = "products";
            var id = productId;
            return $"{PathPrefix}/{ImageFolder}/{entityType}/{id}/{type.ToLower()}";
        }

        /// <summary>
        /// Generates a standardized storage path for a product image based on its associated entities and type.
        /// </summary>
        /// <param name="type">The <see cref="ProductImageType"/> of the image.</param>
        /// <param name="contentType">The MIME content type of the image (e.g., "image/jpeg").</param>
        /// <returns>A formatted string representing the suggested storage path for the image file.</returns>
        public static string GetStorageFileName(string type, string contentType)
        {
            var timestamp = DateTimeOffset.UtcNow.ToString("yyyyMMddHHmmss");
            var fileExtension = contentType.Split('/')[1];
            return $"{timestamp}_{type.ToLower()}.{fileExtension}";
        }
    }

    /// <summary>
    /// Defines the categorized types of product images, influencing how they are displayed or used.
    /// </summary>
    public enum ProductImageType
    {
        /// <summary>No specific type assigned.</summary>
        None = 0,

        /// <summary>The primary or default image for a product/variant.</summary>
        Default = 1,

        /// <summary>An image optimized for square display (e.g., product grids).</summary>
        Square = 2,

        /// <summary>A small preview image.</summary>
        Thumbnail = 3,

        /// <summary>An image part of a product's main gallery.</summary>
        Gallery = 4,

        /// <summary>
        /// A specialized image used for search and discovery features, often optimized for visual similarity algorithms.
        /// </summary>
        Search = 5
    }

    #region Errors

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="ProductImage"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a product image is required but not provided.
        /// </summary>
        public static Error Required =>
            Error.Validation(code: "ProductImage.Required", description: "At least one product image is required.");

        /// <summary>
        /// Error indicating that an image of the specified type already exists for the given product/variant.
        /// Prevents duplicate image types (e.g., two 'Default' images).
        /// </summary>
        /// <param name="productId">The ID of the product.</param>
        /// <param name="variantId">The optional ID of the variant.</param>
        /// <param name="type">The type of the image (e.g., <see cref="ProductImageType.Default"/>).</param>
        public static Error AlreadyExists(Guid productId, Guid? variantId, string type) =>
            Error.Conflict(code: "ProductImage.AlreadyExists",
                description:
                $"Product image of type '{type}' for product '{productId}'{(variantId.HasValue ? $" and variant '{variantId}'" : string.Empty)} already exists.");

        /// <summary>
        /// Error indicating that a requested product image could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="ProductImage"/> that was not found.</param>
        public static Error NotFound(Guid id) =>
            Error.NotFound(code: "ProductImage.NotFound", description: $"Product image with ID '{id}' was not found.");

        /// <summary>
        /// Error indicating that the provided image type is invalid (not one of <see cref="Constraints.ValidTypes"/>).
        /// </summary>
        public static Error InvalidType =>
            Error.Validation(code: "ProductImage.InvalidType",
                description: $"Type must be one of: {string.Join(separator: ", ", value: Constraints.ValidTypes)}.");

        /// <summary>
        /// Error indicating that the provided content type is invalid (not one of <see cref="Constraints.ValidContentTypes"/>).
        /// </summary>
        public static Error InvalidContentType =>
            Error.Validation(code: "ProductImage.InvalidContentType",
                description:
                $"Content type must be one of: {string.Join(separator: ", ", value: Constraints.ValidContentTypes)}.");

        /// <summary>
        /// Error indicating that the provided dimension unit is invalid (not one of <see cref="Constraints.ValidDimensionUnits"/>).
        /// </summary>
        public static Error InvalidDimensionUnit =>
            Error.Validation(code: "ProductImage.InvalidDimensionUnit",
                description:
                $"Dimension unit must be one of: {string.Join(separator: ", ", value: Constraints.ValidDimensionUnits)}.");

        /// <summary>
        /// Error indicating that the image URL is invalid (e.g., empty, too long).
        /// </summary>
        public static Error InvalidUrl =>
            Error.Validation(code: "ProductImage.InvalidUrl",
                description: $"URL must not exceed {Constraints.UrlMaxLength} characters and cannot be empty.");

        /// <summary>
        /// Error indicating that the provided image embedding vector for a specific model has an invalid dimension.
        /// </summary>
        /// <param name="modelName">The name of the embedding model (e.g., "OpenCLIP", "DINOv2").</param>
        /// <param name="expectedDimension">The expected dimension for the model's embedding.</param>
        /// <param name="actualDimension">The actual dimension of the provided embedding.</param>
        public static Error InvalidEmbeddingDimension(string modelName, int expectedDimension, int actualDimension) =>
            Error.Validation(code: "ProductImage.InvalidEmbeddingDimension",
                description:
                $"Embedding for model '{modelName}' must be {expectedDimension}-dimensional, but received {actualDimension}.");

        /// <summary>
        /// Error indicating that an operation requiring an image embedding for a specific model was attempted, but no embedding has been generated for that model yet.
        /// </summary>
        /// <param name="modelName">The name of the embedding model.</param>
        public static Error EmbeddingNotGenerated(string modelName) =>
            Error.Validation(code: "ProductImage.EmbeddingNotGenerated",
                description: $"Image embedding for model '{modelName}' has not been generated yet.");

        /// <summary>
        /// Error indicating that an invalid or unsupported model name was provided for an embedding operation.
        /// </summary>
        /// <param name="modelName">The invalid model name provided.</param>
        public static Error InvalidModelNameForEmbedding(string modelName) =>
            Error.Validation(code: "ProductImage.InvalidModelNameForEmbedding",
                description: $"Invalid or unsupported model name '{modelName}' for embedding operation.");
    }

    #endregion

    #region Core Properties

    public Guid Id { get; set; }

    /// <summary>
    /// Gets or sets the MIME type of the image content (e.g., "image/jpeg", "image/png").
    /// </summary>
    public string ContentType { get; set; } = "image/jpeg";

    /// <summary>
    /// Gets or sets the width of the image in pixels. Nullable if dimensions are unknown.
    /// </summary>
    public int? Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the image in pixels. Nullable if dimensions are unknown.
    /// </summary>
    public int? Height { get; set; }

    /// <summary>
    /// Gets or sets the unit of measurement for physical dimensions, if applicable (e.g., "cm", "in").
    /// </summary>
    public string? DimensionsUnit { get; set; }

    /// <summary>
    /// Gets or sets the 1024-dimensional embedding vector of the image, typically derived from
    /// an OpenCLIP ViT-H/14 (LAION-2B) model. This vector is used for visual similarity calculations.
    /// </summary>
    public Vector? EmbeddingOpenCLIP { get; set; }

    /// <summary>
    /// Gets or sets the version or name of the machine learning model that generated the <see cref="EmbeddingOpenCLIP"/>.
    /// Defaults to "openclip-vit-h-14-laion2b".
    /// </summary>
    public string? EmbeddingOpenCLIPModel { get; set; } = "openclip-vit-h-14-laion2b";

    /// <summary>
    /// Gets or sets the timestamp indicating when the <see cref="EmbeddingOpenCLIP"/> was last generated.
    /// </summary>
    public DateTimeOffset? EmbeddingOpenCLIPGeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the embedding vector generated by the DINOv2 model.
    /// </summary>
    public Vector? EmbeddingDinoV2 { get; set; }

    /// <summary>
    /// Gets or sets the name or version of the machine learning model that generated the <see cref="EmbeddingDinoV2"/>.
    /// </summary>
    public string? EmbeddingDinoV2Model { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the <see cref="EmbeddingDinoV2"/> was last generated.
    /// </summary>
    public DateTimeOffset? EmbeddingDinoV2GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the embedding vector generated by the BLIP-2 model.
    /// </summary>
    public Vector? EmbeddingBLIP2 { get; set; }

    /// <summary>
    /// Gets or sets the name or version of the machine learning model that generated the <see cref="EmbeddingBLIP2"/>.
    /// </summary>
    public string? EmbeddingBLIP2Model { get; set; }

    /// <summary>
    /// Gets or sets the timestamp indicating when the <see cref="EmbeddingBLIP2"/> was last generated.
    /// </summary>
    public DateTimeOffset? EmbeddingBLIP2GeneratedAt { get; set; }

    #endregion

    #region Relationships

    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="Product"/> this image is associated with.
    /// Null if the image is associated directly with a <see cref="Variant"/>.
    /// </summary>
    public Guid? ProductId { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the <see cref="Variant"/> this image is associated with.
    /// Null if the image is associated directly with a <see cref="Product"/>.
    /// </summary>
    public Guid? VariantId { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Product"/>.
    /// </summary>
    public Product? Product { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Variant"/>.
    /// </summary>
    public Variant? Variant { get; set; }

    #endregion

    #region Computed Properties

    /// <summary>
    /// Indicates if this image is of type <see cref="ProductImageType.Default"/>.
    /// </summary>
    public new bool IsDefault => Type == nameof(ProductImageType.Default);

    /// <summary>
    /// Indicates if this image is of type <see cref="ProductImageType.Square"/>.
    /// </summary>
    public bool IsSquare => Type == nameof(ProductImageType.Square);

    /// <summary>
    /// Indicates if this image is of type <see cref="ProductImageType.Thumbnail"/>.
    /// </summary>
    public bool IsThumbnail => Type == nameof(ProductImageType.Thumbnail);

    /// <summary>
    /// Gets the aspect ratio of the image as a string (e.g., "16:9", "1:1").
    /// Returns "unknown" if dimensions are not available or height is zero.
    /// </summary>
    public string AspectRatio => Width.HasValue && Height.HasValue && Height > 0 ? $"{Width}:{Height}" : "unknown";

    /// <summary>
    /// Indicates if the image has a generated <see cref="EmbeddingOpenCLIP"/>.
    /// </summary>
    public bool HasEmbeddingOpenCLIP => EmbeddingOpenCLIP != null;

    /// <summary>
    /// Indicates if the image has a generated <see cref="EmbeddingDinoV2"/>.
    /// </summary>
    public bool HasEmbeddingDinoV2 => EmbeddingDinoV2 != null;

    /// <summary>
    /// Indicates if the image has a generated <see cref="EmbeddingBLIP2"/>.
    /// </summary>
    public bool HasEmbeddingBLIP2 => EmbeddingBLIP2 != null;

    /// <summary>
    /// Indicates if the image has any generated embedding from any model.
    /// </summary>
    public bool HasAnyEmbedding => HasEmbeddingOpenCLIP || HasEmbeddingDinoV2 || HasEmbeddingBLIP2;

    public double? CalculatedAspectRatio =>
        Width.HasValue && Height.HasValue && Height > 0
            ? Math.Round((double)Width.Value / Height.Value, 4)
            : null;

    public bool MatchesExpectedAspectRatio(double tolerance = 0.02)
    {
        var spec = GetSizeSpec(Enum.Parse<ProductImageType>(Type));

        if (!spec.AspectRatio.HasValue || !CalculatedAspectRatio.HasValue)
            return true;

        return Math.Abs(CalculatedAspectRatio.Value - spec.AspectRatio.Value) <= tolerance;
    }

    #endregion

    #region Constructors

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private ProductImage() { }

    #endregion

    #region Factory

    /// <summary>
    /// Factory method to create a new <see cref="ProductImage"/> instance.
    /// Performs validation on URL, content type, dimensions unit, and image type.
    /// </summary>
    /// <param name="url">The URL where the image file is hosted.</param>
    /// <param name="productId">Optional: The ID of the parent <see cref="Product"/> this image is for.</param>
    /// <param name="variantId">Optional: The ID of the parent <see cref="Variant"/> this image is for.</param>
    /// <param name="alt">Alternative text for the image, for accessibility and SEO.</param>
    /// <param name="position">The display order of the image among others.</param>
    /// <param name="type">The classification of the image (e.g., "Default", "Square"). Defaults to "Default".</param>
    /// <param name="contentType">The MIME type of the image (e.g., "image/jpeg"). Defaults to "image/jpeg".</param>
    /// <param name="width">The width of the image in pixels.</param>
    /// <param name="height">The height of the image in pixels.</param>
    /// <param name="dimensionsUnit">The unit of measurement for dimensions (e.g., "cm", "in").</param>
    /// <param name="publicMetadata">Optional dictionary for public-facing metadata.</param>
    /// <param name="privateMetadata">Optional dictionary for internal-only metadata.</param>
    /// <returns>
    /// An <see cref="ErrorOr{ProductImage}"/> result.
    /// Returns the newly created <see cref="ProductImage"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails (e.g., <see cref="Errors.InvalidUrl"/>, <see cref="Errors.InvalidContentType"/>).
    /// </returns>
    /// <remarks>
    /// This method performs validation to ensure the image details are consistent with defined constraints.
    /// It initializes metadata dictionaries to prevent null references.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var productImageResult = ProductImage.Create(
    ///     url: "https://example.com/images/shirt_main.jpg",
    ///     productId: Guid.NewGuid(),
    ///     alt: "Blue T-Shirt main view",
    ///     type: nameof(ProductImageType.Default),
    ///     contentType: "image/jpeg",
    ///     width: 800,
    ///     height: 600);
    /// 
    /// if (productImageResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating ProductImage: {productImageResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newImage = productImageResult.Value;
    ///     Console.WriteLine($"Created ProductImage with URL: {newImage.Url}");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
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

        if (!Constraints.ValidTypes.Contains(value: type.ToString()))
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
            PublicMetadata =
                new Dictionary<string, object?>(dictionary: publicMetadata ?? new Dictionary<string, object?>()),
            PrivateMetadata =
                new Dictionary<string, object?>(dictionary: privateMetadata ?? new Dictionary<string, object?>()),
            CreatedAt = DateTimeOffset.UtcNow
        };

        return image;
    }

    #endregion

    #region Business Logic

    /// <summary>
    /// Marks the <see cref="ProductImage"/> for logical deletion.
    /// In this context, deletion typically means removing it from the collection of its parent <see cref="Product"/> or <see cref="Variant"/>.
    /// Actual database removal would be handled by the parent aggregate or persistence layer.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Always returns <see cref="Result.Deleted"/>, as the removal from the parent collection
    /// is managed by the <see cref="Product"/> or <see cref="Variant"/> aggregate.
    /// </returns>
    /// <remarks>
    /// This method signals that the image should no longer be associated with its parent entity.
    /// The <see cref="Product.RemoveImage(Guid)"/> or <see cref="Variant.RemoveImage(Guid)"/> methods should be used to initiate the removal.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;

    /// <summary>
    /// Updates the properties of the <see cref="ProductImage"/>.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="variantId"></param>
    /// <param name="url">The new URL for the image. If null, the existing URL is retained.</param>
    /// <param name="alt">The new alt text for the image. If null, the existing alt text is retained.</param>
    /// <param name="position">The new position for the image. If null, the existing position is retained.</param>
    /// <param name="type">The new type for the image. If null, the existing type is retained.</param>
    /// <param name="contentType">The new content type for the image. If null, the existing content type is retained.</param>
    /// <param name="width">The new width for the image. If null, the existing width is retained.</param>
    /// <param name="height">The new height for the image. If null, the existing height is retained.</param>
    /// <param name="dimensionsUnit">The new dimensions unit for the image. If null, the existing unit is retained.</param>
    /// <param name="publicMetadata">The new public metadata for the image. If null, the existing public metadata is retained.</param>
    /// <param name="privateMetadata">The new private metadata for the image. If null, the existing private metadata is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{ProductImage}"/> result.
    /// Returns the updated <see cref="ProductImage"/> instance on success.
    /// Returns <see cref="Errors.InvalidContentType"/> if the new content type is invalid.
    /// Returns <see cref="Errors.InvalidDimensionUnit"/> if the new dimensions unit is invalid.
    /// </returns>
    /// <remarks>
    /// This method performs validation for new values provided. If the <paramref name="url"/> changes,
    /// any existing <see cref="Embedding"/> and <see cref="EmbeddingGeneratedAt"/> are cleared,
    /// as the embedding would no longer be valid for the new image.
    /// The <c>UpdatedAt</c> timestamp is automatically updated if any changes occur.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var productImage = GetProductImageById(imageId);
    /// var updateResult = productImage.Update(
    ///     alt: "Updated alt text for image",
    ///     position: 1,
    ///     publicMetadata: new Dictionary&lt;string, object?&gt; { { "caption", "Product in natural light" } });
    /// 
    /// if (updateResult.IsError)
    /// {
    ///     Console.WriteLine($"Error updating ProductImage: {updateResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"ProductImage '{productImage.Url}' updated successfully.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<ProductImage> Update(
        Guid? variantId = null,
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
            // Clear all embeddings when URL changes, as they are no longer valid for the new image.
            EmbeddingOpenCLIP = null;
            EmbeddingOpenCLIPGeneratedAt = null;
            EmbeddingDinoV2 = null;
            EmbeddingDinoV2GeneratedAt = null;
            EmbeddingBLIP2 = null;
            EmbeddingBLIP2GeneratedAt = null;
            changed = true;
        }

        if (variantId != null && variantId != VariantId)
        {
            VariantId = variantId;
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

    /// <summary>
    /// Sets the visual embedding for the image for a specific model.
    /// This embedding is used for visual similarity searches and recommendations.
    /// </summary>
    /// <param name="modelName">The name of the model for which the embedding is being set (e.g., "OpenCLIP", "DINOv2", "BLIP-2").</param>
    /// <param name="embedding">The float array representing the multi-dimensional embedding vector.</param>
    /// <param name="expectedDimension">The expected dimension of the embedding for the given model.</param>
    /// <param name="modelVersion">Optional: The specific version or variant name of the model that generated the embedding.</param>
    /// <returns>
    /// An <see cref="ErrorOr{ProductImage}"/> result.
    /// Returns the updated <see cref="ProductImage"/> instance on success.
    /// Returns <see cref="Errors.InvalidEmbeddingDimension"/> if the provided embedding array has an incorrect dimension.
    /// Returns <see cref="Errors.InvalidModelNameForEmbedding"/> if the model name is not supported.
    /// </returns>
    public ErrorOr<ProductImage> SetEmbedding(string modelName, float[] embedding, int expectedDimension,
        string? modelVersion = null)
    {
        if (embedding.Length != expectedDimension)
            return Errors.InvalidEmbeddingDimension(modelName, expectedDimension, embedding.Length);

        Vector vector = new Vector(v: embedding);
        DateTimeOffset now = DateTimeOffset.UtcNow;
        string defaultModelVersion = modelName.ToLowerInvariant(); // Use lowercased modelName as default version

        switch (modelName.ToLowerInvariant())
        {
            case "openclip":
                EmbeddingOpenCLIP = vector;
                EmbeddingOpenCLIPModel = modelVersion ?? defaultModelVersion;
                EmbeddingOpenCLIPGeneratedAt = now;
                break;
            case "dinov2":
                EmbeddingDinoV2 = vector;
                EmbeddingDinoV2Model = modelVersion ?? defaultModelVersion;
                EmbeddingDinoV2GeneratedAt = now;
                break;
            case "blip2":
                EmbeddingBLIP2 = vector;
                EmbeddingBLIP2Model = modelVersion ?? defaultModelVersion;
                EmbeddingBLIP2GeneratedAt = now;
                break;
            default:
                return Errors.InvalidModelNameForEmbedding(modelName);
        }

        UpdatedAt = now;
        return this;
    }

    /// <summary>
    /// Calculates the cosine similarity between this image's embedding and another image's embedding
    /// for a specified embedding model.
    /// </summary>
    /// <param name="other">The other <see cref="ProductImage"/> whose embedding is to be compared.</param>
    /// <param name="modelName">The name of the embedding model to use for comparison (e.g., "OpenCLIP", "DINOv2", "BLIP-2").</param>
    /// <returns>A <see cref="double"/> value representing the cosine similarity, ranging from 0 to 1.</returns>
    /// <exception cref="InvalidOperationException">Thrown if either image does not have an embedding for the specified model.</exception>
    /// <exception cref="ArgumentException">Thrown if an unsupported model name is provided.</exception>
    public double CalculateSimilarity(ProductImage other, string modelName)
    {
        Vector? thisEmbedding = null;
        Vector? otherEmbedding = null;

        switch (modelName.ToLowerInvariant())
        {
            case "openclip":
                thisEmbedding = EmbeddingOpenCLIP;
                otherEmbedding = other.EmbeddingOpenCLIP;
                break;
            case "dinov2":
                thisEmbedding = EmbeddingDinoV2;
                otherEmbedding = other.EmbeddingDinoV2;
                break;
            case "blip2":
                thisEmbedding = EmbeddingBLIP2;
                otherEmbedding = other.EmbeddingBLIP2;
                break;
            default:
                throw new ArgumentException($"Unsupported model name '{modelName}' for similarity calculation.",
                    nameof(modelName));
        }

        if (thisEmbedding == null || otherEmbedding == null)
            throw new InvalidOperationException(
                $"Both images must have embeddings generated by the '{modelName}' model to calculate similarity.");

        // Cosine distance is 1 - cosine similarity. So similarity = 1 - distance.
        return 1 - thisEmbedding.CosineDistance(b: otherEmbedding);
    }

    #endregion

    #region Helpers

    public sealed record ImageSizeSpec(
        int? TargetWidth,
        int? TargetHeight,
        double? AspectRatio,
        bool AllowUpscale,
        bool CropToFit)
    {
        public bool IsFixedSize => TargetWidth.HasValue && TargetHeight.HasValue;
    }


    public static ImageSizeSpec GetSizeSpec(ProductImageType type)
    {
        return type switch
        {
            ProductImageType.Default => new ImageSizeSpec(
                TargetWidth: 1200,
                TargetHeight: null,
                AspectRatio: null,
                AllowUpscale: false,
                CropToFit: false
            ),

            ProductImageType.Square => new ImageSizeSpec(
                TargetWidth: 1024,
                TargetHeight: 1024,
                AspectRatio: 1d,
                AllowUpscale: false,
                CropToFit: true
            ),

            ProductImageType.Thumbnail => new ImageSizeSpec(
                TargetWidth: 300,
                TargetHeight: 300,
                AspectRatio: 1d,
                AllowUpscale: false,
                CropToFit: true
            ),

            ProductImageType.Gallery => new ImageSizeSpec(
                TargetWidth: 1600,
                TargetHeight: null,
                AspectRatio: null,
                AllowUpscale: false,
                CropToFit: false
            ),

            ProductImageType.Search => new ImageSizeSpec(
                TargetWidth: 512,
                TargetHeight: 512,
                AspectRatio: 1d,
                AllowUpscale: true,
                CropToFit: true
            ),

            _ => new ImageSizeSpec(
                TargetWidth: null,
                TargetHeight: null,
                AspectRatio: null,
                AllowUpscale: true,
                CropToFit: false
            )
        };
    }

    #endregion
}