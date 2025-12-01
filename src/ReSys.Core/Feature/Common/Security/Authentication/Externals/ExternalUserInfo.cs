using System.Text.Json.Serialization;

namespace ReSys.Core.Feature.Common.Security.Authentication.Externals;

/// <summary>
/// Represents normalized user information obtained from an external identity provider
/// (e.g., Google, Facebook, Microsoft, Apple).
/// </summary>
/// <remarks>
/// This model is used to unify external identity payloads into a common shape for user provisioning,
/// profile synchronization, and claims mapping across multiple providers.
/// </remarks>
public record ExternalUserInfo
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
    /// Creates a validated instance of <see cref="ExternalUserInfo"/>.
    /// </summary>
    /// <param name="providerName">The name of the external identity provider (e.g., "Google").</param>
    /// <param name="providerId">The unique identifier of the user from the external provider.</param>
    /// <param name="email">The email address associated with the external account.</param>
    /// <param name="firstName">Optional first name of the external user.</param>
    /// <param name="lastName">Optional last name of the external user.</param>
    /// <param name="profilePictureUrl">Optional profile or avatar URL of the external user.</param>
    /// <param name="emailVerified">Whether the external provider verified the email address.</param>
    /// <param name="additionalClaims">Optional additional claims returned by the external provider.</param>
    /// <returns>A validated and fully initialized instance of <see cref="ExternalUserInfo"/>.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="providerName"/>, <paramref name="providerId"/>, or <paramref name="email"/> are null or whitespace.
    /// </exception>
    public static ExternalUserInfo Create(
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

        return new ExternalUserInfo
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

internal static class StringExtensions
{
    /// <summary>
    /// Returns the specified <paramref name="fallback"/> string if the current <paramref name="value"/> is
    /// <see langword="null"/>, empty, or consists only of white-space characters.
    /// </summary>
    /// <param name="value">The string to evaluate.</param>
    /// <param name="fallback">The value to return if <paramref name="value"/> is null or empty.</param>
    /// <returns>
    /// <paramref name="value"/> if it contains non-whitespace characters; otherwise, <paramref name="fallback"/>.
    /// </returns>
    public static string IfEmpty(this string? value, string fallback) =>
        string.IsNullOrWhiteSpace(value: value) ? fallback : value;
}
