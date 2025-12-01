namespace ReSys.Core.Domain.Payments.Providers.Models;

/// <summary>
/// Request model for refunding a captured payment via a gateway.
/// </summary>
public record GatewayRefundRequest
{
    public string TransactionId { get; init; } = string.Empty;
    public long Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
    public string? Reason { get; init; }
}