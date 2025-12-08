using ErrorOr;

using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;

using ReSys.Core.Domain.Identity.Users;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Interfaces;
using ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;

namespace ReSys.Infrastructure.Security.Authentication.Externals.Services;

/// <summary>
/// Service for managing external user authentication and integration with Identity EF Core
/// </summary>
public sealed class ExternalUserService(
    UserManager<User> userManager,
    ILogger<ExternalUserService> logger)
    : IExternalUserService
{
    // Supported external providers for production e-commerce
    private static readonly HashSet<string> SupportedProviders = new(comparer: StringComparer.OrdinalIgnoreCase)
    {
        "google",
        "facebook"
    };

    /// <summary>
    /// Finds or creates a user based on external authentication information
    /// Handles all Identity EF Core operations for external logins
    /// </summary>
    public async Task<ErrorOr<(User User, bool IsNewUser, bool IsNewLogin)>> FindOrCreateUserWithExternalLoginAsync(
        ExternalUserTransfer externalUserInfo,
        string provider,
        CancellationToken cancellationToken = default)
    {
        // Validate provider is supported
        if (!SupportedProviders.Contains(item: provider))
        {
            logger.LogWarning(message: "Attempted to use unsupported provider: {Provider}",
                args: provider);
            return Error.Validation(code: "Provider.NotSupported",
                description: $"Provider '{provider}' is not supported");
        }

        try
        {
            // Step 1: Try to find user by existing external login
            User? existingUser = await FindUserByExternalLoginAsync(provider: provider,
                providerKey: externalUserInfo.ProviderId);
            if (existingUser != null)
            {
                logger.LogDebug(message: "Found existing user {UserId} with external login {Provider}:{ProviderId}",
                    args:
                    [
                        existingUser.Id,
                        provider,
                        externalUserInfo.ProviderId
                    ]);

                await UpdateUserFromExternalInfoAsync(user: existingUser,
                    externalUserTransfer: externalUserInfo);
                return (existingUser, IsNewUser: false, IsNewLogin: false);
            }

            // Step 2: Try to find user by email and link the external login
            if (!string.IsNullOrWhiteSpace(value: externalUserInfo.Email) &&
                !externalUserInfo.Email.EndsWith(value: "@facebook.local") &&
                !externalUserInfo.Email.EndsWith(value: "@google.local"))
            {
                User? userByEmail = await FindUserByEmailAsync(email: externalUserInfo.Email);
                if (userByEmail != null)
                {
                    logger.LogDebug(
                        message: "Found existing user {UserId} by email, linking external login {Provider}:{ProviderId}",
                        args:
                        [
                            userByEmail.Id,
                            provider,
                            externalUserInfo.ProviderId
                        ]);

                    ErrorOr<Success> linkResult = await LinkExternalLoginToUserAsync(user: userByEmail,
                        provider: provider,
                        externalUserInfo: externalUserInfo);
                    if (linkResult.IsError)
                    {
                        return linkResult.Errors;
                    }

                    await UpdateUserFromExternalInfoAsync(user: userByEmail,
                        externalUserTransfer: externalUserInfo);
                    return (userByEmail, IsNewUser: false, IsNewLogin: true);
                }
            }

            // Step 3: Create new user with external login
            logger.LogDebug(message: "Creating new user for external login {Provider}:{ProviderId}",
                args:
                [
                    provider,
                    externalUserInfo.ProviderId
                ]);

            ErrorOr<User> createResult = await CreateUserWithExternalLoginAsync(
                externalUserInfo: externalUserInfo,
                provider: provider);
            if (createResult.IsError)
            {
                return createResult.Errors;
            }

            return (createResult.Value, IsNewUser: true, IsNewLogin: true);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error during external user management for {Provider}:{ProviderId}",
                args:
                [
                    provider,
                    externalUserInfo.ProviderId
                ]);
            return Error.Failure(code: "ExternalUser.ManagementError",
                description: "Failed to manage external user authentication");
        }
    }

    /// <summary>
    /// Checks if a user already has an external login for a specific provider
    /// </summary>
    public async Task<bool> HasExternalLoginAsync(string userId, string provider, CancellationToken cancellationToken = default)
    {
        try
        {
            User? user = await userManager.FindByIdAsync(userId: userId);
            if (user == null)
                return false;

            IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user: user);
            return logins.Any(predicate: l => l.LoginProvider.Equals(value: provider,
                comparisonType: StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error checking external login for user {UserId} and provider {Provider}",
                args:
                [
                    userId,
                    provider
                ]);
            return false;
        }
    }

    /// <summary>
    /// Gets all external logins for a user
    /// </summary>
    public async Task<IList<UserLoginInfo>> GetExternalLoginsAsync(string userId, CancellationToken cancellationToken = default)
    {
        try
        {
            User? user = await userManager.FindByIdAsync(userId: userId);
            if (user == null)
                return new List<UserLoginInfo>();

            return await userManager.GetLoginsAsync(user: user);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error getting external logins for user {UserId}",
                args: userId);
            return new List<UserLoginInfo>();
        }
    }

    /// <summary>
    /// Removes an external login from a user (with safety checks)
    /// </summary>
    public async Task<ErrorOr<Success>> RemoveExternalLoginAsync(
        string userId,
        string provider,
        string providerKey,
        CancellationToken cancellationToken = default)
    {
        try
        {
            User? user = await userManager.FindByIdAsync(userId: userId);
            if (user == null)
            {
                return Error.NotFound(code: "User.NotFound",
                    description: "User not found");
            }

            // Safety check: don't allow removal of last login method if user has no password
            bool hasPassword = await userManager.HasPasswordAsync(user: user);
            IList<UserLoginInfo> logins = await userManager.GetLoginsAsync(user: user);

            if (!hasPassword && logins.Count <= 1)
            {
                logger.LogWarning(message: "Attempted to remove last external login for user {UserId} without password",
                    args: userId);
                return Error.Validation(code: "ExternalLogin.CannotRemoveLast",
                    description: "Cannot remove the last external login. Set a password first or add another external login.");
            }

            IdentityResult result = await userManager.RemoveLoginAsync(user: user,
                loginProvider: provider,
                providerKey: providerKey);
            if (!result.Succeeded)
            {
                logger.LogError(message: "Failed to remove external login for user {UserId}: {Errors}",
                    args:
                    [
                        userId,
                        string.Join(separator: ", ",
                            values: result.Errors.Select(selector: e => e.Description))
                    ]);
                return Error.Failure(code: "ExternalLogin.RemovalFailed",
                    description: "Failed to remove external login");
            }

            logger.LogInformation(
                message: "Successfully removed external login {Provider}:{ProviderKey} for user {UserId}",
                args:
                [
                    provider,
                    providerKey,
                    userId
                ]);

            return Result.Success;
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error removing external login for user {UserId}",
                args: userId);
            return Error.Failure(code: "ExternalLogin.RemovalError",
                description: "Error occurred while removing external login");
        }
    }

    #region Private Helper Methods

    private async Task<User?> FindUserByExternalLoginAsync(
        string provider,
        string providerKey)
    {
        try
        {
            return await userManager.FindByLoginAsync(loginProvider: provider,
                providerKey: providerKey);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error finding user by external login {Provider}:{ProviderKey}",
                args:
                [
                    provider,
                    providerKey
                ]);
            return null;
        }
    }

    private async Task<User?> FindUserByEmailAsync(string email)
    {
        try
        {
            return await userManager.FindByEmailAsync(email: email);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Error finding user by email {Email}",
                args: email);
            return null;
        }
    }

    private async Task<ErrorOr<Success>> LinkExternalLoginToUserAsync(
        User user,
        string provider,
        ExternalUserTransfer externalUserInfo)
    {
        UserLoginInfo externalLoginInfo = new(
            loginProvider: provider,
            providerKey: externalUserInfo.ProviderId,
            displayName: GetProviderDisplayName(provider: provider));

        IdentityResult result = await userManager.AddLoginAsync(user: user,
            login: externalLoginInfo);
        if (!result.Succeeded)
        {
            logger.LogError(message: "Failed to link external login to existing user {Email}: {Errors}",
                args:
                [
                    user.Email,
                    string.Join(separator: ", ",
                        values: result.Errors.Select(selector: e => e.Description))
                ]);
            return Error.Failure(code: "ExternalLogin.LinkFailed",
                description: "Failed to link external login to existing user");
        }

        logger.LogInformation(message: "Successfully linked external login {Provider}:{ProviderId} to user {UserId}",
            args:
            [
                provider,
                externalUserInfo.ProviderId,
                user.Id
            ]);

        return Result.Success;
    }

    private async Task<ErrorOr<User>> CreateUserWithExternalLoginAsync(
        ExternalUserTransfer externalUserInfo,
        string provider)
    {
        // Create new user with external information
        var newUserResult = User.Create(
            email: externalUserInfo.Email,
            userName: GenerateUsername(externalUserInfo: externalUserInfo),
            emailConfirmed: externalUserInfo.EmailVerified,
            firstName: externalUserInfo.FirstName ?? ExtractFirstNameFromEmail(email: externalUserInfo.Email),
            lastName: externalUserInfo.LastName);

        if (newUserResult.IsError)
            return newUserResult.Errors;
        var newUser = newUserResult.Value;

        // Create the user
        IdentityResult createResult = await userManager.CreateAsync(user: newUser);
        if (!createResult.Succeeded)
        {
            logger.LogError(message: "Failed to create user from external token {Email}: {Errors}",
                args:
                [
                    externalUserInfo.Email,
                    string.Join(separator: ", ",
                        values: createResult.Errors.Select(selector: e => e.Description))
                ]);
            return Error.Failure(code: "User.CreationFailed",
                description: "Failed to create user from external authentication");
        }

        // Add external login to the new user
        UserLoginInfo externalLoginInfo = new(
            loginProvider: provider,
            providerKey: externalUserInfo.ProviderId,
            displayName: GetProviderDisplayName(provider: provider));

        IdentityResult addLoginResult = await userManager.AddLoginAsync(user: newUser,
            login: externalLoginInfo);
        if (!addLoginResult.Succeeded)
        {
            // Rollback: delete the created user if adding login fails
            await userManager.DeleteAsync(user: newUser);
            logger.LogError(message: "Failed to add external login to new user {Email}: {Errors}",
                args:
                [
                    externalUserInfo.Email,
                    string.Join(separator: ", ",
                        values: addLoginResult.Errors.Select(selector: e => e.Description))
                ]);
            return Error.Failure(code: "ExternalLogin.AdditionFailed",
                description: "Failed to add external login to new user");
        }

        logger.LogInformation(message: "Created new user {UserId} from external token via {Provider}",
            args:
            [
                newUser.Id,
                provider
            ]);

        return newUser;
    }

    private async Task UpdateUserFromExternalInfoAsync(
     User user,
     ExternalUserTransfer externalUserTransfer)
    {
        bool requiresUpdate = false;

        // Determine new values (only change what�s provided)
        string? newFirstName = null;
        string? newLastName = null;
        string? newProfileImagePath = null;
        bool emailConfirmed = user.EmailConfirmed;

        // Update first name if changed
        if (!string.IsNullOrWhiteSpace(value: externalUserTransfer.FirstName) &&
            !string.Equals(a: user.FirstName,
                b: externalUserTransfer.FirstName,
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            newFirstName = externalUserTransfer.FirstName;
            requiresUpdate = true;
        }

        // Update last name if changed
        if (!string.IsNullOrWhiteSpace(value: externalUserTransfer.LastName) &&
            !string.Equals(a: user.LastName,
                b: externalUserTransfer.LastName,
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            newLastName = externalUserTransfer.LastName;
            requiresUpdate = true;
        }

        // Update profile image if changed
        if (!string.IsNullOrWhiteSpace(value: externalUserTransfer.ProfilePictureUrl) &&
            !string.Equals(a: user.ProfileImagePath,
                b: externalUserTransfer.ProfilePictureUrl,
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            newProfileImagePath = externalUserTransfer.ProfilePictureUrl;
            requiresUpdate = true;
        }

        // Mark email as confirmed if the external provider verified it
        if (externalUserTransfer.EmailVerified && !user.EmailConfirmed)
        {
            emailConfirmed = true;
            requiresUpdate = true;
        }

        if (!requiresUpdate)
        {
            logger.LogDebug(message: "No profile updates needed for user {UserId} from external info.",
                args: user.Id);
            return;
        }

        // Execute domain update � ensures invariants are respected
        var updateResult = user.Update(
            firstName: newFirstName ?? user.FirstName,
            lastName: newLastName ?? user.LastName,
            profileImagePath: newProfileImagePath ?? user.ProfileImagePath,
            emailConfirmed: emailConfirmed
        );

        if (updateResult.IsError)
        {
            logger.LogWarning(message: "Domain update failed for user {UserId}: {Errors}",
                args:
                [
                    user.Id,
                    string.Join(separator: ", ",
                        values: updateResult.Errors.Select(selector: e => e.Description))
                ]);
            return;
        }

        // Persist changes via the identity manager
        var identityResult = await userManager.UpdateAsync(user: updateResult.Value);
        if (!identityResult.Succeeded)
        {
            logger.LogWarning(message: "Failed to persist user {UserId} update from external info: {Errors}",
                args:
                [
                    user.Id,
                    string.Join(separator: ", ",
                        values: identityResult.Errors.Select(selector: e => e.Description))
                ]);
            return;
        }

        logger.LogInformation(message: "User {UserId} updated from external provider information ({Provider}).",
            args:
            [
                user.Id,
                externalUserTransfer.ProviderName
            ]);
    }


    private static string GenerateUsername(ExternalUserTransfer externalUserInfo)
    {
        // Use email as username if available, otherwise generate from provider info
        if (!string.IsNullOrWhiteSpace(value: externalUserInfo.Email) &&
            !externalUserInfo.Email.EndsWith(value: ".local"))
        {
            return externalUserInfo.Email;
        }

        // Fallback: generate username from name or provider ID
        string baseName = !string.IsNullOrWhiteSpace(value: externalUserInfo.FirstName)
            ? externalUserInfo.FirstName.ToLowerInvariant()
            : "user";

        return $"{baseName}_{externalUserInfo.ProviderId[..Math.Min(val1: 8, val2: externalUserInfo.ProviderId.Length)]}";
    }

    private static string ExtractFirstNameFromEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(value: email))
            return "User";

        string localPart = email.Split(separator: '@')[0];

        // Remove numbers and special characters, capitalize first letter
        string cleanName = new(value: localPart.Where(predicate: char.IsLetter).ToArray());

        return string.IsNullOrEmpty(value: cleanName)
            ? "User"
            : char.ToUpperInvariant(c: cleanName[index: 0]) + cleanName[1..].ToLowerInvariant();
    }

    private static string GetProviderDisplayName(string provider) =>
        provider.ToLowerInvariant() switch
        {
            "google" => "Google",
            "facebook" => "Facebook",
            _ => provider
        };

    #endregion
}