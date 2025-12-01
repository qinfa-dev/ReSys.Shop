using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static class Promotion
        {
            public static AccessPermission Create => AccessPermission.Create(
                name: "admin.promotion.create",
                displayName: "Create Promotion",
                description: "Allows creating a new promotion.").Value;
            public static AccessPermission Update => AccessPermission.Create(
                name: "admin.promotion.update",
                displayName: "Update Promotion",
                description:
                "Allows updating an existing promotion, including activating/deactivating and changing settings.").Value;
            public static AccessPermission Delete => AccessPermission.Create(
                name: "admin.promotion.delete",
                displayName: "Delete Promotion",
                description: "Allows deleting a promotion.").Value;
            public static AccessPermission View => AccessPermission.Create(
                name: "admin.promotion.view",
                displayName: "View Promotion Details",
                description: "Allows viewing details of a promotion.").Value;
            public static AccessPermission List => AccessPermission.Create(
                name: "admin.promotion.list",
                displayName: "List Promotions",
                description: "Allows viewing a list of promotions.").Value;

            public static AccessPermission[] All =>
            [
                Create,
                Update,
                Delete,
                View,
                List
            ];
        }
    }
}