namespace ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

public interface IUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
