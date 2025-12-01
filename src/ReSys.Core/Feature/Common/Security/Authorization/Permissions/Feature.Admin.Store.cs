using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class Store
        {
            public static AccessPermission Create => AccessPermission.Create(name: "admin.store.create",
                displayName: "Create Store",
                description: "Allows creating new stores").Value;
            public static AccessPermission Read => AccessPermission.Create(name: "admin.store.read",
                displayName: "Read Store",
                description: "Allows reading role details").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "admin.store.update",
                displayName: "Update Store",
                description: "Allows updating existing stores").Value;
            public static AccessPermission Delete => AccessPermission.Create(name: "admin.store.delete",
                displayName: "Delete Store",
                description: "Allows deleting stores").Value;
            public static AccessPermission List => AccessPermission.Create(name: "admin.store.list",
                displayName: "List Stores",
                description: "Allows listing all stores").Value;

            public static AccessPermission View => AccessPermission.Create(name: "admin.store.view",
                displayName: "View Store Details",
                description: "Allows viewing detailed information about a store").Value;

            public static AccessPermission Assign => AccessPermission.Create(name: "admin.store.assign",
                displayName: "Assign Store",
                description: "Allows assigning stores to users").Value;

            public static AccessPermission[] All => [Create, Read, Update, Delete, List, View, Assign];
        }
    }
}
