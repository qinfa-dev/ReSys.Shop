using ReSys.Core.Domain.Identity.Permissions;

namespace ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

public static partial class FeaturePermission
{
    public static partial class Admin
    {
        public static partial class Identity
        {
            public static class User
            {
                public static AccessPermission Create => AccessPermission.Create(name: "admin.user.create",
                    displayName: "Create User",
                    description: "Allows creating new users").Value;
                public static AccessPermission List => AccessPermission.Create(name: "admin.user.list",
                    displayName: "List Users",
                    description: "Allows listing all users").Value;
                public static AccessPermission View => AccessPermission.Create(name: "admin.user.view",
                    displayName: "View User Details",
                    description: "Allows viewing detailed information about a user").Value;
                public static AccessPermission Update => AccessPermission.Create(name: "admin.user.update",
                    displayName: "Update User",
                    description: "Allows updating existing users").Value;
                public static AccessPermission Delete => AccessPermission.Create(name: "admin.user.delete",
                    displayName: "Delete User",
                    description: "Allows deleting users").Value;

                // New Role Management Permissions
                public static AccessPermission ViewRoles => AccessPermission.Create(name: "admin.user.view_roles",
                    displayName: "View User Roles",
                    description: "Allows viewing roles assigned to a user").Value;
                public static AccessPermission AssignRole => AccessPermission.Create(name: "admin.user.assign_role",
                    displayName: "Assign Role to User",
                    description: "Allows assigning roles to users").Value;
                public static AccessPermission UnassignRole => AccessPermission.Create(name: "admin.user.unassign_role",
                    displayName: "Unassign Role from User",
                    description: "Allows unassigning roles from users").Value;

                // New Permission Management Permissions
                public static AccessPermission ViewPermissions => AccessPermission.Create(name: "admin.user.view_permissions",
                    displayName: "View User Permissions",
                    description: "Allows viewing permissions (claims) assigned to a user").Value;
                public static AccessPermission AssignPermission => AccessPermission.Create(name: "admin.user.assign_permission",
                    displayName: "Assign Permission to User",
                    description: "Allows assigning permissions (claims) to users").Value;
                public static AccessPermission UnassignPermission => AccessPermission.Create(name: "admin.user.unassign_permission",
                    displayName: "Unassign Permission from User",
                    description: "Allows unassigning permissions (claims) from users").Value;

                public static AccessPermission[] All =>
                [
                    Create, List, View, Update, Delete,
                    ViewRoles, AssignRole, UnassignRole,
                    ViewPermissions, AssignPermission, UnassignPermission
                ];
            }
        }
    }
}