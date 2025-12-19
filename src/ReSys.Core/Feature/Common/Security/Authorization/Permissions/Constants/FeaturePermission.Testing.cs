using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;
public static partial class FeaturePermission
{
    public static partial class Testing
    {
        public static AccessPermission[] All =>
        [
            .. TodoLists.All,
            .. TodoItems.All
        ];
    }
}
