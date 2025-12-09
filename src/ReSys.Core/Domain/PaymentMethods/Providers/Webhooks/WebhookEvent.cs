namespace ReSys.Core.Domain.PaymentMethods.Providers.Webhooks;

/// <summary>
/// Represents a generic webhook event received from a payment provider.
/// </summary>
public record WebhookEvent
{
    /// <summary>
    /// The name of the payment provider that sent the webhook (e.g., "Stripe", "PayPal").
    /// </summary>
    public string ProviderName { get; init; } = string.Empty;

    /// <summary>
    /// The type of event (e.g., "charge.succeeded", "payment_intent.succeeded").
    /// </summary>
    public string EventType { get; init; } = string.Empty;

    /// <summary>
    /// A unique identifier for the webhook event from the provider.
    /// </summary>
    public string EventId { get; init; } = string.Empty;

    /// <summary>
    /// The raw payload received from the webhook, typically JSON.
    /// </summary>
    public string RawPayload { get; init; } = string.Empty;

    /// <summary>
    /// Optional timestamp when the event was created by the provider.
    /// </summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>
    /// Optional dictionary of headers received with the webhook request.
    /// </summary>
    public Dictionary<string, string>? Headers { get; init; }
}