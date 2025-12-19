using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Storefront
    {
        public static class Order
        {
            public static AccessPermission Create => AccessPermission.Create(name: "store.order.create",
                displayName: "Create Order",
                description: "Allows creating orders").Value;
            public static AccessPermission View => AccessPermission.Create(name: "store.order.view",
                displayName: "View Order",
                description: "Allows viewing orders").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "store.order.update",
                displayName: "Update Order",
                description: "Allows updating orders").Value;
            public static AccessPermission Cancel => AccessPermission.Create(name: "store.order.cancel",
                displayName: "Cancel Order",
                description: "Allows canceling orders").Value;
            public static AccessPermission Track => AccessPermission.Create(name: "store.order.track",
                displayName: "Track Order",
                description: "Allows tracking orders").Value;

            public static AccessPermission[] All => [Create, View, Update, Cancel, Track];
        }
    }
}