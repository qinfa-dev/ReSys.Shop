using ErrorOr;

using ReSys.Core.Feature.Common.Payments.Interfaces.Providers;
using ReSys.Core.Feature.Common.Payments.Models;

namespace ReSys.Infrastructure.Payments.Services;

/// <summary>
/// Cash on Delivery (COD) payment provider implementation.
/// 
/// <para>
/// <strong>Purpose:</strong>
/// Handles offline payment processing where payment is collected at delivery time.
/// This is a "no-op" provider since actual payment collection happens outside the system.
/// </para>
/// 
/// <para>
/// <strong>Characteristics:</strong>
/// <list type="bullet">
/// <item><description>Auto-capture is always true (payment considered captured immediately)</description></item>
/// <item><description>No external API calls required</description></item>
/// <item><description>Transaction IDs are generated locally</description></item>
/// <item><description>All operations succeed by default</description></item>
/// <item><description>Refunds and voids are tracked but not processed externally</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Business Logic:</strong>
/// COD orders are marked as "paid" immediately upon authorization, but actual cash
/// collection is the responsibility of the delivery personnel. The system treats
/// these as successful transactions for order processing purposes.
/// </para>
/// </summary>
public class CashOnDeliveryProvider(CashOnDeliveryOptions options) : PaymentProviderBase<CashOnDeliveryOptions>(options)
{
    /// <summary>
    /// Authorizes a COD payment by generating a local transaction ID.
    /// Always succeeds as no external validation is required.
    /// </summary>
    public override Task<ErrorOr<ProviderResponse>> AuthorizeAsync(
        AuthorizationRequest request,
        CancellationToken cancellationToken = default)
    {
        // Generate a unique transaction ID for tracking
        var transactionId = GenerateTransactionId(request.OrderNumber);

        var response = new ProviderResponse(
            Status: PaymentStatus.Authorized,
            TransactionId: transactionId,
            RawResponse: new Dictionary<string, string>
            {
                ["provider"] = "CashOnDelivery",
                ["order_number"] = request.OrderNumber,
                ["amount"] = request.Amount.ToString("F2"),
                ["currency"] = request.Currency,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["delivery_instructions"] = _options.DeliveryInstructions ?? "Collect cash on delivery"
            }
        );

        return Task.FromResult<ErrorOr<ProviderResponse>>(response);
    }

    /// <summary>
    /// Captures a COD payment. Since COD is auto-capture, this confirms the transaction.
    /// In practice, this might be called when the delivery is confirmed.
    /// </summary>
    public override Task<ErrorOr<ProviderResponse>> CaptureAsync(
        CaptureRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new ProviderResponse(
            Status: PaymentStatus.Captured,
            TransactionId: request.TransactionId,
            RawResponse: new Dictionary<string, string>
            {
                ["provider"] = "CashOnDelivery",
                ["captured_amount"] = request.Amount.ToString("F2"),
                ["currency"] = request.Currency,
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["status"] = "Cash collected on delivery"
            }
        );

        return Task.FromResult<ErrorOr<ProviderResponse>>(response);
    }

    /// <summary>
    /// Voids a COD payment (cancels before delivery).
    /// Useful when an order is cancelled before the delivery is attempted.
    /// </summary>
    public override Task<ErrorOr<ProviderResponse>> VoidAsync(
        VoidRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new ProviderResponse(
            Status: PaymentStatus.Voided,
            TransactionId: request.TransactionId,
            RawResponse: new Dictionary<string, string>
            {
                ["provider"] = "CashOnDelivery",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["status"] = "Order cancelled - no cash to collect"
            }
        );

        return Task.FromResult<ErrorOr<ProviderResponse>>(response);
    }

    /// <summary>
    /// Refunds a COD payment (processes return after delivery).
    /// Tracks the refund but actual cash return is handled offline.
    /// </summary>
    public override Task<ErrorOr<ProviderResponse>> RefundAsync(
        RefundRequest request,
        CancellationToken cancellationToken = default)
    {
        var response = new ProviderResponse(
            Status: PaymentStatus.Refunded,
            TransactionId: request.TransactionId,
            RawResponse: new Dictionary<string, string>
            {
                ["provider"] = "CashOnDelivery",
                ["refund_amount"] = request.Amount.ToString("F2"),
                ["currency"] = request.Currency,
                ["reason"] = request.Reason ?? "Customer return",
                ["timestamp"] = DateTimeOffset.UtcNow.ToString("O"),
                ["status"] = "Refund to be processed offline"
            }
        );

        return Task.FromResult<ErrorOr<ProviderResponse>>(response);
    }

    private string GenerateTransactionId(string orderNumber)
    {
        var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        return $"COD-{orderNumber}-{timestamp}";
    }
}

/// <summary>
/// Configuration options for the Cash on Delivery provider.
/// </summary>
public class CashOnDeliveryOptions
{
    /// <summary>
    /// Optional delivery instructions for the driver/courier.
    /// </summary>
    public string? DeliveryInstructions { get; set; }

    /// <summary>
    /// Whether to require signature on delivery.
    /// </summary>
    public bool RequireSignature { get; set; } = true;

    /// <summary>
    /// Maximum amount allowed for COD orders (to reduce fraud risk).
    /// </summary>
    public decimal? MaxOrderAmount { get; set; }

    /// <summary>
    /// Additional handling fee for COD orders.
    /// </summary>
    public decimal HandlingFee { get; set; } = 0m;
}