namespace ReSys.Core.Feature.Common.Security.Authentication.Contexts.HttpContext;

public interface IUserContext
{
    string? UserId { get; }
    string? UserName { get; }
    bool IsAuthenticated { get; }
}
