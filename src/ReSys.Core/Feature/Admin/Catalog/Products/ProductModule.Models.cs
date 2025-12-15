using Mapster;

using ReSys.Core.Domain.Catalog.Products;

namespace ReSys.Core.Feature.Admin.Catalog.Products;

public static partial class ProductModule
{
    public static class Models
    {
        public record Parameter : IHasParameterizableName, IHasUniqueName, IHasMetadata, IHasSlug, IHasSeoMetadata
        {
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string Slug { get; set; } = string.Empty;
            public DateTimeOffset? AvailableOn { get; set; }
            public DateTimeOffset? MakeActiveAt { get; set; }
            public DateTimeOffset? DiscontinueOn { get; set; }
            public bool IsDigital { get; set; }
            public string? MetaTitle { get; set; }
            public string? MetaDescription { get; set; }
            public string? MetaKeywords { get; set; }
            public IDictionary<string, object?>? PublicMetadata { get; set; }
            public IDictionary<string, object?>? PrivateMetadata { get; set; }
        }

        public sealed class ParameterValidator : AbstractValidator<Parameter>
        {
            public ParameterValidator()
            {
                const string prefix = nameof(Product);
                this.AddParameterizableNameRules(prefix: prefix);
                this.AddMetadataSupportRules(prefix: prefix);
                this.AddSlugRules(prefix: prefix);
                this.AddSeoMetaSupportRules(prefix: prefix);

                RuleFor(expression: x => x.Description)
                    .MaximumLength(maximumLength: Product.Constraints.DescriptionMaxLength)
                    .When(predicate: x => !string.IsNullOrEmpty(value: x.Description));

                RuleFor(expression: x => x.DiscontinueOn)
                    .Must(predicate: (obj, discontinueOn) =>
                        discontinueOn.HasValue &&
                        obj.MakeActiveAt.HasValue &&
                        discontinueOn.Value >= obj.MakeActiveAt.Value)
                    .WithErrorCode(errorCode: "Product.DiscontinueOn")
                    .WithMessage(errorMessage: "Discontinue date must be after or equal to make active date.");
            }
        }


        public record SelectItem
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string Slug { get; set; } = string.Empty;
        }

        public record ListItem : IHasIdentity<Guid>
        {
            public Guid Id { get; set; }
            public string Name { get; set; } = string.Empty;
            public string Presentation { get; set; } = string.Empty;
            public string? Description { get; set; }
            public string Slug { get; set; } = string.Empty;
            public string Status { get; set; } = string.Empty;
            public bool IsDigital { get; set; }
            public DateTimeOffset? AvailableOn { get; set; }
            public int VariantCount { get; set; }
            public int ImageCount { get; set; }
            public bool Available { get; set; }
            public bool Purchasable { get; set; }
            public DateTimeOffset CreatedAt { get; set; }
            public DateTimeOffset? UpdatedAt { get; set; }
        }

        public record Detail : Parameter, IHasIdentity<Guid>
        {
            public Guid Id { get; set; }
        }

        public sealed class Mapping : IRegister
        {
            public void Register(TypeAdapterConfig config)
            {
                config.NewConfig<Product, SelectItem>()
                    .Map(member: dest => dest.Id, source: src => src.Id)
                    .Map(member: dest => dest.Name, source: src => src.Name)
                    .Map(member: dest => dest.Presentation, source: src => src.Presentation)
                    .Map(member: dest => dest.Slug, source: src => src.Slug);

                config.NewConfig<Product, ListItem>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation)
                    .Map(dest => dest.Description, src => src.Description)
                    .Map(dest => dest.Slug, src => src.Slug)
                    .Map(dest => dest.Status, src => src.Status.ToString())
                    .Map(dest => dest.IsDigital, src => src.IsDigital)
                    .Map(dest => dest.AvailableOn, src => src.AvailableOn)
                    .Map(dest => dest.VariantCount, src => src.Variants.Count)
                    .Map(dest => dest.ImageCount, src => src.Images.Count)
                    .Map(dest => dest.Available, src => src.Available)
                    .Map(dest => dest.Purchasable, src => src.Purchasable)
                    .Map(dest => dest.CreatedAt, src => src.CreatedAt)
                    .Map(dest => dest.UpdatedAt, src => src.UpdatedAt);

                config.NewConfig<Product, Detail>()
                    .Map(dest => dest.Id, src => src.Id)
                    .Map(dest => dest.Name, src => src.Name)
                    .Map(dest => dest.Presentation, src => src.Presentation)
                    .Map(dest => dest.Description, src => src.Description)
                    .Map(dest => dest.Slug, src => src.Slug)
                    .Map(dest => dest.AvailableOn, src => src.AvailableOn)
                    .Map(dest => dest.MakeActiveAt, src => src.MakeActiveAt)
                    .Map(dest => dest.DiscontinueOn, src => src.DiscontinueOn)
                    .Map(dest => dest.IsDigital, src => src.IsDigital)
                    .Map(dest => dest.MetaTitle, src => src.MetaTitle)
                    .Map(dest => dest.MetaDescription, src => src.MetaDescription)
                    .Map(dest => dest.MetaKeywords, src => src.MetaKeywords)
                    .Map(dest => dest.PublicMetadata, src => src.PublicMetadata)
                    .Map(dest => dest.PrivateMetadata, src => src.PrivateMetadata);
            }
        }
    }
}