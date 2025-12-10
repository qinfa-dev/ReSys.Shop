namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies;

/// <summary>
/// Enumeration of available fulfillment strategies.
/// </summary>
public enum FulfillmentStrategyType
{
    /// <summary>Selects the closest location to the customer.</summary>
    Nearest = 1,

    /// <summary>Selects the location with the highest available inventory.</summary>
    HighestStock = 2,

    /// <summary>Selects the location with the lowest total fulfillment and shipping costs.</summary>
    CostOptimized = 3,

    /// <summary>Selects admin-configured preferred locations.</summary>
    Preferred = 4
}

/// <summary>
/// Factory for creating and managing fulfillment strategy instances.
/// </summary>
/// <remarks>
/// <para>
/// <b>Usage:</b>
/// <code>
/// var factory = new FulfillmentStrategyFactory();
/// var strategy = factory.CreateStrategy(FulfillmentStrategyType.Nearest);
/// var location = strategy.SelectLocation(variant, quantity, locations, lat, lon);
/// </code>
/// </para>
/// 
/// <para>
/// <b>Strategy Registry:</b>
/// The factory maintains a registry of all available strategies.
/// Custom strategies can be registered via constructor or extension methods.
/// </para>
/// </remarks>
public sealed class FulfillmentStrategyFactory
{
    private readonly Dictionary<FulfillmentStrategyType, Func<IFulfillmentStrategy>> _strategies;

    /// <summary>
    /// Initializes a new instance of the FulfillmentStrategyFactory with default strategies.
    /// </summary>
    public FulfillmentStrategyFactory()
    {
        _strategies = new Dictionary<FulfillmentStrategyType, Func<IFulfillmentStrategy>>
        {
            { FulfillmentStrategyType.Nearest, () => new NearestLocationStrategy() },
            { FulfillmentStrategyType.HighestStock, () => new HighestStockStrategy() },
            { FulfillmentStrategyType.CostOptimized, () => new CostOptimizedStrategy() },
            { FulfillmentStrategyType.Preferred, () => new PreferredLocationStrategy() }
        };
    }

    /// <summary>
    /// Creates an instance of the specified fulfillment strategy.
    /// </summary>
    /// <param name="strategyType">The type of strategy to create.</param>
    /// <returns>A new instance of the requested strategy.</returns>
    /// <exception cref="ArgumentException">Thrown if the strategy type is not registered.</exception>
    public IFulfillmentStrategy CreateStrategy(FulfillmentStrategyType strategyType)
    {
        if (!_strategies.TryGetValue(strategyType, out var factory))
        {
            throw new ArgumentException(
                $"Fulfillment strategy '{strategyType}' is not registered.",
                nameof(strategyType));
        }

        return factory();
    }

    /// <summary>
    /// Registers a custom fulfillment strategy.
    /// </summary>
    /// <param name="strategyType">The type to register under.</param>
    /// <param name="factory">A factory function that creates instances of the strategy.</param>
    /// <remarks>
    /// This allows registration of custom strategies that extend the default set.
    /// If a strategy type is already registered, it will be replaced.
    /// </remarks>
    public void RegisterStrategy(FulfillmentStrategyType strategyType, Func<IFulfillmentStrategy> factory)
    {
        _strategies[strategyType] = factory;
    }

    /// <summary>
    /// Gets all registered strategy types.
    /// </summary>
    public IReadOnlyList<FulfillmentStrategyType> GetRegisteredStrategies()
    {
        return _strategies.Keys.ToList().AsReadOnly();
    }

    /// <summary>
    /// Checks if a strategy type is registered.
    /// </summary>
    public bool IsStrategyRegistered(FulfillmentStrategyType strategyType)
    {
        return _strategies.ContainsKey(strategyType);
    }

    /// <summary>
    /// Gets the strategy instance by type (cached per call - creates new instance each time).
    /// </summary>
    /// <remarks>
    /// Consider implementing a more sophisticated cache if strategies are stateful
    /// or if factory creation becomes a bottleneck.
    /// Current implementation creates a new instance per call, which is safe for
    /// stateless strategies but may impact performance with frequent calls.
    /// </remarks>
    public IFulfillmentStrategy GetStrategy(FulfillmentStrategyType strategyType)
    {
        return CreateStrategy(strategyType);
    }
}

/// <summary>
/// Extension methods for working with fulfillment strategies.
/// </summary>
public static class FulfillmentStrategyExtensions
{
    /// <summary>
    /// Gets the display name of a fulfillment strategy type.
    /// </summary>
    public static string GetDisplayName(this FulfillmentStrategyType strategyType)
    {
        return strategyType switch
        {
            FulfillmentStrategyType.Nearest => "Nearest Location",
            FulfillmentStrategyType.HighestStock => "Highest Stock",
            FulfillmentStrategyType.CostOptimized => "Cost Optimized",
            FulfillmentStrategyType.Preferred => "Preferred Location",
            _ => strategyType.ToString()
        };
    }

    /// <summary>
    /// Gets the description of a fulfillment strategy type.
    /// </summary>
    public static string GetDescription(this FulfillmentStrategyType strategyType)
    {
        return strategyType switch
        {
            FulfillmentStrategyType.Nearest => 
                "Selects the location closest to the customer based on geographic distance.",
            FulfillmentStrategyType.HighestStock => 
                "Selects the location with the most available inventory.",
            FulfillmentStrategyType.CostOptimized => 
                "Selects the location to minimize total fulfillment and shipping costs.",
            FulfillmentStrategyType.Preferred => 
                "Selects admin-configured preferred locations.",
            _ => "Unknown strategy"
        };
    }
}
