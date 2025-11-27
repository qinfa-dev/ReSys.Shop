using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;

namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasPosition - Sortable ordering
// ============================================================================

public interface IHasPosition
{
    // Position in a list or ordering; must be >= 1
    int Position { get; set; }
}

public static class HasPosition
{
    // -----------------------
    // Helper methods
    // -----------------------

    // Validates that a position is >= 1
    public static bool IsValid(int position) => position >= 1;

    // Safely set a position if valid
    public static void SetPosition(this IHasPosition? target, int position)
    {
        if (target == null || !IsValid(position: position)) return;
        target.Position = position;
    }

    public static void Increment(this IHasPosition? target)
    {
        if (target == null) return;
        target.Position++;
    }

    public static void Decrement(this IHasPosition? target)
    {
        if (target == null) return;
        target.Position = Math.Max(val1: 1, val2: target.Position - 1);
    }
    // Increment position by 1

    // Decrement position by 1, minimum is 1

    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddPositionRules<T>(this AbstractValidator<T> validator, string prefix) where T : IHasPosition
    {
        validator.RuleFor(expression: x => x.Position)
            .GreaterThanOrEqualTo(valueToCompare: 1)
            .WithErrorCode(errorCode: CommonInput.Errors.OutOfRange(prefix: prefix, field: nameof(IHasPosition.Position), minValue: 1).Code)
            .WithMessage(errorMessage: CommonInput.Errors.OutOfRange(prefix: prefix, field: nameof(IHasPosition.Position), minValue: 1).Description);
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigurePosition<T>(this EntityTypeBuilder<T> builder) where T : class, IHasPosition
    {
        // Position column: required with default value 1
        builder.Property(propertyExpression: x => x.Position)
            .IsRequired()
            .HasDefaultValue(value: 1)
            .HasComment(comment: "Position: Sortable ordering of the entity, minimum value is 1.");

        // Optional index to optimize queries that order/filter by Position
        builder.HasIndex(indexExpression: x => x.Position);
    }
}
