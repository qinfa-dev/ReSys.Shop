using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class OptionValue
            {
                public static AccessPermission Create => AccessPermission.Create(
                    name: "admin.catalog.option_type.option_value.create",
                    displayName: "Create OptionValue",
                    description: "Allows creating a new option value").Value;
                public static AccessPermission List => AccessPermission.Create(
                    name: "admin.catalog.option_type.option_value.list",
                    displayName: "View OptionValues",
                    description: "Allows viewing option values").Value;

                public static AccessPermission View => AccessPermission.Create(
                    name: "admin.catalog.option_type.option_value.view",
                    displayName: "View OptionValue Details",
                    description: "Allows viewing detailed information about an option value").Value;
                public static AccessPermission Update => AccessPermission.Create(
                    name: "admin.catalog.option_type.option_value.update",
                    displayName: "Update OptionValue",
                    description: "Allows updating an existing option value").Value;
                public static AccessPermission Delete => AccessPermission.Create(
                    name: "admin.catalog.option_type.option_value.delete",
                    displayName: "Delete OptionValue",
                    description: "Allows deleting an option value").Value;

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
