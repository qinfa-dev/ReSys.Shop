using System.Diagnostics;

using ErrorOr;

using FluentValidation;

using Humanizer;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Extensions;

namespace ReSys.Core.Common.Domain.Concerns;

/// <summary>
/// Defines an entity with parameterizable name (like Rails' ParameterizableName concern).
/// </summary>
public interface IHasParameterizableName
{
    string Name { get; set; }
    string Presentation { get; set; }
}

/// <summary>
/// Provides reusable validation, normalization, EF Core mapping, and search utilities
/// for entities with parameterizable names.
/// </summary>
public static class HasParameterizableName
{
    // -------------------------------------------------------------------------
    // Inner Constraints
    // -------------------------------------------------------------------------
    public static class Constraints
    {
        public const int MinLength = CommonInput.Constraints.NamesAndUsernames.NameMinLength;
        public const int MaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;
    }

    // -------------------------------------------------------------------------
    // Errors
    // -------------------------------------------------------------------------
    public static class Errors
    {
        public static Error NameRequired(string? prefix) =>
            CommonInput.Errors.Required(prefix: prefix,
                field: nameof(IHasParameterizableName.Name));

        public static Error NameInvalidLengthRange(string? prefix = null) =>
            CommonInput.Errors.InvalidRange(prefix: prefix,
                field: nameof(IHasParameterizableName.Name),
                min: Constraints.MinLength,
                max: Constraints.MaxLength);

        public static Error NameInvalidFormat(string? prefix = null) =>
            CommonInput.Errors.InvalidSlug(prefix: prefix,
                field: nameof(IHasParameterizableName.Name));

        public static Error PresentationInvalidLengthRange(string? prefix = null) =>
            CommonInput.Errors.InvalidRange(prefix: prefix,
                field: nameof(IHasParameterizableName.Presentation),
                min: Constraints.MinLength,
                max: Constraints.MaxLength);

        public static Error PresentationInvalidFormat(string? prefix) =>
            CommonInput.Errors.InvalidName(prefix: prefix,
                field: nameof(IHasParameterizableName.Presentation));
    }

    public static (string name, string presentation) NormalizeParams(string? name, string? presentation)
    {
        name = name?.Trim().ToSlug() ?? string.Empty;
        presentation = presentation?.Trim() ?? string.Empty;

        if (string.IsNullOrWhiteSpace(value: presentation))
        {
            presentation = name.Humanize(casing: LetterCasing.Title);
        }

        return (name, presentation);
    }
    // -------------------------------------------------------------------------
    // FluentValidation
    // -------------------------------------------------------------------------

    public static void AddParameterizableNameRules<TEntity>(
        this AbstractValidator<TEntity> validator,
        string? prefix = null)
        where TEntity : IHasParameterizableName
    {
        Debug.Assert(condition: validator != null,
            message: $"{nameof(validator)} != null");

        validator.RuleFor(expression: x => x.Name)
            .NotEmpty()
            .WithErrorCode(errorCode: Errors.NameInvalidLengthRange(prefix: prefix).Code)
            .WithMessage(errorMessage: Errors.NameInvalidLengthRange(prefix: prefix).Description)
            .Must(predicate: value => !string.IsNullOrEmpty(value: value) && CommonInput.Constraints.SlugsAndVersions.SlugRegex.IsMatch(input: value))
            .WithErrorCode(errorCode: Errors.NameInvalidFormat(prefix: prefix).Code)
            .WithMessage(errorMessage: Errors.NameInvalidFormat(prefix: prefix).Description);

        validator.RuleFor(expression: x => x.Presentation)
            .NotEmpty()
            .WithErrorCode(errorCode: Errors.PresentationInvalidLengthRange(prefix: prefix).Code)
            .WithMessage(errorMessage: Errors.PresentationInvalidLengthRange(prefix: prefix).Description)
            .Must(predicate: value => !string.IsNullOrEmpty(value: value) && CommonInput.Constraints.NamesAndUsernames.NameRegex.IsMatch(input: value))
            .WithErrorCode(errorCode: Errors.PresentationInvalidFormat(prefix: prefix).Code)
            .WithMessage(errorMessage: Errors.PresentationInvalidFormat(prefix: prefix).Description);
    }

    // -------------------------------------------------------------------------
    // EF Core Mapping
    // -------------------------------------------------------------------------

    public static void ConfigureParameterizableName<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        bool isRequired = true)
        where TEntity : class, IHasParameterizableName
    {
        Debug.Assert(condition: builder != null, message: $"{nameof(builder)} != null");

        builder.Property(propertyExpression: x => x.Name)
            .IsRequired(required: isRequired) 
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.ShortTextMaxLength) 
            .HasComment(comment: "Name: Normalized parameterizable name for internal use.");

        builder.Property(propertyExpression: x => x.Presentation)
            .HasMaxLength(maxLength: CommonInput.Constraints.Text.MediumTextMaxLength)
            .HasComment(comment: "Presentation: Human-readable version of the parameterizable name.");
    }


    // -------------------------------------------------------------------------
    // Query / Search Utilities
    // -------------------------------------------------------------------------

    public static IQueryable<TEntity> SearchByName<TEntity>(
        this IQueryable<TEntity> query,
        string? term)
        where TEntity : IHasParameterizableName
    {
        if (string.IsNullOrWhiteSpace(value: term)) return query;

        term = term.ToLowerInvariant();
        return query.Where(predicate: x =>
            (x.Name).ToLower().Contains(term) ||
            (x.Presentation).ToLower().Contains(term)
        );
    }
}
