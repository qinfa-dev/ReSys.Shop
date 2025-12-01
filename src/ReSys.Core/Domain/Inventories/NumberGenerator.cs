namespace ReSys.Core.Domain.Inventories;

/// <summary>
/// Utility class for generating reference numbers.
/// </summary>
public static class NumberGenerator
{
    private static readonly Lock Lock = new();
    private static int s_counter;

    /// <summary>
    /// Generates a unique reference number with the given prefix.
    /// </summary>
    /// <param name="prefix">The prefix to use (e.g., "T" for transfers)</param>
    public static string Generate(string prefix)
    {
        lock (Lock)
        {
            s_counter++;
            return $"{prefix}{DateTime.UtcNow:yyMMdd}{s_counter:D4}";
        }
    }
}