using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;

namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// ISoftDeletable - Soft delete tracking
// ============================================================================

public interface ISoftDeletable
{
    // Indicates whether the entity is soft-deleted
    bool IsDeleted { get; set; }

    // Timestamp of when the entity was soft-deleted (nullable)
    DateTimeOffset? DeletedAt { get; set; }

    // User who performed the deletion (nullable)
    string? DeletedBy { get; set; }
}

public static class SoftDeletable
{
    // -----------------------
    // Helper methods
    // -----------------------

    /// <summary>
    /// Marks the entity as deleted with UTC now and optional userId.
    /// </summary>
    public static void MarkAsDeleted(this ISoftDeletable? target, string? deletedBy = null, DateTimeOffset? deletedAt = null)
    {
        if (target == null) return;

        target.IsDeleted = true;
        target.DeletedAt = deletedAt ?? DateTimeOffset.UtcNow;
        target.DeletedBy = deletedBy;
    }

    /// <summary>
    /// Restores the entity by clearing soft-delete markers.
    /// </summary>
    public static void Restore(this ISoftDeletable? target)
    {
        if (target == null) return;

        target.IsDeleted = false;
        target.DeletedAt = null;
        target.DeletedBy = null;
    }

    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddSoftDeleteRules<T>(this AbstractValidator<T> validator)
        where T : ISoftDeletable
    {
        // IsDeleted is required
        validator.RuleFor(expression: x => x.IsDeleted)
            .NotNull()
            .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(ISoftDeletable.IsDeleted)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(ISoftDeletable.IsDeleted)).Description);

        // DeletedBy: optional, max length enforced
        validator.RuleFor(expression: x => x.DeletedBy)
            .MaximumLength(maximumLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .WithErrorCode(errorCode: CommonInput.Errors.TooLong(
                field: nameof(ISoftDeletable.DeletedBy),
                maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength).Code)
            .WithMessage(errorMessage: CommonInput.Errors.TooLong(
                field: nameof(ISoftDeletable.DeletedBy),
                maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength).Description);
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigureSoftDelete<T>(this EntityTypeBuilder<T> builder)
        where T : class, ISoftDeletable
    {
        // IsDeleted: required boolean, default false
        builder.Property(propertyExpression: m => m.IsDeleted)
            .IsRequired()
            .HasDefaultValue(value: false)
            .HasComment(comment: "IsDeleted: Indicates if the entity is soft-deleted.");

        // DeletedAt: optional timestamp
        builder.Property(propertyExpression: m => m.DeletedAt)
            .IsRequired(required: false)
            .HasComment(comment: "DeletedAt: Timestamp when the entity was soft-deleted.");

        // DeletedBy: optional string, max length enforced
        builder.Property(propertyExpression: m => m.DeletedBy)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.NameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "DeletedBy: User who soft-deleted the entity.");

        // Index on IsDeleted for faster queries filtering active vs deleted records
        builder.HasIndex(indexExpression: m => m.IsDeleted);
    }
}
