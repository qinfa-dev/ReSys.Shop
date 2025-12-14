using Microsoft.AspNetCore.Http;

namespace ReSys.Core.Common.Domain.Concerns;

public abstract class BaseImageAsset : BaseAsset, IHasPosition, IHasMetadata, IHasAuditable
{
    public int Position { get; set; }
    public DateTimeOffset CreatedAt { get; set; }
    public string? CreatedBy { get; set; }
    public DateTimeOffset? UpdatedAt { get; set; }
    public string? UpdatedBy { get; set; }
    public IDictionary<string, object?>? PublicMetadata { get; set; }
        = new Dictionary<string, object?>();

    public IDictionary<string, object?>? PrivateMetadata { get; set; }
        = new Dictionary<string, object?>();
    public bool IsDefault => this.GetPublic<bool>("is_default") == true;

    public List<Error> Validate(string? prefix = null)
    {
        var errors = new List<Error>();

        errors.AddRange(collection: this.ValidateParams(prefix: prefix));

        if (!HasBaseImageAsset.Constraints.IsValidPosition(pos: Position))
            errors.Add(item: HasBaseImageAsset.Errors.InvalidPosition(p: prefix));

        if (!HasBaseImageAsset.Constraints.IsValidAltText(Alt))
        {
            errors.Add(item: HasAsset.Errors.AltTextTooLong(p: prefix));
        }

        return errors;
    }
}

public static class HasBaseImageAsset
{
    // ======================================================================
    // CONSTRAINTS
    // ======================================================================
    public static class Constraints
    {
        public const int PositionMin = 0;
        public const int PositionMax = 10_000;

        public static class File
        {
            public const long MaxFileSize = 10 * 1024 * 1024;

            public static readonly string[] AllowedMimeTypes =
            [
                "image/jpeg",
                "image/png",
                "image/gif",
                "image/webp",
                "image/svg+xml"
            ];

            public static bool IsValidSize(long size) =>
                size > 0 && size <= MaxFileSize;

            public static bool IsValidMime(string? mime) =>
                !string.IsNullOrWhiteSpace(value: mime) &&
                AllowedMimeTypes.Contains(value: mime);
        }
        public static bool IsValidPosition(int pos) =>
            pos >= PositionMin && pos <= PositionMax;

        public static bool IsValidAltText(string? txt) =>
            string.IsNullOrWhiteSpace(value: txt) || txt.Length <= HasAsset.Constraints.KeyMaxLength;
    }

    // ======================================================================
    // ERRORS
    // ======================================================================
    public static class Errors
    {
        private const string Prefix = "ImageAsset";

        public static Error InvalidPosition(string? p = Prefix) =>
            CommonInput.Errors.OutOfRange(prefix: p, field: "Position");

        public static Error InvalidMimeType(string? p = Prefix) =>
            CommonInput.Errors.InvalidValue(prefix: p, field: "MimeType");
    }


    // ======================================================================
    // FLUENTVALIDATION RULES
    // ======================================================================

    public static void ApplyImageAssetRules<TEntity>(
        this AbstractValidator<TEntity> validator,
        string? prefix = null)
        where TEntity : BaseImageAsset
    {
        validator.AddAssetRules(prefix: prefix);

        validator.RuleFor(expression: x => x.Position)
            .InclusiveBetween(from: Constraints.PositionMin, to: Constraints.PositionMax)
            .WithErrorCode(errorCode: Errors.InvalidPosition(p: prefix).Code)
            .WithMessage(errorMessage: Errors.InvalidPosition(p: prefix).Description);
    }

    public static IRuleBuilderOptions<T, IFormFile?> ApplyImageFileRules<T>(
        this IRuleBuilder<T, IFormFile?> rule,
        Func<T, bool> condition)
    {
        return rule
            .NotNull()
            .When(predicate: condition)
            .WithErrorCode(errorCode: "ImageAsset.FileRequired")
            .WithMessage(errorMessage: "Image file is required when no URL is provided.")

            .Must(predicate: file => file == null || Constraints.File.IsValidSize(size: file.Length))
            .WithErrorCode(errorCode: "ImageAsset.FileTooLarge")
            .WithMessage(errorMessage: "File size exceeds the maximum allowed.")

            .Must(predicate: file => file == null || Constraints.File.IsValidMime(mime: file.ContentType))
            .WithErrorCode(errorCode: "ImageAsset.InvalidMimeType")
            .WithMessage(errorMessage: "Invalid file type.");
    }
}