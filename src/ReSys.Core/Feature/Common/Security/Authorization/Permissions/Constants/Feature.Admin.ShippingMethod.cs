using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static class ShippingMethod
        {
            public static AccessPermission Create => AccessPermission.Create(name: "admin.shipping_method.create", displayName: "Create Shipping Method", description: "Allows creating a new shipping method.").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "admin.shipping_method.update", displayName: "Update Shipping Method", description: "Allows updating an existing shipping method, including activating/deactivating and changing settings.").Value;
            public static AccessPermission Delete => AccessPermission.Create(name: "admin.shipping_method.delete", displayName: "Delete Shipping Method", description: "Allows deleting a shipping method.").Value;
            public static AccessPermission View => AccessPermission.Create(name: "admin.shipping_method.view", displayName: "View Shipping Method Details", description: "Allows viewing details of a shipping method.").Value;
            public static AccessPermission List => AccessPermission.Create(name: "admin.shipping_method.list", displayName: "List Shipping Methods", description: "Allows viewing a list of shipping methods.").Value;

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
