using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Admin.Catalog.OptionValues;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Catalog.Variants;

public static partial class VariantModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/catalog/variants")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: CreateHandler)
            .UseEndpointMeta(meta: Annotations.Create)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.Create);

            group.MapPut(pattern: "{id:guid}", handler: UpdateHandler)
            .UseEndpointMeta(meta: Annotations.Update)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.Update);

            group.MapDelete(pattern: "{id:guid}", handler: DeleteHandler)
            .UseEndpointMeta(meta: Annotations.Delete)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.Delete);

            group.MapPost(pattern: "{id:guid}/discontinue", handler: DiscontinueHandler)
                .UseEndpointMeta(meta: Annotations.Discontinue)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.Update);

            group.MapGet(pattern: "{id:guid}", handler: GetByIdHandler)
                .UseEndpointMeta(meta: Annotations.Get.ById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.View);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
            .UseEndpointMeta(meta: Annotations.Get.PagedList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.List);

            group.MapGet(pattern: "select", handler: GetSelectListHandler)
            .UseEndpointMeta(meta: Annotations.Get.SelectList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.List);

            // Price Management
            group.MapPost(pattern: "{id:guid}/prices", handler: async (
                [FromRoute] Guid id,
                [FromBody] Prices.Set.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Prices.Set.Command(VariantId: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Variant price set successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Prices.Set)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.Update);

            group.MapGet(pattern: "{id:guid}/prices", handler: async (
                [FromRoute] Guid id,
                [AsParameters] Prices.Get.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new Prices.Get.Query(VariantId: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Variant prices retrieved successfully"));
            })
            .UseEndpointMeta(meta: Annotations.Prices.List)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.View);

            // Option Values Management
            group.MapGet(pattern: "option-values", handler: async (
                    [AsParameters] OptionValueModule.Get.SelectList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new OptionValueModule.Get.SelectList.Query(Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Variant option values Get successfully"));
                })
                .UseEndpointMeta(meta: Annotations.OptionValues.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.View);

            group.MapPut(pattern: "{id:guid}/option-values", handler: async (
                [FromRoute] Guid id,
                [FromBody] OptionValues.Manage.Request request,
                [FromServices] ISender mediator,
                CancellationToken ct) =>
            {
                var result = await mediator.Send(request: new OptionValues.Manage.Command(VariantId: id, Request: request), cancellationToken: ct);
                return TypedResults.Ok(value: result.ToApiResponse(message: "Variant option values updated successfully"));
            })
            .UseEndpointMeta(meta: Annotations.OptionValues.Manage)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Variant.Update);
        }

        private async Task<Ok<ApiResponse<PagedList<Get.SelectList.Result>>>> GetSelectListHandler([AsParameters] Get.SelectList.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.SelectList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Variants retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<PagedList<Get.PagedList.Result>>>> GetPagedListHandler([AsParameters] Get.PagedList.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Variants retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Variant retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<Success>>> DiscontinueHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Discontinue.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Variant discontinued successfully"));
        }

        private async Task<Ok<ApiResponse>> DeleteHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Variant deleted successfully"));
        }

        private async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler([FromRoute] Guid id, [FromBody] Update.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Variant updated successfully"));
        }

        private async Task<Ok<ApiResponse<Create.Result>>> CreateHandler([FromBody] Create.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Variant created successfully"));
        }
    }
}