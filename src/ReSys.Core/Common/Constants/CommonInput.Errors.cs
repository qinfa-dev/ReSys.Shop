using ErrorOr;

using ReSys.Core.Common.Extensions;

namespace ReSys.Core.Common.Constants;

public static partial class CommonInput
{
    #region Errors

    public static class Errors
    {
        #region Generic

        public static Error Required(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(Required)}",
                description: msg ?? string.Format(ValidationMessages.General.Required, Label(prefix: prefix, field: field)));
        // Generic Business Errors
        public static Error NotFound(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(NotFound)}",
                description: msg ?? string.Format(ValidationMessages.General.NotFound, Label(prefix: prefix, field: field)));

        public static Error AlreadyExists(string? prefix = null, string? field = null, string? identifier = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(AlreadyExists)}",
                description: msg ?? string.Format(ValidationMessages.General.AlreadyExists, Label(prefix: prefix, field: field), identifier));

        public static Error Conflict(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(Conflict)}",
                description: msg ?? string.Format(ValidationMessages.General.Conflict, Label(prefix: prefix, field: field), string.Empty));

        public static Error InvalidOperation(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidOperation)}",
                description: msg ?? string.Format(ValidationMessages.General.InvalidOperation, Label(prefix: prefix, field: field), string.Empty));

        public static Error NotAuthorized(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(NotAuthorized)}",
                description: msg ?? string.Format(ValidationMessages.General.NotAuthorized, Label(prefix: prefix, field: field)));

        public static Error Forbidden(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(Forbidden)}",
                description: msg ?? string.Format(ValidationMessages.General.Forbidden, Label(prefix: prefix, field: field)));

        public static Error RelationshipConstraintViolation(string? prefix = null, string? field = null, string? action = null, string? relatedEntity = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(RelationshipConstraintViolation)}",
                description: msg ?? string.Format(ValidationMessages.General.RelationshipConstraintViolation, action, relatedEntity));

        public static Error InsufficientPermissions(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InsufficientPermissions)}",
                description: msg ?? string.Format(ValidationMessages.General.InsufficientPermissions, Label(prefix: prefix, field: field)));

        public static Error ServiceUnavailable(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(ServiceUnavailable)}",
                description: msg ?? ValidationMessages.General.ServiceUnavailable);

        public static Error RateLimitExceeded(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(RateLimitExceeded)}",
                description: msg ?? ValidationMessages.General.RateLimitExceeded);

        public static Error FeatureDisabled(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(FeatureDisabled)}",
                description: msg ?? string.Format(ValidationMessages.General.FeatureDisabled, Label(prefix: prefix, field: field)));

        public static Error Null(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(Null)}",
                description: msg ?? string.Format(ValidationMessages.General.Null, Label(prefix: prefix, field: field)));

        public static Error NullOrEmpty(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(NullOrEmpty)}",
                description: msg ?? string.Format(ValidationMessages.General.NullOrEmpty, Label(prefix: prefix, field: field)));

        public static Error TooShort(string? prefix = null, string? field = null, int minLength = Constraints.Text.MinLength,
            string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(TooShort)}",
                description: msg ??
                             string.Format(ValidationMessages.General.TooShort, Label(prefix: prefix, field: field), minLength));

        public static Error TooLong(string? prefix = null, string? field = null, int maxLength = Constraints.Text.MaxLength,
            string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(TooLong)}",
                description: msg ??
                             string.Format(ValidationMessages.General.TooLong, Label(prefix: prefix, field: field), maxLength));

        public static Error InvalidRange(string? prefix = null, string? field = null, object? min = null,
            object? max = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidRange)}",
                description: msg ?? string.Format(ValidationMessages.General.InvalidRange, Label(prefix: prefix, field: field), min, max));

        public static Error InvalidPattern(string? prefix = null, string? field = null,
            string? formatDescription = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidPattern)}",
                description: msg ??
                             string.Format(ValidationMessages.Text.InvalidPattern, Label(prefix: prefix, field: field), formatDescription != null ? $" ({formatDescription})" : ""));

        public static Error InvalidAllowedPattern(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidAllowedPattern)}",
                description: msg ??
                             string.Format(ValidationMessages.Text.InvalidAllowedPattern, Label(prefix: prefix, field: field)));

        public static Error InvalidValue(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidValue)}",
                description: msg ?? string.Format(ValidationMessages.General.InvalidValue, Label(prefix: prefix, field: field)));

        #endregion

        #region Contact

        public static Error InvalidEmail(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidEmail)}",
                description: msg ?? string.Format(ValidationMessages.Contact.InvalidEmail, Label(prefix: prefix, field: field)));

        public static Error InvalidPhone(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidPhone)}",
                description: msg ?? string.Format(ValidationMessages.Contact.InvalidPhone, Label(prefix: prefix, field: field)));

        public static Error InvalidUrl(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidUrl)}",
                description: msg ?? string.Format(ValidationMessages.Contact.InvalidUrl, Label(prefix: prefix, field: field)));

        public static Error InvalidUri(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidUri)}",
                description: msg ?? string.Format(ValidationMessages.Contact.InvalidUri, Label(prefix: prefix, field: field)));

        #endregion

        #region User

        public static Error InvalidName(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidName)}",
                description: msg ?? string.Format(ValidationMessages.User.InvalidName, Label(prefix: prefix, field: field)));

        public static Error InvalidUsername(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidUsername)}",
                description: msg ??
                             string.Format(ValidationMessages.User.InvalidUsername, Label(prefix: prefix, field: field)));

        #endregion

        #region Identifier

        public static Error InvalidGuid(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidGuid)}",
                description: msg ?? string.Format(ValidationMessages.Identifier.InvalidGuid, Label(prefix: prefix, field: field)));

        public static Error InvalidUlid(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidUlid)}",
                description: msg ?? string.Format(ValidationMessages.Identifier.InvalidUlid, Label(prefix: prefix, field: field)));

        public static Error InvalidNanoId(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidNanoId)}",
                description: msg ?? string.Format(ValidationMessages.Identifier.InvalidNanoId, Label(prefix: prefix, field: field)));

        #endregion

        #region Network

        public static Error InvalidIpAddress(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidIpAddress)}",
                description: msg ??
                             string.Format(ValidationMessages.Network.InvalidIpAddress, Label(prefix: prefix, field: field)));

        public static Error InvalidMacAddress(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidMacAddress)}",
                description: msg ??
                             string.Format(ValidationMessages.Network.InvalidMacAddress, Label(prefix: prefix, field: field)));

        public static Error InvalidDomain(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidDomain)}",
                description: msg ?? string.Format(ValidationMessages.Network.InvalidDomain, Label(prefix: prefix, field: field)));

        #endregion

        #region Geographic

        public static Error InvalidPostalCode(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidPostalCode)}",
                description: msg ??
                             string.Format(ValidationMessages.Geographic.InvalidPostalCode, Label(prefix: prefix, field: field)));

        public static Error InvalidZipCode(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidZipCode)}",
                description: msg ??
                             string.Format(ValidationMessages.Geographic.InvalidZipCode, Label(prefix: prefix, field: field)));

        public static Error
            InvalidCanadianPostalCode(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidCanadianPostalCode)}",
                description: msg ??
                             string.Format(ValidationMessages.Geographic.InvalidCanadianPostalCode, Label(prefix: prefix, field: field)));

        public static Error InvalidUkPostalCode(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidUkPostalCode)}",
                description: msg ??
                             string.Format(ValidationMessages.Geographic.InvalidUkPostalCode, Label(prefix: prefix, field: field)));

        #endregion

        #region Security

        public static Error InvalidPassword(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidPassword)}",
                description: msg ??
                             string.Format(ValidationMessages.Security.InvalidPassword, Label(prefix: prefix, field: field)));

        #endregion

        #region Slugs and Versions

        public static Error InvalidSlug(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidSlug)}",
                description: msg ?? string.Format(ValidationMessages.SlugsAndVersions.InvalidSlug, Label(prefix: prefix, field: field)));

        public static Error InvalidSemVer(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidSemVer)}",
                description: msg ??
                             string.Format(ValidationMessages.SlugsAndVersions.InvalidSemVer, Label(prefix: prefix, field: field)));

        public static Error InvalidVersion(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidVersion)}",
                description: msg ??
                             string.Format(ValidationMessages.SlugsAndVersions.InvalidVersion, Label(prefix: prefix, field: field)));

        #endregion

        #region Social

        public static Error InvalidTwitterHandle(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidTwitterHandle)}",
                description: msg ??
                             string.Format(ValidationMessages.Social.InvalidTwitterHandle, Label(prefix: prefix, field: field)));

        public static Error InvalidHashtag(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidHashtag)}",
                description: msg ??
                             string.Format(ValidationMessages.Social.InvalidHashtag, Label(prefix: prefix, field: field)));

        #endregion

        #region Date and Time

        public static Error InvalidDate(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidDate)}",
                description: msg ?? string.Format(ValidationMessages.DateAndTime.InvalidDate, Label(prefix: prefix, field: field)));

        public static Error InvalidTime(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidTime)}",
                description: msg ?? string.Format(ValidationMessages.DateAndTime.InvalidTime, Label(prefix: prefix, field: field)));

        public static Error InvalidTimeSpan(string? prefix = null, string? field = null, string? msg = null) =>

            Error.Validation(

                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidTimeSpan)}",

                description: msg ??

                             string.Format(ValidationMessages.DateAndTime.InvalidTimeSpan, Label(prefix: prefix, field: field)));

        public static Error DateOffsetOutOfRange(
            string? prefix = null,
            string? field = null,
            DateTimeOffset? min = null,
            DateTimeOffset? max = null,
            string? customMessage = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(DateOffsetOutOfRange)}",
                description: customMessage ??
                             string.Format(ValidationMessages.DateAndTime.DateOffsetOutOfRange, Label(prefix: prefix, field: field), min.FormatUtc(), max.FormatUtc()));

        public static Error DateOffsetOutOfExclusiveRange(
            string? prefix = null,
            string? field = null,
            DateTimeOffset? min = null,
            DateTimeOffset? max = null,
            string? customMessage = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(DateOffsetOutOfExclusiveRange)}",
                description: customMessage ??
                             string.Format(ValidationMessages.DateAndTime.DateOffsetOutOfExclusiveRange, Label(prefix: prefix, field: field), min.FormatUtc(), max.FormatUtc()));

        public static Error MustBeInFuture(string? prefix = null, string? field = null, string? customMessage = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(MustBeInFuture)}",
                description: customMessage ??
                             string.Format(ValidationMessages.DateAndTime.MustBeInFuture, Label(prefix: prefix, field: field)));

        public static Error MustBeInPast(string? prefix = null, string? field = null, string? customMessage = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(MustBeInPast)}",
                description: customMessage ??
                             string.Format(ValidationMessages.DateAndTime.MustBeInPast, Label(prefix: prefix, field: field)));

        #endregion

        #region Data

        public static Error InvalidJson(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidJson)}",
                description: msg ?? string.Format(ValidationMessages.Data.InvalidJson, Label(prefix: prefix, field: field)));

        public static Error InvalidBoolean(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidBoolean)}",
                description: msg ??
                             string.Format(ValidationMessages.Data.InvalidBoolean, Label(prefix: prefix, field: field)));

        #endregion

        #region Coordinates

        public static Error InvalidLatitude(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidLatitude)}",
                description: msg ??
                             string.Format(ValidationMessages.Geographic.InvalidLatitude, Label(prefix: prefix, field: field)));

        public static Error InvalidLongitude(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidLongitude)}",
                description: msg ??
                             string.Format(ValidationMessages.Geographic.InvalidLongitude, Label(prefix: prefix, field: field)));

        #endregion

        #region Financial

        public static Error InvalidCreditCard(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidCreditCard)}",
                description: msg ??
                             string.Format(ValidationMessages.Financial.InvalidCreditCard, Label(prefix: prefix, field: field)));

        public static Error InvalidCvv(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidCvv)}",
                description: msg ?? string.Format(ValidationMessages.Financial.InvalidCvv, Label(prefix: prefix, field: field)));

        #endregion

        #region Visual

        public static Error InvalidHexColor(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidHexColor)}",
                description: msg ??
                             string.Format(ValidationMessages.Visual.InvalidHexColor, Label(prefix: prefix, field: field)));

        #endregion

        #region File

        public static Error InvalidFilePath(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidFilePath)}",
                description: msg ??
                             string.Format(ValidationMessages.File.InvalidFilePath, Label(prefix: prefix, field: field)));

        public static Error InvalidFileExtension(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidFileExtension)}",
                description: msg ??
                             string.Format(ValidationMessages.File.InvalidFileExtension, Label(prefix: prefix, field: field)));

        #endregion

        #region Localization

        public static Error InvalidCurrencyCode(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidCurrencyCode)}",
                description: msg ??
                             string.Format(ValidationMessages.Localization.InvalidCurrencyCode, Label(prefix: prefix, field: field)));

        public static Error InvalidLanguageCode(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidLanguageCode)}",
                description: msg ??
                             string.Format(ValidationMessages.Localization.InvalidLanguageCode, Label(prefix: prefix, field: field)));

        #endregion

        #region Numeric

        public static Error InvalidInteger(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidInteger)}",
                description: msg ??
                             string.Format(ValidationMessages.Numeric.InvalidInteger, Label(prefix: prefix, field: field)));

        public static Error InvalidDecimal(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(code: $"{Prefix(prefix: prefix, field: field)}.{nameof(InvalidDecimal)}",
                description: msg ??
                             string.Format(ValidationMessages.Numeric.InvalidDecimal, Label(prefix: prefix, field: field)));

        public static Error OutOfRange(string? prefix = null, string? field = null, object? minValue = null,
            object? maxValue = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(OutOfRange)}",
                description: msg ??
                             string.Format(ValidationMessages.Numeric.OutOfRange, Label(prefix: prefix, field: field), minValue, maxValue));

        #endregion

        #region Text Content

        public static Error TitleTooLong(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(TitleTooLong)}",
                description: msg ??
                             string.Format(ValidationMessages.Text.TitleTooLong, Label(prefix: prefix, field: field), Constraints.Text.TitleMaxLength));

        public static Error DescriptionTooLong(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(DescriptionTooLong)}",
                description: msg ??
                             string.Format(ValidationMessages.Text.DescriptionTooLong, Label(prefix: prefix, field: field), Constraints.Text.DescriptionMaxLength));

        public static Error CommentTooLong(string? prefix = null, string? field = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(CommentTooLong)}",
                description: msg ??
                             string.Format(ValidationMessages.Text.CommentTooLong, Label(prefix: prefix, field: field), Constraints.Text.CommentMaxLength));

        #endregion
        #region Dictionary

        public static Error TooManyEntries(string? prefix = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix)}.TooManyEntries",
                description: msg ??
                             string.Format(ValidationMessages.Dictionary.TooManyEntries, Label(prefix: prefix), Constraints.Dictionary.MaxEntries));

        public static Error KeyRequired(string? prefix = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix)}.KeyRequired",
                description: msg ?? string.Format(ValidationMessages.Dictionary.KeyRequired, Label(prefix: prefix)));

        public static Error KeyInvalidPattern(string? prefix = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix)}.KeyInvalidPattern",
                description: msg ??
                             string.Format(ValidationMessages.Dictionary.KeyInvalidPattern, Label(prefix: prefix), Constraints.Dictionary.KeyAllowedPattern));

        public static Error KeyInvalidLength(string? prefix = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix)}.KeyInvalidLength",
                description: msg ??
                             string.Format(ValidationMessages.Dictionary.KeyInvalidLength, Label(prefix: prefix), Constraints.Dictionary.KeyMinLength, Constraints.Dictionary.KeyMaxLength));


        public static Error ValueInvalidLength(string? prefix = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix)}.ValueInvalidLength",
                description: msg ??
                             string.Format(ValidationMessages.Dictionary.ValueInvalidLength, Label(prefix: prefix), Constraints.Dictionary.ValueMinLength, Constraints.Dictionary.ValueMaxLength));


        #endregion

        #region Enum

        public static Error InvalidEnumValue<TEnum>(
            string? prefix = null,
            string? field = null,
            string? message = null)
            where TEnum : struct, Enum
        {
            string enumName = typeof(TEnum).Name;
            string validValues = EnumDescriptionExtensions.GetEnumContextDescription<TEnum>();
            return Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field ?? enumName)}.InvalidEnumValue",
                description: message ?? string.Format(ValidationMessages.Enum.InvalidEnumValue, enumName, validValues));
        }

        /// <summary>
        /// Creates a typed invalid flag combination error, automatically including valid bitmask descriptions.
        /// </summary>
        public static Error InvalidFlagCombination<TEnum>(
            string? prefix = null,
            string? field = null,
            string? message = null)
            where TEnum : struct, Enum
        {
            string enumName = typeof(TEnum).Name;
            string validFlags = EnumDescriptionExtensions.GetEnumContextDescription<TEnum>();
            return Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field ?? enumName)}.InvalidFlagCombination",
                description: message ?? string.Format(ValidationMessages.Enum.InvalidFlagCombination, enumName, validFlags));
        }

        #region Collections

        public static Error EmptyCollection(string? prefix = null, string? field = null, string? customMessage = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(EmptyCollection)}",
                description: customMessage ?? string.Format(ValidationMessages.Collections.EmptyCollection, Label(prefix: prefix, field: field)));

        public static Error TooFewItems(string? prefix = null, string? field = null, object? min = null, string? msg = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(TooFewItems)}",
                description: msg ?? string.Format(ValidationMessages.Collections.TooFewItems, Label(prefix: prefix, field: field), min));


        public static Error TooManyItems(string? prefix = null, string? field = null, long max = long.MaxValue, string? customMessage = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(TooManyItems)}",
                description: customMessage ?? string.Format(ValidationMessages.Collections.TooManyItems, Label(prefix: prefix, field: field), max));


        public static Error DuplicateItems(string? prefix = null, string? field = null, string? customMessage = null) =>
            Error.Validation(
                code: $"{Prefix(prefix: prefix, field: field)}.{nameof(DuplicateItems)}",
                description: customMessage ?? string.Format(ValidationMessages.Collections.DuplicateItems, Label(prefix: prefix, field: field)));

        #endregion
        #endregion
        #endregion


    }
}