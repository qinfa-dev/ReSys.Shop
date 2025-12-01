namespace ReSys.Core.Feature.Common.Security.Authorization.Providers;

public interface IUserAuthorizationProvider
{
    Task<UserAuthorizationData?> GetUserAuthorizationAsync(string userId);
    Task InvalidateUserAuthorizationAsync(string userId);
}