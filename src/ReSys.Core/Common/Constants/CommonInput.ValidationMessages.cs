namespace ReSys.Core.Common.Constants;

public static partial class CommonInput
{
    public static class ValidationMessages
    {
        public static class General
        {
            public const string InvalidInputCode = "Validation.InvalidInput";
            public const string InvalidValue = "{0} has an invalid value.";
            public const string Required = "{0} is required.";
            public const string Null = "{0} cannot be null.";
            public const string NullOrEmpty = "{0} must have a value.";
            public const string TooShort = "{0} must be at least {1} characters.";
            public const string TooLong = "{0} cannot exceed {1} characters.";
            public const string InvalidRange = "{0} must be between {1} and {2}.";
        }

        public static class Text
        {
            public const string InvalidPatternCode = "Validation.InvalidPattern";
            public const string InvalidPattern = "{0} does not match required pattern{1}.";
            public const string InvalidAllowedPattern = "{0} does not match the allowed pattern.";
            public const string TitleTooLong = "{0} title cannot exceed {1} characters.";
            public const string DescriptionTooLong = "{0} description cannot exceed {1} characters.";
            public const string CommentTooLong = "{0} comment cannot exceed {1} characters.";
        }

        public static class Numeric
        {
            public const string InvalidInteger = "{0} integer format is invalid.";
            public const string InvalidDecimal = "{0} decimal format is invalid.";
            public const string OutOfRange = "{0} must be between {1} and {2}.";
            public const string NegativeValue = "{0} cannot be negative.";
            public const string ZeroValue = "{0} cannot be zero.";
            public const string MustBePositive = "{0} must be a positive value.";
            public const string MustBeGreaterThan = "{0} must be greater than {1}.";
            public const string MustBeLessThan = "{0} must be less than {1}.";
        }

        public static class Contact
        {
            public const string InvalidEmailCode = "Validation.InvalidEmail";
            public const string InvalidEmail = "{0} email format is invalid.";
            public const string InvalidPhone = "{0} phone format is invalid.";
            public const string InvalidUrl = "{0} URL format is invalid.";
            public const string InvalidUri = "{0} URI format is invalid.";
        }

        public static class User
        {
            public const string InvalidName = "{0} name format is invalid.";
            public const string InvalidUsername = "{0} username format is invalid.";
        }

        public static class Identifier
        {
            public const string InvalidGuid = "{0} GUID format is invalid.";
            public const string InvalidUlid = "{0} ULID format is invalid.";
            public const string InvalidNanoId = "{0} NanoID format is invalid.";
        }

        public static class Network
        {
            public const string InvalidIpAddress = "{0} IP address format is invalid.";
            public const string InvalidMacAddress = "{0} MAC address format is invalid.";
            public const string InvalidDomain = "{0} domain format is invalid.";
        }

        public static class Geographic
        {
            public const string InvalidPostalCode = "{0} postal code format is invalid.";
            public const string InvalidZipCode = "{0} ZIP code format is invalid.";
            public const string InvalidCanadianPostalCode = "{0} Canadian postal code format is invalid.";
            public const string InvalidUkPostalCode = "{0} UK postal code format is invalid.";
            public const string InvalidLatitude = "{0} latitude must be between -90 and 90.";
            public const string InvalidLongitude = "{0} longitude must be between -180 and 180.";
        }

        public static class Security
        {
            public const string InvalidPassword = "{0} password format is invalid.";
        }

        public static class SlugsAndVersions
        {
            public const string InvalidSlug = "{0} slug format is invalid.";
            public const string InvalidSemVer = "{0} semantic version format is invalid.";
            public const string InvalidVersion = "{0} version format is invalid.";
        }

        public static class Social
        {
            public const string InvalidTwitterHandle = "{0} Twitter handle format is invalid.";
            public const string InvalidHashtag = "{0} hashtag format is invalid.";
        }

        public static class DateAndTime
        {
            public const string InvalidDate = "{0} date format is invalid.";
            public const string InvalidTime = "{0} time format is invalid.";
            public const string InvalidTimeSpan = "{0} time span format is invalid.";
            public const string DateOffsetOutOfRange = "{0} must be between {1} and {2}.";
            public const string DateOffsetOutOfExclusiveRange = "{0} must be strictly between {1} and {2}.";
            public const string MustBeInFuture = "{0} must be in the future.";
            public const string MustBeInPast = "{0} must be in the past.";
        }

        public static class Data
        {
            public const string InvalidJson = "{0} JSON format is invalid.";
            public const string InvalidBoolean = "{0} boolean format is invalid.";
        }

        public static class Financial
        {
            public const string InvalidCreditCard = "{0} credit card format is invalid.";
            public const string InvalidCvv = "{0} CVV format is invalid.";
            public const string InvalidAmount = "{0} amount format is invalid.";
            public const string NegativeAmount = "{0} cannot be a negative amount.";
            public const string AmountTooHigh = "{0} amount exceeds the maximum allowed.";
        }

        public static class Visual
        {
            public const string InvalidHexColor = "{0} hex color format is invalid.";
        }

        public static class File
        {
            public const string InvalidFilePath = "{0} file path format is invalid.";
            public const string InvalidFileExtension = "{0} file extension format is invalid.";
        }

        public static class Localization
        {
            public const string InvalidCurrencyCode = "{0} currency code format is invalid.";
            public const string InvalidLanguageCode = "{0} language code format is invalid.";
        }

        public static class Dictionary
        {
            public const string TooManyEntries = "{0} contains more than {1} entries.";
            public const string KeyRequired = "{0} key is required and cannot be empty.";
            public const string KeyInvalidPattern = "{0} key must match the allowed pattern {1}.";
            public const string KeyInvalidLength = "{0} key length must be between {1} and {2} characters.";
            public const string ValueInvalidLength = "{0} value length must be between {1} and {2} characters.";
        }

        public static class Enum
        {
            public const string InvalidEnumValue = "{0} value is invalid. Valid values: {1}";
            public const string InvalidFlagCombination = "{0} flag combination is invalid. Allowed flags: {1}";
        }

        public static class Collections
        {
            public const string EmptyCollection = "{0} cannot be empty.";
            public const string TooFewItems = "{0} must have at least {1} items.";
            public const string TooManyItems = "{0} cannot exceed {1} items.";
            public const string DuplicateItems = "{0} contains duplicate items.";
        }

        // Business Logic
        public static class Business
        {
            public const string NotFound = "{0} not found.";
            public const string AlreadyExists = "{0} with this {1} already exists.";
            public const string Conflict = "A conflict occurred while processing {0}{1}.";
            public const string InvalidOperation = "Invalid operation performed on {0}{1}.";
            public const string InvalidState = "{0} is in an invalid state.";
            public const string ItemUnavailable = "{0} is currently unavailable.";
            public const string ConcurrencyConflict = "A concurrency conflict occurred for {0}.";
            public const string NotAuthorized = "You are not authorized to perform this action on {0}.";
            public const string Forbidden = "Access to {0} is forbidden.";
            public const string RelationshipConstraintViolation = "Cannot {0} {1} due to existing relationships.";
            public const string InsufficientPermissions = "Insufficient permissions to access {0}.";
            public const string ServiceUnavailable = "Service is temporarily unavailable. Please try again later.";
            public const string RateLimitExceeded = "Too many requests. Please try again after some time.";
            public const string FeatureDisabled = "{0} feature is currently disabled.";
        }
    }
}
