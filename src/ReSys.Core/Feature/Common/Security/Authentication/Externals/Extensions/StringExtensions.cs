namespace ReSys.Core.Feature.Common.Security.Authentication.Externals.Extensions;

internal static class StringExtensions
{
    /// <summary>
    /// Returns the specified <paramref name="fallback"/> string if the current <paramref name="value"/> is
    /// <see langword="null"/>, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <param name="fallback">The value to return if <paramref name="value"/> is null or empty.</param>
    /// <returns>
    /// <paramref name="value"/> if it contains non-whitespace characters; otherwise, <paramref name="fallback"/>.
    /// </returns>
    public static string IfEmpty(this string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value: value) ? fallback : value;
}
