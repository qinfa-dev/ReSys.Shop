using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Inventory.StockItem",
            Tags = ["Stock Item"],
            Summary = "Stock Item Management API",
            Description = "Endpoints for managing inventory stock items"
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Inventory.StockItem.Create",
            Summary = "Create a new stock item",
            Description = "Creates a new stock item with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Inventory.StockItem.Update",
            Summary = "Update a stock item",
            Description = "Updates an existing stock item by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Inventory.StockItem.Delete",
            Summary = "Delete a stock item",
            Description = "Deletes a stock item by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Inventory.StockItem.GetById",
            Summary = "Get stock item details",
            Description = "Retrieves details of a specific stock item by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Inventory.StockItem.GetPagedList",
            Summary = "Get paged list of stock items",
            Description = "Retrieves a paginated list of stock items.",
            ResponseType = typeof(ApiResponse<PagedList<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Adjust => new()
        {
            Name = "Admin.Inventory.StockItem.Adjust",
            Summary = "Adjust stock quantity",
            Description = "Adjusts the stock quantity for a stock item.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Reserve => new()
        {
            Name = "Admin.Inventory.StockItem.Reserve",
            Summary = "Reserve stock",
            Description = "Reserves stock for an order.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Release => new()
        {
            Name = "Admin.Inventory.StockItem.Release",
            Summary = "Release reserved stock",
            Description = "Releases previously reserved stock.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetMovements => new()
        {
            Name = "Admin.Inventory.StockItem.GetMovements",
            Summary = "Get stock movements",
            Description = "Retrieves stock movement history for a stock item.",
            ResponseType = typeof(ApiResponse<PagedList<Movements.Get.Result>>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}