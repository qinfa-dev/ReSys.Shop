using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

using ReSys.Core.Domain.Catalog.Products.Images;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
{
    public static partial class Images
    {
        public static class Models
        {
            public class ImageParameter :IAsset, IHasPosition
            {
                [FromForm(Name = "type")] public string Type { get; set; } = string.Empty;
                [FromForm(Name = "url")] public string? Url { get; set; }
                [FromForm(Name = "alt")] public string? Alt { get; set; }
                [FromForm(Name = "position")] public int Position { get; set; }
            }

            public class UploadImageParameter : ImageParameter
            {
                [FromForm(Name = "variant_id")] public Guid? VariantId { get; set; }
                [FromForm(Name = "file")] public IFormFile? File { get; init; }
            }

            public sealed class ImageParameterValidator : AbstractValidator<ImageParameter>
            {
                public ImageParameterValidator()
                {
                    this.AddAssetRules(prefix: nameof(ProductImage));
                    this.AddPositionRules(prefix: nameof(ProductImage));
                }
            }

            public sealed class UploadImageParameterValidator : AbstractValidator<UploadImageParameter>
            {
                public UploadImageParameterValidator()
                {
                    this.AddAssetRules(prefix: nameof(ProductImage));
                    this.AddPositionRules(prefix: nameof(ProductImage));
                    RuleFor(expression: x => x.File)
                        .ApplyImageFileRules(condition: m => string.IsNullOrEmpty(value: m.Url));
                }
            }

            public class ImageResult : IHasIdentity<Guid>
            {
                public Guid Id { get; set; }

                public string Type { get; set; } = string.Empty;
                public string Url { get; set; } = string.Empty;
                public string? Alt { get; set; }
                public int Position { get; set; }

                // =========================
                // Storage metadata
                // =========================
                public long Size { get; set; }
                public string ContentType { get; set; } = string.Empty;

                public int? Width { get; set; }
                public int? Height { get; set; }

                public IReadOnlyDictionary<int, string>? Thumbnails { get; set; }
            }

        }
    }
}