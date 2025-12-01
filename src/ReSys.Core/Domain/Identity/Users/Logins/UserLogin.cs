using Microsoft.AspNetCore.Identity;

namespace ReSys.Core.Domain.Identity.Users.Logins;
public class UserLogin : IdentityUserLogin<string>
{
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;
}