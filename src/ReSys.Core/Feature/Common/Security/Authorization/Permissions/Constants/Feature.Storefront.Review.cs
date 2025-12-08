using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Storefront
    {
        public static class Review
        {
            public static AccessPermission Create => AccessPermission.Create(name: "store.review.create",
                displayName: "Create Review",
                description: "Allows creating reviews").Value;
            public static AccessPermission Update => AccessPermission.Create(name: "store.review.update",
                displayName: "Update Review",
                description: "Allows updating reviews").Value;
            public static AccessPermission Delete => AccessPermission.Create(name: "store.review.delete",
                displayName: "Delete Review",
                description: "Allows deleting reviews").Value;
            public static AccessPermission View => AccessPermission.Create(name: "store.review.view",
                displayName: "View Review",
                description: "Allows viewing reviews").Value;

            public static AccessPermission[] All => [Create, Update, Delete, View];
        }
    }
}