namespace ReSys.Core.Domain.Payments.Providers.Models;

/// <summary>
/// Request model for authorizing a payment via a gateway.
/// </summary>
public record GatewayAuthorizationRequest
{
    public long Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string PaymentToken { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public GatewayAddress? BillingAddress { get; init; }
    public GatewayAddress? ShippingAddress { get; init; }
    public Dictionary<string, string>? Metadata { get; init; }
}