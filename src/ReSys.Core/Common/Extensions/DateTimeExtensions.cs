namespace ReSys.Core.Common.Extensions;

public static class DateTimeExtensions
{
    /// <summary>
    /// Formats a nullable DateTimeOffset to a UTC string (ISO 8601 with Z) if it has a value, otherwise returns null.
    /// </summary>
    /// <param name="dateTimeOffset">The DateTimeOffset to format.</param>
    /// <returns>A UTC formatted string or null if the input is null.</returns>
    public static string? FormatUtc(this DateTimeOffset? dateTimeOffset)
    {
        return dateTimeOffset?.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ssZ");
    }
}