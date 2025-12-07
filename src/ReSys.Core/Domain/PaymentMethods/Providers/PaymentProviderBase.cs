using ErrorOr;
using System.Threading;
using System.Threading.Tasks;
using ReSys.Core.Domain.PaymentMethods.Providers.Models;

namespace ReSys.Core.Domain.PaymentMethods.Providers;

/// <summary>
/// Base class for all payment provider implementations.
/// Provides a common foundation and handles shared logic, reducing boilerplate for concrete providers.
/// </summary>
/// <typeparam name="TProviderOptions">The type of provider-specific configuration options.</typeparam>
public abstract class PaymentProviderBase<TProviderOptions> : IPaymentProvider
    where TProviderOptions : class, new()
{
    protected readonly TProviderOptions _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="PaymentProviderBase{TProviderOptions}"/> class.
    /// </summary>
    /// <param name="options">The provider-specific configuration options.</param>
    protected PaymentProviderBase(TProviderOptions options)
    {
        _options = options;
    }

    public abstract Task<ErrorOr<ProviderResponse>> AuthorizeAsync(AuthorizationRequest request, CancellationToken cancellationToken = default);
    public abstract Task<ErrorOr<ProviderResponse>> CaptureAsync(CaptureRequest request, CancellationToken cancellationToken = default);
    public abstract Task<ErrorOr<ProviderResponse>> RefundAsync(RefundRequest request, CancellationToken cancellationToken = default);
    public abstract Task<ErrorOr<ProviderResponse>> VoidAsync(VoidRequest request, CancellationToken cancellationToken = default);

    // Common helper methods can be added here, e.g., for logging, common error mapping, etc.
}