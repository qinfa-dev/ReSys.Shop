using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;

using ReSys.Core.Feature.Common.Security.Authorization.Claims.Constants;
using ReSys.Infrastructure.Security.Authorization.Requirements;

namespace ReSys.Infrastructure.Security.Authorization.Policies;

internal class HasAuthorizationPolicyProvider(IOptions<AuthorizationOptions> options) : IAuthorizationPolicyProvider
{
    private readonly AuthorizationOptions _options = options.Value ?? throw new ArgumentNullException(paramName: nameof(options));

    public Task<AuthorizationPolicy> GetDefaultPolicyAsync()
    {
        return Task.FromResult(result: _options.DefaultPolicy);
    }

    public Task<AuthorizationPolicy?> GetFallbackPolicyAsync()
    {
        return Task.FromResult(result: _options.FallbackPolicy);
    }

    public Task<AuthorizationPolicy?> GetPolicyAsync(string policyName)
    {
        if (string.IsNullOrWhiteSpace(value: policyName))
            throw new ArgumentException(message: "Policy name cannot be null or empty.",
                paramName: nameof(policyName));

        AuthorizationPolicy? existingPolicy = _options.GetPolicy(name: policyName);
        if (existingPolicy != null)
        {
            return Task.FromResult<AuthorizationPolicy?>(result: existingPolicy);
        }

        (List<string> permissions, List<string> policies, List<string> roles) = ParsePolicyName(policyName: policyName);

        if (permissions.Count == 0 && policies.Count == 0 && roles.Count == 0)
            return Task.FromResult<AuthorizationPolicy?>(result: null);

        HasAuthorizeClaimRequirement requirement = new(
            permissions: [.. permissions],
            policies: [.. policies],
            roles: [.. roles]);

        AuthorizationPolicy policy = new AuthorizationPolicyBuilder()
            .AddRequirements(requirements: requirement)
            .Build();

        return Task.FromResult<AuthorizationPolicy?>(result: policy);
    }

    /// <summary>
    /// Parses a policy name into its component permissions, policies, and roles.
    /// Format: "permission:perm1,perm2;policy:pol1,pol2;role:role1,role2"
    /// </summary>
    /// <param name="policyName">The policy name to parse</param>
    /// <returns>Tuple containing lists of permissions, policies, and roles</returns>
    private static (List<string> permissions, List<string> policies, List<string> roles) ParsePolicyName(string policyName)
    {
        List<string> permissions = [];
        List<string> policies = [];
        List<string> roles = [];

        try
        {
            ReadOnlySpan<char> policyParts = policyName.AsSpan();
            const char partSeparator = ';';

            while (!policyParts.IsEmpty)
            {
                int nextSeparator = policyParts.IndexOf(value: partSeparator);
                ReadOnlySpan<char> part = nextSeparator >= 0 ? policyParts[..nextSeparator] : policyParts;

                if (!part.IsEmpty)
                {
                    ProcessPolicyPart(part: part.ToString(),
                        permissions: permissions,
                        policies: policies,
                        roles: roles);
                }

                policyParts = nextSeparator >= 0 ? policyParts[(nextSeparator + 1)..] : ReadOnlySpan<char>.Empty;
            }
        }
        catch (Exception ex)
        {
            throw new ArgumentException(message: $"Invalid policy format: {policyName}",
                paramName: nameof(policyName),
                innerException: ex);
        }

        return (permissions, policies, roles);
    }

    /// <summary>
    /// Processes a single policy part and adds values to the appropriate collection.
    /// </summary>
    /// <param name="part">Policy part to process</param>
    /// <param name="permissions">Collection to add permissions to</param>
    /// <param name="policies">Collection to add policies to</param>
    /// <param name="roles">Collection to add roles to</param>
    private static void ProcessPolicyPart(string part, List<string> permissions, List<string> policies, List<string> roles)
    {
        int colonIndex = part.IndexOf(value: ':');
        if (colonIndex <= 0 || colonIndex >= part.Length - 1)
        {
            return; // Invalid format, skip this part
        }

        string claimType = part[..colonIndex];
        ReadOnlySpan<char> valuesSpan = part.AsSpan(start: colonIndex + 1);

        if (string.Equals(a: claimType,
                b: CustomClaim.Permission,
                comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            AddValuesToList(values: valuesSpan,
                targetList: permissions);
        }
        else if (string.Equals(a: claimType,
                     b: CustomClaim.Policy,
                     comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            AddValuesToList(values: valuesSpan,
                targetList: policies);
        }
        else if (string.Equals(a: claimType,
                     b: CustomClaim.Role,
                     comparisonType: StringComparison.OrdinalIgnoreCase))
        {
            AddValuesToList(values: valuesSpan,
                targetList: roles);
        }
    }

    /// <summary>
    /// Adds comma-separated values to the target list.
    /// </summary>
    /// <param name="values">Span containing comma-separated values</param>
    /// <param name="targetList">List to add values to</param>
    private static void AddValuesToList(ReadOnlySpan<char> values, List<string> targetList)
    {
        if (values.IsEmpty) return;

        const char valueSeparator = ',';

        while (!values.IsEmpty)
        {
            int nextSeparator = values.IndexOf(value: valueSeparator);
            ReadOnlySpan<char> value = nextSeparator >= 0 ? values[..nextSeparator] : values;

            if (!value.IsEmpty)
            {
                string trimmedValue = value.ToString().Trim();
                if (!string.IsNullOrEmpty(value: trimmedValue))
                {
                    targetList.Add(item: trimmedValue);
                }
            }

            values = nextSeparator >= 0 ? values[(nextSeparator + 1)..] : ReadOnlySpan<char>.Empty;
        }
    }
}
