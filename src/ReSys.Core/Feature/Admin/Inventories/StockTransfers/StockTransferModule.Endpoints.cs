using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Inventories.StockTransfers;

public static partial class StockTransferModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/inventory/stock-transfers")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            // CRUD Operations
            group.MapPost(pattern: "/", handler: async (
                [FromBody] Create.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Stock transfer created successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Create)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockTransfer.Create);

            group.MapPost(pattern: "/{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromBody] Update.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Stock transfer updated successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Update)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockTransfer.Update);

            group.MapDelete(pattern: "/{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Stock transfer deleted successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Delete)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockTransfer.Delete);

            group.MapGet(pattern: "/{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Stock transfer retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetById)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockTransfer.View);

            group.MapGet(pattern: "/", handler: async (
                [AsParameters] Get.PagedList.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Stock transfers retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetPagedList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockTransfer.List);

            // Transfer Operations
            group.MapPost(pattern: "/{id:guid}/execute", handler: async (
                [FromRoute] Guid id,
                [FromBody] ExecuteTransfer.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new ExecuteTransfer.Command(Id: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Stock transfer executed successfully"));
            })
            .UseEndpointMeta(meta: Annotations.ExecuteTransfer)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockTransfer.Execute);

            group.MapPost(pattern: "/{id:guid}/receive", handler: async (
                [FromRoute] Guid id,
                [FromBody] ReceiveStock.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new ReceiveStock.Command(Id: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Stock received successfully"));
            })
            .UseEndpointMeta(meta: Annotations.ReceiveStock)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockTransfer.Receive);
        }
    }
}
