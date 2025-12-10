namespace ReSys.Core.Domain.Inventories.Locations.FulfillmentStrategies;

using ReSys.Core.Feature.Storefront.Orders.Commands.FulfillOrder;

/// <summary>
/// Factory for creating fulfillment strategy instances based on the selected strategy type.
/// </summary>
public interface IFulfillmentStrategyFactory
{
    /// <summary>
    /// Creates a fulfillment strategy instance for the given strategy type.
    /// </summary>
    /// <param name="strategy">The fulfillment strategy type to instantiate.</param>
    /// <param name="preferredLocationId">Optional preferred location ID for PreferredLocationStrategy.</param>
    /// <returns>An instance of the requested fulfillment strategy.</returns>
    /// <exception cref="InvalidOperationException">Thrown when an unknown strategy is requested.</exception>
    IFulfillmentStrategy Create(FulfillmentStrategy strategy, Guid? preferredLocationId = null);
}
