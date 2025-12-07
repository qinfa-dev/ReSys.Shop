namespace ReSys.Core.Domain.PaymentMethods.Providers.Models;

public enum PaymentStatus
{
    Undefined = 0,
    Succeeded = 1,
    Failed = 2,
    Pending = 3,
    RequiresAction = 4, // e.g., 3D Secure authentication
    Authorized = 5,
    Captured = 6,
    Voided = 7,
    Refunded = 8,
    PartiallyRefunded = 9
}