using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReSys.Core.Common.Domain.Concerns;

// ============================================================================
// IHasMetadata - Key-value metadata
// ============================================================================

public interface IHasMetadata
{
    // Optional key-value dictionary for public metadata
    IDictionary<string, object?>? PublicMetadata { get; set; }

    // Optional key-value dictionary for private metadata
    IDictionary<string, object?>? PrivateMetadata { get; set; }
}

public static class HasMetadata
{
    // -----------------------
    // Helper methods to get/set metadata
    // -----------------------
    public static object? GetPublic(this IHasMetadata? holder, string key)
        => holder?.PublicMetadata?.TryGetValue(key, out var v) == true ? v : null;

    public static T? GetPublic<T>(this IHasMetadata? holder, string key, T? defaultValue = default)
    {
        var value = holder?.PublicMetadata?.TryGetValue(key, out var v) == true ? v : null;
        return value is T typedValue ? typedValue : defaultValue;
    }

    public static IHasMetadata? SetPublic(this IHasMetadata? holder, string key, object? value)
    {
        if (holder == null) return holder;
        holder.PublicMetadata ??= new Dictionary<string, object?>();
        if (value == null)
            holder.PublicMetadata.Remove(key);
        else
            holder.PublicMetadata[key] = value;
        return holder;
    }

    public static object? GetPrivate(this IHasMetadata? holder, string key)
        => holder?.PrivateMetadata?.TryGetValue(key, out var v) == true ? v : null;

    public static T? GetPrivate<T>(this IHasMetadata? holder, string key, T? defaultValue = default)
    {
        var value = holder?.PrivateMetadata?.TryGetValue(key, out var v) == true ? v : null;
        return value is T typedValue ? typedValue : defaultValue;
    }

    public static IHasMetadata? SetPrivate(this IHasMetadata? holder, string key, object? value)
    {
        if (holder == null) return holder;
        holder.PrivateMetadata ??= new Dictionary<string, object?>();
        if (value == null)
            holder.PrivateMetadata.Remove(key);
        else
            holder.PrivateMetadata[key] = value;
        return holder;
    }

    // -----------------------
    // Metadata comparison (deep equals via JSON serialization for flexibility)
    // -----------------------
    public static bool MetadataEquals(
        this IDictionary<string, object?>? dict1,
        IDictionary<string, object?>? dict2,
        JsonSerializerOptions? options = null)
    {
        if ((dict1 == null || dict1.Count == 0) && (dict2 == null || dict2.Count == 0)) return true;
        if (dict1 == null || dict2 == null) return false;
        if (dict1.Count != dict2.Count) return false;

        options ??= new JsonSerializerOptions { WriteIndented = false };
        var json1 = JsonSerializer.Serialize(dict1, options);
        var json2 = JsonSerializer.Serialize(dict2, options);
        return json1 == json2;
    }

    // -----------------------
    // FluentValidation rules
    // -----------------------
    public static void AddMetadataSupportRules<T>(
        this AbstractValidator<T> validator,
        string? prefix = null,
        string? customMessage = null,
        int? maxEntries = null,
        int? keyMinLength = null,
        int? keyMaxLength = null,
        int? valueMaxLength = null,
        Regex? keyAllowedRegex = null)
        where T : class, IHasMetadata
    {
        string codePrefix = string.IsNullOrWhiteSpace(prefix) ? typeof(T).Name : prefix;
        Debug.Assert(validator != null, $"{nameof(validator)} != null");

        // PublicMetadata: optional, validate if present
        validator.RuleFor(m => m.PublicMetadata)
            .MustBeValidDictionary(prefix: codePrefix, customMessage: customMessage,
                maxEntries: maxEntries, keyMinLength: keyMinLength, keyMaxLength: keyMaxLength,
                valueMaxLength: valueMaxLength, keyAllowedRegex: keyAllowedRegex);

        // PrivateMetadata: same rules as public
        validator.RuleFor(m => m.PrivateMetadata)
            .MustBeValidDictionary(prefix: codePrefix, customMessage: customMessage,
                maxEntries: maxEntries, keyMinLength: keyMinLength, keyMaxLength: keyMaxLength,
                valueMaxLength: valueMaxLength, keyAllowedRegex: keyAllowedRegex);
    }

    // -----------------------
    // EF Core configuration (using proper converters and comparers)
    // -----------------------
    public static void ConfigureMetadata<T>(this EntityTypeBuilder<T> builder)
        where T : class, IHasMetadata
    {
        // PublicMetadata stored as JSON with proper converter and comparer
        builder.Property(x => x.PublicMetadata)
            .ConfigureDictionary(isRequired: false)
            .HasComment("Public key-value metadata for the entity, stored as JSON.");

        // PrivateMetadata stored as JSON with proper converter and comparer
        builder.Property(x => x.PrivateMetadata)
            .ConfigureDictionary(isRequired: false)
            .HasComment("Private key-value metadata for the entity, stored as JSON.");
    }
}