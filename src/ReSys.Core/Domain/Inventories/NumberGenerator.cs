namespace ReSys.Core.Domain.Inventories;

/// <summary>
/// Provides a utility for generating unique, sequential reference numbers with a given prefix.
/// This class is designed to create human-readable identifiers for various inventory-related
/// entities (e.g., stock transfers, inventory units).
/// </summary>
/// <remarks>
/// <para>
/// <strong>Generated Number Format:</strong>
/// The generated number follows the pattern: <c>{PREFIX}{YYMMDD}{COUNTER}</c>.
/// <list type="bullet">
/// <item>
/// <term>PREFIX</term>
/// <description>A user-defined string (e.g., "T" for transfers, "INV" for inventory units).</description>
/// </item>
/// <item>
/// <term>YYMMDD</term>
/// <description>The current date in year-month-day format (e.g., "240115" for January 15, 2024).</description>
/// </item>
/// <item>
/// <term>COUNTER</term>
/// <description>A 4-digit zero-padded sequential counter that resets daily.</description>
/// </item>
/// </list>
/// Example: "T2401150001"
/// </para>
///
/// <para>
/// <strong>Thread Safety:</strong>
/// The counter is incremented in a thread-safe manner using a lock to ensure uniqueness
/// across concurrent calls within the same application instance.
/// </para>
/// </remarks>
public static class NumberGenerator
{
    private static readonly Lock Lock = new();
    private static int s_counter;

    /// <summary>
    /// Generates a unique reference number with the given prefix and a daily sequential counter.
    /// The format is <c>{PREFIX}{YYMMDD}{COUNTER}</c>.
    /// </summary>
    /// <param name="prefix">The prefix to use for the generated number (e.g., "T" for transfers, "INV" for inventory units).</param>
    /// <returns>A unique string representing a reference number.</returns>
    /// <remarks>
    /// The internal counter (<c>s_counter</c>) is reset daily, meaning the counter starts from 1 each new day.
    /// This method is thread-safe.
    /// </remarks>
    public static string Generate(string prefix)
    {
        lock (Lock)
        {
            s_counter++;
            return $"{prefix}{DateTime.UtcNow:yyMMdd}{s_counter:D4}";
        }
    }
}