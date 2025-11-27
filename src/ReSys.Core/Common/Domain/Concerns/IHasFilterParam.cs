using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;

using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

using ReSys.Core.Common.Extensions;

namespace ReSys.Core.Common.Domain.Concerns;
public interface IHasFilterParam
{
    string? FilterParam { get; set; }
}

public static class HasFilterParam
{
    public static class Constraints
    {
        public const int FilterParamMaxLength = 255;
    }

    /// <summary>
    /// Sets the FilterParam property using a property name string or defaults to "Name".
    /// </summary>
    /// <param name="entity">The entity implementing <see cref="IHasFilterParam"/>.</param>
    /// <param name="candidatePropertyName">Optional property name to use for generating the parameter.</param>
    public static void SetFilterParam(this IHasFilterParam entity, string? candidatePropertyName = null)
    {
        Debug.Assert(condition: entity != null,
            message: nameof(entity) + " != null");

        candidatePropertyName ??= "Name";
        PropertyInfo? prop = entity.GetType().GetProperty(name: candidatePropertyName,
            bindingAttr: BindingFlags.Public | BindingFlags.Instance);

        string? candidateValue = prop?.GetValue(obj: entity)?.ToString();

        if (string.IsNullOrWhiteSpace(value: candidateValue))
            return;

        entity.FilterParam = candidateValue.Parameterize();
    }

    /// <summary>
    /// Sets the FilterParam property using a strongly typed lambda expression.
    /// </summary>
    /// <typeparam name="TEntity">The entity type implementing <see cref="IHasFilterParam"/>.</typeparam>
    /// <param name="entity">The entity instance.</param>
    /// <param name="propertyExpression">An expression selecting the candidate property.</param>
    public static void SetFilterParam<TEntity>(
        this TEntity entity,
        Expression<Func<TEntity, object?>> propertyExpression)
        where TEntity : class, IHasFilterParam
    {
        Debug.Assert(condition: entity != null,
            message: nameof(entity) + " != null");
        Debug.Assert(condition: propertyExpression != null,
            message: nameof(propertyExpression) + " != null");

        // Extract property info from expression
        MemberExpression? memberExpression = propertyExpression.Body as MemberExpression ??
                                             (propertyExpression.Body is UnaryExpression unary
                                                 ? unary.Operand as MemberExpression
                                                 : null);

        PropertyInfo? propertyInfo = memberExpression?.Member as PropertyInfo;
        string? candidateValue = propertyInfo?.GetValue(obj: entity)?.ToString();

        if (string.IsNullOrWhiteSpace(value: candidateValue))
            return;

        entity.FilterParam = candidateValue.Parameterize();
    }

    /// <summary>
    /// Converts a string into a lowercase, dash-separated, URL-friendly string.
    /// </summary>
    private static string Parameterize(this string input)
    {
        if (string.IsNullOrWhiteSpace(value: input)) return string.Empty;

        // Trim leading/trailing dashes
        // Replace non-alphanumeric characters with dashes
        string result = input.ToSlug();

        // Truncate if exceeds max length
        if (result.Length > Constraints.FilterParamMaxLength)
            result = result[..Constraints.FilterParamMaxLength];

        return result;
    }

    public static void ConfigureFilterParam<TEntity>(
        this EntityTypeBuilder<TEntity> builder,
        bool isRequired = false)
        where TEntity : class, IHasFilterParam
    {
        Debug.Assert(condition: builder != null,
            message: nameof(builder) + " != null");

        builder.Property(propertyExpression: x => x.FilterParam)
            .HasMaxLength(maxLength: Constraints.FilterParamMaxLength)
            .IsRequired(required: isRequired)
            .HasComment(comment: "URL-friendly filter parameter generated from a source property (e.g., Name).");
    }
}
