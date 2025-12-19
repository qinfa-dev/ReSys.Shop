using FluentAssertions;
using ReSys.Core.Domain.Orders.Payments;
// Added for Aggregate

namespace Core.UnitTests.Domain.Orders.Payments;

public class PaymentTests
{
    [Fact]
    public void Create_ShouldReturnPayment_WhenValidInputs()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amountCents = 1000L;
        var currency = "USD";
        var paymentMethodType = "CreditCard";
        var paymentMethodId = Guid.NewGuid();
        var idempotencyKey = Guid.NewGuid().ToString();

        // Act
        var result = Payment.Create(orderId: orderId, amountCents: amountCents, currency: currency, paymentMethodType: paymentMethodType, paymentMethodId: paymentMethodId, idempotencyKey: idempotencyKey);

        // Assert
        result.IsError.Should().BeFalse();
        result.Value.Should().NotBeNull();
        result.Value.OrderId.Should().Be(expected: orderId);
        result.Value.AmountCents.Should().Be(expected: amountCents);
        result.Value.Currency.Should().Be(expected: currency);
        result.Value.PaymentMethodType.Should().Be(expected: paymentMethodType);
        result.Value.PaymentMethodId.Should().Be(expected: paymentMethodId);
        result.Value.State.Should().Be(expected: Payment.PaymentState.Pending);
        result.Value.IdempotencyKey.Should().Be(expected: idempotencyKey);
        //result.Value.RefundedAmountCents.Should().Be(0);
        result.Value.CreatedAt.Should().BeCloseTo(nearbyTime: DateTimeOffset.UtcNow, precision: TimeSpan.FromSeconds(seconds: 5));
        result.Value.DomainEvents.OfType<Payment.Events.PaymentCreated>().Should().ContainSingle(e => e.IdempotencyKey == idempotencyKey);
    }

    [Fact]
    public void Create_ShouldReturnError_WhenInvalidAmountCents()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amountCents = -100L;
        var currency = "USD";
        var paymentMethodType = "CreditCard";
        var paymentMethodId = Guid.NewGuid();

        // Act
        var result = Payment.Create(orderId: orderId, amountCents: amountCents, currency: currency, paymentMethodType: paymentMethodType, paymentMethodId: paymentMethodId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Payment.InvalidAmountCents");
    }

    [Fact]
    public void Create_ShouldReturnError_WhenCurrencyIsNullOrEmpty()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amountCents = 1000L;
        var paymentMethodType = "CreditCard";
        var paymentMethodId = Guid.NewGuid();

        // Act
        var result = Payment.Create(orderId: orderId, amountCents: amountCents, currency: "", paymentMethodType: paymentMethodType, paymentMethodId: paymentMethodId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Payment.Currency.Required");
    }

    [Fact]
    public void Create_ShouldReturnError_WhenPaymentMethodTypeIsNullOrEmpty()
    {
        // Arrange
        var orderId = Guid.NewGuid();
        var amountCents = 1000L;
        var currency = "USD";
        var paymentMethodId = Guid.NewGuid();

        // Act
        var result = Payment.Create(orderId: orderId, amountCents: amountCents, currency: currency, paymentMethodType: "", paymentMethodId: paymentMethodId);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Payment.PaymentMethodType.Required");
    }

    // --- Authorize Method Tests ---
    [Fact]
    public void Authorize_ShouldTransitionToAuthorized_WhenValidInputs()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        var referenceTransactionId = "AUTH123";
        var gatewayAuthCode = "ABCDEFG";

        // Act
        var result = payment.MarkAsAuthorized(referenceTransactionId, gatewayAuthCode);

        // Assert
        result.IsError.Should().BeFalse();
        payment.State.Should().Be(Payment.PaymentState.Authorized);
        payment.ReferenceTransactionId.Should().Be(referenceTransactionId);
        payment.GatewayAuthCode.Should().Be(gatewayAuthCode);
        payment.AuthorizedAt.Should().NotBeNull();
        payment.UpdatedAt.Should().NotBeNull();
        payment.DomainEvents.OfType<Payment.Events.PaymentAuthorized>().Should().ContainSingle();
    }

    [Fact]
    public void Authorize_ShouldReturnError_WhenNotInPendingOrAuthorizingState()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        payment.State = Payment.PaymentState.Completed; // Invalid state

        // Act
        var result = payment.MarkAsAuthorized("AUTH123", "ABCDEFG");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(Payment.Errors.InvalidStateTransition(Payment.PaymentState.Completed, Payment.PaymentState.Authorized).Code);
    }

    [Fact]
    public void Authorize_ShouldBeIdempotent_WhenAlreadyAuthorized()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid(), idempotencyKey).Value;
        payment.MarkAsAuthorized("AUTH123", "ABCDEFG"); // First auth
        payment.ClearDomainEvents();

        // Act
        var result = payment.MarkAsAuthorized("AUTH123_RETRY", "ABCDEFG_RETRY"); // Second auth with same key

        // Assert
        result.IsError.Should().BeFalse();
        payment.State.Should().Be(Payment.PaymentState.Authorized);
        payment.ReferenceTransactionId.Should().Be("AUTH123"); // Should retain original
        payment.GatewayAuthCode.Should().Be("ABCDEFG"); // Should retain original
        payment.DomainEvents.Should().BeEmpty();
    }

    // --- StartCapturing Method Tests ---
    //[Fact]
    //public void StartCapturing_ShouldTransitionToCapturing_WhenAuthorized()
    //{
    //    // Arrange
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
    //    payment = payment.MarkAsAuthorized("AUTH123", "ABCDEFG").Value; // Move to Authorized
    //    payment.ClearDomainEvents();

    //    // Act
    //    //var result = payment.StartCapturing();

    //    // Assert
    //    //result.IsError.Should().BeFalse();
    //    //payment.State.Should().Be(Payment.PaymentState.Capturing);
    //    //payment.UpdatedAt.Should().NotBeNull();
    //    //payment.DomainEvents.OfType<Payment.Events.PaymentCapturing>().Should().ContainSingle();
    //}

    //[Fact]
    //public void StartCapturing_ShouldReturnError_WhenNotAuthorized()
    //{
    //    // Arrange
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
    //    payment.State = Payment.PaymentState.Pending; // Invalid state

    //    // Act
    //    //var result = payment.StartCapturing();

    //    // Assert
    //    //result.IsError.Should().BeTrue();
    //    //result.FirstError.Code.Should().Be(Payment.Errors.InvalidStateTransition(Payment.PaymentState.Pending, Payment.PaymentState.Capturing).Code);
    //}


    // --- Capture Method Tests ---
    [Fact]
    public void Capture_ShouldTransitionToCompleted_WhenAuthorizedAndValidReferenceTransactionId()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid(), idempotencyKey).Value;
        payment = payment.MarkAsAuthorized("AUTH123", "ABCDEFG").Value; // Move to Authorized
        payment.ClearDomainEvents();

        var referenceTransactionId = "CAPTURE456";

        // Act
        var result = payment.MarkAsCaptured(transactionId: referenceTransactionId);

        // Assert
        result.IsError.Should().BeFalse();
        payment.State.Should().Be(Payment.PaymentState.Completed);
        payment.ReferenceTransactionId.Should().Be(referenceTransactionId);
        payment.IdempotencyKey.Should().Be(idempotencyKey);
        payment.CapturedAt.Should().NotBeNull();
        payment.UpdatedAt.Should().NotBeNull();
        payment.DomainEvents.OfType<Payment.Events.PaymentCaptured>().Should().ContainSingle();
    }

    [Fact]
    public void Capture_ShouldBeIdempotent_WhenAlreadyCompleted()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid(), idempotencyKey).Value;
        payment = payment.MarkAsAuthorized("AUTH123", "ABCDEFG").Value;
        payment = payment.MarkAsCaptured("CAPTURE456").Value; // Completed
        payment.ClearDomainEvents();

        // Act
        var result = payment.MarkAsCaptured(transactionId: "CAPTURE_RETRY"); // Retry capture

        // Assert
        result.IsError.Should().BeFalse(); // Should return Success (Updated) due to idempotency
        payment.State.Should().Be(Payment.PaymentState.Completed); // State should remain completed
        payment.DomainEvents.Should().BeEmpty(); // No new events should be raised
    }

    [Fact]
    public void Capture_ShouldReturnError_WhenInvalidReferenceTransactionId()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        payment = payment.MarkAsAuthorized("AUTH123", "ABCDEFG").Value;

        // Act
        var result = payment.MarkAsCaptured(transactionId: ""); // Empty transaction ID

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Payment.ReferenceTransactionId.Required");
    }

    [Fact]
    public void Capture_ShouldReturnError_WhenNotInAuthorizedOrCapturingState()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        payment.State = Payment.PaymentState.Pending; // Invalid state

        // Act
        var result = payment.MarkAsCaptured("CAPTURE123");

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(Payment.Errors.InvalidStateTransition(Payment.PaymentState.Pending, Payment.PaymentState.Completed).Code);
    }

    [Fact]
    public void Capture_ShouldBeIdempotent_WhenSameIdempotencyKeyAndAlreadyCompleted()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid(), idempotencyKey).Value;
        payment = payment.MarkAsAuthorized("AUTH123", "ABCDEFG").Value;
        payment = payment.MarkAsCaptured("CAPTURE123").Value; // First capture
        payment.ClearDomainEvents();

        // Act
        var result = payment.MarkAsCaptured("CAPTURE123_RETRY"); // Second capture with same key

        // Assert
        result.IsError.Should().BeFalse();
        payment.State.Should().Be(Payment.PaymentState.Completed); // State remains completed
        payment.ReferenceTransactionId.Should().Be("CAPTURE123"); // ReferenceTransactionId from first capture
        payment.DomainEvents.Should().BeEmpty(); // No new events should be raised
    }

    //[Fact]
    //public void Capture_ShouldReturnError_WhenDifferentIdempotencyKeyAndAlreadyExists()
    //{
    //    // Arrange
    //    var initialIdempotencyKey = Guid.NewGuid().ToString();
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid(), initialIdempotencyKey).Value;
    //    payment = payment.MarkAsAuthorized("AUTH123", "ABCDEFG").Value;
    //    payment = payment.MarkAsCaptured("CAPTURE123").Value; // First capture
    //    payment.ClearDomainEvents();

    //    var conflictingIdempotencyKey = Guid.NewGuid().ToString(); // Different key

    //    // Act
    //    var result = payment.MarkAsCaptured("CAPTURE456");

    //    // Assert
    //    result.IsError.Should().BeTrue();
    //    result.FirstError.Code.Should().Be(expected: "Payment.IdempotencyKeyConflict");
    //    payment.State.Should().Be(Payment.PaymentState.Completed); // State should not change
    //    payment.ReferenceTransactionId.Should().Be(expected: "CAPTURE123"); // Should retain original transaction ID
    //}


    // --- Void Method Tests ---
    [Fact]
    public void Void_ShouldTransitionToVoid_WhenAuthorizedOrPending()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value; // Pending
        payment.ClearDomainEvents(); // Clear initial created event

        // Act - Void from Pending
        var resultPending = payment.Void();

        // Assert
        resultPending.IsError.Should().BeFalse();
        payment.State.Should().Be(Payment.PaymentState.Void);
        payment.VoidedAt.Should().NotBeNull();
        payment.UpdatedAt.Should().NotBeNull();
        payment.DomainEvents.OfType<Payment.Events.PaymentVoided>().Should().ContainSingle();

        // Arrange - Void from Authorized
        payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        payment = payment.MarkAsAuthorized("AUTHXYZ", "DEF123").Value;
        payment.ClearDomainEvents();

        // Act
        var resultAuthorized = payment.Void();

        // Assert
        resultAuthorized.IsError.Should().BeFalse();
        payment.State.Should().Be(Payment.PaymentState.Void);
        payment.VoidedAt.Should().NotBeNull();
        payment.DomainEvents.OfType<Payment.Events.PaymentVoided>().Should().ContainSingle();
    }

    //[Fact]
    //public void Void_ShouldReturnError_WhenCompletedOrRefunded()
    //{
    //    // Arrange - Completed
    //    var paymentCompleted = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
    //    //paymentCompleted.Authorize("AUTH123", "ABC");
    //    //paymentCompleted.Capture("CAPTURED"); // Completed

    //    // Act
    //    var resultCompleted = paymentCompleted.Void();

    //    // Assert
    //    resultCompleted.IsError.Should().BeTrue();
    //    resultCompleted.FirstError.Code.Should().Be("Payment.CannotVoidCaptured");

    //    // Arrange - Refunded
    //    var paymentRefunded = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
    //    //paymentRefunded.Authorize("AUTH123", "ABC");
    //    //paymentRefunded.Capture("CAPTURED");
    //    //paymentRefunded.Refund(1000L, "Customer requested"); // Refunded

    //    // Act
    //    var resultRefunded = paymentRefunded.Void();

    //    // Assert
    //    resultRefunded.IsError.Should().BeTrue();
    //    resultRefunded.FirstError.Code.Should().Be("Payment.CannotVoidCaptured");
    //}

    // --- Refund Method Tests ---
    //[Fact]
    //public void Refund_ShouldTransitionToRefunded_WhenFullyRefunded()
    //{
    //    // Arrange
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
    //    payment.Authorize("AUTH123", "ABC");
    //    payment.Capture("CAPTURED"); // Completed
    //    payment.ClearDomainEvents();

    //    // Act
    //    var result = payment.Refund(1000L, "Customer changed mind");

    //    // Assert
    //    result.IsError.Should().BeFalse();
    //    payment.State.Should().Be(Payment.PaymentState.Refunded);
    //    payment.RefundedAmountCents.Should().Be(1000L);
    //    payment.RefundedAt.Should().NotBeNull();
    //    payment.UpdatedAt.Should().NotBeNull();
    //    payment.DomainEvents.OfType<Payment.Events.PaymentRefunded>().Should().ContainSingle();
    //}

    //[Fact]
    //public void Refund_ShouldTransitionToPartiallyRefunded_WhenPartialRefund()
    //{
    //    // Arrange
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
    //    payment.Authorize("AUTH123", "ABC");
    //    payment.Capture("CAPTURED"); // Completed
    //    payment.ClearDomainEvents();

    //    // Act
    //    var result = payment.Refund(500L, "Partial refund");

    //    // Assert
    //    result.IsError.Should().BeFalse();
    //    payment.State.Should().Be(Payment.PaymentState.PartiallyRefunded);
    //    payment.RefundedAmountCents.Should().Be(500L);
    //    payment.RefundedAt.Should().NotBeNull();
    //    payment.UpdatedAt.Should().NotBeNull();
    //    payment.DomainEvents.OfType<Payment.Events.PaymentPartiallyRefunded>().Should().ContainSingle();
    //}

    //[Fact]
    //public void Refund_ShouldReturnError_WhenNotCompletedOrPartiallyRefunded()
    //{
    //    // Arrange
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value; // Pending

    //    // Act
    //    var result = payment.Refund(500L, "Reason");

    //    // Assert
    //    result.IsError.Should().BeTrue();
    //    result.FirstError.Code.Should().Be("Payment.CannotRefundNonCompleted");
    //}

    //[Fact]
    //public void Refund_ShouldReturnError_WhenPartialRefundExceedsAvailable()
    //{
    //    // Arrange
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
    //    payment.Authorize("AUTH123", "ABC");
    //    payment.Capture("CAPTURED");
    //    payment.ClearDomainEvents();

    //    // Act
    //    var result = payment.Refund(1500L, "Too much"); // Exceeds original amount

    //    // Assert
    //    result.IsError.Should().BeTrue();
    //    result.FirstError.Code.Should().Be("Payment.PartialRefundExceedsAmount");
    //}

    //[Fact]
    //public void Refund_ShouldBeIdempotent_WhenAlreadyFullyRefundedWithSameKey()
    //{
    //    // Arrange
    //    var idempotencyKey = Guid.NewGuid().ToString();
    //    var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid(), idempotencyKey).Value;
    //    payment.Authorize("AUTH123", "ABC", idempotencyKey);
    //    payment.Capture("CAPTURED", idempotencyKey);
    //    payment.Refund(1000L, "Full refund", idempotencyKey); // First refund
    //    payment.ClearDomainEvents();

    //    // Act
    //    var result = payment.Refund(1000L, "Retry full refund", idempotencyKey); // Second refund with same key

    //    // Assert
    //    result.IsError.Should().BeFalse();
    //    payment.State.Should().Be(Payment.PaymentState.Refunded);
    //    payment.RefundedAmountCents.Should().Be(1000L);
    //    payment.DomainEvents.Should().BeEmpty();
    //}

    // --- MarkAsFailed Method Tests ---
    [Fact]
    public void MarkAsFailed_ShouldTransitionToFailed_WhenCalled()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        var errorMessage = "Insufficient Funds";
        var gatewayErrorCode = "1001";

        // Act
        var result = payment.MarkAsFailed(errorMessage: errorMessage, gatewayErrorCode: gatewayErrorCode);

        // Assert
        result.IsError.Should().BeFalse();
        payment.State.Should().Be(expected: Payment.PaymentState.Failed);
        payment.FailureReason.Should().Be(expected: errorMessage);
        payment.GatewayErrorCode.Should().Be(expected: gatewayErrorCode);
        payment.UpdatedAt.Should().NotBeNull();
        payment.DomainEvents.OfType<Payment.Events.PaymentFailed>().Should().ContainSingle();
    }

    [Fact]
    public void MarkAsFailed_ShouldReturnError_WhenErrorMessageIsTooLong()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        var longErrorMessage = new string(c: 'a', count: Payment.Constraints.FailureReasonMaxLength + 1);

        // Act
        var result = payment.MarkAsFailed(errorMessage: longErrorMessage);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Payment.FailureReason.TooLong");
    }

    [Fact]
    public void MarkAsFailed_ShouldReturnError_WhenGatewayErrorCodeIsTooLong()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;
        var longErrorCode = new string(c: 'b', count: Payment.Constraints.GatewayErrorCodeMaxLength + 1);

        // Act
        var result = payment.MarkAsFailed(errorMessage: "Error", gatewayErrorCode: longErrorCode);

        // Assert
        result.IsError.Should().BeTrue();
        result.FirstError.Code.Should().Be(expected: "Payment.GatewayErrorCode.TooLong");
    }

    [Fact]
    public void MarkAsFailed_ShouldBeIdempotent_WhenAlreadyFailedWithSameKey()
    {
        // Arrange
        var idempotencyKey = Guid.NewGuid().ToString();
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid(), idempotencyKey).Value;
        payment.MarkAsFailed("Initial failure", null); // First fail
        payment.ClearDomainEvents();

        // Act
        var result = payment.MarkAsFailed("Retry failure", "123"); // Second fail with same key

        // Assert
        result.IsError.Should().BeFalse();
        payment.State.Should().Be(Payment.PaymentState.Failed);
        payment.FailureReason.Should().Be("Initial failure"); // Should retain original
        payment.DomainEvents.Should().BeEmpty();
    }


    // --- ComputedProperties Tests ---
    [Fact]
    public void ComputedProperties_ShouldReflectCorrectState()
    {
        // Arrange
        var payment = Payment.Create(Guid.NewGuid(), 1000L, "USD", "CreditCard", Guid.NewGuid()).Value;

        // Assert initial Pending state
        payment.IsPending.Should().BeTrue();
        payment.IsAuthorizing.Should().BeFalse();
        payment.IsAuthorized.Should().BeFalse();
        payment.IsCapturing.Should().BeFalse();
        payment.IsCompleted.Should().BeFalse();
        //payment.IsPartiallyRefunded.Should().BeFalse();
        //payment.IsRefunded.Should().BeFalse();
        payment.IsVoid.Should().BeFalse();
        payment.IsFailed.Should().BeFalse();
        payment.Amount.Should().Be(expected: 10.0m);
        //payment.AvailableForRefund.Should().Be(1000m);


        // Act & Assert Authorized state
        payment = payment.MarkAsAuthorized("AUTH1", "CODE1").Value;
        payment.IsPending.Should().BeFalse();
        payment.IsAuthorized.Should().BeTrue();
        //payment.AvailableForRefund.Should().Be(1000m);


        // Act & Assert Capturing state
        // //payment.StartCapturing();
        // //payment.IsAuthorized.Should().BeFalse();
        // //payment.IsCapturing.Should().BeTrue();
        // //payment.AvailableForRefund.Should().Be(1000m);


        // Act & Assert Completed state
        payment = payment.MarkAsCaptured("CAPTURE1").Value;
        payment.IsCapturing.Should().BeFalse();
        payment.IsCompleted.Should().BeTrue();
        //payment.AvailableForRefund.Should().Be(1000m);


        //// Act & Assert PartiallyRefunded state
        //payment.Refund(300L, "Partial reason");
        //payment.IsCompleted.Should().BeFalse();
        //payment.IsPartiallyRefunded.Should().BeTrue();
        //payment.RefundedAmountCents.Should().Be(300L);
        //payment.AvailableForRefund.Should().Be(700m);


        //// Act & Assert Refunded state (full refund)
        //payment.Refund(700L, "Full reason"); // Remaining 700
        //payment.IsPartiallyRefunded.Should().BeFalse();
        //payment.IsRefunded.Should().BeTrue();
        //payment.RefundedAmountCents.Should().Be(1000L);
        //payment.AvailableForRefund.Should().Be(0m);


        // Act & Assert Void state
        var newPayment = Payment.Create(Guid.NewGuid(), 500L, "USD", "Debit", Guid.NewGuid()).Value;
        newPayment = newPayment.MarkAsAuthorized("AUTH2", "CODE2").Value;
        newPayment.Void();
        newPayment.IsAuthorized.Should().BeFalse();
        newPayment.IsVoid.Should().BeTrue();
        //newPayment.AvailableForRefund.Should().Be(500m); // Voided payments can't be refunded.
                                                            // AvailableForRefund remains at initial value logically if it hasn't been captured.


        // Act & Assert Failed state
        var anotherPayment = Payment.Create(Guid.NewGuid(), 200L, "USD", "Card", Guid.NewGuid()).Value;
        anotherPayment.MarkAsFailed("Test error", "ERR1");
        anotherPayment.IsPending.Should().BeFalse();
        anotherPayment.IsFailed.Should().BeTrue();
        //anotherPayment.AvailableForRefund.Should().Be(200m); // Failed payments can't be refunded.
    }
}
