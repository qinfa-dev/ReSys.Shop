namespace ReSys.Core.Domain.PaymentMethods.Providers;

/// <summary>
/// Configuration options for the Stripe payment provider.
/// </summary>
public class StripeProviderOptions : ProviderOptions
{
    /// <summary>
    /// Gets or sets the Stripe publishable key.
    /// </summary>
    public string? PublishableKey { get; set; }

    // Other Stripe-specific options can be added here
}
