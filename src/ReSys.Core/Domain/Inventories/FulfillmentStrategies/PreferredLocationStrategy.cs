using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies;

/// <summary>
/// Fulfillment strategy that respects admin-configured preferred locations.
/// Attempts to fulfill from the preferred location with automatic fallback to other locations.
/// </summary>
/// <remarks>
/// <para>
/// <b>Strategy Behavior:</b>
/// - Prioritizes admin-configured preferred locations
/// - Falls back to other locations if preferred lacks stock
/// - Does not split orders across multiple locations by default
/// - Can be configured per store, vendor, or product category
/// </para>
/// 
/// <para>
/// <b>Configuration:</b>
/// Preference is stored in location.PublicMetadata or StoreConfiguration:
/// - Key: "fulfillment_preference_priority" (int, 0-100)
/// - Higher values = higher priority
/// - Default priority for non-preferred locations: 0
/// </para>
/// 
/// <para>
/// <b>Use Cases:</b>
/// - Preferring first-party fulfillment over third-party
/// - Routing to specific distribution centers by region
/// - Promoting newer warehouses to clear inventory
/// - Vendor-specific fulfillment rules
/// - 3PL vs. 1PL routing decisions
/// </para>
/// 
/// <para>
/// <b>Performance:</b>
/// - O(n log n) due to sorting by preference priority
/// - No external API calls or complex calculations
/// - Fast even with many locations
/// </para>
/// </remarks>
public sealed class PreferredLocationStrategy : IFulfillmentStrategy
{
    private const int DefaultPreferencePriority = 0;

    public string Name => "Preferred Location";

    public string Description =>
        "Selects admin-configured preferred locations. " +
        "Ideal for brand control and vendor relationships.";

    public bool SupportsMultipleLocations => false; // Prefers single location fulfillment

    /// <summary>
    /// Selects the highest-priority location with sufficient stock.
    /// Falls back to non-preferred locations if preferred lacks stock.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Selection Order:</b>
    /// 1. Preferred locations (sorted by priority, descending)
    /// 2. Non-preferred locations (if preferred locations lack stock)
    /// 3. First location with stock (if all else fails)
    /// </para>
    /// </remarks>
    public StockLocation? SelectLocation(
        Variant variant,
        int requiredQuantity,
        IEnumerable<StockLocation> availableLocations,
        decimal? customerLatitude = null,
        decimal? customerLongitude = null)
    {
        var locationsWithStock = availableLocations
            .Where(loc => loc.StockItems.Any(si => 
                si.VariantId == variant.Id && 
                si.CountAvailable >= requiredQuantity))
            .ToList();

        if (!locationsWithStock.Any())
            return null;

        // Try preferred locations first (sorted by priority)
        var preferred = locationsWithStock
            .OrderByDescending(loc => GetPreferencePriority(loc))
            .FirstOrDefault(loc => GetPreferencePriority(loc) > DefaultPreferencePriority);

        if (preferred != null)
            return preferred;

        // Fall back to any location with stock
        return locationsWithStock.First();
    }

    /// <summary>
    /// Returns a single location or empty list (does not support splits).
    /// </summary>
    /// <remarks>
    /// The Preferred strategy prioritizes brand control and vendor relationships,
    /// which typically require single-location fulfillment. For split fulfillment scenarios,
    /// fall back to Nearest or HighestStock strategies.
    /// </remarks>
    public IList<(StockLocation Location, int Quantity)> SelectMultipleLocations(
        Variant variant,
        int requiredQuantity,
        IEnumerable<StockLocation> availableLocations,
        int maxLocations = 3,
        decimal? customerLatitude = null,
        decimal? customerLongitude = null)
    {
        var selected = SelectLocation(variant, requiredQuantity, availableLocations, 
            customerLatitude, customerLongitude);

        if (selected == null)
            return new List<(StockLocation, int)>();

        return new List<(StockLocation, int)> { (selected, requiredQuantity) };
    }

    /// <summary>
    /// Gets the preference priority for a location.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Priority Sources (checked in order):</b>
    /// 1. Location.PublicMetadata["fulfillment_preference_priority"]
    /// 2. Location.PrivateMetadata["fulfillment_preference_priority"]
    /// 3. DefaultPreferencePriority (0)
    /// </para>
    /// </remarks>
    private static int GetPreferencePriority(StockLocation location)
    {
        // Check public metadata first
        if (location.PublicMetadata?.TryGetValue("fulfillment_preference_priority", out var pubValue) == true)
        {
            if (int.TryParse(pubValue?.ToString(), out var pubPriority))
                return pubPriority;
        }

        // Check private metadata
        if (location.PrivateMetadata?.TryGetValue("fulfillment_preference_priority", out var privValue) == true)
        {
            if (int.TryParse(privValue?.ToString(), out var privPriority))
                return privPriority;
        }

        return DefaultPreferencePriority;
    }
}
