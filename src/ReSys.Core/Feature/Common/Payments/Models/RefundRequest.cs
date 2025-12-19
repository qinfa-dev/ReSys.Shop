namespace ReSys.Core.Feature.Common.Payments.Models;

public record RefundRequest
{
    public string TransactionId { get; init; } = string.Empty;
    public decimal Amount { get; init; } // Changed to decimal
    public string Currency { get; init; } = string.Empty;
    public string? Reason { get; init; }
    public string? IdempotencyKey { get; init; } // Added IdempotencyKey
}