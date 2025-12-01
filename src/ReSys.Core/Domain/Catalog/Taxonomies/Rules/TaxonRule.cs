using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Catalog.Taxonomies.Taxa;

namespace ReSys.Core.Domain.Catalog.Taxonomies.Rules;

/// <summary>
/// Represents a rule for automatically classifying products into a taxon.
/// </summary>
public sealed class TaxonRule : AuditableEntity
{
    #region Constraints
    public static class Constraints
    {
        public const int TypeMaxLength = CommonInput.Constraints.Text.ShortTextMaxLength;
        public const int ValueMaxLength = CommonInput.Constraints.Text.MediumTextMaxLength;
        public const int PropertyNameMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;

        public static readonly string[] MatchPolicies =
        [
            "is_equal_to", "is_not_equal_to", "contains", "does_not_contain",
            "starts_with", "ends_with", "greater_than", "less_than",
            "greater_than_or_equal", "less_than_or_equal", "in", "not_in",
            "is_null", "is_not_null"
        ];

        public static readonly string[] RuleTypes =
        [
            "product_name", "product_sku", "product_description", "product_price",
            "product_weight", "product_available", "product_archived",
            "product_property", "variant_price", "variant_sku", "classification_taxon"
        ];
    }
    #endregion

    #region Errors (Public)
    public static class Errors
    {
        public static Error Required => Error.Validation(
            code: "TaxonRule.Required",
            description: "At least one taxon rule is required.");

        public static Error TaxonMismatch(Guid id, Guid taxonId) => Error.Validation(
            code: "TaxonRule.TaxonMismatch",
            description: $"Rule belongs to taxon '{id}', but current taxon is '{taxonId}'.");

        public static Error Duplicate => Error.Conflict(
            code: "TaxonRule.Duplicate",
            description: "A rule with the same type, value, and match policy already exists.");

        public static Error NotFound(Guid id) =>
            Error.NotFound(code: "TaxonRule.NotFound", description: $"TaxonRule with ID '{id}' was not found.");

        public static Error InvalidType =>
            Error.Validation(
                code: "TaxonRule.InvalidType",
                description: $"Rule type must be one of: {string.Join(separator: ", ", value: Constraints.RuleTypes)}");

        public static Error InvalidMatchPolicy =>
            Error.Validation(
                code: "TaxonRule.InvalidMatchPolicy",
                description: $"Match policy must be one of: {string.Join(separator: ", ", value: Constraints.MatchPolicies)}");

        public static Error PropertyNameRequired =>
            Error.Validation(
                code: "TaxonRule.PropertyNameRequired",
                description: "Property name is required for product_property rule type");
    }
    #endregion

    #region Properties
    public string Type { get; set; } = null!;
    public string Value { get; set; } = null!;
    public string MatchPolicy { get; set; } = Constraints.MatchPolicies[0];
    public string? PropertyName { get; set; }
    #endregion

    #region Relationships
    public Guid TaxonId { get; set; }
    public Taxon Taxon { get; set; } = null!;
    #endregion

    private TaxonRule() { }

    #region Factory
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
            PropertyName = propertyName?.Trim()
        };

        return rule;
    }
    #endregion

    #region Business Logic
    public ErrorOr<TaxonRule> Update(
        string? type = null,
        string? value = null,
        string? matchPolicy = null,
        string? propertyName = null)
    {
        if (!string.IsNullOrWhiteSpace(value: type))
        {
            var normalized = type.Trim().ToLowerInvariant();
            if (normalized != Type && !Constraints.RuleTypes.Contains(value: normalized))
                return Errors.InvalidType;

            Type = normalized;
        }

        if (value is not null && value.Trim() != Value)
            Value = value.Trim();

        if (!string.IsNullOrWhiteSpace(value: matchPolicy))
        {
            var policy = matchPolicy.Trim().ToLowerInvariant();
            if (policy != MatchPolicy && !Constraints.MatchPolicies.Contains(value: policy))
                return Errors.InvalidMatchPolicy;

            MatchPolicy = policy;
        }

        if (propertyName is not null)
        {
            var trimmed = propertyName.Trim();
            if (Type == "product_property" && string.IsNullOrWhiteSpace(value: trimmed))
                return Errors.PropertyNameRequired;

            PropertyName = trimmed;
        }

        return this;
    }

    public ErrorOr<Deleted> Delete() => Result.Deleted;
    #endregion
}