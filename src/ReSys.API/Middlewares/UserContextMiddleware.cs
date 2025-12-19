using Microsoft.EntityFrameworkCore;

using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;
using ReSys.Core.Domain.Stores;
using ReSys.Core.Feature.Common.Persistence.Interfaces;

namespace ReSys.API.Middlewares;

public sealed class UserContextMiddleware(RequestDelegate next)
{
    private const string AdhocSessionCookieName = "ReSys.Adhoc.SessionId";

    public async Task InvokeAsync(
        HttpContext context,
        IUserContext userContext,
        IApplicationDbContext dbContext)
    {
        HandleAdhocUser(context, userContext);

        var store = await ResolveStoreAsync(context, dbContext);
        userContext.SetStoreId(store.Id);

        await next(context);
    }

    private static void HandleAdhocUser(HttpContext context, IUserContext userContext)
    {
        if (context.Request.Cookies.TryGetValue(AdhocSessionCookieName, out var adhocId) &&
            !string.IsNullOrWhiteSpace(adhocId))
        {
            userContext.SetAdhocId(adhocId);
            return;
        }

        var newAdhocId = Guid.NewGuid().ToString("N");
        userContext.SetAdhocId(newAdhocId);

        context.Response.Cookies.Append(
            AdhocSessionCookieName,
            newAdhocId,
            new CookieOptions
            {
                HttpOnly = true,
                IsEssential = true,
                SameSite = SameSiteMode.Lax,
                Expires = DateTimeOffset.UtcNow.AddDays(60)
            });
    }

    private static async Task<Store> ResolveStoreAsync(
        HttpContext context,
        IApplicationDbContext dbContext)
    {
        var host = context.Request.Host.Host.ToLowerInvariant();

        // 1. Try resolve by site / URL
        var store = await dbContext.Set<Store>()
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Available)
            .FirstOrDefaultAsync(s => s.Url == host);

        if (store is not null)
            return store;

        // 2. Fallback to default store
        store = await dbContext.Set<Store>()
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Default)
            .FirstOrDefaultAsync();

        if (store is not null)
            return store;

        // 3. Absolute fallback (first available store)
        return await dbContext.Set<Store>()
            .AsNoTracking()
            .Where(s => !s.IsDeleted && s.Available)
            .OrderBy(s => s.CreatedAt)
            .FirstAsync();
    }
}
