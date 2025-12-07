using System.Collections.Generic;

namespace ReSys.Core.Domain.PaymentMethods.Providers.Models;

public record ProviderResponse(
    PaymentStatus Status,
    string TransactionId,
    string? ErrorCode = null,
    string? ErrorMessage = null,
    Dictionary<string, string>? RawResponse = null
);