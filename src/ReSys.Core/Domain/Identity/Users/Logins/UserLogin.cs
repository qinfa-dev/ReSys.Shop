using Microsoft.AspNetCore.Identity;

namespace ReSys.Core.Domain.Identity.Users.Logins;
/// <summary>
/// Represents an external login provider for a user in the ASP.NET Core Identity system.
/// This class extends the default <see cref="IdentityUserLogin{TKey}"/> to include a direct
/// navigation property to the associated <see cref="User"/>.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Role in Identity Domain:</strong>
/// <list type="bullet">
/// <item>
/// <term>External Authentication</term>
/// <description>Links a user's account to an external authentication provider (e.g., Google, Facebook, Microsoft).</description>
/// </item>
/// <item>
/// <term>Passwordless Login</term>
/// <description>Enables users to log in using their external provider credentials without needing a separate password for the application.</description>
/// </item>
/// </list>
/// </para>
///
/// <para>
/// <strong>Key Attributes (inherited from <see cref="IdentityUserLogin{TKey}"/>):</strong>
/// <list type="bullet">
/// <item>
/// <term>LoginProvider</term>
/// <description>The provider for the login (e.g., "Google", "Facebook").</description>
/// </item>
/// <item>
/// <term>ProviderKey</term>
/// <description>The unique identifier for the user from the login provider.</description>
/// </item>
/// <item>
/// <term>ProviderDisplayName</term>
/// <description>The display name of the login provider.</description>
/// </item>
/// <item>
/// <term>UserId</term>
/// <description>The ID of the <see cref="User"/> associated with this login.</description>
/// </item>
/// </list>
/// </para>
/// </remarks>
public class UserLogin : IdentityUserLogin<string>
{
    /// <summary>
    /// Gets or sets the navigation property to the <see cref="User"/> entity associated with this login.
    /// This provides a direct link to the user details from the login record.
    /// </summary>
    public virtual User User { get; set; } = null!;
}