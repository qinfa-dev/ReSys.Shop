using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Inventories.StockLocations;

public static partial class StockLocationModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/inventory/stock-locations") // Direct string prefix
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            // CRUD Operations
            group.MapPost(pattern: string.Empty, handler: async ( 
                    [FromBody] Create.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Create.Command(Request: request),
                    cancellationToken: ct);
                return TypedResults.Ok(
                    value: result.ToApiResponseCreated(message: "Stock location created successfully"));
            })
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockLocation.Create);

            group.MapPut(pattern: "{id:guid}", handler: async ( 
                    [FromRoute] Guid id,
                    [FromBody] Update.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Update.Command(Id: id, Request: request),
                    cancellationToken: ct);
                return TypedResults.Ok( // Should be ToApiResponse, not ToApiResponseCreated for Update
                    value: result.ToApiResponse(message: "Stock location updated successfully"));
            })
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockLocation.Update);

            group.MapDelete(pattern: "{id:guid}", handler: async ( 
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(
                    value: result.ToApiResponseDeleted(message: "Stock location deleted successfully"));
            })
                .UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockLocation.Delete);

            group.MapPost(pattern: "{id:guid}/restore", handler: async ( 
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Restore.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(
                    value: result.ToApiResponse(message: "Stock location restored successfully"));
            })
                .UseEndpointMeta(meta: Annotations.Restore)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockLocation.Update);

            group.MapGet(pattern: "{id:guid}", handler: async ( 
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
                return TypedResults.Ok(
                    value: result.ToApiResponse(message: "Stock location retrieved successfully"));
            })
                .UseEndpointMeta(meta: Annotations.GetById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockLocation.View);

            group.MapGet(pattern: string.Empty, handler: async ( 
                    [AsParameters] Get.PagedList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.PagedList.Query(Request: request),
                    cancellationToken: ct);
                return TypedResults.Ok(
                    value: result.ToApiResponse(message: "Stock locations retrieved successfully"));
            })
                .UseEndpointMeta(meta: Annotations.GetPagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockLocation.List);

            group.MapGet(pattern: "select", handler: async ( 
                    [AsParameters] Get.SelectList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.SelectList.Query(Request: request),
                    cancellationToken: ct);
                return TypedResults.Ok(
                    value: result.ToApiResponse(message: "Stock locations retrieved successfully"));
            })
                .UseEndpointMeta(meta: Annotations.GetSelectList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Inventory.StockLocation.List);

        }
    }
}