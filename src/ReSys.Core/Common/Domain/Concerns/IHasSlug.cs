namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasSlug - URL-friendly identifier
// ============================================================================

public interface IHasSlug
{
    // URL-friendly, unique identifier for the entity
    string Slug { get; set; }
}

public static class HasSlug
{
    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddSlugRules<T>(this AbstractValidator<T> validator, string prefix) where T : IHasSlug
    {
        validator.RuleFor(expression: x => x.Slug)
            .NotEmpty() // Slug is required
            .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: prefix, field: nameof(IHasSlug.Slug)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: prefix, field: nameof(IHasSlug.Slug)).Description)
            .MaximumLength(maximumLength: CommonInput.Constraints.SlugsAndVersions.SlugMaxLength) // Enforce max length
            .WithErrorCode(errorCode: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSlug.Slug),
                maxLength: CommonInput.Constraints.SlugsAndVersions.SlugMaxLength).Code)
            .WithMessage(errorMessage: CommonInput.Errors.TooLong(
                prefix: prefix,
                field: nameof(IHasSlug.Slug),
                maxLength: CommonInput.Constraints.SlugsAndVersions.SlugMaxLength).Description)
            .Matches(expression: CommonInput.Constraints.SlugsAndVersions.SlugPattern) // Enforce valid slug format
            .WithErrorCode(errorCode: CommonInput.Errors.InvalidSlug(prefix: prefix, field: nameof(IHasSlug.Slug)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.InvalidSlug(prefix: prefix, field: nameof(IHasSlug.Slug)).Description);
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigureSlug<T>(this EntityTypeBuilder<T> builder) where T : class, IHasSlug
    {
        builder.Property(propertyExpression: x => x.Slug)
            .IsRequired() // Slug is mandatory
            .HasMaxLength(maxLength: CommonInput.Constraints.SlugsAndVersions.SlugMaxLength)
            .HasComment(comment: "Slug: URL-friendly identifier, required and unique if needed.");
    }

    // -----------------------
    // Query helpers
    // -----------------------
    public static IQueryable<T> SearchBySlug<T>(this IQueryable<T> query, string? term) where T : IHasSlug
    {
        if (string.IsNullOrWhiteSpace(value: term)) return query;

        // Simple "contains" search on Slug
        return query.Where(predicate: x => x.Slug.Contains(term));
    }
}
