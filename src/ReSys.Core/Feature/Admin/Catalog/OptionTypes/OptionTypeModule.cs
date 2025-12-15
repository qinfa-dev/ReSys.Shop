using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Catalog.OptionTypes;

public static partial class OptionTypeModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/catalog/option-types")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: async (
                [FromBody] Create.Request request,
                [FromServices] ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new Create.Command(Request: request);
                var result = await mediator.Send(request: command,
                    cancellationToken: cancellationToken);
                var apiResponse =
                    result.ToApiResponseCreated(message: "Option type created successfully");
                return TypedResults.Ok(value: apiResponse);
            })
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.OptionType.Create);

            group.MapPut(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromBody] Update.Request request,
                [FromServices] ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new Update.Command(Id: id, Request: request);
                var result = await mediator.Send(request: command,
                    cancellationToken: cancellationToken);
                var apiResponse = result.ToApiResponseCreated(message: "Option type updated successfully");
                return TypedResults.Ok(value: apiResponse);
            }).UseEndpointMeta(meta: Annotations.Update).RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.OptionType.Update);

            group.MapDelete(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var command = new Delete.Command(Id: id);
                var result = await mediator.Send(request: command,
                    cancellationToken: cancellationToken);
                var apiResponse = result.ToApiResponseDeleted(message: "Option type deleted successfully");
                return TypedResults.Ok(value: apiResponse);
            }).UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.OptionType.Delete);

            group.MapGet(pattern: "select", handler: async (
                [AsParameters] Get.SelectList.Request request,
                [FromServices] ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new Get.SelectList.Query(Request: request);
                var result = await mediator.Send(request: query,
                    cancellationToken: cancellationToken);
                var apiResponse = result.ToApiResponse(message: "Option types retrieved successfully");
                return TypedResults.Ok(value: apiResponse);
            }).UseEndpointMeta(meta: Annotations.GetSelectList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.OptionType.List);

            group.MapGet(pattern: string.Empty, handler: async (
                [AsParameters] Get.PagedList.Request request,
                [FromServices] ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new Get.PagedList.Query(Request: request);
                var result = await mediator.Send(request: query,
                    cancellationToken: cancellationToken);
                var apiResponse = result.ToApiResponse(message: "Option types retrieved successfully");
                return TypedResults.Ok(value: apiResponse);
            }).UseEndpointMeta(meta: Annotations.GetPagedList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.OptionType.List);

            group.MapGet(pattern: "{id:guid}", handler: async (
                [FromRoute] Guid id,
                [FromServices] ISender mediator,
                CancellationToken cancellationToken) =>
            {
                var query = new Get.ById.Query(Id: id);
                var result = await mediator.Send(request: query,
                    cancellationToken: cancellationToken);
                var apiResponse = result.ToApiResponse(message: "Option type retrieved successfully");
                return TypedResults.Ok(value: apiResponse);
            }).UseEndpointMeta(meta: Annotations.GetById)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.OptionType.View);

        }
    }
}
