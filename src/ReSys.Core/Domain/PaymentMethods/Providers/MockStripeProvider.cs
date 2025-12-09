using ErrorOr;
using Microsoft.Extensions.Logging;
using ReSys.Core.Domain.PaymentMethods.Providers.Models;

namespace ReSys.Core.Domain.PaymentMethods.Providers;

/// <summary>
/// A mock implementation of the Stripe payment provider for testing and demonstration purposes.
/// This provider simulates successful and failed payment operations based on predefined logic.
/// </summary>
public class MockStripeProvider : PaymentProviderBase<StripeProviderOptions>
{
    private readonly ILogger<MockStripeProvider> _logger;

    public MockStripeProvider(StripeProviderOptions options, ILogger<MockStripeProvider> logger) : base(options)
    {
        _logger = logger;
        _logger.LogInformation("MockStripeProvider initialized with PublishableKey: {PublishableKey}", _options.PublishableKey);
    }

    public override async Task<ErrorOr<ProviderResponse>> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("MockStripeProvider: Authorizing amount {Amount} {Currency} for order {OrderNumber}",
            request.Amount, request.Currency, request.OrderNumber);

        if (request.Amount <= 0)
        {
            return Error.Validation("MockStripeProvider.InvalidAmount", "Amount must be positive.");
        }

        // Simulate success
        var transactionId = $"mock_auth_{Guid.NewGuid().ToString().Substring(0, 8)}";
        var response = new ProviderResponse(
            Status: PaymentStatus.Authorized,
            TransactionId: transactionId,
            RawResponse: new Dictionary<string, string> { { "mock_status", "authorized" } }
        );
        return response;
    }

    public override async Task<ErrorOr<ProviderResponse>> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("MockStripeProvider: Capturing transaction {TransactionId} for amount {Amount} {Currency}",
            request.TransactionId, request.Amount, request.Currency);

        if (string.IsNullOrEmpty(request.TransactionId))
        {
            return Error.Validation("MockStripeProvider.InvalidTransactionId", "Transaction ID is required.");
        }

        // Simulate success
        var response = new ProviderResponse(
            Status: PaymentStatus.Captured,
            TransactionId: request.TransactionId,
            RawResponse: new Dictionary<string, string> { { "mock_status", "captured" } }
        );
        return response;
    }

    public override async Task<ErrorOr<ProviderResponse>> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("MockStripeProvider: Refunding transaction {TransactionId} for amount {Amount} {Currency}",
            request.TransactionId, request.Amount, request.Currency);

        if (string.IsNullOrEmpty(request.TransactionId))
        {
            return Error.Validation("MockStripeProvider.InvalidTransactionId", "Transaction ID is required.");
        }

        // Simulate success
        var response = new ProviderResponse(
            Status: PaymentStatus.Refunded,
            TransactionId: request.TransactionId,
            RawResponse: new Dictionary<string, string> { { "mock_status", "refunded" } }
        );
        return response;
    }

    public override async Task<ErrorOr<ProviderResponse>> VoidAsync(VoidRequest request, CancellationToken cancellationToken = default)
    {
        await Task.CompletedTask;
        _logger.LogInformation("MockStripeProvider: Voiding transaction {TransactionId}",
            request.TransactionId);

        if (string.IsNullOrEmpty(request.TransactionId))
        {
            return Error.Validation("MockStripeProvider.InvalidTransactionId", "Transaction ID is required.");
        }

        // Simulate success
        var response = new ProviderResponse(
            Status: PaymentStatus.Voided,
            TransactionId: request.TransactionId,
            RawResponse: new Dictionary<string, string> { { "mock_status", "voided" } }
        );
        return response;
    }
}
