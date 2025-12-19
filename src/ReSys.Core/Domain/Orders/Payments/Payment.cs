using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Settings.PaymentMethods;

namespace ReSys.Core.Domain.Orders.Payments;

public sealed class Payment : Aggregate
{
    public enum PaymentState
    {
        Pending = 0,           // Created, not yet sent to gateway
        Authorizing = 1,       // Sent to gateway for auth
        Authorized = 2,        // Auth hold placed (NOT captured)
        Capturing = 3,         // Attempting capture
        Completed = 4,         // Funds captured
        //PartiallyRefunded = 5, // Some amount refunded
        //Refunded = 6,          // Fully refunded
        Failed = 7,            // Authorization/capture failed
        Void = 8               // Cancelled (auth released)
    }

    #region Constraints
    public static class Constraints
    {
        public const long AmountCentsMinValue = 0;
        public const int CurrencyMaxLength = CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength;
        public const int PaymentMethodTypeMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
        public const int ReferenceTransactionIdMaxLength = 100; // Reference to original transaction (e.g., from gateway)
        public const int GatewayAuthCodeMaxLength = 50;
        public const int GatewayErrorCodeMaxLength = 100;
        public const int FailureReasonMaxLength = CommonInput.Constraints.Text.LongTextMaxLength;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error AlreadyCaptured => Error.Validation(code: "Payment.AlreadyCaptured", description: "Payment already captured.");
        public static Error CannotVoidCaptured => Error.Validation(code: "Payment.CannotVoidCaptured", description: "Cannot void captured or completed payment.");
        //public static Error CannotRefundNonCompleted => Error.Validation(code: "Payment.CannotRefundNonCompleted", description: "Can only refund completed payments.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "Payment.NotFound", description: $"Payment with ID '{id}' was not found.");
        public static Error InvalidAmountCents => Error.Validation(code: "Payment.InvalidAmountCents", description: $"Amount cents must be at least {Constraints.AmountCentsMinValue}.");
        public static Error CurrencyRequired => CommonInput.Errors.Required(prefix: nameof(Payment), field: nameof(Currency));
        public static Error CurrencyTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(Currency), maxLength: Constraints.CurrencyMaxLength);
        public static Error PaymentMethodTypeRequired => CommonInput.Errors.Required(prefix: nameof(Payment), field: nameof(PaymentMethodType));
        public static Error PaymentMethodTypeTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(PaymentMethodType), maxLength: Constraints.PaymentMethodTypeMaxLength);
        public static Error ReferenceTransactionIdRequired => CommonInput.Errors.Required(prefix: nameof(Payment), field: nameof(ReferenceTransactionId));
        public static Error ReferenceTransactionIdTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(ReferenceTransactionId), maxLength: Constraints.ReferenceTransactionIdMaxLength);
        public static Error GatewayAuthCodeTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(GatewayAuthCode), maxLength: Constraints.GatewayAuthCodeMaxLength);
        public static Error GatewayErrorCodeTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(GatewayErrorCode), maxLength: Constraints.GatewayErrorCodeMaxLength);
        public static Error FailureReasonTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(FailureReason), maxLength: Constraints.FailureReasonMaxLength);
        public static Error IdempotencyKeyConflict => Error.Conflict(code: "Payment.IdempotencyKeyConflict", description: "Payment operation with this idempotency key already exists with a different state or parameters.");
        public static Error InvalidStateTransition(PaymentState from, PaymentState to) => Error.Validation(code: "Payment.InvalidStateTransition", description: $"Cannot transition from {from} to {to}.");
        public static Error AuthorizationRequired => Error.Validation(code: "Payment.AuthorizationRequired", description: "Payment must be authorized before capture.");
        //public static Error PartialRefundExceedsAmount(decimal requested, decimal available) => Error.Validation(code: "Payment.PartialRefundExceedsAmount", description: $"Requested refund amount {requested:C} exceeds available amount {available:C}.");
        //public static Error PaymentAlreadyRefunded => Error.Conflict(code: "Payment.PaymentAlreadyRefunded", description: "Payment has already been fully refunded.");
        //public static Error PaymentNotRefunded => Error.Validation(code: "Payment.PaymentNotRefunded", description: "Payment must be refunded before attempting to refund again.");
    }
    #endregion

    #region Properties
    public Guid OrderId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public decimal AmountCents { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentState State { get; set; } = PaymentState.Pending;
    public string PaymentMethodType { get; set; } = string.Empty;

    public string? ReferenceTransactionId { get; set; } // Reference to original transaction ID (gateway response)
    public string? GatewayAuthCode { get; set; } // Authorization code from gateway
    public string? GatewayErrorCode { get; set; } // Error code from gateway

    public DateTimeOffset? AuthorizedAt { get; set; }
    public DateTimeOffset? CapturedAt { get; set; }
    //public decimal RefundedAmountCents { get; set; } // Tracks total refunded amount
    public DateTimeOffset? VoidedAt { get; set; }
    //public DateTimeOffset? RefundedAt { get; set; } // Last refund timestamp
    public string? FailureReason { get; set; }
    public string? IdempotencyKey { get; set; }
    #endregion

    #region Relationships
    public Order Order { get; set; } = null!;
    public PaymentMethod? PaymentMethod { get; set; }
    #endregion
    #region Computed Properties
    public bool IsPending => State == PaymentState.Pending;
    public bool IsAuthorizing => State == PaymentState.Authorizing;
    public bool IsAuthorized => State == PaymentState.Authorized;
    public bool IsCapturing => State == PaymentState.Capturing;
    public bool IsCompleted => State == PaymentState.Completed;
    //public bool IsPartiallyRefunded => State == PaymentState.PartiallyRefunded;
    //public bool IsRefunded => State == PaymentState.Refunded; // Can be fully or partially
    public bool IsVoid => State == PaymentState.Void;
    public bool IsFailed => State == PaymentState.Failed;
    public decimal Amount => AmountCents / 100m;
    //public decimal TotalRefundedAmount => RefundedAmountCents / 100m;
    //public decimal AvailableForRefund => AmountCents - RefundedAmountCents;
    #endregion
    #region Constructors
    private Payment() { }
    #endregion

    #region Factory Methods
    public static ErrorOr<Payment> Create(Guid orderId, decimal amountCents, string currency, string paymentMethodType, Guid paymentMethodId, string? idempotencyKey = null)
    {
        if (amountCents < Constraints.AmountCentsMinValue) return Errors.InvalidAmountCents;
        if (string.IsNullOrWhiteSpace(value: currency)) return Errors.CurrencyRequired;
        if (currency.Length > Constraints.CurrencyMaxLength) return Errors.CurrencyTooLong;
        if (string.IsNullOrWhiteSpace(value: paymentMethodType)) return Errors.PaymentMethodTypeRequired;
        if (paymentMethodType.Length > Constraints.PaymentMethodTypeMaxLength) return Errors.PaymentMethodTypeTooLong;

        var payment = new Payment
        {
            Id = Guid.NewGuid(),
            OrderId = orderId,
            PaymentMethodId = paymentMethodId,
            AmountCents = amountCents,
            Currency = currency,
            State = PaymentState.Pending,
            PaymentMethodType = paymentMethodType,
            IdempotencyKey = idempotencyKey,
            CreatedAt = DateTimeOffset.UtcNow,
            //RefundedAmountCents = 0
        };

        payment.AddDomainEvent(domainEvent: new Events.PaymentCreated(PaymentId: payment.Id, OrderId: orderId, IdempotencyKey: idempotencyKey));

        return payment;
    }
    #endregion

    #region Business Logic - Authorize

    /// <summary>
    /// Marks the payment as authorized, setting the transaction ID and state.
    /// </summary>
    /// <param name="transactionId">The transaction ID provided by the payment gateway.</param>
    /// <param name="gatewayAuthCode">Optional authorization code from the gateway.</param>
    public ErrorOr<Payment> MarkAsAuthorized(string transactionId, string? gatewayAuthCode = null)
    {
        if (State == PaymentState.Authorized) return this; // Idempotent
        if (State != PaymentState.Pending && State != PaymentState.Authorizing)
            return Errors.InvalidStateTransition(State, PaymentState.Authorized);

        ReferenceTransactionId = transactionId;
        GatewayAuthCode = gatewayAuthCode;
        State = PaymentState.Authorized;
        AuthorizedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.PaymentAuthorized(Id, OrderId, transactionId));
        return this;
    }

    #endregion

    #region Business Logic - Capture

    /// <summary>
    /// Marks the payment as captured (completed), setting the transaction ID and state.
    /// </summary>
    /// <param name="transactionId">The transaction ID provided by the payment gateway for the capture. If null, it will use the existing ReferenceTransactionId.</param>
    public ErrorOr<Payment> MarkAsCaptured(string? transactionId = null)
    {
        if (State == PaymentState.Completed) return this; // Idempotent
        if (State != PaymentState.Authorized && State != PaymentState.Capturing)
            return Errors.InvalidStateTransition(State, PaymentState.Completed);

        if (string.IsNullOrEmpty(transactionId))
            return Errors.ReferenceTransactionIdRequired;

        ReferenceTransactionId = transactionId ?? ReferenceTransactionId;
        State = PaymentState.Completed;
        CapturedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.PaymentCaptured(Id, OrderId, ReferenceTransactionId!));
        return this;
    }

    #endregion

    #region Business Logic - Void
    /// <summary>
    /// Voids an authorized (but not yet captured) payment.
    /// </summary>
    /// <returns>Updated result or error.</returns>
    public ErrorOr<Updated> Void()
    {
        if (State == PaymentState.Void) return Result.Updated; // Idempotent
        if (State == PaymentState.Completed) return Errors.CannotVoidCaptured;

        State = PaymentState.Void;
        VoidedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(new Events.PaymentVoided(PaymentId: Id, OrderId: OrderId, ReferenceTransactionId: ReferenceTransactionId));
        return Result.Updated;
    }
    #endregion

    #region Business Logic - Refund
    ///// <summary>
    ///// Refunds a portion or all of a captured payment.
    ///// </summary>
    ///// <param name="amountCents">The amount to refund in cents.</param>
    ///// <param name="reason">The reason for the refund.</param>
    ///// <param name="idempotencyKey">Optional idempotency key for retries.</param>
    ///// <returns>Updated result or error.</returns>
    //public ErrorOr<Updated> Refund(decimal amountCents, string reason, string? idempotencyKey = null)
    //{
    //    // Idempotency check:
    //    if (!string.IsNullOrEmpty(idempotencyKey) && IdempotencyKey != idempotencyKey)
    //    {
    //        return Errors.IdempotencyKeyConflict;
    //    }

    //    if (State == PaymentState.Refunded) return Result.Updated; // Idempotent for full refund
    //    if (State != PaymentState.Completed && State != PaymentState.PartiallyRefunded) return Errors.CannotRefundNonCompleted;
    //    if (amountCents <= 0) return Errors.InvalidAmountCents;

    //    if (RefundedAmountCents + amountCents > AmountCents)
    //    {
    //        return Errors.PartialRefundExceedsAmount(amountCents / 100m, (AmountCents - RefundedAmountCents) / 100m);
    //    }

    //    RefundedAmountCents += amountCents;
    //    RefundedAt = DateTimeOffset.UtcNow;
    //    IdempotencyKey = idempotencyKey;
    //    UpdatedAt = DateTimeOffset.UtcNow;

    //    if (RefundedAmountCents == AmountCents)
    //    {
    //        State = PaymentState.Refunded;
    //        AddDomainEvent(new Events.PaymentRefunded(Id, OrderId, amountCents, ReferenceTransactionId, reason));
    //    }
    //    else
    //    {
    //        State = PaymentState.PartiallyRefunded;
    //        AddDomainEvent(new Events.PaymentPartiallyRefunded(Id, OrderId, amountCents, ReferenceTransactionId, reason));
    //    }

    //    return Result.Updated;
    //}
    #endregion

    #region Business Logic - Mark as Pending

    /// <summary>
    /// Marks the payment as pending, optionally providing an error message.
    /// This is typically used when a payment requires further action or its status is unknown.
    /// </summary>
    /// <param name="errorMessage">Optional message indicating why the payment is pending.</param>
    public ErrorOr<Payment> MarkAsPending(string? errorMessage = null)
    {
        if (State == PaymentState.Pending) return this; // Idempotent

        State = PaymentState.Pending;
        FailureReason = errorMessage;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.PaymentPending(Id, OrderId, errorMessage ?? "Payment requires action."));
        return this;
    }

    #endregion

    #region Business Logic - Mark as Failed

    /// <summary>
    /// Marks the payment as failed, optionally providing an error message and gateway error code.
    /// </summary>
    /// <param name="errorMessage">Optional message indicating why the payment failed.</param>
    /// <param name="gatewayErrorCode">Optional error code from the payment gateway.</param>
    public ErrorOr<Payment> MarkAsFailed(string errorMessage, string? gatewayErrorCode = null)
    {
        if (State == PaymentState.Failed) return this; // Idempotent

        if (errorMessage.Length > Constraints.FailureReasonMaxLength)
            return Errors.FailureReasonTooLong;
        if (!string.IsNullOrEmpty(gatewayErrorCode) && gatewayErrorCode.Length > Constraints.GatewayErrorCodeMaxLength)
            return Errors.GatewayErrorCodeTooLong;

        State = PaymentState.Failed;
        FailureReason = errorMessage;
        GatewayErrorCode = gatewayErrorCode;
        UpdatedAt = DateTimeOffset.UtcNow;

        AddDomainEvent(new Events.PaymentFailed(Id, OrderId, errorMessage, gatewayErrorCode));
        return this;
    }

    #endregion

    #region Events
    public static class Events
    {
        public sealed record PaymentCreated(Guid PaymentId, Guid OrderId, Guid? StoreId = null, string? IdempotencyKey = null) : DomainEvent;
        public sealed record PaymentAuthorized(Guid PaymentId, Guid OrderId, string ReferenceTransactionId) : DomainEvent;
        public sealed record PaymentCapturing(Guid PaymentId, Guid OrderId) : DomainEvent;
        public sealed record PaymentCaptured(Guid PaymentId, Guid OrderId, string ReferenceTransactionId) : DomainEvent;
        public sealed record PaymentVoided(Guid PaymentId, Guid OrderId, string? ReferenceTransactionId) : DomainEvent;
        //public sealed record PaymentPartiallyRefunded(Guid PaymentId, Guid OrderId, decimal RefundAmountCents, string? ReferenceTransactionId, string Reason) : DomainEvent;
        //public sealed record PaymentRefunded(Guid PaymentId, Guid OrderId, decimal RefundAmountCents, string? ReferenceTransactionId, string Reason) : DomainEvent; // Full refund
        public sealed record PaymentFailed(Guid PaymentId, Guid OrderId, string ErrorMessage, string? GatewayErrorCode) : DomainEvent;
        public sealed record PaymentPending(Guid PaymentId, Guid OrderId, string ErrorMessage) : DomainEvent;
    }
    #endregion
}