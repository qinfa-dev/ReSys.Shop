using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Catalog.PropertyTypes;

public static partial class PropertyTypeModule
{
    private static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Catalog.Property",
            Tags = ["Property"],
            Summary = "Property Management API",
            Description = "Endpoints for managing catalog properties"
        };

        // ---------------- Endpoint Metadata ----------------
        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Catalog.Property.Create",
            Summary = "Create a new property",
            Description = "Creates a new catalog property with the specified details.",
            ResponseType = typeof(ApiResponse<PropertyTypeModule.Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Catalog.Property.Delete",
            Summary = "Delete a property",
            Description = "Deletes a catalog property by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Catalog.Property.Get.ById",
            Summary = "Get property details",
            Description = "Retrieves details of a specific catalog property by ID.",
            ResponseType = typeof(ApiResponse<PropertyTypeModule.Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Catalog.Property.Get.PagedList",
            Summary = "Get paged list of properties",
            Description = "Retrieves a paginated list of catalog properties.",
            ResponseType = typeof(ApiResponse<List<PropertyTypeModule.Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Catalog.Property.Get.SelectList",
            Summary = "Get selectable list of properties",
            Description = "Retrieves a simplified list of catalog properties for selection purposes.",
            ResponseType = typeof(ApiResponse<List<PropertyTypeModule.Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Catalog.Property.Update",
            Summary = "Update a property",
            Description = "Updates an existing catalog property by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta UpdateDisplayOn => new()
        {
            Name = "Admin.Catalog.Property.Update.DisplayOn",
            Summary = "Update a property",
            Description = "Updates an existing catalog property's display on by ID.",
            ResponseType = typeof(ApiResponse<UpdateDisplayOn.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}