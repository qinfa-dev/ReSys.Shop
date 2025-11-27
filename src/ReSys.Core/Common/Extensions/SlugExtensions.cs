using Slugify;

namespace ReSys.Core.Common.Extensions;

public static class SlugExtensions
{
    private static readonly SlugHelper SlugHelper = new(
        config: new SlugHelperConfiguration
        {
            ForceLowerCase = true,

            // Replacement map for German + Vietnamese accents
            StringReplacements = new Dictionary<string, string>
            {
                { " ", "-" }, // Explicitly replace spaces with hyphens
                // German (lowercase and uppercase)
                { "ß", "ss" },
                { "ä", "ae" },
                { "ö", "oe" },
                { "ü", "ue" },
                { "Ä", "Ae" },
                { "Ö", "Oe" },
                { "Ü", "Ue" },

                // Vietnamese (accent removal)
                // a/A
                { "à", "a" }, { "á", "a" }, { "ả", "a" }, { "ã", "a" }, { "ạ", "a" },
                { "ă", "a" }, { "ằ", "a" }, { "ắ", "a" }, { "ẳ", "a" }, { "ẵ", "a" }, { "ặ", "a" },
                { "â", "a" }, { "ầ", "a" }, { "ấ", "a" }, { "ẩ", "a" }, { "ẫ", "a" }, { "ậ", "a" },
                // A
                { "À", "A" }, { "Á", "A" }, { "Ả", "A" }, { "Ã", "A" }, { "Ạ", "A" },
                { "Ă", "A" }, { "Ằ", "A" }, { "Ắ", "A" }, { "Ẳ", "A" }, { "Ẵ", "A" }, { "Ặ", "A" },
                { "Â", "A" }, { "Ầ", "A" }, { "Ấ", "A" }, { "Ẩ", "A" }, { "Ẫ", "A" }, { "Ậ", "A" },

                // đ/D
                { "đ", "d" },
                { "Đ", "D" },

                // e/E
                { "è", "e" }, { "é", "e" }, { "ẻ", "e" }, { "ẽ", "e" }, { "ẹ", "e" },
                { "ê", "e" }, { "ề", "e" }, { "ế", "e" }, { "ể", "e" }, { "ễ", "e" }, { "ệ", "e" },
                // E
                { "È", "E" }, { "É", "E" }, { "Ẻ", "E" }, { "Ẽ", "E" }, { "Ẹ", "E" },
                { "Ê", "E" }, { "Ề", "E" }, { "Ế", "E" }, { "Ể", "E" }, { "Ễ", "E" }, { "Ệ", "E" },

                // i/I
                { "ì", "i" }, { "í", "i" }, { "ỉ", "i" }, { "ĩ", "i" }, { "ị", "i" },
                // I
                { "Ì", "I" }, { "Í", "I" }, { "Ỉ", "I" }, { "Ĩ", "I" }, { "Ị", "I" },

                // o/O
                { "ò", "o" }, { "ó", "o" }, { "ỏ", "o" }, { "õ", "o" }, { "ọ", "o" },
                { "ơ", "o" }, { "ờ", "o" }, { "ớ", "o" }, { "ở", "o" }, { "ỡ", "o" }, { "ợ", "o" },
                { "ô", "o" }, { "ồ", "o" }, { "ố", "o" }, { "ổ", "o" }, { "ỗ", "o" }, { "ộ", "o" },
                // O
                { "Ò", "O" }, { "Ó", "O" }, { "Ỏ", "O" }, { "Õ", "O" }, { "Ọ", "O" },
                { "Ơ", "O" }, { "Ờ", "O" }, { "Ớ", "O" }, { "Ở", "O" }, { "Ỡ", "O" }, { "Ợ", "O" },
                { "Ô", "O" }, { "Ồ", "O" }, { "Ố", "O" }, { "Ổ", "O" }, { "Ỗ", "O" }, { "Ộ", "O" },

                // u/U
                { "ù", "u" }, { "ú", "u" }, { "ủ", "u" }, { "ũ", "u" }, { "ụ", "u" },
                { "ư", "u" }, { "ừ", "u" }, { "ứ", "u" }, { "ử", "u" }, { "ữ", "u" }, { "ự", "u" },
                // U
                { "Ù", "U" }, { "Ú", "U" }, { "Ủ", "U" }, { "Ũ", "U" }, { "Ụ", "U" },
                { "Ư", "U" }, { "Ừ", "U" }, { "Ứ", "U" }, { "Ử", "U" }, { "Ữ", "U" }, { "Ự", "U" },

                // y/Y
                { "ỳ", "y" }, { "ý", "y" }, { "ỷ", "y" }, { "ỹ", "y" }, { "ỵ", "y" },
                // Y
                { "Ỳ", "Y" }, { "Ý", "Y" }, { "Ỷ", "Y" }, { "Ỹ", "Y" }, { "Ỵ", "Y" }
            },

            // Keep Chinese characters: allow all Unicode letters and numbers

        });

    /// <summary>
    /// Converts any string (English, Vietnamese, German, or Chinese characters kept as-is) to a URL-friendly slug.
    /// </summary>
    public static string ToSlug(this string? text)
    {
        return string.IsNullOrWhiteSpace(value: text)
            ? string.Empty
            : SlugHelper.GenerateSlug(inputString: text);
    }
}
