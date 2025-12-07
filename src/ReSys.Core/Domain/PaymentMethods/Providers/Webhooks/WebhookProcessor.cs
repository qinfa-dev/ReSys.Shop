using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ErrorOr;
using Microsoft.Extensions.Logging;
using ReSys.Core.Domain.PaymentMethods.Providers.Webhooks;

namespace ReSys.Core.Domain.PaymentMethods.Providers.Webhooks;

/// <summary>
/// Orchestrates the processing of incoming webhook events by dispatching them to
/// the appropriate <see cref="IWebhookHandler"/> based on the provider name.
/// </summary>
public class WebhookProcessor
{
    private readonly IEnumerable<IWebhookHandler> _handlers;
    private readonly ILogger<WebhookProcessor> _logger;

    public WebhookProcessor(IEnumerable<IWebhookHandler> handlers, ILogger<WebhookProcessor> logger)
    {
        _handlers = handlers;
        _logger = logger;
    }

    /// <summary>
    /// Processes an incoming webhook event by finding and delegating to the responsible handler.
    /// </summary>
    /// <param name="webhookEvent">The generic webhook event to process.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{Success}"/> indicating the outcome of the processing.</returns>
    public async Task<ErrorOr<Success>> ProcessWebhookAsync(WebhookEvent webhookEvent, CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Processing webhook for provider: {ProviderName}, EventId: {EventId}",
            webhookEvent.ProviderName, webhookEvent.EventId);

        var handler = _handlers.FirstOrDefault(h => h.ProviderName.Equals(webhookEvent.ProviderName, StringComparison.OrdinalIgnoreCase));

        if (handler is null)
        {
            _logger.LogWarning("No webhook handler found for provider: {ProviderName}", webhookEvent.ProviderName);
            return Error.NotFound(
                code: "WebhookProcessor.NoHandlerFound",
                description: $"No webhook handler found for provider '{webhookEvent.ProviderName}'.");
        }

        return await handler.HandleWebhookAsync(webhookEvent, cancellationToken);
    }
}
