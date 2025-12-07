namespace ReSys.Core.Domain.PaymentMethods.Providers.Models;

/// <summary>
/// Represents an address structure for gateway requests.
/// </summary>
public record GatewayAddress
{
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public string? Company { get; init; }
    public string? AddressLine1 { get; init; }
    public string? AddressLine2 { get; init; }
    public string? City { get; init; }
    public string? State { get; init; }
    public string? PostalCode { get; init; }
    public string? Country { get; init; }
    public string? Phone { get; init; }
}