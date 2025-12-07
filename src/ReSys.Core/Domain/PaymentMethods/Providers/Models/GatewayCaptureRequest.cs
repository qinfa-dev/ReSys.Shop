namespace ReSys.Core.Domain.Payments.Providers.Models;

/// <summary>
/// Request model for capturing an authorized payment via a gateway.
/// </summary>
public record GatewayCaptureRequest
{
    public string TransactionId { get; init; } = string.Empty;
    public long Amount { get; init; }
    public string Currency { get; init; } = string.Empty;
}