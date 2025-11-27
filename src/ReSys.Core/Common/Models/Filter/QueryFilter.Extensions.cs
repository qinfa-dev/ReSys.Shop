using System.Collections.Concurrent;
using System.Collections.Specialized;
using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Web;

namespace ReSys.Core.Common.Models.Filter;

/// <summary>
/// Provides extension methods for applying query filters to IQueryable with enhanced query parameter support.
/// </summary>
public static class QueryFilterExtensions
{
    // Cache: Reflection lookups to improve performance
    private static readonly ConcurrentDictionary<string, MethodInfo> MethodCache = new();
    private static readonly ConcurrentDictionary<string, Dictionary<string, PropertyInfo>> PropertyMappingCache = new();

    // Cached method references
    private static readonly MethodInfo StringContainsMethod = GetCachedMethod(type: typeof(string), methodName: "Contains", parameterTypes: [typeof(string), typeof(StringComparison)]);
    private static readonly MethodInfo StringStartsWithMethod = GetCachedMethod(type: typeof(string), methodName: "StartsWith", parameterTypes: [typeof(string), typeof(StringComparison)]);
    private static readonly MethodInfo StringEndsWithMethod = GetCachedMethod(type: typeof(string), methodName: "EndsWith", parameterTypes: [typeof(string), typeof(StringComparison)]);

    /// <summary>
    /// Operator mapping for query parameter parsing.
    /// </summary>
    private static readonly Dictionary<string, FilterOperator> OperatorMap = new(comparer: StringComparer.OrdinalIgnoreCase)
    {
        [key: "eq"] = FilterOperator.Equal,
        [key: "ne"] = FilterOperator.NotEqual,
        [key: "gt"] = FilterOperator.GreaterThan,
        [key: "gte"] = FilterOperator.GreaterThanOrEqual,
        [key: "lt"] = FilterOperator.LessThan,
        [key: "lte"] = FilterOperator.LessThanOrEqual,
        [key: "contains"] = FilterOperator.Contains,
        [key: "notcontains"] = FilterOperator.NotContains,
        [key: "startswith"] = FilterOperator.StartsWith,
        [key: "endswith"] = FilterOperator.EndsWith,
        [key: "in"] = FilterOperator.In,
        [key: "notin"] = FilterOperator.NotIn,
        [key: "isnull"] = FilterOperator.IsNull,
        [key: "isnotnull"] = FilterOperator.IsNotNull,
        [key: "range"] = FilterOperator.Range
    };

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, Dictionary<string, string> queryParams)
    {
        ArgumentNullException.ThrowIfNull(argument: query);
        ArgumentNullException.ThrowIfNull(argument: queryParams);

        if (queryParams.Count == 0)
            return query;

        List<QueryFilterParameter> filters = ParseQueryParameters(queryParams: queryParams);
        return query.ApplyFilters(filters: filters);
    }

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, IQueryFilterParams filterParams)
    {
        ArgumentNullException.ThrowIfNull(argument: query);
        ArgumentNullException.ThrowIfNull(argument: filterParams);

        if (string.IsNullOrWhiteSpace(value: filterParams.Filters))
            return query;

        return query.ApplyFilters(queryParams: ParseQueryString(queryString: filterParams.Filters));
    }

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, string queryString)
    {
        ArgumentNullException.ThrowIfNull(argument: query);

        if (string.IsNullOrWhiteSpace(value: queryString))
            return query;

        return query.ApplyFilters(queryParams: ParseQueryString(queryString: queryString));
    }

    public static IQueryable<T> ApplyFilters<T>(this IQueryable<T> query, List<QueryFilterParameter> filters)
    {
        ArgumentNullException.ThrowIfNull(argument: query);

        if (filters is null || filters.Count == 0)
            return query;

        try
        {
            QueryFilterGroup filterGroups = BuildFilterGroups(filters: filters);
            Expression<Func<T, bool>>? expression = BuildGroupExpression<T>(group: filterGroups);

            return expression != null ? query.Where(predicate: expression) : query;
        }
        catch (ArgumentException ex)
        {
            throw new InvalidOperationException(message: "Invalid filter criteria.", innerException: ex);
        }
        catch (NotSupportedException ex)
        {
            throw new InvalidOperationException(message: "An unsupported filter operation was requested.", innerException: ex);
        }
    }

    public static List<QueryFilterParameter> ParseQueryParameters(Dictionary<string, string> queryParams)
    {
        if (queryParams is null || queryParams.Count == 0)
            return [];

        List<QueryFilterParameter> filters = new(capacity: queryParams.Count);
        FilterLogicalOperator globalLogic = FilterLogicalOperator.All;

        if (queryParams.TryGetValue(key: "logic", value: out string? logicValue) &&
            logicValue.Equals(value: "or", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            globalLogic = FilterLogicalOperator.Any;
        }

        int currentGroup = 0;

        foreach (KeyValuePair<string, string> param in queryParams)
        {
            if (string.IsNullOrWhiteSpace(value: param.Key))
                continue;

            if (param.Key.StartsWith(value: "group", comparisonType: StringComparison.OrdinalIgnoreCase) &&
                param.Key.Length > 5 &&
                int.TryParse(s: param.Value, result: out int groupId))
            {
                currentGroup = groupId;
                continue;
            }

            if (param.Key.Equals(value: "logic", comparisonType: StringComparison.OrdinalIgnoreCase))
                continue;

            bool isNullCheck = param.Key.Contains(value: "[isnull]", comparisonType: StringComparison.OrdinalIgnoreCase) ||
                             param.Key.Contains(value: "[isnotnull]", comparisonType: StringComparison.OrdinalIgnoreCase);

            if (string.IsNullOrWhiteSpace(value: param.Value) && !isNullCheck)
                continue;

            QueryFilterParameter? filter = ParseSingleQueryParameter(key: param.Key, value: param.Value, globalLogic: globalLogic, currentGroup: currentGroup);
            if (filter != null)
            {
                filters.Add(item: filter);
            }
        }

        return filters;
    }

    private static QueryFilterParameter? ParseSingleQueryParameter(string key, string value,
        FilterLogicalOperator globalLogic, int currentGroup)
    {
        if (string.IsNullOrWhiteSpace(value: key))
            return null;

        FilterLogicalOperator logicalOp = globalLogic;
        bool isLogicalOpExplicit = false;

        if (key.StartsWith(value: "or_", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            logicalOp = FilterLogicalOperator.Any;
            isLogicalOpExplicit = true;
            key = key[3..];
        }
        else if (key.StartsWith(value: "and_", comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            logicalOp = FilterLogicalOperator.All;
            isLogicalOpExplicit = true;
            key = key[4..];
        }

        int bracketStart = key.IndexOf(value: '[');
        if (bracketStart > 0)
        {
            int bracketEnd = key.IndexOf(value: ']', startIndex: bracketStart);
            if (bracketEnd > bracketStart)
            {
                string fieldName = key[..bracketStart];
                string operatorStr = key[(bracketStart + 1)..bracketEnd];

                if (OperatorMap.TryGetValue(key: operatorStr, value: out FilterOperator filterOperator))
                {
                    return new QueryFilterParameter
                    {
                        Field = fieldName,
                        Operator = filterOperator,
                        Value = value,
                        LogicalOperator = logicalOp,
                        IsLogicalOperatorExplicit = isLogicalOpExplicit,
                        Group = currentGroup
                    };
                }
            }
        }
        else
        {
            int lastUnderscore = key.LastIndexOf(value: '_');
            if (lastUnderscore > 0)
            {
                string fieldName = key[..lastUnderscore];
                string operatorStr = key[(lastUnderscore + 1)..];

                if (OperatorMap.TryGetValue(key: operatorStr, value: out FilterOperator filterOperator))
                {
                    return new QueryFilterParameter
                    {
                        Field = fieldName,
                        Operator = filterOperator,
                        Value = value,
                        LogicalOperator = logicalOp,
                        IsLogicalOperatorExplicit = isLogicalOpExplicit,
                        Group = currentGroup
                    };
                }
            }
        }

        return null;
    }

    private static QueryFilterGroup BuildFilterGroups(List<QueryFilterParameter> filters)
    {
        QueryFilterGroup rootGroup = new() { GroupId = 0 };
        Dictionary<int, QueryFilterGroup> groups = new() { { 0, rootGroup } };

        foreach (QueryFilterParameter filter in filters)
        {
            int groupId = filter.Group ?? 0;

            if (!groups.TryGetValue(key: groupId, value: out QueryFilterGroup? group))
            {
                group = new QueryFilterGroup { GroupId = groupId };
                groups[key: groupId] = group;

                if (groupId != 0)
                {
                    rootGroup.SubGroups.Add(item: group);
                }
            }

            group.Filters.Add(item: filter);
        }

        return rootGroup;
    }

    private static Expression<Func<T, bool>>? BuildGroupExpression<T>(QueryFilterGroup group)
    {
        ParameterExpression parameter = Expression.Parameter(type: typeof(T), name: "x");
        Expression? expression = BuildGroupExpressionRecursive<T>(parameter: parameter, group: group);

        return expression != null ? Expression.Lambda<Func<T, bool>>(body: expression, parameters: parameter) : null;
    }

    private static Expression? BuildGroupExpressionRecursive<T>(ParameterExpression parameter, QueryFilterGroup group)
    {
        if (group.Filters.Count == 0 && group.SubGroups.Count == 0)
            return null;

        Expression? groupExpression = null;

        bool hasOrFilter = group.Filters.Any(predicate: f => f.LogicalOperator == FilterLogicalOperator.Any);
        bool hasExplicitAndFilter = group.Filters.Any(predicate: f =>
            f.LogicalOperator == FilterLogicalOperator.All && f.IsLogicalOperatorExplicit);

        if (hasOrFilter && group.Filters.Any(predicate: f => f.LogicalOperator == FilterLogicalOperator.All))
        {
            if (!hasExplicitAndFilter)
            {
                foreach (QueryFilterParameter filter in group.Filters)
                {
                    Expression? filterExpression = BuildQueryFilterExpression<T>(parameter: parameter, filter: filter);
                    if (filterExpression != null)
                    {
                        groupExpression = groupExpression == null
                            ? filterExpression
                            : Expression.OrElse(left: groupExpression, right: filterExpression);
                    }
                }
            }
            else
            {
                Expression? orExpression = null;
                Expression? andExpression = null;

                foreach (QueryFilterParameter filter in group.Filters)
                {
                    Expression? filterExpression = BuildQueryFilterExpression<T>(parameter: parameter, filter: filter);
                    if (filterExpression == null)
                        continue;

                    if (filter.LogicalOperator == FilterLogicalOperator.Any)
                    {
                        orExpression = orExpression == null
                            ? filterExpression
                            : Expression.OrElse(left: orExpression, right: filterExpression);
                    }
                    else
                    {
                        andExpression = andExpression == null
                            ? filterExpression
                            : Expression.AndAlso(left: andExpression, right: filterExpression);
                    }
                }

                groupExpression = CombineExpressions(left: orExpression, right: andExpression, combiner: Expression.AndAlso);
            }
        }
        else
        {
            FilterLogicalOperator logicalOp = hasOrFilter ? FilterLogicalOperator.Any : FilterLogicalOperator.All;
            Func<Expression, Expression, BinaryExpression> combiner = logicalOp == FilterLogicalOperator.All
                ? Expression.AndAlso
                : Expression.OrElse;

            foreach (QueryFilterParameter filter in group.Filters)
            {
                Expression? filterExpression = BuildQueryFilterExpression<T>(parameter: parameter, filter: filter);
                if (filterExpression != null)
                {
                    groupExpression = groupExpression == null
                        ? filterExpression
                        : combiner(arg1: groupExpression, arg2: filterExpression);
                }
            }
        }

        foreach (QueryFilterGroup subGroup in group.SubGroups)
        {
            Expression? subExpression = BuildGroupExpressionRecursive<T>(parameter: parameter, group: subGroup);
            if (subExpression != null)
            {
                groupExpression = groupExpression == null
                    ? subExpression
                    : Expression.AndAlso(left: groupExpression, right: subExpression);
            }
        }

        return groupExpression;
    }

    private static Expression? CombineExpressions(Expression? left, Expression? right,
        Func<Expression, Expression, BinaryExpression> combiner)
    {
        if (left != null && right != null)
            return combiner(arg1: left, arg2: right);
        return left ?? right;
    }

    private static Expression? BuildQueryFilterExpression<T>(ParameterExpression parameter, QueryFilterParameter filter)
    {
        try
        {
            var segments = filter.Field.Split(separator: '.');
            Expression currentExpression = parameter;
            Type currentType = typeof(T);

            // Handle IsNull/IsNotNull separately
            if (filter.Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
            {
                return BuildNullCheckChain(parameter: parameter, segments: segments, currentType: currentType, notNull: filter.Operator == FilterOperator.IsNotNull);
            }

            // Build nested property access with null checking
            foreach (var segment in segments)
            {
                var propertyMapping = GetPropertyMappingForType(type: currentType);
                var property = FindSingleProperty(propertyMapping: propertyMapping, fieldName: segment);

                if (property == null)
                    return null; // Invalid property, filter will be ignored

                // Handle null checking for reference types
                if (currentType.IsClass)
                {
                    var nullCheck = Expression.NotEqual(left: currentExpression, right: Expression.Constant(value: null, type: currentType));
                    currentExpression = Expression.Condition(
                        test: nullCheck,
                        ifTrue: Expression.Property(expression: currentExpression, property: property),
                        ifFalse: Expression.Default(type: property.PropertyType)
                    );
                }
                else
                {
                    currentExpression = Expression.Property(expression: currentExpression, property: property);
                }

                currentType = Nullable.GetUnderlyingType(nullableType: property.PropertyType) ?? property.PropertyType;
            }

            // Create the filter criteria
            var criteria = new FilterCriteria
            {
                PropertyName = filter.Field,
                Operator = filter.Operator,
                Value = ConvertQueryValue(value: filter.Value, op: filter.Operator, targetType: currentType)
            };

            return BuildFilterExpression(criterion: criteria, property: currentExpression);
        }
        catch
        {
            return null; // If anything goes wrong, ignore the filter
        }
    }

    private static Expression BuildNullCheckChain(Expression parameter, string[] segments, Type currentType, bool notNull)
    {
        Expression currentExpression = parameter;
        Expression? nullCheckExpression = null;

        for (int i = 0; i < segments.Length; i++)
        {
            var propertyMapping = GetPropertyMappingForType(type: currentType);
            var property = FindSingleProperty(propertyMapping: propertyMapping, fieldName: segments[i]);

            if (property == null)
                return Expression.Constant(value: !notNull);

            var propertyAccess = Expression.Property(expression: currentExpression, property: property);

            if (i == segments.Length - 1)
            {
                // For the last segment, we check if it's null/not null
                var isNull = Expression.Equal(left: propertyAccess, right: Expression.Constant(value: null, type: propertyAccess.Type));
                return notNull ? Expression.Not(expression: isNull) : isNull;
            }

            // For intermediate segments, we need to ensure they're not null
            if (property.PropertyType.IsClass)
            {
                var intermediateNullCheck = Expression.NotEqual(left: propertyAccess, right: Expression.Constant(value: null, type: propertyAccess.Type));
                nullCheckExpression = nullCheckExpression == null
                    ? intermediateNullCheck
                    : Expression.AndAlso(left: nullCheckExpression, right: intermediateNullCheck);
            }

            currentExpression = propertyAccess;
            currentType = Nullable.GetUnderlyingType(nullableType: property.PropertyType) ?? property.PropertyType;
        }

        return nullCheckExpression ?? Expression.Constant(value: !notNull);
    }

    private static Expression BuildFilterExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null && criterion.Operator != FilterOperator.IsNull && criterion.Operator != FilterOperator.IsNotNull)
            return Expression.Constant(value: false);

        try
        {
            return criterion.Operator switch
            {
                FilterOperator.Contains or FilterOperator.NotContains or
                FilterOperator.StartsWith or FilterOperator.EndsWith => BuildStringExpression(criterion: criterion, property: property),
                FilterOperator.In or FilterOperator.NotIn => BuildCollectionExpression(criterion: criterion, property: property),
                FilterOperator.Range => BuildRangeExpression(criterion: criterion, property: property),
                FilterOperator.IsNull => Expression.Equal(left: property, right: Expression.Constant(value: null, type: property.Type)),
                FilterOperator.IsNotNull => Expression.NotEqual(left: property, right: Expression.Constant(value: null, type: property.Type)),
                _ => BuildComparisonExpression(criterion: criterion, property: property)
            };
        }
        catch
        {
            return Expression.Constant(value: true); // For invalid operations, return true to not filter out records
        }
    }

    private static Expression BuildComparisonExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
            return Expression.Constant(value: true);

        Type propertyType = Nullable.GetUnderlyingType(nullableType: property.Type) ?? property.Type;
        object? convertedValue = ConvertValue(value: criterion.Value, targetType: propertyType);

        if (convertedValue == null)
            return Expression.Constant(value: true); // Invalid conversion should not filter out records

        var valueExpression = Expression.Constant(value: convertedValue, type: property.Type);

        try
        {
            return criterion.Operator switch
            {
                FilterOperator.Equal => Expression.Equal(left: property, right: valueExpression),
                FilterOperator.NotEqual => Expression.NotEqual(left: property, right: valueExpression),
                FilterOperator.GreaterThan => Expression.GreaterThan(left: property, right: valueExpression),
                FilterOperator.GreaterThanOrEqual => Expression.GreaterThanOrEqual(left: property, right: valueExpression),
                FilterOperator.LessThan => Expression.LessThan(left: property, right: valueExpression),
                FilterOperator.LessThanOrEqual => Expression.LessThanOrEqual(left: property, right: valueExpression),
                _ => Expression.Constant(value: true)
            };
        }
        catch
        {
            return Expression.Constant(value: true); // Any comparison errors should not filter out records
        }
    }

    private static Expression BuildRangeExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
            return Expression.Constant(value: false);

        string[] parts = criterion.Value.ToString()!
            .Split(separator: ',', options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (parts.Length != 2)
            return Expression.Constant(value: false);

        Type propertyType = Nullable.GetUnderlyingType(nullableType: property.Type) ?? property.Type;
        object? minValue = ConvertValue(value: parts[0], targetType: propertyType);
        object? maxValue = ConvertValue(value: parts[1], targetType: propertyType);

        if (minValue == null || maxValue == null)
            return Expression.Constant(value: false);

        var minExpr = Expression.GreaterThanOrEqual(
            left: property,
            right: Expression.Constant(value: minValue, type: property.Type));
        var maxExpr = Expression.LessThanOrEqual(
            left: property,
            right: Expression.Constant(value: maxValue, type: property.Type));

        return Expression.AndAlso(left: minExpr, right: maxExpr);
    }

    private static Expression BuildCollectionExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
            return Expression.Constant(value: false);

        string[] values = criterion.Value.ToString()!
            .Split(separator: ',', options: StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        if (values.Length == 0)
            return Expression.Constant(value: false);

        Type propertyType = Nullable.GetUnderlyingType(nullableType: property.Type) ?? property.Type;
        var convertedValues = values
            .Select(selector: v => ConvertValue(value: v, targetType: propertyType))
            .Where(predicate: v => v != null)
            .ToList();

        if (convertedValues.Count == 0)
            return Expression.Constant(value: false);

        var array = Array.CreateInstance(elementType: propertyType, length: convertedValues.Count);
        for (int i = 0; i < convertedValues.Count; i++)
        {
            array.SetValue(value: convertedValues[index: i], index: i);
        }

        var containsMethod = typeof(Enumerable)
            .GetMethods()
            .First(predicate: m => m.Name == "Contains" && m.GetParameters().Length == 2)
            .MakeGenericMethod(typeArguments: propertyType);

        var result = Expression.Call(
            method: containsMethod,
            arg0: Expression.Constant(value: array),
            arg1: property);

        return criterion.Operator == FilterOperator.NotIn
            ? Expression.Not(expression: result)
            : result;
    }

    private static Expression BuildStringExpression(FilterCriteria criterion, Expression property)
    {
        if (criterion.Value == null)
            return Expression.Constant(value: false);

        var value = criterion.Value.ToString();
        if (string.IsNullOrEmpty(value: value))
            return Expression.Constant(value: false);

        var stringValue = Expression.Constant(value: value);
        var comparison = Expression.Constant(value: StringComparison.OrdinalIgnoreCase);

        // Add null check for the property
        Expression nullCheck = Expression.NotEqual(left: property, right: Expression.Constant(value: null, type: property.Type));

        Expression stringOperation = criterion.Operator switch
        {
            FilterOperator.Contains => Expression.Call(instance: property, method: StringContainsMethod, arg0: stringValue, arg1: comparison),
            FilterOperator.StartsWith => Expression.Call(instance: property, method: StringStartsWithMethod, arg0: stringValue, arg1: comparison),
            FilterOperator.EndsWith => Expression.Call(instance: property, method: StringEndsWithMethod, arg0: stringValue, arg1: comparison),
            FilterOperator.NotContains => Expression.Not(expression: Expression.Call(instance: property, method: StringContainsMethod, arg0: stringValue, arg1: comparison)),
            _ => throw new NotSupportedException(message: $"String operator {criterion.Operator} is not supported.")
        };

        // Combine null check with string operation
        return Expression.AndAlso(left: nullCheck, right: stringOperation);
    }

    private static Dictionary<string, PropertyInfo> GetPropertyMapping<T>()
    {
        string cacheKey = typeof(T).FullName ?? typeof(T).Name;

        return PropertyMappingCache.GetOrAdd(key: cacheKey, valueFactory: _ =>
        {
            Dictionary<string, PropertyInfo> mapping = new(comparer: StringComparer.OrdinalIgnoreCase);
            IEnumerable<PropertyInfo> properties = typeof(T).GetProperties().Where(predicate: p => p.CanRead);

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                mapping[key: propertyName] = property;
                mapping[key: propertyName.ToLowerInvariant()] = property;
                mapping[key: ToSnakeCase(input: propertyName)] = property;
                mapping[key: ToKebabCase(input: propertyName)] = property;
            }

            return mapping;
        });
    }

    private static PropertyInfo? FindSingleProperty(Dictionary<string, PropertyInfo> propertyMapping, string fieldName)
    {
        if (propertyMapping.TryGetValue(key: fieldName, value: out PropertyInfo? property))
            return property;

        string normalized = fieldName.Replace(oldValue: "_", newValue: "").Replace(oldValue: "-", newValue: "").ToLowerInvariant();

        foreach (KeyValuePair<string, PropertyInfo> kvp in propertyMapping)
        {
            if (kvp.Key.Replace(oldValue: "_", newValue: "").Replace(oldValue: "-", newValue: "").ToLowerInvariant() == normalized)
                return kvp.Value;
        }

        return null;
    }

    private static string ToSnakeCase(string input)
    {
        if (string.IsNullOrEmpty(value: input))
            return input;

        StringBuilder sb = new(capacity: input.Length + 5);
        sb.Append(value: char.ToLowerInvariant(c: input[index: 0]));

        for (int i = 1; i < input.Length; i++)
        {
            char c = input[index: i];
            if (char.IsUpper(c: c))
            {
                sb.Append(value: '_');
                sb.Append(value: char.ToLowerInvariant(c: c));
            }
            else
            {
                sb.Append(value: c);
            }
        }

        return sb.ToString();
    }

    private static string ToKebabCase(string input) =>
        ToSnakeCase(input: input).Replace(oldChar: '_', newChar: '-');

    private static Dictionary<string, PropertyInfo> GetPropertyMappingForType(Type type)
    {
        string cacheKey = type.FullName ?? type.Name;

        return PropertyMappingCache.GetOrAdd(key: cacheKey, valueFactory: _ =>
        {
            Dictionary<string, PropertyInfo> mapping = new(comparer: StringComparer.OrdinalIgnoreCase);
            IEnumerable<PropertyInfo> properties = type.GetProperties().Where(predicate: p => p.CanRead);

            foreach (PropertyInfo property in properties)
            {
                string propertyName = property.Name;
                mapping[key: propertyName] = property;
                mapping[key: propertyName.ToLowerInvariant()] = property;
                mapping[key: ToSnakeCase(input: propertyName)] = property;
                mapping[key: ToKebabCase(input: propertyName)] = property;
            }

            return mapping;
        });
    }

    private static Expression GetPropertyExpression<T>(Expression parameter, string propertyName)
    {
        var segments = propertyName.Split(separator: '.');
        Expression currentExpression = parameter;
        Type currentType = typeof(T);

        for (int i = 0; i < segments.Length; i++)
        {
            var propertyMapping = GetPropertyMappingForType(type: currentType);
            var property = FindSingleProperty(propertyMapping: propertyMapping, fieldName: segments[i]);

            if (property == null)
            {
                string available = string.Join(separator: ", ", values: propertyMapping.Keys.Take(count: 10));
                throw new ArgumentException(
                    message: $"Property '{segments[i]}' not found on type '{currentType.Name}'. Available: {available}...");
            }

            var propertyAccess = Expression.Property(expression: currentExpression, property: property);

            // If this is a reference type and not the final property, we need to handle null
            if (currentType.IsClass && i < segments.Length - 1)
            {
                // Create a null-safe access using a ternary operation
                currentExpression = Expression.Condition(
                    test: Expression.Equal(left: currentExpression, right: Expression.Constant(value: null, type: currentType)),
                    ifTrue: Expression.Default(type: property.PropertyType),
                    ifFalse: propertyAccess
                );
            }
            else
            {
                currentExpression = propertyAccess;
            }

            currentType = Nullable.GetUnderlyingType(nullableType: property.PropertyType) ?? property.PropertyType;
        }

        return currentExpression;
    }

    private static MethodInfo GetCachedMethod(Type type, string methodName, params Type[] parameterTypes)
    {
        string key = $"{type.FullName}.{methodName}({string.Join(separator: ",", values: parameterTypes.Select(selector: t => t.FullName))})";

        return MethodCache.GetOrAdd(key: key, valueFactory: _ =>
        {
            MethodInfo? method = type.GetMethod(name: methodName, types: parameterTypes);
            return method ?? throw new InvalidOperationException(message: $"Method {methodName} not found on type {type.Name}");
        });
    }

    private static Dictionary<string, string> ParseQueryString(string queryString)
    {
        if (string.IsNullOrWhiteSpace(value: queryString))
            return new Dictionary<string, string>(comparer: StringComparer.OrdinalIgnoreCase);

        if (queryString[index: 0] == '?')
            queryString = queryString[1..];

        try
        {
            NameValueCollection collection = HttpUtility.ParseQueryString(query: queryString);
            Dictionary<string, string> result = new(comparer: StringComparer.OrdinalIgnoreCase);

            foreach (string? key in collection.AllKeys)
            {
                if (!string.IsNullOrEmpty(value: key))
                {
                    result[key: key] = collection[name: key] ?? string.Empty;
                }
            }

            return result;
        }
        catch
        {
            return ParseQueryStringManually(queryString: queryString);
        }
    }

    private static Dictionary<string, string> ParseQueryStringManually(string queryString)
    {
        Dictionary<string, string> result = new(comparer: StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrWhiteSpace(value: queryString))
            return result;

        string[] pairs = queryString.Split(separator: '&', options: StringSplitOptions.RemoveEmptyEntries);

        foreach (string pair in pairs)
        {
            string[] keyValue = pair.Split(separator: '=', count: 2);
            if (keyValue.Length >= 1)
            {
                string key = Uri.UnescapeDataString(stringToUnescape: keyValue[0]);
                string value = keyValue.Length == 2 ? Uri.UnescapeDataString(stringToUnescape: keyValue[1]) : string.Empty;

                if (!string.IsNullOrEmpty(value: key))
                {
                    result[key: key] = value;
                }
            }
        }

        return result;
    }
    private static object? ConvertValue(object? value, Type targetType)
    {
        if (value == null)
            return null;

        string? stringValue = value.ToString();
        if (string.IsNullOrEmpty(value: stringValue))
            return null;

        try
        {
            if (targetType == typeof(string)) return stringValue;
            if (targetType == typeof(int)) return int.Parse(s: stringValue, provider: CultureInfo.InvariantCulture);
            if (targetType == typeof(long)) return long.Parse(s: stringValue, provider: CultureInfo.InvariantCulture);
            if (targetType == typeof(decimal)) return decimal.Parse(s: stringValue, provider: CultureInfo.InvariantCulture);
            if (targetType == typeof(double)) return double.Parse(s: stringValue, provider: CultureInfo.InvariantCulture);
            if (targetType == typeof(float)) return float.Parse(s: stringValue, provider: CultureInfo.InvariantCulture);
            if (targetType == typeof(bool)) return bool.Parse(value: stringValue);
            if (targetType == typeof(DateTime)) return DateTime.Parse(s: stringValue, provider: CultureInfo.InvariantCulture);
            if (targetType == typeof(DateTimeOffset)) return DateTimeOffset.Parse(input: stringValue, formatProvider: CultureInfo.InvariantCulture);
            if (targetType == typeof(Guid)) return Guid.Parse(input: stringValue);
            if (targetType.IsEnum) return Enum.Parse(enumType: targetType, value: stringValue, ignoreCase: true);

            return Convert.ChangeType(value: value, conversionType: targetType, provider: CultureInfo.InvariantCulture);
        }
        catch
        {
            return null;
        }
    }
    private static object? ConvertQueryValue(string value, FilterOperator op, Type targetType)
    {
        if (op is FilterOperator.IsNull or FilterOperator.IsNotNull)
            return null;

        if (op is FilterOperator.Range or FilterOperator.In or FilterOperator.NotIn)
            return value;

        return string.IsNullOrEmpty(value: value) ? null : ConvertValue(value: value, targetType: targetType);
    }
}