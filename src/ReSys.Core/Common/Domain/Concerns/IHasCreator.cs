namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasCreator - Creation tracking
// ============================================================================

public interface IHasCreator
{
    // Timestamp of when the entity was created
    DateTimeOffset CreatedAt { get; set; }

    // User who created the entity (optional)
    string? CreatedBy { get; set; }
}

public static class HasCreator
{
    // -----------------------
    // Helper methods
    // -----------------------

    /// <summary>
    /// Sets the creation metadata for the entity.
    /// </summary>
    public static void SetCreator(this IHasCreator? target, string? createdBy, DateTimeOffset? createdAt = null)
    {
        if (target == null) return;
        target.CreatedBy = createdBy;
        target.CreatedAt = createdAt ?? DateTimeOffset.UtcNow;
    }

    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddCreatorRules<T>(this AbstractValidator<T> validator) where T : IHasCreator
    {
        // CreatedAt is required and must not be default
        validator.RuleFor(expression: x => x.CreatedAt)
            .NotEqual(toCompare: default(DateTimeOffset))
            .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(IHasCreator.CreatedAt)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(IHasCreator.CreatedAt)).Description);

        // CreatedBy: optional but cannot exceed max length
        validator.RuleFor(expression: x => x.CreatedBy)
            .NotEmpty()
            .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(IHasCreator.CreatedBy)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(IHasCreator.CreatedBy)).Description)
            .MaximumLength(maximumLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .WithErrorCode(errorCode: CommonInput.Errors.TooLong(field: nameof(IHasCreator.CreatedBy), maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength).Code)
            .WithMessage(errorMessage: CommonInput.Errors.TooLong(field: nameof(IHasCreator.CreatedBy), maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength).Description);
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigureCreator<T>(this EntityTypeBuilder<T> builder) where T : class, IHasCreator
    {
        // CreatedBy column: optional string with max length
        builder.Property(propertyExpression: m => m.CreatedBy)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "CreatedBy: User who initially created this record.");

        // CreatedAt column: required timestamp
        builder.Property(propertyExpression: m => m.CreatedAt)
            .IsRequired()
            .HasComment(comment: "CreatedAt: Timestamp of when the record was created.");

        builder.HasIndex(indexExpression: m => m.CreatedBy);
    }
}
