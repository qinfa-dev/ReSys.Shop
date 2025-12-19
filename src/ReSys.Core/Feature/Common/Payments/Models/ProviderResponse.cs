namespace ReSys.Core.Feature.Common.Payments.Models;

public record ProviderResponse(
    PaymentStatus Status,
    string TransactionId,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? RawResponse = null
);