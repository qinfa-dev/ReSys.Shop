using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    private static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Identity.Role",
            Tags = ["Role"],
            Summary = "Identity Role Management API",
            Description =
                "Administrative endpoints for role management including CRUD operations, user assignment, and permission management"
        };

        // ---------------- Endpoint Metadata ----------------
        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Identity.Role.Create",
            Summary = "Create a new role",
            Description = "Creates a new role with specified name and description",
            ResponseType = typeof(ApiResponse<IdentityRoleModule.Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Identity.Role.Update",
            Summary = "Update role information",
            Description = "Updates role description and properties (name cannot be changed for system roles)",
            ResponseType = typeof(ApiResponse<IdentityRoleModule.Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Identity.Role.Delete",
            Summary = "Delete a role",
            Description = "Permanently deletes a role (cannot delete system roles or roles with users)",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Identity.Role.GetById",
            Summary = "Get role by ID",
            Description =
                "Retrieves detailed information about a specific role including permissions and user count",
            ResponseType = typeof(ApiResponse<IdentityRoleModule.Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Identity.Role.GetPagedList",
            Summary = "List roles with pagination",
            Description = "Retrieves a paginated list of roles with their information",
            ResponseType = typeof(ApiResponse<List<IdentityRoleModule.Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Identity.Role.GetSelectList",
            Summary = "List roles with select",
            Description = "Retrieves a select list of roles with their information",
            ResponseType = typeof(ApiResponse<List<IdentityRoleModule.Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        // ---------------- Role Users Management Endpoints ----------------
        public static class Users
        {
            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Identity.Role.Users.Get",
                Summary = "Get users assigned to a role",
                Description = "Retrieves a paginated list of users assigned to a specific role.",
                ResponseType = typeof(ApiResponse<List<IdentityRoleModule.Users.GetList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Assign => new()
            {
                Name = "Admin.Identity.Role.Users.Assign",
                Summary = "Assign a user to a role",
                Description = "Assigns a specific user to a role.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Unassign => new()
            {
                Name = "Admin.Identity.Role.Users.Unassign",
                Summary = "Unassign a user from a role",
                Description = "Unassigns a specific user from a role.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }
       

        // ---------------- Role Permissions Management Endpoints ----------------
        public static class Permissions
        {
            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Identity.Role.Permissions.Get",
                Summary = "Get permissions assigned to a role",
                Description = "Retrieves a list of permissions assigned to a specific role.",
                ResponseType = typeof(ApiResponse<List<IdentityRoleModule.Permissions.GetList.Result>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Assign => new()
            {
                Name = "Admin.Identity.Role.Permissions.Assign",
                Summary = "Assign a permission to a role",
                Description = "Assigns a specific permission (claim) to a role.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Unassign => new()
            {
                Name = "Admin.Identity.Role.Permissions.Unassign",
                Summary = "Unassign a permission from a role",
                Description = "Unassigns a specific permission (claim) from a role.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }
      
    }
}