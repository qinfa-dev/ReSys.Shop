namespace ReSys.Core.Domain.Settings;

/// <summary>
/// Defines the possible data types for a configuration setting's value.
/// This enum helps in validating and correctly parsing configuration values
/// at the application layer.
/// </summary>
public enum ConfigurationValueType
{
    /// <summary>
    /// The configuration value is expected to be a string.
    /// </summary>
    String,
    /// <summary>
    /// The configuration value is expected to be a boolean (true/false).
    /// </summary>
    Boolean,
    /// <summary>
    /// The configuration value is expected to be an integer number.
    /// </summary>
    Integer
}