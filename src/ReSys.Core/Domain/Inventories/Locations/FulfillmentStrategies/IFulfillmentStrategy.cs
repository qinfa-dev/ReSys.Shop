using ReSys.Core.Domain.Catalog.Products.Variants;

namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

/// <summary>
/// Represents a customer's location for fulfillment calculations (distance, shipping cost).
/// </summary>
public sealed record CustomerLocation(
    decimal Latitude,
    decimal Longitude,
    string? City = null,
    string? State = null,
    string? PostalCode = null
);

/// <summary>
/// Represents an allocation of inventory from a specific location.
/// </summary>
public sealed record FulfillmentAllocation(
    Guid LocationId,
    string LocationName,
    int AllocatedQuantity,
    decimal? ShippingCost = null,
    int? EstimatedDays = null
);

/// <summary>
/// Defines a strategy for allocating inventory across multiple locations to fulfill an order.
/// Implementations determine which locations to source from based on various criteria.
/// </summary>
public interface IFulfillmentStrategy
{
    /// <summary>
    /// Allocates inventory from available locations based on the strategy's criteria.
    /// </summary>
    /// <param name="variant">The product variant to fulfill</param>
    /// <param name="quantity">The quantity required</param>
    /// <param name="availableLocations">Locations with sufficient stock</param>
    /// <param name="customerLocation">Customer's shipping location (for distance/cost calculations)</param>
    /// <returns>List of allocations in order of fulfillment preference</returns>
    Task<List<FulfillmentAllocation>> AllocateAsync(
        Variant variant,
        int quantity,
        IEnumerable<StockLocation> availableLocations,
        CustomerLocation? customerLocation = null);
}
