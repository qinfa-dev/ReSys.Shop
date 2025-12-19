using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static partial class Setting
        {
            public static class PaymentMethod
            {
                public static AccessPermission Create => AccessPermission.Create(name: "admin.store.create",
                    displayName: "Create Payment Method",
                    description: "Allows creating new stores").Value;
                public static AccessPermission Read => AccessPermission.Create(name: "admin.store.read",
                    displayName: "Read Payment Method",
                    description: "Allows reading role details").Value;
                public static AccessPermission Update => AccessPermission.Create(name: "admin.store.update",
                    displayName: "Update Payment Method",
                    description: "Allows updating existing stores").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.store.delete",
                    displayName: "Delete Payment Method",
                    description: "Allows deleting stores").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.store.list",
                    displayName: "List Payment Methods",
                    description: "Allows listing all stores").Value;

                public static AccessPermission View => AccessPermission.Create(name: "admin.store.view",
                    displayName: "View Payment Method Details",
                    description: "Allows viewing detailed information about a store").Value;

                public static AccessPermission Assign => AccessPermission.Create(name: "admin.store.assign",
                    displayName: "Assign Payment Method",
                    description: "Allows assigning stores to users").Value;

                public static AccessPermission[] All => [Create, Read, Update, Delete, List, Assign];
            }
        }
    }
}