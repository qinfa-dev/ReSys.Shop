using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static partial class Inventory
        {
            public static class StockItem
            {
                public static AccessPermission Create => AccessPermission.Create(
                    name: "admin.stock_item.create",
                    displayName: "Create Stock Item",
                    description: "Allows creating a new stock item."
                ).Value;

                public static AccessPermission List => AccessPermission.Create(
                    name: "admin.stock_item.list",
                    displayName: "View Stock Item List",
                    description: "Allows viewing the list of stock items."
                ).Value;

                public static AccessPermission View => AccessPermission.Create(
                    name: "admin.stock_item.view",
                    displayName: "View Stock Item Details",
                    description: "Allows viewing details of a specific stock item."
                ).Value;

                public static AccessPermission Update => AccessPermission.Create(
                    name: "admin.stock_item.update",
                    displayName: "Update Stock Item",
                    description: "Allows updating stock item information."
                ).Value;

                public static AccessPermission Delete => AccessPermission.Create(
                    name: "admin.stock_item.delete",
                    displayName: "Delete Stock Item",
                    description: "Allows deleting a stock item from inventory."
                ).Value;

                public static AccessPermission Adjust => AccessPermission.Create(
                    name: "admin.stock_item.adjust",
                    displayName: "Adjust Stock Levels",
                    description: "Allows performing stock adjustments such as corrections or audits."
                ).Value;

                public static AccessPermission Reserve => AccessPermission.Create(
                    name: "admin.stock_item.reserve",
                    displayName: "Reserve Stock",
                    description: "Allows reserving stock items for orders or allocations."
                ).Value;

                public static AccessPermission Release => AccessPermission.Create(
                    name: "admin.stock_item.release",
                    displayName: "Release Reserved Stock",
                    description: "Allows releasing previously reserved stock items."
                ).Value;

                public static AccessPermission Ship => AccessPermission.Create(
                    name: "admin.stock_item.ship",
                    displayName: "Ship Stock Item",
                    description: "Allows marking stock items as shipped."
                ).Value;

                public static AccessPermission ViewMovementHistory => AccessPermission.Create(
                    name: "admin.stock_item.view_movement_history",
                    displayName: "View Movement History",
                    description: "Allows viewing the movement history of stock items."
                ).Value;

                public static AccessPermission CreateMovement => AccessPermission.Create(
                    name: "admin.stock_item.create_movement",
                    displayName: "Create Stock Movement",
                    description: "Allows creating new stock movement records."
                ).Value;

                public static AccessPermission ViewLowStockAlert => AccessPermission.Create(
                    name: "admin.stock_item.view_low_stock_alert",
                    displayName: "View Low Stock Alerts",
                    description: "Allows viewing alerts for low-stock items."
                ).Value;

                public static AccessPermission ViewOutOfStock => AccessPermission.Create(
                    name: "admin.stock_item.viewoutofstock",
                    displayName: "View Out-of-Stock Items",
                    description: "Allows viewing items that are currently out of stock."
                ).Value;

                public static AccessPermission ViewStockByLocation => AccessPermission.Create(
                    name: "admin.stock_item.view_stock_by_location",
                    displayName: "View Stock by Location",
                    description: "Allows viewing stock levels by warehouse or location."
                ).Value;

                public static AccessPermission[] All =>
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete,
                    Adjust,
                    Reserve,
                    Release,
                    Ship,
                    ViewMovementHistory,
                    CreateMovement,
                    ViewLowStockAlert,
                    ViewOutOfStock,
                    ViewStockByLocation
                ];
            }
        }
    }
}
