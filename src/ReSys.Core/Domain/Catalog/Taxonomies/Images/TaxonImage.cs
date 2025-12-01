using ErrorOr;

using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Images;

public sealed class TaxonImage : BaseImageAsset
{
    #region Errors
    public static class Errors
    {
        public static Error NotFound(Guid id) => Error.NotFound(code: "TaxonImage.NotFound", description: $"TaxonImage with ID '{id}' was not found.");
    }
    #endregion

    #region Properties
    public Guid TaxonId { get; set; }
    #endregion

    #region Relationships
    public Taxon Taxon { get; set; } = null!;
    #endregion

    #region Constructors
    private TaxonImage() { }
    #endregion

    #region Factory Methods
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
        image.SetPublic(key: "width", value: size?.ToString() ?? string.Empty);
        image.SetPublic(key: "height", value: size?.ToString() ?? string.Empty);
        return image;
    }
    #endregion

    #region Business Logic
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

        if (alt != null && Alt == alt)
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
