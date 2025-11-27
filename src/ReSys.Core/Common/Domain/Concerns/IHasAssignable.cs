using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;

namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasAssignable - Assignment tracking
// ============================================================================
public interface IHasAssignable
{
    DateTimeOffset? AssignedAt { get; set; }
    string? AssignedBy { get; set; }
    string? AssignedTo { get; set; }
}

public static class HasAssignable
{
    public static bool IsAssigned(this IHasAssignable? target) =>
        target?.AssignedAt != null && !string.IsNullOrWhiteSpace(value: target.AssignedTo);

    public static void MarkAsAssigned(this IHasAssignable? target, string assignedTo, string? assignedBy = null)
    {
        if (target == null || string.IsNullOrWhiteSpace(value: assignedTo)) return;
        target.AssignedAt = DateTimeOffset.UtcNow;
        target.AssignedTo = assignedTo;
        target.AssignedBy = assignedBy;
    }

    public static void MarkAsUnassigned(this IHasAssignable? target)
    {
        if (target == null) return;
        target.AssignedAt = null;
        target.AssignedBy = null;
        target.AssignedTo = null;
    }

    public static void AddAssignableRules<T>(this AbstractValidator<T> validator) where T : IHasAssignable
    {
        validator.RuleFor(expression: x => x.AssignedTo)
            .NotEmpty()
            .When(predicate: x => x.AssignedAt != null)
            .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(IHasAssignable.AssignedTo)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(IHasAssignable.AssignedTo))
                .Description)
            .MaximumLength(maximumLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength)
            .WithErrorCode(errorCode: CommonInput.Errors.TooLong(field: nameof(IHasAssignable.AssignedTo),
                maxLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength).Code)
            .WithMessage(errorMessage: CommonInput.Errors.TooLong(field: nameof(IHasAssignable.AssignedTo),
                maxLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength).Description);
        validator.RuleFor(expression: x => x.AssignedAt)
            .NotNull()
            .When(predicate: x => !string.IsNullOrWhiteSpace(value: x.AssignedTo))
            .WithErrorCode(errorCode: CommonInput.Errors.Required(prefix: nameof(IHasAssignable.AssignedAt)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Required(prefix: nameof(IHasAssignable.AssignedAt))
                .Description);
    }

    public static void ConfigureAssignable<T>(this EntityTypeBuilder<T> builder) where T : class, IHasAssignable
    {
        builder.Property(propertyExpression: e => e.AssignedAt)
            .IsRequired(required: false)
            .HasComment(
                comment: "AssignedAt: The date and time when the entity was assigned. Nullable if not yet assigned.");

        builder.Property(propertyExpression: e => e.AssignedBy)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength)
            .IsRequired(required: false)
            .HasComment(
                comment: "AssignedBy: The username of the person who assigned this entity. Nullable if unknown.");

        builder.Property(propertyExpression: e => e.AssignedTo)
            .HasMaxLength(maxLength: CommonInput.Constraints.NamesAndUsernames.UsernameMaxLength)
            .IsRequired(required: false)
            .HasComment(comment: "AssignedTo: The username of the assignee. Nullable if not yet assigned.");

        builder.HasIndex(indexExpression: e => e.AssignedTo);
    }
}


