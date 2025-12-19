using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static partial class Setting
        {
            public static AccessPermission[] All =>
            [
                ..PaymentMethod.All,
                ..ShippingMethod.All,
            ];
        }
    }
}