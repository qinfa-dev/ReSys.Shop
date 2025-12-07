using ErrorOr;

namespace ReSys.Core.Domain.Payments.Providers;

/// <summary>
/// Handles incoming Stripe webhook events.
/// </summary>
public interface IStripeWebhookHandler
{
    /// <summary>
    /// Processes a Stripe webhook event, including signature verification and event handling.
    /// </summary>
    /// <param name="payload">The raw JSON payload of the webhook event.</param>
    /// <param name="signature">The value of the 'Stripe-Signature' header.</param>
    /// <param name="paymentMethodId">The ID of the PaymentMethod this webhook is associated with, used to retrieve specific webhook secrets.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{T}"/> of <see cref="Success"/> indicating if the webhook was processed successfully.</returns>
    Task<ErrorOr<Success>> HandleWebhookAsync(
        string payload,
        string signature,
        Guid paymentMethodId, // Added to get configuration
        CancellationToken cancellationToken = default);
}
