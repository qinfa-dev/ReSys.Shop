using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Identity
        {
            public static class AccessControlPermission
            {
                public static AccessPermission View => AccessPermission.Create(name: "admin.permissions.view",
                    displayName: "View Access Control",
                    description: "Allows viewing access control permissions").Value;
                public static AccessPermission Assign => AccessPermission.Create(name: "admin.permissions.assign",
                    displayName: "Assign Permissions",
                    description: "Allows assigning permissions to roles").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.permissions.list",
                    displayName: "List Permissions",
                    description: "Allows listing all available permissions").Value;
                public static AccessPermission Manage => AccessPermission.Create(name: "admin.permissions.manage",
                    displayName: "Manage Permissions",
                    description: "Allows managing permissions (create, update, delete)").Value;

                public static AccessPermission[] All => [View, Assign, List, Manage];
            }
        }
    }

}
