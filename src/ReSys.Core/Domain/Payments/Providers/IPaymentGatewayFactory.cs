using ErrorOr;

namespace ReSys.Core.Domain.Payments.Providers;

/// <summary>
/// Factory interface for creating and retrieving concrete IPaymentGateway implementations.
/// </summary>
public interface IPaymentGatewayFactory
{
    /// <summary>
    /// Gets an initialized IPaymentGateway instance for the given PaymentMethod.
    /// The factory is responsible for selecting the correct gateway type and initializing it
    /// with the PaymentMethod's credentials and settings.
    /// </summary>
    /// <param name="paymentMethod">The PaymentMethod containing the configuration for the desired gateway.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{T}"/> of <see cref="IPaymentGateway"/> containing the initialized gateway instance on success, or errors.</returns>
    Task<ErrorOr<IPaymentGateway>> GetGatewayAsync(
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);
}
