using System.Text.RegularExpressions;

namespace ReSys.Core.Common.Constants;

public static partial class CommonInput
{
    public static class Constraints
    {
        public static class General
        {
            public const bool IsRequiredByDefault = false;
            public const string DefaultFieldName = "Name";
        }

        public static class Text
        {
            public const int MinLength = 1;
            public const int MaxLength = 1000;
            public const string AllowedPattern = @"^[\w\s-]+$";

            public const int TinyTextMaxLength = 100;
            public const int ShortTextMaxLength = 255;
            public const int MediumTextMaxLength = 1000;
            public const int LongTextMaxLength = 5000;
            public const int DescriptionMaxLength = 500;
            public const int TitleMaxLength = 200;
            public const int CommentMaxLength = 2000;
        }

        public static class Numeric
        {
            public const int MinValue = 0;
            public const int MaxValue = 1_000_000;
        }

        public static class Email
        {
            public const string Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            public static readonly Regex Regex = new(
                pattern: Pattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int MinLength = 5;
            public const int MaxLength = 256;
        }

        public static class UrlAndUri
        {
            public const string UrlPattern = @"^https?:\/\/[^\s/$.?#].[^\s]*$";
            public static readonly Regex UrlRegex = new(
                pattern: UrlPattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int UrlMinLength = 10;
            public const int UrlMaxLength = 2048;

            public const string UriPattern = @"^[a-zA-Z][a-zA-Z0-9+.-]*:\/\/[^\s]*$";
            public static readonly Regex UriRegex = new(
                pattern: UriPattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int UriMaxLength = 2048;
        }

        public static class PhoneNumbers
        {
            public const string E164Pattern = @"^\+[1-9]\d{1,14}$";
            public static readonly Regex E164Regex = new(
                pattern: E164Pattern,
                options: RegexOptions.Compiled);
            public const int E164MaxLength = 15;

            public const string Pattern = @"^\+?[\d\s()-]{7,20}$";
            public static readonly Regex Regex = new(
                pattern: Pattern,
                options: RegexOptions.Compiled);
            public const int MinLength = 7;
            public const int MaxLength = 20;
        }

        public static class NamesAndUsernames
        {
            public const string NamePattern = @"^[\p{L} \.'-]+$";
            public static readonly Regex NameRegex = new(
                pattern: NamePattern,
                options: RegexOptions.Compiled);
            public const int NameMinLength = Text.MinLength; // Using Text.MinLength
            public const int NameMaxLength = 100;

            public const string UsernamePattern = @"^[a-zA-Z0-9._-]+$";
            public static readonly Regex UsernameRegex = new(
                pattern: UsernamePattern,
                options: RegexOptions.Compiled);
            public const int UsernameMinLength = 3;
            public const int UsernameMaxLength = 256;
        }

        public static class Identifiers
        {
            public const string GuidPattern = @"^[0-9a-fA-F]{8}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{4}-[0-9a-fA-F]{12}$";
            public static readonly Regex GuidRegex = new(
                pattern: GuidPattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int GuidLength = 36;

            public const string UlidPattern = @"^[0-9A-HJKMNP-TV-Z]{26}$";
            public static readonly Regex UlidRegex = new(
                pattern: UlidPattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int UlidLength = 26;

            public const string NanoIdPattern = @"^[A-Za-z0-9_-]{21}$";
            public static readonly Regex NanoIdRegex = new(
                pattern: NanoIdPattern,
                options: RegexOptions.Compiled);
            public const int NanoIdLength = 21;
        }

        public static class Network
        {
            public const string IpV4Pattern = @"^((25[0-5]|(2[0-4]|1\d|[1-9]?)\d)\.){3}(25[0-5]|(2[0-4]|1\d|[1-9]?)\d)$";
            public static readonly Regex IpV4Regex = new(
                pattern: IpV4Pattern,
                options: RegexOptions.Compiled);
            public const int IpV4MaxLength = 15;

            public const string IpV6Pattern = @"^(([0-9a-fA-F]{1,4}:){7,7}[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,7}:|([0-9a-fA-F]{1,4}:){1,6}:[0-9a-fA-F]{1,4}|([0-9a-fA-F]{1,4}:){1,5}(:[0-9a-fA-F]{1,4}){1,2}|([0-9a-fA-F]{1,4}:){1,4}(:[0-9a-fA-F]{1,4}){1,3}|([0-9a-fA-F]{1,4}:){1,3}(:[0-9a-fA-F]{1,4}){1,4}|([0-9a-fA-F]{1,4}:){1,2}(:[0-9a-fA-F]{1,4}){1,5}|[0-9a-fA-F]{1,4}:((:[0-9a-fA-F]{1,4}){1,6})|:((:[0-9a-fA-F]{1,4}){1,7}|:)|fe80:(:[0-9a-fA-F]{0,4}){0,4}%[0-9a-zA-Z]{1,}|::(ffff(:0{1,4}){0,1}:){0,1}((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])|([0-9a-fA-F]{1,4}:){1,4}:((25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9])\.){3,3}(25[0-5]|(2[0-4]|1{0,1}[0-9]){0,1}[0-9]))$";
            public static readonly Regex IpV6Regex = new(
                pattern: IpV6Pattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int IpV6MaxLength = 45;

            public const string MacAddressPattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";
            public static readonly Regex MacAddressRegex = new(
                pattern: MacAddressPattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int MacAddressMaxLength = 17;

            public const string DomainPattern = @"^(?:[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?\.)+[a-zA-Z]{2,}$";
            public static readonly Regex DomainRegex = new(
                pattern: DomainPattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int DomainMaxLength = 255;
        }
        public static class GeographicAndPostalCodes
        {
            public const string PostalCodePattern = @"^[A-Za-z0-9\s-]{3,10}$";
            public static readonly Regex PostalCodeRegex = new(
                pattern: PostalCodePattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int PostalCodeMaxLength = 10;

            public const string ZipCodePattern = @"^\d{5}(-\d{4})?$";
            public static readonly Regex ZipCodeRegex = new(
                pattern: ZipCodePattern,
                options: RegexOptions.Compiled);
            public const int ZipCodeMaxLength = 10;

            public const string CanadianPostalCodePattern = @"^[A-Z]\d[A-Z]\s?\d[A-Z]\d$";
            public static readonly Regex CanadianPostalCodeRegex = new(
                pattern: CanadianPostalCodePattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int CanadianPostalCodeMaxLength = 7;

            public const string UkPostalCodePattern = @"^[A-Z]{1,2}\d[A-Z\d]?\s?\d[A-Z]{2}$";
            public static readonly Regex UkPostalCodeRegex = new(
                pattern: UkPostalCodePattern,
                options: RegexOptions.Compiled | RegexOptions.IgnoreCase);
            public const int UkPostalCodeMaxLength = 8;

            // Geographic Coordinates
            public const decimal LatitudeMin = -90m;
            public const decimal LatitudeMax = 90m;
            public const decimal LongitudeMin = -180m;
            public const decimal LongitudeMax = 180m;
        }

        public static class Passwords
        {
            public const string StrongPasswordPattern = @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[@$!%*?&])[A-Za-z\d@$!%*?&]{8,32}$";
            public static readonly Regex StrongPasswordRegex = new(
                pattern: StrongPasswordPattern,
                options: RegexOptions.Compiled);
            public const int MinLength = 8;
            public const int MaxLength = 128;
        }

        public static class SlugsAndVersions
        {
            public const string SlugPattern = @"^[a-z0-9]+(?:-[a-z0-9]+)*$";
            public static readonly Regex SlugRegex = new(
                pattern: SlugPattern,
                options: RegexOptions.Compiled);
            public const int SlugMinLength = Text.MinLength;
            public const int SlugMaxLength = 200;

            public const string SemVerPattern = @"^(0|[1-9]\d*)\.(0|[1-9]\d*)\.(0|[1-9]\d*)(?:-((?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*)(?:\.(?:0|[1-9]\d*|\d*[a-zA-Z-][0-9a-zA-Z-]*))*))?(?:\+([0-9a-zA-Z-]+(?:\.[0-9a-zA-Z-]+)*))?$";
            public static readonly Regex SemVerRegex = new(
                pattern: SemVerPattern,
                options: RegexOptions.Compiled);
            public const int SemVerMaxLength = 50;

            public const string VersionPattern = @"^\d+(\.\d+){1,3}$";
            public static readonly Regex VersionRegex = new(
                pattern: VersionPattern,
                options: RegexOptions.Compiled);
            public const int VersionMaxLength = 50;
        }

        public static class SocialMedia
        {
            public const string TwitterHandlePattern = @"^@?[A-Za-z0-9_]{1,15}$";
            public static readonly Regex TwitterHandleRegex = new(
                pattern: TwitterHandlePattern,
                options: RegexOptions.Compiled);
            public const int TwitterHandleMaxLength = 16; // Including optional @

            public const string HashtagPattern = @"^#[A-Za-z0-9_]+$";
            public static readonly Regex HashtagRegex = new(
                pattern: HashtagPattern,
                options: RegexOptions.Compiled);
            public const int HashtagMaxLength = 280;
        }

        public static class DateAndTime
        {
            public const string DatePattern = @"^\d{4}-\d{2}-\d{2}$";
            public static readonly Regex DateRegex = new(
                pattern: DatePattern,
                options: RegexOptions.Compiled);
            public const int DateMaxLength = 10;

            public const string TimePattern = @"^([01]\d|2[0-3]):([0-5]\d):([0-5]\d)$";
            public static readonly Regex TimeRegex = new(
                pattern: TimePattern,
                options: RegexOptions.Compiled);
            public const int TimeMaxLength = 8;

            public const string TimeSpanPattern = @"^([0-9]+:)?[0-5][0-9]:[0-5][0-9](:[0-5][0-9])?$";
            public static readonly Regex TimeSpanRegex = new(
                pattern: TimeSpanPattern,
                options: RegexOptions.Compiled);
            public const int TimeSpanMaxLength = 12;
        }

        public static class Json
        {
            public const string Pattern = @"^\{.*\}$|^\[.*\]$";
            public static readonly Regex Regex = new(
                pattern: Pattern,
                options: RegexOptions.Compiled | RegexOptions.Singleline);
        }

        public static class Boolean
        {
            public const string Pattern = @"^(true|false|True|False|TRUE|FALSE|1|0)$";
            public static readonly Regex Regex = new(
                pattern: Pattern,
                options: RegexOptions.Compiled);
        }

        public static class PaymentAndCreditCards
        {
            public const string CreditCardPattern = @"^\d{13,19}$";
            public static readonly Regex CreditCardRegex = new(
                pattern: CreditCardPattern,
                options: RegexOptions.Compiled);
            public const int CreditCardMaxLength = 19;

            public const string CvvPattern = @"^\d{3,4}$";
            public static readonly Regex CvvRegex = new(
                pattern: CvvPattern,
                options: RegexOptions.Compiled);
            public const int CvvMaxLength = 4;
        }

        public static class Color
        {
            public const string HexColorPattern = @"^#([A-Fa-f0-9]{6}|[A-Fa-f0-9]{3})$";
            public static readonly Regex HexColorRegex = new(
                pattern: HexColorPattern,
                options: RegexOptions.Compiled);
            public const int HexColorMaxLength = 7;
        }

        public static class FileSystem
        {
            public const string FilePathPattern = @"^[^<>:""|?*]+$";
            public static readonly Regex FilePathRegex = new(
                pattern: FilePathPattern,
                options: RegexOptions.Compiled);
            public const int FilePathMaxLength = 260;

            public const string FileExtensionPattern = @"^\.[a-zA-Z0-9]+$";
            public static readonly Regex FileExtensionRegex = new(
                pattern: FileExtensionPattern,
                options: RegexOptions.Compiled);
            public const int FileExtensionMaxLength = 10;
        }

        public static class CurrencyAndLanguage
        {
            public const string CurrencyCodePattern = @"^[A-Z]{3}$";
            public static readonly Regex CurrencyCodeRegex = new(
                pattern: CurrencyCodePattern,
                options: RegexOptions.Compiled);
            public const int CurrencyCodeLength = 3;

            public const string LanguageCodePattern = @"^[a-z]{2}(-[A-Z]{2})?$";
            public static readonly Regex LanguageCodeRegex = new(
                pattern: LanguageCodePattern,
                options: RegexOptions.Compiled);
            public const int LanguageCodeMaxLength = 5;
        }

        public static class NumericPatterns
        {
            public const string IntegerPattern = @"^-?\d+$";
            public static readonly Regex IntegerRegex = new(
                pattern: IntegerPattern,
                options: RegexOptions.Compiled);

            public const string DecimalPattern = @"^-?\d*\.?\d+$";
            public static readonly Regex DecimalRegex = new(
                pattern: DecimalPattern,
                options: RegexOptions.Compiled);
        }

        public static class Dictionary
        {
            public const int MaxEntries = 50;
            public const int KeyMinLength = Text.MinLength;
            public const int KeyMaxLength = 64;
            public const int ValueMinLength = Numeric.MinValue;
            public const int ValueMaxLength = 2048;
            public const string KeyAllowedPattern = @"^[A-Za-z0-9_.-]+$";
            public static readonly Regex KeyAllowedRegex = new(
                pattern: KeyAllowedPattern,
                options: RegexOptions.Compiled);
        }
    }
}