using Microsoft.Extensions.DependencyInjection;

using ReSys.Core.Feature.Common.Payments.Interfaces.Encryptor;
using ReSys.Core.Feature.Common.Payments.Interfaces.Providers;
using ReSys.Core.Feature.Common.Payments.Interfaces.Webhooks;
using ReSys.Infrastructure.Payments.Services;
using ReSys.Infrastructure.Payments.Webhooks;

namespace ReSys.Infrastructure.Payments;

/// <summary>
/// Extension methods for registering payment providers in dependency injection.
/// </summary>
public static class PaymentProviderServiceExtensions
{
    public static IServiceCollection AddPaymentServices(this IServiceCollection services)
    {
        // Register payment provider services
        services.AddPaymentProviders();

        // Register credential encryptor
        services.AddSingleton<IPaymentCredentialEncryptor, AesCredentialEncryptor>();

        // Register webhook handlers
        services.AddScoped<IWebhookHandler, StripeWebhookHandler>();
        services.AddScoped<IWebhookHandler, PayPalWebhookHandler>();
        services.AddScoped<WebhookProcessor>();

        return services;
    }

    /// <summary>
    /// Registers payment provider services in the DI container.
    /// </summary>
    public static IServiceCollection AddPaymentProviders(this IServiceCollection services)
    {
        // Register the factory
        services.AddScoped<IPaymentProviderFactory, PaymentProviderFactory>();

        // Register HttpClient for PayPal
        services.AddHttpClient("PayPal", client =>
        {
            client.DefaultRequestHeaders.Add("Accept", "application/json");
            client.DefaultRequestHeaders.Add("Accept-Language", "en_US");
            client.Timeout = TimeSpan.FromSeconds(30);
        });

        return services;
    }
}