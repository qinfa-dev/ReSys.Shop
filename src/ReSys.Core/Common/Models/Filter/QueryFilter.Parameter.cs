namespace ReSys.Core.Common.Models.Filter;

/// <summary>
/// Represents a filter parameter parsed from HTTP query parameters with enhanced logical operations support.
/// </summary>
public sealed class QueryFilterParameter
{
    /// <summary>
    /// The field name to filter on (supports dot notation for nested properties).
    /// </summary>
    public required string Field { get; set; }

    /// <summary>
    /// The filter operator to apply.
    /// </summary>
    public FilterOperator Operator { get; set; }

    /// <summary>
    /// The value to filter by as a string (will be converted to appropriate type).
    /// </summary>
    public required string Value { get; set; }

    /// <summary>
    /// For IN operations, comma-separated values can be provided in Value property.
    /// </summary>
    public string? Values { get; set; }

    /// <summary>
    /// The logical operator for chaining this filter with others.
    /// </summary>
    public FilterLogicalOperator LogicalOperator { get; set; } = FilterLogicalOperator.All;

    /// <summary>
    /// Indicates whether the logical operator was explicitly set (via prefix) or defaulted.
    /// </summary>
    public bool IsLogicalOperatorExplicit { get; set; } = false;

    /// <summary>
    /// Optional group identifier for grouping conditions.
    /// </summary>
    public int? Group { get; set; }

    /// <summary>
    /// Validates that the filter parameter is properly configured.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when the parameter is invalid.</exception>
    public void Validate()
    {
        if (string.IsNullOrWhiteSpace(value: Field))
        {
            throw new ArgumentException(message: "Field cannot be null or whitespace.",
                paramName: nameof(Field));
        }

        // Validate that null check operators don't require values
        if (Operator is FilterOperator.IsNull or FilterOperator.IsNotNull)
        {
            // For null checks, reject non-empty values since they shouldn't have values
            if (!string.IsNullOrWhiteSpace(value: Value))
            {
                throw new ArgumentException(message: $"Operator {Operator} should not have a value.",
                    paramName: nameof(Value));
            }
            return; // Value is not required for null checks
        }

        // Validate IN operations have proper format
        if (Operator is FilterOperator.In or FilterOperator.NotIn)
        {
            if (string.IsNullOrWhiteSpace(value: Value) && string.IsNullOrWhiteSpace(value: Values))
            {
                throw new ArgumentException(
                    message: $"Operator {Operator} requires values in Value or Values property.",
                    paramName: nameof(Value));
            }
            return; // Values are provided, validation passes
        }

        // Validate Range operations have proper format
        if (Operator == FilterOperator.Range)
        {
            if (string.IsNullOrWhiteSpace(value: Value) || !Value.Contains(value: ','))
            {
                throw new ArgumentException(message: "Range operator requires two comma-separated values.",
                    paramName: nameof(Value));
            }
            return; // Range validation passes
        }

        // Validate that other operators have values
        if (string.IsNullOrWhiteSpace(value: Value))
        {
            throw new ArgumentException(message: $"Operator {Operator} requires a value.",
                paramName: nameof(Value));
        }
    }
}
