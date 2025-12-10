using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

/// <summary>
/// Fulfillment strategy that allocates stock from a preferred/specified location.
/// Used when customer explicitly selects a location (e.g., store pickup, preferred warehouse).
/// </summary>
public sealed class PreferredLocationStrategy : IFulfillmentStrategy
{
    private readonly Guid _preferredLocationId;

    /// <summary>
    /// Creates a strategy that prioritizes a specific location.
    /// </summary>
    /// <param name="preferredLocationId">The location to prioritize for fulfillment</param>
    public PreferredLocationStrategy(Guid preferredLocationId)
    {
        _preferredLocationId = preferredLocationId;
    }

    /// <summary>
    /// Allocates stock first from the preferred location, then from others if needed.
    /// </summary>
    public Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null)
    {
        var locationsList = availableLocations.ToList();
        var allocations = new List<FulfillmentAllocation>();
        int remaining = quantity;

        // First, try to allocate from preferred location
        var preferredLocation = locationsList
            .FirstOrDefault(l => l.Id == _preferredLocationId);

        if (preferredLocation != null)
        {
            var stockItem = preferredLocation.StockItems
                .FirstOrDefault(si => si.VariantId == variant.Id);

            if (stockItem != null && stockItem.CountAvailable > 0)
            {
                int allocate = Math.Min(stockItem.CountAvailable, remaining);
                allocations.Add(new(
                    LocationId: preferredLocation.Id,
                    LocationName: preferredLocation.Name,
                    AllocatedQuantity: allocate));

                remaining -= allocate;
            }
        }

        // If still need stock, allocate from other locations (highest stock first)
        if (remaining > 0)
        {
            var otherLocations = locationsList
                .Where(l => l.Id != _preferredLocationId)
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

            foreach (var item in otherLocations)
            {
                if (remaining <= 0) break;

                int allocate = Math.Min(item.AvailableQty, remaining);
                allocations.Add(new(
                    LocationId: item.Location.Id,
                    LocationName: item.Location.Name,
                    AllocatedQuantity: allocate));

                remaining -= allocate;
            }
        }

        return Task.FromResult(allocations);
    }
}
