using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static partial class Identity
        {
            public static AccessPermission[] All =>
            [
                ..User.All,
                ..Role.All, 
                ..AccessControlPermission.All,
            ];
        }
    }
}
