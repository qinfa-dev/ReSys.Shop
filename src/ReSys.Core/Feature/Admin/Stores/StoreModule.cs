using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Stores;

public static partial class StoreModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/stores")
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
                    return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Store created successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Create);

            group.MapPut(pattern: "{id:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] Update.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Update.Command(Id: id, Request: request),
                        cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Store updated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapPut(pattern: "{id:guid}/addresses", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] SetAddress.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new SetAddress.Command(Id: id, Request: request),
                        cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Store address updated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.SetAddress)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapPut(pattern: "{id:guid}/social-links", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] SetSocialLinks.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new SetSocialLinks.Command(Id: id, Request: request),
                        cancellationToken: ct);
                    return TypedResults.Ok(
                        value: result.ToApiResponse(message: "Store social links updated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.SetSocialLinks)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapPost(pattern: "{id:guid}/make-default", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new MakeDefault.Command(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Store set as default successfully"));
                })
                .UseEndpointMeta(meta: Annotations.MakeDefault)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapPost(pattern: "{id:guid}/protect-with-password", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] ProtectWithPassword.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new ProtectWithPassword.Command(Id: id, Request: request),
                        cancellationToken: ct);
                    return TypedResults.Ok(
                        value: result.ToApiResponse(message: "Store protected with password successfully"));
                })
                .UseEndpointMeta(meta: Annotations.ProtectWithPassword)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapPost(pattern: "{id:guid}/remove-password-protection", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new RemovePasswordProtection.Command(Id: id),
                        cancellationToken: ct);
                    return TypedResults.Ok(
                        value: result.ToApiResponse(message: "Store password protection removed successfully"));
                })
                .UseEndpointMeta(meta: Annotations.RemovePasswordProtection)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapDelete(pattern: "{id:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Store deleted successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Delete);

            group.MapPost(pattern: "{id:guid}/restore", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Restore.Command(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Store restored successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Restore)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store
                    .Update); // Restoring is an update operation

            group.MapGet(pattern: "{id:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Store retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.GetById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.View);

            group.MapGet(pattern: string.Empty, handler: async (
                    [AsParameters] Get.PagedList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Get.PagedList.Query(Request: request),
                        cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Stores retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.GetPagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.List);

            // Product Methods Endpoints
            group.MapPost(pattern: "{id:guid}/products", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] Products.Add.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(new Products.Add.Command(id, request), ct);
                    return TypedResults.Ok(value: result.ToApiResponse("Product added to store successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Products.Add)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update); // Add permission, might need specific Create permission

            group.MapPut(pattern: "{id:guid}/products/{productId:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid productId,
                    [FromBody] Products.Update.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(new Products.Update.Command(id, productId, request), ct);
                    return TypedResults.Ok(value: result.ToApiResponse("Product settings in store updated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Products.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapDelete(pattern: "{id:guid}/products/{productId:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid productId,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(new Products.Remove.Command(id, productId), ct);
                    return TypedResults.Ok(value: result.ToApiResponse("Product removed from store successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Products.Remove)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update); // Remove permission, might need specific Delete permission

            group.MapGet(pattern: "{id:guid}/products/{productId:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid productId,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(new Products.Get.ById.Query(id, productId), ct);
                    return TypedResults.Ok(value: result.ToApiResponse("Store product retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Products.GetById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.View);

            group.MapGet(pattern: "products", handler: async (
                    [AsParameters] Products.Get.PagedList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(new Products.Get.PagedList.Query(request), ct);
                    return TypedResults.Ok(value: result.ToApiResponse("Store products retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Products.GetPagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.View);

            // Payment Methods Endpoints
            group.MapPost(pattern: "{storeId:guid}/payment-methods/manage", handler: async ( // Corrected pattern
                    [FromRoute] Guid storeId,
                    [FromBody] PaymentMethods.Manage.Request request,
                    ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(new PaymentMethods.Manage.Command(storeId, request), ct);
                    return TypedResults.Ok(value: result.ToApiResponse("Payment methods synchronized successfully"));
                })
                .UseEndpointMeta(meta: Annotations.PaymentMethods.Manage)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Store.Update);

            group.MapGet("/payment-methods", async (
                    [AsParameters] PaymentMethods.Get.PagedList.Request request,
                    ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(new PaymentMethods.Get.PagedList.Query(request), ct);
                    return TypedResults.Ok(result);
                })
                .UseEndpointMeta(Annotations.GetPagedList)
                .RequireAccessPermission(FeaturePermission.Admin.Store.View);

            // Shipping Methods Endpoints
            group.MapPost("{storeId:guid}/shipping-methods/manage", async (
                    [FromRoute] Guid storeId,
                    [FromBody] ShippingMethods.Manage.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(new ShippingMethods.Manage.Command(storeId, request), ct);
                return TypedResults.Ok(result.ToApiResponse("Store shipping methods synchronized successfully"));
            })
                .UseEndpointMeta(Annotations.ShippingMethods.Manage)
                .RequireAccessPermission(FeaturePermission.Admin.Store.Update);

            group.MapGet("/shipping-methods", async (
                    [AsParameters] ShippingMethods.Get.PagedList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
            {
                var result = await mediator.Send(new ShippingMethods.Get.PagedList.Query(request), ct);
                return TypedResults.Ok(result.ToApiResponse("Store shipping methods retrieved successfully"));
            })
                .UseEndpointMeta(Annotations.ShippingMethods.GetPagedList)
                .RequireAccessPermission(FeaturePermission.Admin.Store.View);
        }
    }
}
