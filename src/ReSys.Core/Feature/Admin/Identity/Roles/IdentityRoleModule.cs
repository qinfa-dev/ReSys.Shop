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

namespace ReSys.Core.Feature.Admin.Identity.Roles;

public static partial class IdentityRoleModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/identity/roles")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: string.Empty, handler: CreateHandler)
                .UseEndpointMeta(meta: Annotations.Create)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.Create);

            group.MapPut(pattern: "{id}", handler: UpdateHandler)
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.Update);

            group.MapDelete(pattern: "{id}", handler: DeleteHandler)
                .UseEndpointMeta(meta: Annotations.Delete)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.Delete);

            group.MapGet(pattern: "{id}", handler: GetByIdHandler)
                .UseEndpointMeta(meta: Annotations.GetById)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.View);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
                .UseEndpointMeta(meta: Annotations.GetPagedList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.List);

            group.MapGet(pattern: "select",
                    handler: GetSelectHandler)
                .UseEndpointMeta(meta: Annotations.GetSelectList)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.List);

            // ---------------- Role Users Management Endpoints ----------------
            group.MapGet(pattern: "/{id}/users",
                    handler: GeUsersHandler)
                .UseEndpointMeta(meta: Annotations.Users.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.ViewUsers);

            group.MapPost(pattern: "/{id}/users/assign",
                    handler: AssignUserHandler)
                .UseEndpointMeta(meta: Annotations.Users.Assign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.AssignUser);

            group.MapPost(pattern: "/{id}/users/unassign",
                    handler: UnassginHandler)
                .UseEndpointMeta(meta: Annotations.Users.Unassign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.UnassignUser);

            // ---------------- Role Permissions Management Endpoints ----------------
            group.MapGet(pattern: "/{id}/permissions",
                    handler: GetPermissionsHandler)
                .UseEndpointMeta(meta: Annotations.Permissions.Get)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.ViewPermissions);

            group.MapPost(pattern: "/{id}/permissions/assign",
                    handler: AssignPermissionHandler)
                .UseEndpointMeta(meta: Annotations.Permissions.Assign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.AssignPermission);

            group.MapPost(pattern: "/{id}/permissions/unassign",
                    handler: UnassignPermissionHandler)
                .UseEndpointMeta(meta: Annotations.Permissions.Unassign)
                .RequireAccessPermission(permission: FeaturePermission.Admin.Identity.Role.UnassignPermission);
        }

        private static async Task<Ok<ApiResponse<Success>>> UnassignPermissionHandler([FromRoute] string id,
            [FromBody] Permissions.Unassign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Permissions.Unassign.Command(RoleId: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Permission unassigned from role successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Success>>> AssignPermissionHandler([FromRoute] string id,
            [FromBody] Permissions.Assign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Permissions.Assign.Command(RoleId: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Permission assigned to role successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<List<Permissions.GetList.Result>>>> GetPermissionsHandler(
            [FromRoute] string id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Permissions.GetList.Query query = new Permissions.GetList.Query(RoleId: id);
            ErrorOr<List<Permissions.GetList.Result>> result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            ApiResponse<List<Permissions.GetList.Result>> apiResponse = result.ToApiResponse(message: "Permissions in role retrieved successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Success>>> UnassginHandler([FromRoute] string id,
            [FromBody] Users.Unassign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Users.Unassign.Command(RoleId: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "User unassigned from role successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Success>>> AssignUserHandler([FromRoute] string id,
            [FromBody] Users.Assign.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new Users.Assign.Command(RoleId: id, Request: request);
            var result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "User assigned to role successfully");

            return TypedResults.Ok(value: apiResponse);
        }
        private static async Task<Ok<ApiResponse<List<Users.GetList.Result>>>> GeUsersHandler([FromRoute] string id,
            [AsParameters] Users.GetList.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Users.GetList.Query query = new Users.GetList.Query(RoleId: id, Request: request);
            ErrorOr<PagedList<Users.GetList.Result>> result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            ApiResponse<List<Users.GetList.Result>> apiResponse = result.ToApiResponsePaged(message: "Users in role retrieved successfully");

            return TypedResults.Ok(value: apiResponse);
        }
        private static async Task<Ok<ApiResponse<List<Get.SelectList.Result>>>> GetSelectHandler(
            [AsParameters] Get.SelectList.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Get.SelectList.Query query = new Get.SelectList.Query(Request: request);
            ErrorOr<PagedList<Get.SelectList.Result>> result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            ApiResponse<List<Get.SelectList.Result>> apiResponse = result.ToApiResponsePaged(message: "Roles retrieved successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<List<Get.PagedList.Result>>>> GetPagedListHandler(
            [AsParameters] Get.PagedList.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Get.PagedList.Query query = new Get.PagedList.Query(Request: request);
            ErrorOr<PagedList<Get.PagedList.Result>> result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            ApiResponse<List<Get.PagedList.Result>> apiResponse = result.ToApiResponsePaged(message: "Roles retrieved successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler([FromRoute] string id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Get.ById.Query query = new Get.ById.Query(Id: id);
            ErrorOr<Get.ById.Result> result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            ApiResponse<Get.ById.Result> apiResponse = result.ToApiResponse(message: "Role details retrieved successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse>> DeleteHandler([FromRoute] string id,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Delete.Command command = new Delete.Command(Id: id);
            ErrorOr<Deleted> result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            ApiResponse apiResponse = result.ToApiResponseDeleted(message: "Role deleted successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler([FromRoute] string id,
            [FromBody] Update.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Update.Command command = new Update.Command(Id: id, Request: request);
            ErrorOr<Update.Result> result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            ApiResponse<Update.Result> apiResponse = result.ToApiResponse(message: "Role updated successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Create.Result>>> CreateHandler([FromBody] Create.Request request,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            Create.Command command = new Create.Command(Request: request);
            ErrorOr<Create.Result> result = await mediator.Send(request: command, cancellationToken: cancellationToken);
            ApiResponse<Create.Result> apiResponse = result.ToApiResponseCreated(message: "Role created successfully");
            return TypedResults.Ok(value: apiResponse);
        }
    }
}