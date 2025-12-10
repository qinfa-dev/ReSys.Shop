namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

using ReSys.Core.Feature.Storefront.Orders.Commands.FulfillOrder;

/// <summary>
/// Default implementation of the fulfillment strategy factory.
/// Creates strategy instances based on the selected strategy type.
/// </summary>
public sealed class FulfillmentStrategyFactory : IFulfillmentStrategyFactory
{
    /// <summary>
    /// Creates a fulfillment strategy instance for the given strategy type.
    /// </summary>
    /// <param name="strategy">The fulfillment strategy type to instantiate.</param>
    /// <param name="preferredLocationId">Optional preferred location ID for PreferredLocationStrategy.</param>
    /// <returns>An instance of the requested fulfillment strategy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when CostOptimized is requested (not yet implemented) or PreferredLocationStrategy without preferredLocationId.</exception>
    public IFulfillmentStrategy Create(FulfillmentStrategy strategy, Guid? preferredLocationId = null) =>
        strategy switch
        {
            FulfillmentStrategy.Nearest => new NearestLocationStrategy(),
            FulfillmentStrategy.HighestStock => new HighestStockStrategy(),
            FulfillmentStrategy.Preferred =>
                preferredLocationId.HasValue
                    ? new PreferredLocationStrategy(preferredLocationId.Value)
                    : throw new InvalidOperationException(
                        "PreferredLocationStrategy requires a preferredLocationId."),
            FulfillmentStrategy.CostOptimized => throw new InvalidOperationException(
                "CostOptimized strategy is not yet implemented. It requires a shipping calculator service."),
            _ => throw new InvalidOperationException($"Unknown fulfillment strategy: {strategy}")
        };
}
