using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies
{
    public class FulfillmentPlanner(FulfillmentStrategyFactory strategyFactory, IUnitOfWork unitOfWork)
        : IFulfillmentPlanner
    {

        public async Task<ErrorOr<FulfillmentPlanResult>> PlanFulfillment(Order order, string strategyType, CancellationToken cancellationToken = default)
        {
            if (order is null) return Error.NotFound("Order.NotFound", "Order cannot be null.");
            if (!order.LineItems.Any()) return Error.Validation("Order.Empty", "Order has no line items to fulfill.");

            if (!Enum.TryParse(strategyType, true, out FulfillmentStrategyType parsedStrategyType))
            {
                // Fallback to a default strategy if the provided one is invalid
                parsedStrategyType = FulfillmentStrategyType.HighestStock;
            }

            var strategy = strategyFactory.GetStrategy(parsedStrategyType);

            var fulfillmentShipmentPlans = new List<FulfillmentShipmentPlan>();
            var remainingLineItems = order.LineItems.ToDictionary(li => li.Id, li => li.Quantity);

            foreach (var lineItem in order.LineItems)
            {
                // Fetch variant with stock locations
                var variant = await unitOfWork.Context.Set<Variant>()
                    .Include(v => v.StockItems).ThenInclude(si => si.StockLocation)
                    .FirstOrDefaultAsync(v => v.Id == lineItem.VariantId, cancellationToken);

                if (variant is null)
                {
                    return Error.NotFound("Variant.NotFound", $"Variant {lineItem.VariantId} for line item {lineItem.Id} not found.");
                }

                // Get available stock locations for this variant
                var availableLocations = variant.StockItems
                    .Where(si => si.QuantityOnHand >= lineItem.Quantity) // For simplicity, assume enough stock in one location
                    .Select(si => si.StockLocation)
                    .ToList();

                if (!availableLocations.Any())
                {
                    // This means we can't fully fulfill from a single location
                    // Need more sophisticated logic for partial fulfillment/backorder
                    return Error.Validation("Fulfillment.NoStock", $"No single location has enough stock for variant {variant.Id}.");
                }

                // For simplicity, using SelectLocation here.
                // A more complex planner would use SelectMultipleLocations if needed.
                var selectedLocation = strategy.SelectLocation(
                    variant, 
                    lineItem.Quantity, 
                    availableLocations
                );

                if (selectedLocation is null)
                {
                    return Error.Validation("Fulfillment.NoLocation", $"Could not find a suitable fulfillment location for variant {variant.Id}.");
                }

                // Find existing shipment plan for this location or create a new one
                var shipmentPlan = fulfillmentShipmentPlans.FirstOrDefault(p => p.FulfillmentLocationId == selectedLocation.Id);
                if (shipmentPlan is null)
                {
                    var shipmentPlanResult = FulfillmentShipmentPlan.Create(selectedLocation.Id, new List<FulfillmentItem>());
                    if (shipmentPlanResult.IsError) return shipmentPlanResult.Errors;
                    shipmentPlan = shipmentPlanResult.Value;
                    fulfillmentShipmentPlans.Add(shipmentPlan);
                }

                var fulfillmentItemResult = FulfillmentItem.Create(lineItem.Id, variant.Id, lineItem.Quantity);
                if (fulfillmentItemResult.IsError) return fulfillmentItemResult.Errors;
                shipmentPlan.Items.Add(fulfillmentItemResult.Value);
            }

            var planResult = FulfillmentPlanResult.Create(fulfillmentShipmentPlans);
            if (planResult.IsError) return planResult.Errors;

            // TODO: Determine IsPartialFulfillment and IsFullyFulfillable based on all line items
            return planResult.Value;
        }
    }
}
