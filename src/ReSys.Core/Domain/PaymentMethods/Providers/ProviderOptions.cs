namespace ReSys.Core.Domain.PaymentMethods.Providers;

/// <summary>
/// Base class for payment provider-specific configuration options.
/// Concrete providers will inherit from this to define their unique settings (e.g., API keys, endpoints).
/// </summary>
public abstract class ProviderOptions
{
    /// <summary>
    /// Gets or sets the API key for the payment provider.
    /// </summary>
    public string? ApiKey { get; set; }

    /// <summary>
    /// Gets or sets the secret key for the payment provider.
    /// </summary>
    public string? SecretKey { get; set; }

    /// <summary>
    /// Gets or sets the webhook secret for the payment provider.
    /// </summary>
    public string? WebhookSecret { get; set; }

    // Add other common options here if needed, or leave to concrete implementations
}
