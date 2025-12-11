using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    private static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Identity.User",
            Tags = ["User"],
            Summary = "Identity User Management API",
            Description = "Endpoints for managing identity users"
        };

        // ---------------- Endpoint Metadata ----------------
        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Identity.User.Create",
            Summary = "Create identity user",
            Description = "Creates a new identity user with the provided information.",
            ResponseType = typeof(ApiResponse<IdentityUserModule.Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Identity.User.Delete",
            Summary = "Delete identity user",
            Description = "Deletes an identity user by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Identity.User.GetById",
            Summary = "Get identity user details",
            Description = "Retrieves identity user details by ID.",
            ResponseType = typeof(ApiResponse<IdentityUserModule.Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Identity.User.Get.PagedList",
            Summary = "Get paged user list",
            Description = "Retrieves a paginated list of identity users.",
            ResponseType = typeof(ApiResponse<List<IdentityUserModule.Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Identity.User.Get.SelectList",
            Summary = "Get user select list",
            Description = "Retrieves a simplified list of identity users for selection inputs.",
            ResponseType = typeof(ApiResponse<List<IdentityUserModule.Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Identity.User.Update",
            Summary = "Update identity user",
            Description = "Updates an existing identity user by ID.",
            ResponseType = typeof(ApiResponse<IdentityUserModule.Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        // ---------------- Role Management Endpoints ----------------
        public static class Roles
        {
            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Identity.User.Role.Get",
                Summary = "Get user roles",
                Description = "Retrieves all roles assigned to a specific user.",
                ResponseType = typeof(ApiResponse<List<Models.RoleItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Assign => new()
            {
                Name = "Admin.Identity.User.Role.Assign",
                Summary = "Assign role to user",
                Description = "Assigns a role to a specific user.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Unassign => new()
            {
                Name = "Admin.Identity.User.Role.Unassign",
                Summary = "Unassign role from user",
                Description = "Unassigns a role from a specific user.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }

        // ---------------- Permission Management Endpoints ----------------
        public static class Permissions
        {
            public static ApiEndpointMeta Get => new()
            {
                Name = "Admin.Identity.User.GetPermissions",
                Summary = "Get user permissions",
                Description = "Retrieves all permissions (claims) assigned to a specific user.",
                ResponseType = typeof(ApiResponse<List<Models.PermissionItem>>),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Assign => new()
            {
                Name = "Admin.Identity.User.AssignPermission",
                Summary = "Assign permission to user",
                Description = "Assigns a permission (claim) to a specific user.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };

            public static ApiEndpointMeta Unassign => new()
            {
                Name = "Admin.Identity.User.UnassignPermission",
                Summary = "Unassign permission from user",
                Description = "Unassigns a permission (claim) from a specific user.",
                ResponseType = typeof(ApiResponse),
                StatusCode = StatusCodes.Status200OK
            };
        }

    }
}