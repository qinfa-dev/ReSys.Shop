using System.Security.Claims;
using System.Text.Json;

using AsyncKeyedLock;

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;

using ReSys.Core.Domain.Identity.Roles;
using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Constants;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Models;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Providers;
using ReSys.Infrastructure.Security.Authorization.Options;

using Serilog;

namespace ReSys.Infrastructure.Security.Authorization.Providers;

public sealed class AuthorizeClaimDataProvider(
    UserManager<User> userManager,
    RoleManager<Role> roleManager,
    IDistributedCache cache,
    IOptions<AuthUserCacheOption> authCacheOption)
    : IAuthorizeClaimDataProvider
{
    private static readonly AsyncKeyedLocker<string> UserLocks = new();

    private static readonly JsonSerializerOptions JsonOptions = new(defaults: JsonSerializerDefaults.Web)
    {
        PropertyNameCaseInsensitive = true
    };

    private readonly AuthUserCacheOption _jwtOptions = authCacheOption.Value;
    private const string RoleClaimsCacheKey = "AllRoleClaims";

    public async Task<AuthorizeClaimData?> GetUserAuthorizationAsync(string userId)
    {
        string cacheKey = $"UserAuth_{userId}";

        if (await TryGetCachedAuthAsync(cacheKey: cacheKey) is { } cached)
            return cached;

        using (await UserLocks.LockAsync(key: userId))
        {
            if (await TryGetCachedAuthAsync(cacheKey: cacheKey) is { } rechecked)
                return rechecked;

            return await FetchAndCacheAuthData(userId: userId,
                cacheKey: cacheKey);
        }
    }

    private async ValueTask<AuthorizeClaimData?> TryGetCachedAuthAsync(string cacheKey)
    {
        try
        {
            string? cachedData = await cache.GetStringAsync(key: cacheKey).ConfigureAwait(continueOnCapturedContext: false);
            return string.IsNullOrEmpty(value: cachedData)
                ? null
                : JsonSerializer.Deserialize<AuthorizeClaimData>(json: cachedData,
                    options: JsonOptions);
        }
        catch (Exception ex)
        {
            Log.Warning(exception: ex,
                messageTemplate: "Cache retrieval failed for {Key}",
                propertyValue: cacheKey);
            _ = SafeCacheRemoveAsync(key: cacheKey); // fire-and-forget cleanup
            return null;
        }
    }

    private async Task<AuthorizeClaimData?> FetchAndCacheAuthData(string userId, string cacheKey)
    {
        User? user = await userManager.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(predicate: u => u.Id == userId);

        if (user is null)
        {
            Log.Warning(messageTemplate: "User not found: {UserId}",
                propertyValue: userId);
            return null;
        }

        (IList<string> roles, List<Claim> roleClaims) = await GetRolesAndClaimsAsync(user: user);

        AuthorizeClaimData authData = new(
            UserId: userId,
            UserName: user.UserName ?? string.Empty,
            Email: user.Email ?? string.Empty,
            Permissions: GetDistinctValues(claims: roleClaims,
                claimType: CustomClaim.Permission),
            Roles: roles.ToList().AsReadOnly(),
            Policies: GetDistinctValues(claims: roleClaims,
                claimType: CustomClaim.Policy));

        await CacheAuthData(cacheKey: cacheKey,
            data: authData);
        return authData;
    }

    private async Task<(IList<string> Roles, List<Claim> Claims)> GetRolesAndClaimsAsync(User user)
    {
        IList<string> roleNames = await userManager.GetRolesAsync(user: user);
        if (roleNames.Count == 0)
            return (roleNames, []);

        // Try to load role claims mapping from cache
        string? cachedJson = await cache.GetStringAsync(key: RoleClaimsCacheKey);
        Dictionary<string, List<Claim>>? roleClaimsMap = null;

        if (!string.IsNullOrEmpty(value: cachedJson))
        {
            try
            {
                roleClaimsMap = JsonSerializer.Deserialize<Dictionary<string, List<Claim>>>(json: cachedJson,
                    options: JsonOptions);
            }
            catch (Exception ex)
            {
                Log.Warning(exception: ex,
                    messageTemplate: "Failed to deserialize cached role claims, refreshing...");
                await SafeCacheRemoveAsync(key: RoleClaimsCacheKey);
            }
        }

        // Refresh role claims cache if missing
        if (roleClaimsMap is null)
        {
            List<Role> roles = await roleManager.Roles.AsNoTracking().ToListAsync();
            roleClaimsMap = new Dictionary<string, List<Claim>>(capacity: roles.Count);

            foreach (Role role in roles)
            {
                IList<Claim> claimsForRole = await roleManager.GetClaimsAsync(role: role); // Renamed variable
                roleClaimsMap[key: role.Name!] = [.. claimsForRole];
            }

            try
            {
                string serialized = JsonSerializer.Serialize(value: roleClaimsMap,
                    options: JsonOptions);
                DistributedCacheEntryOptions options = new()
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes: _jwtOptions.RoleClaimsCacheExpiryInMinutes),
                    SlidingExpiration = TimeSpan.FromMinutes(minutes: _jwtOptions.RoleClaimsCacheExpiryInMinutes / 2)
                };
                await cache.SetStringAsync(key: RoleClaimsCacheKey,
                    value: serialized,
                    options: options);
            }
            catch (Exception ex)
            {
                Log.Warning(exception: ex,
                    messageTemplate: "Failed to cache role claims");
            }
        }

        // Collect all claims from roles
        List<Claim> roleClaims = roleNames
            .Where(predicate: roleClaimsMap.ContainsKey)
            .SelectMany(selector: r => roleClaimsMap[key: r])
            .ToList();

        // Collect claims directly assigned to the user
        IList<Claim> userClaims = await userManager.GetClaimsAsync(user: user);

        // Combine role claims and user claims
        List<Claim> allClaims = new(capacity: roleClaims.Count + userClaims.Count);
        allClaims.AddRange(collection: roleClaims);
        allClaims.AddRange(collection: userClaims);

        return (roleNames, allClaims);
    }

    private static IReadOnlyList<string> GetDistinctValues(IEnumerable<Claim> claims, string claimType) =>
        claims
            .Where(predicate: c => c.Type.ToLower() == claimType && !string.IsNullOrEmpty(value: c.Value))
            .Select(selector: c => c.Value)
            .Distinct()
            .ToList()
            .AsReadOnly();

    private async Task CacheAuthData(string cacheKey, AuthorizeClaimData data)
    {
        try
        {
            string serialized = JsonSerializer.Serialize(value: data,
                options: JsonOptions);
            DistributedCacheEntryOptions options = new()
            {
                AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(minutes: _jwtOptions.UserAuthCacheExpiryInMinutes),
                SlidingExpiration = TimeSpan.FromMinutes(minutes: _jwtOptions.UserAuthCacheSlidingInMinutes)
            };

            await cache.SetStringAsync(key: cacheKey,
                value: serialized,
                options: options);
        }
        catch (Exception ex)
        {
            Log.Warning(exception: ex,
                messageTemplate: "Caching failed for {Key}",
                propertyValue: cacheKey);
        }
    }

    public async Task InvalidateUserAuthorizationAsync(string userId)
    {
        string cacheKey = $"UserAuth_{userId}";
        await SafeCacheRemoveAsync(key: cacheKey);
        Log.Information(messageTemplate: "Cache invalidated for {UserId}",
            propertyValue: userId);
    }

    private async Task SafeCacheRemoveAsync(string key)
    {
        try
        {
            await cache.RemoveAsync(key: key);
        }
        catch (Exception ex)
        {
            Log.Warning(exception: ex,
                messageTemplate: "Cache removal failed for {Key}",
                propertyValue: key);
        }
    }
}
