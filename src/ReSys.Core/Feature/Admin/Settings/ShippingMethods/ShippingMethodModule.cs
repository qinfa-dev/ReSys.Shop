using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Settings.ShippingMethods;

public static partial class ShippingMethodModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/settings/shipping-methods")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: async (
                [FromBody] Create.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Shipping method created successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Create)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.Create);

            group.MapPut(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromBody] Update.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Shipping method updated successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Update)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.Update);

            group.MapDelete(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Shipping method deleted successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Delete)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.Delete);

            // Re-adding Activate endpoint
            group.MapPost(pattern: "{id:guid}/activate", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Activate.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Shipping method activated successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Activate)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.Update);

            // Re-adding Deactivate endpoint
            group.MapPost(pattern: "{id:guid}/deactivate", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Deactivate.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Shipping method deactivated successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Deactivate)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.Update);

            group.MapGet(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Shipping method retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetById)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.View);

            group.MapGet(pattern: string.Empty, handler: async (
                [AsParameters] Get.PagedList.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Shipping methods retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetPagedList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.List);

            group.MapGet(pattern: "select", handler: async (
                [AsParameters] Get.SelectList.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.SelectList.Query(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Shipping methods (select list) retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetSelectList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.ShippingMethod.List);
        }
    }
}