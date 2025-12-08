using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Inventory
        {
            public static class StockTransfer
            {
                public static AccessPermission Create => AccessPermission.Create(
                    name: "admin.stock_transfer.create",
                    displayName: "Create Stock Transfer",
                    description: "Allows creating a new stock transfer between locations."
                ).Value;

                public static AccessPermission List => AccessPermission.Create(
                    name: "admin.stock_transfer.list",
                    displayName: "View Stock Transfers",
                    description: "Allows viewing the list of all stock transfer records."
                ).Value;

                public static AccessPermission View => AccessPermission.Create(
                    name: "admin.stock_transfer.view",
                    displayName: "View Stock Transfer Details",
                    description: "Allows viewing details of a specific stock transfer."
                ).Value;

                public static AccessPermission Transfer => AccessPermission.Create(
                    name: "admin.stock_transfer.transfer",
                    displayName: "Initiate Stock Transfer",
                    description: "Allows initiating or processing a stock transfer between locations."
                ).Value;

                public static AccessPermission Execute => AccessPermission.Create(
                    name: "admin.stock_transfer.execute",
                    displayName: "Execute Stock Transfer",
                    description: "Allows executing receipt of transferred stock items."
                ).Value;

                public static AccessPermission Receive => AccessPermission.Create(
                    name: "admin.stock_transfer.receive",
                    displayName: "Receive Stock Transfer",
                    description: "Allows confirming receipt of transferred stock items."
                ).Value;

                public static AccessPermission Update => AccessPermission.Create(
                    name: "admin.stock_transfer.update",
                    displayName: "Update Stock Transfer",
                    description: "Allows updating details of a stock transfer."
                ).Value;

                public static AccessPermission Delete => AccessPermission.Create(
                    name: "admin.stock_transfer.delete",
                    displayName: "Delete Stock Transfer",
                    description: "Allows deleting a stock transfer record."
                ).Value;

                public static AccessPermission[] All =>
                [
                    Create,
                    List,
                    View,
                    Transfer,
                    Execute,
                    Receive,
                    Update,
                    Delete
                ];
            }
        }
    }
}
