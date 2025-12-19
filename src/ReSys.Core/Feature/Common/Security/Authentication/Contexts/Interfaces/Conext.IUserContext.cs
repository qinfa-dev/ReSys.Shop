namespace ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

public interface IUserContext
{
    string? UserId { get; }
    string? AdhocCustomerId { get; } 
    Guid? StoreId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
    void SetAdhocCustomerId(string adhocCustomerId); 
    void SetStoreId(Guid storeId); 
}
