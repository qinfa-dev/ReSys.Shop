using ErrorOr;

using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Payments.Interfaces.Webhooks;
using ReSys.Core.Feature.Common.Persistence.Interfaces;
using ReSys.Core.Domain.Orders;
using ReSys.Core.Domain.Orders.Payments;

using Stripe;
using IsolationLevel = System.Data.IsolationLevel; // Add this using directive for IsolationLevel

namespace ReSys.Infrastructure.Payments.Webhooks;

/// <summary>
/// Handles webhook events from Stripe.
/// </summary>
public class StripeWebhookHandler(ILogger<StripeWebhookHandler> logger, IUnitOfWork unitOfWork) : IWebhookHandler
{
    public string ProviderName => "Stripe";

    public async Task<ErrorOr<Success>> HandleWebhookAsync(
        WebhookEvent webhookEvent,
        string? webhookSecret,
        CancellationToken cancellationToken = default)
    {
        try
        {
            logger.LogInformation("Processing Stripe webhook event: {EventType}, ID: {EventId}",
                webhookEvent.EventType, webhookEvent.EventId);

            Event stripeEvent;
            // Implement webhook signature verification
            if (string.IsNullOrWhiteSpace(webhookSecret))
            {
                logger.LogWarning("Stripe webhook secret is missing. Processing event without signature verification.");
                stripeEvent = EventUtility.ParseEvent(webhookEvent.RawPayload);
            }
            else
            {
                // Retrieve the webhook signature from the webhook event headers
                string? stripeSignature = null;
                webhookEvent.Headers?.TryGetValue("stripe-signature", out stripeSignature);

                if (string.IsNullOrWhiteSpace(stripeSignature))
                {
                    logger.LogError("Stripe-Signature header is missing for event {EventId}.", webhookEvent.EventId);
                    return Error.Failure(
                        code: "StripeWebhook.SignatureMissing",
                        description: "Stripe-Signature header is missing.");
                }
                stripeEvent = EventUtility.ConstructEvent(
                    webhookEvent.RawPayload,
                    stripeSignature,
                    webhookSecret
                );
            }
            
            return await unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                // Handle different event types
                return await (stripeEvent.Type switch
                {
                    "payment_intent.succeeded" => HandlePaymentIntentSucceeded(stripeEvent, cancellationToken),
                    "payment_intent.payment_failed" => HandlePaymentIntentFailed(stripeEvent, cancellationToken),
                    "payment_intent.canceled" => HandlePaymentIntentCanceled(stripeEvent, cancellationToken),
                    //"charge.refunded" => HandleChargeRefunded(stripeEvent, cancellationToken),
                    _ => HandleUnknownEvent(stripeEvent.Type)
                });
            }, isolationLevel: IsolationLevel.ReadCommitted, cancellationToken: cancellationToken); // Pass cancellation token correctly
        }
        catch (StripeException ex)
        {
            logger.LogError(ex, "Error verifying or parsing Stripe webhook: {EventId}. Message: {Message}",
                webhookEvent.EventId, ex.Message);
            return Error.Failure(
                code: "StripeWebhook.VerificationError",
                description: $"Failed to verify or parse webhook: {ex.Message}");
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error processing Stripe webhook: {EventId}", webhookEvent.EventId);
            return Error.Failure(
                code: "StripeWebhook.ProcessingError",
                description: $"Failed to process webhook: {ex.Message}");
        }
    }

    private async Task<ErrorOr<Success>> HandlePaymentIntentSucceeded(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            logger.LogError("PaymentIntent object is null for event {EventType}", stripeEvent.Type);
            return Error.Validation("StripeWebhook.InvalidObject", "PaymentIntent object is null.");
        }

        logger.LogInformation("Stripe Payment Intent succeeded: {PaymentIntentId}", paymentIntent.Id);

        var payment = await unitOfWork.Context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.ReferenceTransactionId == paymentIntent.Id, cancellationToken);

        if (payment == null)
        {
            logger.LogWarning("Payment record not found for Stripe Payment Intent ID: {PaymentIntentId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", paymentIntent.Id);
            return Result.Success;
        }

        var captureResult = payment.MarkAsCaptured(paymentIntent.Id);
        if (captureResult.IsError) return captureResult.FirstError;

        var order = await unitOfWork.Context.Set<Order>()
            .Include(o => o.Payments) // Include payments for order state check
            .FirstOrDefaultAsync(o => o.Id == payment.OrderId, cancellationToken);
        
        if (order == null)
        {
            logger.LogError("Order not found for Payment ID: {PaymentId}", payment.Id);
            return Error.NotFound("StripeWebhook.OrderNotFound", $"Order not found for payment {payment.Id}");
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

    private async Task<ErrorOr<Success>> HandlePaymentIntentFailed(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            logger.LogError("PaymentIntent object is null for event {EventType}", stripeEvent.Type);
            return Error.Validation("StripeWebhook.InvalidObject", "PaymentIntent object is null.");
        }

        logger.LogWarning("Stripe Payment Intent failed: {PaymentIntentId}. Last Error: {ErrorCode} - {ErrorMessage}",
            paymentIntent.Id, paymentIntent.LastPaymentError?.Code, paymentIntent.LastPaymentError?.Message);

        var payment = await unitOfWork.Context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.ReferenceTransactionId == paymentIntent.Id, cancellationToken);
        
        if (payment == null)
        {
            logger.LogWarning("Payment record not found for Stripe Payment Intent ID: {PaymentIntentId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", paymentIntent.Id);
            return Result.Success;
        }

        var failResult = payment.MarkAsFailed(paymentIntent.LastPaymentError?.Message ?? "Payment failed", paymentIntent.LastPaymentError?.Code);
        if (failResult.IsError) return failResult.FirstError;

        // No automatic order state change on failure, usually requires user action.
        // The order might remain in Payment state for retry.

        return Result.Success;
    }

    private async Task<ErrorOr<Success>> HandlePaymentIntentCanceled(Event stripeEvent, CancellationToken cancellationToken)
    {
        var paymentIntent = stripeEvent.Data.Object as PaymentIntent;
        if (paymentIntent == null)
        {
            logger.LogError("PaymentIntent object is null for event {EventType}", stripeEvent.Type);
            return Error.Validation("StripeWebhook.InvalidObject", "PaymentIntent object is null.");
        }

        logger.LogInformation("Stripe Payment Intent cancelled: {PaymentIntentId}", paymentIntent.Id);

        var payment = await unitOfWork.Context.Set<Payment>()
            .FirstOrDefaultAsync(p => p.ReferenceTransactionId == paymentIntent.Id, cancellationToken);
        
        if (payment == null)
        {
            logger.LogWarning("Payment record not found for Stripe Payment Intent ID: {PaymentIntentId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", paymentIntent.Id);
            return Result.Success;
        }

        var voidResult = payment.Void();
        if (voidResult.IsError) return voidResult.FirstError;

        // No automatic order state change on cancellation, usually implies order cancellation or user action.

        return Result.Success;
    }

    //private async Task<ErrorOr<Success>> HandleChargeRefunded(Event stripeEvent, CancellationToken cancellationToken)
    //{
    //    var charge = stripeEvent.Data.Object as Charge;
    //    if (charge == null || charge.Refunds == null || !charge.Refunds.Any())
    //    {
    //        logger.LogError("Charge or Refunds object is null/empty for event {EventType}", stripeEvent.Type);
    //        return Error.Validation("StripeWebhook.InvalidObject", "Charge or Refunds object is null/empty.");
    //    }

    //    var refund = charge.Refunds.First(); // Assuming single refund per charge event
    //    logger.LogInformation("Stripe Charge refunded: {RefundId} for PaymentIntent {PaymentIntentId}",
    //        refund.Id, charge.PaymentIntentId);

    //    var payment = await unitOfWork.Context.Set<Payment>()
    //        .FirstOrDefaultAsync(p => p.ReferenceTransactionId == charge.PaymentIntentId, cancellationToken);
    //    
    //    if (payment == null)
    //    {
    //        logger.LogWarning("Payment record not found for Stripe Payment Intent ID: {PaymentIntentId}. It might be that the payment was created with a different transaction ID, or the webhook arrived before payment creation.", charge.PaymentIntentId);
    //        return Result.Success;
    //    }

    //    var refundAmountCents = (decimal)refund.Amount; // Stripe refund amount is in cents
    //    var refundResult = payment.Refund(refundAmountCents, $"Stripe Refund {refund.Id}");
    //    if (refundResult.IsError) return refundResult.FirstError;

    //    // No automatic order state change on refund, but might trigger a return process in the future.

    //    return Result.Success;
    //}

    private Task<ErrorOr<Success>> HandleUnknownEvent(string eventType)
    {
        logger.LogInformation("Unhandled Stripe event type: {EventType}", eventType);
        return Task.FromResult<ErrorOr<Success>>(Result.Success);
    }
}