namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasSeoMetadata - SEO fields for entities
// ============================================================================

public interface IHasSeoMetadata
{
    // Optional SEO title (shown in HTML <title>)
    string? MetaTitle { get; set; }

    // Optional SEO description (shown in HTML meta description)
    string? MetaDescription { get; set; }

    // Optional SEO keywords (comma-separated)
    string? MetaKeywords { get; set; }
}

public static class HasSeoMetadata
{
    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddSeoMetaSupportRules<T>(this AbstractValidator<T> validator, string prefix) where T : IHasSeoMetadata
    {
        // MetaTitle: optional, max length defined by TitleMaxLength
        validator.RuleFor(expression: x => x.MetaTitle)
            .MaximumLength(maximumLength: CommonInput.Constraints.Text.TitleMaxLength)
            .WithErrorCode(errorCode: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSeoMetadata.MetaTitle),
                maxLength: CommonInput.Constraints.Text.TitleMaxLength).Code)
            .WithMessage(errorMessage: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSeoMetadata.MetaTitle),
                maxLength: CommonInput.Constraints.Text.TitleMaxLength).Description);

        // MetaDescription: optional, max length defined by DescriptionMaxLength
        validator.RuleFor(expression: x => x.MetaDescription)
            .MaximumLength(maximumLength: CommonInput.Constraints.Text.DescriptionMaxLength)
            .WithErrorCode(errorCode: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSeoMetadata.MetaDescription),
                maxLength: CommonInput.Constraints.Text.DescriptionMaxLength).Code)
            .WithMessage(errorMessage: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSeoMetadata.MetaDescription),
                maxLength: CommonInput.Constraints.Text.DescriptionMaxLength).Description);

        // MetaKeywords: optional, max length defined by ShortTextMaxLength
        validator.RuleFor(expression: x => x.MetaKeywords)
            .MaximumLength(maximumLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .WithErrorCode(errorCode: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSeoMetadata.MetaKeywords),
                maxLength: CommonInput.Constraints.Text.ShortTextMaxLength).Code)
            .WithMessage(errorMessage: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSeoMetadata.MetaKeywords),
                maxLength: CommonInput.Constraints.Text.ShortTextMaxLength).Description);
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigureSeoMetadata<T>(this EntityTypeBuilder<T> builder) where T : class, IHasSeoMetadata
    {
        // MetaTitle column: optional, max length enforced
        builder.Property(propertyExpression: x => x.MetaTitle)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.TitleMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "MetaTitle: Optional SEO title for the entity.");

        // MetaDescription column: optional, max length enforced
        builder.Property(propertyExpression: x => x.MetaDescription)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.DescriptionMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "MetaDescription: Optional SEO description for the entity.");

        // MetaKeywords column: optional, max length enforced
        builder.Property(propertyExpression: x => x.MetaKeywords)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "MetaKeywords: Optional SEO keywords (comma-separated).");
    }
}
