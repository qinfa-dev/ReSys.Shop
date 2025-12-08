using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Inventory
        {
            public static class StockLocation
            {
                public static AccessPermission Create => AccessPermission.Create(
                    name: "admin.stock_location.create",
                    displayName: "Create Stock Location",
                    description: "Allows creating a new stock location in the inventory system."
                ).Value;

                public static AccessPermission List => AccessPermission.Create(
                    name: "admin.stock_location.list",
                    displayName: "View Stock Locations",
                    description: "Allows viewing a list of all stock locations."
                ).Value;

                public static AccessPermission View => AccessPermission.Create(
                    name: "admin.stock_location.view",
                    displayName: "View Stock Location Details",
                    description: "Allows viewing detailed information for a stock location."
                ).Value;

                public static AccessPermission Update => AccessPermission.Create(
                    name: "admin.stock_location.update",
                    displayName: "Update Stock Location",
                    description: "Allows modifying existing stock location information."
                ).Value;

                public static AccessPermission Delete => AccessPermission.Create(
                    name: "admin.stock_location.delete",
                    displayName: "Delete Stock Location",
                    description: "Allows deleting a stock location."
                ).Value;

                public static AccessPermission LinkStore => AccessPermission.Create(
                    name: "admin.stock_location.link_store",
                    displayName: "Link Storefront to Stock Location",
                    description: "Allows linking a retail store to a stock location."
                ).Value;

                public static AccessPermission UnlinkStore => AccessPermission.Create(
                    name: "admin.stock_location.unlink_store",
                    displayName: "Unlink Storefront from Stock Location",
                    description: "Allows unlinking a store from a stock location."
                ).Value;

                public static AccessPermission[] All =>
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete,
                    LinkStore,
                    UnlinkStore
                ];
            }
        }
    }
}
