using ErrorOr;

using ReSys.Core.Domain.Payments.Providers.Models;

namespace ReSys.Core.Domain.Payments.Providers;

/// <summary>
/// Base interface for all payment gateway implementations.
/// Defines the contract for interacting with external payment processing services.
/// </summary>
public interface IPaymentGateway
{
    /// <summary>
    /// Initializes the payment gateway with the specific PaymentMethod configuration.
    /// This method is called by the factory to set up the gateway instance.
    /// </summary>
    /// <param name="paymentMethod">The PaymentMethod instance containing encrypted credentials and settings.</param>
    /// <param name="encryptor">The encryptor service to decrypt credentials.</param>
    void Initialize(PaymentMethod paymentMethod, IPaymentCredentialEncryptor encryptor);

    /// <summary>
    /// Authorizes a payment transaction with the gateway.
    /// </summary>
    Task<ErrorOr<PaymentResult>> AuthorizeAsync(
        GatewayAuthorizationRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures previously authorized funds.
    /// </summary>
    Task<ErrorOr<PaymentResult>> CaptureAsync(
        GatewayCaptureRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a previously authorized (but not captured) payment.
    /// </summary>
    Task<ErrorOr<PaymentResult>> VoidAsync(
        GatewayVoidRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a captured payment.
    /// </summary>
    Task<ErrorOr<PaymentResult>> RefundAsync(
        GatewayRefundRequest request,
        CancellationToken cancellationToken = default);
}