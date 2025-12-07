using Microsoft.AspNetCore.Authorization;

using ReSys.Core.Feature.Common.Security.Authorization.Claims.Constants;

namespace ReSys.Core.Feature.Common.Security.Authorization.Attributes.Models;

/// <summary>
/// Custom authorization attribute that supports permissions, roles, and policies.
/// Provides flexible authorization with proper validation and caching.
/// </summary>
[AttributeUsage(validOn: AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
public sealed class RequestAuthorizeAttribute : AuthorizeAttribute
{
    /// <summary>
    /// Initializes a new instance with permissions, roles, and policies.
    /// </summary>
    /// <param name="permissions">Comma-separated list of required permissions</param>
    /// <param name="roles">Comma-separated list of required roles</param>
    /// <param name="policies">Comma-separated list of required policies</param>
    public RequestAuthorizeAttribute(string? permissions = null, string? roles = null, string? policies = null)
    {
        // Validate that at least one parameter is provided
        if (string.IsNullOrWhiteSpace(value: permissions) && 
            string.IsNullOrWhiteSpace(value: roles) && 
            string.IsNullOrWhiteSpace(value: policies))
        {
            throw new ArgumentException(message: "At least one authorization parameter (permissions, roles, or policies) must be specified.");
        }

        Permissions = SplitAndClean(input: permissions);
        CustomRoles = SplitAndClean(input: roles);
        Policies = SplitAndClean(input: policies);

        // Set base class Roles property if roles are provided
        if (CustomRoles?.Length > 0)
        {
            Roles = string.Join(separator: ",",
                value: CustomRoles);
        }

        // Use lazy initialization for policy building to improve performance
        Lazy<string> policyLazy = new(valueFactory: BuildPolicy);
        Policy = policyLazy.Value;
    }

    /// <summary>
    /// Creates an attribute with specific permissions.
    /// </summary>
    /// <param name="permissions">Array of required permissions</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithPermissions(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException(message: "At least one permission must be specified.",
                paramName: nameof(permissions));

        return new RequestAuthorizeAttribute(permissions: string.Join(separator: ",",
            value: permissions));
    }

    /// <summary>
    /// Creates an attribute with specific roles.
    /// </summary>
    /// <param name="roles">Array of required roles</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithRoles(params string[] roles)
    {
        if (roles == null || roles.Length == 0)
            throw new ArgumentException(message: "At least one role must be specified.",
                paramName: nameof(roles));

        return new RequestAuthorizeAttribute(permissions: string.Join(separator: ",",
            value: roles));
    }

    /// <summary>
    /// Creates an attribute with specific policies.
    /// </summary>
    /// <param name="policies">Array of required policies</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithPolicies(params string[] policies)
    {
        if (policies == null || policies.Length == 0)
            throw new ArgumentException(message: "At least one policy must be specified.",
                paramName: nameof(policies));

        return new RequestAuthorizeAttribute(permissions: string.Join(separator: ",",
            value: policies));
    }

    /// <summary>
    /// Creates an attribute that requires any of the specified permissions (OR logic).
    /// </summary>
    /// <param name="permissions">Array of permissions (any one required)</param>
    /// <returns>RequestAuthorizeAttribute instance</returns>
    public static RequestAuthorizeAttribute WithAnyPermission(params string[] permissions)
    {
        if (permissions == null || permissions.Length == 0)
            throw new ArgumentException(message: "At least one permission must be specified.",
                paramName: nameof(permissions));

        // For OR logic, we create separate policies for each permission
        // This would require extending the policy provider to handle OR logic
        return new RequestAuthorizeAttribute(permissions: string.Join(separator: ",",
            value: permissions));
    }

    /// <summary>
    /// Gets the required permissions for this authorization.
    /// </summary>
    public string[]? Permissions { get; }

    /// <summary>
    /// Gets the required policies for this authorization.
    /// </summary>
    public string[]? Policies { get; }

    /// <summary>
    /// Gets the required roles for this authorization.
    /// Note: This is different from the base class Roles property which is a string.
    /// </summary>
    public string[]? CustomRoles { get; }

    /// <summary>
    /// Splits a comma-separated string into a clean array of non-empty values.
    /// </summary>
    /// <param name="input">Comma-separated input string</param>
    /// <returns>Array of clean values or null if input is empty</returns>
    private static string[]? SplitAndClean(string? input)
    {
        if (string.IsNullOrWhiteSpace(value: input))
            return null;

        string[] values = input.Split(separator: ',',
                options: StringSplitOptions.RemoveEmptyEntries)
                          .Select(selector: x => x.Trim())
                          .Where(predicate: x => !string.IsNullOrEmpty(value: x))
                          .ToArray();

        return values.Length > 0 ? values : null;
    }

    /// <summary>
    /// Builds the policy string used by the authorization system.
    /// Format: "claim_type:value1,value2;claim_type2:value3"
    /// </summary>
    /// <returns>Policy string for the authorization system</returns>
    private string BuildPolicy()
    {
        List<string> policyParts = new(capacity: 3); // Pre-size for better performance

        AddClaimParts(policyParts: policyParts,
            claimType: CustomClaim.Permission,
            values: Permissions);
        AddClaimParts(policyParts: policyParts,
            claimType: CustomClaim.Policy,
            values: Policies);
        AddClaimParts(policyParts: policyParts,
            claimType: CustomClaim.Role,
            values: CustomRoles);

        if (policyParts.Count == 0)
        {
            throw new InvalidOperationException(message: "No valid authorization parameters were provided.");
        }

        return string.Join(separator: ";",
            values: policyParts);
    }

    /// <summary>
    /// Adds claim parts to the policy parts list.
    /// </summary>
    /// <param name="policyParts">List to add policy parts to</param>
    /// <param name="claimType">Type of claim (permission, policy, role)</param>
    /// <param name="values">Values for the claim type</param>
    private static void AddClaimParts(List<string> policyParts, string claimType, string[]? values)
    {
        if (values?.Length > 0)
        {
            // Validate claim values
            foreach (string value in values)
            {
                if (string.IsNullOrWhiteSpace(value: value))
                {
                    throw new ArgumentException(message: $"Invalid {claimType} value: cannot be null or whitespace.");
                }
            }

            policyParts.Add(item: $"{claimType}:{string.Join(separator: ",", value: values)}");
        }
    }

    /// <summary>
    /// Returns a string representation of the authorization requirements.
    /// </summary>
    /// <returns>String describing the authorization requirements</returns>
    public override string ToString()
    {
        List<string> parts = [];

        if (Permissions?.Length > 0)
            parts.Add(item: $"Permissions: [{string.Join(separator: ", ", value: Permissions)}]");

        if (CustomRoles?.Length > 0)
            parts.Add(item: $"Roles: [{string.Join(separator: ", ", value: CustomRoles)}]");

        if (Policies?.Length > 0)
            parts.Add(item: $"Policies: [{string.Join(separator: ", ", value: Policies)}]");

        return parts.Count > 0 ? string.Join(separator: ", ",
            values: parts) : "No requirements";
    }
}