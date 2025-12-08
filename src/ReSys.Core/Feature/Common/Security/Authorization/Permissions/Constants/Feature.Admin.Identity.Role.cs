using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class Feature
{
    public static partial class Admin
    {
        public static partial class Identity
        {
            public static class Role
            {
                public static AccessPermission Create => AccessPermission.Create(name: "admin.role.create",
                    displayName: "Create Role",
                    description: "Allows creating new roles").Value;
                public static AccessPermission Read => AccessPermission.Create(name: "admin.role.read",
                    displayName: "Read Role",
                    description: "Allows reading role details").Value;
                public static AccessPermission Update => AccessPermission.Create(name: "admin.role.update",
                    displayName: "Update Role",
                    description: "Allows updating existing roles").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.role.delete",
                    displayName: "Delete Role",
                    description: "Allows deleting roles").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.role.list",
                    displayName: "List Roles",
                    description: "Allows listing all roles").Value;
                public static AccessPermission Assign => AccessPermission.Create(name: "admin.role.assign",
                    displayName: "Assign Role",
                    description: "Allows assigning roles to users").Value;
                
                public static AccessPermission ViewUsers => AccessPermission.Create(name: "admin.role.users.view",
                    displayName: "View Role Users",
                    description: "Allows viewing users assigned to a role").Value;
                public static AccessPermission AssignUser => AccessPermission.Create(name: "admin.role.users.assign",
                    displayName: "Assign User to Role",
                    description: "Allows assigning users to a role").Value;
                public static AccessPermission UnassignUser => AccessPermission.Create(name: "admin.role.users.unassign",
                    displayName: "Unassign User from Role",
                    description: "Allows unassigning users from a role").Value;

                public static AccessPermission ViewPermissions => AccessPermission.Create(name: "admin.role.permissions.view",
                    displayName: "View Role Permissions",
                    description: "Allows viewing permissions assigned to a role").Value;
                public static AccessPermission AssignPermission => AccessPermission.Create(name: "admin.role.permissions.assign",
                    displayName: "Assign Permission to Role",
                    description: "Allows assigning permissions to a role").Value;
                public static AccessPermission UnassignPermission => AccessPermission.Create(name: "admin.role.permissions.unassign",
                    displayName: "Unassign Permission from Role",
                    description: "Allows unassigning permissions from a role").Value;

                public static AccessPermission[] All => [Create, Read, Update, Delete, List, Assign, ViewUsers, AssignUser, UnassignUser, ViewPermissions, AssignPermission, UnassignPermission];
            }
        }

    }
}
