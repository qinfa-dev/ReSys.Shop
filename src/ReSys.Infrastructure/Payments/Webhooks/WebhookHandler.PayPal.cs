using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization; // For JsonPropertyName

using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Payments.Interfaces.Webhooks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.Payments;

namespace ReSys.Infrastructure.Payments.Webhooks;


/// <summary>
/// Handles webhook events from PayPal.
/// 
/// <para>
/// <strong>Supported Events:</strong>
/// <list type="bullet">
/// <item><description>PAYMENT.CAPTURE.COMPLETED - Payment captured successfully</description></item>
/// <item><description>PAYMENT.CAPTURE.DENIED - Payment capture failed</description></item>
/// <item><description>PAYMENT.CAPTURE.REFUNDED - Payment was refunded</description></item>
/// <item><description>CHECKOUT.ORDER.APPROVED - Order approved by customer</description></item>
/// </list>
/// </para>
/// </summary>
public class PayPalWebhookHandler(
    ILogger<PayPalWebhookHandler> logger, 
    IUnitOfWork unitOfWork,
    IHttpClientFactory httpClientFactory) : IWebhookHandler
{
    private readonly HttpClient _httpClient = httpClientFactory.CreateClient("PayPal");

    public string ProviderName => "PayPal";

    public async Task<ErrorOr<Success>> HandleWebhookAsync(
        WebhookEvent webhookEvent,
        string? webhookSecret, // This will be the WebhookId from PayPalOptions
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Processing PayPal webhook event: {EventType}, ID: {EventId}",
                webhookEvent.EventType, webhookEvent.EventId);

            // Parse PayPal webhook payload
            var payload = JsonSerializer.Deserialize<PayPalWebhookPayload>(
                webhookEvent.RawPayload,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            if (payload == null)
            {
                return Error.Validation(
                    code: "PayPalWebhook.InvalidPayload",
                    description: "Failed to parse PayPal webhook payload");
            }

            // Implement full PayPal webhook signature verification using PayPal's verification API.
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                logger.LogWarning("PayPal webhook secret (WebhookId) is missing. Processing event without full signature verification.");
            }
            else
            {
                var isVerified = await VerifyPayPalWebhook(webhookEvent, webhookSecret, cancellationToken);
                if (!isVerified)
                {
                    logger.LogError("PayPal webhook verification failed for event {EventId}.", webhookEvent.EventId);
                    return Error.Failure("PayPalWebhook.VerificationFailed", "PayPal webhook verification failed.");
                }
                logger.LogInformation("PayPal webhook verification successful for event {EventId}.", webhookEvent.EventId);
            }

            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Handle different event types
                return await (webhookEvent.EventType switch
                {
                    "PAYMENT.CAPTURE.COMPLETED" => HandleCaptureCompleted(payload, cancellationToken),
                    "PAYMENT.CAPTURE.DENIED" => HandleCaptureDenied(payload, cancellationToken),
                    //"PAYMENT.CAPTURE.REFUNDED" => HandleCaptureRefunded(payload, cancellationToken),
                    "CHECKOUT.ORDER.APPROVED" => HandleOrderApproved(payload, cancellationToken),
                    _ => HandleUnknownEvent(webhookEvent.EventType)
                });
            }, cancellationToken: cancellationToken); // Pass cancellation token correctly
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing PayPal webhook: {EventId}", webhookEvent.EventId);
            return Error.Failure(
                code: "PayPalWebhook.ProcessingError",
                description: $"Failed to process webhook: {ex.Message}");
        }
    }

    private async Task<ErrorOr<Success>> HandleCaptureCompleted(
        PayPalWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("PayPal capture completed: {ResourceId}", payload.Resource?.Id);

        var paymentId = payload.Resource?.Id;
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            logger.LogError("PayPal capture completed webhook is missing Resource ID.");
            return Error.Validation("PayPalWebhook.MissingResourceId", "Webhook resource ID is missing.");
        }

        var payment = await unitOfWork.Context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.ReferenceTransactionId == paymentId, cancellationToken);
        
        if (payment == null)
        {
            logger.LogWarning("Payment record not found for PayPal Capture ID: {PaymentId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", paymentId);
            return Result.Success;
        }

        var captureResult = payment.MarkAsCaptured(paymentId);
        if (captureResult.IsError) return captureResult.FirstError;

        var order = await unitOfWork.Context.Set<Order>()
            .Include(o => o.Payments) // Include payments for order state check
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);

        if (order == null)
        {
            logger.LogError("Order not found for Payment ID: {PaymentId}", payment.Id);
            return Error.NotFound("PayPalWebhook.OrderNotFound", $"Order not found for payment {payment.Id}");
        }

        // Attempt to move order to Confirm state, if conditions are met
        if (order.State == Order.Order.OrderState.Payment)
        {
            var nextResult = order.Next(); // Should transition to Confirm
            if (nextResult.IsError) return nextResult.FirstError;
        }
        else if (order.State == Order.Order.OrderState.Confirm)
        {
            var nextResult = order.Next(); // Should transition to Complete
            if (nextResult.IsError) return nextResult.FirstError;
        }

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> HandleCaptureDenied(
        PayPalWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        logger.LogWarning("PayPal capture denied: {ResourceId}", payload.Resource?.Id);

        var paymentId = payload.Resource?.Id;
        if (string.IsNullOrWhiteSpace(paymentId))
        {
            logger.LogError("PayPal capture denied webhook is missing Resource ID.");
            return Error.Validation("PayPalWebhook.MissingResourceId", "Webhook resource ID is missing.");
        }

        var payment = await unitOfWork.Context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.ReferenceTransactionId == paymentId, cancellationToken);

        if (payment == null)
        {
            logger.LogWarning("Payment record not found for PayPal Capture ID: {PaymentId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", paymentId);
            return Result.Success;
        }

        var failResult = payment.MarkAsFailed("PayPal capture denied.");
        if (failResult.IsError) return failResult.FirstError;

        // No automatic order state change on failure, usually requires user action.

        return Result.Success;
    }

    //private async Task<ErrorOr<Success>> HandleCaptureRefunded(
    //    PayPalWebhookPayload payload,
    //    CancellationToken cancellationToken)
    //{
    //    logger.LogInformation("PayPal capture refunded: {ResourceId}", payload.Resource?.Id);

    //    var captureId = payload.Resource?.Id;
    //    if (string.IsNullOrWhiteSpace(captureId))
    //    {
    //        logger.LogError("PayPal capture refunded webhook is missing Resource ID.");
    //        return Error.Validation("PayPalWebhook.MissingResourceId", "Webhook resource ID is missing.");
    //    }

    //    // PayPal's refund events typically refer to a capture ID. We need to find the payment linked to this capture.
    //    var payment = await unitOfWork.Context.Set<Payment>()
    //        .FirstOrDefaultAsync(p => p.ReferenceTransactionId == captureId && p.State == Payment.PaymentState.Completed, cancellationToken);

    //    if (payment == null)
    //    {
    //        logger.LogWarning("Completed Payment record not found for PayPal Capture ID: {CaptureId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", captureId);
    //        return Result.Success;
    //    }

    //    var refundAmount = 0m;
    //    if (payload.Resource?.Amount?.Value != null && decimal.TryParse(payload.Resource.Amount.Value, out var parsedAmount))
    //    {
    //        refundAmount = parsedAmount * 100m; // Convert to cents
    //    }

    //    var refundResult = payment.Refund(refundAmount, $"PayPal Refund {captureId}");
    //    if (refundResult.IsError) return refundResult.FirstError;

    //    // No automatic order state change on refund.

    //    return Result.Success;
    //}

    private async Task<ErrorOr<Success>> HandleOrderApproved(
        PayPalWebhookPayload payload,
        CancellationToken cancellationToken)
    {
        logger.LogInformation("PayPal order approved: {ResourceId}", payload.Resource?.Id);

        var orderIdFromPayload = payload.Resource?.Id; // This would typically be the PayPal Order ID
        if (string.IsNullOrWhiteSpace(orderIdFromPayload))
        {
            logger.LogError("PayPal order approved webhook is missing Resource ID (PayPal Order ID).");
            return Error.Validation("PayPalWebhook.MissingResourceId", "Webhook resource ID is missing.");
        }
        
        // Find the payment associated with this PayPal Order ID
        // The Payment record in our system stores the PayPal Order ID in ReferenceTransactionId
        var payment = await unitOfWork.Context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.ReferenceTransactionId == orderIdFromPayload && p.State == Payment.PaymentState.Pending, cancellationToken);

        if (payment == null)
        {
            logger.LogWarning("Pending Payment record not found for PayPal Order ID: {PayPalOrderId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", orderIdFromPayload);
            return Result.Success;
        }

        // Transition the payment to Authorized state
        var authorizeResult = payment.MarkAsAuthorized(orderIdFromPayload, string.Empty); // PayPal doesn't typically provide a separate auth code here, use string.Empty
        if (authorizeResult.IsError) return authorizeResult.FirstError;

        var order = await unitOfWork.Context.Set<Order>()
            .Include(o => o.Payments) // Include payments for order state check
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);
        
        if (order == null)
        {
            logger.LogError("Order not found for Payment ID: {PaymentId}", payment.Id);
            return Error.NotFound("PayPalWebhook.OrderNotFound", $"Order not found for payment {payment.Id}");
        }

        // If AutoCapture is true for this payment method, and the order is in Payment state,
        // we should attempt to capture and move to the next state.
        // However, the actual CAPTURE event will come as PAYMENT.CAPTURE.COMPLETED,
        // so this event primarily marks the authorization.
        // We'll let the PAYMENT.CAPTURE.COMPLETED handle the full order progression.

        // If the order is in Payment state, we can move it to Confirm.
        if (order.State == Order.Order.OrderState.Payment)
        {
            var nextResult = order.Next(); // Should transition to Confirm
            if (nextResult.IsError) return nextResult.FirstError;
        }

        return Result.Success;
    }

    private Task<ErrorOr<Success>> HandleUnknownEvent(string eventType)
    {
        logger.LogInformation("Unhandled PayPal event type: {EventType}", eventType);
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }

    private async Task<bool> VerifyPayPalWebhook(
        WebhookEvent webhookEvent, 
        string webhookId, // This is the webhook_id from PayPalOptions
        CancellationToken cancellationToken)
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "v1/notifications/verify-webhook");
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        // Extract required headers
        string? transmissionId = null; // Initialize to null
        string? transmissionTime = null; // Initialize to null
        string? transmissionSig = null; // Initialize to null
        string? certUrl = null; // Initialize to null
        string? authAlgo = null; // Initialize to null

        webhookEvent.Headers?.TryGetValue("paypal-transmission-id", out transmissionId);
        webhookEvent.Headers?.TryGetValue("paypal-transmission-time", out transmissionTime);
        webhookEvent.Headers?.TryGetValue("paypal-transmission-sig", out transmissionSig);
        webhookEvent.Headers?.TryGetValue("paypal-cert-url", out certUrl);
        webhookEvent.Headers?.TryGetValue("paypal-auth-algo", out authAlgo);

        if (string.IsNullOrWhiteSpace(transmissionId) ||
            string.IsNullOrWhiteSpace(transmissionTime) ||
            string.IsNullOrWhiteSpace(transmissionSig) ||
            string.IsNullOrWhiteSpace(certUrl) ||
            string.IsNullOrWhiteSpace(authAlgo))
        {
            logger.LogError("Missing one or more required PayPal webhook verification headers.");
            return false;
        }

        var verificationRequest = new
        {
            auth_algo = authAlgo,
            cert_url = certUrl,
            transmission_id = transmissionId,
            transmission_time = transmissionTime,
            transmission_sig = transmissionSig,
            webhook_id = webhookId,
            webhook_event = JsonDocument.Parse(webhookEvent.RawPayload).RootElement // Pass the raw JSON event
        };

        request.Content = JsonContent.Create(verificationRequest);

        try
        {
            // PayPal's webhook verification endpoint should be relative to the HttpClient's BaseAddress
            var response = await _httpClient.PostAsync("v1/notifications/verify-webhook", request.Content, cancellationToken);
            response.EnsureSuccessStatusCode();

            var verificationResponse = await response.Content.ReadFromJsonAsync<PayPalWebhookVerificationResponse>(cancellationToken);
            
            return verificationResponse?.VerificationStatus == "SUCCESS";
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "HTTP request failed during PayPal webhook verification: {Message}", ex.Message);
            return false;
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error during PayPal webhook verification: {Message}", ex.Message);
            return false;
        }
    }

    #region PayPal Webhook Models
    private record PayPalWebhookPayload(
        string Id,
        string EventType,
        string CreateTime,
        PayPalResource? Resource);

    private record PayPalResource(
        string Id,
        string Status,
        PayPalAmount? Amount);

    private record PayPalAmount(
        string CurrencyCode,
        string Value);

    private record PayPalWebhookVerificationResponse
    {
        [JsonPropertyName("verification_status")]
        public string? VerificationStatus { get; init; }
    }
    #endregion
}