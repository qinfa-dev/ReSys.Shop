using ReSys.Core.Domain.Orders;

namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies
{
    public interface IFulfillmentPlanner
    {
        /// <summary>
        /// Creates a fulfillment plan for an order, determining which items should be shipped from which locations.
        /// </summary>
        /// <param name="order">The order for which to create the fulfillment plan.</param>
        /// <param name="strategyType">The type of fulfillment strategy to use (e.g., "NearestLocation", "HighestStock").</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>An ErrorOr containing the FulfillmentPlanResult or a list of errors.</returns>
        Task<ErrorOr<FulfillmentPlanResult>> PlanFulfillment(Order order, string strategyType, CancellationToken cancellationToken = default);
    }
}
