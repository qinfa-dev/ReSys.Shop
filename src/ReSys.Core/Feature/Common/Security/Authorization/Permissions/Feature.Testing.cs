using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;
public static partial class Feature
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
