using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Rules;

/// <summary>
/// Represents a rule used for automatically classifying products into a specific <see cref="Taxon"/>.
/// These rules define criteria based on product attributes (e.g., name, price, properties)
/// to dynamically assign products to categories without manual intervention.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Taxonomy Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Dynamic Categorization</term>
/// <description>Automates the process of adding products to <see cref="Taxon"/>s based on defined criteria.</description>
/// </item>
/// <item>
/// <term>Flexible Criteria</term>
/// <description>Supports various rule types and matching policies for versatile classification.</description>
/// </item>
/// <item>
/// <term>Maintainability</term>
/// <description>Reduces manual effort for product classification in large catalogs.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes:</strong>
/// <list type="bullet">
/// <item>
/// <term>TaxonId</term>
/// <description>The unique identifier of the <see cref="Taxon"/> to which products are classified by this rule.</description>
/// </item>
/// <item>
/// <term>Type</term>
/// <description>The kind of rule (e.g., "product_name", "product_price", "product_property").</description>
/// </item>
/// <item>
/// <term>Value</term>
/// <description>The value or pattern to match against (e.g., "T-Shirt", "100").</description>
/// </item>
/// <item>
/// <term>MatchPolicy</term>
/// <description>How the value should be matched (e.g., "contains", "greater_than", "is_equal_to").</description>
/// </item>
/// <item>
/// <term>PropertyName</term>
/// <description>For "product_property" rules, specifies which product property to evaluate.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class TaxonRule : AuditableEntity
{
    #region Constraints
    /// <summary>
    /// Defines constraints and constant values specific to <see cref="TaxonRule"/> operations and properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Maximum length for the rule's <see cref="Type"/> string.</summary>
        public const int TypeMaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        /// <summary>Maximum length for the rule's <see cref="Value"/> string.</summary>
        public const int ValueMaxLength = CommonInput.Constraints.Text.MediumTextMaxLength;
        /// <summary>Maximum length for the <see cref="PropertyName"/> field when the rule type is 'product_property'.</summary>
        public const int PropertyNameMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;

        /// <summary>
        /// An array of valid match policies that define how the rule's <see cref="Value"/> should be compared.
        /// </summary>
        public static readonly string[] MatchPolicies =
        [
            "is_equal_to", "is_not_equal_to", "contains", "does_not_contain",
            "starts_with", "ends_with", "greater_than", "less_than",
            "greater_than_or_equal", "less_than_or_equal", "in", "not_in",
            "is_null", "is_not_null"
        ];

        /// <summary>
        /// An array of valid rule types that define which product attribute the rule applies to.
        /// </summary>
        public static readonly string[] RuleTypes =
        [
            "product_name", "product_sku", "product_description", "product_price",
            "product_weight", "product_available", "product_archived",
            "product_property", "variant_price", "variant_sku", "classification_taxon"
        ];
    }
    #endregion

    #region Errors (Public)
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="TaxonRule"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>Error indicating that at least one taxon rule is required for an automatic taxon.</summary>
        public static Error Required => Error.Validation(
            code: "TaxonRule.Required",
            description: "At least one taxon rule is required.");

        /// <summary>Error indicating that a rule belongs to a different taxon than expected.</summary>
        /// <param name="id">The ID of the <see cref="TaxonRule"/>.</param>
        /// <param name="taxonId">The ID of the <see cref="Taxon"/> to which the rule is supposed to belong.</param>
        public static Error TaxonMismatch(Guid id, Guid taxonId) => Error.Validation(
            code: "TaxonRule.TaxonMismatch",
            description: $"Rule belongs to taxon '{id}', but current taxon is '{taxonId}'.");

        /// <summary>Error indicating that a rule with the same type, value, and match policy already exists.</summary>
        public static Error Duplicate => Error.Conflict(
            code: "TaxonRule.Duplicate",
            description: "A rule with the same type, value, and match policy already exists.");

        /// <summary>Error indicating that a requested <see cref="TaxonRule"/> could not be found.</summary>
        /// <param name="id">The unique identifier of the <see cref="TaxonRule"/> that was not found.</param>
        public static Error NotFound(Guid id) =>
            Error.NotFound(code: "TaxonRule.NotFound", description: $"TaxonRule with ID '{id}' was not found.");

        /// <summary>Error indicating that the provided rule type is invalid.</summary>
        public static Error InvalidType =>
            Error.Validation(
                code: "TaxonRule.InvalidType",
                description: $"Rule type must be one of: {string.Join(separator: ", ", value: Constraints.RuleTypes)}");

        /// <summary>Error indicating that the provided match policy is invalid.</summary>
        public static Error InvalidMatchPolicy =>
            Error.Validation(
                code: "TaxonRule.InvalidMatchPolicy",
                description: $"Match policy must be one of: {string.Join(separator: ", ", value: Constraints.MatchPolicies)}");

        /// <summary>Error indicating that a property name is required for 'product_property' rule types.</summary>
        public static Error PropertyNameRequired =>
            Error.Validation(
                code: "TaxonRule.PropertyNameRequired",
                description: "Property name is required for product_property rule type");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Gets or sets the type of rule, defining which product attribute this rule applies to.
    /// Uses predefined types from <see cref="Constraints.RuleTypes"/>.
    /// </summary>
    public string Type { get; set; } = null!;
    /// <summary>
    /// Gets or sets the value or pattern to match against.
    /// The interpretation of this value depends on the <see cref="Type"/> and <see cref="MatchPolicy"/>.
    /// </summary>
    public string Value { get; set; } = null!;
    /// <summary>
    /// Gets or sets the policy for matching the <see cref="Value"/> against a product attribute.
    /// Uses predefined policies from <see cref="Constraints.MatchPolicies"/>.
    /// Defaults to "is_equal_to".
    /// </summary>
    public string MatchPolicy { get; set; } = Constraints.MatchPolicies[0];
    /// <summary>
    /// Gets or sets the name of the product property to evaluate when <see cref="Type"/> is "product_property".
    /// Required for this rule type.
    /// </summary>
    public string? PropertyName { get; set; }
    #endregion

    #region Relationships
    /// <summary>
    /// Gets or sets the unique identifier of the parent <see cref="Taxon"/> this rule belongs to.
    /// </summary>
    public Guid TaxonId { get; set; }
    /// <summary>
    /// Gets or sets the navigation property to the associated <see cref="Taxon"/>.
    /// </summary>
    public Taxon Taxon { get; set; } = null!;
    #endregion

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private TaxonRule() { }

    #region Factory
    /// <summary>
    /// Factory method to create a new <see cref="TaxonRule"/> instance.
    /// Performs validation on the rule type, match policy, and property name.
    /// </summary>
    /// <param name="taxonId">The unique identifier of the <see cref="Taxon"/> this rule is for.</param>
    /// <param name="type">The type of rule (e.g., "product_name", "product_price").</param>
    /// <param name="value">The value or pattern to match against.</param>
    /// <param name="matchPolicy">Optional: The policy for matching the value. Defaults to "is_equal_to".</param>
    /// <param name="propertyName">Optional: The product property name for 'product_property' rule types.</param>
    /// <returns>
    /// An <see cref="ErrorOr{TaxonRule}"/> result.
    /// Returns the newly created <see cref="TaxonRule"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails (e.g., <see cref="Errors.InvalidType"/>, <see cref="Errors.PropertyNameRequired"/>).
    /// </returns>
    /// <remarks>
    /// This method normalizes and validates the <paramref name="type"/> and <paramref name="matchPolicy"/>
    /// against predefined valid values. It also enforces the requirement for <paramref name="propertyName"/>
    /// when the rule <paramref name="type"/> is "product_property".
    /// </remarks>
    public static ErrorOr<TaxonRule> Create(
        Guid taxonId,
        string type,
        string value,
        string? matchPolicy = null,
        string? propertyName = null)
    {
        // --- Validate Type ---
        var normalizedType = type.Trim().ToLowerInvariant();
        if (!Constraints.RuleTypes.Contains(value: normalizedType))
            return Errors.InvalidType;

        // --- Validate MatchPolicy ---
        var policy = matchPolicy?.Trim().ToLowerInvariant() ?? Constraints.MatchPolicies[0];
        if (!Constraints.MatchPolicies.Contains(value: policy))
            return Errors.InvalidMatchPolicy;

        // --- Validate PropertyName for product_property ---
        if (normalizedType == "product_property" && string.IsNullOrWhiteSpace(value: propertyName))
            return Errors.PropertyNameRequired;

        var rule = new TaxonRule
        {
            TaxonId = taxonId,
            Type = normalizedType,
            Value = value.Trim(),
            MatchPolicy = policy,
            PropertyName = propertyName?.Trim(),
            CreatedAt = DateTimeOffset.UtcNow // Inherited from AuditableEntity
        };

        return rule;
    }
    #endregion

    #region Business Logic
    /// <summary>
    /// Updates the attributes of the <see cref="TaxonRule"/>.
    /// This method allows for partial updates; only provided parameters will be changed.
    /// </summary>
    /// <param name="type">The new rule type. If null, the existing type is retained.</param>
    /// <param name="value">The new value or pattern to match against. If null, the existing value is retained.</param>
    /// <param name="matchPolicy">The new match policy. If null, the existing policy is retained.</param>
    /// <param name="propertyName">The new product property name. If null, the existing property name is retained.</param>
    /// <returns>
    /// An <see cref="ErrorOr{TaxonRule}"/> result.
    /// Returns the updated <see cref="TaxonRule"/> instance on success.
    /// Returns one of the <see cref="Errors"/> if validation fails.
    /// </returns>
    /// <remarks>
    /// This method performs validation for new values provided, ensuring they conform to defined constraints.
    /// It automatically updates the <c>UpdatedAt</c> timestamp if any changes occur.
    /// It also re-validates the <paramref name="propertyName"/> requirement if the rule <paramref name="type"/> is 'product_property'.
    /// </remarks>
    public ErrorOr<TaxonRule> Update(
        string? type = null,
        string? value = null,
        string? matchPolicy = null,
        string? propertyName = null)
    {
        bool changed = false;

        if (!string.IsNullOrWhiteSpace(value: type))
        {
            var normalized = type.Trim().ToLowerInvariant();
            if (normalized != Type && !Constraints.RuleTypes.Contains(value: normalized))
                return Errors.InvalidType;

            if (normalized != Type) { Type = normalized; changed = true; }
        }

        if (value is not null && value.Trim() != Value)
        {
            Value = value.Trim();
            changed = true;
        }

        if (!string.IsNullOrWhiteSpace(value: matchPolicy))
        {
            var policy = matchPolicy.Trim().ToLowerInvariant();
            if (policy != MatchPolicy && !Constraints.MatchPolicies.Contains(value: policy))
                return Errors.InvalidMatchPolicy;

            if (policy != MatchPolicy) { MatchPolicy = policy; changed = true; }
        }

        if (propertyName is not null)
        {
            var trimmed = propertyName.Trim();
            if (Type == "product_property" && string.IsNullOrWhiteSpace(value: trimmed))
                return Errors.PropertyNameRequired;

            if (trimmed != PropertyName) { PropertyName = trimmed; changed = true; }
        }

        if (changed)
        {
            UpdatedAt = DateTimeOffset.UtcNow;
            // A domain event for TaxonRule updated could be added here if needed.
        }

        return this;
    }

    /// <summary>
    /// Marks the <see cref="TaxonRule"/> for logical deletion.
    /// In this context, deletion typically means signaling that the rule should be removed from its parent <see cref="Taxon"/>.
    /// The actual removal from the parent aggregate's collection is handled by the <see cref="Taxon"/> aggregate.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{Deleted}"/> result.
    /// Always returns <see cref="Result.Deleted"/>, as the removal from the parent collection
    /// is managed by the <see cref="Taxon"/> aggregate.
    /// </returns>
    /// <remarks>
    /// This method signals that the taxon rule should no longer be associated with its parent <see cref="Taxon"/>.
    /// The <see cref="Taxon.RemoveRule(Guid)"/> method should be used to initiate the removal from the taxon's collection.
    /// </remarks>
    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}