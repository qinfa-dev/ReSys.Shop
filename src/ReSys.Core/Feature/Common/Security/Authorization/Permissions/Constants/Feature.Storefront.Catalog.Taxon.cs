using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Storefront
    {
        public static partial class Catalog
        {
            public static class Taxon
            {
                public static AccessPermission View  => AccessPermission.Create(name: "storefront.taxon.view",
                    displayName: "View Taxon Details (Storefront)",
                    description: "Allows viewing detailed information about a taxon on the storefront").Value;
                public static AccessPermission List  => AccessPermission.Create(name: "storefront.taxon.list",
                    displayName: "List Taxons (Storefront)",
                    description: "Allows listing taxons on the storefront").Value;
                public static AccessPermission Search  => AccessPermission.Create(name: "storefront.taxon.search",
                    displayName: "Search Taxons (Storefront)",
                    description: "Allows searching for taxons on the storefront").Value;

                public static AccessPermission[] All => [View, List, Search];
            }
        }
    }
}
