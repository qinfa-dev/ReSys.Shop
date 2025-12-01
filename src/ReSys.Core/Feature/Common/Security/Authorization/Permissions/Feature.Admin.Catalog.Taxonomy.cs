using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class Taxonomy
            {
                public static AccessPermission Create => AccessPermission.Create(name: "admin.taxonomy.create",
                    displayName: "Create Taxonomy",
                    description: "Allows creating a new taxonomy").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.taxonomy.list",
                    displayName: "View Taxonomies",
                    description: "Allows viewing taxonomies").Value;
                public static AccessPermission View => AccessPermission.Create(name: "admin.taxonomy.view",
                    displayName: "View Taxonomy Details",
                    description: "Allows viewing detailed information about a taxonomy").Value;
                public static AccessPermission Update => AccessPermission.Create(name: "admin.taxonomy.update",
                    displayName: "Update Taxonomy",
                    description: "Allows updating an existing taxonomy").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.taxonomy.delete",
                    displayName: "Delete Taxonomy",
                    description: "Allows deleting a taxonomy").Value;

                public static AccessPermission[] All => [Create, List, View, Update, Delete];
            }
        }
    }
}