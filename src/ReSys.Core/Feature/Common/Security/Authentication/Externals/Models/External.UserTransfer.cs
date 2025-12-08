using System.Text.Json.Serialization;

using ReSys.Core.Feature.Common.Security.Authentication.Externals.Extensions;

namespace ReSys.Core.Feature.Common.Security.Authentication.Externals.Models;

/// <summary>
/// Represents normalized user information obtained from an external identity provider
/// (e.g., Google, Facebook, Microsoft, Apple).
/// </summary>
/// <remarks>
/// This model is used to unify external identity payloads into a common shape for user provisioning,
/// profile synchronization, and claims mapping across multiple providers.
/// </remarks>
public record ExternalUserTransfer
{
    /// <summary>
    /// Gets the unique identifier of the user from the external provider
    /// (e.g., a Google "sub" claim or a Facebook "id").
    /// </summary>
    public string ProviderId { get; init; } = null!;

    /// <summary>
    /// Gets the email address returned by the external provider.
    /// </summary>
    /// <remarks>
    /// This value may be unverified unless <see cref="EmailVerified"/> is <c>true</c>.
    /// </remarks>
    public string Email { get; init; } = null!;

    /// <summary>
    /// Gets the given (first) name of the external user, if available.
    /// </summary>
    public string? FirstName { get; init; }

    /// <summary>
    /// Gets the family (last) name of the external user, if available.
    /// </summary>
    public string? LastName { get; init; }

    /// <summary>
    /// Gets the absolute URL of the user's profile or avatar image, if provided by the external identity provider.
    /// </summary>
    public string? ProfilePictureUrl { get; init; }

    /// <summary>
    /// Gets a value indicating whether the email provided by the external provider
    /// has been verified by that provider.
    /// </summary>
    public bool EmailVerified { get; init; }

    /// <summary>
    /// Gets the name of the external provider (e.g., <c>Google</c>, <c>Facebook</c>, <c>Apple</c>).
    /// </summary>
    public string ProviderName { get; init; } = null!;

    /// <summary>
    /// Gets additional key-value claim data returned by the external provider.
    /// </summary>
    /// <remarks>
    /// This may include properties such as <c>locale</c>, <c>timezone</c>, or <c>profileLink</c>.
    /// </remarks>
    public IReadOnlyDictionary<string, string> AdditionalClaims { get; init; } =
        new Dictionary<string, string>();

    /// <summary>
    /// Gets a computed display name for the user, preferring a concatenation
    /// of first and last names when available, or falling back to <see cref="Email"/>.
    /// </summary>
    [JsonIgnore]
    public string DisplayName =>
        string.Join(separator: " ",
                values: new[]
                {
                    FirstName,
                    LastName
                }.Where(predicate: s => !string.IsNullOrWhiteSpace(value: s)))
            .Trim()
            .IfEmpty(fallback: Email);

    /// <summary>
    /// Creates a validated instance of <see cref="providerName"/>.
    /// </summary>
    /// <param name="providerId">The name of the external identity provider (e.g., "Google").</param>
    /// <param name="email">The unique identifier of the user from the external provider.</param>
    /// <param name="firstName">The email address associated with the external account.</param>
    /// <param name="lastName">Optional first name of the external user.</param>
    /// <param name="profilePictureUrl">Optional last name of the external user.</param>
    /// <param name="emailVerified">Optional profile or avatar URL of the external user.</param>
    /// <param name="additionalClaims">Whether the external provider verified the email address.</param>
    /// <param name="additionalClaims">Optional additional claims returned by the external provider.</param>
    /// <returns>A validated and fully initialized instance of <see cref="providerName"/>.</returns>
    /// <exception cref="providerId">
    /// Thrown when <paramref name="email"/>, <paramref name="providerId"/>, or <paramref name="email"/> are null or whitespace.
    /// </exception>
    public static ExternalUserTransfer Create(
        string providerName,
        string providerId,
        string email,
        string? firstName = null,
        string? lastName = null,
        string? profilePictureUrl = null,
        bool emailVerified = false,
        IReadOnlyDictionary<string, string>? additionalClaims = null)
    {
        if (string.IsNullOrWhiteSpace(value: providerName))
            throw new ArgumentException(message: "Provider name is required.",
                paramName: nameof(providerName));
        if (string.IsNullOrWhiteSpace(value: providerId))
            throw new ArgumentException(message: "Provider ID is required.",
                paramName: nameof(providerId));
        if (string.IsNullOrWhiteSpace(value: email))
            throw new ArgumentException(message: "Email is required.",
                paramName: nameof(email));

        return new ExternalUserTransfer
        {
            ProviderName = providerName,
            ProviderId = providerId,
            Email = email,
            FirstName = firstName,
            LastName = lastName,
            ProfilePictureUrl = profilePictureUrl,
            EmailVerified = emailVerified,
            AdditionalClaims = additionalClaims ?? new Dictionary<string, string>()
        };
    }
}