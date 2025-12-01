using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Concerns;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Identity.UserAddresses;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Domain.Inventories.Locations;
using ReSys.Core.Domain.Orders.Adjustments;
using ReSys.Core.Domain.Orders.LineItems;
using ReSys.Core.Domain.Orders.Payments;
using ReSys.Core.Domain.Orders.Shipments;
using ReSys.Core.Domain.Promotions.Calculations;
using ReSys.Core.Domain.Promotions.Promotions;
using ReSys.Core.Domain.Shipping;
using ReSys.Core.Domain.Stores;

namespace ReSys.Core.Domain.Orders;

/// <summary>
/// Represents an order in the e-commerce system. This is the aggregate root for the Orders bounded context.
/// 
/// The Order manages the entire customer purchase lifecycle from initial shopping cart through payment
/// and fulfillment. It orchestrates interactions with multiple sub-domains (Catalog, Shipping, Payments,
/// Inventories, Promotions) while maintaining invariant consistency and business rule enforcement.
/// 
/// <example>
/// <code>
/// // Create a new order
/// var orderResult = Order.Create(storeId: store.Id, currency: "USD", userId: user.Id);
/// if (orderResult.IsError) return Problem(orderResult.FirstError);
/// var order = orderResult.Value;
/// 
/// // Add items to cart
/// var addResult = order.AddLineItem(variant: selectedVariant, quantity: 2);
/// if (addResult.IsError) return Problem(addResult.FirstError);
/// 
/// // Set addresses and progress through states
/// await order.SetShippingAddress(address);
/// await order.SetBillingAddress(address);
/// var toDeliveryResult = order.Next(); // Cart → Address → Delivery
/// 
/// // Select shipping and apply promotion
/// order.SetShippingMethod(shippingMethod);
/// order.ApplyPromotion(promotion, code: "SUMMER20");
/// var toPaymentResult = order.Next(); // Delivery → Payment
/// 
/// // Process payment and complete
/// order.AddPayment(amountCents, paymentMethodId, paymentType);
/// var completeResult = order.Complete(); // Payment → Confirm → Complete
/// 
/// // Save changes (events published after SaveChangesAsync)
/// _dbContext.Orders.Add(order);
/// await _dbContext.SaveChangesAsync();
/// </code>
/// </example>
/// 
/// <remarks>
/// KEY PATTERNS:
/// • Factory Method: Use Order.Create() for safe instantiation with validation
/// • State Machine: Use Next() for valid state transitions (Cart → Complete)
/// • ErrorOr: All operations return ErrorOr for railway-oriented error handling
/// • Domain Events: Significant changes published as domain events for decoupled integration
/// • Owned Entities: LineItems, Adjustments, Payments accessible only through Order aggregate
/// 
/// DIGITAL VS PHYSICAL ORDERS:
/// • Digital: No shipping address/method/fulfillment location required
/// • Physical: Addresses and shipping method mandatory before Payment state
/// • Fully digital orders auto-skip Address and Delivery validation
/// 
/// FINANCIAL PRECISION:
/// • All monetary values stored in cents (long) to avoid floating-point precision issues
/// • Totals automatically recalculated after changes: ItemTotal + ShipmentTotal + AdjustmentTotal
/// • Adjustments can be negative (discounts) or positive (taxes, fees)
/// 
/// PROMOTION HANDLING:
/// • Single promotion per order (replaces previous if reapplied)
/// • Coupon codes validated if promotion requires
/// • Adjustments distributed at order-level or line-item level as configured
/// • Non-promotion adjustments preserved when switching promotions
/// 
/// INVENTORY COORDINATION:
/// • Domain events signal inventory operations: FinalizeInventory (completion), ReleaseInventory (cancellation)
/// • Event handlers coordinate with Inventories bounded context
/// • Multi-location fulfillment via FulfillmentLocation assignment
/// </remarks>
/// </summary>
public class Order : Aggregate, IHasMetadata
{
    /// <summary>
    /// Defines the valid states an order progresses through during its lifecycle.
    /// </summary>
    /// <remarks>
    /// Order state progression follows this sequence:
    /// 
    /// 1. <see cref="OrderState.Cart"/> - Initial state. Customer adds/removes items, reviews cart contents.
    /// 2. <see cref="OrderState.Address"/> - Shipping and billing addresses set (if physical order).
    /// 3. <see cref="OrderState.Delivery"/> - Shipping method selected, fulfillment location assigned.
    /// 4. <see cref="OrderState.Payment"/> - Payment method authorized and captured.
    /// 5. <see cref="OrderState.Confirm"/> - Final review before order completion.
    /// 6. <see cref="OrderState.Complete"/> - Order completed, inventory finalized. Terminal state.
    /// 
    /// Alternative path:
    /// - <see cref="OrderState.Canceled"/> - Can be reached from any non-Complete state. Terminal state.
    /// 
    /// Transitions are one-way forward (except Cancel which is available anytime) and enforced
    /// by the Next() method which validates prerequisites before allowing transitions.
    /// </remarks>
    public enum OrderState { Cart = 0, Address = 1, Delivery = 2, Payment = 3, Confirm = 4, Complete = 5, Canceled = 6 }

    #region Constraints
    /// <summary>
    /// Defines all constraints and validation limits for Order domain objects.
    /// These constraints prevent invalid states and maintain data consistency.
    /// </summary>
    /// <remarks>
    /// FINANCIAL CONSTRAINTS:
    /// • AmountCentsMinValue: Ensures non-negative payment amounts (no negative payments allowed)
    /// • All monetary values stored as cents (long) for precision and to avoid float rounding
    /// 
    /// INVENTORY CONSTRAINTS:
    /// • QuantityMinValue: Prevents orders with zero or negative quantities per item
    /// • At least 1 unit must be ordered per line item
    /// 
    /// STRING CONSTRAINTS:
    /// • EmailMaxLength: Email addresses limited to email specification max
    /// • CurrencyMaxLength: ISO 4217 currency codes are always 3 characters
    /// • PromoCodeMaxLength: Promotional codes padded for business flexibility
    /// • SpecialInstructionsMaxLength: Allow reasonable instruction length for customer notes
    /// </remarks>
    public static class Constraints
    {
        /// <summary>Maximum length for customer email address.</summary>
        public const int EmailMaxLength = CommonInput.Constraints.Email.MaxLength;
        
        /// <summary>Maximum length for special instructions/notes provided by customer.</summary>
        public const int SpecialInstructionsMaxLength = CommonInput.Constraints.Text.MediumTextMaxLength;
        
        /// <summary>Maximum length for promotional/coupon code.</summary>
        public const int PromoCodeMaxLength = 50;
        
        /// <summary>Maximum length for payment method type identifier (e.g., "CreditCard", "PayPal").</summary>
        public const int PaymentMethodTypeMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
        
        /// <summary>Minimum quantity allowed per line item (must be at least 1).</summary>
        public const int QuantityMinValue = 1;
        
        /// <summary>Minimum amount in cents allowed for payments (no negative amounts).</summary>
        public const long AmountCentsMinValue = 0;
        
        /// <summary>Length of ISO 4217 currency code (always 3 characters: USD, EUR, etc.).</summary>
        public const int CurrencyMaxLength = CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines all possible error scenarios in the Order domain.
    /// Each error method provides a specific, actionable error message for debugging and user feedback.
    /// </summary>
    /// <remarks>
    /// ERROR CATEGORIES:
    /// 
    /// STATE TRANSITION ERRORS:
    /// • InvalidStateTransition: Attempted invalid order state progression
    /// • CannotCancelCompleted: Cannot cancel completed orders (invariant violation)
    /// 
    /// LINE ITEM ERRORS:
    /// • LineItemNotFound: Referenced line item does not exist
    /// • InvalidQuantity: Quantity violates minimum value constraint
    /// • VariantNotPurchasable: Variant is not available for purchase
    /// 
    /// ADDRESS ERRORS:
    /// • AddressRequired: Physical orders require shipping/billing addresses
    /// • DigitalOrderNoShipping: Digital orders cannot have shipping address
    /// 
    /// SHIPPING ERRORS:
    /// • ShippingMethodRequired: Physical orders must have shipping method selected
    /// • DigitalOrderNoShipping: Digital orders cannot use shipping
    /// 
    /// PAYMENT ERRORS:
    /// • InvalidAmountCents: Payment amount violates minimum constraint
    /// • PaymentRequired: Order total not fully covered by payments
    /// • PaymentNotCompleted: Payments must be completed/captured to finalize
    /// 
    /// PROMOTION ERRORS:
    /// • PromotionAlreadyApplied: Cannot apply multiple different promotions
    /// • PromotionRequired: Promotion reference is null or invalid
    /// 
    /// BUSINESS RULE ERRORS:
    /// • NotFound: Order with specified ID does not exist
    /// • EmptyCart: Cannot checkout empty cart (no items)
    /// </remarks>
    public static class Errors
    {
        /// <summary>Triggered when attempting an invalid order state transition.</summary>
        public static Error InvalidStateTransition(OrderState from, OrderState to) => Error.Validation(code: "Order.InvalidStateTransition", description: $"Cannot transition from {from} to {to}.");
        
        /// <summary>Triggered when order with specified ID is not found in database.</summary>
        public static Error NotFound(Guid id) => Error.NotFound(code: "Order.NotFound", description: $"Order with ID '{id}' was not found.");
        
        /// <summary>Triggered when attempting to cancel an order that is already complete.</summary>
        public static Error CannotCancelCompleted => Error.Validation(code: "Order.CannotCancelCompleted", description: "Cannot cancel completed order.");
        
        /// <summary>Triggered when attempting to modify a line item that does not exist in the order.</summary>
        public static Error LineItemNotFound => Error.NotFound(code: "Order.LineItemNotFound", description: "Line item not found.");
        
        /// <summary>Triggered when attempting to apply a second different promotion to an order.</summary>
        public static Error PromotionAlreadyApplied => Error.Conflict(code: "Order.PromotionAlreadyApplied", description: "Promotion already applied to order.");

        /// <summary>Triggered when line item quantity is less than minimum allowed (< 1).</summary>
        public static Error InvalidQuantity => CommonInput.Errors.TooFewItems(field: nameof(LineItem.Quantity), min: Constraints.QuantityMinValue);
        
        /// <summary>Triggered when required address is missing for physical order.</summary>
        public static Error AddressRequired => CommonInput.Errors.Required(field: "Address");
        
        /// <summary>Triggered when shipping method not selected before transitioning to Payment state.</summary>
        public static Error ShippingMethodRequired => CommonInput.Errors.Required(field: "Shipping method");
        
        /// <summary>Triggered when payment amount is negative or less than zero.</summary>
        public static Error InvalidAmountCents => CommonInput.Errors.TooFewItems(field: "Amount cents", min: Constraints.AmountCentsMinValue);
        
        /// <summary>Triggered when promotion reference is null.</summary>
        public static Error PromotionRequired => CommonInput.Errors.Required(field: "Promotion");
    }
    #endregion

    #region Properties
    /// <summary>
    /// Foreign key reference to the Store where this order was placed.
    /// Used for multi-store/multi-channel isolation and store-specific configuration.
    /// </summary>
    public Guid StoreId { get; set; }
    
    /// <summary>
    /// Foreign key reference to the ApplicationUser who placed this order (nullable for guest orders).
    /// Allows tracking order history and customer identification.
    /// </summary>
    public string? UserId { get; set; }
    
    /// <summary>
    /// Foreign key reference to the Promotion applied to this order (nullable if no promotion).
    /// Only one promotion allowed per order; replaces previous promotion if reapplied.
    /// </summary>
    public Guid? PromotionId { get; set; }
    
    /// <summary>
    /// Foreign key reference to the ShippingMethod selected for this order (nullable for digital orders).
    /// Used to calculate ShipmentTotalCents based on weight and order value.
    /// </summary>
    public Guid? ShippingMethodId { get; set; }
    
    /// <summary>
    /// Foreign key reference to the warehouse (StockLocation) fulfilling this order.
    /// Assigned during Delivery state; required before transitioning to Payment for physical orders.
    /// Enables multi-location fulfillment and inventory tracking.
    /// </summary>
    public Guid? FulfillmentLocationId { get; set; }

    /// <summary>
    /// Unique order number generated at creation (format: R{yyyyMMdd}{random}).
    /// Used as human-readable order identifier for customer communication.
    /// </summary>
    public string Number { get; set; } = string.Empty;
    
    /// <summary>
    /// Current state of the order in its lifecycle (Cart, Address, Delivery, Payment, Confirm, Complete, or Canceled).
    /// Transitions enforced by Next() method; represents where order is in fulfillment process.
    /// </summary>
    public OrderState State { get; set; } = OrderState.Cart;
    
    /// <summary>
    /// Sum of all line item subtotals in cents (quantity × unit price for each item).
    /// This is the merchandise total before shipping, taxes, or promotional adjustments.
    /// Recalculated after: AddLineItem, RemoveLineItem, UpdateLineItemQuantity, or price changes.
    /// </summary>
    public long ItemTotalCents { get; set; } // Sum of base-line item subtotals (quantity * unit price)
    
    /// <summary>
    /// Total shipping cost in cents calculated by the selected ShippingMethod.
    /// Recalculated when: ShippingMethod changes or order properties affecting calculation change.
    /// Zero for digital orders (no shipping).
    /// </summary>
    public long ShipmentTotalCents { get; set; }
    
    /// <summary>
    /// Grand total of the order in cents: ItemTotalCents + ShipmentTotalCents + AdjustmentTotalCents.
    /// This is the final amount customer must pay.
    /// Recalculated whenever any component changes.
    /// </summary>
    public long TotalCents { get; set; }
    
    /// <summary>
    /// Sum of all adjustments in cents (positive for taxes/fees, negative for discounts).
    /// Includes both order-level adjustments (OrderAdjustment) and line-item adjustments (LineItemAdjustment).
    /// Recalculated after applying/removing promotions or any adjustment changes.
    /// </summary>
    public long AdjustmentTotalCents { get; set; }
    
    /// <summary>
    /// ISO 4217 currency code (e.g., "USD", "EUR", "GBP").
    /// Set at order creation; all monetary values in this order use this currency.
    /// Line items capture prices in this currency at creation time.
    /// </summary>
    public string Currency { get; set; } = string.Empty;
    
    /// <summary>
    /// Customer's email address (nullable for orders where email will be collected later).
    /// Used for order confirmations, shipping updates, and customer identification.
    /// </summary>
    public string? Email { get; set; }
    
    /// <summary>
    /// Special delivery instructions provided by customer (e.g., "Leave at side door", "Ring doorbell twice").
    /// Informational field that helps fulfill orders according to customer preferences.
    /// </summary>
    public string? SpecialInstructions { get; set; }
    
    /// <summary>
    /// Promotional code provided by customer (e.g., "SUMMER20", "WELCOME10").
    /// Stored as uppercase; used to validate promotion eligibility.
    /// Nullable if no promotion applied or if promotion doesn't require code.
    /// </summary>
    public string? PromoCode { get; set; }
    
    /// <summary>
    /// Timestamp when order reached Complete state (nullable until completion).
    /// Marks the point at which order was finalized and inventory was reduced.
    /// </summary>
    public DateTimeOffset? CompletedAt { get; set; }
    
    /// <summary>
    /// Timestamp when order was canceled (nullable until cancellation).
    /// Marks the point at which inventory was released back to available stock.
    /// </summary>
    public DateTimeOffset? CanceledAt { get; set; }
    
    /// <summary>
    /// Public metadata (key-value dictionary) for storing customer-facing custom data.
    /// Example: gift message, special packaging preference, delivery time window.
    /// </summary>
    public IDictionary<string, object?>? PublicMetadata { get; set; } = new Dictionary<string, object?>();
    
    /// <summary>
    /// Private metadata (key-value dictionary) for storing system/internal custom data.
    /// Example: affiliate tracking, internal notes, custom routing rules.
    /// Not exposed to customers or external systems.
    /// </summary>
    public IDictionary<string, object?>? PrivateMetadata { get; set; } = new Dictionary<string, object?>();
    #endregion

    #region Relationships
    public UserAddress? ShipAddress { get; set; }
    public UserAddress? BillAddress { get; set; }
    public Store Store { get; set; } = null!;
    public ApplicationUser? User { get; set; }
    public Promotion? Promotion { get; set; }
    public StockLocation? FulfillmentLocation { get; set; }
    public ICollection<LineItem> LineItems { get; set; } = new List<LineItem>();
    public ICollection<OrderAdjustment> Adjustments { get; set; } = new List<OrderAdjustment>();
    public ICollection<Shipment> Shipments { get; set; } = new List<Shipment>();
    public ICollection<Payment> Payments { get; set; } = new List<Payment>();
    #endregion

    #region Computed Properties
    /// <summary>
    /// Indicates whether order is in Cart state (items can still be added/removed).
    /// </summary>
    public bool IsCart => State == OrderState.Cart;
    
    /// <summary>
    /// Indicates whether order is in Complete state (terminal state, inventory finalized).
    /// </summary>
    public bool IsComplete => State == OrderState.Complete;
    
    /// <summary>
    /// Indicates whether order is in Canceled state (terminal state, inventory released).
    /// </summary>
    public bool IsCanceled => State == OrderState.Canceled;
    
    /// <summary>
    /// Total number of units in order (sum of all line item quantities).
    /// Useful for weight calculations and volume assessments.
    /// </summary>
    public int ItemCount => LineItems.Sum(selector: li => li.Quantity);
    
    /// <summary>
    /// Grand total converted to decimal currency value (TotalCents ÷ 100).
    /// </summary>
    public decimal Total => TotalCents / 100m;
    
    /// <summary>
    /// Item total converted to decimal currency value (ItemTotalCents ÷ 100).
    /// </summary>
    public virtual decimal ItemTotal => ItemTotalCents / 100m;
    
    /// <summary>
    /// Shipment total converted to decimal currency value (ShipmentTotalCents ÷ 100).
    /// </summary>
    public virtual decimal ShipmentTotal => ShipmentTotalCents / 100m;
    
    /// <summary>
    /// Total weight of all line items in kilograms (sum of variant weight × quantity).
    /// Used for shipping cost calculation; variants should have weight > 0 for accuracy.
    /// Returns 0 if variants have no weight specified.
    /// </summary>
    public decimal TotalWeight => LineItems.Sum(selector: li => (li.Variant.Weight ?? 0) * li.Quantity);
    
    /// <summary>
    /// Sum of all promotion-related adjustments in cents (order-level + line-item level).
    /// Negative value indicates discount/reduction; used to display promotion savings.
    /// </summary>
    public long PromotionTotalCents => Adjustments.Where(predicate: a => a.IsPromotion).Sum(selector: a => a.AmountCents) + LineItems.SelectMany(selector: li => li.Adjustments).Where(predicate: a => a.IsPromotion).Sum(selector: a => a.AmountCents);
    
    /// <summary>
    /// Promotion total converted to decimal currency value (PromotionTotalCents ÷ 100).
    /// Displayed to customer as discount savings.
    /// </summary>
    public decimal PromotionTotal => PromotionTotalCents / 100m;
    
    /// <summary>
    /// Indicates whether a promotion is currently applied to this order.
    /// </summary>
    public bool HasPromotion => PromotionId.HasValue;
    
    /// <summary>
    /// Indicates whether all line items in the order are digital products.
    /// If true, order skips address validation and shipping requirements during state transitions.
    /// Used to route digital orders through faster/simpler fulfillment path.
    /// </summary>
    public bool IsFullyDigital => LineItems.Any() && LineItems.All(predicate: li => li.Variant.Product.IsDigital);
    #endregion

    #region Constructors
    /// <summary>Private constructor prevents direct instantiation; use Create() factory method instead.</summary>
    private Order() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new order in Cart state ready for items to be added.
    /// </summary>
    /// <param name="storeId">The store where order is being placed (required).</param>
    /// <param name="currency">ISO 4217 currency code for all monetary values in this order (e.g., "USD").</param>
    /// <param name="userId">Optional reference to the user placing the order (null for guest orders).</param>
    /// <param name="email">Optional email address for order notifications.</param>
    /// <returns>
    /// ErrorOr result containing the created Order or validation error.
    /// Always succeeds if store ID is valid and currency is provided.
    /// </returns>
    /// <remarks>
    /// The order is initialized in Cart state with:
    /// • Unique order number generated (R{yyyyMMdd}{random})
    /// • Empty line items, adjustments, shipments, and payments
    /// • All monetary totals set to 0
    /// • CreatedAt timestamp set to current UTC time
    /// 
    /// A domain event (Created) is published to notify the system of new order.
    /// 
    /// Example:
    /// <code>
    /// var result = Order.Create(
    ///     storeId: store.Id,
    ///     currency: "USD",
    ///     userId: user?.Id,
    ///     email: "customer@example.com");
    /// 
    /// if (result.IsError) return BadRequest(result.FirstError);
    /// var order = result.Value;
    /// </code>
    /// </remarks>
    public static ErrorOr<Order> Create(Guid storeId, string currency, string? userId = null, string? email = null)
    {
        var order = new Order
        {
            Id = Guid.NewGuid(),
            StoreId = storeId,
            UserId = userId,
            Email = email?.Trim(),
            Number = GenerateOrderNumber(),
            State = OrderState.Cart,
            Currency = currency,
            CreatedAt = DateTimeOffset.UtcNow
        };

        order.AddDomainEvent(domainEvent: new Events.Created(OrderId: order.Id, StoreId: order.StoreId));
        return order;
    }
    #endregion

    #region Business Logic - State Transitions
    /// <summary>
    /// Progresses the order to the next state in its lifecycle.
    /// </summary>
    /// <returns>
    /// ErrorOr result containing the order (if successful) or validation error describing why transition failed.
    /// Returns specific error if prerequisites for next state are not met.
    /// </returns>
    /// <remarks>
    /// This method implements the state machine progression:
    /// • Cart → Address: Requires at least one line item
    /// • Address → Delivery: Requires both addresses for physical orders
    /// • Delivery → Payment: Requires shipping method; creates shipment
    /// • Payment → Confirm: Requires sufficient payment coverage
    /// • Confirm → Complete: Finalizes order and triggers inventory reduction
    /// 
    /// The method validates prerequisites before allowing transition and publishes
    /// a StateChanged domain event to notify other aggregates.
    /// </remarks>
    public ErrorOr<Order> Next()
    {
        return State switch
        {
            OrderState.Cart => ToAddress(),
            OrderState.Address => ToDelivery(),
            OrderState.Delivery => ToPayment(),
            OrderState.Payment => ToConfirm(),
            OrderState.Confirm => Complete(),
            _ => Errors.InvalidStateTransition(from: State, to: State + 1)
        };
    }

    /// <summary>Transitions order from Cart to Address state after validating prerequisites.</summary>
    private ErrorOr<Order> ToAddress()
    {
        if (!LineItems.Any()) return Error.Validation(code: "Order.EmptyCart", description: "Cannot checkout empty cart.");
        State = OrderState.Address;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.StateChanged(OrderId: Id, NewState: OrderState.Address));
        return this;
    }

    /// <summary>
    /// Transitions order from Address to Delivery state.
    /// For physical orders, both shipping and billing addresses are required.
    /// Digital orders skip this validation.
    /// </summary>
    private ErrorOr<Order> ToDelivery()
    {
        if (!IsFullyDigital && (ShipAddress == null || BillAddress == null))
            return Errors.AddressRequired;
        State = OrderState.Delivery;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.StateChanged(OrderId: Id, NewState: OrderState.Delivery));
        return this;
    }

    /// <summary>
    /// Transitions order from Delivery to Payment state.
    /// For physical orders, creates a Shipment record and requires shipping method.
    /// Digital orders skip shipment creation.
    /// </summary>
    private ErrorOr<Order> ToPayment()
    {
        if (!IsFullyDigital)
        {
            if (!ShippingMethodId.HasValue)
                return Error.Validation(code: "Order.ShippingMethodRequired",
                    description: "Shipping method must be set before payment.");

            var shipmentResult = Shipment.Create(orderId: Id, shippingMethodId: ShippingMethodId.Value, stockLocationId: FulfillmentLocationId);
            if (shipmentResult.IsError) return shipmentResult.FirstError;
            Shipments.Add(item: shipmentResult.Value);
        }

        State = OrderState.Payment;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.StateChanged(OrderId: Id, NewState: OrderState.Payment));
        return this;
    }

    /// <summary>
    /// Transitions order from Payment to Confirm state.
    /// Requires that total payments received meet or exceed the order total.
    /// </summary>
    private ErrorOr<Order> ToConfirm()
    {
        if (TotalCents > Payments.Sum(selector: p => p.AmountCents))
            return Error.Validation(code: "Order.PaymentRequired", description: "Payment is required to confirm order.");

        State = OrderState.Confirm;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.StateChanged(OrderId: Id, NewState: OrderState.Confirm));
        return this;
    }

    /// <summary>
    /// Completes the order, triggering inventory finalization.
    /// </summary>
    /// <remarks>
    /// Transitions order from Confirm to Complete state (terminal state).
    /// 
    /// Prerequisites:
    /// • All payments must be completed/captured (not just pending)
    /// • Total completed payments must meet or exceed order total
    /// 
    /// Side effects:
    /// • Sets CompletedAt timestamp
    /// • Publishes Completed event (triggers notifications)
    /// • Publishes FinalizeInventory event (reduces stock)
    /// • If promotion applied, publishes Promotion.Used event
    /// 
    /// This is the final state; order cannot be modified after completion.
    /// Cancellation is no longer allowed after this point.
    /// </remarks>
    private ErrorOr<Order> Complete()
    {
        var completedPayments = Payments.Where(predicate: p => p.IsCompleted).Sum(selector: p => p.AmountCents);
        if (completedPayments < TotalCents)
        {
            return Error.Validation(code: "Order.PaymentNotCompleted", description: "Payment must be completed before completing the order.");
        }

        CompletedAt = DateTimeOffset.UtcNow;
        State = OrderState.Complete;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.Completed(OrderId: Id, StoreId: StoreId));
        AddDomainEvent(domainEvent: new Events.FinalizeInventory(OrderId: Id, StoreId: StoreId));
        if (HasPromotion && PromotionId.HasValue)
        {
            AddDomainEvent(domainEvent: new Promotion.Events.Used(PromotionId: PromotionId!.Value, OrderId: Id));
        }
        return this;
    }

    /// <summary>
    /// Cancels the order and releases reserved inventory.
    /// </summary>
    /// <remarks>
    /// Transitions order to Canceled state (terminal state).
    /// 
    /// Restrictions:
    /// • Cannot cancel completed orders (they're already finalized)
    /// • Can be called multiple times (idempotent); second call is no-op
    /// 
    /// Side effects:
    /// • Sets CanceledAt timestamp
    /// • Publishes Canceled event (triggers notifications)
    /// • Publishes ReleaseInventory event (restores stock availability)
    /// 
    /// After cancellation, order cannot be modified or progressed further.
    /// </remarks>
    public ErrorOr<Order> Cancel()
    {
        if (State == OrderState.Complete) return Errors.CannotCancelCompleted;
        if (State == OrderState.Canceled) return this;
        CanceledAt = DateTimeOffset.UtcNow;
        State = OrderState.Canceled;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.Canceled(OrderId: Id, StoreId: StoreId));
        AddDomainEvent(domainEvent: new Events.ReleaseInventory(OrderId: Id, StoreId: StoreId));
        return this;
    }
    #endregion

    #region Business Logic - Fulfillment Management
    /// <summary>
    /// Assigns a warehouse (stock location) where this order will be fulfilled from.
    /// Should be called during Delivery state before transitioning to Payment.
    /// </summary>
    /// <param name="location">The warehouse/stock location to fulfill from (null will return error).</param>
    /// <returns>
    /// ErrorOr result containing the order (if successful) or validation error.
    /// Fails if location is null, digital order, or order is not in Pending state.
    /// </returns>
    /// <remarks>
    /// This assignment coordinates between multiple domains:
    /// • Order domain: Tracks fulfillment location
    /// • Inventories domain: Uses this location for stock allocation
    /// • Shipping domain: Uses this location for shipping calculations
    /// 
    /// Digital orders cannot have fulfillment locations (no physical inventory needed).
    /// Physical orders must have location assigned before moving to Payment state.
    /// </remarks>
    public ErrorOr<Order> SetFulfillmentLocation(StockLocation? location)
    {
        if (location == null)
            return Error.Validation(code: "Order.FulfillmentLocationRequired",
                description: "Fulfillment location is required.");

        if (IsFullyDigital)
            return Error.Validation(code: "Order.DigitalOrderNoFulfillment",
                description: "Digital orders do not require a fulfillment location.");

        FulfillmentLocationId = location.Id;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.FulfillmentLocationSelected(
            OrderId: Id,
            StoreId: StoreId,
            StockLocationId: location.Id));

        return this;
    }
    #endregion

    #region Business Logic - Line Item Management
    /// <summary>
    /// Adds a product variant to the order with specified quantity.
    /// </summary>
    /// <remarks>
    /// If the variant is already in the order, updates the quantity instead of creating a duplicate.
    /// Prices are captured at addition time (snapshot) in the order's currency.
    /// 
    /// Restrictions:
    /// • Only purchasable variants can be added
    /// • Quantity must be ≥ 1
    /// • Only available in Cart state (though code doesn't enforce)
    /// 
    /// Side effects:
    /// • Recalculates all totals
    /// • Publishes LineItemAdded event to coordinate with inventory
    /// </remarks>
    public ErrorOr<Order> AddLineItem(Variant? variant, int quantity)
    {
        if (variant == null) return Error.Validation(code: "Order.VariantRequired", description: "Variant cannot be null.");
        if (quantity < Constraints.QuantityMinValue) return Errors.InvalidQuantity;
        if (!variant.Purchasable) return Error.Validation(code: "Order.VariantNotPurchasable", description: "Variant is not available for purchase.");

        var existing = LineItems.FirstOrDefault(predicate: li => li.VariantId == variant.Id);
        if (existing != null)
        {
            var updateResult = existing.UpdateQuantity(quantity: existing.Quantity + quantity);
            if (updateResult.IsError) return updateResult.FirstError;
        }
        else
        {
            var lineItemResult = LineItem.Create(orderId: Id, variant: variant, quantity: quantity, currency: Currency);
            if (lineItemResult.IsError) return lineItemResult.FirstError;
            LineItems.Add(item: lineItemResult.Value);
        }

        RecalculateTotals();
        AddDomainEvent(domainEvent: new Events.LineItemAdded(OrderId: Id, VariantId: variant.Id, Quantity: quantity));
        return this;
    }

    /// <summary>
    /// Removes a line item from the order by ID.
    /// </summary>
    /// <remarks>
    /// Fails silently if line item does not exist (returns error).
    /// Recalculates totals after removal.
    /// Publishes LineItemRemoved event.
    /// </remarks>
    public ErrorOr<Order> RemoveLineItem(Guid lineItemId)
    {
        var lineItem = LineItems.FirstOrDefault(predicate: li => li.Id == lineItemId);
        if (lineItem == null) return Errors.LineItemNotFound;
        LineItems.Remove(item: lineItem);
        RecalculateTotals();
        AddDomainEvent(domainEvent: new Events.LineItemRemoved(OrderId: Id, LineItemId: lineItemId));
        return this;
    }

    /// <summary>
    /// Updates the quantity of an existing line item.
    /// </summary>
    /// <remarks>
    /// Quantity must be ≥ 1. Set to 0 to remove the item instead.
    /// Recalculates totals after update.
    /// </remarks>
    public ErrorOr<Order> UpdateLineItemQuantity(Guid lineItemId, int quantity)
    {
        if (quantity < Constraints.QuantityMinValue) return Errors.InvalidQuantity;

        var lineItem = LineItems.FirstOrDefault(predicate: li => li.Id == lineItemId);
        if (lineItem == null) return Errors.LineItemNotFound;
        var updateResult = lineItem.UpdateQuantity(quantity: quantity);
        if (updateResult.IsError) return updateResult.FirstError;
        RecalculateTotals();
        return this;
    }

    /// <summary>
    /// Recalculates all order totals: ItemTotal, AdjustmentTotal, and grand Total.
    /// </summary>
    /// <remarks>
    /// Called automatically after:
    /// • Adding/removing/updating line items
    /// • Applying/removing promotions
    /// • Setting shipping method
    /// • Any change affecting financial calculations
    /// 
    /// Calculation formula:
    /// ItemTotalCents = sum of (quantity × unit price) for each line item
    /// AdjustmentTotalCents = order adjustments + sum of line-item adjustments
    /// TotalCents = ItemTotalCents + ShipmentTotalCents + AdjustmentTotalCents
    /// 
    /// Always updates UpdatedAt timestamp.
    /// </remarks>
    public void RecalculateTotals()
    {
        ItemTotalCents = LineItems.Sum(selector: li => li.SubtotalCents);
        var orderAdjustmentsTotal = Adjustments.Sum(selector: a => a.AmountCents);
        var lineItemAdjustmentsTotal = LineItems.Sum(selector: li => li.Adjustments.Sum(selector: a => a.AmountCents));

        AdjustmentTotalCents = orderAdjustmentsTotal + lineItemAdjustmentsTotal; // Sum of all adjustments
        TotalCents = ItemTotalCents + ShipmentTotalCents + AdjustmentTotalCents;
        UpdatedAt = DateTimeOffset.UtcNow;
    }
    #endregion

    #region Business Logic - Address Management
    /// <summary>
    /// Sets the shipping address for the order.
    /// </summary>
    /// <remarks>
    /// Required for physical orders before transitioning to Delivery state.
    /// Digital orders cannot have shipping addresses (returns error if attempted).
    /// 
    /// Updates UpdatedAt timestamp and publishes ShippingAddressSet event.
    /// </remarks>
    public ErrorOr<Order> SetShippingAddress(UserAddress? address)
    {
        if (address == null) return Errors.AddressRequired;

        if (IsFullyDigital)
            return Error.Validation(code: "Order.DigitalOrderNoShipping",
                description: "Digital orders do not require shipping address.");

        ShipAddress = address;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.ShippingAddressSet(OrderId: Id));
        return this;
    }

    /// <summary>
    /// Sets the billing address for the order.
    /// </summary>
    /// <remarks>
    /// Required for all orders (including digital) to process billing information.
    /// Updates UpdatedAt timestamp and publishes BillingAddressSet event.
    /// </remarks>
    public ErrorOr<Order> SetBillingAddress(UserAddress? address)
    {
        if (address == null) return Errors.AddressRequired;
        BillAddress = address;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.BillingAddressSet(OrderId: Id));
        return this;
    }
    #endregion

    #region Business Logic - Promotion Management
    /// <summary>
    /// Applies a promotion to the order with optional coupon code validation.
    /// </summary>
    /// <remarks>
    /// Only one promotion per order. Applying a new promotion replaces the previous one.
    /// 
    /// Process:
    /// 1. Validates promotion is not null
    /// 2. Checks coupon code if promotion requires it
    /// 3. Calculates discount adjustments using PromotionCalculator
    /// 4. Clears previous promotion adjustments
    /// 5. Applies new adjustments at order or line-item level
    /// 6. Recalculates totals
    /// 7. Publishes PromotionApplied event
    /// 
    /// Side effects:
    /// • Replaces PromotionId, PromoCode
    /// • Clears all previous promotion-related adjustments
    /// • Non-promotion adjustments (taxes, fees) preserved
    /// • Triggers total recalculation
    /// </remarks>
    public ErrorOr<Order> ApplyPromotion(Promotion? promotion, string? code = null)
    {
        if (promotion == null) return Errors.PromotionRequired;
        if (PromotionId.HasValue && PromotionId != promotion.Id) return Errors.PromotionAlreadyApplied;
        if (promotion.RequiresCouponCode && promotion.PromotionCode?.Trim().ToUpperInvariant() != code?.Trim().ToUpperInvariant())
            return Promotion.Errors.InvalidCode;

        var calcResult = PromotionCalculator.Calculate(promotion: promotion, order: this);
        if (calcResult.IsError) return calcResult.FirstError;

        Adjustments.Clear();
        foreach (var li in LineItems) li.Adjustments.Clear();

        PromotionId = promotion.Id;
        PromoCode = code?.Trim().ToUpperInvariant();

        foreach (var adj in calcResult.Value.Adjustments)
        {
            if (adj.LineItemId.HasValue)
            {
                var lineItem = LineItems.FirstOrDefault(predicate: li => li.Id == adj.LineItemId.Value);
                if (lineItem != null)
                {
                    var liAdjustment = LineItemAdjustment.Create(lineItemId: lineItem.Id, amountCents: adj.Amount, description: adj.Description, promotionId: promotion.Id).Value;
                    lineItem.Adjustments.Add(item: liAdjustment);
                }
            }
            else
            {
                var orderAdjustment = OrderAdjustment.Create(orderId: Id, amountCents: adj.Amount, description: adj.Description, promotionId: promotion.Id).Value;
                Adjustments.Add(item: orderAdjustment);
            }
        }

        RecalculateTotals();

        AddDomainEvent(domainEvent: new Events.PromotionApplied(OrderId: Id, PromotionId: promotion.Id, DiscountAmount: PromotionTotal));

        return this;
    }

    public ErrorOr<Order> RemovePromotion()
    {
        if (!PromotionId.HasValue) return this;
        var oldPromotionId = PromotionId.Value;
        PromotionId = null;
        PromoCode = null;

        // Clear all promotion-related adjustments
        var nonPromotionAdjustments = Adjustments.Where(predicate: a => !a.IsPromotion).ToList();
        Adjustments.Clear();
        foreach (var adj in nonPromotionAdjustments)
        {
            Adjustments.Add(item: adj);
        }

        foreach (var li in LineItems)
        {
            var nonPromotionLineAdjustments = li.Adjustments.Where(predicate: a => !a.IsPromotion).ToList();
            li.Adjustments.Clear();
            foreach (var adj in nonPromotionLineAdjustments)
            {
                li.Adjustments.Add(item: adj);
            }
        }

        RecalculateTotals();
        AddDomainEvent(domainEvent: new Events.PromotionRemoved(OrderId: Id, PromotionId: oldPromotionId));
        return this;
    }
    #endregion

    #region Business Logic - Shipping Management
    /// <summary>
    /// Selects a shipping method for the order and calculates shipping cost.
    /// </summary>
    /// <remarks>
    /// Physical orders require shipping method selection before Payment state.
    /// Digital orders cannot have shipping method (returns error if attempted).
    /// 
    /// Process:
    /// 1. Validates shipping method not null
    /// 2. Checks order is not fully digital
    /// 3. Calculates cost based on order weight and value via ShippingMethod.CalculateCost()
    /// 4. Stores cost as ShipmentTotalCents (converted to cents)
    /// 5. Recalculates grand total
    /// 6. Publishes ShippingMethodSelected event
    /// 
    /// Called during Delivery state to prepare for Payment state transition.
    /// </remarks>
    public ErrorOr<Order> SetShippingMethod(ShippingMethod? shippingMethod)
    {
        if (shippingMethod == null) return Errors.ShippingMethodRequired;

        if (IsFullyDigital)
            return Error.Validation(code: "Order.DigitalOrderNoShipping",
                description: "Digital orders do not require shipping method.");

        ShippingMethodId = shippingMethod.Id;
        ShipmentTotalCents = (long)(shippingMethod.CalculateCost(
            orderWeight: TotalWeight,
            orderTotal: ItemTotal) * 100);
        RecalculateTotals();
        AddDomainEvent(domainEvent: new Events.ShippingMethodSelected(
            OrderId: Id,
            ShippingMethodId: shippingMethod.Id));
        return this;
    }
    #endregion

    #region Business Logic - Payment Management
    /// <summary>
    /// Records a payment/charge against the order.
    /// </summary>
    /// <remarks>
    /// Creates a new Payment record in Pending state.
    /// Multiple payments can be accumulated to cover the order total.
    /// 
    /// Parameters:
    /// • amountCents: Amount in cents (must be ≥ 0)
    /// • paymentMethodId: Reference to payment method configuration
    /// • paymentMethodType: Type identifier (e.g., "CreditCard", "PayPal", "ApplePay")
    /// 
    /// Returns the created Payment aggregate so caller can transition it (e.g., Capture).
    /// 
    /// Used during Payment state; later payment completion checked during Confirm transition.
    /// </remarks>
    public ErrorOr<Payment> AddPayment(long amountCents, Guid paymentMethodId, string paymentMethodType)
    {
        if (amountCents < Constraints.AmountCentsMinValue) return Errors.InvalidAmountCents;

        var paymentResult = Payment.Create(orderId: Id, amountCents: amountCents, currency: Currency, paymentMethodType: paymentMethodType, paymentMethodId: paymentMethodId);
        if (paymentResult.IsError) return paymentResult.FirstError;

        Payments.Add(item: paymentResult.Value);
        return paymentResult.Value;
    }
    #endregion

    #region Helpers
    /// <summary>
    /// Generates a unique, human-readable order number.
    /// Format: R{yyyyMMdd}{4-digit random}
    /// Example: R202312101234
    /// </summary>
    private static string GenerateOrderNumber() => $"R{DateTimeOffset.UtcNow:yyyyMMdd}{Random.Shared.Next(minValue: 1000, maxValue: 9999)}";
    #endregion

    #region Events
    /// <summary>
    /// Domain events published by Order aggregate to communicate state changes.
    /// </summary>
    /// <remarks>
    /// Events enable decoupled communication with:
    /// • Inventory system: FinalizeInventory, ReleaseInventory, LineItemAdded, LineItemRemoved
    /// • Notification system: Created, Completed, Canceled (for emails, SMS)
    /// • Analytics: StateChanged, Completed, Canceled
    /// • Promotions: PromotionApplied, PromotionRemoved, PromotionUsed
    /// • Fulfillment: FulfillmentLocationSelected, ShippingMethodSelected
    /// • Address events: ShippingAddressSet, BillingAddressSet
    /// 
    /// Events are automatically published by EF Core interceptor after SaveChangesAsync.
    /// </remarks>
    public static class Events
    {
        /// <summary>Published when new order is created.</summary>
        public sealed record Created(Guid OrderId, Guid StoreId) : DomainEvent;
        
        /// <summary>Published when order transitions between states.</summary>
        public sealed record StateChanged(Guid OrderId, OrderState NewState) : DomainEvent;
        
        /// <summary>Published when order reaches Complete state (terminal).</summary>
        public sealed record Completed(Guid OrderId, Guid StoreId) : DomainEvent;
        
        /// <summary>Published when order is canceled (terminal).</summary>
        public sealed record Canceled(Guid OrderId, Guid StoreId) : DomainEvent;
        
        /// <summary>Published when product variant is added to order.</summary>
        public sealed record LineItemAdded(Guid OrderId, Guid VariantId, int Quantity) : DomainEvent;
        
        /// <summary>Published when line item is removed from order.</summary>
        public sealed record LineItemRemoved(Guid OrderId, Guid LineItemId) : DomainEvent;
        
        /// <summary>Published when order completes; signals inventory to reduce stock.</summary>
        public sealed record FinalizeInventory(Guid OrderId, Guid StoreId) : DomainEvent;
        
        /// <summary>Published when order is canceled; signals inventory to release reserved stock.</summary>
        public sealed record ReleaseInventory(Guid OrderId, Guid StoreId) : DomainEvent;
        
        /// <summary>Published when fulfillment warehouse location is assigned.</summary>
        public sealed record FulfillmentLocationSelected(Guid OrderId, Guid StoreId, Guid StockLocationId) : DomainEvent;
        
        /// <summary>Published when promotion is applied with calculated discount.</summary>
        public sealed record PromotionApplied(Guid OrderId, Guid PromotionId, decimal DiscountAmount) : DomainEvent;
        
        /// <summary>Published when promotion is removed from order.</summary>
        public sealed record PromotionRemoved(Guid OrderId, Guid PromotionId) : DomainEvent;
        
        /// <summary>Published when order completes with active promotion; signals promotion usage tracking.</summary>
        public sealed record PromotionUsed(Guid OrderId, Guid PromotionId) : DomainEvent;
        
        /// <summary>Published when shipping method is selected.</summary>
        public sealed record ShippingMethodSelected(Guid OrderId, Guid ShippingMethodId) : DomainEvent;
        
        /// <summary>Published when shipping address is set.</summary>
        public sealed record ShippingAddressSet(Guid OrderId) : DomainEvent;
        
        /// <summary>Published when billing address is set.</summary>
        public sealed record BillingAddressSet(Guid OrderId) : DomainEvent;
    }
    #endregion
}