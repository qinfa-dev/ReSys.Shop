using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Settings.PaymentMethods;

public static partial class PaymentMethodModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/settings/payment-methods")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: async (
                [FromBody] Create.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Payment method created successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Create)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.PaymentMethod.Create);

            group.MapPut(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromBody] Update.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Payment method updated successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Update)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.PaymentMethod.Update);

            group.MapDelete(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Payment method deleted successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Delete)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.PaymentMethod.Delete);

            group.MapPost(pattern: "{id:guid}/restore", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Restore.Command(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Payment method restored successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Restore)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.PaymentMethod.Update); // Restoring is an update operation

            group.MapGet(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Payment method retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetById)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.PaymentMethod.View);

            group.MapGet(pattern: string.Empty, handler: async (
                [AsParameters] Get.PagedList.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Payment methods retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetPagedList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.PaymentMethod.List);

            group.MapGet(pattern: "select", handler: async (
                [AsParameters] Get.SelectList.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Get.SelectList.Query(Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Payment methods (select list) retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.GetSelectList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Setting.PaymentMethod.List);
        }
    }
}
