using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Catalog.Taxons;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Catalog.Products;

public static partial class ProductModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/catalog/products")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            // CRUD Operations
            group.MapPost(pattern: string.Empty, handler: CreateHandler)
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Create);

            group.MapPut(pattern: "{id:guid}", handler: UpdateHandler)
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update);

            group.MapDelete(pattern: "{id:guid}", handler: DeleteHandler)
                .UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Delete);

            group.MapGet(pattern: "{id:guid}", handler: GetByIdHandler)
                .UseEndpointMeta(meta: Annotations.Get.ById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.View);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
                .UseEndpointMeta(meta: Annotations.Get.PagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.List);

            group.MapGet(pattern: "/select", handler: GetSelectListHandler)
                .UseEndpointMeta(meta: Annotations.Get.SelectList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.List);

            // Status Management
            group.MapPatch(pattern: "/{id:guid}/activate", handler: ActivateProductHandler)
                .UseEndpointMeta(meta: Annotations.Status.Activate)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update);

            group.MapPatch(pattern: "/{id:guid}/archive", handler: ArchiveProductHandler)
                .UseEndpointMeta(meta: Annotations.Status.Archive)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update);

            group.MapPatch(pattern: "/{id:guid}/draft", handler: DraftProductHandler)
                .UseEndpointMeta(meta: Annotations.Status.Draft)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update);

            group.MapPatch(pattern: "/{id:guid}/discontinue", handler: DiscontinueProductHandler)
                .UseEndpointMeta(meta: Annotations.Status.Discontinue)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update);

            // Image Management
            group.MapPut(pattern: "{id:guid}/images/batch", handler: ManageBatchImagesHandler)
                .DisableAntiforgery()
                .UseEndpointMeta(meta: Annotations.Images.Manage)
                .Accepts<Images.Manage.Request>(contentType: "multipart/form-data")
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update)
                .ProducesValidationProblem();

            group.MapPost(pattern: "{id:guid}/images/", handler: UploadImageHandler)
                .DisableAntiforgery()
                .UseEndpointMeta(meta: Annotations.Images.Upload)
                .Accepts<Images.Upload.Request>(contentType: "multipart/form-data")
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update)
                .ProducesValidationProblem();

            group.MapPut(pattern: "{id:guid}/images/{imageId:guid}", handler: EditImageHandler)
                .DisableAntiforgery()
                .UseEndpointMeta(meta: Annotations.Images.Edit)
                .Accepts<Images.Edit.Request>(contentType: "multipart/form-data")
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update)
                .ProducesValidationProblem();

            group.MapDelete(pattern: "{id:guid}/images/{imageId:guid}", handler: RemoveImageHandler)
                .UseEndpointMeta(meta: Annotations.Images.Remove)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update);

            group.MapGet(pattern: "images", handler: GetImagesHandler)
                .UseEndpointMeta(meta: Annotations.Images.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.View);

            // Classifications Management
            group.MapGet(pattern: "classifications", handler: GetClassificationHandler)
                .UseEndpointMeta(meta: Annotations.Classifications.Get)
                .RequireAccessPermissions(permissions: FeaturePermission.Admin.Catalog.Product.List);

            group.MapPut(pattern: "{id:guid}/classifications", handler: ManageClassificationsHandler)
                .UseEndpointMeta(meta: Annotations.Classifications.Manage)
                .RequireAccessPermissions(permissions: FeaturePermission.Admin.Catalog.Product.Update);

            // Option Types Management
            group.MapGet(pattern: "option-types", handler: GetOptionTypesHandler)
                .UseEndpointMeta(meta: Annotations.OptionTypes.Get)
                .RequireAccessPermissions(permissions: FeaturePermission.Admin.Catalog.Product.List);

            group.MapPut(pattern: "{id:guid}/option-types", handler: ManageOptionTypesHandler)
                .UseEndpointMeta(meta: Annotations.OptionTypes.Manage)
                .RequireAccessPermissions(permissions: FeaturePermission.Admin.Catalog.Product.Update);

            // Properties Management
            group.MapGet(pattern: "properties", handler: GetProductPropertyTypesListHandler)
                .UseEndpointMeta(meta: Annotations.Properties.Get)
                .RequireAccessPermissions(permissions: FeaturePermission.Admin.Catalog.Property.List);

            group.MapPut(pattern: "{id:guid}/properties", handler: ManageProductPropertyTypesHandler)
                .UseEndpointMeta(meta: Annotations.Properties.Manage)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Catalog.Product.Update);
        }

        // ==================== CRUD Handlers ====================

        private static async Task<Ok<ApiResponse<Create.Result>>> CreateHandler(
            [FromBody] Create.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Product created successfully"));
        }

        private static async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler(
            [FromRoute] Guid id,
            [FromBody] Update.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Product updated successfully"));
        }

        private static async Task<Ok<ApiResponse>> DeleteHandler(
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Product deleted successfully"));
        }

        private static async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler(
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.PagedList.Result>>>> GetPagedListHandler(
            [AsParameters] Get.PagedList.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Products retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.SelectList.Result>>>> GetSelectListHandler(
            [AsParameters] Get.SelectList.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Get.SelectList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Products retrieved successfully"));
        }

        // ==================== Status Handlers ====================

        private static async Task<Ok<ApiResponse<Success>>> ActivateProductHandler(
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Statuses.Activate.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product activated successfully"));
        }

        private static async Task<Ok<ApiResponse<Success>>> ArchiveProductHandler(
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Statuses.Archive.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product archived successfully"));
        }

        private static async Task<Ok<ApiResponse<Success>>> DraftProductHandler(
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Statuses.Draft.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product set to draft successfully"));
        }

        private static async Task<Ok<ApiResponse<Success>>> DiscontinueProductHandler(
            [FromRoute] Guid id,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Statuses.Discontinue.Command(Id: id), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product discontinued successfully"));
        }

        // ==================== Image Handlers ====================

        private static async Task<Ok<ApiResponse<List<Images.Manage.Result>>>> ManageBatchImagesHandler(
            [FromRoute] Guid id,
            [FromForm] Images.Manage.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Images.Manage.Command(ProductId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product images updated successfully"));
        }

        private static async Task<Ok<ApiResponse<Images.Upload.Result>>> UploadImageHandler(
            [FromRoute] Guid id,
            [FromForm] Images.Upload.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Images.Upload.Command(ProductId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product image uploaded successfully"));
        }

        private static async Task<Ok<ApiResponse<Images.Edit.Result>>> EditImageHandler(
            [FromRoute] Guid id,
            [FromRoute] Guid imageId,
            [FromForm] Images.Edit.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Images.Edit.Command(ProductId: id, ImageId: imageId, request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product image edited successfully"));
        }

        private static async Task<Ok<ApiResponse>> RemoveImageHandler(
            [FromRoute] Guid id,
            [FromRoute] Guid imageId,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Images.Remove.Command(ProductId: id, ImageId: imageId), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Product image removed successfully"));
        }

        private static async Task<Ok<ApiResponse<PagedList<Images.GetList.Result>>>> GetImagesHandler(
            [AsParameters] Images.GetList.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Images.GetList.Query( Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product images retrieved successfully"));
        }

        // ==================== Classification Handlers ====================

        private static async Task<Ok<ApiResponse<PagedList<Classifications.Get.SelectList.Result>>>> GetClassificationHandler(
            [AsParameters] Classifications.Get.SelectList.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Classifications.Get.SelectList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Classifications retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse<Success>>> ManageClassificationsHandler(
            [FromRoute] Guid id,
            [FromBody] Classifications.Manage.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new Classifications.Manage.Command(ProductId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product classifications updated successfully"));
        }

        // ==================== Option Type Handlers ====================

        private static async Task<Ok<ApiResponse<PagedList<OptionTypes.Get.SelectList.Result>>>> GetOptionTypesHandler(
            [AsParameters] OptionTypes.Get.SelectList.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new OptionTypes.Get.SelectList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Option types retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse<Success>>> ManageOptionTypesHandler(
            [FromRoute] Guid id,
            [FromBody] OptionTypes.Manage.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new OptionTypes.Manage.Command(ProductId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product option types updated successfully"));
        }

        // ==================== Property Type Handlers ====================

        private static async Task<Ok<ApiResponse<PagedList<PropertyType.Get.SelectList.Result>>>> GetProductPropertyTypesListHandler(
            [AsParameters] PropertyType.Get.SelectList.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new PropertyType.Get.SelectList.Query(Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Properties retrieved successfully"));
        }

        private static async Task<Ok<ApiResponse<List<PropertyType.Manage.Result>>>> ManageProductPropertyTypesHandler(
            [FromRoute] Guid id,
            [FromBody] PropertyType.Manage.Request request,
            [FromServices] ISender mediator,
            CancellationToken ct)
        {
            var result = await mediator.Send(request: new PropertyType.Manage.Command(ProductId: id, Request: request), cancellationToken: ct);
            return TypedResults.Ok(value: result.ToApiResponse(message: "Product properties updated successfully"));
        }
    }
}