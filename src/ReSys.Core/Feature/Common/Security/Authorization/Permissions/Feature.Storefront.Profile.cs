using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Storefront
    {
        public static class Profile
        {
            public static AccessPermission View => AccessPermission.Create(name: "store.profile.view",
                displayName: "View Profile",
                description: "Allows viewing user profile").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "store.profile.update",
                displayName: "Update Profile",
                description: "Allows updating user profile").Value;
            public static AccessPermission Delete => AccessPermission.Create(name: "store.profile.delete",
                displayName: "Delete Profile",
                description: "Allows deleting user profile").Value;

            public static AccessPermission[] All => [View, Update, Delete];
        }
    }
}