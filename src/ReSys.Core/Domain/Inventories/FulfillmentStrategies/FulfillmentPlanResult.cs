namespace ReSys.Core.Domain.Inventories.FulfillmentStrategies
{
    // Represents the overall plan for fulfilling an order, broken down by shipments.
    public record FulfillmentPlanResult
    {
        public List<FulfillmentShipmentPlan> Shipments { get; init; } = new();
        public bool IsPartialFulfillment { get; init; }
        public bool IsFullyFulfillable { get; init; }

        public static ErrorOr<FulfillmentPlanResult> Create(List<FulfillmentShipmentPlan>? shipments)
        {
            // Basic validation
            if (shipments == null || !shipments.Any())
            {
                return Error.Validation("FulfillmentPlan.Empty", "Fulfillment plan cannot be empty.");
            }

            // More complex validation (e.g., all line items covered, no over-allocation) can go here.

            var result = new FulfillmentPlanResult
            {
                Shipments = shipments,
                IsFullyFulfillable = true, // Placeholder, actual logic depends on coverage
                IsPartialFulfillment = false // Placeholder
            };
            return result;
        }
    }

    // Represents a single shipment within the overall fulfillment plan.
    public record FulfillmentShipmentPlan
    {
        public Guid FulfillmentLocationId { get; init; }
        public List<FulfillmentItem> Items { get; init; } = new();

        public static ErrorOr<FulfillmentShipmentPlan> Create(Guid fulfillmentLocationId, List<FulfillmentItem>? items)
        {
            if (fulfillmentLocationId == Guid.Empty)
            {
                return Error.Validation("FulfillmentShipmentPlan.LocationRequired", "Fulfillment location ID is required.");
            }
            if (items == null || !items.Any())
            {
                return Error.Validation("FulfillmentShipmentPlan.EmptyItems", "Shipment plan must contain items.");
            }

            var plan = new FulfillmentShipmentPlan
            {
                FulfillmentLocationId = fulfillmentLocationId,
                Items = items
            };
            return plan;
        }
    }

    // Represents a single line item quantity to be fulfilled from a specific location.
    public record FulfillmentItem
    {
        public Guid LineItemId { get; init; }
        public Guid VariantId { get; init; } // Added VariantId for easier lookup
        public int Quantity { get; init; }

        public static ErrorOr<FulfillmentItem> Create(Guid lineItemId, Guid variantId, int quantity)
        {
            if (lineItemId == Guid.Empty)
            {
                return Error.Validation("FulfillmentItem.LineItemIdRequired", "Line item ID is required.");
            }
            if (variantId == Guid.Empty)
            {
                return Error.Validation("FulfillmentItem.VariantIdRequired", "Variant ID is required.");
            }
            if (quantity <= 0)
            {
                return Error.Validation("FulfillmentItem.QuantityInvalid", "Quantity must be greater than zero.");
            }

            var item = new FulfillmentItem
            {
                LineItemId = lineItemId,
                VariantId = variantId,
                Quantity = quantity
            };
            return item;
        }
    }
}
