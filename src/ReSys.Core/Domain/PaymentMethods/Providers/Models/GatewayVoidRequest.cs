namespace ReSys.Core.Domain.Payments.Providers.Models;

/// <summary>
/// Request model for voiding an authorized payment via a gateway.
/// </summary>
public record GatewayVoidRequest
{
    public string TransactionId { get; init; } = string.Empty;
}