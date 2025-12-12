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

namespace ReSys.Core.Feature.Catalog.Taxonomies;

public static partial class TaxonomyModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/catalog/taxonomies")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: CreateHandler)
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxonomy.Create);

            group.MapPut(pattern: "{id:guid}", handler: UpdateHandler)
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxonomy.Update);

            group.MapDelete(pattern: "{id:guid}", handler: DeleteHandler)
                .UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxonomy.Delete);

            group.MapGet(pattern: "select", handler: GetSelectListHandler)
                .UseEndpointMeta(meta: Annotations.Get.SelectList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxonomy.List);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
                .UseEndpointMeta(meta: Annotations.Get.PagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxonomy.List);

            group.MapGet(pattern: "{id:guid}", handler: GetByIdHandler)
                .UseEndpointMeta(meta: Annotations.Get.ById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxonomy.View);

        }

        private static async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler([FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Get.ById.Query(Id: id);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Taxonomy retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.PagedList.Result>>>> GetPagedListHandler(
            [AsParameters] Get.PagedList.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Get.PagedList.Query(Request: request);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Taxonomies retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.SelectList.Result>>>> GetSelectListHandler(
            [AsParameters] Get.SelectList.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Get.SelectList.Query(Request: request);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Taxonomies retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse>> DeleteHandler([FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Delete.Command(Id: id);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponseDeleted(message: "Taxonomy deleted successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler([FromRoute] Guid id,
            [FromBody] Update.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Update.Command(Id: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponseCreated(message: "Taxonomy updated successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Create.Result>>> CreateHandler([FromBody] Create.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Create.Command(Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponseCreated(message: "Taxonomy created successfully");
            return TypedResults.Ok(value: apiResponse);
        }
    }
}