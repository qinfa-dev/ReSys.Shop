using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Images;

/// <summary>
/// Represents an image associated with a <see cref="Taxon"/> (e.g., a category icon or banner).
/// This entity allows for visual representation of categories within the taxonomy.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Taxonomy Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Visual Cues</term>
/// <description>Provides visual elements for categories, enhancing user experience.</description>
/// </item>
/// <item>
/// <term>Branding</term>
/// <description>Supports category-specific branding or promotional imagery.</description>
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
/// <term>TaxonId</term>
/// <description>The unique identifier of the associated <see cref="Taxon"/>.</description>
/// </item>
/// <item>
/// <term>Type</term>
/// <description>A classification for the image's purpose (e.g., 'Icon', 'Banner').</description>
/// </item>
/// <item>
/// <term>Url</term>
/// <description>The direct link to the image file.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class TaxonImage : BaseImageAsset
{
    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="TaxonImage"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested <see cref="TaxonImage"/> could not be found.
        /// </summary>
        /// <param name="id">The unique identifier of the <see cref="TaxonImage"/> that was not found.</param>
        public static Error NotFound(Guid id) => Error.NotFound(code: "TaxonImage.NotFound", description: $"TaxonImage with ID '{id}' was not found.");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the unique identifier of the associated <see cref="Taxon"/>.
    /// </summary>
    public Guid TaxonId { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Taxon"/>.
    /// </summary>
    public Taxon Taxon { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private TaxonImage() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Factory method to create a new <see cref="TaxonImage"/> instance.
    /// Initializes basic image properties and sets position and metadata.
    /// </summary>
    /// <param name="taxonId">The unique identifier of the parent <see cref="Taxon"/> this image is for.</param>
    /// <param name="type">The classification of the image (e.g., "Icon", "Banner").</param>
    /// <param name="url">The URL where the image file is hosted.</param>
    /// <param name="alt">Alternative text for the image, for accessibility and SEO.</param>
    /// <param name="position">The display order of the image among others. Defaults to 1.</param>
    /// <param name="size">Optional: The file size of the image.</param>
    /// <param name="width">Optional: The width of the image in pixels.</param>
    /// <param name="height">Optional: The height of the image in pixels.</param>
    /// <returns>
    /// An <see cref="ErrorOr{TaxonImage}"/> result.
    /// Returns the newly created <see cref="TaxonImage"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method initializes metadata fields for size, width, and height.
    /// Basic validation for <paramref name="taxonId"/>, <paramref name="type"/>, and <paramref name="url"/>
    /// is typically handled at a higher level (e.g., in <see cref="Taxon.AddImage(TaxonImage)"/>).
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// Guid taxonId = Guid.NewGuid(); // Assume this is an existing Taxon ID
    /// var taxonImageResult = TaxonImage.Create(
    ///     taxonId: taxonId,
    ///     type: "Icon",
    ///     url: "https://example.com/icons/clothing.png",
    ///     alt: "Clothing category icon",
    ///     position: 1,
    ///     size: 10240,
    ///     width: 64,
    ///     height: 64);
    /// 
    /// if (taxonImageResult.IsError)
    /// {
    ///     Console.WriteLine($"Error creating TaxonImage: {taxonImageResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     var newImage = taxonImageResult.Value;
    ///     Console.WriteLine($"Created TaxonImage for Taxon {taxonId} with URL: {newImage.Url}");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public static ErrorOr<TaxonImage> Create(
        Guid taxonId,
        string type,
        string? url = null,
        string? alt = null,
        int position = 1,
        int? size = null,
        int? width = null,
        int? height = null)
    {
        var image = new TaxonImage
        {
            Id = Guid.NewGuid(),
            TaxonId = taxonId,
            Type = type.Trim(),
            Alt = alt?.Trim(),
            Url = url?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow
        };
        image.SetPosition(position: position);
        image.SetPublic(key: "size", value: size?.ToString() ?? string.Empty);
        image.SetPublic(key: "width", value: width?.ToString() ?? string.Empty);
        image.SetPublic(key: "height", value: height?.ToString() ?? string.Empty);
        return image;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the properties of the <see cref="TaxonImage"/>.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="type">The new classification of the image (e.g., "Icon", "Banner"). If null, the existing type is retained.</param>
    /// <param name="url">The new URL for the image. If null, the existing URL is retained.</param>
    /// <param name="alt">The new alternative text for the image. If null, the existing alt text is retained.</param>
    /// <param name="size">Optional: The new file size of the image. If null, the existing size is retained.</param>
    /// <param name="width">Optional: The new width of the image. If null, the existing width is retained.</param>
    /// <param name="height">Optional: The new height of the image. If null, the existing height is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{TaxonImage}"/> result.
    /// Returns the updated <see cref="TaxonImage"/> instance on success.
    /// </returns>
    /// <remarks>
    /// This method updates the image's type, URL, alt text, and also updates metadata for size, width, and height.
    /// The <c>UpdatedAt</c> timestamp is automatically updated if any changes occur.
    /// <para>
    /// <strong>Usage Example:</strong>
    /// <code>
    /// var taxonImage = GetTaxonImageById(imageId);
    /// var updateResult = taxonImage.Update(
    ///     url: "https://example.com/icons/clothing_v2.png",
    ///     alt: "Updated clothing category icon",
    ///     size: 15360);
    /// 
    /// if (updateResult.IsError)
    /// {
    ///     Console.WriteLine($"Error updating TaxonImage: {updateResult.FirstError.Description}");
    /// }
    /// else
    /// {
    ///     Console.WriteLine($"TaxonImage '{taxonImage.Url}' updated successfully.");
    /// }
    /// </code>
    /// </para>
    /// </remarks>
    public ErrorOr<TaxonImage> Update(
        string? type = null,
        string? url = null,
        string? alt = null,
        int? size = null,
        int? width = null,
        int? height = null)
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(value: type) && type.Trim() != Type)
        {
            Type = type.Trim();
            changed = true;
        }

        if (url != null && url != Url)
        {
            Url = url.Trim();
            changed = true;
        }

        if (alt != null && Alt != alt)
        {
            Alt = alt.Trim();
            changed = true;
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        this.SetPublic(key: "size", value: size?.ToString() ?? "");
        this.SetPublic(key: "width", value: width?.ToString() ?? "");
        this.SetPublic(key: "height", value: height?.ToString() ?? "");

        return this;
    }

    #endregion
}
