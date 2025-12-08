using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Models;
using ReSys.Core.Feature.Common.Security.Authorization.Claims.Providers;

namespace ReSys.Infrastructure.Security.Authorization.Requirements;

/// <summary>
/// Authorization handler that validates user permissions, roles, and policies.
/// Provides detailed logging and comprehensive authorization logic.
/// </summary>
internal class HasAuthorizeClaimRequirementHandler(
    IServiceProvider serviceProvider,
    ILogger<HasAuthorizeClaimRequirementHandler> logger)
    : AuthorizationHandler<HasAuthorizeClaimRequirement>
{
    protected override async Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        HasAuthorizeClaimRequirement requirement)
    {
        try
        {
            IUserContext userContext = serviceProvider.GetRequiredService<IUserContext>();
            string? userId = userContext.UserId;

            // Check if user is authenticated
            if (!userContext.IsAuthenticated || string.IsNullOrWhiteSpace(value: userId))
            {
                logger.LogWarning(message: "Authorization failed: User not authenticated");
                context.Fail(reason: new AuthorizationFailureReason(handler: this,
                    message: "User not authenticated"));
                return;
            }

            logger.LogDebug(message: "Evaluating authorization for user {UserId}",
                args: userId);
            IAuthorizeClaimDataProvider authorizationProvider = serviceProvider.GetRequiredService<IAuthorizeClaimDataProvider>();
            AuthorizeClaimData? userAuthorization = await authorizationProvider.GetUserAuthorizationAsync(userId: userId);

            if (userAuthorization is null)
            {
                logger.LogWarning(message: "Authorization failed: User data not found for user {UserId}",
                    args: userId);
                context.Fail(reason: new AuthorizationFailureReason(handler: this,
                    message: "User authorization data not found"));
                return;
            }

            // Validate permissions
            if (!ValidatePermissions(requirement: requirement,
                    userAuthorization: userAuthorization,
                    userId: userId))
            {
                context.Fail(reason: new AuthorizationFailureReason(handler: this,
                    message: "Insufficient permissions"));
                return;
            }

            // Validate policies  
            if (!ValidatePolicies(requirement: requirement,
                    userAuthorization: userAuthorization,
                    userId: userId))
            {
                context.Fail(reason: new AuthorizationFailureReason(handler: this,
                    message: "Policy requirements not met"));
                return;
            }

            // Validate roles
            if (!ValidateRoles(requirement: requirement,
                    userAuthorization: userAuthorization,
                    userId: userId))
            {
                context.Fail(reason: new AuthorizationFailureReason(handler: this,
                    message: "Role requirements not met"));
                return;
            }

            logger.LogDebug(message: "Authorization succeeded for user {UserId}",
                args: userId);
            context.Succeed(requirement: requirement);
        }
        catch (Exception ex)
        {
            logger.LogError(exception: ex,
                message: "Unexpected error during authorization evaluation");
            context.Fail(reason: new AuthorizationFailureReason(handler: this,
                message: "Authorization evaluation failed"));
        }
    }

    /// <summary>
    /// Validates that the user has all required permissions.
    /// </summary>
    /// <param name="requirement">Authorization requirement</param>
    /// <param name="userAuthorization">User authorization data</param>
    /// <param name="userId">User ID for logging</param>
    /// <returns>True if all permissions are satisfied</returns>
    private bool ValidatePermissions(
        HasAuthorizeClaimRequirement requirement,
        AuthorizeClaimData userAuthorization,
        string userId)
    {
        if (requirement.Permissions.Length == 0)
            return true;

        logger.LogDebug(message: "Checking permissions {RequiredPermissions} for user {UserId}",
            args:
            [
                requirement.Permissions,
                userId
            ]);

        foreach (string requiredPermission in requirement.Permissions)
        {
            if (!userAuthorization.Permissions.Contains(value: requiredPermission))
            {
                logger.LogWarning(message: "User {UserId} missing required permission: {Permission}",
                    args:
                    [
                        userId,
                        requiredPermission
                    ]);
                return false;
            }
        }

        logger.LogDebug(message: "All permission requirements satisfied for user {UserId}",
            args: userId);
        return true;
    }

    /// <summary>
    /// Validates that the user satisfies all required policies.
    /// </summary>
    /// <param name="requirement">Authorization requirement</param>
    /// <param name="userAuthorization">User authorization data</param>
    /// <param name="userId">User ID for logging</param>
    /// <returns>True if all policies are satisfied</returns>
    private bool ValidatePolicies(
        HasAuthorizeClaimRequirement requirement,
        AuthorizeClaimData userAuthorization,
        string userId)
    {
        if (requirement.Policies.Length == 0)
            return true;

        logger.LogDebug(message: "Checking policies {RequiredPolicies} for user {UserId}",
            args:
            [
                requirement.Policies,
                userId
            ]);

        foreach (string requiredPolicy in requirement.Policies)
        {
            if (!userAuthorization.Policies.Contains(value: requiredPolicy))
            {
                logger.LogWarning(message: "User {UserId} does not satisfy required policy: {Policy}",
                    args:
                    [
                        userId,
                        requiredPolicy
                    ]);
                return false;
            }
        }

        logger.LogDebug(message: "All policy requirements satisfied for user {UserId}",
            args: userId);
        return true;
    }

    /// <summary>
    /// Validates that the user has all required roles.
    /// </summary>
    /// <param name="requirement">Authorization requirement</param>
    /// <param name="userAuthorization">User authorization data</param>
    /// <param name="userId">User ID for logging</param>
    /// <returns>True if all roles are satisfied</returns>
    private bool ValidateRoles(
        HasAuthorizeClaimRequirement requirement,
        AuthorizeClaimData userAuthorization,
        string userId)
    {
        if (requirement.Roles.Length == 0)
            return true;

        logger.LogDebug(message: "Checking roles {RequiredRoles} for user {UserId}",
            args:
            [
                requirement.Roles,
                userId
            ]);

        foreach (string requiredRole in requirement.Roles)
        {
            if (!userAuthorization.Roles.Contains(value: requiredRole))
            {
                logger.LogWarning(message: "User {UserId} missing required role: {Role}",
                    args:
                    [
                        userId,
                        requiredRole
                    ]);
                return false;
            }
        }

        logger.LogDebug(message: "All role requirements satisfied for user {UserId}",
            args: userId);
        return true;
    }
}
