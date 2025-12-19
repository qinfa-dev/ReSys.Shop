using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Inventories.StockItems;

public static partial class StockItemModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/inventory/stock-items")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: CreateHandler)
            .UseEndpointMeta(meta: Annotations.Create)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.Create);

            group.MapPost(pattern: "{id:guid}", handler: UpdateHandler)
            .UseEndpointMeta(meta: Annotations.Update)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.Update);

            group.MapDelete(pattern: "{id:guid}", handler: DeleteHandler)
            .UseEndpointMeta(meta: Annotations.Delete)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.Delete);

            group.MapGet(pattern: "{id:guid}", handler: GetByIdHandler)
            .UseEndpointMeta(meta: Annotations.GetById)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.View);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
            .UseEndpointMeta(meta: Annotations.GetPagedList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.List);

            // Stock Management Operations
            group.MapPost(pattern: "{id:guid}/adjust", handler: AdjustHandler)
            .UseEndpointMeta(meta: Annotations.Adjust)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.Update);

            group.MapPost(pattern: "{id:guid}/reserve", handler: ReserveHandler)
            .UseEndpointMeta(meta: Annotations.Reserve)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.Update);

            group.MapPost(pattern: "{id:guid}/release", handler: ReleaseHandler)
            .UseEndpointMeta(meta: Annotations.Release)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.Update);

            group.MapGet(pattern: "{id:guid}/movements", handler: GetStockMovementHandler)
            .UseEndpointMeta(meta: Annotations.GetMovements)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockItem.View);
        }

        private static async Task<Ok<ApiResponse<Success>>> ReleaseHandler([FromRoute] Guid id, [FromBody] Release.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Release.Command(Id: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Stock released successfully"));
        }

        private static async Task<Ok<ApiResponse<Success>>> ReserveHandler([FromRoute] Guid id, [FromBody] Reserve.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Reserve.Command(Id: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Stock reserved successfully"));
        }

        private static async Task<Ok<ApiResponse<Success>>> AdjustHandler([FromRoute] Guid id, [FromBody] Adjust.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Adjust.Command(Id: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Stock adjusted successfully"));
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.PagedList.Result>>>> GetPagedListHandler([AsParameters] Get.PagedList.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Stock items retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Stock item retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse>> DeleteHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Stock item deleted successfully"));
        }

        private static async Task<Ok<ApiResponse<PagedList<Movements.Get.Result>>>> GetStockMovementHandler([FromRoute] Guid id, [AsParameters] Movements.Get.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Movements.Get.Query(StockItemId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Stock movements retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse<Create.Result>>> CreateHandler([FromBody] Create.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Stock item created successfully"));
        }

        private static async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler([FromRoute] Guid id, [FromBody] Update.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Stock item updated successfully"));
        }
    }

}