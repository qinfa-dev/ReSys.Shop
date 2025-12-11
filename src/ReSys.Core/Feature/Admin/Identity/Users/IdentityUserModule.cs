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

namespace ReSys.Core.Feature.Admin.Identity.Users;

public static partial class IdentityUserModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/identity/users")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: CreateHandler)
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.Create);

            group.MapPut(pattern: "{id}", handler: UpdateHandler)
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.Update);

            group.MapDelete(pattern: "{id}", handler: DeleteHandler)
                .UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.Delete);

            group.MapGet(pattern: "select", handler: GetSelectListHandler)
                .UseEndpointMeta(meta: Annotations.GetSelectList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.List);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
                .UseEndpointMeta(meta: Annotations.GetPagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.List);

            group.MapGet(pattern: "{id}", handler: GetByIdHandler)
                .UseEndpointMeta(meta: Annotations.GetById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.View);

            group.MapGet(pattern: "/roles", handler: GetRolesHandler)
                .UseEndpointMeta(meta: Annotations.Roles.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.ViewRoles);

            group.MapPost(pattern: "/roles/assign", handler: AssignRoleHandler)
                .UseEndpointMeta(meta: Annotations.Roles.Assign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.AssignRole);

            group.MapPost(pattern: "/roles/unassign", handler: UnAssignRoleHandler)
                .UseEndpointMeta(meta: Annotations.Roles.Unassign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.UnassignRole);

            group.MapGet(pattern: "/permissions", handler: GetPermissionHandler)
                .UseEndpointMeta(meta: Annotations.Permissions.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.ViewPermissions);

            group.MapPost(pattern: "/permissions/assign", handler: AssignPermissionHandler)
                .UseEndpointMeta(meta: Annotations.Permissions.Assign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.AssignPermission);

            group.MapPost(pattern: "/permissions/unassign", handler: UnassignPermissionHandler)
                .UseEndpointMeta(meta: Annotations.Permissions.Unassign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.User.UnassignPermission);
        }

        private static async Task<Ok<ApiResponse<Success>>> UnassignPermissionHandler([FromQuery] string id,
            [FromBody] Permissions.Unassign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Permissions.Unassign.Command(UserId: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Permission unassigned successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Success>>> AssignPermissionHandler([FromQuery] string id,
            [FromBody] Permissions.Assign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Permissions.Assign.Command(id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Permission assigned successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<List<Models.PermissionItem>>>> GetPermissionHandler(
            [AsParameters] Permissions.GetList.Parameter id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Permissions.GetList.Query(id);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "User permissions retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Success>>> UnAssignRoleHandler([FromQuery] string? id,
            [FromBody] Roles.Unassign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Roles.Unassign.Command(id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Role unassigned successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Success>>> AssignRoleHandler([FromQuery] string? id,
            [FromBody] Roles.Assign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Roles.Assign.Command(UserId: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Role assigned successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<List<Roles.GetList.Result>>>> GetRolesHandler(
            [AsParameters] Roles.GetList.Parameter parameter,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Roles.GetList.Query(parameter);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "User roles retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler([FromRoute] string id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Get.ById.Query(Id: id);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "User retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.PagedList.Result>>>> GetPagedListHandler(
            [AsParameters] Get.PagedList.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Get.PagedList.Query(Request: request);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Users retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.SelectList.Result>>>> GetSelectListHandler(
            [AsParameters] Get.SelectList.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new Get.SelectList.Query(Request: request);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Users retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse>> DeleteHandler([FromRoute] string id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Delete.Command(Id: id);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponseDeleted(message: "User deleted successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler([FromRoute] string id,
            [FromBody] Update.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Update.Command(Id: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponseCreated(message: "User updated successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Create.Result>>> CreateHandler([FromBody] Create.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Create.Command(Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponseCreated(message: "User created successfully");
            return TypedResults.Ok(value: apiResponse);
        }
    }
}