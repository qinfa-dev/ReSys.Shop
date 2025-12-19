namespace ReSys.Core.Feature.Common.Payments.Models;

public record AuthorizationRequest
{
    public decimal Amount { get; init; } // Changed to decimal
    public string Currency { get; init; } = string.Empty;
    public string PaymentToken { get; init; } = string.Empty;
    public string OrderNumber { get; init; } = string.Empty;
    public string? CustomerEmail { get; init; }
    public GatewayAddress? BillingAddress { get; init; }
    public GatewayAddress? ShippingAddress { get; init; }
    public string? IdempotencyKey { get; init; } // Added IdempotencyKey
    public Dictionary<string, string>? Metadata { get; init; }
}