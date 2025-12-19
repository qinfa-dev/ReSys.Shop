using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Storefront
    {
        public static AccessPermission[] All =>
        [
            .. Product.All,
            .. Order.All,
            .. Cart.All,
            .. Wishlist.All,
            .. Review.All,
            .. Profile.All
        ];
    }
}