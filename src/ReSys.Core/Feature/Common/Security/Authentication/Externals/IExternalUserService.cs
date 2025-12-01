using ErrorOr;

using Microsoft.AspNetCore.Identity;

using ReSys.Core.Domain.Identity.Users;

namespace ReSys.Core.Feature.Common.Security.Authentication.Externals;

/// <summary>
/// Interface for managing external user authentication and integration with Identity
/// </summary>
public interface IExternalUserService
{
    /// <summary>
    /// Finds or creates a user based on external authentication information
    /// Handles all Identity operations for external logins
    /// </summary>
    Task<ErrorOr<(ApplicationUser User, bool IsNewUser, bool IsNewLogin)>> FindOrCreateUserWithExternalLoginAsync(
        ExternalUserInfo externalUserInfo,
        string provider,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a user already has an external login for a specific provider
    /// </summary>
    Task<bool> HasExternalLoginAsync(string userId, string provider, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all external logins for a user
    /// </summary>
    Task<IList<UserLoginInfo>> GetExternalLoginsAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an external login from a user (with safety checks)
    /// </summary>
    Task<ErrorOr<Success>> RemoveExternalLoginAsync(
        string userId,
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default);
}