using ErrorOr;

using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Settings.PaymentMethods;
using ReSys.Core.Feature.Common.Payments.Interfaces.Encryptor;
using ReSys.Core.Feature.Common.Payments.Interfaces.Providers;
using ReSys.Core.Feature.Common.Payments.Options;

namespace ReSys.Infrastructure.Payments.Services;

/// <summary>
/// Factory for creating and initializing payment provider instances.
/// 
/// <para>
/// <strong>Responsibilities:</strong>
/// <list type="bullet">
/// <item><description>Maps PaymentMethod types to concrete provider implementations</description></item>
/// <item><description>Extracts and decrypts provider credentials from PaymentMethod metadata</description></item>
/// <item><description>Initializes providers with proper configuration</description></item>
/// <item><description>Validates provider setup before returning instances</description></item>
/// </list>
/// </para>
/// 
/// <para>
/// <strong>Security:</strong>
/// Uses IPaymentCredentialEncryptor to safely decrypt sensitive credentials
/// stored in PaymentMethod.PrivateMetadata before passing them to providers.
/// </para>
/// </summary>
public class PaymentProviderFactory(
    IPaymentCredentialEncryptor encryptor,
    IHttpClientFactory httpClientFactory,
    ILoggerFactory loggerFactory)
    : IPaymentProviderFactory
{
    /// <summary>
    /// Gets an initialized payment provider for the specified payment method.
    /// </summary>
    public async Task<ErrorOr<IPaymentProvider>> GetProviderAsync(
        PaymentMethod paymentMethod,
        CancellationToken cancellationToken = default)
    {
        try
        {
            await Task.CompletedTask;
            return paymentMethod.Type switch
            {
                PaymentMethod.PaymentType.CashOnDelivery => CreateCashOnDeliveryProvider(paymentMethod),
                PaymentMethod.PaymentType.Stripe => CreateStripeProvider(paymentMethod),
                PaymentMethod.PaymentType.PayPal => CreatePayPalProvider(paymentMethod),
                PaymentMethod.PaymentType.CreditCard => CreateStripeProvider(paymentMethod), 
                PaymentMethod.PaymentType.DebitCard => CreateStripeProvider(paymentMethod), 

                // Add other providers as they're implemented
                _ => Error.NotFound(
                    code: "PaymentProviderFactory.ProviderNotImplemented",
                    description: $"No provider implementation found for payment type: {paymentMethod.Type}")
            };
        }
        catch (Exception ex)
        {
            return Error.Failure(
                code: "PaymentProviderFactory.InitializationError",
                description: $"Failed to initialize payment provider: {ex.Message}");
        }
    }

    private static ErrorOr<IPaymentProvider> CreateCashOnDeliveryProvider(PaymentMethod paymentMethod)
    {
        var options = new CashOnDeliveryOptions();

        // Extract configuration from public metadata
        if (paymentMethod.PublicMetadata != null)
        {
            if (paymentMethod.PublicMetadata.TryGetValue("delivery_instructions", out var instructions))
            {
                options.DeliveryInstructions = instructions?.ToString();
            }

            if (paymentMethod.PublicMetadata.TryGetValue("require_signature", out var requireSig)
                && bool.TryParse(requireSig?.ToString(), out var requireSignature))
            {
                options.RequireSignature = requireSignature;
            }

            if (paymentMethod.PublicMetadata.TryGetValue("max_order_amount", out var maxAmount)
                && decimal.TryParse(maxAmount?.ToString(), out var max))
            {
                options.MaxOrderAmount = max;
            }

            if (paymentMethod.PublicMetadata.TryGetValue("handling_fee", out var fee)
                && decimal.TryParse(fee?.ToString(), out var handlingFee))
            {
                options.HandlingFee = handlingFee;
            }
        }

        return new CashOnDeliveryProvider(options);
    }

    private ErrorOr<IPaymentProvider> CreateStripeProvider(PaymentMethod paymentMethod)
    {
        var options = new StripeOptions();

        // Extract encrypted credentials from private metadata
        if (paymentMethod.PrivateMetadata == null)
        {
            return Error.Validation("Stripe.MissingCredentials", "Stripe requires credentials in PrivateMetadata");
        }

        if (!paymentMethod.PrivateMetadata.TryGetValue("secret_key_encrypted", out var encryptedSecretKey) || string.IsNullOrWhiteSpace(encryptedSecretKey?.ToString()))
        {
            return Error.Validation("Stripe.MissingSecretKey", "Stripe secret_key_encrypted not found or is empty in PrivateMetadata");
        }

        // Decrypt the secret key
        options.SecretKey = encryptor.Decrypt(encryptedSecretKey?.ToString() ?? "");

        // Extract public configuration
        if (paymentMethod.PublicMetadata != null)
        {
            if (paymentMethod.PublicMetadata.TryGetValue("publishable_key", out var pubKey))
            {
                options.PublishableKey = pubKey?.ToString() ?? "";
            }

            if (paymentMethod.PublicMetadata.TryGetValue("statement_descriptor", out var descriptor))
            {
                options.StatementDescriptor = descriptor?.ToString();
            }
        }

        // Extract webhook secret from private metadata if available
        if (paymentMethod.PrivateMetadata.TryGetValue("webhook_secret_encrypted", out var encryptedWebhook))
        {
            options.WebhookSecret = encryptor.Decrypt(encryptedWebhook?.ToString() ?? "");
        }

        options.AutoCapture = paymentMethod.AutoCapture;

        var logger = loggerFactory.CreateLogger<StripeProvider>();
        return new StripeProvider(options, logger);
    }

    private  ErrorOr<IPaymentProvider> CreatePayPalProvider(PaymentMethod paymentMethod)
    {
        var options = new PayPalOptions();

        // Extract encrypted credentials from private metadata
        if (paymentMethod.PrivateMetadata == null)
        {
            return Error.Validation("PayPal.MissingCredentials", "PayPal requires credentials in PrivateMetadata");
        }

        if (!paymentMethod.PrivateMetadata.TryGetValue("client_secret_encrypted", out var encryptedSecret) || string.IsNullOrWhiteSpace(encryptedSecret?.ToString()))
        {
            return Error.Validation("PayPal.MissingClientSecret", "PayPal client_secret_encrypted not found or is empty in PrivateMetadata");
        }

        // Decrypt the client secret
        options.ClientSecret = encryptor.Decrypt(encryptedSecret?.ToString() ?? "");

        // Extract public configuration
        if (paymentMethod.PublicMetadata != null)
        {
            if (paymentMethod.PublicMetadata.TryGetValue("client_id", out var clientId))
            {
                options.ClientId = clientId?.ToString() ?? "";
            }

            if (paymentMethod.PublicMetadata.TryGetValue("use_sandbox", out var sandbox)
                && bool.TryParse(sandbox?.ToString(), out var useSandbox))
            {
                options.UseSandbox = useSandbox;
            }

            if (paymentMethod.PublicMetadata.TryGetValue("brand_name", out var brand))
            {
                options.BrandName = brand?.ToString();
            }

            if (paymentMethod.PublicMetadata.TryGetValue("return_url", out var returnUrl))
            {
                options.ReturnUrl = returnUrl?.ToString() ?? "";
            }

            if (paymentMethod.PublicMetadata.TryGetValue("cancel_url", out var cancelUrl))
            {
                options.CancelUrl = cancelUrl?.ToString() ?? "";
            }
        }

        // Extract webhook ID from private metadata if available
        if (paymentMethod.PrivateMetadata.TryGetValue("webhook_id_encrypted", out var encryptedWebhookId))
        {
            options.WebhookId = encryptor.Decrypt(encryptedWebhookId?.ToString() ?? "");
        }

        options.AutoCapture = paymentMethod.AutoCapture;

        var logger = loggerFactory.CreateLogger<PayPalProvider>();
        return new PayPalProvider(options, logger, httpClientFactory);
    }
}