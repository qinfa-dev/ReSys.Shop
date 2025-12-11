using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Domain.Promotions.Calculations;
using ReSys.Core.Domain.Promotions.Promotions;

namespace ReSys.Core.Domain.Promotions.Actions;

/// <summary>
/// Defines how a promotion calculates discounts or rewards.
/// Supports multiple discount types: order discounts, item discounts, free shipping, and buy-x-get-y offers.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Encapsulates the discount/reward calculation logic for different promotion types.
/// Acts as a strategy pattern implementation where each action type has its own calculation method.
/// </para>
/// 
/// <para>
/// <b>Supported Promotion Types:</b>
/// <list type="bullet">
/// <item><b>OrderDiscount:</b> Apply a fixed amount or percentage discount to the entire order</item>
/// <item><b>ItemDiscount:</b> Apply a discount to specific eligible items only</item>
/// <item><b>FreeShipping:</b> Waive shipping costs (if any)</item>
/// <item><b>BuyXGetY:</b> Buy a specific quantity of one variant, get free quantity of another</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Metadata Storage:</b>
/// Action configuration (discount percentage, fixed amount, variant IDs, etc.) is stored in
/// PrivateMetadata dictionary to avoid polluting the entity properties. Use GetPrivate/SetPrivate helpers.
/// </para>
/// 
/// <para>
/// <b>Example Usage:</b>
/// <code>
/// // Create a 20% off order discount promotion
/// var action = PromotionAction.CreateOrderDiscount(
///     discountType: Promotion.DiscountType.Percentage,
///     value: 20m);
/// 
/// // Create a buy 2, get 1 free promotion
/// var action = PromotionAction.CreateBuyXGetY(
///     buyVariantId: variantId1,
///     buyQuantity: 2,
///     getVariantId: variantId2,
///     getQuantity: 1);
/// </code>
/// </para>
/// </remarks>
public sealed class PromotionAction : AuditableEntity<Guid>, IHasMetadata
{
    #region Constraints
    public static class Constraints
    {
        public const int MinQuantity = 1;
        public const decimal MinValue = 0m;
        public const decimal MaxValue = 1_000_000m;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error InvalidBuyQuantity =>
            Error.Validation(
                code: "PromotionAction.InvalidBuyQuantity",
                description: "Buy quantity must be greater than zero.");

        public static Error InvalidGetQuantity =>
            Error.Validation(
                code: "PromotionAction.InvalidGetQuantity",
                description: "Get quantity must be greater than zero.");

        public static Error InvalidValue =>
            Error.Validation(
                code: "PromotionAction.InvalidValue",
                description: $"Value must be >= {Constraints.MinValue}.");

        public static Error InvalidDiscountType =>
            Error.Validation(
                code: "PromotionAction.InvalidDiscountType",
                description: "Invalid discount type.");

        public static Error InvalidPercentageValue =>
            Error.Validation(
                code: "PromotionAction.InvalidPercentageValue",
                description: "Percentage discount value must be between 0.0m and 1.0m (0% to 100%).");

        public static Error Required =>
            Error.Validation(
                code: "PromotionAction.Required",
                description: "Promotion action is required.");
    }
    #endregion

    #region Properties
    public Guid PromotionId { get; set; }
    public Promotion Promotion { get; set; } = null!;

    /// <summary>Gets the promotion type (OrderDiscount, ItemDiscount, FreeShipping, or BuyXGetY).</summary>
    public Promotion.PromotionType Type { get; set; }

    public IDictionary<string, object?>? PublicMetadata { get; set; }
    public IDictionary<string, object?>? PrivateMetadata { get; set; }
    #endregion

    #region Constructors
    private PromotionAction() { }
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates an order-level discount action that applies to the entire order.
    /// </summary>
    /// <param name="discountType">Percentage-based or fixed amount discount.</param>
    /// <param name="value">Discount value (0-1.0m for percentage, amount in currency for fixed).</param>
    /// <returns>
    /// On success: A new PromotionAction configured for order discounts.
    /// On failure: Validation error if value is invalid.
    /// </returns>
    public static ErrorOr<PromotionAction> CreateOrderDiscount(
        Promotion.DiscountType discountType,
        decimal value)
    {
        if (value < Constraints.MinValue)
            return Errors.InvalidValue;

        if (discountType == Promotion.DiscountType.Percentage && (value < 0.0m || value > 1.0m))
        {
            return Errors.InvalidPercentageValue;
        }

        var action = new PromotionAction
        {
            Id = Guid.NewGuid(),
            Type = Promotion.PromotionType.OrderDiscount
        };

        action.SetPrivate(key: "discountType", value: discountType);
        action.SetPrivate(key: "value", value: value);

        return action;
    }

    /// <summary>
    /// Creates an item-level discount action that applies only to eligible items.
    /// </summary>
    /// <param name="discountType">Percentage-based or fixed amount discount.</param>
    /// <param name="value">Discount value (0-1.0m for percentage, amount in currency for fixed).</param>
    /// <returns>
    /// On success: A new PromotionAction configured for item discounts.
    /// On failure: Validation error if value is invalid.
    /// </returns>
    public static ErrorOr<PromotionAction> CreateItemDiscount(
        Promotion.DiscountType discountType,
        decimal value)
    {
        if (value < Constraints.MinValue)
            return Errors.InvalidValue;

        if (discountType == Promotion.DiscountType.Percentage && (value < 0.0m || value > 1.0m))
        {
            return Errors.InvalidPercentageValue;
        }

        var action = new PromotionAction
        {
            Id = Guid.NewGuid(),
            Type = Promotion.PromotionType.ItemDiscount
        };

        action.SetPrivate(key: "discountType", value: discountType);
        action.SetPrivate(key: "value", value: value);

        return action;
    }

    /// <summary>
    /// Creates a free shipping action that waives shipping costs.
    /// </summary>
    /// <remarks>
    /// Only applies to orders with physical items (not fully digital orders).
    /// </remarks>
    public static PromotionAction CreateFreeShipping()
        => new()
        {
            Id = Guid.NewGuid(),
            Type = Promotion.PromotionType.FreeShipping
        };

    /// <summary>
    /// Creates a buy-x-get-y action: buy a quantity of one product, get a quantity of another free.
    /// </summary>
    /// <param name="buyVariantId">The variant ID that must be purchased.</param>
    /// <param name="buyQuantity">How many units of the buy variant must be purchased (must be >= 1).</param>
    /// <param name="getVariantId">The variant ID that becomes free.</param>
    /// <param name="getQuantity">How many units of the get variant become free (must be >= 1).</param>
    /// <returns>
    /// On success: A new PromotionAction configured for buy-x-get-y promotions.
    /// On failure: Validation error if quantities are less than 1.
    /// </returns>
    /// <remarks>
    /// Example: Buy 2 red shirts, get 1 blue shirt free.
    /// If customer buys 4 red shirts, they get 2 blue shirts free (2 cycles of the 2:1 ratio).
    /// </remarks>
    public static ErrorOr<PromotionAction> CreateBuyXGetY(
        Guid buyVariantId,
        int buyQuantity,
        Guid getVariantId,
        int getQuantity)
    {
        if (buyQuantity < Constraints.MinQuantity)
            return Errors.InvalidBuyQuantity;

        if (getQuantity < Constraints.MinQuantity)
            return Errors.InvalidGetQuantity;

        var action = new PromotionAction
        {
            Id = Guid.NewGuid(),
            Type = Promotion.PromotionType.BuyXGetY
        };

        action.SetPrivate(key: "buyVariantId", value: buyVariantId);
        action.SetPrivate(key: "buyQuantity", value: buyQuantity);
        action.SetPrivate(key: "getVariantId", value: getVariantId);
        action.SetPrivate(key: "getQuantity", value: getQuantity);

        return action;
    }

    #endregion

    #region Business Logic: Calculation

    /// <summary>
    /// Calculates the adjustments (discounts/rewards) based on the promotion action type and order context.
    /// </summary>
    /// <param name="context">The calculation context containing the promotion, order, and eligible items.</param>
    /// <returns>A list of adjustments to apply. May be empty if promotion doesn't apply.</returns>
    /// <remarks>
    /// Delegates to type-specific calculation methods. Each method handles:
    /// <list type="bullet">
    /// <item>Retrieving action configuration from PrivateMetadata</item>
    /// <item>Calculating the discount amount</item>
    /// <item>Respecting MaximumDiscountAmount cap from promotion</item>
    /// <item>Determining scope (order-level or per line-item)</item>
    /// <item>Handling rounding and edge cases</item>
    /// </list>
    /// </remarks>
    public List<PromotionAdjustment> Calculate(PromotionCalculationContext context)
    {
        return Type switch
        {
            Promotion.PromotionType.OrderDiscount => CalculateOrderDiscount(context),
            Promotion.PromotionType.ItemDiscount => CalculateItemDiscount(context),
            Promotion.PromotionType.FreeShipping => CalculateFreeShipping(context),
            Promotion.PromotionType.BuyXGetY => CalculateBuyXGetY(context),
            _ => new List<PromotionAdjustment>()
        };
    }

    /// <summary>
    /// Calculates an order-level discount applied to the entire order.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Calculation:</b>
    /// <list type="bullet">
    /// <item>If percentage: discount = order.ItemTotal * (value / 100)</item>
    /// <item>If fixed: discount = value</item>
    /// <item>Capped by: order total and promotion MaximumDiscountAmount</item>
    /// </list>
    /// </para>
    /// </remarks>
    private List<PromotionAdjustment> CalculateOrderDiscount(PromotionCalculationContext context)
    {
        var adjustments = new List<PromotionAdjustment>();
        var discountType = this.GetPrivate<Promotion.DiscountType?>(key: "discountType") ?? Promotion.DiscountType.FixedAmount;
        var value = this.GetPrivate<decimal?>(key: "value") ?? 0m;

        // Calculate base discount
        var discountAmount = discountType == Promotion.DiscountType.Percentage
            ? context.Order.ItemTotal * (value / 100m)
            : value;

        // Cap by order total
        discountAmount = Math.Min(discountAmount, context.Order.ItemTotal);

        // Cap by promotion maximum
        if (context.Promotion.MaximumDiscountAmount.HasValue)
            discountAmount = Math.Min(discountAmount, context.Promotion.MaximumDiscountAmount.Value);

        if (discountAmount > 0)
        {
            adjustments.Add(new PromotionAdjustment(
                Description: $"Discount: {context.Promotion.Name}",
                Amount: -(long)(discountAmount * 100)));
        }

        return adjustments;
    }

    /// <summary>
    /// Calculates a discount applied only to eligible line items.
    /// Distributes the discount proportionally across items.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Distribution Logic:</b>
    /// <list type="bullet">
    /// <item>Calculate total discount amount (percentage or fixed)</item>
    /// <item>Distribute proportionally across eligible items based on item subtotal</item>
    /// <item>Handle rounding by absorbing difference in the last item</item>
    /// <item>Respect MaximumDiscountAmount cap</item>
    /// </list>
    /// </para>
    /// </remarks>
    private List<PromotionAdjustment> CalculateItemDiscount(PromotionCalculationContext context)
    {
        var adjustments = new List<PromotionAdjustment>();
        var baseAmount = context.EligibleItems.Sum(li => li.Subtotal);

        if (baseAmount == 0)
            return adjustments;

        var discountType = this.GetPrivate<Promotion.DiscountType?>(key: "discountType") ?? Promotion.DiscountType.FixedAmount;
        var value = this.GetPrivate<decimal?>(key: "value") ?? 0m;

        // Calculate total discount
        var totalDiscount = discountType == Promotion.DiscountType.Percentage
            ? baseAmount * (value / 100m)
            : value;

        // Cap by eligible items total
        totalDiscount = Math.Min(totalDiscount, baseAmount);

        // Cap by promotion maximum
        if (context.Promotion.MaximumDiscountAmount.HasValue)
            totalDiscount = Math.Min(totalDiscount, context.Promotion.MaximumDiscountAmount.Value);

        var remainingDiscount = totalDiscount;

        // Distribute discount across items proportionally
        for (int i = 0; i < context.EligibleItems.Count; i++)
        {
            var item = context.EligibleItems[i];
            decimal discount;

            if (i == context.EligibleItems.Count - 1)
            {
                // Last item absorbs any rounding difference
                discount = remainingDiscount;
            }
            else
            {
                var proportion = item.Subtotal / baseAmount;
                discount = Math.Round(totalDiscount * proportion, 2);
                remainingDiscount -= discount;
            }

            if (discount > 0)
            {
                adjustments.Add(new PromotionAdjustment(
                    Description: $"Discount: {context.Promotion.Name}",
                    Amount: -(long)(discount * 100),
                    LineItemId: item.Id));
            }
        }

        return adjustments;
    }

    /// <summary>
    /// Calculates a free shipping discount.
    /// </summary>
    /// <remarks>
    /// Only applies to orders with physical items (not fully digital).
    /// Respects MaximumDiscountAmount cap.
    /// </remarks>
    private List<PromotionAdjustment> CalculateFreeShipping(PromotionCalculationContext context)
    {
        var adjustments = new List<PromotionAdjustment>();

        // Skip if fully digital order or no shipping cost
        if (context.Order.IsFullyDigital || context.Order.ShipmentTotal <= 0)
            return adjustments;

        decimal discountAmount = context.Order.ShipmentTotal;

        // Cap by promotion maximum
        if (context.Promotion.MaximumDiscountAmount.HasValue)
            discountAmount = Math.Min(discountAmount, context.Promotion.MaximumDiscountAmount.Value);

        if (discountAmount > 0)
        {
            adjustments.Add(new PromotionAdjustment(
                Description: $"Free Shipping: {context.Promotion.Name}",
                Amount: -(long)(discountAmount * 100)));
        }

        return adjustments;
    }

    /// <summary>
    /// Calculates a buy-x-get-y free promotion.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b>Calculation Logic:</b>
    /// <list type="number">
    /// <item>Get quantity of buy variant that meets the buy requirement</item>
    /// <item>Calculate how many free units customer qualifies for (cycles * getQuantity)</item>
    /// <item>Find matching get variant items and apply discount</item>
    /// <item>Respect MaximumDiscountAmount cap</item>
    /// <item>Prioritize discounting items in order they appear</item>
    /// </list>
    /// </para>
    /// 
    /// <para>
    /// <b>Example:</b>
    /// Rule: Buy 2, Get 1 Free
    /// Customer buys: 4 units of variant A, 3 units of variant B
    /// Result: Gets 2 units of B free (4 � 2 = 2 cycles, 2 * 1 = 2 free units)
    /// </para>
    /// </remarks>
    private List<PromotionAdjustment> CalculateBuyXGetY(PromotionCalculationContext context)
    {
        var adjustments = new List<PromotionAdjustment>();

        var buyVariantId = this.GetPrivate<Guid?>(key: "buyVariantId");
        var buyQuantity = this.GetPrivate<int?>(key: "buyQuantity") ?? 0;
        var getVariantId = this.GetPrivate<Guid?>(key: "getVariantId");
        var getQuantity = this.GetPrivate<int?>(key: "getQuantity") ?? 0;

        // Validate configuration
        if (!buyVariantId.HasValue || !getVariantId.HasValue || buyQuantity < 1 || getQuantity < 1)
            return adjustments;

        // Find items of the buy variant
        var boughtItems = context.EligibleItems
            .Where(li => li.VariantId == buyVariantId.Value)
            .ToList();

        if (!boughtItems.Any())
            return adjustments;

        // Calculate total bought quantity
        var boughtQty = boughtItems.Sum(li => li.Quantity);
        if (boughtQty < buyQuantity)
            return adjustments; // Doesn't meet minimum buy requirement

        // Calculate how many free items customer qualifies for
        var numCycles = boughtQty / buyQuantity;
        var freeQty = numCycles * getQuantity;

        // Find items of the get variant
        var getItems = context.EligibleItems
            .Where(li => li.VariantId == getVariantId.Value)
            .ToList();

        var availableFreeQty = getItems.Sum(li => li.Quantity);
        var qtyToDiscount = Math.Min(availableFreeQty, freeQty);

        if (qtyToDiscount <= 0)
            return adjustments;

        int remainingToDiscount = qtyToDiscount;

        // Apply discount to get items
        foreach (var item in getItems)
        {
            var discountQtyForItem = Math.Min(item.Quantity, remainingToDiscount);
            var discount = discountQtyForItem * item.UnitPrice;

            // Cap by promotion maximum
            if (context.Promotion.MaximumDiscountAmount.HasValue)
                discount = Math.Min(discount, context.Promotion.MaximumDiscountAmount.Value);

            if (discount > 0)
            {
                adjustments.Add(new PromotionAdjustment(
                    Description: $"Free Item: {context.Promotion.Name}",
                    Amount: -(long)(discount * 100),
                    LineItemId: item.Id));
            }

            remainingToDiscount -= discountQtyForItem;
            if (remainingToDiscount <= 0)
                break;
        }

        return adjustments;
    }

    #endregion
}
