using ReSys.Core.Feature.Common.Security.Authorization.Claims.Models;

namespace ReSys.Core.Feature.Common.Security.Authorization.Claims.Providers;

public interface IClaimAuthorizeDataProvider
{
    Task<ClaimAuthorizeData?> GetUserAuthorizationAsync(string userId);
    Task InvalidateUserAuthorizationAsync(string userId);
}