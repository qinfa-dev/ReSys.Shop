using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authorization.Attributes.Extensions;
using ReSys.Core.Feature.Common.Security.Authorization.Permissions.Constants;

namespace ReSys.Core.Feature.Admin.Promotions;

public static partial class PromotionModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/promotions")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            // CRUD Operations
            group.MapPost(pattern: string.Empty, handler: async (
                    [FromBody] Create.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Create.Command(Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Promotion created successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Create);

            group.MapPut(pattern: "{id:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] Update.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Update.Command(Id: id, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion updated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            group.MapDelete(pattern: "{id:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Delete.Command(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Promotion deleted successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Delete);

            group.MapGet(pattern: "{id:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Get.ById.Query(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.GetById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);

            group.MapGet(pattern: string.Empty, handler: async (
                    [AsParameters] Get.PagedList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Get.PagedList.Query(Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotions retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.GetPagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.List);

            group.MapGet(pattern: "/select", handler: async (
                    [AsParameters] Get.SelectList.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Get.SelectList.Query(Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotions retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.GetSelectList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.List);

            // Status Management
            group.MapPost(pattern: "{id:guid}/activate", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Activate.Command(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion activated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Activate)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            group.MapPost(pattern: "{id:guid}/deactivate", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Deactivate.Command(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion deactivated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Deactivate)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            group.MapGet(pattern: "{id:guid}/validate", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Validate.Query(Id: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion validated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Validate)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);

            // Rules Management
            group.MapGet(pattern: "{id:guid}/rules", handler: async (
                    [FromRoute] Guid id,
                    [AsParameters] Rules.Get.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Get.Query(PromotionId: id, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion rules retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);

            group.MapPost(pattern: "{id:guid}/rules", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] Rules.Add.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Add.Command(PromotionId: id, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponseCreated(message: "Rule added successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Add)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            group.MapPut(pattern: "{id:guid}/rules/{ruleId:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid ruleId,
                    [FromBody] Rules.Update.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Update.Command(PromotionId: id, RuleId: ruleId, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Rule updated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            group.MapDelete(pattern: "{id:guid}/rules/{ruleId:guid}", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid ruleId,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Delete.Command(PromotionId: id, RuleId: ruleId), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponseDeleted(message: "Rule deleted successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            // Rule Taxons Management
            group.MapGet(pattern: "{id:guid}/rules/{ruleId:guid}/taxons", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid ruleId,
                    [AsParameters] Rules.Taxons.Get.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Taxons.Get.Query(PromotionId: id, RuleId: ruleId, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion rule taxons retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Taxons.GetTaxons)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);

            group.MapPut(pattern: "{id:guid}/rules/{ruleId:guid}/taxons", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid ruleId,
                    [FromBody] Rules.Taxons.Manage.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Taxons.Manage.Command(PromotionId: id, RuleId: ruleId, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion rule taxons synchronized successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Taxons.Manage)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            // Rule Users Management
            group.MapGet(pattern: "{id:guid}/rules/{ruleId:guid}/users", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid ruleId,
                    [AsParameters] Rules.Users.Get.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Users.Get.Query(PromotionId: id, RuleId: ruleId, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion rule users retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Users.GetUsers)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);

            group.MapPut(pattern: "{id:guid}/rules/{ruleId:guid}/users", handler: async (
                    [FromRoute] Guid id,
                    [FromRoute] Guid ruleId,
                    [FromBody] Rules.Users.Manage.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Rules.Users.Manage.Command(PromotionId: id, RuleId: ruleId, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion rule users synchronized successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Rules.Users.ManageUsers)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.Update);

            // Analytics & Testing
            group.MapGet(pattern: "{id:guid}/stats", handler: async (
                    [FromRoute] Guid id,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Analytics.Stats.Get.Query(PromotionId: id), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion statistics retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Analytics.GetStats)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);

            group.MapPost(pattern: "/{id:guid}/preview", handler: async (
                    [FromRoute] Guid id,
                    [FromBody] Analytics.Preview.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Analytics.Preview.Command(PromotionId: id, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion preview calculated successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Analytics.Preview)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);

            group.MapGet(pattern: "{id:guid}/history", handler: async (
                    [FromRoute] Guid id,
                    [AsParameters] Analytics.History.Get.Request request,
                    [FromServices] ISender mediator,
                    CancellationToken ct) =>
                {
                    var result = await mediator.Send(request: new Analytics.History.Get.Query(PromotionId: id, Request: request), cancellationToken: ct);
                    return TypedResults.Ok(value: result.ToApiResponse(message: "Promotion history retrieved successfully"));
                })
                .UseEndpointMeta(meta: Annotations.Analytics.GetHistory)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Promotion.View);
        }
    }
}