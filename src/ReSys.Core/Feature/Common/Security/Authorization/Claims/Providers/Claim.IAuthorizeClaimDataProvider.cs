using ReSys.Core.Feature.Common.Security.Authorization.Claims.Models;

namespace ReSys.Core.Feature.Common.Security.Authorization.Claims.Providers;

public interface IAuthorizeClaimDataProvider
{
    Task<AuthorizeClaimData?> GetUserAuthorizationAsync(string userId);
    Task InvalidateUserAuthorizationAsync(string userId);
}