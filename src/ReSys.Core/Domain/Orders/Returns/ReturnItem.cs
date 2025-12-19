//using ReSys.Core.Common.Domain.Entities;
//using ReSys.Core.Common.Domain.Events;
//using ReSys.Core.Domain.Catalog.Products.Variants;
//using ReSys.Core.Domain.Orders.LineItems;
//using ReSys.Core.Domain.Orders.Shipments;

//namespace ReSys.Core.Domain.Orders.Returns;

///// <summary>
///// Represents a return request for an inventory unit.
///// Tracks the return lifecycle from initiation through reception, acceptance/rejection, and reimbursement.
///// </summary>
///// <remarks>
///// <para>
///// <b>CORE PRINCIPLE - One ReturnItem = One InventoryUnit:</b>
///// Following Solidus pattern, each ReturnItem represents exactly ONE inventory unit.
///// Since each InventoryUnit represents one physical item, each ReturnItem also represents one physical item.
///// For returning multiple units, create multiple ReturnItem instances.
///// </para>
///// 
///// <para>
///// <b>Reception State Machine:</b>
///// <list type="bullet">
///// <item><b>Awaiting:</b> Return initiated, waiting for customer to ship item back</item>
///// <item><b>Received:</b> Item received at warehouse, triggers acceptance evaluation</item>
///// <item><b>GivenToCustomer:</b> In-store/counter return (no shipping needed)</item>
///// <item><b>LostInTransit:</b> Item lost during return shipping</item>
///// <item><b>ShippedWrongItem:</b> Customer returned wrong item</item>
///// <item><b>ShortShipped:</b> Less items received than expected</item>
///// <item><b>InTransit:</b> Item is in transit back to warehouse</item>
///// <item><b>Cancelled:</b> Return cancelled before completion</item>
///// <item><b>Expired:</b> Return window expired</item>
///// <item><b>Unexchanged:</b> Exchange was returned instead of original</item>
///// </list>
///// </para>
///// 
///// <para>
///// <b>Acceptance State Machine:</b>
///// <list type="bullet">
///// <item><b>Pending:</b> Awaiting evaluation (initial state)</item>
///// <item><b>Accepted:</b> Return approved, eligible for refund/exchange</item>
///// <item><b>Rejected:</b> Return denied (wrong item, damaged, outside window)</item>
///// <item><b>ManualInterventionRequired:</b> Needs manual review by staff</item>
///// </list>
///// </para>
///// 
///// <para>
///// <b>Return Flow:</b>
///// 1. Customer initiates return → ReturnItem created (Awaiting/Pending)
///// 2. Item ships back → InTransit
///// 3. Warehouse receives → Received
///// 4. Automatic evaluation → AttemptAccept() (Accepted/Rejected/ManualIntervention)
///// 5. If accepted and exchange → Create exchange InventoryUnit
///// 6. If accepted and refund → Associate Reimbursement
///// 7. If resellable → Restore inventory
///// </para>
///// 
///// <para>
///// <b>Exchange vs Refund:</b>
///// • Exchange: ExchangeVariantId set, AmountCents = 0, new InventoryUnit created
///// • Refund: No ExchangeVariantId, AmountCents calculated, Reimbursement associated
///// • Cannot have both exchange and reimbursement
///// </para>
///// 
///// <para>
///// <b>Example Usage:</b>
///// <code>
///// // Create return for shipped unit
///// var returnResult = ReturnItem.Create(
/////     inventoryUnitId: unit.Id,
/////     returnAuthorizationId: rma.Id);
///// 
///// // Customer ships item back
///// returnItem.MarkInTransit();
///// 
///// // Warehouse receives
///// returnItem.Receive(); // Auto-evaluates acceptance
///// 
///// // If exchange requested
///// returnItem.SetExchangeVariant(newVariant);
///// 
///// // If refund requested
///// returnItem.SetRefundAmount(calculator.Calculate(returnItem));
///// returnItem.AssociateReimbursement(reimbursement.Id);
///// 
///// // Process inventory
///// returnItem.ProcessInventoryUnit(); // Marks unit as Returned
///// </code>
///// </para>
///// </remarks>
//public sealed class ReturnItem : Aggregate
//{
//    #region Enums

//    /// <summary>
//    /// Tracks the physical reception status of the returned item.
//    /// </summary>
//    public enum ReturnReceptionStatus
//    {
//        /// <summary>Return initiated, waiting for item to arrive.</summary>
//        Awaiting = 0,

//        /// <summary>Item physically received at warehouse.</summary>
//        Received = 1,

//        /// <summary>In-store/counter return (no shipping needed).</summary>
//        GivenToCustomer = 2,

//        /// <summary>Item lost during return shipping.</summary>
//        LostInTransit = 3,

//        /// <summary>Customer shipped wrong item back.</summary>
//        ShippedWrongItem = 4,

//        /// <summary>Fewer items received than expected.</summary>
//        ShortShipped = 5,

//        /// <summary>Item is in transit back to warehouse.</summary>
//        InTransit = 6,

//        /// <summary>Return cancelled before completion.</summary>
//        Cancelled = 7,

//        /// <summary>Return window expired.</summary>
//        Expired = 8,

//        /// <summary>Exchange item was returned instead of original.</summary>
//        Unexchanged = 9
//    }

//    /// <summary>
//    /// Tracks the acceptance/approval status of the return.
//    /// </summary>
//    public enum ReturnAcceptanceStatus
//    {
//        /// <summary>Awaiting evaluation (initial state).</summary>
//        Pending = 0,

//        /// <summary>Return approved, eligible for refund/exchange.</summary>
//        Accepted = 1,

//        /// <summary>Return denied.</summary>
//        Rejected = 2,

//        /// <summary>Requires manual staff review.</summary>
//        ManualInterventionRequired = 3
//    }

//    #endregion

//    #region Constants

//    /// <summary>Reception statuses that indicate partial completion (not final).</summary>
//    private static readonly ReturnReceptionStatus[] IntermediateReceptionStatuses =
//    {
//        ReturnReceptionStatus.GivenToCustomer,
//        ReturnReceptionStatus.LostInTransit,
//        ReturnReceptionStatus.ShippedWrongItem,
//        ReturnReceptionStatus.ShortShipped,
//        ReturnReceptionStatus.InTransit
//    };

//    /// <summary>Reception statuses that indicate final completion.</summary>
//    private static readonly ReturnReceptionStatus[] CompletedReceptionStatuses =
//    {
//        ReturnReceptionStatus.Received,
//        ReturnReceptionStatus.GivenToCustomer,
//        ReturnReceptionStatus.LostInTransit,
//        ReturnReceptionStatus.ShippedWrongItem,
//        ReturnReceptionStatus.ShortShipped,
//        ReturnReceptionStatus.InTransit
//    };

//    #endregion

//    #region Errors

//    public static class Errors
//    {
//        public static Error NotFound(Guid id) =>
//            Error.NotFound(
//                code: "ReturnItem.NotFound",
//                description: $"Return item with ID '{id}' was not found.");

//        public static Error InventoryUnitNotLoaded =>
//            Error.Validation(
//                code: "ReturnItem.InventoryUnitNotLoaded",
//                description: "Inventory unit must be loaded to evaluate return.");

//        public static Error InventoryUnitNotShipped =>
//            Error.Validation(
//                code: "ReturnItem.InventoryUnitNotShipped",
//                description: "Can only return inventory units that have been shipped.");

//        public static Error InventoryUnitAlreadyReturned =>
//            Error.Conflict(
//                code: "ReturnItem.InventoryUnitAlreadyReturned",
//                description: "This inventory unit has already been returned.");

//        public static Error ReturnWindowExpired(double daysSinceShipment) =>
//            Error.Validation(
//                code: "ReturnItem.ReturnWindowExpired",
//                description: $"Return window expired. {daysSinceShipment:F0} days since shipment.");

//        public static Error CannotCancelAfterReception =>
//            Error.Validation(
//                code: "ReturnItem.CannotCancelAfterReception",
//                description: "Cannot cancel return after it has been received.");

//        public static Error AcceptanceNotPending =>
//            Error.Validation(
//                code: "ReturnItem.AcceptanceNotPending",
//                description: "Can only accept/reject returns with pending or manual intervention status.");

//        public static Error ExchangeVariantRequired =>
//            Error.Validation(
//                code: "ReturnItem.ExchangeVariantRequired",
//                description: "Exchange variant is required.");

//        public static Error ExchangeVariantNotPurchasable =>
//            Error.Validation(
//                code: "ReturnItem.ExchangeVariantNotPurchasable",
//                description: "Exchange variant is not available for purchase.");

//        public static Error ExchangeAlreadyProcessed =>
//            Error.Validation(
//                code: "ReturnItem.ExchangeAlreadyProcessed",
//                description: "Exchange has already been processed.");

//        public static Error ExchangePriceMismatch(decimal original, decimal exchange) =>
//            Error.Validation(
//                code: "ReturnItem.ExchangePriceMismatch",
//                description: $"Exchange variant price differs too much from original. Original: {original:C}, Exchange: {exchange:C}");

//        public static Error ReimbursementNotAllowed =>
//            Error.Validation(
//                code: "ReturnItem.ReimbursementNotAllowed",
//                description: "Cannot associate reimbursement unless return is accepted.");

//        public static Error NotReceived =>
//            Error.Validation(
//                code: "ReturnItem.NotReceived",
//                description: "Return must be received before processing inventory.");

//        public static Error AcceptanceNotDecided =>
//            Error.Validation(
//                code: "ReturnItem.AcceptanceNotDecided",
//                description: "Return acceptance must be decided before processing inventory.");

//        public static Error ExchangeAndReimbursementConflict =>
//            Error.Validation(
//                code: "ReturnItem.ExchangeAndReimbursementConflict",
//                description: "Cannot have both exchange and reimbursement for a return item.");

//        public static Error AcceptedButNotReceived =>
//            Error.Validation(
//                code: "ReturnItem.AcceptedButNotReceived",
//                description: "Cannot accept return that hasn't been received.");
//    }

//    #endregion

//    #region Properties - Core Identity

//    /// <summary>
//    /// Foreign key to the inventory unit being returned.
//    /// CRITICAL: Each ReturnItem maps to exactly one InventoryUnit (1:1 for active returns).
//    /// </summary>
//    public Guid InventoryUnitId { get; set; }

//    /// <summary>
//    /// Foreign key to the return authorization (RMA) if required.
//    /// Optional - some businesses allow returns without pre-authorization.
//    /// </summary>
//    public Guid? ReturnAuthorizationId { get; set; }

//    /// <summary>
//    /// Foreign key to the customer return (group of returns shipped together).
//    /// Optional - allows grouping multiple returns into one shipment back.
//    /// </summary>
//    public Guid? CustomerReturnId { get; set; }

//    #endregion

//    #region Properties - Financial

//    /// <summary>
//    /// Foreign key to the reimbursement (refund) record.
//    /// Set when return is processed for refund (not exchange).
//    /// </summary>
//    public Guid? ReimbursementId { get; set; }

//    /// <summary>
//    /// Foreign key to preferred reimbursement type (store credit, original payment, etc.).
//    /// Customer's preference for how they want to be refunded.
//    /// </summary>
//    public Guid? PreferredReimbursementTypeId { get; set; }

//    /// <summary>
//    /// Foreign key to override reimbursement type.
//    /// Set by staff to override customer preference if needed.
//    /// </summary>
//    public Guid? OverrideReimbursementTypeId { get; set; }

//    /// <summary>
//    /// Pre-tax refund amount in cents.
//    /// Calculated by refund calculator based on original price.
//    /// Set to 0 for exchanges (no refund).
//    /// </summary>
//    public long AmountCents { get; set; }

//    /// <summary>
//    /// Restocking fee in cents to deduct from refund.
//    /// Applied based on quality check and business rules.
//    /// </summary>
//    public decimal RestockingFeeCents { get; set; }

//    #endregion

//    #region Properties - Exchange

//    /// <summary>
//    /// Foreign key to the variant customer wants as exchange.
//    /// If set, this is an exchange (not refund), AmountCents should be 0.
//    /// </summary>
//    public Guid? ExchangeVariantId { get; set; }

//    /// <summary>
//    /// Foreign key to the exchange inventory unit created for this return.
//    /// Set after exchange is processed and new unit shipped.
//    /// Enables tracking: original unit → return → exchange unit.
//    /// </summary>
//    public Guid? ExchangeInventoryUnitId { get; set; }

//    #endregion

//    #region Properties - State

//    /// <summary>
//    /// Current reception status (awaiting, received, in_transit, etc.).
//    /// Tracks physical location/status of returned item.
//    /// </summary>
//    public ReturnReceptionStatus ReceptionStatus { get; set; } = ReturnReceptionStatus.Awaiting;

//    /// <summary>
//    /// Current acceptance status (pending, accepted, rejected, manual).
//    /// Tracks approval/evaluation status of the return.
//    /// </summary>
//    public ReturnAcceptanceStatus AcceptanceStatus { get; set; } = ReturnAcceptanceStatus.Pending;

//    #endregion

//    #region Properties - Quality & Inventory

//    /// <summary>
//    /// Foreign key to return reason (defective, wrong size, changed mind, etc.).
//    /// Tracks why customer is returning the item.
//    /// </summary>
//    public Guid? ReturnReasonId { get; set; }

//    /// <summary>
//    /// Whether returned item can be restocked and resold.
//    /// True = restore to inventory, False = mark as damaged/unsellable.
//    /// </summary>
//    public bool Resellable { get; set; } = true;

//    /// <summary>
//    /// Whether returned item passed quality inspection.
//    /// Used to determine resellability and restocking fee.
//    /// </summary>
//    public bool PassedQualityCheck { get; set; }

//    /// <summary>
//    /// Notes about damage found during quality check.
//    /// Null if item is in perfect condition.
//    /// </summary>
//    public string? DamageAssessment { get; set; }

//    /// <summary>
//    /// Serialized validation errors from eligibility evaluation.
//    /// Populated when automatic acceptance fails validation.
//    /// Used for debugging and customer communication.
//    /// </summary>
//    public IDictionary<string, object?>? AcceptanceStatusErrors { get; set; }

//    #endregion

//    #region Relationships

//    /// <summary>The inventory unit being returned.</summary>
//    public InventoryUnit? InventoryUnit { get; set; }

//    /// <summary>The variant customer wants as exchange.</summary>
//    public Variant? ExchangeVariant { get; set; }

//    /// <summary>The new inventory unit created for exchange.</summary>
//    public InventoryUnit? ExchangeInventoryUnit { get; set; }

//    /// <summary>Return authorization (RMA) for this return.</summary>
//    public ReturnAuthorization? ReturnAuthorization { get; set; }

//    /// <summary>Customer return (grouped shipment) containing this return.</summary>
//    public CustomerReturn? CustomerReturn { get; set; }

//    /// <summary>Reimbursement (refund) for this return.</summary>
//    public Reimbursement? Reimbursement { get; set; }

//    /// <summary>Customer's preferred refund method.</summary>
//    public ReimbursementType? PreferredReimbursementType { get; set; }

//    /// <summary>Staff override for refund method.</summary>
//    public ReimbursementType? OverrideReimbursementType { get; set; }

//    /// <summary>Reason for return.</summary>
//    public ReturnReason? ReturnReason { get; set; }

//    #endregion

//    #region Computed Properties - Convenience Accessors

//    /// <summary>Gets the order this return belongs to (through inventory unit).</summary>
//    public Order? Order => InventoryUnit?.Order;

//    /// <summary>Gets the product variant that was returned.</summary>
//    public Variant? Variant => InventoryUnit?.Variant;

//    /// <summary>Gets the shipment the original item was in.</summary>
//    public Shipment? Shipment => InventoryUnit?.Shipment;

//    /// <summary>Gets the line item the returned unit fulfilled.</summary>
//    public LineItem? LineItem => InventoryUnit?.LineItem;

//    /// <summary>Gets the currency for monetary calculations.</summary>
//    public string Currency => ReturnAuthorization?.Currency ?? Order?.Currency ?? "USD";

//    #endregion

//    #region Computed Properties - State Queries

//    /// <summary>Has reception been completed (item received or terminal state)?</summary>
//    public bool HasCompletedReception => CompletedReceptionStatuses.Contains(ReceptionStatus);

//    /// <summary>Is return awaiting item to arrive?</summary>
//    public bool IsAwaiting => ReceptionStatus == ReturnReceptionStatus.Awaiting;

//    /// <summary>Has item been received at warehouse?</summary>
//    public bool IsReceived => ReceptionStatus == ReturnReceptionStatus.Received;

//    /// <summary>Is return cancelled?</summary>
//    public bool IsCancelled => ReceptionStatus == ReturnReceptionStatus.Cancelled;

//    /// <summary>Has return expired (past return window)?</summary>
//    public bool IsExpired => ReceptionStatus == ReturnReceptionStatus.Expired;

//    /// <summary>Is acceptance decided (accepted or rejected)?</summary>
//    public bool IsDecided =>
//        AcceptanceStatus == ReturnAcceptanceStatus.Accepted ||
//        AcceptanceStatus == ReturnAcceptanceStatus.Rejected;

//    /// <summary>Is acceptance pending decision?</summary>
//    public bool IsUndecided =>
//        AcceptanceStatus == ReturnAcceptanceStatus.Pending ||
//        AcceptanceStatus == ReturnAcceptanceStatus.ManualInterventionRequired;

//    /// <summary>Is return accepted?</summary>
//    public bool IsAccepted => AcceptanceStatus == ReturnAcceptanceStatus.Accepted;

//    /// <summary>Is return rejected?</summary>
//    public bool IsRejected => AcceptanceStatus == ReturnAcceptanceStatus.Rejected;

//    /// <summary>Has reimbursement been associated?</summary>
//    public bool IsReimbursed => ReimbursementId.HasValue;

//    #endregion

//    #region Computed Properties - Exchange Queries

//    /// <summary>Is exchange requested (variant selected)?</summary>
//    public bool IsExchangeRequested => ExchangeVariantId.HasValue;

//    /// <summary>Has exchange been processed (new unit created)?</summary>
//    public bool IsExchangeProcessed => ExchangeInventoryUnitId.HasValue;

//    /// <summary>Is exchange required (requested but not processed)?</summary>
//    public bool IsExchangeRequired => IsExchangeRequested && !IsExchangeProcessed;

//    #endregion

//    #region Constructors

//    private ReturnItem() { }

//    #endregion

//    #region Factory Methods

//    /// <summary>
//    /// Creates a new return item for an inventory unit.
//    /// </summary>
//    /// <param name="inventoryUnitId">The inventory unit being returned (required)</param>
//    /// <param name="amountCents">Pre-tax refund amount in cents (optional, calculated if not provided)</param>
//    /// <param name="returnAuthorizationId">RMA ID if pre-authorized (optional)</param>
//    /// <param name="customerReturnId">Customer return group ID (optional)</param>
//    /// <param name="returnReasonId">Reason for return (optional)</param>
//    /// <returns>ErrorOr containing the created ReturnItem</returns>
//    /// <remarks>
//    /// Initial state is Awaiting/Pending.
//    /// AmountCents should be calculated by refund calculator if not provided.
//    /// For exchanges, set AmountCents = 0 after calling SetExchangeVariant().
//    /// </remarks>
//    public static ErrorOr<ReturnItem> Create(
//        Guid inventoryUnitId,
//        long? amountCents = null,
//        Guid? returnAuthorizationId = null,
//        Guid? customerReturnId = null,
//        Guid? returnReasonId = null)
//    {
//        var item = new ReturnItem
//        {
//            Id = Guid.NewGuid(),
//            InventoryUnitId = inventoryUnitId,
//            AmountCents = amountCents ?? 0,
//            ReturnAuthorizationId = returnAuthorizationId,
//            CustomerReturnId = customerReturnId,
//            ReturnReasonId = returnReasonId,
//            ReceptionStatus = ReturnReceptionStatus.Awaiting,
//            AcceptanceStatus = ReturnAcceptanceStatus.Pending,
//            Resellable = true,
//            CreatedAt = DateTimeOffset.UtcNow,
//            UpdatedAt = DateTimeOffset.UtcNow
//        };

//        item.AddDomainEvent(new Events.Created(
//            ReturnItemId: item.Id,
//            InventoryUnitId: inventoryUnitId));

//        return item;
//    }

//    /// <summary>
//    /// Creates or retrieves return item for an inventory unit.
//    /// Follows Solidus from_inventory_unit pattern.
//    /// </summary>
//    /// <param name="inventoryUnit">The inventory unit to create return for</param>
//    /// <param name="amountCents">Calculated refund amount</param>
//    /// <returns>ErrorOr containing new or existing valid ReturnItem</returns>
//    /// <remarks>
//    /// Application service should:
//    /// 1. Query for existing valid return for this unit
//    /// 2. If found, return it
//    /// 3. If not, create new with calculated amount
//    /// 
//    /// This method is a factory helper - actual query happens in application layer.
//    /// </remarks>
//    public static ErrorOr<ReturnItem> FromInventoryUnit(
//        InventoryUnit inventoryUnit,
//        long amountCents)
//    {
//        if (inventoryUnit == null)
//        {
//            return Error.Validation(
//                code: "ReturnItem.InventoryUnitRequired",
//                description: "Inventory unit is required.");
//        }

//        return Create(
//            inventoryUnitId: inventoryUnit.Id,
//            amountCents: amountCents);
//    }

//    #endregion

//    #region Business Logic - Reception Status Transitions

//    /// <summary>
//    /// Marks return as in transit back to warehouse.
//    /// </summary>
//    public ErrorOr<ReturnItem> MarkInTransit()
//    {
//        if (ReceptionStatus != ReturnReceptionStatus.Awaiting)
//            return this; // Idempotent

//        ReceptionStatus = ReturnReceptionStatus.InTransit;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.MarkedInTransit(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        return this;
//    }

//    /// <summary>
//    /// Transitions return to Received state and triggers automatic acceptance evaluation.
//    /// Called when warehouse physically receives the returned item.
//    /// </summary>
//    public ErrorOr<ReturnItem> Receive()
//    {
//        if (IsReceived)
//            return this; // Idempotent

//        ReceptionStatus = ReturnReceptionStatus.Received;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.Received(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        // Attempt automatic acceptance evaluation
//        return AttemptAccept();
//    }

//    /// <summary>
//    /// Transitions to GivenToCustomer (for in-store/counter returns).
//    /// Used when customer returns item directly at store without shipping.
//    /// </summary>
//    public ErrorOr<ReturnItem> GiveToCustomer()
//    {
//        if (ReceptionStatus == ReturnReceptionStatus.GivenToCustomer)
//            return this; // Idempotent

//        ReceptionStatus = ReturnReceptionStatus.GivenToCustomer;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.GivenToCustomer(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        // Attempt automatic acceptance evaluation
//        return AttemptAccept();
//    }

//    /// <summary>
//    /// Marks return as lost in transit.
//    /// </summary>
//    public ErrorOr<ReturnItem> MarkLostInTransit()
//    {
//        ReceptionStatus = ReturnReceptionStatus.LostInTransit;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.LostInTransit(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        return this;
//    }

//    /// <summary>
//    /// Marks that customer shipped wrong item back.
//    /// </summary>
//    public ErrorOr<ReturnItem> MarkShippedWrongItem()
//    {
//        ReceptionStatus = ReturnReceptionStatus.ShippedWrongItem;
//        AcceptanceStatus = ReturnAcceptanceStatus.Rejected;
//        AcceptanceStatusErrors = new Dictionary<string, object?>
//        {
//            { "reason", "Customer shipped wrong item" }
//        };
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.ShippedWrongItem(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        return this;
//    }

//    /// <summary>
//    /// Cancels the return before it's received.
//    /// </summary>
//    public ErrorOr<ReturnItem> Cancel()
//    {
//        if (HasCompletedReception)
//            return Errors.CannotCancelAfterReception;

//        if (IsCancelled)
//            return this; // Idempotent

//        ReceptionStatus = ReturnReceptionStatus.Cancelled;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.Cancelled(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        return this;
//    }

//    /// <summary>
//    /// Marks return as expired (past return window).
//    /// </summary>
//    public ErrorOr<ReturnItem> Expire()
//    {
//        if (IsExpired)
//            return this; // Idempotent

//        ReceptionStatus = ReturnReceptionStatus.Expired;
//        AcceptanceStatus = ReturnAcceptanceStatus.Rejected;
//        AcceptanceStatusErrors = new Dictionary<string, object?>
//        {
//            { "reason", "Return window expired" }
//        };
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.Expired(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        return this;
//    }

//    #endregion

//    #region Business Logic - Acceptance Status Transitions

//    /// <summary>
//    /// Automatically evaluates return eligibility and transitions acceptance status.
//    /// Called after item is received.
//    /// </summary>
//    /// <remarks>
//    /// Eligibility checks (extensible via strategy pattern):
//    /// • Inventory unit was actually shipped
//    /// • Inventory unit not already returned
//    /// • Within return window (configurable days)
//    /// • Item condition acceptable
//    /// • Return reason valid
//    /// 
//    /// Results:
//    /// • Accepted: All checks passed
//    /// • Rejected: Failed validation
//    /// • ManualInterventionRequired: Edge case needs staff review
//    /// 
//    /// AcceptanceStatusErrors populated with failure reasons.
//    /// </remarks>
//    public ErrorOr<ReturnItem> AttemptAccept()
//    {
//        if (IsDecided)
//            return this; // Already decided

//        // Validate inventory unit loaded
//        if (InventoryUnit == null)
//            return Errors.InventoryUnitNotLoaded;

//        // Check if unit was actually shipped
//        if (InventoryUnit.State != InventoryUnit.InventoryUnitState.Shipped)
//        {
//            return RejectWithReason("Item was not shipped",
//                new { currentState = InventoryUnit.State.ToString() });
//        }

//        // Check if already returned
//        if (InventoryUnit.State == InventoryUnit.InventoryUnitState.Returned)
//            return Errors.InventoryUnitAlreadyReturned;

//        // Check return window (30 days default - should be configurable)
//        if (InventoryUnit.StateChangedAt != null)
//        {
//            var daysSinceShipment = (DateTimeOffset.UtcNow - InventoryUnit.StateChangedAt.Value).TotalDays;
//            if (daysSinceShipment > 30) // TODO: Make configurable
//            {
//                return RejectWithReason("Return window expired",
//                    new { daysSinceShipment = Math.Round(daysSinceShipment, 1) });
//            }
//        }

//        // All checks passed - accept
//        return Accept(isAutomatic: true);
//    }

//    /// <summary>
//    /// Accepts the return (manual or automatic).
//    /// Performs quality check and calculates restocking fee.
//    /// </summary>
//    /// <param name="isAutomatic">Whether this is automatic acceptance (vs manual by staff)</param>
//    public ErrorOr<ReturnItem> Accept(bool isAutomatic = false)
//    {
//        if (IsAccepted)
//            return this; // Idempotent

//        if (!IsUndecided)
//            return Errors.AcceptanceNotPending;

//        // Quality check (default: passes unless manually failed)
//        PassedQualityCheck = true;

//        if (!PassedQualityCheck)
//        {
//            Resellable = false;
//            DamageAssessment = "Item returned with damage";
//        }

//        // Calculate restocking fee based on quality
//        RestockingFeeCents = CalculateRestockingFee();

//        AcceptanceStatus = ReturnAcceptanceStatus.Accepted;
//        AcceptanceStatusErrors = null;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.Accepted(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId,
//            Automatic: isAutomatic));

//        return this;
//    }

//    /// <summary>
//    /// Rejects the return.
//    /// </summary>
//    public ErrorOr<ReturnItem> Reject(string? reason = null, object? errorDetails = null)
//    {
//        if (IsRejected)
//            return this; // Idempotent

//        if (!IsUndecided)
//            return Errors.AcceptanceNotPending;

//        return RejectWithReason(reason ?? "Return rejected", errorDetails);
//    }

//    /// <summary>
//    /// Flags return as requiring manual staff intervention.
//    /// Used when automatic evaluation cannot determine accept/reject.
//    /// </summary>
//    public ErrorOr<ReturnItem> RequireManualIntervention(string? reason = null, object? details = null)
//    {
//        AcceptanceStatus = ReturnAcceptanceStatus.ManualInterventionRequired;

//        if (reason != null)
//        {
//            AcceptanceStatusErrors = new Dictionary<string, object?>
//            {
//                { "reason", reason }
//            };

//            if (details != null)
//            {
//                AcceptanceStatusErrors["details"] = details;
//            }
//        }

//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.ManualInterventionRequired(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId,
//            Reason: reason));

//        return this;
//    }

//    /// <summary>
//    /// Helper method to reject with standardized error format.
//    /// </summary>
//    private ErrorOr<ReturnItem> RejectWithReason(string reason, object? details = null)
//    {
//        AcceptanceStatus = ReturnAcceptanceStatus.Rejected;
//        AcceptanceStatusErrors = new Dictionary<string, object?>
//        {
//            { "reason", reason }
//        };

//        if (details != null)
//        {
//            AcceptanceStatusErrors["details"] = details;
//        }

//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.Rejected(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId,
//            Reason: reason));

//        return this;
//    }

//    #endregion

//    #region Business Logic - Exchange Handling

//    /// <summary>
//    /// Sets the exchange variant (customer wants to exchange, not refund).
//    /// Validates variant eligibility and price parity.
//    /// </summary>
//    /// <param name="variant">The variant customer wants to exchange for</param>
//    /// <param name="allowPriceDifference">Whether to allow price differences (default: false)</param>
//    /// <returns>ErrorOr containing this ReturnItem</returns>
//    /// <remarks>
//    /// Exchange rules:
//    /// • Variant must be purchasable
//    /// • Price must be within threshold of original (unless allowPriceDifference=true)
//    /// • Cannot change exchange after processing
//    /// • AmountCents set to 0 (no refund for exchanges)
//    /// 
//    /// Application service responsibility:
//    /// • Query eligible variants (same product, similar price, in stock)
//    /// • Create exchange shipment and inventory unit
//    /// • Link exchange unit back to this return
//    /// </remarks>
//    public ErrorOr<ReturnItem> SetExchangeVariant(
//        Variant? variant,
//        bool allowPriceDifference = false)
//    {
//        if (variant == null)
//            return Errors.ExchangeVariantRequired;

//        if (IsExchangeProcessed)
//            return Errors.ExchangeAlreadyProcessed;

//        // Validate variant is purchasable
//        if (!variant.Purchasable)
//            return Errors.ExchangeVariantNotPurchasable;

//        // Validate price parity (business rule - configurable)
//        if (!allowPriceDifference && InventoryUnit?.LineItem != null)
//        {
//            var originalPriceCents = InventoryUnit.LineItem.PriceCents;
//            var exchangePriceCents = (long)((variant.PriceIn(Currency) ?? 0) * 100);

//            // Allow ±20% price difference (example threshold)
//            var priceDifference = Math.Abs(originalPriceCents - exchangePriceCents);
//            var priceThreshold = (decimal)(originalPriceCents * 0.2m);

//            if (priceDifference > priceThreshold)
//            {
//                return Errors.ExchangePriceMismatch(
//                    originalPriceCents / 100m,
//                    exchangePriceCents / 100m);
//            }
//        }

//        ExchangeVariantId = variant.Id;
//        ExchangeVariant = variant;
//        AmountCents = 0; // No refund for exchanges
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.ExchangeVariantSelected(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId,
//            ExchangeVariantId: variant.Id));

//        return this;
//    }

//    /// <summary>
//    /// Associates the exchange inventory unit after it's been created and shipped.
//    /// Called by application service after exchange fulfillment.
//    /// </summary>
//    /// <param name="exchangeInventoryUnitId">ID of the newly created exchange unit</param>
//    public ErrorOr<ReturnItem> AssociateExchangeInventoryUnit(Guid exchangeInventoryUnitId)
//    {
//        if (!IsExchangeRequested)
//        {
//            return Error.Validation(
//                code: "ReturnItem.NoExchangeRequested",
//                description: "Cannot associate exchange unit when no exchange was requested.");
//        }

//        if (IsExchangeProcessed)
//            return this; // Idempotent

//        ExchangeInventoryUnitId = exchangeInventoryUnitId;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.ExchangeProcessed(
//            ReturnItemId: Id,
//            ExchangeInventoryUnitId: exchangeInventoryUnitId));

//        return this;
//    }

//    /// <summary>
//    /// Cancels an exchange request (reverts to refund).
//    /// Only allowed before exchange is processed.
//    /// </summary>
//    public ErrorOr<ReturnItem> CancelExchange()
//    {
//        if (!IsExchangeRequested)
//            return this; // Nothing to cancel

//        if (IsExchangeProcessed)
//        {
//            return Error.Validation(
//                code: "ReturnItem.CannotCancelProcessedExchange",
//                description: "Cannot cancel exchange that has already been processed.");
//        }

//        ExchangeVariantId = null;
//        ExchangeVariant = null;
//        // AmountCents should be recalculated by application service
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.ExchangeCancelled(
//            ReturnItemId: Id,
//            InventoryUnitId: InventoryUnitId));

//        return this;
//    }

//    #endregion

//    #region Business Logic - Reimbursement

//    /// <summary>
//    /// Sets the refund amount in cents.
//    /// Called by application service after calculating via refund calculator.
//    /// </summary>
//    /// <param name="amountCents">Pre-tax refund amount in cents</param>
//    /// <remarks>
//    /// Amount calculation factors:
//    /// • Original item price
//    /// • Return window (early vs late returns)
//    /// • Item condition (quality check)
//    /// • Restocking fee
//    /// • Partial returns
//    /// 
//    /// Calculation done by application service using strategy pattern.
//    /// </remarks>
//    public ErrorOr<ReturnItem> SetRefundAmount(long amountCents)
//    {
//        if (IsExchangeRequested)
//        {
//            return Error.Validation(
//                code: "ReturnItem.CannotSetRefundForExchange",
//                description: "Cannot set refund amount for exchange items.");
//        }

//        if (amountCents < 0)
//        {
//            return Error.Validation(
//                code: "ReturnItem.InvalidRefundAmount",
//                description: "Refund amount cannot be negative.");
//        }

//        AmountCents = amountCents;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        return this;
//    }

//    /// <summary>
//    /// Associates a reimbursement (refund) with this return.
//    /// Can only associate with accepted returns that are not exchanges.
//    /// </summary>
//    /// <param name="reimbursementId">ID of the reimbursement record</param>
//    public ErrorOr<ReturnItem> AssociateReimbursement(Guid reimbursementId)
//    {
//        if (!IsAccepted)
//            return Errors.ReimbursementNotAllowed;

//        if (IsExchangeRequested)
//            return this; // OK - exchanges don't get reimbursements

//        if (IsReimbursed)
//            return this; // Idempotent

//        ReimbursementId = reimbursementId;
//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.ReimbursementAssociated(
//            ReturnItemId: Id,
//            ReimbursementId: reimbursementId));

//        return this;
//    }

//    /// <summary>
//    /// Sets preferred reimbursement type (customer preference).
//    /// </summary>
//    public ErrorOr<ReturnItem> SetPreferredReimbursementType(Guid reimbursementTypeId)
//    {
//        PreferredReimbursementTypeId = reimbursementTypeId;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        return this;
//    }

//    /// <summary>
//    /// Sets override reimbursement type (staff override).
//    /// </summary>
//    public ErrorOr<ReturnItem> SetOverrideReimbursementType(Guid reimbursementTypeId)
//    {
//        OverrideReimbursementTypeId = reimbursementTypeId;
//        UpdatedAt = DateTimeOffset.UtcNow;
//        return this;
//    }

//    #endregion

//    #region Business Logic - Quality Assessment

//    /// <summary>
//    /// Records quality check results.
//    /// Called by warehouse staff after inspecting returned item.
//    /// </summary>
//    /// <param name="passed">Whether item passed quality inspection</param>
//    /// <param name="damageAssessment">Notes about damage (if any)</param>
//    /// <param name="resellable">Whether item can be restocked</param>
//    public ErrorOr<ReturnItem> RecordQualityCheck(
//        bool passed,
//        string? damageAssessment = null,
//        bool? resellable = null)
//    {
//        PassedQualityCheck = passed;
//        DamageAssessment = damageAssessment;
//        Resellable = resellable ?? passed; // Default: failed QC = not resellable

//        // Recalculate restocking fee based on quality
//        RestockingFeeCents = CalculateRestockingFee();

//        UpdatedAt = DateTimeOffset.UtcNow;

//        AddDomainEvent(new Events.QualityCheckRecorded(
//            ReturnItemId: Id,
//            Passed: passed,
//            Resellable: Resellable));

//        return this;
//    }

//    /// <summary>
//    /// Calculates restocking fee based on quality and business rules.
//    /// </summary>
//    private decimal CalculateRestockingFee()
//    {
//        // Business rules (should be configurable):
//        // • Perfect condition: No fee
//        // • Minor wear: $5 fee
//        // • Not resellable: $10 fee

//        if (PassedQualityCheck && Resellable)
//            return 0; // No fee

//        if (Resellable)
//            return 500; // $5 for minor wear

//        return 1000; // $10 for damaged items
//    }

//    #endregion

//    #region Business Logic - Inventory Processing

//    /// <summary>
//    /// Processes inventory operations after return is accepted and received.
//    /// Transitions inventory unit to Returned state.
//    /// Publishes event for inventory restoration if resellable.
//    /// </summary>
//    /// <remarks>
//    /// Prerequisites:
//    /// • Return must be received
//    /// • Acceptance must be decided
//    /// • Only accepted returns are processed
//    /// 
//    /// Side effects:
//    /// • Inventory unit marked as Returned
//    /// • If resellable: InventoryRestored event published
//    /// • Application service handles stock restoration
//    /// 
//    /// Does NOT:
//    /// • Create stock movements (application service responsibility)
//    /// • Call external APIs (application service responsibility)
//    /// </remarks>
//    public ErrorOr<Success> ProcessInventoryUnit()
//    {
//        if (InventoryUnit == null)
//        {
//            return Error.NotFound(
//                code: "InventoryUnit.Missing",
//                description: "Inventory unit not loaded");
//        }

//        // Validate prerequisites
//        if (!HasCompletedReception)
//            return Errors.NotReceived;

//        if (!IsDecided)
//            return Errors.AcceptanceNotDecided;

//        // Only process accepted returns
//        if (!IsAccepted)
//            return Result.Success; // No processing needed for rejected

//        // Transition inventory unit to Returned
//        var returnResult = InventoryUnit.Return();
//        if (returnResult.IsError)
//            return returnResult.Errors;

//        // Publish event for inventory restoration if resellable
//        if (Resellable)
//        {
//            AddDomainEvent(new Events.InventoryRestored(
//                ReturnItemId: Id,
//                InventoryUnitId: InventoryUnitId,
//                VariantId: InventoryUnit.VariantId,
//                StockLocationId: InventoryUnit.StockLocationId ?? Guid.Empty));
//        }

//        return Result.Success;
//    }

//    #endregion

//    #region Business Logic - Invariant Validation

//    /// <summary>
//    /// Validates return item invariants for consistency.
//    /// Should be called before persisting critical state changes.
//    /// </summary>
//    public ErrorOr<Success> ValidateInvariants()
//    {
//        // Cannot have both exchange and reimbursement
//        if (IsExchangeRequested && IsReimbursed)
//            return Errors.ExchangeAndReimbursementConflict;

//        // Accepted returns must be received
//        if (IsAccepted && !HasCompletedReception)
//            return Errors.AcceptedButNotReceived;

//        // Exchange amount must be 0
//        if (IsExchangeRequested && AmountCents != 0)
//        {
//            return Error.Validation(
//                code: "ReturnItem.ExchangeAmountMustBeZero",
//                description: "Exchange items cannot have refund amount.");
//        }

//        // Refund amount must be non-negative
//        if (AmountCents < 0)
//        {
//            return Error.Validation(
//                code: "ReturnItem.NegativeRefundAmount",
//                description: "Refund amount cannot be negative.");
//        }

//        return Result.Success;
//    }

//    #endregion

//    #region Domain Events

//    /// <summary>
//    /// Domain events published by ReturnItem aggregate.
//    /// Enables decoupled communication with other bounded contexts:
//    /// • Inventory: Stock restoration, location tracking
//    /// • Finance: Refund processing, reimbursement tracking
//    /// • Fulfillment: Exchange shipment creation
//    /// • Notification: Customer updates, staff alerts
//    /// • Analytics: Return reasons, quality metrics
//    /// </summary>
//    public static class Events
//    {
//        /// <summary>Published when return item is created.</summary>
//        public sealed record Created(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when return is marked in transit.</summary>
//        public sealed record MarkedInTransit(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when return is physically received.</summary>
//        public sealed record Received(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published for in-store/counter returns.</summary>
//        public sealed record GivenToCustomer(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when return is lost in transit.</summary>
//        public sealed record LostInTransit(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when customer shipped wrong item.</summary>
//        public sealed record ShippedWrongItem(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when return is cancelled.</summary>
//        public sealed record Cancelled(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when return window expires.</summary>
//        public sealed record Expired(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when return is accepted.</summary>
//        public sealed record Accepted(
//            Guid ReturnItemId,
//            Guid InventoryUnitId,
//            bool Automatic) : DomainEvent;

//        /// <summary>Published when return is rejected.</summary>
//        public sealed record Rejected(
//            Guid ReturnItemId,
//            Guid InventoryUnitId,
//            string? Reason) : DomainEvent;

//        /// <summary>Published when manual intervention is required.</summary>
//        public sealed record ManualInterventionRequired(
//            Guid ReturnItemId,
//            Guid InventoryUnitId,
//            string? Reason) : DomainEvent;

//        /// <summary>Published when exchange variant is selected.</summary>
//        public sealed record ExchangeVariantSelected(
//            Guid ReturnItemId,
//            Guid InventoryUnitId,
//            Guid ExchangeVariantId) : DomainEvent;

//        /// <summary>Published when exchange is processed and unit created.</summary>
//        public sealed record ExchangeProcessed(
//            Guid ReturnItemId,
//            Guid ExchangeInventoryUnitId) : DomainEvent;

//        /// <summary>Published when exchange is cancelled.</summary>
//        public sealed record ExchangeCancelled(
//            Guid ReturnItemId,
//            Guid InventoryUnitId) : DomainEvent;

//        /// <summary>Published when reimbursement is associated.</summary>
//        public sealed record ReimbursementAssociated(
//            Guid ReturnItemId,
//            Guid ReimbursementId) : DomainEvent;

//        /// <summary>Published when quality check is recorded.</summary>
//        public sealed record QualityCheckRecorded(
//            Guid ReturnItemId,
//            bool Passed,
//            bool Resellable) : DomainEvent;

//        /// <summary>
//        /// Published when inventory should be restored.
//        /// Handled by Inventory bounded context to restock item.
//        /// </summary>
//        public sealed record InventoryRestored(
//            Guid ReturnItemId,
//            Guid InventoryUnitId,
//            Guid VariantId,
//            Guid StockLocationId) : DomainEvent;
//    }

//    #endregion
//}



///// <summary>
///// Placeholder for ReturnAuthorization aggregate.
///// Manages RMA (Return Merchandise Authorization) process.
///// </summary>
//public class ReturnAuthorization : Aggregate
//{
//    public Guid OrderId { get; set; }
//    public string? Currency { get; set; }
//    public string? AuthorizationNumber { get; set; }
//    public DateTimeOffset? ExpiresAt { get; set; }
//}

///// <summary>
///// Placeholder for CustomerReturn aggregate.
///// Groups multiple return items shipped together back to warehouse.
///// </summary>
//public class CustomerReturn : Aggregate
//{
//    public Guid OrderId { get; set; }
//    public Guid StockLocationId { get; set; }
//    public string? TrackingNumber { get; set; }
//    public ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
//}

///// <summary>
///// Placeholder for Reimbursement aggregate.
///// Manages refund processing and tracking.
///// </summary>
//public class Reimbursement : Aggregate
//{
//    public Guid OrderId { get; set; }
//    public decimal TotalCents { get; set; }
//    public string? Status { get; set; } // pending, reimbursed, failed
//    public ICollection<ReturnItem> ReturnItems { get; set; } = new List<ReturnItem>();
//}

///// <summary>
///// Placeholder for ReimbursementType entity.
///// Defines refund methods (store credit, original payment, check, etc.).
///// </summary>
//public class ReimbursementType : Entity
//{
//    public string Name { get; set; } = string.Empty;
//    public string Type { get; set; } = string.Empty; // store_credit, original_payment, etc.
//    public bool Active { get; set; }
//}

///// <summary>
///// Placeholder for ReturnReason entity.
///// Categorizes why customers return items.
///// </summary>
//public class ReturnReason : Entity
//{
//    public string Name { get; set; } = string.Empty;
//    public bool Active { get; set; }
//    public int SortOrder { get; set; }
//}