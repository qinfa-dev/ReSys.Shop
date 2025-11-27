using FluentValidation;

using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasAuditable - Combines creation and update tracking
// ============================================================================

public interface IHasAuditable : IHasCreator, IHasUpdater { }

public static class HasAuditable
{
    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddAuditableRules<T>(this AbstractValidator<T> validator) where T : IHasAuditable
    {
        // Add rules from both IHasCreator and IHasUpdater
        validator.AddCreatorRules();
        validator.AddUpdaterRules();
    }

    // -----------------------
    // EF Core configuration
    // -----------------------
    public static void ConfigureAuditable<T>(this EntityTypeBuilder<T> builder) where T : class, IHasAuditable
    {
        // Apply EF Core configuration for both creation and update tracking
        builder.ConfigureCreator();
        builder.ConfigureUpdater();
    }
}