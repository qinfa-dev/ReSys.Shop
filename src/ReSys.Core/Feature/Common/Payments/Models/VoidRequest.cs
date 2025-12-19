namespace ReSys.Core.Feature.Common.Payments.Models;

public record VoidRequest
{
    public string TransactionId { get; init; } = string.Empty;
    public string? IdempotencyKey { get; init; } // Added IdempotencyKey
}