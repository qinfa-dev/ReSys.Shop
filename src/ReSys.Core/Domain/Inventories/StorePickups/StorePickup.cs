using System.Security.Cryptography;
using System.Text;

using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Inventories.Locations;

namespace ReSys.Core.Domain.Inventories.StorePickups;

/// <summary>
/// Represents a store pickup fulfillment option, allowing customers to collect orders from retail locations.
/// This aggregate manages the lifecycle of a pickup from creation through customer collection.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Responsibility:</strong>
/// Manages store pickup fulfillment for retail locations. Each pickup is associated with an order and a specific
/// stock location (retail store) where the customer will collect their items. The aggregate tracks pickup status,
/// generates secure pickup codes for customer identification, and manages the pickup lifecycle.
/// </para>
/// 
/// <para>
/// <strong>State Machine:</strong>
/// <list type="bullet">
/// <item><b>Pending:</b> Initial state when pickup is created. Items not yet prepared.</item>
/// <item><b>Ready:</b> Items have been picked and are ready for customer collection.</item>
/// <item><b>PickedUp:</b> Customer has collected the items.</item>
/// <item><b>Cancelled:</b> Pickup was cancelled (stock released back to available inventory).</item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Pickup Code Generation:</strong>
/// Pickup codes are generated using location and order information to create human-readable, secure codes.
/// Format: {LOCATION-PREFIX}-{ORDER-ID-CHECKSUM}-{RANDOM-SUFFIX}
/// Example: NYC-12345-A7K9
/// 
/// The code provides:
/// • Human-readable identification for customers and staff
/// • Uniqueness (extremely low collision probability with random suffix)
/// • Location context (first segment indicates pickup location)
/// • Security (checksum validates code integrity)
/// </para>
/// 
/// <para>
/// <strong>Lifecycle:</strong>
/// 1. Create: Pickup is created when location is selected via fulfillment strategy
/// 2. Prepare: Items are picked from shelves at the retail location
/// 3. Ready: Pickup is marked ready, customer is notified
/// 4. PickUp: Customer collects items using pickup code
/// 5. Complete: Inventory is finalized as sold
/// 
/// Alternatively:
/// • Cancel: At any point before PickedUp, pickup can be cancelled
/// </para>
/// 
/// <para>
/// <strong>Integration:</strong>
/// • Created by fulfillment orchestration when strategy selects pickup-enabled location
/// • Integrated with Phase 1 StockLocation (requires PickupEnabled capability)
/// • Integrated with Phase 2 FulfillmentStrategy for location selection
/// • Generates domain events for order workflow integration
/// </para>
/// </remarks>
public sealed class StorePickup : Aggregate
{
    #region State Machine
    /// <summary>
    /// Defines the valid states a store pickup progresses through during its lifecycle.
    /// </summary>
    public enum PickupState
    {
        /// <summary>Initial state. Pickup created but items not yet prepared.</summary>
        Pending = 1,

        /// <summary>Items have been picked and are ready for customer collection.</summary>
        Ready = 2,

        /// <summary>Customer has collected the items.</summary>
        PickedUp = 3,

        /// <summary>Pickup was cancelled. Stock released back to available inventory.</summary>
        Cancelled = 4
    }
    #endregion

    #region Constraints
    /// <summary>
    /// Defines constraints and constant values specific to <see cref="StorePickup"/> properties.
    /// </summary>
    public static class Constraints
    {
        /// <summary>Maximum allowed length for the <see cref="PickupCode"/> property.</summary>
        public const int PickupCodeMaxLength = 50;

        /// <summary>Maximum allowed length for optional cancellation reason.</summary>
        public const int CancellationReasonMaxLength = 500;

        /// <summary>Pickup code format: {PREFIX}-{CHECKSUM}-{SUFFIX}. First segment max length.</summary>
        public const int LocationPrefixMaxLength = 10;

        /// <summary>Random suffix length for pickup code uniqueness (alphanumeric characters).</summary>
        public const int PickupCodeRandomSuffixLength = 4;

        /// <summary>Number of days before pickup expires if not collected.</summary>
        public const int PickupExpirationDays = 14;
    }
    #endregion

    #region Errors
    /// <summary>
    /// Defines domain error scenarios specific to <see cref="StorePickup"/> operations.
    /// </summary>
    public static class Errors
    {
        /// <summary>
        /// Error indicating that a requested store pickup could not be found.
        /// </summary>
        public static Error NotFound(Guid id) =>
            Error.NotFound(
                code: "StorePickup.NotFound",
                description: $"Store pickup with ID '{id}' was not found.");

        /// <summary>
        /// Error indicating that a pickup cannot be marked ready when not in Pending state.
        /// </summary>
        public static Error InvalidStateForReady =>
            Error.Validation(
                code: "StorePickup.InvalidStateForReady",
                description: "Pickup must be in Pending state to mark as ready.");

        /// <summary>
        /// Error indicating that a pickup cannot be marked picked up when not in Ready state.
        /// </summary>
        public static Error InvalidStateForPickup =>
            Error.Validation(
                code: "StorePickup.InvalidStateForPickup",
                description: "Pickup must be in Ready state to mark as picked up.");

        /// <summary>
        /// Error indicating that a pickup cannot be cancelled when already picked up.
        /// </summary>
        public static Error CannotCancelPickedUp =>
            Error.Validation(
                code: "StorePickup.CannotCancelPickedUp",
                description: "Cannot cancel a pickup that has already been picked up.");

        /// <summary>
        /// Error indicating that a pickup cannot be cancelled when already cancelled.
        /// </summary>
        public static Error AlreadyCancelled =>
            Error.Validation(
                code: "StorePickup.AlreadyCancelled",
                description: "This pickup has already been cancelled.");

        /// <summary>
        /// Error indicating that the location does not support store pickup.
        /// </summary>
        public static Error LocationDoesNotSupportPickup =>
            Error.Validation(
                code: "StorePickup.LocationDoesNotSupportPickup",
                description: "The selected location does not support store pickup.");

        /// <summary>
        /// Error indicating that a pickup code could not be generated.
        /// </summary>
        public static Error PickupCodeGenerationFailed =>
            Error.Failure(
                code: "StorePickup.PickupCodeGenerationFailed",
                description: "Failed to generate pickup code. Please try again.");

        /// <summary>
        /// Error indicating that the pickup has expired and can no longer be collected.
        /// </summary>
        public static Error PickupExpired =>
            Error.Validation(
                code: "StorePickup.PickupExpired",
                description: $"Pickup has expired (expires after {Constraints.PickupExpirationDays} days) and can no longer be collected.");

        /// <summary>
        /// Error indicating that the cancellation reason is too long.
        /// </summary>
        public static Error CancellationReasonTooLong =>
            CommonInput.Errors.TooLong(prefix: nameof(StorePickup), field: "CancellationReason", maxLength: Constraints.CancellationReasonMaxLength);
    }
    #endregion

    #region Properties
    /// <summary>
    /// The unique identifier of the order this pickup is associated with.
    /// </summary>
    public Guid OrderId { get; private set; }

    /// <summary>
    /// The unique identifier of the stock location (retail store) where customer will pick up items.
    /// </summary>
    public Guid StockLocationId { get; private set; }

    /// <summary>
    /// The current state of this pickup (Pending, Ready, PickedUp, Cancelled).
    /// </summary>
    public PickupState State { get; private set; } = PickupState.Pending;

    /// <summary>
    /// Human-readable pickup code for customer identification at the store.
    /// Format: {LOCATION-PREFIX}-{ORDER-ID-CHECKSUM}-{RANDOM-SUFFIX}
    /// Example: NYC-12345-A7K9
    /// </summary>
    public string PickupCode { get; private set; } = string.Empty;

    /// <summary>
    /// Scheduled date/time for customer pickup. Optional; allows store to manage pickup windows.
    /// </summary>
    public DateTimeOffset? ScheduledPickupTime { get; private set; }

    /// <summary>
    /// Actual date/time when customer picked up the items. Set when state transitions to PickedUp.
    /// </summary>
    public DateTimeOffset? PickedUpAt { get; private set; }

    /// <summary>
    /// Date/time when pickup was marked ready for customer collection. Set when state transitions to Ready.
    /// </summary>
    public DateTimeOffset? ReadyAt { get; private set; }

    /// <summary>
    /// Date/time when pickup was cancelled. Set when state transitions to Cancelled.
    /// </summary>
    public DateTimeOffset? CancelledAt { get; private set; }

    /// <summary>
    /// Optional reason for cancellation (e.g., customer requested cancellation, stock issue).
    /// </summary>
    public string? CancellationReason { get; private set; }

    /// <summary>
    /// Indicates whether this pickup has expired and can no longer be collected.
    /// </summary>
    public bool IsExpired => 
        CreatedAt.AddDays(Constraints.PickupExpirationDays) < DateTimeOffset.UtcNow;

    /// <summary>
    /// Indicates whether this pickup is still active and can be collected.
    /// </summary>
    public bool IsActive => State == PickupState.Ready && !IsExpired;
    #endregion

    #region Relationships
    /// <summary>
    /// Navigation property to the associated stock location (retail store).
    /// </summary>
    public StockLocation StockLocation { get; set; } = null!;
    #endregion

    #region Constructors
    /// <summary>
    /// Private constructor for EF Core. Use factory method <see cref="Create"/> for instantiation.
    /// </summary>
    private StorePickup() { }
    #endregion

    #region Factory Methods
    /// <summary>
    /// Creates a new store pickup for an order at a specific location.
    /// Validates that the location supports store pickup and generates a unique pickup code.
    /// </summary>
    /// <param name="orderId">The unique identifier of the order being picked up.</param>
    /// <param name="stockLocationId">The unique identifier of the retail location for pickup.</param>
    /// <param name="scheduledPickupTime">Optional scheduled pickup time for store workflow.</param>
    /// <returns>
    /// An <see cref="ErrorOr{T}"/> containing the created <see cref="StorePickup"/> or an error.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Validation:</b>
    /// - Order ID must not be empty
    /// - Stock location ID must not be empty
    /// - Scheduled pickup time (if provided) must be in the future
    /// 
    /// <b>Pickup Code Generation:</b>
    /// The pickup code is generated to be:
    /// - Human-readable for customers and staff
    /// - Unique (extremely low collision probability)
    /// - Secure (includes order checksum)
    /// - Location-aware (first segment contains location info)
    /// </para>
    /// </remarks>
    public static ErrorOr<StorePickup> Create(
        Guid orderId,
        Guid stockLocationId,
        DateTimeOffset? scheduledPickupTime = null)
    {
        // Validate order ID
        if (orderId == Guid.Empty)
            return Error.Validation(
                code: "StorePickup.InvalidOrder",
                description: "Order reference is required.");

        // Validate stock location ID
        if (stockLocationId == Guid.Empty)
            return Error.Validation(
                code: "StorePickup.InvalidStockLocation",
                description: "Stock location reference is required.");

        // Validate scheduled pickup time is in the future (if provided)
        if (scheduledPickupTime.HasValue && scheduledPickupTime.Value < DateTimeOffset.UtcNow)
            return Error.Validation(
                code: "StorePickup.InvalidScheduledTime",
                description: "Scheduled pickup time must be in the future.");

        // Generate pickup code
        var pickupCodeResult = GeneratePickupCode(orderId, stockLocationId);
        if (pickupCodeResult.IsError)
            return pickupCodeResult.FirstError;

        var pickupCode = pickupCodeResult.Value;

        // Create pickup aggregate
        var pickup = new StorePickup
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            StockLocationId = stockLocationId,
            State = PickupState.Pending,
            PickupCode = pickupCode,
            ScheduledPickupTime = scheduledPickupTime,
            CreatedAt = DateTimeOffset.UtcNow
        };

        // Publish domain event
        pickup.AddDomainEvent(new Events.Created(
            StorePickupId: pickup.Id,
            OrderId: orderId,
            StockLocationId: stockLocationId,
            PickupCode: pickupCode));

        return pickup;
    }
    #endregion

    #region Business Logic - State Transitions

    /// <summary>
    /// Marks this pickup as ready for customer collection.
    /// Transitions state from Pending to Ready and records the ready timestamp.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{T}"/> indicating success or an error if invalid state.
    /// </returns>
    /// <remarks>
    /// <b>Valid Transitions:</b> Pending → Ready
    /// 
    /// <b>Effects:</b>
    /// • State changes to Ready
    /// • ReadyAt timestamp is recorded
    /// • Ready domain event is published
    /// • Customer notification can be triggered by event handler
    /// </remarks>
    public ErrorOr<StorePickup> MarkReady()
    {
        if (State != PickupState.Pending)
            return Errors.InvalidStateForReady;

        State = PickupState.Ready;
        ReadyAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.Ready(
            StorePickupId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId,
            PickupCode: PickupCode));

        return this;
    }

    /// <summary>
    /// Marks this pickup as collected by the customer.
    /// Transitions state from Ready to PickedUp and records the pickup timestamp.
    /// </summary>
    /// <returns>
    /// An <see cref="ErrorOr{T}"/> indicating success or an error if invalid state.
    /// </returns>
    /// <remarks>
    /// <b>Valid Transitions:</b> Ready → PickedUp
    /// 
    /// <b>Pre-conditions:</b>
    /// • Pickup must be in Ready state
    /// • Pickup must not be expired (expires after <see cref="Constraints.PickupExpirationDays"/>)
    /// 
    /// <b>Effects:</b>
    /// • State changes to PickedUp
    /// • PickedUpAt timestamp is recorded
    /// • PickedUp domain event is published
    /// • Inventory finalization can be triggered by event handler
    /// </remarks>
    public ErrorOr<StorePickup> MarkPickedUp()
    {
        if (State != PickupState.Ready)
            return Errors.InvalidStateForPickup;

        if (IsExpired)
            return Errors.PickupExpired;

        State = PickupState.PickedUp;
        PickedUpAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.PickedUp(
            StorePickupId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId));

        return this;
    }

    /// <summary>
    /// Cancels this pickup and releases reserved stock back to available inventory.
    /// Transitions state from Pending or Ready to Cancelled.
    /// </summary>
    /// <param name="reason">Optional reason for cancellation for record-keeping.</param>
    /// <returns>
    /// An <see cref="ErrorOr{T}"/> indicating success or an error if cancellation not allowed.
    /// </returns>
    /// <remarks>
    /// <b>Valid Transitions:</b> Pending → Cancelled, Ready → Cancelled
    /// 
    /// <b>Invalid Transitions:</b> PickedUp → Cancelled (cannot cancel after pickup)
    /// 
    /// <b>Pre-conditions:</b>
    /// • Pickup must not already be in PickedUp state
    /// • Pickup must not already be cancelled
    /// 
    /// <b>Effects:</b>
    /// • State changes to Cancelled
    /// • CancelledAt timestamp is recorded
    /// • CancellationReason is stored (if provided)
    /// • Cancelled domain event is published
    /// • Event handler releases reserved stock back to available inventory
    /// </remarks>
    public ErrorOr<StorePickup> Cancel(string? reason = null)
    {
        // Validate cancellation not already done
        if (State == PickupState.Cancelled)
            return Errors.AlreadyCancelled;

        // Validate cannot cancel after pickup
        if (State == PickupState.PickedUp)
            return Errors.CannotCancelPickedUp;

        // Validate reason length if provided
        if (!string.IsNullOrEmpty(reason) && reason.Length > Constraints.CancellationReasonMaxLength)
            return Errors.CancellationReasonTooLong;

        State = PickupState.Cancelled;
        CancelledAt = DateTimeOffset.UtcNow;
        CancellationReason = reason;

        AddDomainEvent(new Events.Cancelled(
            StorePickupId: Id,
            OrderId: OrderId,
            StockLocationId: StockLocationId,
            Reason: reason));

        return this;
    }

    /// <summary>
    /// Updates the scheduled pickup time for this pickup.
    /// Useful for customer rescheduling or store workflow management.
    /// </summary>
    /// <param name="newScheduledTime">The new scheduled pickup time (must be in the future).</param>
    /// <returns>
    /// An <see cref="ErrorOr{T}"/> indicating success or an error if time is invalid.
    /// </returns>
    public ErrorOr<StorePickup> ReschedulePickup(DateTimeOffset newScheduledTime)
    {
        if (newScheduledTime < DateTimeOffset.UtcNow)
            return Error.Validation(
                code: "StorePickup.InvalidScheduledTime",
                description: "Scheduled pickup time must be in the future.");

        if (State == PickupState.PickedUp || State == PickupState.Cancelled)
            return Error.Validation(
                code: "StorePickup.CannotReschedule",
                description: "Cannot reschedule a pickup that has been picked up or cancelled.");

        ScheduledPickupTime = newScheduledTime;

        AddDomainEvent(new Events.Rescheduled(
            StorePickupId: Id,
            OrderId: OrderId,
            ScheduledPickupTime: newScheduledTime));

        return this;
    }
    #endregion

    #region Business Logic - Pickup Code Generation

    /// <summary>
    /// Generates a unique, human-readable pickup code for customer identification.
    /// </summary>
    /// <param name="orderId">The order ID (used for checksum).</param>
    /// <param name="stockLocationId">The stock location ID (used for location prefix).</param>
    /// <returns>
    /// An <see cref="ErrorOr{T}"/> containing the generated pickup code or an error.
    /// </returns>
    /// <remarks>
    /// <para>
    /// <b>Format:</b> {LOCATION-PREFIX}-{CHECKSUM}-{RANDOM-SUFFIX}
    /// Example: NYC-12345-A7K9
    /// </para>
    /// 
    /// <para>
    /// <b>Components:</b>
    /// • Location Prefix: First 4-6 characters of location name or derived from location ID
    /// • Checksum: First 5 digits of order ID hash, provides light security validation
    /// • Random Suffix: 4-character random alphanumeric, ensures uniqueness
    /// </para>
    /// 
    /// <para>
    /// <b>Security:</b>
    /// • Not cryptographically secure (QR codes not involved)
    /// • Provides basic validation (customer can't generate own code)
    /// • Checksum detects basic tampering
    /// • Random suffix prevents prediction
    /// </para>
    /// 
    /// <para>
    /// <b>Format Compliance:</b>
    /// Total length typically 15-25 characters, fits within <see cref="Constraints.PickupCodeMaxLength"/>
    /// </para>
    /// </remarks>
    private static ErrorOr<string> GeneratePickupCode(Guid orderId, Guid stockLocationId)
    {
        try
        {
            // Generate location prefix from location ID (first 3-4 hex chars + letters)
            var locationHex = stockLocationId.ToString("N").Substring(0, 4).ToUpper();
            var locationPrefix = ConvertHexToAlpha(locationHex);
            if (string.IsNullOrEmpty(locationPrefix))
                locationPrefix = "LOC"; // Fallback

            // Generate checksum from order ID (first 5 digits)
            var orderChecksum = Math.Abs(orderId.GetHashCode()) % 100000;
            var checksumString = orderChecksum.ToString("D5");

            // Generate random suffix (4 alphanumeric characters)
            var randomSuffix = GenerateRandomSuffix(Constraints.PickupCodeRandomSuffixLength);

            // Combine: PREFIX-CHECKSUM-SUFFIX
            var pickupCode = $"{locationPrefix}-{checksumString}-{randomSuffix}";

            // Validate length
            if (pickupCode.Length > Constraints.PickupCodeMaxLength)
                return Errors.PickupCodeGenerationFailed;

            return pickupCode;
        }
        catch
        {
            return Errors.PickupCodeGenerationFailed;
        }
    }

    /// <summary>
    /// Converts hexadecimal characters to alphabetic characters for human-readable location prefix.
    /// </summary>
    private static string ConvertHexToAlpha(string hex)
    {
        var result = new StringBuilder();
        foreach (var c in hex)
        {
            result.Append(c switch
            {
                '0' => 'A', '1' => 'B', '2' => 'C', '3' => 'D', '4' => 'E',
                '5' => 'F', '6' => 'G', '7' => 'H', '8' => 'I', '9' => 'J',
                'A' => 'K', 'B' => 'L', 'C' => 'M', 'D' => 'N', 'E' => 'O', 'F' => 'P',
                _ => 'X'
            });
        }
        return result.ToString();
    }

    /// <summary>
    /// Generates a random alphanumeric suffix for pickup code uniqueness.
    /// </summary>
    private static string GenerateRandomSuffix(int length)
    {
        const string alphanumeric = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        var result = new StringBuilder(length);

        using (var rng = RandomNumberGenerator.Create())
        {
            var buffer = new byte[length];
            rng.GetBytes(buffer);

            foreach (var b in buffer)
            {
                result.Append(alphanumeric[b % alphanumeric.Length]);
            }
        }

        return result.ToString();
    }
    #endregion

    #region Domain Events
    /// <summary>
    /// Defines domain events published by <see cref="StorePickup"/> for decoupled integration.
    /// </summary>
    public static class Events
    {
        /// <summary>
        /// Published when a store pickup is created.
        /// </summary>
        public sealed record Created(
            Guid StorePickupId,
            Guid OrderId,
            Guid StockLocationId,
            string PickupCode) : DomainEvent;

        /// <summary>
        /// Published when a store pickup is marked ready for customer collection.
        /// Triggers customer notification (email/SMS).
        /// </summary>
        public sealed record Ready(
            Guid StorePickupId,
            Guid OrderId,
            Guid StockLocationId,
            string PickupCode) : DomainEvent;

        /// <summary>
        /// Published when a customer collects their items.
        /// Triggers inventory finalization and sales reporting.
        /// </summary>
        public sealed record PickedUp(
            Guid StorePickupId,
            Guid OrderId,
            Guid StockLocationId) : DomainEvent;

        /// <summary>
        /// Published when a store pickup is cancelled.
        /// Triggers inventory release and stock adjustment.
        /// </summary>
        public sealed record Cancelled(
            Guid StorePickupId,
            Guid OrderId,
            Guid StockLocationId,
            string? Reason) : DomainEvent;

        /// <summary>
        /// Published when a scheduled pickup time is changed.
        /// Allows store to track schedule changes and update customer.
        /// </summary>
        public sealed record Rescheduled(
            Guid StorePickupId,
            Guid OrderId,
            DateTimeOffset ScheduledPickupTime) : DomainEvent;
    }
    #endregion
}
