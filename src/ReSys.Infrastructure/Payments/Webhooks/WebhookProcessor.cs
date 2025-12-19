using ErrorOr;

using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Payments.Interfaces.Webhooks;

namespace ReSys.Infrastructure.Payments.Webhooks;

/// <summary>
/// Orchestrates the processing of incoming webhook events by dispatching them to
/// the appropriate <see cref="IWebhookHandler"/> based on the provider name.
/// </summary>
public class WebhookProcessor(IEnumerable<IWebhookHandler> handlers, ILogger<WebhookProcessor> logger)
{
    /// <summary>
    /// Processes an incoming webhook event by finding and delegating to the responsible handler.
    /// </summary>
    /// <param name="webhookEvent">The generic webhook event to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{Success}"/> indicating the outcome of the processing.</returns>
        public async Task<ErrorOr<Success>> ProcessWebhookAsync(
            WebhookEvent webhookEvent,
            string? webhookSecret,
            CancellationToken cancellationToken = default)
        {
            logger.LogInformation("Processing webhook for provider: {ProviderName}, EventId: {EventId}",
                webhookEvent.ProviderName, webhookEvent.EventId);
    
            var handler = handlers.FirstOrDefault(h => h.ProviderName.Equals(webhookEvent.ProviderName, StringComparison.OrdinalIgnoreCase));
    
            if (handler is null)
            {
                logger.LogWarning("No webhook handler found for provider: {ProviderName}", webhookEvent.ProviderName);
                return Error.NotFound(
                    code: "WebhookProcessor.NoHandlerFound",
                    description: $"No webhook handler found for provider '{webhookEvent.ProviderName}'.");
            }
    
            return await handler.HandleWebhookAsync(webhookEvent, webhookSecret, cancellationToken);
        }
}