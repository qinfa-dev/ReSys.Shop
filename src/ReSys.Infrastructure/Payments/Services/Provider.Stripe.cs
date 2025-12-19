using ErrorOr;

using Microsoft.Extensions.Logging;
using Stripe;

using ReSys.Core.Feature.Common.Payments.Interfaces.Providers;
using ReSys.Core.Feature.Common.Payments.Models;
using ReSys.Core.Feature.Common.Payments.Options;

namespace ReSys.Infrastructure.Payments.Services;

/// <summary>
/// Stripe payment provider implementation using Stripe .NET client library.
/// </summary>
public class StripeProvider : PaymentProviderBase<StripeOptions>
{
    private readonly ILogger<StripeProvider> _logger;

    public StripeProvider(StripeOptions options, ILogger<StripeProvider> logger) : base(options)
    {
        _logger = logger;
        StripeConfiguration.ApiKey = _options.SecretKey;
    }

    /// <summary>
    /// Authorizes a payment using Stripe.
    /// The AuthorizationRequest.PaymentToken should be a Stripe PaymentMethod ID.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Authorizing Stripe payment for order {OrderNumber}, amount {Amount} {Currency}",
            request.OrderNumber, request.Amount, request.Currency);

        try
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntentOptions = new PaymentIntentCreateOptions
            {
                Amount = (long)(request.Amount * 100), // Stripe uses cents
                Currency = request.Currency,
                PaymentMethod = request.PaymentToken,
                ConfirmationMethod = "manual",
                Confirm = true,
                CaptureMethod = _options.AutoCapture ? "automatic" : "manual",
                StatementDescriptor = _options.StatementDescriptor,
                Description = $"Order {request.OrderNumber}",
                Metadata = request.Metadata,
                SetupFutureUsage = "off_session", // If we want to save cards for later
                Shipping = request.ShippingAddress != null ? new ChargeShippingOptions
                {
                    Address = new AddressOptions
                    {
                        Line1 = request.ShippingAddress.AddressLine1,
                        Line2 = request.ShippingAddress.AddressLine2,
                        City = request.ShippingAddress.City,
                        State = request.ShippingAddress.State,
                        PostalCode = request.ShippingAddress.PostalCode,
                        Country = request.ShippingAddress.Country
                    },
                    Name = $"{request.ShippingAddress.FirstName} {request.ShippingAddress.LastName}".Trim()
                } : null,
                ReceiptEmail = request.CustomerEmail
            };

            // Add idempotency key
            var requestOptions = new RequestOptions
            {
                IdempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString()
            };

            var paymentIntent = await paymentIntentService.CreateAsync(paymentIntentOptions, requestOptions, cancellationToken);

            return MapStripePaymentIntentToProviderResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe authorization failed for order {OrderNumber}: {Message}",
                request.OrderNumber, ex.Message);
            return Error.Failure(
                code: $"Stripe.AuthFailed.{ex.StripeError?.Code}",
                description: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe authorization for order {OrderNumber}",
                request.OrderNumber);
            return Error.Failure(
                code: "Stripe.AuthorizationError",
                description: $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Captures a previously authorized Stripe payment.
    /// The CaptureRequest.TransactionId should be the Stripe PaymentIntent ID.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> CaptureAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Capturing Stripe payment {TransactionId}, amount {Amount} {Currency}",
            request.TransactionId, request.Amount, request.Currency);

        try
        {
            var paymentIntentService = new PaymentIntentService();
            var paymentIntentCaptureOptions = new PaymentIntentCaptureOptions
            {
                AmountToCapture = (long)(request.Amount * 100),
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString()
            };

            var paymentIntent = await paymentIntentService.CaptureAsync(
                request.TransactionId, paymentIntentCaptureOptions, requestOptions, cancellationToken);

            return MapStripePaymentIntentToProviderResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe capture failed for transaction {TransactionId}: {Message}",
                request.TransactionId, ex.Message);
            return Error.Failure(
                code: $"Stripe.CaptureFailed.{ex.StripeError?.Code}",
                description: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe capture for transaction {TransactionId}",
                request.TransactionId);
            return Error.Failure(
                code: "Stripe.CaptureError",
                description: $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Voids (cancels) an authorized but uncaptured Stripe payment.
    /// The VoidRequest.TransactionId should be the Stripe PaymentIntent ID.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> VoidAsync(
        VoidRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Voiding Stripe payment {TransactionId}", request.TransactionId);

        try
        {
            var paymentIntentService = new PaymentIntentService();
            var requestOptions = new RequestOptions
            {
                IdempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString()
            };

            var paymentIntent = await paymentIntentService.CancelAsync(
                request.TransactionId, null, requestOptions, cancellationToken);

            return MapStripePaymentIntentToProviderResponse(paymentIntent);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe void failed for transaction {TransactionId}: {Message}",
                request.TransactionId, ex.Message);
            return Error.Failure(
                code: $"Stripe.VoidFailed.{ex.StripeError?.Code}",
                description: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe void for transaction {TransactionId}",
                request.TransactionId);
            return Error.Failure(
                code: "Stripe.VoidError",
                description: $"Unexpected error: {ex.Message}");
        }
    }

    /// <summary>
    /// Refunds a captured Stripe payment.
    /// The RefundRequest.TransactionId should be the Stripe PaymentIntent ID.
    /// </summary>
    public override async Task<ErrorOr<ProviderResponse>> RefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Refunding Stripe payment {TransactionId}, amount {Amount} {Currency}",
            request.TransactionId, request.Amount, request.Currency);

        try
        {
            var refundService = new RefundService();
            var refundOptions = new RefundCreateOptions
            {
                PaymentIntent = request.TransactionId,
                Amount = (long)(request.Amount * 100),
                Reason = request.Reason
            };

            var requestOptions = new RequestOptions
            {
                IdempotencyKey = request.IdempotencyKey ?? Guid.NewGuid().ToString()
            };

            var refund = await refundService.CreateAsync(refundOptions, requestOptions, cancellationToken);

            return MapStripeRefundToProviderResponse(refund, request.TransactionId);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe refund failed for transaction {TransactionId}: {Message}",
                request.TransactionId, ex.Message);
            return Error.Failure(
                code: $"Stripe.RefundFailed.{ex.StripeError?.Code}",
                description: ex.Message);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error during Stripe refund for transaction {TransactionId}",
                request.TransactionId);
            return Error.Failure(
                code: "Stripe.RefundError",
                description: $"Unexpected error: {ex.Message}");
        }
    }

    private ProviderResponse MapStripePaymentIntentToProviderResponse(PaymentIntent paymentIntent)
    {
        var status = MapStripePaymentIntentStatus(paymentIntent.Status);

        return new ProviderResponse(
            Status: status,
            TransactionId: paymentIntent.Id,
            RawResponse: new Dictionary<string, string>
            {
                ["provider"] = "Stripe",
                ["payment_intent_id"] = paymentIntent.Id,
                ["client_secret"] = paymentIntent.ClientSecret,
                ["status"] = paymentIntent.Status,
                ["amount"] = (paymentIntent.Amount / 100M).ToString(),
                ["currency"] = paymentIntent.Currency
            }
        );
    }

    private ProviderResponse MapStripeRefundToProviderResponse(Refund refund, string paymentIntentId)
    {
        var status = MapStripeRefundStatus(refund.Status);

        return new ProviderResponse(
            Status: status,
            TransactionId: refund.Id,
            RawResponse: new Dictionary<string, string>
            {
                ["provider"] = "Stripe",
                ["refund_id"] = refund.Id,
                ["payment_intent_id"] = paymentIntentId,
                ["status"] = refund.Status,
                ["amount"] = (refund.Amount / 100M).ToString(),
                ["currency"] = refund.Currency
            }
        );
    }

    private PaymentStatus MapStripePaymentIntentStatus(string stripeStatus) =>
        stripeStatus switch
        {
            "requires_payment_method" => PaymentStatus.RequiresAction,
            "requires_confirmation" => PaymentStatus.RequiresAction,
            "requires_action" => PaymentStatus.RequiresAction,
            "processing" => PaymentStatus.Processing,
            "succeeded" => PaymentStatus.Captured, // If capture_method was automatic
            "requires_capture" => PaymentStatus.Authorized, // If capture_method was manual
            "canceled" => PaymentStatus.Voided,
            _ => PaymentStatus.Pending // Default to pending for unknown statuses
        };

    private PaymentStatus MapStripeRefundStatus(string stripeStatus) =>
        stripeStatus switch
        {
            "pending" => PaymentStatus.Processing,
            "succeeded" => PaymentStatus.Refunded,
            "failed" => PaymentStatus.Failed,
            _ => PaymentStatus.Pending // Default to pending for unknown statuses
        };
}
