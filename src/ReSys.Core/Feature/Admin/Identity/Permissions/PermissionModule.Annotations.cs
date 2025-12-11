using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Identity.Permissions;

public static partial class PermissionModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Identity.Permission",
            Tags = ["Permission"],
            Summary = "Permission Management API",
            Description = "Endpoints for managing access permissions"
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Identity.Permission.GetById",
            Summary = "Get access permission details",
            Description = "Retrieves details of a specific access permission by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetByName => new()
        {
            Name = "Admin.Identity.Permission.GetByName",
            Summary = "Get access permission details by name",
            Description = "Retrieves details of a specific access permission by Name.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>), 
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Account.Permission.GetPagedList",
            Summary = "Get paged list of access permissions",
            Description = "Retrieves a paginated list of access permissions.",
            ResponseType = typeof(ApiResponse<List<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Account.Permission.GetSelectList",
            Summary = "Get selectable list of access permissions",
            Description = "Retrieves a simplified list of access permissions for selection purposes.",
            ResponseType = typeof(ApiResponse<List<Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}