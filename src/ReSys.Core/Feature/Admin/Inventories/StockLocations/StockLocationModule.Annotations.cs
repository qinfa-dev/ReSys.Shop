using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Inventory.StockLocation",
            Tags = ["Stock Location Management"],
            Summary = "Stock Location Management API",
            Description = "Endpoints for managing inventory stock locations"
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Inventory.StockLocation.Create",
            Summary = "Create a new stock location",
            Description = "Creates a new stock location with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Inventory.StockLocation.Update",
            Summary = "Update a stock location",
            Description = "Updates an existing stock location by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Inventory.StockLocation.Delete",
            Summary = "Delete a stock location",
            Description = "Soft deletes a stock location by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Restore => new()
        {
            Name = "Admin.Inventory.StockLocation.Restore",
            Summary = "Restore a stock location",
            Description = "Restores a soft-deleted stock location by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Inventory.StockLocation.GetById",
            Summary = "Get stock location details",
            Description = "Retrieves details of a specific stock location by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Inventory.StockLocation.GetPagedList",
            Summary = "Get paged list of stock locations",
            Description = "Retrieves a paginated list of stock locations.",
            ResponseType = typeof(ApiResponse<PagedList<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Admin.Inventory.StockLocation.GetSelectList",
            Summary = "Get selectable list of stock locations",
            Description = "Retrieves a simplified list of stock locations for selection purposes.",
            ResponseType = typeof(ApiResponse<PagedList<Get.SelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}
