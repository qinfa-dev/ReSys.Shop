using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Storefront
    {
        public static class Wishlist
        {
            public static AccessPermission Add => AccessPermission.Create(name: "store.wishlist.add",
                displayName: "Add to Wishlist",
                description: "Allows adding items to wishlist").Value;
            public static AccessPermission Remove => AccessPermission.Create(name: "store.wishlist.remove",
                displayName: "Remove from Wishlist",
                description: "Allows removing items from wishlist").Value;
            public static AccessPermission View => AccessPermission.Create(name: "store.wishlist.view",
                displayName: "View Wishlist",
                description: "Allows viewing wishlist").Value;
            public static AccessPermission Share => AccessPermission.Create(name: "store.wishlist.share",
                displayName: "Share Wishlist",
                description: "Allows sharing wishlist").Value;

            public static AccessPermission[] All => [Add, Remove, View, Share];
        }
    }
}