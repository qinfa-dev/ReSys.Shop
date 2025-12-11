namespace ReSys.Core.Domain.PaymentMethods.Providers;

/// <summary>
/// Factory interface for creating and retrieving concrete IPaymentGateway implementations.
/// </summary>
public interface IPaymentProviderFactory
{
    /// <summary>
    /// Gets an initialized IPaymentProvider instance for the given PaymentMethod.
    /// The factory is responsible for selecting the correct provider type and initializing it
    /// with the PaymentMethod's credentials and settings.
    /// </summary>
    /// <param name="paymentMethod">The PaymentMethod containing the configuration for the desired provider.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An <see cref="ErrorOr{T}"/> of <see cref="IPaymentProvider"/> containing the initialized provider instance on success, or errors.</returns>
    Task<ErrorOr<IPaymentProvider>> GetProviderAsync(
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default);
}
