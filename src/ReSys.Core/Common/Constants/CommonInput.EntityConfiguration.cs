using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace ReSys.Core.Common.Constants;

public static partial class CommonInput
{
    #region EF Core Configuration
    #region General Input Configuration
    public static PropertyBuilder<string?> ConfigureInputOptional(
        this PropertyBuilder<string?> builder,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? columnName = null,
        int? minLength = null,
        int? maxLength = null,
        string? pattern = null,
        string? columnType = null,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        int min = minLength ?? Constraints.Text.MinLength;
        int max = maxLength ?? Constraints.Text.MaxLength;
        string colName = columnName ?? Constraints.General.DefaultFieldName;

        builder
            .HasColumnName(name: colName)
            .IsRequired(required: isRequired)
            .HasMaxLength(maxLength: max)
            .HasAnnotation(annotation: "MinLength",
                value: min)
            .HasAnnotation("ErrorCode", errorCode ?? (string.IsNullOrWhiteSpace(pattern) ? ValidationMessages.General.InvalidInputCode : ValidationMessages.Text.InvalidPatternCode));

        if (!string.IsNullOrWhiteSpace(value: pattern))
            builder.HasAnnotation(annotation: "Pattern",
                value: pattern);

        if (!string.IsNullOrWhiteSpace(value: columnType))
            builder.HasColumnType(typeName: columnType);

        return builder;
    }
    public static PropertyBuilder<string> ConfigureInput(
        this PropertyBuilder<string> builder,
        string? columnName = null,
        int? minLength = null,
        int? maxLength = null,
        string? pattern = null,
        string? columnType = null,
        string? errorCode = null)
    {
        ArgumentNullException.ThrowIfNull(argument: builder);

        int min = minLength ?? Constraints.Text.MinLength;
        int max = maxLength ?? Constraints.Text.MaxLength;
        string colName = columnName ?? Constraints.General.DefaultFieldName;

        builder
            .HasColumnName(name: colName)
            .IsRequired()
            .HasMaxLength(maxLength: max)
            .HasAnnotation(annotation: "MinLength",
                value: min)
            .HasAnnotation("ErrorCode", errorCode ?? (string.IsNullOrWhiteSpace(pattern) ? ValidationMessages.General.InvalidInputCode : ValidationMessages.Text.InvalidPatternCode));

        if (!string.IsNullOrWhiteSpace(value: pattern))
            builder.HasAnnotation(annotation: "Pattern",
                value: pattern);

        if (!string.IsNullOrWhiteSpace(value: columnType))
            builder.HasColumnType(typeName: columnType);

        return builder;
    }
    #endregion

    #region Email & Phone
    public static PropertyBuilder<string?> ConfigureEmail(this PropertyBuilder<string?> builder, bool isRequired = false, string? errorCode = null) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Email.MaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Email.Pattern)
            .HasAnnotation("ErrorCode", errorCode ?? ValidationMessages.Contact.InvalidEmailCode);

    public static PropertyBuilder<string?> ConfigurePhone(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.PhoneNumbers.MaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.PhoneNumbers.Pattern);

    public static PropertyBuilder<string?> ConfigurePhoneE164(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.PhoneNumbers.E164MaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.PhoneNumbers.E164Pattern);
    #endregion

    #region URL & URI
    public static PropertyBuilder<string?> ConfigureUrlOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.UrlAndUri.UrlMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.UrlAndUri.UrlPattern);

    public static PropertyBuilder<string> ConfigureUrl(this PropertyBuilder<string> builder) =>
        builder.IsRequired()
            .HasMaxLength(maxLength: Constraints.UrlAndUri.UrlMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.UrlAndUri.UrlPattern);

    public static PropertyBuilder<string?> ConfigureUri(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.UrlAndUri.UriMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.UrlAndUri.UriPattern);
    #endregion

    #region Names & Usernames
    public static PropertyBuilder<string?> ConfigureNameOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.NamesAndUsernames.NameMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.NamesAndUsernames.NamePattern);

    public static PropertyBuilder<string> ConfigureName(this PropertyBuilder<string> builder) =>
        builder.IsRequired(required: true)
            .HasMaxLength(maxLength: Constraints.NamesAndUsernames.NameMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.NamesAndUsernames.NamePattern);

    public static PropertyBuilder<string?> ConfigureUsername(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.NamesAndUsernames.UsernameMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.NamesAndUsernames.UsernamePattern);
    #endregion

    #region Identifiers
    public static PropertyBuilder<string?> ConfigureGuid(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: 36)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Identifiers.GuidPattern);

    public static PropertyBuilder<string?> ConfigureUlid(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: 26)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Identifiers.UlidPattern);

    public static PropertyBuilder<string?> ConfigureNanoId(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: 21)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Identifiers.NanoIdPattern);
    #endregion

    #region Network
    public static PropertyBuilder<string?> ConfigureIpV4(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Network.IpV4MaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Network.IpV4Pattern);

    public static PropertyBuilder<string?> ConfigureIpV6(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Network.IpV6MaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Network.IpV6Pattern);

    public static PropertyBuilder<string?> ConfigureMacAddress(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Network.MacAddressMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Network.MacAddressPattern);

    public static PropertyBuilder<string?> ConfigureDomainOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Network.DomainMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Network.DomainPattern);

    public static PropertyBuilder<string> ConfigureDomain(this PropertyBuilder<string> builder) =>
        builder
            .HasMaxLength(maxLength: Constraints.Network.DomainMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Network.DomainPattern);
    #endregion

    #region Geographic & Postal Codes
    public static PropertyBuilder<string?> ConfigurePostalCode(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.GeographicAndPostalCodes.PostalCodeMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.GeographicAndPostalCodes.PostalCodePattern);

    public static PropertyBuilder<string?> ConfigureZipCode(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.GeographicAndPostalCodes.ZipCodeMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.GeographicAndPostalCodes.ZipCodePattern);

    public static PropertyBuilder<string?> ConfigureCanadianPostalCode(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.GeographicAndPostalCodes.CanadianPostalCodeMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.GeographicAndPostalCodes.CanadianPostalCodePattern);

    public static PropertyBuilder<string?> ConfigureUkPostalCode(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.GeographicAndPostalCodes.UkPostalCodeMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.GeographicAndPostalCodes.UkPostalCodePattern);
    #endregion

    #region Passwords
    public static PropertyBuilder<string?> ConfigurePassword(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Passwords.MaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Passwords.StrongPasswordPattern);
    #endregion

    #region Slugs & Versions
    public static PropertyBuilder<string?> ConfigureSlugOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.SlugsAndVersions.SlugMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.SlugsAndVersions.SlugPattern);

    public static PropertyBuilder<string> ConfigureSlug(this PropertyBuilder<string> builder) =>
        builder.HasMaxLength(maxLength: Constraints.SlugsAndVersions.SlugMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.SlugsAndVersions.SlugPattern);

    public static PropertyBuilder<string?> ConfigureSemVer(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.SlugsAndVersions.SemVerMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.SlugsAndVersions.SemVerPattern);

    public static PropertyBuilder<string?> ConfigureVersion(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.SlugsAndVersions.VersionMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.SlugsAndVersions.VersionPattern);
    #endregion

    #region Social Media
    public static PropertyBuilder<string?> ConfigureTwitterHandle(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.SocialMedia.TwitterHandleMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.SocialMedia.TwitterHandlePattern);

    public static PropertyBuilder<string?> ConfigureHashtag(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.SocialMedia.HashtagMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.SocialMedia.HashtagPattern);
    #endregion

    #region Date & Time
    public static PropertyBuilder<string?> ConfigureDate(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.DateAndTime.DateMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.DateAndTime.DatePattern);

    public static PropertyBuilder<string?> ConfigureTime(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.DateAndTime.TimeMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.DateAndTime.TimePattern);

    public static PropertyBuilder<string?> ConfigureTimeSpan(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.DateAndTime.TimeSpanMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.DateAndTime.TimeSpanPattern);
    #endregion

    #region JSON & Boolean
    public static PropertyBuilder<string?> ConfigureJsonOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Json.Pattern);

    public static PropertyBuilder<string> ConfigureJson(this PropertyBuilder<string> builder, bool isRequired = false) =>
        builder.HasAnnotation(annotation: "Pattern",
                value: Constraints.Json.Pattern);


    public static PropertyBuilder<string?> ConfigureBoolean(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Boolean.Pattern);
    #endregion

    #region Payment & Credit Cards
    public static PropertyBuilder<string?> ConfigureCreditCard(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.PaymentAndCreditCards.CreditCardMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.PaymentAndCreditCards.CreditCardPattern);

    public static PropertyBuilder<string?> ConfigureCvv(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.PaymentAndCreditCards.CvvMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.PaymentAndCreditCards.CvvPattern);
    #endregion

    #region Color
    public static PropertyBuilder<string?> ConfigureHexColor(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Color.HexColorMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.Color.HexColorPattern);
    #endregion

    #region File System
    public static PropertyBuilder<string?> ConfigureFilePath(this PropertyBuilder<string?> builder, bool isRequired = false) =>
    builder.IsRequired(required: isRequired)
        .HasMaxLength(maxLength: Constraints.FileSystem.FilePathMaxLength)
        .HasAnnotation(annotation: "Pattern",
            value: Constraints.FileSystem.FilePathPattern);

    public static PropertyBuilder<string?> ConfigureFileExtension(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.FileSystem.FileExtensionMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.FileSystem.FileExtensionPattern);
    #endregion

    #region Currency & Language
    public static PropertyBuilder<string?> ConfigureCurrencyCodeOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.CurrencyAndLanguage.CurrencyCodeLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.CurrencyAndLanguage.CurrencyCodePattern);
    public static PropertyBuilder<string> ConfigureCurrencyCode(this PropertyBuilder<string> builder) =>
        builder
            .HasMaxLength(maxLength: Constraints.CurrencyAndLanguage.CurrencyCodeLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.CurrencyAndLanguage.CurrencyCodePattern);

    public static PropertyBuilder<string?> ConfigureLanguageCodeOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.CurrencyAndLanguage.LanguageCodeMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.CurrencyAndLanguage.LanguageCodePattern);

    public static PropertyBuilder<string> ConfigureLanguageCode(this PropertyBuilder<string> builder, bool isRequired = false) =>
        builder.HasMaxLength(maxLength: Constraints.CurrencyAndLanguage.LanguageCodeMaxLength)
            .HasAnnotation(annotation: "Pattern",
                value: Constraints.CurrencyAndLanguage.LanguageCodePattern);

    #endregion

    #region Text Content Lengths
    public static PropertyBuilder<string?> ConfigureShortTextOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Text.ShortTextMaxLength);

    public static PropertyBuilder<string> ConfigureShortText(this PropertyBuilder<string> builder) =>
    builder.HasMaxLength(maxLength: Constraints.Text.ShortTextMaxLength);

    public static PropertyBuilder<string?> ConfigureMediumTextOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Text.MediumTextMaxLength);

    public static PropertyBuilder<string> ConfigureMediumText(this PropertyBuilder<string> builder) =>
        builder.HasMaxLength(maxLength: Constraints.Text.MediumTextMaxLength);

    public static PropertyBuilder<string?> ConfigureLongTextOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Text.LongTextMaxLength);

    public static PropertyBuilder<string> ConfigureLongText(this PropertyBuilder<string> builder) =>
        builder.HasMaxLength(maxLength: Constraints.Text.LongTextMaxLength);

    public static PropertyBuilder<string?> ConfigureDescriptionOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Text.DescriptionMaxLength);

    public static PropertyBuilder<string> ConfigureDescription(this PropertyBuilder<string> builder) =>
        builder
            .HasMaxLength(maxLength: Constraints.Text.DescriptionMaxLength);

    public static PropertyBuilder<string?> ConfigureTitleOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Text.TitleMaxLength);

    public static PropertyBuilder<string> ConfigureTitle(this PropertyBuilder<string> builder) =>
        builder.HasMaxLength(maxLength: Constraints.Text.TitleMaxLength);

    public static PropertyBuilder<string?> ConfigureCommentOptional(this PropertyBuilder<string?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasMaxLength(maxLength: Constraints.Text.CommentMaxLength);

    public static PropertyBuilder<string> ConfigureComment(this PropertyBuilder<string> builder, bool isRequired = false) =>
        builder
            .HasMaxLength(maxLength: Constraints.Text.CommentMaxLength);
    #endregion

    #region Geographic Coordinates
    public static PropertyBuilder<decimal> ConfigureLatitude(this PropertyBuilder<decimal> builder) =>
        builder.IsRequired()
            .HasPrecision(precision: 9,
                scale: 6);

    public static PropertyBuilder<decimal?> ConfigureLatitude(this PropertyBuilder<decimal?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasPrecision(precision: 9,
                scale: 6);

    public static PropertyBuilder<decimal> ConfigureLongitude(this PropertyBuilder<decimal> builder) =>
        builder.IsRequired()
            .HasPrecision(precision: 9,
                scale: 6);

    public static PropertyBuilder<decimal?> ConfigureLongitude(this PropertyBuilder<decimal?> builder, bool isRequired = false) =>
        builder.IsRequired(required: isRequired)
            .HasPrecision(precision: 9,
                scale: 6);
    #endregion

    #region Dictionary
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonObjectConverter() }
    };

    private sealed class JsonObjectConverter : JsonConverter<object?>
    {
        public override object? Read(ref Utf8JsonReader reader, Type t, JsonSerializerOptions o)
            => reader.TokenType switch
            {
                JsonTokenType.Null => null,
                JsonTokenType.True => true,
                JsonTokenType.False => false,
                JsonTokenType.Number when reader.TryGetInt64(out long l) => l,
                JsonTokenType.Number => reader.GetDouble(),
                JsonTokenType.String when reader.TryGetDateTime(out var dt) => dt,
                JsonTokenType.String => reader.GetString(),
                JsonTokenType.StartObject => JsonSerializer.Deserialize<Dictionary<string, object?>>(ref reader, o),
                JsonTokenType.StartArray => JsonSerializer.Deserialize<List<object?>>(ref reader, o),
                _ => throw new JsonException()
            };

        public override void Write(Utf8JsonWriter w, object? v, JsonSerializerOptions o)
            => JsonSerializer.Serialize(w, v, v?.GetType() ?? typeof(object), o);
    }

    // -----------------------------------------------------
    // Converter (works for nullable or required)
    // -----------------------------------------------------
    private static readonly ValueConverter<IDictionary<string, object?>?, string> DictConverter =
        new(
            v => JsonSerializer.Serialize(v ?? new Dictionary<string, object?>(), JsonOptions),
            v => string.IsNullOrWhiteSpace(v)
                ? new Dictionary<string, object?>()
                : JsonSerializer.Deserialize<Dictionary<string, object?>>(v, JsonOptions)
                  ?? new Dictionary<string, object?>()
        );

    // -----------------------------------------------------
    // Comparer (expression-safe)
    // -----------------------------------------------------
    private static readonly ValueComparer<IDictionary<string, object?>?> DictComparer =
        new(
            (a, b) =>
                JsonSerializer.Serialize(a ?? new Dictionary<string, object?>(), JsonOptions)
                ==
                JsonSerializer.Serialize(b ?? new Dictionary<string, object?>(), JsonOptions),

            d =>
                JsonSerializer.Serialize(d ?? new Dictionary<string, object?>(), JsonOptions)
                .GetHashCode(),

            d =>
                JsonSerializer.Deserialize<Dictionary<string, object?>>(
                    JsonSerializer.Serialize(d ?? new Dictionary<string, object?>(), JsonOptions),
                    JsonOptions
                )!
        );

    // -----------------------------------------------------
    // Core internal method
    // -----------------------------------------------------
    private static PropertyBuilder<IDictionary<string, object?>?> ConfigureInternal(
        PropertyBuilder<IDictionary<string, object?>?> builder,
        bool isRequired,
        string? columnType)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.HasConversion(DictConverter);
        builder.Metadata.SetValueComparer(DictComparer);
        builder.HasColumnType(columnType ?? "jsonb");

        if (isRequired)
            builder.IsRequired();

        return builder;
    }

    // -----------------------------------------------------
    // Public API
    // -----------------------------------------------------

    // Optional dictionary
    public static PropertyBuilder<IDictionary<string, object?>?> ConfigureDictionary(
        this PropertyBuilder<IDictionary<string, object?>?> builder,
        bool isRequired = false,
        string? columnType = null)
        => ConfigureInternal(builder, isRequired, columnType);

    // Required dictionary
    public static PropertyBuilder<IDictionary<string, object?>> ConfigureDictionaryRequired(
        this PropertyBuilder<IDictionary<string, object?>> builder,
        string? columnType = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Cast to nullable version for reuse of internal code
        var cast = (PropertyBuilder<IDictionary<string, object?>?>)(object)builder;
        ConfigureInternal(cast, isRequired: true, columnType);

        return builder;
    }
    #endregion

    #region DateTime Utc Conversions
    private static readonly ValueConverter<DateTime, DateTime> UtcDateTimeConverter =
    new(convertToProviderExpression: v => v.Kind == DateTimeKind.Utc ? v : v.ToUniversalTime(),
        convertFromProviderExpression: v => DateTime.SpecifyKind(v,
            DateTimeKind.Utc));

    private static readonly ValueConverter<DateTime?, DateTime?> UtcNullableDateTimeConverter =
        new(
            convertToProviderExpression: v =>
                v.HasValue ? v.Value.Kind == DateTimeKind.Utc ? v.Value : v.Value.ToUniversalTime() : null,
            convertFromProviderExpression: v => v.HasValue ? DateTime.SpecifyKind(v.Value,
                DateTimeKind.Utc) : null);

    private static readonly ValueConverter<DateTimeOffset, DateTimeOffset> UtcDateTimeOffsetConverter =
        new(convertToProviderExpression: v => v.ToUniversalTime(),
            convertFromProviderExpression: v => v.ToUniversalTime());

    private static readonly ValueConverter<DateTimeOffset?, DateTimeOffset?> UtcNullableDateTimeOffsetConverter =
        new(convertToProviderExpression: v => v.HasValue ? v.Value.ToUniversalTime() : null,
            convertFromProviderExpression: v => v.HasValue ? v.Value.ToUniversalTime() : null);

    public static void ApplyUtcConversions(this ModelBuilder modelBuilder)
    {
        foreach (IMutableEntityType entityType in modelBuilder.Model.GetEntityTypes())
        {
            foreach (PropertyInfo property in entityType.ClrType.GetProperties())
            {
                if (property.PropertyType == typeof(DateTime))
                {
                    modelBuilder.Entity(name: entityType.Name)
                        .Property(propertyName: property.Name)
                        .HasConversion(converter: UtcDateTimeConverter);
                }
                else if (property.PropertyType == typeof(DateTime?))
                {
                    modelBuilder.Entity(name: entityType.Name)
                        .Property(propertyName: property.Name)
                        .HasConversion(converter: UtcNullableDateTimeConverter);
                }
                else if (property.PropertyType == typeof(DateTimeOffset))
                {
                    modelBuilder.Entity(name: entityType.Name)
                        .Property(propertyName: property.Name)
                        .HasConversion(converter: UtcDateTimeOffsetConverter);
                }
                else if (property.PropertyType == typeof(DateTimeOffset?))
                {
                    modelBuilder.Entity(name: entityType.Name)
                        .Property(propertyName: property.Name)
                        .HasConversion(converter: UtcNullableDateTimeOffsetConverter);
                }
            }
        }
    }
    #endregion

    #region Enum
    #region PostgreSQL Native Enum

    /// <summary>
    /// Configures an enum property to use PostgreSQL native enum type.
    /// This is the recommended approach for Npgsql 9.0+.
    /// Note: Enum must be mapped in UseNpgsql() configuration with MapEnum<TEnum>("enum_name")
    /// </summary>
    public static PropertyBuilder<TEnum> ConfigurePostgresEnum<TEnum>(
        this PropertyBuilder<TEnum> builder,
        string? columnName = null,
        bool required = true)
        where TEnum : struct, Enum
    {
        if (columnName != null)
            builder.HasColumnName(name: columnName);

        builder.IsRequired(required: required);

        // PostgreSQL will handle the enum natively when mapped via MapEnum()
        // No conversion needed - EF Core will map directly to PostgreSQL enum type

        return builder;
    }

    /// <summary>
    /// Configures a nullable enum property to use PostgreSQL native enum type.
    /// </summary>
    public static PropertyBuilder<TEnum?> ConfigurePostgresEnumOptional<TEnum>(
        this PropertyBuilder<TEnum?> builder,
        string? columnName = null)
        where TEnum : struct, Enum
    {
        if (columnName != null)
            builder.HasColumnName(name: columnName);
        else
            builder.HasColumnName(name: typeof(TEnum).Name);

        builder.IsRequired(required: false);

        // PostgreSQL will handle the enum natively when mapped via MapEnum()
        
        return builder;
    }

    #endregion
    #endregion
    #endregion
}
