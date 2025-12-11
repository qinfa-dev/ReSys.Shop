using System.Diagnostics;

namespace ReSys.Core.Common.Domain.Concerns;

/// <summary>
/// Represents an entity that exposes an identity key of type <typeparamref name="TId"/>.
/// </summary>
public interface IHasIdentity<TId> where TId : struct
{
    TId Id { get; set; }
}

public static class HasIdentity
{

    #region Utilities
    public static bool IsDefault<TId>(TId id)
    {
        if (id is null)
            return true;

        // Handle common cases explicitly
        return id switch
        {
            Guid g when g == Guid.Empty => true,
            int i when i == 0 => true,
            long l when l == 0 => true,
            string s when string.IsNullOrWhiteSpace(value: s) => true,
            _ => EqualityComparer<TId>.Default.Equals(x: id, y: default!)
        };
    }

    #endregion

    // -------------------------------------------------------------------------
    // REGION: ERRORS
    // -------------------------------------------------------------------------
    #region Errors

    /// <summary>
    /// Provides standardized validation errors for ID checks.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Creates an <see cref="Error"/> representing an invalid or default Id.
        /// </summary>
        public static Error Invalid(string? prefix = null, string? msg = null)
        {
            const string field = "Id";
            return Error.Validation(
                code: $"{CommonInput.Prefix(prefix: prefix, field: field)}.Invalid",
                description: msg ?? $"{CommonInput.Label(prefix: prefix, field: field)} must be a valid, non-default value.");
        }
    }

    #endregion


    #region Validation

    public static void ApplyIdRules<TEntity, TId>(
        this AbstractValidator<TEntity> validator,
        string? prefix = null)
        where TEntity : IHasIdentity<TId>
        where TId : struct
    {
        Debug.Assert(condition: validator != null, message: nameof(validator) + " != null");

        validator.RuleFor(expression: x => x.Id)
            .Must(predicate: id => !IsDefault(id: id))
            .WithErrorCode(errorCode: Errors.Invalid(prefix: prefix).Code)
            .WithMessage(errorMessage: Errors.Invalid(prefix: prefix).Description);
    }

    #endregion


    #region EF Core
    public static void ConfigureId<TEntity, TId>(
        this EntityTypeBuilder<TEntity> builder,
        bool isRequired = true)
        where TEntity : class, IHasIdentity<TId>
        where TId : struct
    {
        Debug.Assert(condition: builder != null, message: $"{nameof(builder)} != null");

        // Id: The primary key of the entity
        var propertyBuilder = builder.Property(propertyExpression: x => x.Id)
            .IsRequired(required: isRequired) // The Id is required by default

            // Add a comment for database documentation
            .HasComment(comment: "Id: Primary key of the entity.");

        // Map common types with sensible defaults
        if (typeof(TId) == typeof(Guid))
        {
            // For GUIDs, automatically generate a new GUID in SQL Server
            propertyBuilder.HasDefaultValueSql(sql: "NEWID()");
        }
        else if (typeof(TId) == typeof(int) || typeof(TId) == typeof(long))
        {
            // For numeric types, enable identity/auto-increment
            propertyBuilder.ValueGeneratedOnAdd();
        }
    }


    #endregion
}
