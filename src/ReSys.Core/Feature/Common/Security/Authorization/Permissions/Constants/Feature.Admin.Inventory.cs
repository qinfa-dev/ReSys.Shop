using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Inventory
        {
            public static AccessPermission[] All =>
            [
                ..StockItem.All,
                ..StockLocation.All,
                ..StockTransfer.All
            ];
        }
    }
}
