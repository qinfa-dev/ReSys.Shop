using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Admin.Inventories.StockTransfers;

public static partial class StockTransferModule
{
    private static class Annotations
    {
        public static ApiGroupMeta Group => new()
        {
            Name = "Admin.Inventory.StockTransfer",
            Tags = ["Stock Transfer Management"],
            Summary = "Stock Transfer Management API",
            Description = "Endpoints for managing inventory stock transfers"
        };

        public static ApiEndpointMeta Create => new()
        {
            Name = "Admin.Inventory.StockTransfer.Create",
            Summary = "Create a new stock transfer",
            Description = "Creates a new stock transfer with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Admin.Inventory.StockTransfer.Update",
            Summary = "Update a stock transfer",
            Description = "Updates an existing stock transfer by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Admin.Inventory.StockTransfer.Delete",
            Summary = "Delete a stock transfer",
            Description = "Deletes a stock transfer by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Admin.Inventory.StockTransfer.GetById",
            Summary = "Get stock transfer details",
            Description = "Retrieves details of a specific stock transfer by ID.",
            ResponseType = typeof(ApiResponse<Get.ById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Admin.Inventory.StockTransfer.GetPagedList",
            Summary = "Get paged list of stock transfers",
            Description = "Retrieves a paginated list of stock transfers.",
            ResponseType = typeof(ApiResponse<PagedList<Get.PagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta ExecuteTransfer => new()
        {
            Name = "Admin.Inventory.StockTransfer.Execute",
            Summary = "Execute stock transfer",
            Description = "Executes a stock transfer between locations.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta ReceiveStock => new()
        {
            Name = "Admin.Inventory.StockTransfer.Receive",
            Summary = "Receive stock from supplier",
            Description = "Receives stock from external supplier (no source location).",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };
    }
}
