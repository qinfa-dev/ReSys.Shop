namespace ReSys.Core.Domain.Payments.Providers.Models;

public record PaymentResult
{
    public bool Success { get; init; }
    public string TransactionId { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string? ErrorMessage { get; init; }
    public string? ErrorCode { get; init; }
    public Dictionary<string, string>? RawResponse { get; init; }
}
