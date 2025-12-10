using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies;

/// <summary>
/// Defines the strategy pattern for selecting the best location(s) to fulfill an order.
/// Different implementations can prioritize distance, inventory levels, cost, or admin preferences.
/// </summary>
public interface IFulfillmentStrategy
{
    /// <summary>
    /// Gets the name of this fulfillment strategy.
    /// </summary>
    string Name { get; }

    /// <summary>
    /// Gets the description of this fulfillment strategy.
    /// </summary>
    string Description { get; }

    /// <summary>
    /// Selects the best location to fulfill a line item based on this strategy.
    /// </summary>
    /// <param name="variant">The product variant to fulfill.</param>
    /// <param name="requiredQuantity">The quantity required to fulfill the order.</param>
    /// <param name="availableLocations">The collection of locations that have stock available for this variant.</param>
    /// <param name="customerLatitude">Optional: Customer latitude for distance-based strategies.</param>
    /// <param name="customerLongitude">Optional: Customer longitude for distance-based strategies.</param>
    /// <returns>
    /// The selected location to fulfill from, or null if no suitable location is found.
    /// The returned location should have sufficient stock to fulfill the requested quantity.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Implementation Contract:</b>
    /// - Must never return a location with insufficient stock for the required quantity
    /// - Must respect location fulfillment capabilities (CanShip, CanPickup, etc.)
    /// - Should handle null coordinates gracefully in distance calculations
    /// - Should provide consistent results for identical inputs
    /// </para>
    /// 
    /// <para>
    /// <b>Example Implementations:</b>
    /// <list type="bullet">
    /// <item><b>Nearest:</b> Returns location closest to customer using Haversine distance</item>
    /// <item><b>HighestStock:</b> Returns location with most inventory available</item>
    /// <item><b>CostOptimized:</b> Returns location minimizing fulfillment + shipping costs</item>
    /// <item><b>Preferred:</b> Returns admin-configured preferred location (with fallback)</item>
    /// </list>
    /// </para>
    /// </remarks>
    StockLocation? SelectLocation(
        Variant variant,
        int requiredQuantity,
        IEnumerable<StockLocation> availableLocations,
        decimal? customerLatitude = null,
        decimal? customerLongitude = null);

    /// <summary>
    /// Selects multiple locations to partially fulfill a large order when a single location
    /// doesn't have enough stock.
    /// </summary>
    /// <param name="variant">The product variant to fulfill.</param>
    /// <param name="requiredQuantity">The total quantity required across all locations.</param>
    /// <param name="availableLocations">The collection of locations that have stock available.</param>
    /// <param name="maxLocations">Maximum number of locations to use (default: 3).</param>
    /// <param name="customerLatitude">Optional: Customer latitude for distance-based strategies.</param>
    /// <param name="customerLongitude">Optional: Customer longitude for distance-based strategies.</param>
    /// <returns>
    /// A list of tuples (Location, QuantityToFulfillFrom) ordered by selection priority.
    /// The sum of quantities should equal or exceed requiredQuantity.
    /// Returns empty list if insufficient total stock across all locations.
    /// </returns>
    /// <remarks>
    /// <para>
    /// This method is useful for large orders or when inventory is distributed across multiple locations.
    /// Implementation should balance minimizing shipments with fulfillment efficiency.
    /// </para>
    /// </remarks>
    IList<(StockLocation Location, int Quantity)> SelectMultipleLocations(
        Variant variant,
        int requiredQuantity,
        IEnumerable<StockLocation> availableLocations,
        int maxLocations = 3,
        decimal? customerLatitude = null,
        decimal? customerLongitude = null);

    /// <summary>
    /// Indicates whether this strategy can select multiple locations for a single line item.
    /// </summary>
    /// <remarks>
    /// Some strategies (e.g., Preferred) may only select a single location, while others
    /// (e.g., Nearest, HighestStock) can split across multiple locations if needed.
    /// </remarks>
    bool SupportsMultipleLocations { get; }
}
