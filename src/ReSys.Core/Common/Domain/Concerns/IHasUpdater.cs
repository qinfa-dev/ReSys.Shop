namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasUpdater - Tracks who last updated an entity and when
// ============================================================================

public interface IHasUpdater
{
    // Timestamp of the last update (nullable)
    DateTimeOffset? UpdatedAt { get; set; }

    // Username or identifier of the person who last updated the entity (nullable)
    string? UpdatedBy { get; set; }
}

public static class HasUpdater
{
    // -----------------------
    // Helper methods
    // -----------------------

    /// <summary>
    /// Marks the entity as updated by setting UpdatedAt to UtcNow and optionally UpdatedBy.
    /// </summary>
    public static void MarkAsUpdated(this IHasUpdater? target, string? updatedBy = null)
    {
        if (target == null) return;
        target.UpdatedAt = DateTimeOffset.UtcNow;
        target.UpdatedBy = updatedBy;
    }

    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddUpdaterRules<T>(this AbstractValidator<T> validator) where T : IHasUpdater
    {
        // UpdatedAt must be valid if present
        validator.RuleFor(expression: x => x.UpdatedAt)
            .Must(predicate: at => !at.HasValue || at.Value != default)
            .When(predicate: x => x.UpdatedAt.HasValue);

        // UpdatedBy cannot be empty if present
        validator.RuleFor(expression: x => x.UpdatedBy)
            .NotEmpty()
            .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.UpdatedBy));
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigureUpdater<T>(this EntityTypeBuilder<T> builder) where T : class, IHasUpdater
    {
        // UpdatedBy column: optional string, user max length 
        builder.Property(propertyExpression: m => m.UpdatedBy)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "UpdatedBy: User who last updated this record.");

        // UpdatedAt column: optional timestamp
        builder.Property(propertyExpression: m => m.UpdatedAt)
            .IsRequired(required: false)
            .HasComment(comment: "UpdatedAt: Timestamp of when the record was last updated.");
    }
}
