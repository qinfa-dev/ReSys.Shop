namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasVersion - Optimistic concurrency control
// ============================================================================

public interface IHasVersion
{
    // Version number for optimistic concurrency. Must be >= 0
    long Version { get; set; }
}

public static class HasVersion
{
    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddVersionRules<T>(this AbstractValidator<T> validator) where T : IHasVersion
    {
        validator.RuleFor(expression: x => x.Version)
            .GreaterThanOrEqualTo(valueToCompare: 0) // Version must be non-negative
            .WithErrorCode(errorCode: CommonInput.Errors.OutOfRange(
                field: nameof(IHasVersion.Version),
                minValue: 0).Code)
            .WithMessage(errorMessage: CommonInput.Errors.OutOfRange(
                field: nameof(IHasVersion.Version),
                minValue: 0).Description);
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigureVersion<T>(this EntityTypeBuilder<T> builder) where T : class, IHasVersion
    {
        builder.Property(propertyExpression: x => x.Version)
            .IsRequired() // Always required
            .HasDefaultValue(value: 0L) // Default version is 0
            .ValueGeneratedOnAddOrUpdate() // EF will manage updates automatically
            .IsConcurrencyToken() // Used for optimistic concurrency checks
            .HasComment(comment: "Version: Optimistic concurrency token, incremented on updates.");
    }
}