using System.Reflection;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ReSys.Core.Common.Constants;

public static partial class CommonInput
{
    #region FluentValidation Rules
    // -------------------------------------------------------------------------
    // FluentValidation Extension Methods
    // -------------------------------------------------------------------------
    #region Generic
    public static IRuleBuilderOptions<T, string?> NullableRequired<T>(
        this IRuleBuilder<T, string?> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb
            .NotEmpty()
            .WithErrorCode(errorCode: Errors.NullOrEmpty(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.NullOrEmpty(prefix: prefix,
                field: field,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string> Required<T>(
        this IRuleBuilder<T, string> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.NotEmpty()
            .WithErrorCode(errorCode: Errors.NullOrEmpty(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.NullOrEmpty(prefix: prefix,
                field: field,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string?> MinLength<T>(
        this IRuleBuilder<T, string?> rb,
        int min,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.MinimumLength(minimumLength: min)
            .WithErrorCode(errorCode: Errors.TooShort(prefix: prefix,
                field: field,
                minLength: min).Code)
            .WithMessage(errorMessage: Errors.TooShort(prefix: prefix,
                field: field,
                minLength: min,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string> MinLengthRequired<T>(
        this IRuleBuilder<T, string> rb,
        int min,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.MinimumLength(minimumLength: min)
            .WithErrorCode(errorCode: Errors.TooShort(prefix: prefix,
                field: field,
                minLength: min).Code)
            .WithMessage(errorMessage: Errors.TooShort(prefix: prefix,
                field: field,
                minLength: min,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string?> MaxLength<T>(
        this IRuleBuilder<T, string?> rb,
        int max,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.MaximumLength(maximumLength: max)
            .WithErrorCode(errorCode: Errors.TooLong(prefix: prefix,
                field: field,
                maxLength: max).Code)
            .WithMessage(errorMessage: Errors.TooLong(prefix: prefix,
                field: field,
                maxLength: max,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string> MaxLengthRequired<T>(
          this IRuleBuilder<T, string> rb,
          int max,
          string? prefix = null,
          string? field = null,
          string? msg = null) =>
          rb.MaximumLength(maximumLength: max)
              .WithErrorCode(errorCode: Errors.TooLong(prefix: prefix,
                  field: field,
                  maxLength: max).Code)
              .WithMessage(errorMessage: Errors.TooLong(prefix: prefix,
                  field: field,
                  maxLength: max,
                  msg: msg).Description);


    public static IRuleBuilderOptions<T, string?> LengthRange<T>(
        this IRuleBuilder<T, string?> rb,
        int min, int max,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.MinimumLength(minimumLength: min)
            .WithErrorCode(errorCode: Errors.TooShort(prefix: prefix,
                field: field,
                minLength: min).Code)
            .WithMessage(errorMessage: Errors.TooShort(prefix: prefix,
                field: field,
                minLength: min,
                msg: msg).Description)
            .MaximumLength(maximumLength: max)
            .WithErrorCode(errorCode: Errors.TooLong(prefix: prefix,
                field: field,
                maxLength: max).Code)
            .WithMessage(errorMessage: Errors.TooLong(prefix: prefix,
                field: field,
                maxLength: max,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string> LengthRangeRequired<T>(
        this IRuleBuilder<T, string> rb,
        int min, int max,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.MinLengthRequired(min: min,
                prefix: prefix,
                field: field,
                msg: msg)
            .MaxLengthRequired(max: max,
                prefix: prefix,
                field: field,
                msg: msg);

    public static IRuleBuilderOptions<T, string?> LengthInRange<T>(
        this IRuleBuilder<T, string?> rb,
        int min,
        int max,
        string? prefix = null,
        string? field = null,
        string? msg = null)
    {
        return rb
            .Must(predicate: v => v == null || v.Length >= min && v.Length <= max)
            .WithErrorCode(errorCode: Errors.InvalidRange(prefix: prefix,
                field: field,
                min: min,
                max: max).Code)
            .WithMessage(errorMessage: msg ?? Errors.InvalidRange(prefix: prefix,
                field: field,
                min: min,
                max: max).Description);
    }


    public static IRuleBuilderOptions<T, string?> MustMatchPattern<T>(
        this IRuleBuilder<T, string?> rb,
        string pattern,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.Matches(expression: pattern)
            .WithErrorCode(errorCode: Errors.InvalidPattern(prefix: prefix,
                field: field,
                formatDescription: pattern).Code)
            .WithMessage(errorMessage: Errors.InvalidPattern(prefix: prefix,
                field: field,
                formatDescription: pattern,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string> MustMatchPatternRequired<T>(
        this IRuleBuilder<T, string> rb,
        string pattern,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.Matches(expression: pattern)
            .WithErrorCode(errorCode: Errors.InvalidPattern(prefix: prefix,
                field: field,
                formatDescription: pattern).Code)
            .WithMessage(errorMessage: Errors.InvalidPattern(prefix: prefix,
                field: field,
                formatDescription: pattern,
                msg: msg).Description);

    // Generic Input Validation
    public static IRuleBuilderOptions<T, string?> MustBeValidInput<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        int? minLength = null,
        int? maxLength = null,
        string? pattern = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
        .LengthRange(min: minLength ?? Constraints.Text.MinLength,
            max: maxLength ?? Constraints.Text.MaxLength,
            prefix: prefix,
            field: field,
            msg: msg)
        .MustMatchPattern(pattern: pattern ?? Constraints.Text.AllowedPattern,
            prefix: prefix,
            field: field,
            msg: msg);

    public static IRuleBuilderOptions<T, string> MustBeValidInputRequired<T>(
        this IRuleBuilder<T, string> rb,
        string? prefix = null,
        string? field = null,
        int? minLength = null,
        int? maxLength = null,
        string? pattern = null,
        string? msg = null) => rb.Required(prefix: prefix,
            field: field,
            msg: msg)
        .LengthRangeRequired(min: minLength ?? Constraints.Text.MinLength,
            max: maxLength ?? Constraints.Text.MaxLength,
            prefix: prefix,
            field: field,
            msg: msg)
        .MustMatchPatternRequired(pattern: pattern ?? Constraints.Text.AllowedPattern,
            prefix: prefix,
            field: field,
            msg: msg);
    #endregion

    #region Contact
    // Contact Validations
    public static IRuleBuilderOptions<T, string?> MustBeValidEmail<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.Email.MinLength,
                max: Constraints.Email.MaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.Email.Pattern,
                prefix: prefix,
                field: field,
                msg: msg)
            .EmailAddress()
            .WithErrorCode(errorCode: Errors.InvalidEmail(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.InvalidEmail(prefix: prefix,
                field: field,
                msg: msg).Description);
    public static IRuleBuilderOptions<T, string> MustBeValidEmailRequired<T>(
        this IRuleBuilder<T, string> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.Required(prefix: prefix,
                field: field,
                msg: msg)
        .LengthRange(min: Constraints.Email.MinLength,
            max: Constraints.Email.MaxLength,
            prefix: prefix,
            field: field,
            msg: msg)
                    .MustMatchPattern(pattern: Constraints.Email.Pattern,            prefix: prefix,
            field: field,
            msg: msg)
        .EmailAddress()
        .WithErrorCode(errorCode: Errors.InvalidEmail(prefix: prefix,
            field: field).Code)
        .WithMessage(errorMessage: Errors.InvalidEmail(prefix: prefix,
            field: field,
            msg: msg).Description);

    public static IRuleBuilderOptions<T, string?> MustBeValidPhone<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.PhoneNumbers.MinLength,
                max: Constraints.PhoneNumbers.MaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.PhoneNumbers.Pattern,
                prefix: prefix,
                field: field,
                msg: msg);

    public static IRuleBuilderOptions<T, string> MustBeValidPhoneRequired<T>(
        this IRuleBuilder<T, string> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.Required(prefix: prefix,
                field: field,
                msg: msg)
                    .LengthRangeRequired(min: Constraints.PhoneNumbers.MinLength,            max: Constraints.PhoneNumbers.MaxLength,
            prefix: prefix,
            field: field,
            msg: msg)
                    .MustMatchPatternRequired(pattern: Constraints.PhoneNumbers.Pattern,            prefix: prefix,
            field: field,
            msg: msg);


    public static IRuleBuilderOptions<T, string?> MustBeValidPhoneE164<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 2,
                max: Constraints.PhoneNumbers.E164MaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.PhoneNumbers.E164Pattern,
                prefix: prefix,
                field: field,
                msg: msg);

    public static IRuleBuilderOptions<T, string> MustBeValidPhoneE164Required<T>(
        this IRuleBuilder<T, string> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.Required(prefix: prefix,
                field: field,
                msg: msg)
        .LengthRangeRequired(min: 2,
            max: Constraints.PhoneNumbers.E164MaxLength,
            prefix: prefix,
            field: field,
            msg: msg)
                    .MustMatchPatternRequired(pattern: Constraints.PhoneNumbers.E164Pattern,            prefix: prefix,
            field: field,
            msg: msg);

    public static IRuleBuilderOptions<T, string?> MustBeValidUrl<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.UrlAndUri.UrlMinLength,
                max: Constraints.UrlAndUri.UrlMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.UrlAndUri.UrlPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            .Must(predicate: x => string.IsNullOrWhiteSpace(value: x) || Uri.TryCreate(uriString: x,
                uriKind: UriKind.Absolute,
                result: out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
            .WithErrorCode(errorCode: Errors.InvalidUrl(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.InvalidUrl(prefix: prefix,
                field: field,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string> MustBeValidUrlRequired<T>(
        this IRuleBuilder<T, string> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.Required(prefix: prefix,
                field: field,
                msg: msg)
                    .LengthRangeRequired(min: Constraints.UrlAndUri.UrlMinLength,            max: Constraints.UrlAndUri.UrlMaxLength,
            prefix: prefix,
            field: field,
            msg: msg)
                    .MustMatchPatternRequired(pattern: Constraints.UrlAndUri.UrlPattern,            prefix: prefix,
            field: field,
            msg: msg)
        .Must(predicate: x => string.IsNullOrWhiteSpace(value: x) || Uri.TryCreate(uriString: x,
            uriKind: UriKind.Absolute,
            result: out Uri? uri) && (uri.Scheme == Uri.UriSchemeHttp || uri.Scheme == Uri.UriSchemeHttps))
        .WithErrorCode(errorCode: Errors.InvalidUrl(prefix: prefix,
            field: field).Code)
        .WithMessage(errorMessage: Errors.InvalidUrl(prefix: prefix,
            field: field,
            msg: msg).Description);

    public static IRuleBuilderOptions<T, string?> MustBeValidUri<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.UrlAndUri.UriPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            .Must(predicate: x => string.IsNullOrWhiteSpace(value: x) || Uri.TryCreate(uriString: x,
                uriKind: UriKind.Absolute,
                result: out _))
            .WithErrorCode(errorCode: Errors.InvalidUri(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.InvalidUri(prefix: prefix,
                field: field,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string> MustBeValidUriRequired<T>(
        this IRuleBuilder<T, string> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.Required(prefix: prefix,
                field: field,
                msg: msg)
                    .MustMatchPatternRequired(pattern: Constraints.UrlAndUri.UriPattern,            prefix: prefix,
            field: field,
            msg: msg)
        .Must(predicate: x => string.IsNullOrWhiteSpace(value: x) || Uri.TryCreate(uriString: x,
            uriKind: UriKind.Absolute,
            result: out _))
        .WithErrorCode(errorCode: Errors.InvalidUri(prefix: prefix,
            field: field).Code)
        .WithMessage(errorMessage: Errors.InvalidUri(prefix: prefix,
            field: field,
            msg: msg).Description);
    #endregion

    #region User
    // Name and User Validations
    public static IRuleBuilderOptions<T, string?> MustBeValidName<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.NamesAndUsernames.NameMinLength,
                max: Constraints.NamesAndUsernames.NameMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.NamesAndUsernames.NamePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidUsername<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.NamesAndUsernames.UsernameMinLength,
                max: Constraints.NamesAndUsernames.UsernameMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.NamesAndUsernames.UsernamePattern,
                prefix: prefix,
                field: field,
                msg: msg);
    #endregion

    #region Identifier
    // Identifier Validations
    public static IRuleBuilderOptions<T, string?> MustBeValidGuid<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.Identifiers.GuidPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            .Must(predicate: x => string.IsNullOrWhiteSpace(value: x) || Guid.TryParse(input: x,
                result: out _))
            .WithErrorCode(errorCode: Errors.InvalidGuid(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.InvalidGuid(prefix: prefix,
                field: field,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string?> MustBeValidUlid<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 26,
                max: 26,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.Identifiers.UlidPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidNanoId<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 21,
                max: 21,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.Identifiers.NanoIdPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Network
    // Network Validations
    public static IRuleBuilderOptions<T, string?> MustBeValidIpV4<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 7,
                max: Constraints.Network.IpV4MaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.Network.IpV4Pattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidIpV6<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 2,
                max: Constraints.Network.IpV6MaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.Network.IpV6Pattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidMacAddress<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.Network.MacAddressPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidDomain<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.Network.DomainPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Geographic
    // Geographic Validations
    public static IRuleBuilderOptions<T, string?> MustBeValidPostalCode<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 3,
                max: Constraints.GeographicAndPostalCodes.PostalCodeMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.GeographicAndPostalCodes.PostalCodePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidZipCode<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 5,
                max: Constraints.GeographicAndPostalCodes.ZipCodeMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.GeographicAndPostalCodes.ZipCodePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidCanadianPostalCode<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 6,
                max: Constraints.GeographicAndPostalCodes.CanadianPostalCodeMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.GeographicAndPostalCodes.CanadianPostalCodePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidUkPostalCode<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 5,
                max: Constraints.GeographicAndPostalCodes.UkPostalCodeMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.GeographicAndPostalCodes.UkPostalCodePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Security
    // Security Validations
    public static IRuleBuilderOptions<T, string?> MustBeValidPassword<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.Passwords.MinLength,
                max: Constraints.Passwords.MaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.Passwords.StrongPasswordPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Slugs and Versions
    // Slugs and Versions
    public static IRuleBuilderOptions<T, string?> MustBeValidSlug<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.SlugsAndVersions.SlugMinLength,
                max: Constraints.SlugsAndVersions.SlugMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.SlugsAndVersions.SlugPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidSemVer<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.SlugsAndVersions.SemVerPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidVersion<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.SlugsAndVersions.VersionPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Social
    // Social Handles
    public static IRuleBuilderOptions<T, string?> MustBeValidTwitterHandle<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 1,
                max: Constraints.SocialMedia.TwitterHandleMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.SocialMedia.TwitterHandlePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidHashtag<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.SocialMedia.HashtagPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Date and Time
    // Date and Time
    public static IRuleBuilderOptions<T, string?> MustBeValidDate<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 10,
                max: Constraints.DateAndTime.DateMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.DateAndTime.DatePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidTime<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 8,
                max: Constraints.DateAndTime.TimeMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.DateAndTime.TimePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidTimeSpan<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.DateAndTime.TimeSpanPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Data
    // JSON and Boolean
    public static IRuleBuilderOptions<T, string?> MustBeValidJson<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.Json.Pattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidBoolean<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.Boolean.Pattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Financial
    // Financial
    public static IRuleBuilderOptions<T, string?> MustBeValidCreditCard<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 13,
                max: Constraints.PaymentAndCreditCards.CreditCardMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.PaymentAndCreditCards.CreditCardPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            .WithErrorCode(errorCode: Errors.InvalidCreditCard(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.InvalidCreditCard(prefix: prefix,
                field: field,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, string?> MustBeValidCvv<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 3,
                max: Constraints.PaymentAndCreditCards.CvvMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.PaymentAndCreditCards.CvvPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Visual
    // Color
    public static IRuleBuilderOptions<T, string?> MustBeValidHexColor<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 4,
                max: Constraints.Color.HexColorMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.Color.HexColorPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region File
    // File
    public static IRuleBuilderOptions<T, string?> MustBeValidFilePath<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.FileSystem.FilePathPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidFileExtension<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .MustMatchPattern(pattern: Constraints.FileSystem.FileExtensionPattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Localization
    // Currency and Language
    public static IRuleBuilderOptions<T, string?> MustBeValidCurrencyCode<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: Constraints.CurrencyAndLanguage.CurrencyCodeLength,
                max: Constraints.CurrencyAndLanguage.CurrencyCodeLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.CurrencyAndLanguage.CurrencyCodePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;

    public static IRuleBuilderOptions<T, string?> MustBeValidLanguageCode<T>(
        this IRuleBuilder<T, string?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NullableRequired(prefix: prefix,
            field: field,
            msg: msg) : rb)
            .LengthRange(min: 2,
                max: Constraints.CurrencyAndLanguage.LanguageCodeMaxLength,
                prefix: prefix,
                field: field,
                msg: msg)
            .MustMatchPattern(pattern: Constraints.CurrencyAndLanguage.LanguageCodePattern,
                prefix: prefix,
                field: field,
                msg: msg)
            ;
    #endregion

    #region Coordinates
    // Geographic Coordinates
    public static IRuleBuilderOptions<T, decimal?> IsValidLatitude<T>(
        this IRuleBuilder<T, decimal?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NotNull().WithErrorCode(errorCode: Errors.Null(prefix: prefix,
            field: field).Code).WithMessage(errorMessage: Errors.Null(prefix: prefix,
            field: field,
            msg: msg).Description) : rb)
            .InclusiveBetween(from: Constraints.GeographicAndPostalCodes.LatitudeMin,
                to: Constraints.GeographicAndPostalCodes.LatitudeMax)
            .WithErrorCode(errorCode: Errors.InvalidLatitude(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.InvalidLatitude(prefix: prefix,
                field: field,
                msg: msg).Description);

    public static IRuleBuilderOptions<T, decimal?> IsValidLongitude<T>(
        this IRuleBuilder<T, decimal?> rb,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        (isRequired ? rb.NotNull().WithErrorCode(errorCode: Errors.Null(prefix: prefix,
            field: field).Code).WithMessage(errorMessage: Errors.Null(prefix: prefix,
            field: field,
            msg: msg).Description) : rb)
            .InclusiveBetween(from: Constraints.GeographicAndPostalCodes.LongitudeMin,
                to: Constraints.GeographicAndPostalCodes.LongitudeMax)
            .WithErrorCode(errorCode: Errors.InvalidLongitude(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.InvalidLongitude(prefix: prefix,
                field: field,
                msg: msg).Description);
    #endregion

    #region Enum
    public static bool IsValid<TEnum>(TEnum value, bool allowInvalidFlags = false)
      where TEnum : struct, Enum
    {
        if (Enum.IsDefined(enumType: typeof(TEnum),
                value: value))
            return true;

        if (!allowInvalidFlags || typeof(TEnum).GetCustomAttribute<FlagsAttribute>() is null)
            return false;

        long numeric = Convert.ToInt64(value: value);
        long validMask = Enum.GetValues<TEnum>()
            .Select(selector: v => Convert.ToInt64(value: v))
            .Aggregate(seed: 0L,
                func: (acc,
                    next) => acc | next);

        return (numeric & validMask) == numeric;
    }


    public static IRuleBuilderOptions<T, TEnum> MustBeValidEnum<T, TEnum>(
        this IRuleBuilder<T, TEnum> rule,
        string? prefix = null,
        string? field = null,
        bool allowInvalidFlags = false,
        string? message = null)
        where TEnum : struct, Enum
    {
        return rule.Must(predicate: v => IsValid(value: v,
                allowInvalidFlags: allowInvalidFlags))
            .WithErrorCode(
                errorCode: allowInvalidFlags
                    ? Errors.InvalidFlagCombination<TEnum>(prefix: prefix,
                        field: field).Code
                    : Errors.InvalidEnumValue<TEnum>(prefix: prefix,
                        field: field).Code)
            .WithMessage(
                errorMessage: allowInvalidFlags
                    ? Errors.InvalidFlagCombination<TEnum>(prefix: prefix,
                        field: field,
                        message: message).Description
                    : Errors.InvalidEnumValue<TEnum>(prefix: prefix,
                        field: field,
                        message: message).Description);
    }
    #endregion

    #region Dictional 
    public static IRuleBuilderOptions<T, IDictionary<string, object?>?> MustBeValidDictionary<T>(
         this IRuleBuilder<T, IDictionary<string, object?>?> ruleBuilder,
         string? prefix = null,
         string? customMessage = null,
         int? maxEntries = null,
         int? keyMinLength = null,
         int? keyMaxLength = null,
         int? valueMaxLength = null, // Interpreted as max JSON serialized length
         Regex? keyAllowedRegex = null) // Optional: pass null to skip
    {
        maxEntries ??= Constraints.Dictionary.MaxEntries;
        keyMinLength ??= Constraints.Dictionary.KeyMinLength;
        keyMaxLength ??= Constraints.Dictionary.KeyMaxLength;
        valueMaxLength ??= Constraints.Dictionary.ValueMaxLength;
        keyAllowedRegex ??= Constraints.Dictionary.KeyAllowedRegex;

        IRuleBuilderOptions<T, IDictionary<string, object?>?> builder = ruleBuilder
            .Must(d => d == null || d.Count <= maxEntries)
            .WithErrorCode(Errors.TooManyEntries(prefix: prefix).Code)
            .WithMessage(Errors.TooManyEntries(prefix: prefix, msg: customMessage).Description);

        return builder.DependentRules(() =>
        {
            ruleBuilder
                .Must(d => d == null || d.All(kv => !string.IsNullOrWhiteSpace(kv.Key) && kv.Key.Length >= keyMinLength && kv.Key.Length <= keyMaxLength))
                .WithErrorCode(Errors.KeyInvalidLength(prefix: prefix).Code)
                .WithMessage(Errors.KeyInvalidLength(prefix: prefix, msg: customMessage).Description);

            if (keyAllowedRegex != null)
            {
                ruleBuilder
                    .Must(d => d == null || d.All(kv => keyAllowedRegex.IsMatch(kv.Key)))
                    .WithErrorCode(Errors.KeyInvalidPattern(prefix: prefix).Code)
                    .WithMessage(Errors.KeyInvalidPattern(prefix: prefix, msg: customMessage).Description);
            }

            ruleBuilder
                .Must(d => d == null || d.All(kv =>
                {
                    if (kv.Value == null) return true;
                    var valueJson = JsonSerializer.Serialize(kv.Value);
                    return valueJson.Length <= valueMaxLength;
                }))
                .WithErrorCode(Errors.ValueInvalidLength(prefix: prefix).Code)
                .WithMessage(Errors.ValueInvalidLength(prefix: prefix, msg: customMessage).Description);
        }).When((_, dict) => dict != null);
    }
    #endregion

    #region DateTimes

    public static IRuleBuilderOptions<T, DateTimeOffset?> Required<T>(
        this IRuleBuilder<T, DateTimeOffset?> rb,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        rb.NotNull()
            .WithErrorCode(errorCode: CommonInput.Errors.Null(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: CommonInput.Errors.Null(prefix: prefix,
                field: field,
                msg: msg).Description)
            .NotEmpty()
            .WithErrorCode(errorCode: CommonInput.Errors.NullOrEmpty(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: CommonInput.Errors.NullOrEmpty(prefix: prefix,
                field: field,
                msg: msg).Description);


    /// <summary>
    /// Validates that a non-nullable <see cref="DateTimeOffset"/> is within the allowed range.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustBeValidDateTimeOffsetRequired<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder,
        string? prefix = null,
        string? field = null,
        DateTimeOffset? minDate = null,
        DateTimeOffset? maxDate = null,
        bool exclusive = false,
        string? customMessage = null)
    {
        DateTimeOffset min = minDate ?? DateTimeOffset.MinValue;
        DateTimeOffset max = maxDate ?? DateTimeOffset.MaxValue;

        return exclusive
            ? ruleBuilder
                .GreaterThan(valueToCompare: min)
                .WithErrorCode(errorCode: Errors.DateOffsetOutOfExclusiveRange(prefix: prefix,
                    field: field).Code)
                .WithMessage(errorMessage: Errors.DateOffsetOutOfExclusiveRange(prefix: prefix,
                    field: field,
                    min: min,
                    max: max,
                    customMessage: customMessage).Description)
                .LessThan(valueToCompare: max)
                .WithErrorCode(errorCode: Errors.DateOffsetOutOfExclusiveRange(prefix: prefix,
                    field: field).Code)
                .WithMessage(errorMessage: Errors.DateOffsetOutOfExclusiveRange(prefix: prefix,
                    field: field,
                    min: min,
                    max: max,
                    customMessage: customMessage).Description)
            : ruleBuilder
                .GreaterThanOrEqualTo(valueToCompare: min)
                .WithErrorCode(errorCode: Errors.DateOffsetOutOfRange(prefix: prefix,
                    field: field).Code)
                .WithMessage(errorMessage: Errors.DateOffsetOutOfRange(prefix: prefix,
                    field: field,
                    min: min,
                    max: max,
                    customMessage: customMessage).Description)
                .LessThanOrEqualTo(valueToCompare: max)
                .WithErrorCode(errorCode: Errors.DateOffsetOutOfRange(prefix: prefix,
                    field: field).Code)
                .WithMessage(errorMessage: Errors.DateOffsetOutOfRange(prefix: prefix,
                    field: field,
                    min: min,
                    max: max,
                    customMessage: customMessage).Description);
    }

    /// <summary>
    /// Validates that a nullable <see cref="DateTimeOffset"/> value, if provided, is within the allowed range.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset?> MustBeValidDateTimeOffset<T>(
        this IRuleBuilder<T, DateTimeOffset?> ruleBuilder,
        string? prefix = null,
        string? field = null,
        DateTimeOffset? minDate = null,
        DateTimeOffset? maxDate = null,
        bool exclusive = false,
        string? customMessage = null)
    {
        DateTimeOffset min = minDate ?? DateTimeOffset.MinValue;
        DateTimeOffset max = maxDate ?? DateTimeOffset.MaxValue;

        if (exclusive)
        {
            return ruleBuilder
                .Must(predicate: v => !v.HasValue || v.Value > min && v.Value < max)
                .WithErrorCode(errorCode: Errors.DateOffsetOutOfExclusiveRange(prefix: prefix,
                    field: field).Code)
                .WithMessage(errorMessage: Errors.DateOffsetOutOfExclusiveRange(prefix: prefix,
                    field: field,
                    min: min,
                    max: max,
                    customMessage: customMessage).Description);
        }
        else
        {
            return ruleBuilder
                .Must(predicate: v => !v.HasValue || v.Value >= min && v.Value <= max)
                .WithErrorCode(errorCode: Errors.DateOffsetOutOfRange(prefix: prefix,
                    field: field).Code)
                .WithMessage(errorMessage: Errors.DateOffsetOutOfRange(prefix: prefix,
                    field: field,
                    min: min,
                    max: max,
                    customMessage: customMessage).Description);
        }
    }

    /// <summary>
    /// Validates that a <see cref="DateTimeOffset"/> is in the future.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustBeInFuture<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder,
        string? prefix = null,
        string? field = null,
        string? customMessage = null) =>
        ruleBuilder
            .Must(predicate: v => v.ToUniversalTime() > DateTimeOffset.UtcNow)
            .WithErrorCode(errorCode: Errors.MustBeInFuture(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.MustBeInFuture(prefix: prefix,
                field: field,
                customMessage: customMessage).Description);

    /// <summary>
    /// Validates that a nullable <see cref="DateTimeOffset"/> is in the future if provided.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset?> MustBeInFutureOptional<T>(
        this IRuleBuilder<T, DateTimeOffset?> ruleBuilder,
        string? prefix = null,
        string? field = null,
        string? customMessage = null) =>
        ruleBuilder
            .Must(predicate: v => !v.HasValue || v.Value.ToUniversalTime() > DateTimeOffset.UtcNow)
            .WithErrorCode(errorCode: Errors.MustBeInFuture(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.MustBeInFuture(prefix: prefix,
                    field: field,
                    customMessage: customMessage)
                .Description);
    /// <summary>
    /// Validates that a <see cref="DateTimeOffset"/> is in the past.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustBeInPast<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder,
        string? prefix = null,
        string? field = null,
        string? customMessage = null) =>
        ruleBuilder
            .Must(predicate: v => v.ToUniversalTime() < DateTimeOffset.UtcNow)
            .WithErrorCode(errorCode: Errors.MustBeInPast(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.MustBeInPast(prefix: prefix,
                field: field,
                customMessage: customMessage).Description);

    /// <summary>
    /// Validates that a nullable <see cref="DateTimeOffset"/> is in the past if provided.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset?> MustBeInPastOptional<T>(
        this IRuleBuilder<T, DateTimeOffset?> ruleBuilder,
        string? prefix = null,
        string? field = null,
        string? customMessage = null) =>
        ruleBuilder
            .Must(predicate: v => !v.HasValue || v.Value.ToUniversalTime() < DateTimeOffset.UtcNow)
            .WithErrorCode(errorCode: Errors.MustBeInPast(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.MustBeInPast(prefix: prefix,
                field: field,
                customMessage: customMessage).Description);

    /// <summary>
    /// Validates that a DateTimeOffset is in UTC.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset> MustBeUtc<T>(
        this IRuleBuilder<T, DateTimeOffset> ruleBuilder,
        string? prefix = null,
        string? field = null,
        string? msg = null) =>
        ruleBuilder
            .Must(predicate: v => v.Offset == TimeSpan.Zero)
            .WithErrorCode(errorCode: Errors.DateOffsetOutOfRange(prefix: prefix,
                field: field,
                min: DateTimeOffset.UtcNow,
                max: DateTimeOffset.UtcNow).Code)
            .WithMessage(errorMessage: msg ?? $"{Label(prefix: prefix, field: field)} must be in UTC.");

    /// <summary>
    /// Validates that a nullable DateTimeOffset is in UTC if provided.
    /// </summary>
    public static IRuleBuilderOptions<T, DateTimeOffset?> MustBeUtcOptional<T>(
        this IRuleBuilder<T, DateTimeOffset?> ruleBuilder,
        string? prefix = null,
        string? field = null,
        string? customMessage = null) =>
        ruleBuilder
            .Must(predicate: v => !v.HasValue || v.Value.Offset == TimeSpan.Zero)
            .WithErrorCode(errorCode: Errors.DateOffsetOutOfRange(prefix: prefix,
                field: field,
                min: DateTimeOffset.UtcNow,
                max: DateTimeOffset.UtcNow).Code)
            .WithMessage(errorMessage: customMessage ?? $"{Label(prefix: prefix, field: field)} must be in UTC if provided.");

    #endregion

    #region Collections

    public static IRuleBuilderOptions<T, IEnumerable<TItem>> MustBeValidCollectionRequired<T, TItem>(
        this IRuleBuilder<T, IEnumerable<TItem>> ruleBuilder,
        string? prefix = null,
        string? field = null,
        int? minItems = null,
        int? maxItems = null,
        bool allowDuplicates = false,
        string? customMessage = null) => ruleBuilder
            .NotEmpty()
            .WithErrorCode(errorCode: Errors.EmptyCollection(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.EmptyCollection(prefix: prefix,
                field: field,
                customMessage: customMessage).Description)
            .Must(predicate: c => c.Count() >= (minItems ?? Constraints.Text.MinLength))
            .WithErrorCode(errorCode: Errors.TooFewItems(prefix: prefix,
                field: field).Code)
            .WithMessage(messageProvider: _ => Errors.TooFewItems(prefix: prefix,
                field: field,
                min: minItems ?? 0,
                msg: customMessage).Description)
            .Must(predicate: c => c.Count() <= (maxItems ?? Constraints.Text.MaxLength))
            .WithErrorCode(errorCode: Errors.TooManyItems(prefix: prefix,
                field: field).Code)
            .WithMessage(messageProvider: _ => Errors.TooManyItems(prefix: prefix,
                field: field,
                max: maxItems ?? Constraints.Text.MaxLength,
                customMessage: customMessage).Description)
            .Must(predicate: c =>
            {
                IEnumerable<TItem> enumerable = c as TItem[] ?? c.ToArray();
                return allowDuplicates || enumerable.Distinct().Count() == enumerable.Count();
            })
            .WithErrorCode(errorCode: Errors.DuplicateItems(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.DuplicateItems(prefix: prefix,
                field: field,
                customMessage: customMessage).Description);

    public static IRuleBuilderOptions<T, IEnumerable<TItem>?> MustBeValidCollection<T, TItem>(
        this IRuleBuilder<T, IEnumerable<TItem>?> ruleBuilder,
        string? prefix = null,
        string? field = null,
        int? minItems = null,
        int? maxItems = null,
        bool allowDuplicates = false,
        bool isRequired = Constraints.General.IsRequiredByDefault,
        string? customMessage = null)
    {
        // The When(predicate, action) syntax is for use inside AbstractValidator, not for IRuleBuilder extension methods.
        // The logic is rewritten to apply validation conditionally.
        if (isRequired)
        {
            // If required, chain NotEmpty first. The subsequent Must calls will have a non-null collection.
            return ruleBuilder
                .NotEmpty()
                    .WithErrorCode(errorCode: Errors.EmptyCollection(prefix: prefix,
                    field: field).Code)
                    .WithMessage(errorMessage: Errors.EmptyCollection(prefix: prefix,
                    field: field,
                    customMessage: customMessage).Description)
                .Must(predicate: c => c!.Count() >= (minItems ?? Constraints.Text.MinLength))
                    .WithErrorCode(errorCode: Errors.TooFewItems(prefix: prefix,
                    field: field).Code)
                    .WithMessage(messageProvider: _ => Errors.TooFewItems(prefix: prefix,
                    field: field,
                    min: minItems ?? Constraints.Text.MinLength,
                    msg: customMessage).Description)
                .Must(predicate: c => c!.Count() <= (maxItems ?? Constraints.Text.MaxLength))
                    .WithErrorCode(errorCode: Errors.TooManyItems(prefix: prefix,
                    field: field).Code)
                    .WithMessage(messageProvider: _ => Errors.TooManyItems(prefix: prefix,
                    field: field,
                    max: maxItems ?? Constraints.Text.MaxLength,
                    customMessage: customMessage).Description)
                .Must(predicate: c => c != null && (allowDuplicates || c.Distinct().Count() == c.Count()))
                    .WithErrorCode(errorCode: Errors.DuplicateItems(prefix: prefix,
                    field: field).Code)
                    .WithMessage(errorMessage: Errors.DuplicateItems(prefix: prefix,
                    field: field,
                    customMessage: customMessage).Description);
        }

        // If not required, the collection can be null. The rules should only execute if the collection is not null.
        return ruleBuilder
            .Must(predicate: c => c == null || c.Count() >= (minItems ?? Constraints.Text.MinLength))
            .WithErrorCode(errorCode: Errors.TooFewItems(prefix: prefix,
                field: field).Code)
            .WithMessage(messageProvider: _ =>
                Errors.TooFewItems(prefix: prefix,
                        field: field,
                        min: minItems ?? Constraints.Text.MinLength,
                        msg: customMessage)
                    .Description)
            .Must(predicate: c => c == null || c.Count() <= (maxItems ?? Constraints.Text.MaxLength))
            .WithErrorCode(errorCode: Errors.TooManyItems(prefix: prefix,
                field: field).Code)
            .WithMessage(messageProvider: _ =>
                Errors.TooManyItems(prefix: prefix,
                        field: field,
                        max: maxItems ?? Constraints.Text.MaxLength,
                        customMessage: customMessage)
                    .Description)
            .Must(predicate: c => c == null || allowDuplicates || c.Distinct().Count() == c.Count())
            .WithErrorCode(errorCode: Errors.DuplicateItems(prefix: prefix,
                field: field).Code)
            .WithMessage(errorMessage: Errors.DuplicateItems(prefix: prefix,
                field: field,
                customMessage: customMessage).Description);
    }

    #endregion    
    #endregion

}
