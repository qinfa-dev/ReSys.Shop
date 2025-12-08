using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Storefront
    {
        public static class Product
        {
            public static AccessPermission View => AccessPermission.Create(name: "store.product.view",
                displayName: "View Product",
                description: "Allows viewing products").Value;
            public static AccessPermission Search => AccessPermission.Create(name: "store.product.search",
                displayName: "Search Product",
                description: "Allows searching products").Value;
            public static AccessPermission Browse => AccessPermission.Create(name: "store.product.browse",
                displayName: "Browse Product",
                description: "Allows browsing products").Value;

            public static AccessPermission[] All => [View, Search, Browse];
        }
    }
}