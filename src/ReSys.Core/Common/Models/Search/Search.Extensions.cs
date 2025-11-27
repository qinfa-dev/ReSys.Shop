using System.Collections.Concurrent;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace ReSys.Core.Common.Models.Search;

public static class SearchExtensions
{
    private static readonly ConcurrentDictionary<Type, PropertyInfo[]> StringPropertiesCache = new();
    private static readonly ConcurrentDictionary<string, Dictionary<string, PropertyInfo>> PropertyMappingCache = new();

    /// <summary>
    /// Applies search with configurable options for SPA scenarios.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, ISearchParams searchParams)
    {
        if (string.IsNullOrWhiteSpace(value: searchParams.SearchTerm))
            return query;

        string searchTerm = searchParams.CaseSensitive.HasValue && searchParams.CaseSensitive.Value
            ? searchParams.SearchTerm
            : searchParams.SearchTerm.ToLower(culture: CultureInfo.CurrentCulture);

        if (searchParams.SearchFields?.Length > 0)
        {
            return query.ApplySearchInFields(searchTerm: searchTerm,
                searchFields: searchParams.SearchFields,
                searchParams: searchParams);
        }

        return query.ApplyFullTextSearch(searchTerm: searchTerm,
            searchParams: searchParams);
    }

    /// <summary>
    /// Fluent API for typed search with lambda expressions.
    /// </summary>
    public static SearchBuilder<T> Search<T>(this IQueryable<T> query, string searchTerm)
    {
        return new SearchBuilder<T>(query: query,
            searchTerm: searchTerm);
    }

    /// <summary>
    /// Quick search for single field.
    /// </summary>
    public static IQueryable<T> SearchIn<T>(this IQueryable<T> query, string searchTerm, Expression<Func<T, string>> field)
    {
        if (string.IsNullOrWhiteSpace(value: searchTerm))
            return query;

        return query.ApplySearch(searchTerm: searchTerm,
            searchExpressions: field);
    }

    /// <summary>
    /// Search with multiple lambda expressions.
    /// </summary>
    public static IQueryable<T> ApplySearch<T>(this IQueryable<T> query, string searchTerm, params Expression<Func<T, string>>[]? searchExpressions)
    {
        if (string.IsNullOrWhiteSpace(value: searchTerm) || searchExpressions == null || searchExpressions.Length == 0)
            return query;

        ParameterExpression param = Expression.Parameter(type: typeof(T),
            name: "x");
        ConstantExpression searchConstant = Expression.Constant(value: searchTerm.ToLower(culture: CultureInfo.CurrentCulture));
        Expression? combinedExpression = null;

        foreach (Expression<Func<T, string>> searchExpression in searchExpressions)
        {
            Expression? propertyExpression = ReplaceParameter(expression: searchExpression.Body,
                oldParameter: searchExpression.Parameters[index: 0],
                newParameter: param);
            BinaryExpression? searchCondition = CreateSearchCondition(propertyExpression: propertyExpression,
                searchConstant: searchConstant);

            combinedExpression = combinedExpression == null
                ? searchCondition
                : Expression.OrElse(left: combinedExpression,
                    right: searchCondition);
        }

        if (combinedExpression != null)
        {
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(body: combinedExpression,
                parameters: param);
            query = query.Where(predicate: lambda);
        }

        return query;
    }

    private static IQueryable<T> ApplySearchInFields<T>(this IQueryable<T> query, string searchTerm, string[] searchFields, ISearchParams searchParams)
    {
        ParameterExpression param = Expression.Parameter(type: typeof(T),
            name: "x");
        ConstantExpression searchConstant = Expression.Constant(value: searchTerm);
        Expression? combinedExpression = null;

        Type entityType = typeof(T);
        Dictionary<string, PropertyInfo> propertyMapping = GetPropertyMapping<T>();

        foreach (string fieldName in searchFields)
        {
            PropertyInfo? property = FindProperty(propertyMapping: propertyMapping,
                fieldName: fieldName);
            if (property == null || property.PropertyType != typeof(string))
                continue;

            MemberExpression propertyAccess = Expression.Property(expression: param,
                property: property);
            BinaryExpression searchCondition = CreateSearchCondition(propertyExpression: propertyAccess,
                searchConstant: searchConstant,
                searchParams: searchParams);

            combinedExpression = combinedExpression == null
                ? searchCondition
                : Expression.OrElse(left: combinedExpression,
                    right: searchCondition);
        }

        if (combinedExpression != null)
        {
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(body: combinedExpression,
                parameters: param);
            query = query.Where(predicate: lambda);
        }

        return query;
    }

    private static IQueryable<T> ApplyFullTextSearch<T>(this IQueryable<T> query, string searchTerm, ISearchParams searchParams)
    {
        PropertyInfo[] stringProperties = GetStringProperties<T>();
        if (stringProperties.Length == 0)
            return query;

        ParameterExpression param = Expression.Parameter(type: typeof(T),
            name: "x");
        ConstantExpression searchConstant = Expression.Constant(value: searchTerm);
        Expression? combinedExpression = null;

        foreach (PropertyInfo property in stringProperties)
        {
            MemberExpression propertyAccess = Expression.Property(expression: param,
                property: property);
            BinaryExpression searchCondition = CreateSearchCondition(propertyExpression: propertyAccess,
                searchConstant: searchConstant,
                searchParams: searchParams);

            combinedExpression = combinedExpression == null
                ? searchCondition
                : Expression.OrElse(left: combinedExpression,
                    right: searchCondition);
        }

        if (combinedExpression != null)
        {
            Expression<Func<T, bool>> lambda = Expression.Lambda<Func<T, bool>>(body: combinedExpression,
                parameters: param);
            query = query.Where(predicate: lambda);
        }

        return query;
    }

    /// <summary>
    /// Creates a mapping of field names to properties, supporting multiple naming conventions.
    /// </summary>
    private static Dictionary<string, PropertyInfo> GetPropertyMapping<T>()
    {
        string cacheKey = typeof(T).FullName ?? typeof(T).Name;

        return PropertyMappingCache.GetOrAdd(key: cacheKey,
            valueFactory: _ =>
            {
                Dictionary<string, PropertyInfo> mapping = new(comparer: StringComparer.OrdinalIgnoreCase);
                PropertyInfo[] properties = typeof(T).GetProperties()
                    .Where(predicate: p => p.PropertyType == typeof(string) && p.CanRead)
                    .ToArray();

                foreach (PropertyInfo property in properties)
                {
                    string propertyName = property.Name;

                    // Add original property name
                    mapping[key: propertyName] = property;

                    // Add lowercase version
                    mapping[key: propertyName.ToLower()] = property;

                    // Add snake_case version
                    string snakeCaseName = ToSnakeCase(input: propertyName);
                    mapping[key: snakeCaseName] = property;

                    // Add kebab-case version
                    string kebabCaseName = ToKebabCase(input: propertyName);
                    mapping[key: kebabCaseName] = property;
                }

                return mapping;
            });
    }

    /// <summary>
    /// Finds a property using flexible field name matching.
    /// </summary>
    private static PropertyInfo? FindProperty(Dictionary<string, PropertyInfo> propertyMapping, string fieldName)
    {
        if (propertyMapping.TryGetValue(key: fieldName,
                value: out PropertyInfo? property))
        {
            return property;
        }

        // Try with normalized field name (remove underscores, hyphens, make lowercase)
        string normalizedFieldName = fieldName.Replace(oldValue: "_",
            newValue: "").Replace(oldValue: "-",
            newValue: "").ToLower();

        foreach (KeyValuePair<string, PropertyInfo> kvp in propertyMapping)
        {
            string normalizedMappingKey = kvp.Key.Replace(oldValue: "_",
                newValue: "").Replace(oldValue: "-",
                newValue: "").ToLower();
            if (normalizedMappingKey == normalizedFieldName)
            {
                return kvp.Value;
            }
        }

        return null;
    }

    /// <summary>
    /// Converts PascalCase/camelCase to snake_case.
    /// </summary>
    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(value: input))
            return input;

        StringBuilder result = new();
        result.Append(value: char.ToLower(c: input[index: 0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[index: i];
            if (char.IsUpper(c: c))
            {
                result.Append(value: '_');
                result.Append(value: char.ToLower(c: c));
            }
            else
            {
                result.Append(value: c);
            }
        }

        return result.ToString();
    }

    /// <summary>
    /// Converts PascalCase/camelCase to kebab-case.
    /// </summary>
    private static string ToKebabCase(string input)
    {
        if (string.IsNullOrEmpty(value: input))
            return input;

        StringBuilder result = new();
        result.Append(value: char.ToLower(c: input[index: 0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[index: i];
            if (char.IsUpper(c: c))
            {
                result.Append(value: '-');
                result.Append(value: char.ToLower(c: c));
            }
            else
            {
                result.Append(value: c);
            }
        }

        return result.ToString();
    }

    private static BinaryExpression CreateSearchCondition(Expression propertyExpression, ConstantExpression searchConstant, ISearchParams? searchParams = null)
    {
        searchParams ??= new SearchParams();

        // Null check
        BinaryExpression nullCheck = Expression.NotEqual(left: propertyExpression,
            right: Expression.Constant(value: null,
                type: typeof(string)));

        Expression searchExpression;

        if (searchParams.CaseSensitive.HasValue && searchParams.CaseSensitive.Value)
        {
            searchExpression = propertyExpression;
        }
        else
        {
            MethodInfo? toLowerMethod = typeof(string).GetMethod(name: "ToLower",
                types: Type.EmptyTypes);
            searchExpression = Expression.Call(instance: propertyExpression,
                method: toLowerMethod!);
        }

        // Choose search method
        Expression searchCall;
        if (searchParams.ExactMatch.HasValue && searchParams.ExactMatch.Value)
        {
            MethodInfo? equalsMethod = typeof(string).GetMethod(name: "Equals",
                types:
                [
                    typeof(string)
                ]);
            searchCall = Expression.Call(instance: searchExpression,
                method: equalsMethod!,
                arguments: searchConstant);
        }
        else if (searchParams.StartsWith.HasValue && searchParams.StartsWith.Value)
        {
            MethodInfo? startsWithMethod = typeof(string).GetMethod(name: "StartsWith",
                types:
                [
                    typeof(string)
                ]);
            searchCall = Expression.Call(instance: searchExpression,
                method: startsWithMethod!,
                arguments: searchConstant);
        }
        else
        {
            MethodInfo? containsMethod = typeof(string).GetMethod(name: "Contains",
                types:
                [
                    typeof(string)
                ]);
            searchCall = Expression.Call(instance: searchExpression,
                method: containsMethod!,
                arguments: searchConstant);
        }

        return Expression.AndAlso(left: nullCheck,
            right: searchCall);
    }

    private static BinaryExpression CreateSearchCondition(Expression propertyExpression, ConstantExpression searchConstant)
    {
        // Null check
        BinaryExpression nullCheck = Expression.NotEqual(left: propertyExpression,
            right: Expression.Constant(value: null,
                type: typeof(string)));

        MethodInfo? toLowerMethod = typeof(string).GetMethod(name: "ToLower",
            types: Type.EmptyTypes);
        Expression searchExpression = Expression.Call(instance: propertyExpression,
            method: toLowerMethod!);

        MethodInfo? containsMethod = typeof(string).GetMethod(name: "Contains",
            types:
            [
                typeof(string)
            ]);
        Expression searchCall = Expression.Call(instance: searchExpression,
            method: containsMethod!,
            arguments: searchConstant);

        return Expression.AndAlso(left: nullCheck,
            right: searchCall);
    }

    private static PropertyInfo[] GetStringProperties<T>()
    {
        return StringPropertiesCache.GetOrAdd(key: typeof(T),
            valueFactory: type =>
                type.GetProperties()
                    .Where(predicate: p => p.PropertyType == typeof(string) && p.CanRead)
                    .ToArray());
    }

    private static Expression ReplaceParameter(Expression expression, ParameterExpression oldParameter, ParameterExpression newParameter)
    {
        return new ParameterReplacer(oldParameter: oldParameter,
            newParameter: newParameter).Visit(node: expression);
    }

    private class ParameterReplacer(
        ParameterExpression oldParameter,
        ParameterExpression newParameter)
        : ExpressionVisitor
    {
        protected override Expression VisitParameter(ParameterExpression node)
        {
            return node == oldParameter ? newParameter : base.VisitParameter(node: node);
        }
    }
}