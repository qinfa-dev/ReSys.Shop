using System.Text.RegularExpressions;

using ReSys.Core.Common.Domain.Entities;

namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// ASSET INTERFACE + BASE IMPLEMENTATION
// ============================================================================
public interface IAsset
{
    string Type { get; set; }
    string? Url { get; set; }
    string? Alt { get; set; }
    bool Attached { get; }
}

public abstract class BaseAsset : AuditableEntity, IAsset
{
    public string Type { get; set; } = string.Empty;
    public string? Url { get; set; }
    public string? Alt { get; set; }
    public bool Attached => !string.IsNullOrWhiteSpace(value: Url);
}

public static class HasAsset
{

    public static bool IsAttached(this IAsset? asset) =>
        asset is { Attached: true };

    public static bool Matches(this IAsset? asset, IAsset? right) =>
        asset is null && right is null ||
        asset is not null && right is not null &&
        string.Equals(a: asset.Type, b: right.Type, comparisonType: StringComparison.OrdinalIgnoreCase) &&
        string.Equals(a: asset.Url, b: right.Url, comparisonType: StringComparison.OrdinalIgnoreCase);

    public static List<Error> ValidateParams(this IAsset? asset, string? prefix = null)
    {
        if (asset is null)
            return [Errors.TypeRequired(p: prefix)];

        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(value: asset.Type))
            errors.Add(item: Errors.TypeRequired(p: prefix));
        else if (asset.Type.Length > Constraints.TypeMaxLength)
            errors.Add(item: Errors.InvalidTypeLength(p: prefix));
        else if (!Regex.IsMatch(input: asset.Type,
                     pattern: Constraints.TypeAllowedPattern))
            errors.Add(item: Errors.InvalidTypePattern(p: prefix));

        if (!string.IsNullOrWhiteSpace(value: asset.Url) &&
            asset.Url.Length > Constraints.UrlMaxLength)
            errors.Add(item: Errors.UrlTooLong(p: prefix));

        return errors;
    }

    public static class Constraints
    {
        public const int MaxAssets = CommonInput.Constraints.Dictionary.MaxEntries;
        public const int TypeMaxLength = CommonInput.Constraints.Dictionary.KeyMaxLength;
        public const string TypeAllowedPattern = CommonInput.Constraints.Dictionary.KeyAllowedPattern;
        public static readonly Regex TypeAllowedRegex = CommonInput.Constraints.Dictionary.KeyAllowedRegex;

        public const int KeyMinLength = CommonInput.Constraints.Text.MinLength;
        public const int KeyMaxLength = 512;
        public const int UrlMaxLength = CommonInput.Constraints.UrlAndUri.UrlMaxLength;
        public const int FilenameMaxLength = 255;

        public static bool IsValidType(string? type) =>
            !string.IsNullOrWhiteSpace(value: type) &&
            type.Length <= TypeMaxLength &&
            TypeAllowedRegex.IsMatch(input: type);

        public static bool IsValidUrl(string? url) =>
            string.IsNullOrWhiteSpace(value: url) || url.Length <= UrlMaxLength;
    }

    public static class Errors
    {
        private const string Prefix = "Asset";

        public static Error TypeRequired(string? p = Prefix) =>
            CommonInput.Errors.Required(prefix: p, field: nameof(IAsset.Type));
        public static Error InvalidTypeLength(string? p = Prefix) =>
            CommonInput.Errors.TooLong(prefix: p, field: nameof(IAsset.Type), maxLength: Constraints.TypeMaxLength);
        public static Error InvalidTypePattern(string? p = Prefix) =>
            CommonInput.Errors.InvalidPattern(prefix: p, field: nameof(IAsset.Type), formatDescription: Constraints.TypeAllowedPattern);
        public static Error UrlTooLong(string? p = Prefix) =>
            CommonInput.Errors.TooLong(prefix: p, field: nameof(IAsset.Url), maxLength: Constraints.UrlMaxLength);
        public static Error TooManyAssets(string? p = Prefix) =>
            CommonInput.Errors.TooManyItems(prefix: p, max: Constraints.MaxAssets);
        public static Error KeyRequired(string? p = Prefix) =>
            CommonInput.Errors.Required(prefix: p, field: "Key");
        public static Error KeyTooShort(string? p = Prefix) =>
            CommonInput.Errors.TooShort(prefix: p, field: "Key", minLength: Constraints.KeyMinLength);
        public static Error KeyTooLong(string? p = Prefix) =>
            CommonInput.Errors.TooLong(prefix: p, field: "Key", maxLength: Constraints.KeyMaxLength);
        public static Error FilenameTooLong(string? p = Prefix) =>
            CommonInput.Errors.TooLong(prefix: p, field: "Filename", maxLength: Constraints.FilenameMaxLength);
        public static Error InvalidKeyPattern(string? p = Prefix) =>
            CommonInput.Errors.InvalidPattern(prefix: p, field: "Key", formatDescription: Constraints.TypeAllowedPattern);
    }


    public static void ApplyRules<TEntity>(
        this AbstractValidator<TEntity> validator, string? prefix = null)
        where TEntity : IAsset
    {
        validator.RuleFor(expression: x => x.Type)
            .NotEmpty()
            .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: prefix, field: nameof(IAsset.Type)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: prefix, field: nameof(IAsset.Type)).Description)
            .MaximumLength(maximumLength: Constraints.TypeMaxLength)
            .WithErrorCode(errorCode: Errors.InvalidTypeLength(p: prefix).Code)
            .WithMessage(errorMessage: Errors.InvalidTypeLength(p: prefix).Description)
            .Matches(expression: Constraints.TypeAllowedPattern)
            .WithErrorCode(errorCode: Errors.InvalidTypePattern(p: prefix).Code)
            .WithMessage(errorMessage: Errors.InvalidTypePattern(p: prefix).Description);

        validator.RuleFor(expression: x => x.Url)
            .MaximumLength(maximumLength: Constraints.UrlMaxLength)
            .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.Url))
            .WithErrorCode(errorCode: Errors.UrlTooLong(p: prefix).Code)
            .WithMessage(errorMessage: Errors.UrlTooLong(p: prefix).Description);
    }

    public static void ConfigureAsset<TEntity>(
        this EntityTypeBuilder<TEntity> builder, bool urlRequired = false)
        where TEntity : class, IAsset
    {
        builder.Property(propertyExpression: x => x.Type)
            .IsRequired()
            .HasMaxLength(maxLength: Constraints.TypeMaxLength);

        builder.Property(propertyExpression: x => x.Url)
            .HasMaxLength(maxLength: Constraints.UrlMaxLength)
            .IsRequired(required: urlRequired);
    }
}