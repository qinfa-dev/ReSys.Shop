using ReSys.Core.Common.Models.Filter;
using ReSys.Core.Domain.Catalog.Products;
using ReSys.Core.Domain.Catalog.Taxonomies.Rules;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

/// <summary>
/// Provides extension methods for <see cref="TaxonRule"/> entities to facilitate their integration
/// with query filtering mechanisms, primarily for dynamically building queries based on taxon rules.
/// </summary>
/// <remarks>
/// These extension methods bridge the declarative <see cref="TaxonRule"/> definitions with
/// the programmatic requirements of filtering product collections based on those rules.
/// They help in translating rule types and match policies into executable filter logic.
/// </remarks>
public static class TaxonRuleExtensions
{
    /// <summary>
    /// Maps the <see cref="TaxonRule.Type"/> to the corresponding property field name of a <see cref="Product"/> entity.
    /// This allows for dynamic construction of filter expressions.
    /// </summary>
    /// <param name="rule">The <see cref="TaxonRule"/> instance.</param>
    /// <returns>A string representing the name of the product field to filter by.</returns>
    /// <exception cref="NotSupportedException">Thrown if the <see cref="TaxonRule.Type"/> is not supported for field name mapping.</exception>
    /// <remarks>
    /// This method translates a high-level rule type (e.g., "product_name") into a code-level property name (e.g., <c>nameof(Product.Name)</c>).
    /// For "product_property" rules, it constructs a nested path including the <see cref="TaxonRule.PropertyName"/>.
    /// Special handling might be required at the application layer for rules involving collections (e.g., variant rules, classification rules).
    /// <para>
    /// <strong>Example Mappings:</strong>
    /// <list type="bullet">
    /// <item><term>"product_name"</term><description><c>nameof(Product.Name)</c></description></item>
    /// <item><term>"product_property"</term><description><c>"Properties.{rule.PropertyName}"</c></description></item>
    /// <item><term>"variant_price"</term><description><c>"Variants.Price"</c> (Requires complex query logic)</description></item>
    /// </list>
    /// </para>
    /// </remarks>
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
    /// Maps the <see cref="TaxonRule.MatchPolicy"/> to the corresponding <see cref="FilterOperator"/> enum.
    /// This translates the declarative match policy into an executable filter operation for query builders.
    /// </summary>
    /// <param name="rule">The <see cref="TaxonRule"/> instance.</param>
    /// <returns>A <see cref="FilterOperator"/> enum value representing the match policy.</returns>
    /// <exception cref="NotSupportedException">Thrown if the <see cref="TaxonRule.MatchPolicy"/> is not supported for operator mapping.</exception>
    /// <remarks>
    /// This method is crucial for converting rule definitions into filter criteria used by a generic filtering mechanism.
    /// It covers a wide range of comparison operators for various data types.
    /// <para>
    /// <strong>Example Mappings:</strong>
    /// <list type="bullet">
    /// <item><term>"is_equal_to"</term><description><see cref="FilterOperator.Equal"/></description></item>
    /// <item><term>"contains"</term><description><see cref="FilterOperator.Contains"/></description></item>
    /// <item><term>"greater_than"</term><description><see cref="FilterOperator.GreaterThan"/></description></item>
    /// </list>
    /// </para>
    /// </remarks>
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
    /// Determines if a <see cref="TaxonRule"/> can be directly converted into a simple query filter
    /// that operates on properties of the <see cref="Product"/> aggregate.
    /// </summary>
    /// <param name="rule">The <see cref="TaxonRule"/> instance.</param>
    /// <returns>True if the rule can be converted to a simple query filter, false otherwise.</returns>
    /// <remarks>
    /// Rules that return <c>false</c> from this method (e.g., "variant_price", "variant_sku", "classification_taxon")
    /// require more complex handling. This typically involves:
    /// <list type="bullet">
    /// <item><description>Joining across collections (e.g., <c>Product.Variants</c>, <c>Product.Classifications</c>).</description></item>
    /// <item><description>Performing subqueries or using advanced LINQ expressions.</description></item>
    /// </list>
    /// Such complex rules often need custom logic within the application service layer when building queries
    /// to retrieve products for an automatic taxon.
    /// </remarks>
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