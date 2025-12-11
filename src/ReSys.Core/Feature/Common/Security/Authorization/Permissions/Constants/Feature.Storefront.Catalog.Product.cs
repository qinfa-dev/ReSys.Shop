using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Storefront
    {
        public static partial class Catalog
        {
            public static class Product
            {
                public static AccessPermission View  => AccessPermission.Create(name: "storefront.product.view",
                    displayName: "View Product Details",
                    description: "Allows viewing detailed information about a product").Value;
                public static AccessPermission List  => AccessPermission.Create(name: "storefront.product.list",
                    displayName: "View Products",
                    description: "Allows viewing products").Value;
                public static AccessPermission Search  => AccessPermission.Create(name: "storefront.product.search",
                    displayName: "Search Products",
                    description: "Allows searching for products").Value;

                public static AccessPermission[] All => [View, List, Search];
            }
        }
    }
}
