using Microsoft.AspNetCore.Identity;

namespace ReSys.Core.Domain.Identity.Users.Tokens;
/// <summary>
/// Represents an authentication token for a user in the ASP.NET Core Identity system.
/// This class extends the default <see cref="IdentityUserToken{TKey}"/> to include a direct
/// navigation property to the associated <see cref="User"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>Security Token Storage</term>
/// <description>Stores security tokens such as two-factor authentication recovery codes, or tokens for password resets.</description>
/// </item>
/// <item>
/// <term>Provider-Specific Tokens</term>
/// <description>Can store tokens issued by external authentication providers for a specific user.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes (inherited from <see cref="IdentityUserToken{TKey}"/>):</strong>
/// <list type="bullet">
/// <item>
/// <term>UserId</term>
/// <description>The ID of the <see cref="User"/> associated with this token.</description>
/// </item>
/// <item>
/// <term>LoginProvider</term>
/// <description>The login provider that generated the token.</description>
/// </item>
/// <item>
/// <term>Name</term>
/// <description>The name of the token (e.g., "RecoveryCode", "PasswordReset").</description>
/// </item>
/// <item>
/// <term>Value</term>
/// <description>The actual token value.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public class UserToken : IdentityUserToken<string>
{
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="User"/> entity associated with this token.
    /// This provides a direct link to the user details from the token record.
    /// </summary>
    public virtual User User { get; set; } = null!;
}