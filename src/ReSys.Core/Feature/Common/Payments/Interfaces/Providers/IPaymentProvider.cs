using ReSys.Core.Feature.Common.Payments.Models;

namespace ReSys.Core.Feature.Common.Payments.Interfaces.Providers;

/// <summary>
/// Comprehensive interface for all payment provider implementations.
/// Defines the contract for interacting with external payment processing services
/// using standardized request and response models.
/// </summary>
public interface IPaymentProvider
{
    /// <summary>
    /// Authorizes a payment transaction with the provider.
    /// </summary>
    /// <param name="request">The authorization request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{ProviderResponse}"/> indicating the outcome of the authorization.</returns>
    Task<ErrorOr<ProviderResponse>> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Captures previously authorized funds.
    /// </summary>
    /// <param name="request">The capture request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{ProviderResponse}"/> indicating the outcome of the capture.</returns>
    Task<ErrorOr<ProviderResponse>> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Voids a previously authorized (but not captured) payment.
    /// </summary>
    /// <param name="request">The void request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{ProviderResponse}"/> indicating the outcome of the void operation.</returns>
    Task<ErrorOr<ProviderResponse>> VoidAsync(VoidRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Refunds a captured payment.
    /// </summary>
    /// <param name="request">The refund request details.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{ProviderResponse}"/> indicating the outcome of the refund.</returns>
    Task<ErrorOr<ProviderResponse>> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default);
}
