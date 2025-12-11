namespace ReSys.Core.Domain.PaymentMethods.Providers.Webhooks;

/// <summary>
/// Defines the contract for handling webhook events from a specific payment provider.
/// </summary>
public interface IWebhookHandler
{
    /// <summary>
    /// Gets the unique name of the payment provider this handler is for (e.g., "Stripe", "PayPal").
    /// </summary>
    string ProviderName { get; }

    /// <summary>
    /// Processes a generic webhook event, mapping it to internal domain events or actions.
    /// </summary>
    /// <param name="webhookEvent">The generic webhook event containing the raw payload and metadata.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{Success}"/> indicating the outcome of the processing.</returns>
    Task<ErrorOr<Success>> HandleWebhookAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default);
}