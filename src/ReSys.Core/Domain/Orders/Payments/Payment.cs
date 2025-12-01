using ErrorOr;

using ReSys.Core.Common.Constants;
using ReSys.Core.Common.Domain.Entities;
using ReSys.Core.Common.Domain.Events;
using ReSys.Core.Domain.Payments;

namespace ReSys.Core.Domain.Orders.Payments;

public sealed class Payment : Aggregate
{
    public enum PaymentState { Pending = 0, Processing = 1, Completed = 2, Failed = 3, Void = 4, Refunded = 5 }

    #region Constraints
    public static class Constraints
    {
        public const long AmountCentsMinValue = 0;
        public const int CurrencyMaxLength = CommonInput.Constraints.CurrencyAndLanguage.CurrencyCodeLength;
        public const int PaymentMethodTypeMaxLength = CommonInput.Constraints.NamesAndUsernames.NameMaxLength;
        public const int TransactionIdMaxLength = 100;
        public const int FailureReasonMaxLength = CommonInput.Constraints.Text.LongTextMaxLength;
    }
    #endregion

    #region Errors
    public static class Errors
    {
        public static Error AlreadyCaptured => Error.Validation(code: "Payment.AlreadyCaptured", description: "Payment already captured.");
        public static Error CannotVoidCaptured => Error.Validation(code: "Payment.CannotVoidCaptured", description: "Cannot void captured payment.");
        public static Error CannotRefundNonCompleted => Error.Validation(code: "Payment.CannotRefundNonCompleted", description: "Can only refund completed payments.");
        public static Error NotFound(Guid id) => Error.NotFound(code: "Payment.NotFound", description: $"Payment with ID '{id}' was not found.");
        public static Error InvalidAmountCents => Error.Validation(code: "Payment.InvalidAmountCents", description: $"Amount cents must be at least {Constraints.AmountCentsMinValue}.");
        public static Error CurrencyRequired => CommonInput.Errors.Required(prefix: nameof(Payment), field: nameof(Currency));
        public static Error CurrencyTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(Currency), maxLength: Constraints.CurrencyMaxLength);
        public static Error PaymentMethodTypeRequired => CommonInput.Errors.Required(prefix: nameof(Payment), field: nameof(PaymentMethodType));
        public static Error PaymentMethodTypeTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(PaymentMethodType), maxLength: Constraints.PaymentMethodTypeMaxLength);
        public static Error TransactionIdRequired => CommonInput.Errors.Required(prefix: nameof(Payment), field: nameof(TransactionId));
        public static Error TransactionIdTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(TransactionId), maxLength: Constraints.TransactionIdMaxLength);
        public static Error FailureReasonTooLong => CommonInput.Errors.TooLong(prefix: nameof(Payment), field: nameof(FailureReason), maxLength: Constraints.FailureReasonMaxLength);
    }
    #endregion

    #region Properties
    public Guid OrderId { get; set; }
    public Guid? PaymentMethodId { get; set; }
    public long AmountCents { get; set; }
    public string Currency { get; set; } = string.Empty;
    public PaymentState State { get; set; } = PaymentState.Pending;
    public string PaymentMethodType { get; set; } = string.Empty;
    public string? TransactionId { get; set; }
    public DateTimeOffset? CapturedAt { get; set; }
    public DateTimeOffset? VoidedAt { get; set; }
    public DateTimeOffset? RefundedAt { get; set; }
    public string? FailureReason { get; set; }
    #endregion

    #region Relationships
    public Order Order { get; set; } = null!;
    public PaymentMethod? PaymentMethod { get; set; }
    #endregion
    #region Computed Properties
    public bool IsCompleted => State == PaymentState.Completed;
    public bool IsPending => State == PaymentState.Pending;
    public bool IsVoid => State == PaymentState.Void;
    public bool IsRefunded => State == PaymentState.Refunded;
    public bool IsFailed => State == PaymentState.Failed;
    public decimal Amount => AmountCents / 100m;
    #endregion
    #region Constructors
    private Payment() { }
    #endregion

    #region Factory Methods
    public static ErrorOr<Payment> Create(Guid orderId, long amountCents, string currency, string paymentMethodType, Guid paymentMethodId)
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
            CreatedAt = DateTimeOffset.UtcNow
        };

        payment.AddDomainEvent(domainEvent: new Events.PaymentCreated(PaymentId: payment.Id, OrderId: orderId));

        return payment;
    }
    #endregion

    #region Business Logic
    public ErrorOr<Updated> Capture(string transactionId, Guid? storeId = null)
    {
        if (State == PaymentState.Completed) return Errors.AlreadyCaptured;
        if (State == PaymentState.Void) return Error.Validation(code: "Payment.CannotCaptureVoid", description: "Cannot capture voided payment.");
        if (string.IsNullOrWhiteSpace(value: transactionId)) return Errors.TransactionIdRequired;
        if (transactionId.Length > Constraints.TransactionIdMaxLength) return Errors.TransactionIdTooLong;

        State = PaymentState.Completed;
        CapturedAt = DateTimeOffset.UtcNow;
        TransactionId = transactionId;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PaymentCaptured(PaymentId: Id, OrderId: OrderId, StoreId: storeId));
        return Result.Updated;
    }
    public ErrorOr<Updated> StartProcessing(Guid? storeId = null)
    {
        if (State != PaymentState.Pending)
            return Error.Validation(code: "Payment.NotPending",
                description: "Only pending payments can be processed.");

        if (State == PaymentState.Processing) return Result.Updated;

        State = PaymentState.Processing;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PaymentProcessing(PaymentId: Id, OrderId: OrderId, StoreId: storeId));
        return Result.Updated;
    }

    public ErrorOr<Updated> Void(Guid? storeId = null)
    {
        if (State == PaymentState.Completed) return Errors.CannotVoidCaptured;
        if (State == PaymentState.Void) return Result.Updated;

        State = PaymentState.Void;
        VoidedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PaymentVoided(PaymentId: Id, OrderId: OrderId, StoreId: storeId));
        return Result.Updated;
    }

    public ErrorOr<Updated> Refund(Guid? storeId = null)
    {
        if (State != PaymentState.Completed) return Errors.CannotRefundNonCompleted;
        if (State == PaymentState.Refunded) return Result.Updated;

        State = PaymentState.Refunded;
        RefundedAt = DateTimeOffset.UtcNow;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PaymentRefunded(PaymentId: Id, OrderId: OrderId, StoreId: storeId));
        return Result.Updated;
    }

    public ErrorOr<Updated> MarkAsFailed(string? errorMessage = null, Guid? storeId = null)
    {
        if (errorMessage != null && errorMessage.Length > Constraints.FailureReasonMaxLength) return Errors.FailureReasonTooLong;

        State = PaymentState.Failed;
        FailureReason = errorMessage;
        UpdatedAt = DateTimeOffset.UtcNow;
        AddDomainEvent(domainEvent: new Events.PaymentFailed(PaymentId: Id, OrderId: OrderId, StoreId: storeId, ErrorMessage: errorMessage));
        return Result.Updated;
    }

    #endregion

    #region Events
    public static class Events
    {
        public sealed record PaymentCreated(Guid PaymentId, Guid OrderId, Guid? StoreId = null) : DomainEvent;
        public sealed record PaymentProcessing(Guid PaymentId, Guid OrderId, Guid? StoreId = null) : DomainEvent;
        public sealed record PaymentCaptured(Guid PaymentId, Guid OrderId, Guid? StoreId = null) : DomainEvent;
        public sealed record PaymentVoided(Guid PaymentId, Guid OrderId, Guid? StoreId = null) : DomainEvent;
        public sealed record PaymentRefunded(Guid PaymentId, Guid OrderId, Guid? StoreId = null) : DomainEvent;
        public sealed record PaymentFailed(Guid PaymentId, Guid OrderId, Guid? StoreId = null, string? ErrorMessage = null) : DomainEvent;
    }
    #endregion
}