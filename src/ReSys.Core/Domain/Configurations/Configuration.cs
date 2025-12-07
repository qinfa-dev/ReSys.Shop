using ReSys.Core.Common.Domain;
using ReSys.Core.Common.Domain.Entities;
using ErrorOr;

namespace ReSys.Core.Domain.Configurations;

/// <summary>
/// Represents a dynamic application configuration entry stored as a key-value pair.
/// This allows for flexible, runtime-adjustable settings without requiring code changes or redeployments.
/// Configurations can be system-wide or module-specific.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Application:</strong>
/// This aggregate enables external management of application settings.
/// <list type="bullet">
/// <item>
/// <term>Dynamic Settings</term>
/// <description>Adjust behaviors, feature flags, or thresholds during runtime.</description>
/// </item>
/// <item>
/// <term>Centralized Control</term>
/// <description>Manage all configurable parameters from a single source (e.g., an admin panel).</description>
/// </item>
/// <item>
/// <term>Typed Values</term>
/// <description>Supports various data types (string, int, bool, etc.) ensuring data integrity.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Fields:</strong>
/// <list type="bullet">
/// <item>
/// <term>Key</term>
/// <description>A unique identifier for the configuration setting (e.g., "MaxItemsInCart").</description>
/// </item>
/// <item>
/// <term>Value</term>
/// <description>The current value of the configuration setting.</description>
/// </item>
/// <item>
/// <term>DefaultValue</term>
/// <description>The fallback value if the primary value is not set or invalid.</description>
/// </item>
/// <item>
/// <term>ValueType</term>
/// <description>The expected data type of the configuration's value, for validation and parsing.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public sealed class Configuration : Aggregate<Guid>
{
    /// <summary>
    /// Gets the unique key identifying this configuration setting.
    /// This key is used to retrieve the configuration value.
    /// </summary>
    public string Key { get; private set; } = string.Empty;
    /// <summary>
    /// Gets the current value of the configuration setting.
    /// This value can be updated dynamically.
    /// </summary>
    public string Value { get; private set; } = string.Empty;
    /// <summary>
    /// Gets a descriptive explanation of what this configuration setting controls.
    /// </summary>
    public string Description { get; private set; } = string.Empty;
    /// <summary>
    /// Gets the default or fallback value for this configuration setting.
    /// </summary>
    public string DefaultValue { get; private set; } = string.Empty;
    /// <summary>
    /// Gets the expected data type of the configuration's value.
    /// Used for validation and proper casting/parsing at the application layer.
    /// </summary>
    public ConfigurationValueType ValueType { get; private set; }

    /// <summary>
    /// Private constructor for ORM (Entity Framework Core) materialization.
    /// </summary>
    private Configuration() { }

    /// <summary>
    /// Initializes a new instance of the <see cref="Configuration"/> class.
    /// This constructor is primarily used internally by the static <see cref="Create"/> factory method.
    /// </summary>
    /// <param name="id">The unique identifier of the configuration.</param>
    /// <param name="key">The unique key of the configuration setting.</param>
    /// <param name="value">The current value of the configuration setting.</param>
    /// <param name="description">A description of the configuration setting.</param>
    /// <param name="defaultValue">The default value of the configuration setting.</param>
    /// <param name="valueType">The expected data type of the configuration's value.</param>
    private Configuration(
        Guid id,
        string key,
        string value,
        string description,
        string defaultValue,
        ConfigurationValueType valueType)
        : base(id)
    {
        Key = key;
        Value = value;
        Description = description;
        DefaultValue = defaultValue;
        ValueType = valueType;
    }

    /// <summary>
    /// Factory method to create a new <see cref="Configuration"/> instance.
    /// Performs basic validation and initializes the configuration with a new GUID.
    /// </summary>
    /// <param name="key">The unique key for the new configuration setting.</param>
    /// <param name="value">The initial value for the configuration setting.</param>
    /// <param name="description">A descriptive explanation of the configuration setting.</param>
    /// <param name="defaultValue">The default fallback value for the configuration setting.</param>
    /// <param name="valueType">The expected data type of the configuration's value.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Configuration}"/> result.
    /// Returns the newly created <see cref="Configuration"/> instance on success.
    /// Returns <see cref="Configuration.Errors.KeyRequired"/> if the key is null or whitespace.
    /// </returns>
    public static ErrorOr<Configuration> Create(
        string key,
        string value,
        string description,
        string defaultValue,
        ConfigurationValueType valueType)
    {
        // Basic validation as per existing domain entities (e.g., Product.cs)
        if (string.IsNullOrWhiteSpace(key))
        {
            return Configuration.Errors.KeyRequired;
        }

        // Additional validation can be added here if needed,
        // for instance, checking if the value can be parsed according to ValueType

        return new Configuration(
            Guid.NewGuid(),
            key,
            value,
            description,
            defaultValue,
            valueType);
    }

    /// <summary>
    /// Updates the <see cref="Value"/> of the configuration setting.
    /// </summary>
    /// <param name="newValue">The new value to set for the configuration.</param>
    /// <returns>
    /// An <see cref="ErrorOr{Updated}"/> result.
    /// Returns <see cref="Result.Updated"/> on successful update.
    /// Returns <see cref="Configuration.Errors.ValueRequired"/> if the new value is null or whitespace.
    /// </returns>
    public ErrorOr<Updated> Update(string newValue)
    {
        if (string.IsNullOrWhiteSpace(newValue))
        {
            return Configuration.Errors.ValueRequired;
        }
        
        // Add specific parsing/casting logic based on ValueType if necessary for validation
        // For now, we'll allow any string and assume parsing happens at application layer.

        Value = newValue;
        return Result.Updated;
    }

    /// <summary>
    /// Defines constraints and limits for configuration keys and values.
    /// </summary>
    public static class Constraints
    {
        /// <summary>
        /// Maximum length for the configuration key.
        /// </summary>
        public const int KeyMaxLength = 100;
        /// <summary>
        /// Maximum length for the configuration value.
        /// </summary>
        public const int ValueMaxLength = 500;
        /// <summary>
        /// Maximum length for the configuration description.
        /// </summary>
        public const int DescriptionMaxLength = 500;
        /// <summary>
        /// Maximum length for the default value of the configuration.
        /// </summary>
        public const int DefaultValueMaxLength = 500;
    }

    /// <summary>
    /// Defines domain error scenarios specific to <see cref="Configuration"/> operations.
    /// These errors are returned via the <see cref="ErrorOr"/> pattern for robust error handling.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that the configuration key is missing or empty.
        /// </summary>
        public static Error KeyRequired => Error.Validation(
            code: "Configuration.KeyRequired",
            description: "Configuration key is required.");

        /// <summary>
        /// Error indicating that the configuration value is missing or empty during an update.
        /// </summary>
        public static Error ValueRequired => Error.Validation(
            code: "Configuration.ValueRequired",
            description: "Configuration value is required.");
            
        /// <summary>
        /// Error indicating that a requested configuration could not be found.
        /// </summary>
        public static Error NotFound => Error.NotFound(
            code: "Configuration.NotFound",
            description: "Configuration not found.");
    }
}
