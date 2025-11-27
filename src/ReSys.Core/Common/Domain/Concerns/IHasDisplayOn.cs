using FluentValidation;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;

namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasDisplayOn - Visibility control
// ============================================================================
public enum DisplayOn { None, Both, FrontEnd, BackEnd }

public interface IHasDisplayOn
{
    DisplayOn DisplayOn { get; set; }
}

public static class HasDisplayOn
{
    public static bool AvailableOnFrontEnd(this IHasDisplayOn entity) =>
        entity.DisplayOn == DisplayOn.Both || entity.DisplayOn == DisplayOn.FrontEnd;

    public static bool AvailableOnBackEnd(this IHasDisplayOn entity) =>
        entity.DisplayOn == DisplayOn.Both || entity.DisplayOn == DisplayOn.BackEnd;

    public static void AddDisplayOnRules<T>(this AbstractValidator<T> validator, string prefix) where T : IHasDisplayOn
    {
        validator.RuleFor(expression: x => x.DisplayOn)
            .IsInEnum()
            .WithErrorCode(errorCode: CommonInput.Errors.InvalidEnumValue<DisplayOn>(prefix: prefix, field: nameof(IHasDisplayOn.DisplayOn)).Code)
            .WithMessage(errorMessage: CommonInput.Errors.InvalidEnumValue<DisplayOn>(prefix: prefix, field: nameof(IHasDisplayOn.DisplayOn)).Description);
    }

    public static void ConfigureDisplayOn<T>(this EntityTypeBuilder<T> builder) where T : class, IHasDisplayOn
    {
        builder.Property(propertyExpression: x => x.DisplayOn)
            .HasComment(comment: "DisplayOn: Controls the visibility of the entity. Options: None, FrontEnd, BackEnd, Both.");
    }
}
