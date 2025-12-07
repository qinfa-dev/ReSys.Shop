using Microsoft.AspNetCore.Identity;

namespace ReSys.Core.Domain.Identity.Users.Tokens;
public class UserToken : IdentityUserToken<string>
{
    public virtual User User { get; set; } = null!;
}