using ErrorOr;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Catalog.Products.Variants;
using ReSys.Core.Domain.Orders.Shipments;

namespace ReSys.Core.Domain.Orders.Returns;

/// <summary>
/// ⚠️ DISABLED: Return flow is currently disabled. This model exists for data compatibility only.
/// All return-related features (reception, acceptance, reimbursement, exchanges) are non-functional.
/// 
/// This class may be re-enabled in future versions with full Spree-aligned implementation:
/// - CustomerReturn aggregate for grouping returns
/// - Reimbursement aggregate for refund tracking
/// - ReturnAuthorization for approval workflow
/// 
/// Represents a return request for an inventory unit. Tracks the return lifecycle from initiation
/// through reception, acceptance/rejection, and reimbursement processing.
/// </summary>
/// <remarks>
/// <para>
/// <b>Responsibility:</b>
/// Each ReturnItem represents a customer's desire to return a specific inventory unit. It manages:
/// <list type="bullet">
/// <item>Return eligibility validation</item>
/// <item>Return reception status (awaiting, received, given to customer, cancelled)</item>
/// <item>Acceptance status (pending, accepted, rejected, manual intervention required)</item>
/// <item>Reimbursement processing and exchange variant selection</item>
/// <item>Inventory restoration if return is accepted and resellable</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Return Reception Flow:</b>
/// <list type="number">
/// <item><b>Awaiting:</b> Return initiated, waiting for item to be received</item>
/// <item><b>Received:</b> Item received from customer, acceptance status evaluated</item>
/// <item><b>GivenToCustomer:</b> Item processed without formal receipt (warehouse/store pickup)</item>
/// <item><b>Cancelled:</b> Return cancelled before completion</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Acceptance Status Flow:</b>
/// <list type="number">
/// <item><b>Pending:</b> Initial state, waiting for evaluation</item>
/// <item><b>Accepted:</b> Return approved, eligible for reimbursement/exchange</item>
/// <item><b>Rejected:</b> Return denied, no reimbursement</item>
/// <item><b>ManualInterventionRequired:</b> Return requires manual review</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Exchange vs Refund:</b>
/// <list type="bullet">
/// <item><b>Exchange:</b> Customer selects ExchangeVariant; new InventoryUnit created with variant</item>
/// <item><b>Refund:</b> No ExchangeVariant; reimbursement calculated and processed</item>
/// <item><b>No Reimbursement:</b> Return rejected or not yet accepted</item>
/// </list>
/// </para>
/// 
/// <para>
/// <b>Tax Handling:</b>
/// Removed in this port. All pricing is pre-tax; tax calculations handled at LineItem level.
/// </para>
/// </remarks>
public sealed class ReturnItem : Aggregate
{
    #region Enums
    public enum ReturnReceptionStatus
    {
        Awaiting = 0,
        Received = 1,
        GivenToCustomer = 2,
        Cancelled = 3
    }

    public enum ReturnAcceptanceStatus
    {
        Pending = 0,
        Accepted = 1,
        Rejected = 2,
        ManualInterventionRequired = 3
    }
    #endregion

    #region Constants
    private static readonly ReturnReceptionStatus[] CompletedReceptionStatuses = 
    {
        ReturnReceptionStatus.Received,
        ReturnReceptionStatus.GivenToCustomer
    };
    #endregion

    #region Constraints
    public static class Constraints
    {
        public const int MinReturnQuantity = 1;
        public const long PreTaxAmountMaxCents = long.MaxValue;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error InvalidReturnQuantity =>
            Error.Validation(
                code: "ReturnItem.InvalidReturnQuantity",
                description: "Return quantity must be at least 1.");



        public static Error IneligibleForReturn =>
            Error.Validation(
                code: "ReturnItem.IneligibleForReturn",
                description: "This inventory unit is not eligible for return.");

        public static Error InvalidExchangeVariant =>
            Error.Validation(
                code: "ReturnItem.InvalidExchangeVariant",
                description: "The selected exchange variant is not eligible for exchange.");

        public static Error CannotProcessExchange =>
            Error.Conflict(
                code: "ReturnItem.CannotProcessExchange",
                description: "Cannot process exchange for this return item.");

        public static Error ReimbursementNotAssociated =>
            Error.Validation(
                code: "ReturnItem.ReimbursementNotAssociated",
                description: "Cannot associate reimbursement unless return is accepted.");

        public static Error OtherCompletedReturnExists =>
            Error.Conflict(
                code: "ReturnItem.OtherCompletedReturnExists",
                description: "Another completed return already exists for this inventory unit.");

        public static Error CannotCancelAfterReception =>
            Error.Validation(
                code: "ReturnItem.CannotCancelAfterReception",
                description: "Cannot cancel a return that has already been received.");

        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "ReturnItem.NotFound",
                description: $"Return item with ID '{id}' was not found.");

        public static Error InvalidStateTransition(ReturnReceptionStatus from, ReturnReceptionStatus to) =>
            Error.Validation(
                code: "ReturnItem.InvalidStateTransition",
                description: $"Cannot transition from {from} to {to}.");

        public static Error AcceptanceNotPending =>
            Error.Validation(
                code: "ReturnItem.AcceptanceNotPending",
                description: "Can only accept/reject returns with pending acceptance status.");

        public static Error InventoryUnitAlreadyReturned =>
            Error.Conflict(
                code: "ReturnItem.InventoryUnitAlreadyReturned",
                description: "The inventory unit associated with this return has already been returned.");

        public static Error ExchangeAndReimbursementConflict =>
            Error.Validation(
                code: "ReturnItem.ExchangeAndReimbursementConflict",
                description: "Cannot have both exchange and reimbursement for a return item.");

        public static Error AcceptedButNotReceived =>
            Error.Validation(
                code: "ReturnItem.AcceptedButNotReceived",
                description: "Cannot accept return that hasn't been received.");
    }
    #endregion

    #region Properties

    /// <summary>Gets the inventory unit being returned.</summary>
    public Guid InventoryUnitId { get; set; }

    /// <summary>Gets the return authorization ID that approved this return (if any).</summary>
    public Guid? ReturnAuthorizationId { get; set; }

    /// <summary>Gets the optional customer return (grouped returns) ID.</summary>
    public Guid? CustomerReturnId { get; set; }

    /// <summary>Gets the optional reimbursement ID if processed for refund.</summary>
    public Guid? ReimbursementId { get; set; }

    /// <summary>Gets the optional exchange variant if customer requested exchange.</summary>
    public Guid? ExchangeVariantId { get; set; }

    /// <summary>Gets the quantity of the inventory unit being returned.</summary>
    /// <remarks>
    /// When the return is partial (return_quantity &lt; inventory_unit.quantity),
    /// a new InventoryUnit is created for the returned quantity.
    /// </remarks>
    public int ReturnQuantity { get; set; }

    /// <summary>Gets the pre-tax amount in cents to be refunded for this return.</summary>
    /// <remarks>
    /// Set to 0 for exchanges. Calculated based on the item's price and return quantity.
    /// </remarks>
    public long PreTaxAmountCents { get; set; }

    /// <summary>Gets the current reception status (awaiting, received, given, cancelled).</summary>
    public ReturnReceptionStatus ReceptionStatus { get; set; } = ReturnReceptionStatus.Awaiting;

    /// <summary>Gets the current acceptance status (pending, accepted, rejected, manual).</summary>
    public ReturnAcceptanceStatus AcceptanceStatus { get; set; } = ReturnAcceptanceStatus.Pending;

    /// <summary>Gets a value indicating whether this return item can be restocked as inventory.</summary>
    /// <remarks>
    /// Used to determine if stock movements should be created when return is received.
    /// </remarks>
    public bool Resellable { get; set; } = true;

    /// <summary>Gets serialized validation errors from eligibility checks.</summary>
    /// <remarks>
    /// Populated during attempt_accept state transition if eligibility checks fail.
    /// </remarks>
    public IDictionary<string, object?>? AcceptanceStatusErrors { get; set; }

    /// <summary>Gets or sets the restocking fee in cents to be deducted from the refund.</summary>
    public decimal RestockingFeeCents { get; set; }

    /// <summary>Gets or sets notes or references related to any damage found on the returned item.</summary>
    public string? DamageAssessment { get; set; }

    /// <summary>Gets or sets a value indicating whether the returned item passed the quality check.</summary>
    public bool PassedQualityCheck { get; set; }
    #endregion

    #region Relationships
    public InventoryUnit? InventoryUnit { get; set; }
    public Variant? ExchangeVariant { get; set; }
    
    /// <summary>Gets the exchange inventory units created from this return (if exchanged).</summary>
    public ICollection<InventoryUnit> ExchangeInventoryUnits { get; set; } = new List<InventoryUnit>();
    #endregion

    #region Constructors
    private ReturnItem() { }
    #endregion

    #region Factory Methods

    /// <summary>
    /// Creates a new return item for an inventory unit.
    /// </summary>
    /// <param name="inventoryUnitId">The inventory unit being returned.</param>
    /// <param name="returnQuantity">Optional quantity (defaults to inventory unit's full quantity).</param>
    /// <param name="preTaxAmountCents">Pre-tax refund amount in cents (default: auto-calculated).</param>
    /// <param name="returnAuthorizationId">Optional RMA (return authorization) ID.</param>
    /// <param name="customerReturnId">Optional customer return group ID.</param>
    /// <returns>
    /// On success: A new ReturnItem instance in Awaiting/Pending state.
    /// On failure: Error if quantity invalid or exceeds inventory unit quantity.
    /// </returns>
    public static ErrorOr<ReturnItem> Create(
        Guid inventoryUnitId,
        int? returnQuantity = null,
        long? preTaxAmountCents = null,
        Guid? returnAuthorizationId = null,
        Guid? customerReturnId = null)
    {
        if (returnQuantity.HasValue && returnQuantity.Value < Constraints.MinReturnQuantity)
            return Errors.InvalidReturnQuantity;

        var item = new ReturnItem
        {
            Id = Guid.NewGuid(),
            InventoryUnitId = inventoryUnitId,
            ReturnQuantity = returnQuantity ?? 1,
            PreTaxAmountCents = preTaxAmountCents ?? 0,
            ReturnAuthorizationId = returnAuthorizationId,
            CustomerReturnId = customerReturnId,
            ReceptionStatus = ReturnReceptionStatus.Awaiting,
            AcceptanceStatus = ReturnAcceptanceStatus.Pending,
            CreatedAt = DateTimeOffset.UtcNow
        };

        item.AddDomainEvent(
            domainEvent: new Events.Created(
                ReturnItemId: item.Id,
                InventoryUnitId: inventoryUnitId,
                ReturnQuantity: item.ReturnQuantity));

        return item;
    }

    /// <summary>
    /// ⚠️ DISABLED: Returns feature is disabled.
    /// Creates or retrieves an existing return item for an inventory unit.
    /// </summary>
    [Obsolete("Returns feature is disabled. This method will be re-implemented when returns are enabled.")]
    public static ErrorOr<ReturnItem> FromInventoryUnit(InventoryUnit? inventoryUnit)
    {
        return Error.Validation(
            code: "ReturnItem.FeatureDisabled",
            description: "Return flow is currently disabled. Please contact support.");
    }

    /// <summary>
    /// Creates a return for a partial quantity of a non-individually tracked inventory unit.
    /// This will split the original inventory unit.
    /// </summary>
    public static ErrorOr<(ReturnItem NewReturnItem, InventoryUnit SplitOffUnit)> CreateForPartialReturn(
        InventoryUnit unitToSplit, 
        int returnQuantity)
    {
        // TODO: This logic is invalidated by the InventoryUnit refactoring and needs to be reimplemented.
        // The creation of an InventoryUnit now requires a ShipmentId, which is not available here.
        // The concept of splitting a unit during a return needs to be re-evaluated.
        return Error.Failure("ReturnItem.CreateForPartialReturn.NotImplemented", "Partial return logic has not been implemented for the new shipment model.");
        /*
        if (unitToSplit.RequiresIndividualTracking)
        {
            return Error.Validation(
                code: "ReturnItem.CannotPartiallyReturnTrackedUnit",
                description: "Individually tracked inventory units cannot be partially returned.");
        }

        if (returnQuantity <= 0 || returnQuantity >= unitToSplit.Quantity)
        {
            return Error.Validation(
                code: "ReturnItem.InvalidPartialQuantity",
                description: $"Return quantity must be greater than 0 and less than the unit quantity of {unitToSplit.Quantity}.");
        }

        // 1. "Split" the original inventory unit by reducing its quantity.
        // (Note: This modification of a separate aggregate is a simplification.
        // In a stricter system, this would be orchestrated by a domain service).
        unitToSplit.Quantity -= returnQuantity;

        // 2. Create a new inventory unit representing the part being returned.
        var splitOffUnitResult = InventoryUnit.Create(
            unitToSplit.VariantId,
            unitToSplit.OrderId,
            unitToSplit.LineItemId,
            returnQuantity,
            unitToSplit.StockLocationId,
            unitToSplit.ShipmentId);
        
        if (splitOffUnitResult.IsError)
            return splitOffUnitResult.Errors;

        var splitOffUnit = splitOffUnitResult.Value;
        splitOffUnit.State = unitToSplit.State; // Match the original unit's state.

        // 3. Create a ReturnItem for the new, split-off inventory unit.
        var returnItemResult = Create(splitOffUnit.Id, returnQuantity);
        if (returnItemResult.IsError)
            return returnItemResult.Errors;

        return (returnItemResult.Value, splitOffUnit);
        */
    }

    #endregion

    #region Business Logic: Reception Status Transitions

    /// <summary>
    /// Transitions return to Received state and triggers acceptance evaluation.
    /// </summary>
    /// <returns>This return item (for method chaining).</returns>
    /// <remarks>
    /// After reception, acceptance status is evaluated. This may trigger automatic acceptance
    /// or rejection based on return eligibility rules.
    /// </remarks>
    public ErrorOr<ReturnItem> Receive()
    {
        if (ReceptionStatus != ReturnReceptionStatus.Awaiting)
            return this; // Idempotent

        ReceptionStatus = ReturnReceptionStatus.Received;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.Received(
                ReturnItemId: Id,
                InventoryUnitId: InventoryUnitId));

        // Attempt automatic acceptance based on eligibility
        return AttemptAccept();
    }

    /// <summary>
    /// Transitions return to GivenToCustomer state (for in-store/warehouse returns).
    /// </summary>
    public ErrorOr<ReturnItem> GiveToCustomer()
    {
        if (ReceptionStatus != ReturnReceptionStatus.Awaiting)
            return this; // Idempotent

        ReceptionStatus = ReturnReceptionStatus.GivenToCustomer;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.GivenToCustomer(
                ReturnItemId: Id,
                InventoryUnitId: InventoryUnitId));

        // Attempt automatic acceptance based on eligibility
        return AttemptAccept();
    }

    /// <summary>
    /// Transitions return to Cancelled state.
    /// </summary>
    /// <returns>
    /// On success: This return item.
    /// On failure: Error if return has already been received.
    /// </returns>
    public ErrorOr<ReturnItem> Cancel()
    {
        if (ReceptionStatus != ReturnReceptionStatus.Awaiting)
            return Errors.CannotCancelAfterReception;

        if (ReceptionStatus == ReturnReceptionStatus.Cancelled)
            return this; // Idempotent

        ReceptionStatus = ReturnReceptionStatus.Cancelled;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.Cancelled(
                ReturnItemId: Id,
                InventoryUnitId: InventoryUnitId));

        return this;
    }

    #endregion

    #region Business Logic: Acceptance Status Transitions

    /// <summary>
    /// Automatically evaluates return eligibility and transitions acceptance status.
    /// </summary>
    /// <remarks>
    /// This is called after a return is received. Eligibility checks may result in:
    /// <list type="bullet">
    /// <item>Accepted - return is eligible and valid</item>
    /// <item>Rejected - return is ineligible (e.g., item damaged, outside return window)</item>
    /// <item>ManualInterventionRequired - return needs manual review</item>
    /// </list>
    /// 
    /// Subclasses would override this with custom eligibility validation logic.
    /// </remarks>
    public ErrorOr<ReturnItem> AttemptAccept()
    {
        if (AcceptanceStatus != ReturnAcceptanceStatus.Pending)
            return this; // Already decided

        // NEW: Validate inventory unit state
        if (InventoryUnit == null)
        {
            return Error.NotFound(
                code: "ReturnItem.InventoryUnitNotLoaded",
                description: "Inventory unit must be loaded to evaluate return.");
        }

        // Check if unit was actually shipped
        if (InventoryUnit.State != InventoryUnit.InventoryUnitState.Shipped)
        {
            AcceptanceStatus = ReturnAcceptanceStatus.Rejected;
            AcceptanceStatusErrors = new Dictionary<string, object?>
            {
                { "reason", "Item was not shipped" },
                { "currentState", InventoryUnit.State.ToString() }
            };
            UpdatedAt = DateTimeOffset.UtcNow;
            
            AddDomainEvent(
                domainEvent: new Events.Rejected(
                    ReturnItemId: Id,
                    InventoryUnitId: InventoryUnitId));
            
            return this;
        }

        // Check if already returned
        if (InventoryUnit.State == InventoryUnit.InventoryUnitState.Returned)
        {
            return Errors.InventoryUnitAlreadyReturned;
        }

        // NEW: Check return window (example: 30 days, should be configurable)
        if (InventoryUnit.UpdatedAt.HasValue)
        {
            var daysSinceShipment = (DateTimeOffset.UtcNow - InventoryUnit.UpdatedAt.Value).TotalDays;
            if (daysSinceShipment > 30)
            {
                AcceptanceStatus = ReturnAcceptanceStatus.Rejected;
                AcceptanceStatusErrors = new Dictionary<string, object?>
                {
                    { "reason", "Return window expired" },
                    { "daysSinceShipment", daysSinceShipment }
                };
                UpdatedAt = DateTimeOffset.UtcNow;
                
                AddDomainEvent(
                    domainEvent: new Events.Rejected(
                        ReturnItemId: Id,
                        InventoryUnitId: InventoryUnitId));
                
                return this;
            }
        }

        // Passed all checks - accept
        return Accept(isAutomatic: true);
    }

    /// <summary>
    /// Manually accepts a return, performs quality check, and applies restocking fees.
    /// </summary>
    public ErrorOr<ReturnItem> Accept(bool isAutomatic = false)
    {
        if (AcceptanceStatus == ReturnAcceptanceStatus.Accepted)
            return this; // Idempotent

        if (AcceptanceStatus != ReturnAcceptanceStatus.Pending && 
            AcceptanceStatus != ReturnAcceptanceStatus.ManualInterventionRequired)
        {
            return Errors.AcceptanceNotPending;
        }

        // --- Quality Check and Restocking Fee Logic ---
        // Placeholder logic: In a real system, this might involve complex rules.
        PassedQualityCheck = true; // Assume it passes by default
        DamageAssessment = PassedQualityCheck ? null : "Item returned with visible wear.";

        if (!PassedQualityCheck)
        {
            // Business Rule: If item fails quality check, it's not resellable.
            Resellable = false;
        }

        // Business Rule: Apply a restocking fee if the item is not in pristine condition
        // or based on company policy.
        if (Resellable && PassedQualityCheck)
        {
            RestockingFeeCents = 0; // No fee for perfect returns
        }
        else
        {
            // Example: Apply a $5 restocking fee. This should be based on configurable rules.
            RestockingFeeCents = 500; 
        }
        // --- End of Placeholder Logic ---

        AcceptanceStatus = ReturnAcceptanceStatus.Accepted;
        AcceptanceStatusErrors = null; // Clear any previous errors
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.Accepted(
                ReturnItemId: Id,
                InventoryUnitId: InventoryUnitId,
                Automatic: isAutomatic));

        return this;
    }

    /// <summary>
    /// Rejects a return (bypassing eligibility checks).
    /// </summary>
    public ErrorOr<ReturnItem> Reject()
    {
        if (AcceptanceStatus == ReturnAcceptanceStatus.Rejected)
            return this; // Idempotent

        if (AcceptanceStatus != ReturnAcceptanceStatus.Pending && 
            AcceptanceStatus != ReturnAcceptanceStatus.ManualInterventionRequired)
        {
            return Errors.AcceptanceNotPending;
        }

        AcceptanceStatus = ReturnAcceptanceStatus.Rejected;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.Rejected(
                ReturnItemId: Id,
                InventoryUnitId: InventoryUnitId));

        return this;
    }

    /// <summary>
    /// Flags return as requiring manual intervention.
    /// </summary>
    public ErrorOr<ReturnItem> RequireManualIntervention()
    {
        AcceptanceStatus = ReturnAcceptanceStatus.ManualInterventionRequired;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.ManualInterventionRequired(
                ReturnItemId: Id,
                InventoryUnitId: InventoryUnitId));

        return this;
    }

    #endregion

    #region Business Logic: Exchange Handling

    /// <summary>
    /// Selects an exchange variant for this return (customer wants to exchange, not refund).
    /// </summary>
    /// <param name="variant">The variant to exchange for.</param>
    /// <returns>
    /// On success: This return item.
    /// On failure: Error if variant is not eligible or return cannot be exchanged.
    /// </returns>
    public ErrorOr<ReturnItem> SetExchangeVariant(Variant? variant)
    {
        if (variant == null)
        {
            return Error.Validation(
                code: "ReturnItem.VariantRequired",
                description: "Exchange variant is required.");
        }

        // NEW: Cannot set exchange variant after return processed
        if (IsExchangeProcessed)
        {
            return Error.Validation(
                code: "ReturnItem.ExchangeAlreadyProcessed",
                description: "Exchange has already been processed.");
        }

        // NEW: Validate variant is purchasable
        if (!variant.Purchasable)
        {
            return Error.Validation(
                code: "ReturnItem.ExchangeVariantNotPurchasable",
                description: "Exchange variant is not available for purchase.");
        }

        // NEW: Optionally validate price parity (business rule)
        if (InventoryUnit?.Variant != null)
        {
            var originalPrice = InventoryUnit.LineItem?.PriceCents ?? 0;
            var exchangePrice = (long)(variant.PriceIn(InventoryUnit.LineItem?.Currency ?? "USD") * 100 ?? 0);
            
            // Allow exchange only if within 20% of original price (example rule)
            var priceDifference = Math.Abs(originalPrice - exchangePrice);
            var priceThreshold = originalPrice * 0.2m;
            
            if (priceDifference > priceThreshold)
            {
                return Error.Validation(
                    code: "ReturnItem.ExchangePriceMismatch",
                    description: $"Exchange variant price differs too much from original. Original: {originalPrice / 100m:C}, Exchange: {exchangePrice / 100m:C}");
            }
        }

        ExchangeVariantId = variant.Id;
        ExchangeVariant = variant;
        PreTaxAmountCents = 0; // No refund for exchanges
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.ExchangeVariantSelected(
                ReturnItemId: Id,
                InventoryUnitId: InventoryUnitId,
                ExchangeVariantId: variant.Id));

        return this;
    }

    /// <summary>
    /// Gets a value indicating whether this return is requesting an exchange.
    /// </summary>
    public bool IsExchangeRequested => ExchangeVariantId.HasValue;

    /// <summary>
    /// Gets a value indicating whether an exchange has been processed for this return.
    /// </summary>
    public bool IsExchangeProcessed => ExchangeInventoryUnits.Any();

    /// <summary>
    /// Gets a value indicating whether an exchange is still pending (requested but not processed).
    /// </summary>
    public bool IsExchangeRequired => IsExchangeRequested && !IsExchangeProcessed;

    #endregion

    #region Business Logic: Reimbursement

    /// <summary>
    /// Associates a reimbursement with this return (for refunds).
    /// </summary>
    /// <param name="reimbursementId">The reimbursement ID.</param>
    /// <returns>
    /// On success: This return item.
    /// On failure: Error if return not accepted or is an exchange.
    /// </returns>
    public ErrorOr<ReturnItem> AssociateReimbursement(Guid reimbursementId)
    {
        if (AcceptanceStatus != ReturnAcceptanceStatus.Accepted)
            return Errors.ReimbursementNotAssociated;

        // Exchanges don't get reimbursed (no refund amount)
        if (IsExchangeRequested && PreTaxAmountCents == 0)
            return this; // OK for exchange

        ReimbursementId = reimbursementId;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(
            domainEvent: new Events.ReimbursementAssociated(
                ReturnItemId: Id,
                ReimbursementId: reimbursementId));

        return this;
    }

    #endregion

    #region Business Logic: Inventory Processing

    /// <summary>
    /// Processes inventory operations after return reception:
    /// - Transitions inventory unit to Returned state
    /// - Creates stock movement to restore inventory if resellable
    /// </summary>
    /// <remarks>
    /// Called after return reception status is finalized.
    /// </remarks>
    public ErrorOr<Success> ProcessInventoryUnit()
    {
        if (InventoryUnit == null)
        {
            return Error.NotFound(
                code: "InventoryUnit.Missing",
                description: "Inventory unit not loaded");
        }

        // NEW: Validate return has been received
        if (!HasCompletedReception)
        {
            return Error.Validation(
                code: "ReturnItem.NotReceived",
                description: "Return must be received before processing inventory.");
        }

        // NEW: Validate acceptance status
        if (!IsDecided)
        {
            return Error.Validation(
                code: "ReturnItem.AcceptanceNotDecided",
                description: "Return acceptance must be decided before processing inventory.");
        }

        // Only process if accepted
        if (AcceptanceStatus != ReturnAcceptanceStatus.Accepted)
        {
            return Result.Success; // Nothing to process for rejected returns
        }

        // Transition inventory unit to Returned
        var returnResult = InventoryUnit.Return();
        if (returnResult.IsError)
            return returnResult.Errors;

        // Publish event for inventory restoration if resellable
        if (Resellable && AcceptanceStatus == ReturnAcceptanceStatus.Accepted)
        {
            AddDomainEvent(
                domainEvent: new Events.InventoryRestored(
                    ReturnItemId: Id,
                    InventoryUnitId: InventoryUnitId,
                    Quantity: 1)); // Always 1 per unit
        }

        return Result.Success;
    }

    #endregion

    #region Business Logic: Invariants

    // NEW: Add validation method
    public ErrorOr<Success> ValidateInvariants()
    {
        // Validate quantity (always 1 with fixed InventoryUnit)
        if (ReturnQuantity != 1)
        {
            return Error.Validation(
                code: "ReturnItem.InvalidQuantity",
                description: "Return quantity must be exactly 1 per return item.");
        }

        // Validate state consistency
        if (IsExchangeRequested && IsReimbursed)
        {
            return Error.Validation(
                code: "ReturnItem.ExchangeAndReimbursementConflict",
                description: "Cannot have both exchange and reimbursement.");
        }

        // Validate reception/acceptance state consistency
        if (AcceptanceStatus == ReturnAcceptanceStatus.Accepted && 
            !HasCompletedReception)
        {
            return Error.Validation(
                code: "ReturnItem.AcceptedButNotReceived",
                description: "Cannot accept return that hasn't been received.");
        }

        return Result.Success;
    }

    #endregion

    #region Queries

    /// <summary>
    /// Gets a value indicating whether this return has completed reception.
    /// </summary>
    public bool HasCompletedReception => CompletedReceptionStatuses.Contains(ReceptionStatus);

    /// <summary>
    /// Gets a value indicating whether this return is fully decided (accepted or rejected).
    /// </summary>
    public bool IsDecided => 
        AcceptanceStatus == ReturnAcceptanceStatus.Accepted || 
        AcceptanceStatus == ReturnAcceptanceStatus.Rejected;

    /// <summary>
    /// Gets a value indicating whether this return is reimbursed.
    /// </summary>
    public bool IsReimbursed => ReimbursementId.HasValue;

    #endregion

    #region Domain Events

    public static class Events
    {
        /// <summary>Raised when a return item is created.</summary>
        public sealed record Created(Guid ReturnItemId, Guid InventoryUnitId, int ReturnQuantity) : DomainEvent;

        /// <summary>Raised when a return is received from customer.</summary>
        public sealed record Received(Guid ReturnItemId, Guid InventoryUnitId) : DomainEvent;

        /// <summary>Raised when a return is given to customer (in-store return).</summary>
        public sealed record GivenToCustomer(Guid ReturnItemId, Guid InventoryUnitId) : DomainEvent;

        /// <summary>Raised when a return is cancelled.</summary>
        public sealed record Cancelled(Guid ReturnItemId, Guid InventoryUnitId) : DomainEvent;

        /// <summary>Raised when return acceptance is evaluated (automatic).</summary>
        public sealed record Accepted(Guid ReturnItemId, Guid InventoryUnitId, bool Automatic) : DomainEvent;

        /// <summary>Raised when a return is rejected.</summary>
        public sealed record Rejected(Guid ReturnItemId, Guid InventoryUnitId) : DomainEvent;

        /// <summary>Raised when a return requires manual intervention.</summary>
        public sealed record ManualInterventionRequired(Guid ReturnItemId, Guid InventoryUnitId) : DomainEvent;

        /// <summary>Raised when an exchange variant is selected.</summary>
        public sealed record ExchangeVariantSelected(Guid ReturnItemId, Guid InventoryUnitId, Guid ExchangeVariantId) : DomainEvent;

        /// <summary>Raised when a reimbursement is associated.</summary>
        public sealed record ReimbursementAssociated(Guid ReturnItemId, Guid ReimbursementId) : DomainEvent;

        /// <summary>Raised when inventory should be restored from accepted return.</summary>
        public sealed record InventoryRestored(Guid ReturnItemId, Guid InventoryUnitId, int Quantity) : DomainEvent;
    }

    #endregion
}
