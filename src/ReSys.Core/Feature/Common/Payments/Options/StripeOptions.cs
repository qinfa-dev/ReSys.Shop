namespace ReSys.Core.Feature.Common.Payments.Options;

/// <summary>
/// Configuration options for the Stripe payment provider.
/// </summary>
public class StripeOptions : ProviderOptions
{
    /// <summary>
    /// Stripe Publishable Key.
    /// Can be stored in PublicMetadata.
    /// </summary>
    public string PublishableKey { get; set; } = string.Empty;

    /// <summary>
    /// Whether to automatically capture payments.
    /// </summary>
    public bool AutoCapture { get; set; } = false;

    /// <summary>
    /// A statement descriptor to be shown on the customer's bank statement.
    /// </summary>
    public string? StatementDescriptor { get; set; }

    /// <summary>
    /// Base URL for Stripe API (can be overridden for testing).
    /// </summary>
    public string BaseUrl { get; set; } = "https://api.stripe.com";
}