using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies;

/// <summary>
/// Fulfillment strategy that selects locations to minimize total fulfillment and shipping costs.
/// Factors in location-specific handling costs and estimated shipping distance.
/// </summary>
/// <remarks>
/// <para>
/// <b>Strategy Behavior:</b>
/// - Balances fulfillment cost with shipping distance
/// - Requires location metadata with fulfillment costs
/// - Uses Haversine distance for shipping cost estimation
/// - Can split across multiple locations for cost optimization
/// </para>
/// 
/// <para>
/// <b>Cost Calculation:</b>
/// TotalCost = FulfillmentCost + (Distance × ShippingCostPerKm) + (Quantity × HandlingCostPerUnit)
/// 
/// Metadata Keys Expected (in location.PrivateMetadata):
/// - "fulfillment_cost_base": Base fulfillment cost per order (decimal)
/// - "handling_cost_per_unit": Cost to handle each unit (decimal)
/// - "shipping_cost_per_km": Estimated cost per km to ship from this location (decimal)
/// </para>
/// 
/// <para>
/// <b>Use Cases:</b>
/// - Optimizing profit margins on orders
/// - Balancing cost with service level
/// - Closing out seasonal inventory at discount locations
/// - Multi-warehouse cost optimization
/// </para>
/// 
/// <para>
/// <b>Performance:</b>
/// - O(n log n) due to sorting cost calculations
/// - Requires metadata lookups and distance calculations
/// - Suitable for B2B orders where cost is critical
/// </para>
/// </remarks>
public sealed class CostOptimizedStrategy : IFulfillmentStrategy
{
    private const decimal DefaultFulfillmentCost = 5m;
    private const decimal DefaultHandlingCostPerUnit = 0.50m;
    private const decimal DefaultShippingCostPerKm = 0.10m;

    public string Name => "Cost Optimized";

    public string Description =>
        "Selects locations to minimize total fulfillment and shipping costs. " +
        "Ideal for margin optimization and cost-sensitive orders.";

    public bool SupportsMultipleLocations => true;

    /// <summary>
    /// Selects the single location with lowest total fulfillment cost.
    /// </summary>
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
            .OrderBy(loc => CalculateFulfillmentCost(
                location: loc,
                variant: variant,
                quantity: requiredQuantity,
                customerLatitude: customerLatitude,
                customerLongitude: customerLongitude))
            .FirstOrDefault();
    }

    /// <summary>
    /// Selects multiple locations minimizing total cost across all locations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Selection Algorithm:</b>
    /// 1. Calculate cost per location for fulfilling the full quantity
    /// 2. Sort by cost (lowest first)
    /// 3. Allocate from lowest-cost location until quantity satisfied
    /// 4. Return up to maxLocations ordered by cost
    /// </para>
    /// 
    /// <para>
    /// Note: This greedy approach works well for cost optimization, but more sophisticated
    /// algorithms (e.g., bin packing) may be needed for complex scenarios with split shipments.
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
            .OrderBy(loc => CalculateFulfillmentCost(
                location: loc,
                variant: variant,
                quantity: 1, // Cost per unit for comparison
                customerLatitude: customerLatitude,
                customerLongitude: customerLongitude))
            .ToList();

        if (!locationsWithStock.Any())
            return result;

        // Allocate quantities from lowest-cost locations
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

    /// <summary>
    /// Calculates the total cost to fulfill a quantity from a specific location.
    /// </summary>
    /// <remarks>
    /// <para>
    /// Cost formula:
    /// TotalCost = FulfillmentCost + (Distance × ShippingCostPerKm) + (Quantity × HandlingCostPerUnit)
    /// </para>
    /// 
    /// <para>
    /// If customer coordinates are not provided, shipping cost is estimated at a flat rate
    /// assuming average distance of 500 km.
    /// </para>
    /// </remarks>
    private decimal CalculateFulfillmentCost(
        StockLocation location,
        Variant variant,
        int quantity,
        decimal? customerLatitude,
        decimal? customerLongitude)
    {
        var fulfillmentCost = GetMetadataValue<decimal>(
            location.PrivateMetadata,
            "fulfillment_cost_base",
            DefaultFulfillmentCost);

        var handlingCostPerUnit = GetMetadataValue<decimal>(
            location.PrivateMetadata,
            "handling_cost_per_unit",
            DefaultHandlingCostPerUnit);

        var shippingCostPerKm = GetMetadataValue<decimal>(
            location.PrivateMetadata,
            "shipping_cost_per_km",
            DefaultShippingCostPerKm);

        // Calculate shipping cost
        decimal shippingCost = 0m;
        if (customerLatitude.HasValue && customerLongitude.HasValue && location.HasLocation)
        {
            var distance = location.CalculateDistanceTo(customerLatitude.Value, customerLongitude.Value);
            shippingCost = (distance ?? 500m) * shippingCostPerKm;
        }
        else
        {
            // Default to 500 km average distance if coordinates unavailable
            shippingCost = 500m * shippingCostPerKm;
        }

        var handlingCost = quantity * handlingCostPerUnit;

        return fulfillmentCost + shippingCost + handlingCost;
    }

    /// <summary>
    /// Safely retrieves a metadata value with type conversion and default fallback.
    /// </summary>
    private static T GetMetadataValue<T>(
        IDictionary<string, object?>? metadata,
        string key,
        T defaultValue) where T : struct
    {
        if (metadata == null || !metadata.ContainsKey(key))
            return defaultValue;

        try
        {
            var value = metadata[key];
            if (value == null)
                return defaultValue;

            return (T)Convert.ChangeType(value, typeof(T));
        }
        catch
        {
            return defaultValue;
        }
    }
}
