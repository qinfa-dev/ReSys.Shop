using Microsoft.AspNetCore.Identity;

namespace ReSys.Core.Domain.Identity.Users.Tokens;
public class UserToken : IdentityUserToken<string>
{
    public virtual ApplicationUser ApplicationUser { get; set; } = null!;
}