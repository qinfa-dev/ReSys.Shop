using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class OptionType
            {
                public static AccessPermission Create => AccessPermission.Create(
                    name: "admin.catalog.option_type.create",
                    displayName: "Create OptionType",
                    description: "Allows creating a new option type").Value;
                public static AccessPermission List => AccessPermission.Create(
                    name: "admin.catalog.option_type.list",
                    displayName: "View OptionTypes",
                    description: "Allows viewing option types").Value;

                public static AccessPermission View => AccessPermission.Create(
                    name: "admin.catalog.option_type.view",
                    displayName: "View OptionType Details",
                    description: "Allows viewing detailed information about an option type").Value;
                public static AccessPermission Update => AccessPermission.Create(
                    name: "admin.catalog.option_type.update").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.catalog.option_type.delete").Value;
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
