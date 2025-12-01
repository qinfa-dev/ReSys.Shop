using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Taxonomies.Rules;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

/// <summary>
/// Extension methods for TaxonRule to support query filter integration.
/// </summary>
public static class TaxonRuleExtensions
{
    /// <summary>
    /// Maps the TaxonRule type to the corresponding Product property field name.
    /// </summary>
    public static string GetFieldName(this TaxonRule rule)
    {
        return rule.Type switch
        {
            "product_name" => nameof(Product.Name),
            "product_sku" => "Sku",
            "product_description" => "Description",
            "product_price" => "Price",
            "product_weight" => "Weight",
            "product_available" => "Available",
            "product_archived" => "Archived",
            "product_property" => $"Properties.{rule.PropertyName}", // Nested property access
            "variant_price" => "Variants.Price", // This might need special handling
            "variant_sku" => "Variants.Sku", // This might need special handling
            "classification_taxon" => "Classifications.TaxonId", // This might need special handling
            _ => throw new NotSupportedException(message: $"TaxonRule type '{rule.Type}' is not supported.")
        };
    }

    /// <summary>
    /// Maps the TaxonRule match policy to the corresponding FilterOperator.
    /// </summary>
    public static FilterOperator GetFilterOperator(this TaxonRule rule)
    {
        return rule.MatchPolicy switch
        {
            "is_equal_to" => FilterOperator.Equal,
            "is_not_equal_to" => FilterOperator.NotEqual,
            "contains" => FilterOperator.Contains,
            "does_not_contain" => FilterOperator.NotContains,
            "starts_with" => FilterOperator.StartsWith,
            "ends_with" => FilterOperator.EndsWith,
            "greater_than" => FilterOperator.GreaterThan,
            "less_than" => FilterOperator.LessThan,
            "greater_than_or_equal" => FilterOperator.GreaterThanOrEqual,
            "less_than_or_equal" => FilterOperator.LessThanOrEqual,
            "in" => FilterOperator.In,
            "not_in" => FilterOperator.NotIn,
            "is_null" => FilterOperator.IsNull,
            "is_not_null" => FilterOperator.IsNotNull,
            _ => throw new NotSupportedException(message: $"Match policy '{rule.MatchPolicy}' is not supported.")
        };
    }

    /// <summary>
    /// Validates if the rule can be converted to a query filter.
    /// Some rule types might require special handling (e.g., variant rules, classification rules).
    /// </summary>
    public static bool CanConvertToQueryFilter(this TaxonRule rule)
    {
        return rule.Type switch
        {
            "product_name" => true,
            "product_sku" => true,
            "product_description" => true,
            "product_price" => true,
            "product_weight" => true,
            "product_available" => true,
            "product_archived" => true,
            "product_property" => !string.IsNullOrWhiteSpace(value: rule.PropertyName),
            "variant_price" => false, // Requires special collection handling
            "variant_sku" => false, // Requires special collection handling
            "classification_taxon" => false, // Requires special collection handling
            _ => false
        };
    }
}