//using ErrorOr;
//using ReSys.Core.Common.Domain.Concerns; // Added for IHasAuditable
//using ReSys.Core.Common.Domain.Entities;
//using ReSys.Core.Domain.Catalog.Products.Variants;
//using System;

//using ReSys.Core.Domain.Orders.LineItems;

//namespace ReSys.Core.Domain.Fulfillment;

///// <summary>
///// Represents a specific item (product variant and quantity) that is part of a <see cref="FulfillmentOrder"/>.
///// This entity details what needs to be picked, packed, and shipped.
///// </summary>
///// <remarks>
///// <para>
///// <strong>Role in Fulfillment:</strong>
///// <list type="bullet">
///// <item>
///// <term>Granular Tracking</term>
///// <description>Tracks individual product variants within a fulfillment order.</description>
///// </item>
///// <item>
///// <term>Reference to Order Line Item</term>
///// <description>Maintains a link to the original customer order's line item.</description>
///// </item>
///// <item>
///// <term>Auditable</term>
///// <description>Inherits auditing capabilities for tracking creation timestamps.</description>
///// </item>
///// </list>
///// </para>
/////
///// <para>
///// This is an owned entity of the <see cref="FulfillmentOrder"/> aggregate, meaning its lifecycle
///// is managed by its parent fulfillment order. It is never accessed directly.
///// </para>
///// </remarks>
//public sealed class FulfillmentLineItem : AuditableEntity<Guid>
//{
//    /// <summary>
//    /// Gets or sets the unique identifier of the parent <see cref="FulfillmentOrder"/>.
//    /// </summary>
//    public Guid FulfillmentOrderId { get; set; }

//    /// <summary>
//    /// Gets or sets the unique identifier of the original <see cref="LineItem"/> from the customer order.
//    /// This links the fulfillment item back to the customer's purchase.
//    /// </summary>
//    public Guid LineItemId { get; set; } // Reference to the original Order LineItem

//    /// <summary>
//    /// Gets or sets the unique identifier of the product <see cref="Variant"/> to be fulfilled.
//    /// </summary>
//    public Guid VariantId { get; set; }

//    /// <summary>
//    /// Gets or sets the quantity of the product variant to be fulfilled.
//    /// </summary>
//    public int Quantity { get; set; }

//    #region Navigation properties

//    /// <summary>
//    /// Gets or sets the navigation property to the parent <see cref="FulfillmentOrder"/>.
//    /// </summary>
//    public FulfillmentOrder FulfillmentOrder { get; set; } = null!;

//    /// <summary>
//    /// Gets or sets the navigation property to the product <see cref="Variant"/> being fulfilled.
//    /// This allows access to variant details if needed (e.g., SKU, weight).
//    /// </summary>
//    public Variant Variant { get; set; } = null!; // To get variant details if needed

//    #endregion

//    /// <summary>
//    /// Private constructor for ORM (Entity Framework Core) materialization.
//    /// </summary>
//    private FulfillmentLineItem() { }

//    /// <summary>
//    /// Factory method to create a new <see cref="FulfillmentLineItem"/> instance.
//    /// </summary>
//    /// <param name="fulfillmentOrderId">The unique identifier of the parent fulfillment order.</param>
//    /// <param name="lineItemId">The unique identifier of the original order line item.</param>
//    /// <param name="variantId">The unique identifier of the product variant to fulfill.</param>
//    /// <param name="quantity">The quantity of the product variant to fulfill.</param>
//    /// <returns>
//    /// An <see cref="ErrorOr{FulfillmentLineItem}"/> result.
//    /// Returns the newly created <see cref="FulfillmentLineItem"/> instance on success.
//    /// Returns <see cref="Error.Validation"/> if any required ID is empty or quantity is not positive.
//    /// </returns>
//    public static ErrorOr<FulfillmentLineItem> Create(Guid fulfillmentOrderId, Guid lineItemId, Guid variantId,
//        int quantity)
//    {
//        if (fulfillmentOrderId == Guid.Empty)
//            return Error.Validation("FulfillmentLineItem.InvalidFulfillmentOrder", "Fulfillment Order ID is required.");
//        if (lineItemId == Guid.Empty)
//            return Error.Validation("FulfillmentLineItem.InvalidLineItem", "Line Item ID is required.");
//        if (variantId == Guid.Empty)
//            return Error.Validation("FulfillmentLineItem.InvalidVariant", "Variant ID is required.");
//        if (quantity <= 0) return Error.Validation("FulfillmentLineItem.InvalidQuantity", "Quantity must be positive.");

//        return new FulfillmentLineItem
//        {
//            Id = Guid.NewGuid(),
//            FulfillmentOrderId = fulfillmentOrderId,
//            LineItemId = lineItemId,
//            VariantId = variantId,
//            Quantity = quantity,
//            CreatedAt = DateTimeOffset.UtcNow
//        };
//    }
//}
