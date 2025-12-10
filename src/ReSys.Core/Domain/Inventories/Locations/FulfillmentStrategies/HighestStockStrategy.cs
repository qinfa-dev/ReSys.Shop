using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

/// <summary>
/// Fulfillment strategy that allocates stock from locations with the highest available quantity.
/// Helps balance inventory across locations.
/// </summary>
public sealed class HighestStockStrategy : IFulfillmentStrategy
{
    /// <summary>
    /// Allocates stock from locations with the highest available quantities first.
    /// </summary>
    public Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null)
    {
        // Sort by available quantity (highest first)
        var sortedLocations = availableLocations
            .Select(l => new
            {
                Location = l,
                AvailableQty = l.StockItems
                    .FirstOrDefault(si => si.VariantId == variant.Id)?
                    .CountAvailable ?? 0
            })
            .OrderByDescending(x => x.AvailableQty)
            .Where(x => x.AvailableQty > 0)
            .ToList();

        var allocations = new List<FulfillmentAllocation>();
        int remaining = quantity;

        foreach (var item in sortedLocations)
        {
            if (remaining <= 0) break;

            int allocate = Math.Min(item.AvailableQty, remaining);
            allocations.Add(new(
                LocationId: item.Location.Id,
                LocationName: item.Location.Name,
                AllocatedQuantity: allocate));

            remaining -= allocate;
        }

        return Task.FromResult(allocations);
    }
}
