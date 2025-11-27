using Humanizer;

namespace ReSys.Core.Common.Extensions;

public static class HumanizeExtensions
{
    /// <summary>
    /// Convert a PascalCase or camelCase entity name to a human-friendly label.
    /// Examples: "OptionType" -> "Option type", "TodoList" -> "Todo list"
    /// Uses the Humanizer library for robust string humanization.
    /// </summary>
    public static string ToHumanize(this string? name)
    {
        if (string.IsNullOrWhiteSpace(value: name))
            return string.Empty;

        return name.Humanize();
    }
}