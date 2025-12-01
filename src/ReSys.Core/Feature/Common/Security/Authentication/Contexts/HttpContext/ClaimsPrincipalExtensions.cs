using System.Security.Claims;

namespace ReSys.Core.Feature.Common.Security.Authentication.Contexts.HttpContext;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal user)
    {
        return user.FindFirst(type: ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetUserName(this ClaimsPrincipal user)
    {
        return user?.FindFirst(type: ClaimTypes.Name)?.Value;
    }

    public static bool IsAuthenticated(this ClaimsPrincipal user)
    {
        return user?.Identity?.IsAuthenticated ?? false;
    }
}
