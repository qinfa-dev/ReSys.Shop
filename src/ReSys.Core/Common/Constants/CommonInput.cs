using ReSys.Core.Common.Extensions;

namespace ReSys.Core.Common.Constants;

public static partial class CommonInput
{
    private const string DefaultPrefix = "Entity";
    private const string DefaultFieldName = "Validation";

    /// <summary>
    /// Generates a prefixed error code string, typically for use in validation or error reporting.
    /// </summary>
    /// <param name="prefix">The primary identifier, often representing an entity or a feature. Defaults to "Entity" if null or whitespace.</param>
    /// <param name="field">The specific field or context. Defaults to "Validation" if null.</param>
    /// <returns>A dot-separated error code in the format "Prefix.Field".</returns>
    /// <example>
    /// <code>
    /// // Returns "User.UserName"
    /// var errorCode = Prefix("User", "UserName");
    ///
    /// // Returns "Product.Sku"
    /// var skuError = Prefix("Product", "Sku");
    ///
    /// // Returns "Entity.Validation" (using default values)
    /// var defaultCode = Prefix();
    /// </code>
    /// </example>
    public static string Prefix(string? prefix = null, string? field = null)
    {
        string effectivePrefix = string.IsNullOrWhiteSpace(value: prefix) ? DefaultPrefix : prefix;
        string effectiveField = field ?? DefaultFieldName;
        return $"{effectivePrefix}.{effectiveField}";
    }

    /// <summary>
    /// Generates a human-readable label from a prefix or field, intended for display in user interfaces.
    /// </summary>
    /// <param name="prefix">The prefix, typically an entity name.</param>
    /// <param name="field">The specific field name. If provided, this is used to generate the label.</param>
    /// <returns>A humanized string. It prioritizes the 'field' for generation if it's available, otherwise it uses the 'prefix'.</returns>
    /// <example>
    /// <code>
    /// // Returns "User Name"
    /// var fieldLabel = Label("User", "UserName");
    ///
    /// // Returns "Product"
    /// var prefixLabel = Label("Product");
    ///
    /// // Returns "Default Prefix"
    /// var defaultLabel = Label("DefaultPrefix");
    /// </code>
    /// </example>
    public static string Label(string? prefix = null, string? field = null)
    {
        if (!string.IsNullOrWhiteSpace(value: field))
            return field.ToHumanize();

        return prefix.ToHumanize();
    }
}
