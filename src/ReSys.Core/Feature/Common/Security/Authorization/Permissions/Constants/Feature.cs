using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static AccessPermission[] AllPermissions =>
    [
        .. Testing.All,
        .. Admin.All,
        .. Storefront.All
    ];
}
