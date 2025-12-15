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

namespace ReSys.Core.Feature.Catalog.Taxons;

public static partial class TaxonModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/catalog/taxons")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: CreateHandler)
            .UseEndpointMeta(meta: Annotations.Create)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.Create);

            group.MapPost(pattern: "{id:guid}", handler: UpdateHandler)
            .UseEndpointMeta(meta: Annotations.Update)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.Update);

            group.MapDelete(pattern: "{id:guid}", handler: DeleteHandler)
            .UseEndpointMeta(meta: Annotations.Delete)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.Delete);

            group.MapGet(pattern: "{id:guid}", handler: GetByIdHandler)
            .UseEndpointMeta(meta: Annotations.Get.ById)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.View);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
            .UseEndpointMeta(meta: Annotations.Get.PagedList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.List);

            group.MapGet(pattern: "select", handler: GetSelectListHandler)
            .UseEndpointMeta(meta: Annotations.Get.SelectList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.List);

            // Image Management
            group.MapPost(pattern: "{id:guid}/images", handler: GetImagesHandler)
            .DisableAntiforgery()
            .UseEndpointMeta(meta: Annotations.Images.Update)
            .Accepts<Images.Batch.Request>(contentType: "multipart/form-data")
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.Update)
            .ProducesValidationProblem();

            group.MapGet(pattern: "{id:guid}/images", handler: UpdateImagesHandler)
                .UseEndpointMeta(meta: Annotations.Images.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.View);

            // Hierarchy Operations
            group.MapGet(pattern: "tree", handler: HierarchyGetTreeHandler)
            .UseEndpointMeta(meta: Annotations.Hierarchy.GetTree)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.List);

            group.MapGet(pattern: "flat", handler: HierarchyGetFlatListHandler)
            .UseEndpointMeta(meta: Annotations.Hierarchy.GetFlatList)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.List);

            group.MapPost(pattern: "rebuild/{taxonomyId:guid}", handler: HierarchyRebuildHandler)
            .UseEndpointMeta(meta: Annotations.Hierarchy.Rebuild)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.Update);

            group.MapGet(pattern: "/validate/{taxonomyId:guid}", handler: HierarchyValidateHandler)
            .UseEndpointMeta(meta: Annotations.Hierarchy.Validate)
            .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.View);

            // Rules Management
            group.MapGet(pattern: "{id:guid}/rules", handler: RulesGetHandler)
                .UseEndpointMeta(meta: Annotations.Rules.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.View);

            group.MapPut(pattern: "{id:guid}/rules", handler: RuleUpdateHandler)
                .UseEndpointMeta(meta: Annotations.Rules.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Taxon.Update);
        }

        private static async Task<Ok<ApiResponse<Rules.Update.Result>>> RuleUpdateHandler([FromRoute] Guid id, [FromBody] Rules.Update.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Rules.Update.Command(TaxonId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxon rules updated successfully"));
        }

        private async Task<Ok<ApiResponse<PagedList<Models.RuleItem>>>> RulesGetHandler([FromRoute] Guid id, [AsParameters] Rules.Get.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Rules.Get.Query(TaxonId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxon rules retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<Success>>> HierarchyValidateHandler([FromRoute] Guid taxonomyId, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Hierarchy.Validate.Query(TaxonomyId: taxonomyId), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxonomy hierarchy validated successfully"));
        }

        private async Task<Ok<ApiResponse<Success>>> HierarchyRebuildHandler([FromRoute] Guid taxonomyId, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Hierarchy.Rebuild.Command(TaxonomyId: taxonomyId), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxonomy hierarchy rebuilt successfully"));
        }

        private async Task<Ok<ApiResponse<PagedList<Models.FlatListItem>>>> HierarchyGetFlatListHandler([AsParameters] Hierarchy.FlatList.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Hierarchy.FlatList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Flat taxon list retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<Models.TreeListItem>>> HierarchyGetTreeHandler([AsParameters] Hierarchy.Tree.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Hierarchy.Tree.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxon tree retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<PagedList<Images.Get.Result>>>> UpdateImagesHandler([FromRoute] Guid id, [AsParameters] Images.Get.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Images.Get.Query(TaxonId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxon images retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<List<Images.Batch.Result>>>> GetImagesHandler([FromRoute] Guid id, [FromForm] Images.Batch.Request request, ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Images.Batch.Command(TaxonId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxon images updated successfully"));
        }

        private async Task<Ok<ApiResponse<PagedList<Get.SelectList.Result>>>> GetSelectListHandler([AsParameters] Get.SelectList.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.SelectList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxons retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<PagedList<Get.PagedList.Result>>>> GetPagedListHandler([AsParameters] Get.PagedList.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxons retrieved successfully"));
        }

        private async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Taxon retrieved successfully"));
        }

        private async Task<Ok<ApiResponse>> DeleteHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Taxon deleted successfully"));
        }

        private async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler([FromRoute] Guid id, [FromBody] Update.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Taxon updated successfully"));
        }

        private async Task<Ok<ApiResponse<Create.Result>>> CreateHandler([FromBody] Create.Request request, [FromServices] ISender mediator, CancellationToken ct)
        {
            var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Taxon created successfully"));
        }
    }
}