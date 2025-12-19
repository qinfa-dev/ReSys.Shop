namespace ReSys.Core.Feature.Common.Payments.Models;

/// <summary>
/// Represents the status of a payment transaction throughout its lifecycle.
/// 
/// <para>
/// <strong>Status Flow:</strong>
/// Pending → Processing → Authorized → Captured (Success Path)
/// Pending → Failed (Failure Path)
/// Authorized → Voided (Cancellation)
/// Captured → Refunded (Return)
/// </para>
/// </summary>
public enum PaymentStatus
{
    /// <summary>
    /// Payment has been initiated but not yet processed.
    /// Initial state for most payment methods.
    /// </summary>
    Pending,

    /// <summary>
    /// Payment is currently being processed by the provider.
    /// Transient state during API calls.
    /// </summary>
    Processing,

    /// <summary>
    /// Payment requires additional action from the customer.
    /// Common for 3D Secure authentication or PayPal approval.
    /// </summary>
    RequiresAction,

    /// <summary>
    /// Payment has been authorized but not yet captured.
    /// Funds are held but not yet transferred.
    /// </summary>
    Authorized,

    /// <summary>
    /// Payment has been captured and funds are being settled.
    /// Final success state for most payment methods.
    /// </summary>
    Captured,

    /// <summary>
    /// Payment authorization was voided/cancelled before capture.
    /// Funds are released back to the customer.
    /// </summary>
    Voided,

    /// <summary>
    /// Payment was captured but later refunded to the customer.
    /// Funds returned to customer's payment method.
    /// </summary>
    Refunded,

    /// <summary>
    /// Payment failed due to insufficient funds, declined card, or other errors.
    /// Terminal failure state.
    /// </summary>
    Failed,

    /// <summary>
    /// Payment is under review for fraud or compliance reasons.
    /// Requires manual review before proceeding.
    /// </summary>
    UnderReview
}