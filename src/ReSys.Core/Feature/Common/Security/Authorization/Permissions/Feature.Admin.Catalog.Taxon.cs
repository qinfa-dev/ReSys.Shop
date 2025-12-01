using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Catalog
        {
            public static class Taxon
            {
                public static AccessPermission Create => AccessPermission.Create(
                    name: "admin.taxon.create",
                    displayName: "Create Taxon",
                    description: "Allows creating a new taxon").Value;
                public static AccessPermission List => AccessPermission.Create(
                    name: "admin.taxon.list",
                    displayName: "View Taxons",
                    description: "Allows viewing taxons").Value;
                public static AccessPermission View => AccessPermission.Create(
                    name: "admin.taxon.view",
                    displayName: "View Taxon Details",
                    description: "Allows viewing detailed information about a taxon").Value;
                public static AccessPermission Update => AccessPermission.Create(
                    name: "admin.taxon.update",
                    displayName: "Update Taxon",
                    description: "Allows updating an existing taxon").Value;
                public static AccessPermission Delete => AccessPermission.Create(
                    name: "admin.taxon.delete",
                    displayName: "Delete Taxon",
                    description: "Allows deleting a taxon").Value;
                public static AccessPermission Move => AccessPermission.Create(
                    name: "admin.taxon.move",
                    displayName: "Move Taxon",
                    description: "Allows moving a taxon in the hierarchy").Value;

                public static AccessPermission[] All => [Create, List, View, Update, Delete, Move];
            }
        }
    }
}
