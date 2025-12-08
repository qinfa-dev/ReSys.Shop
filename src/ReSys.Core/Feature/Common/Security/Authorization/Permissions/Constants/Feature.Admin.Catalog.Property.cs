using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class Property
            {
                public static AccessPermission Create  => AccessPermission.Create(name: "admin.catalog.property.create",
                    displayName: "Create Property",
                    description: "Allows creating a new property").Value;
                public static AccessPermission List  => AccessPermission.Create(name: "admin.catalog.property.list",
                    displayName: "View Properties",
                    description: "Allows viewing properties").Value;
                public static AccessPermission View  => AccessPermission.Create(name: "admin.catalog.property.view",
                    displayName: "View Property Details",
                    description: "Allows viewing detailed information about a property").Value;
                public static AccessPermission Update  => AccessPermission.Create(name: "admin.catalog.property.update",
                    displayName: "Update Property",
                    description: "Allows updating an existing property").Value;
                public static AccessPermission Delete  => AccessPermission.Create(name: "admin.catalog.property.delete",
                    displayName: "Delete Property",
                    description: "Allows deleting a property").Value;

                public static AccessPermission[] All =>
                [
                    Create,
                    List,
                    View,
                    Update,
                    Delete
                ];
            }
        }
    }
}