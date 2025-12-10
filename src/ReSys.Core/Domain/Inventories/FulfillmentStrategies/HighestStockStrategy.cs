using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies;

/// <summary>
/// Fulfillment strategy that selects locations based on available inventory levels.
/// Prioritizes locations with the highest stock of the requested variant.
/// </summary>
/// <remarks>
/// <para>
/// <b>Strategy Behavior:</b>
/// - Prioritizes locations with most available inventory
/// - Reduces inventory fragmentation across locations
/// - Consolidates fulfillment to fewer locations
/// - Can split across multiple locations for large orders
/// </para>
/// 
/// <para>
/// <b>Use Cases:</b>
/// - Minimizing number of shipments for an order
/// - Reducing inventory fragmentation
/// - Clearing high-stock locations to make room for new inventory
/// - Warehouse management and inventory balancing
/// </para>
/// 
/// <para>
/// <b>Performance:</b>
/// - O(n log n) complexity due to sorting by stock quantity
/// - Efficient with large location networks
/// - No external API calls or distance calculations required
/// </para>
/// </remarks>
public sealed class HighestStockStrategy : IFulfillmentStrategy
{
    public string Name => "Highest Stock";

    public string Description =>
        "Selects the location with the most available inventory. " +
        "Ideal for consolidating fulfillment and reducing fragmentation.";

    public bool SupportsMultipleLocations => true;

    /// <summary>
    /// Selects the single location with highest available inventory.
    /// </summary>
    /// <remarks>
    /// If multiple locations have the same maximum stock, returns the first one encountered.
    /// </remarks>
    public StockLocation? SelectLocation(
        Variant variant,
        int requiredQuantity,
        IEnumerable<StockLocation> availableLocations,
        decimal? customerLatitude = null,
        decimal? customerLongitude = null)
    {
        return availableLocations
            .Where(loc => loc.StockItems.Any(si => 
                si.VariantId == variant.Id && 
                si.CountAvailable >= requiredQuantity))
            .OrderByDescending(loc => loc.StockItems
                .FirstOrDefault(si => si.VariantId == variant.Id)?
                .CountAvailable ?? 0)
            .FirstOrDefault();
    }

    /// <summary>
    /// Selects multiple locations ordered by inventory availability (highest first).
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Selection Algorithm:</b>
    /// 1. Filter locations with stock for the variant
    /// 2. Sort by available quantity (highest first)
    /// 3. Allocate quantity from highest-stock location until satisfied or locations exhausted
    /// 4. Return up to maxLocations ordered by stock quantity
    /// </para>
    /// 
    /// <para>
    /// This approach ensures the fewest shipments while leveraging well-stocked locations first,
    /// which helps prevent deadlock situations where orders wait for low-stock locations to replenish.
    /// </para>
    /// </remarks>
    public IList<(StockLocation Location, int Quantity)> SelectMultipleLocations(
        Variant variant,
        int requiredQuantity,
        IEnumerable<StockLocation> availableLocations,
        int maxLocations = 3,
        decimal? customerLatitude = null,
        decimal? customerLongitude = null)
    {
        var result = new List<(StockLocation, int)>();
        var remaining = requiredQuantity;

        var locationsWithStock = availableLocations
            .Where(loc => loc.StockItems.Any(si => 
                si.VariantId == variant.Id && 
                si.CountAvailable > 0))
            .OrderByDescending(loc => loc.StockItems
                .FirstOrDefault(si => si.VariantId == variant.Id)?
                .CountAvailable ?? 0)
            .ToList();

        if (!locationsWithStock.Any())
            return result;

        // Allocate quantities from highest-stock locations
        foreach (var location in locationsWithStock.Take(maxLocations))
        {
            if (remaining <= 0)
                break;

            var stockItem = location.StockItems
                .FirstOrDefault(si => si.VariantId == variant.Id);

            if (stockItem == null)
                continue;

            var availableQty = stockItem.CountAvailable;
            var qtyToAllocate = Math.Min(remaining, availableQty);

            if (qtyToAllocate > 0)
            {
                result.Add((location, qtyToAllocate));
                remaining -= qtyToAllocate;
            }
        }

        // Return empty if couldn't fulfill required quantity
        return remaining > 0 ? new List<(StockLocation, int)>() : result;
    }
}

