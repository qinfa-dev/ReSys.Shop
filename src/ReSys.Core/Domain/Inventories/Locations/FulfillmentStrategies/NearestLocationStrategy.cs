using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

/// <summary>
/// Fulfillment strategy that allocates stock from the nearest location to the customer.
/// Uses Haversine formula to calculate geographic distance.
/// </summary>
public sealed class NearestLocationStrategy : IFulfillmentStrategy
{
    /// <summary>
    /// Allocates stock from the location nearest to the customer.
    /// </summary>
    public Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null)
    {
        if (customerLocation == null)
            throw new InvalidOperationException("Nearest location strategy requires customer location coordinates");

        // Filter locations with geographic data and sort by distance
        var sortedLocations = availableLocations
            .Where(l => l.Latitude.HasValue && l.Longitude.HasValue)
            .OrderBy(l => CalculateDistance(
                l.Latitude!.Value,
                l.Longitude!.Value,
                customerLocation.Latitude,
                customerLocation.Longitude))
            .ToList();

        var allocations = new List<FulfillmentAllocation>();
        int remaining = quantity;

        foreach (var location in sortedLocations)
        {
            if (remaining <= 0) break;

            var stockItem = location.StockItems
                .FirstOrDefault(si => si.VariantId == variant.Id);

            if (stockItem == null || stockItem.CountAvailable <= 0) continue;

            int allocate = Math.Min(stockItem.CountAvailable, remaining);
            allocations.Add(new(
                LocationId: location.Id,
                LocationName: location.Name,
                AllocatedQuantity: allocate));

            remaining -= allocate;
        }

        return Task.FromResult(allocations);
    }

    /// <summary>
    /// Calculates the great-circle distance between two points using the Haversine formula.
    /// </summary>
    /// <returns>Distance in kilometers</returns>
    private static decimal CalculateDistance(
        decimal fromLat,
        decimal fromLng,
        decimal toLat,
        decimal toLng)
    {
        decimal earthRadiusKm = 6371;

        var fromLatRad = (double)(fromLat * (decimal)Math.PI / 180);
        var toLatRad = (double)(toLat * (decimal)Math.PI / 180);
        var deltaLat = (double)((toLat - fromLat) * (decimal)Math.PI / 180);
        var deltaLng = (double)((toLng - fromLng) * (decimal)Math.PI / 180);

        var a = Math.Sin(deltaLat / 2) * Math.Sin(deltaLat / 2) +
                Math.Cos(fromLatRad) * Math.Cos(toLatRad) *
                Math.Sin(deltaLng / 2) * Math.Sin(deltaLng / 2);

        var c = 2 * Math.Asin(Math.Sqrt(a));
        return earthRadiusKm * (decimal)c;
    }
}
