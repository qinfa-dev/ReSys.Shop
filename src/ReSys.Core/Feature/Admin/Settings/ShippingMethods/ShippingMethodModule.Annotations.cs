using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Setting.ShippingMethod",
            Tags = ["Shipping Method Management"],
            Summary = "Shipping Method Management API",
            Description = "Endpoints for managing global shipping methods."
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Setting.ShippingMethod.Create",
            Summary = "Create a new shipping method",
            Description = "Creates a new global shipping method.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Setting.ShippingMethod.Update",
            Summary = "Update a shipping method",
            Description = "Updates an existing global shipping method by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Setting.ShippingMethod.Delete",
            Summary = "Delete a shipping method",
            Description = "Soft deletes a global shipping method by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        // Re-adding Activate annotation
        public static ApiEndpointMeta Activate => new()
        {
            Name = "Admin.Setting.ShippingMethod.Activate",
            Summary = "Activate a shipping method",
            Description = "Activates a shipping method by ID.",
            ResponseType = typeof(ApiResponse<Activate.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        // Re-adding Deactivate annotation
        public static ApiEndpointMeta Deactivate => new()
        {
            Name = "Admin.Setting.ShippingMethod.Deactivate",
            Summary = "Deactivate a shipping method",
            Description = "Deactivates a shipping method by ID.",
            ResponseType = typeof(ApiResponse<Deactivate.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Setting.ShippingMethod.GetById",
            Summary = "Get shipping method details",
            Description = "Retrieves details of a specific global shipping method by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Setting.ShippingMethod.GetPagedList",
            Summary = "Get paged list of shipping methods",
            Description = "Retrieves a paginated list of global shipping methods.",
            ResponseType = typeof(ApiResponse<PagedList<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Setting.ShippingMethod.GetSelectList",
            Summary = "Get selectable list of shipping methods",
            Description = "Retrieves a simplified list of global shipping methods for selection purposes.",
            ResponseType = typeof(ApiResponse<PagedList<Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}
