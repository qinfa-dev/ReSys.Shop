using System.Collections.Concurrent;
using System.Linq.Expressions;
using System.Reflection;

namespace ReSys.Core.Common.Models.Sort;

public static class SortParamExtensions
{
    private static readonly ConcurrentDictionary<string, PropertyInfo?> PropertyCache = new();
    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();

    /// <summary>
    /// Applies sorting based on a single ISortParam instance.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, ISortParam? sortParams = null)
    {
        if (sortParams == null || string.IsNullOrWhiteSpace(value: sortParams.SortBy))
            return query;

        return query.ApplySingleSort(sortBy: sortParams.SortBy!,
            sortOrder: sortParams.SortOrder);
    }

    /// <summary>
    /// Applies multiple sorting parameters sequentially.
    /// </summary>
    public static IQueryable<T> ApplySort<T>(this IQueryable<T> query, params ISortParam[]? sortParams)
    {
        if (sortParams == null || sortParams.Length == 0)
            return query;

        IOrderedQueryable<T>? orderedQuery = null;

        foreach (ISortParam sort in sortParams.Where(s => !string.IsNullOrWhiteSpace(s.SortBy)))
        {
            PropertyInfo? propertyInfo = GetPropertyInfo<T>(sort.SortBy!);

            if (propertyInfo == null)
            {
                // Skip invalid property
                continue;
            }

            bool descending = IsDescending(sort.SortOrder);

            if (orderedQuery == null)
            {
                // First valid sort
                orderedQuery = query.ApplyOrderBy(propertyInfo, descending);
            }
            else
            {
                // Subsequent valid sorts
                orderedQuery = orderedQuery.ApplyThenByInternal(propertyInfo, descending);
            }
        }

        return orderedQuery ?? query;
    }

    /// <summary>
    /// Fluent sorting builder for custom pipelines.
    /// </summary>
    public static SortBuilder<T> Sort<T>(this IQueryable<T> query) => new(query: query);

    /// <summary>
    /// Type-safe sorting with lambda expressions.
    /// </summary>
    public static IOrderedQueryable<T> OrderBy<T, TKey>(
        this IQueryable<T> query,
        Expression<Func<T, TKey>> keySelector,
        bool descending = false)
    {
        return descending
            ? query.OrderByDescending(keySelector: keySelector)
            : query.OrderBy(keySelector: keySelector);
    }

    // ----------------------------
    // Internal helpers
    // ----------------------------

    private static IQueryable<T> ApplySingleSort<T>(this IQueryable<T> query, string sortBy, string? sortOrder)
    {
        PropertyInfo? propertyInfo = GetPropertyInfo<T>(sortBy: sortBy);
        if (propertyInfo == null)
            return query;

        bool descending = IsDescending(sortOrder: sortOrder);
        return query.ApplyOrderBy(propertyInfo: propertyInfo,
            descending: descending);
    }

    private static IOrderedQueryable<T> ApplyThenBy<T>(
        this IOrderedQueryable<T> query,
        string sortBy,
        string? sortOrder)
    {
        PropertyInfo? propertyInfo = GetPropertyInfo<T>(sortBy: sortBy);
        if (propertyInfo == null)
            return query;

        bool descending = IsDescending(sortOrder: sortOrder);
        return query.ApplyThenByInternal(propertyInfo: propertyInfo,
            descending: descending);
    }

    private static IOrderedQueryable<T> ApplyOrderBy<T>(
        this IQueryable<T> query,
        PropertyInfo propertyInfo,
        bool descending)
    {
        MethodInfo method = GetOrCreateMethod<T>(propertyInfo: propertyInfo,
            methodName: descending
                ? "OrderByDescending"
                : "OrderBy");

        ParameterExpression parameter = Expression.Parameter(type: typeof(T),
            name: "x");
        MemberExpression property = Expression.Property(expression: parameter,
            property: propertyInfo);
        LambdaExpression lambda = Expression.Lambda(body: property,
            parameters: parameter);

        return (IOrderedQueryable<T>)method.Invoke(obj: null,
        parameters:
        [
            query,
            lambda
        ])!;
    }

    private static IOrderedQueryable<T> ApplyThenByInternal<T>(
        this IOrderedQueryable<T> query,
        PropertyInfo propertyInfo,
        bool descending)
    {
        MethodInfo method = GetOrCreateMethod<T>(propertyInfo: propertyInfo,
            methodName: descending
                ? "ThenByDescending"
                : "ThenBy");

        ParameterExpression parameter = Expression.Parameter(type: typeof(T),
            name: "x");
        MemberExpression property = Expression.Property(expression: parameter,
            property: propertyInfo);
        LambdaExpression lambda = Expression.Lambda(body: property,
            parameters: parameter);

        return (IOrderedQueryable<T>)(method.Invoke(obj: null,
        parameters:
        [
            query,
            lambda
        ]) ?? query);
    }

    // ----------------------------
    // Utility helpers
    // ----------------------------

    private static PropertyInfo? GetPropertyInfo<T>(string sortBy)
    {
        string cacheKey = $"{typeof(T).FullName}.{sortBy}";
        return PropertyCache.GetOrAdd(key: cacheKey,
            valueFactory: _ =>
                typeof(T).GetProperty(name: sortBy,
                    bindingAttr: BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase));
    }

    private static MethodInfo GetOrCreateMethod<T>(PropertyInfo propertyInfo, string methodName)
    {
        string cacheKey = $"{typeof(T).FullName}.{propertyInfo.Name}.{methodName}";

        return MethodCache.GetOrAdd(key: cacheKey,
            valueFactory: _ =>
            {
                return typeof(Queryable).GetMethods()
                    .First(predicate: m => m.Name == methodName &&
                                           m.GetParameters()
                                               .Length ==
                                           2 &&
                                           m.GetParameters()[1].ParameterType.IsGenericType)
                    .MakeGenericMethod(typeArguments: [typeof(T), propertyInfo.PropertyType]);
            });
    }

    private static bool IsDescending(string? sortOrder) =>
        string.Equals(a: sortOrder,
            b: "desc",
            comparisonType: StringComparison.OrdinalIgnoreCase);
}
