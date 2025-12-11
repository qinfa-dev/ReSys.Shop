using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static AccessPermission[] All =>
        [
            .. Identity.All,
            .. AuditLog.All,
            .. Catalog.All,
            .. Inventory.All,
            .. ShippingMethod.All,
            .. PaymentMethod.All,
            .. Store.All,
            .. Promotion.All
        ];
    }
}
