using Microsoft.AspNetCore.Http;

using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Extensions;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Infrastructure.Security.Authentication.Contexts;

public sealed class UserContext(IHttpContextAccessor httpContextAccessor) : IUserContext
{
    public string? UserId => httpContextAccessor.HttpContext?.User.GetUserId();

    public string? UserName => httpContextAccessor.HttpContext?.User.GetUserName();

    public bool IsAuthenticated =>
        httpContextAccessor.HttpContext?.User.Identity?.IsAuthenticated ?? false;
}
