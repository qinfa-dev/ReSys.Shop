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

namespace ReSys.Core.Feature.Admin.Identity.Permissions;

public static partial class PermissionModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/admin/account/permissions")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapGet(pattern: "select", handler: GetSelectListHandler)
                .UseEndpointMeta(meta: Annotations.GetSelectList)
                .RequireAccessPermission(Common.Security.Authorization.Permissions.Constants.FeaturePermission.Admin.Identity.AccessControlPermission.List);

            group.MapGet(pattern: string.Empty, handler: GetPagedListHandler)
                .UseEndpointMeta(meta: Annotations.GetPagedList)
                .RequireAccessPermission(Common.Security.Authorization.Permissions.Constants.FeaturePermission.Admin.Identity.AccessControlPermission.List);

            group.MapGet(pattern: "{id:guid}", handler: GetByIdHandler)
                .UseEndpointMeta(meta: Annotations.GetById)
                .RequireAccessPermission(Common.Security.Authorization.Permissions.Constants.FeaturePermission.Admin.Identity.AccessControlPermission.View);

            group.MapGet(pattern: "{name}", handler: GetByNameHandler)
                .UseEndpointMeta(meta: Annotations.GetByName)
                .RequireAccessPermission(Common.Security.Authorization.Permissions.Constants.FeaturePermission.Admin.Identity.AccessControlPermission.View);
        }

        private static async Task<Ok<ApiResponse<Get.ByName.Result>>> GetByNameHandler([FromRoute] string name, [FromServices] ISender mediator, CancellationToken cancellationToken)
        {
            var query = new Get.ByName.Query(Name: name);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Access permission retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Get.ById.Result>>> GetByIdHandler([FromRoute] Guid id, [FromServices] ISender mediator, CancellationToken cancellationToken)
        {
            var query = new Get.ById.Query(Id: id);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Access permission retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.PagedList.Result>>>> GetPagedListHandler([AsParameters] Get.PagedList.Request request, [FromServices] ISender mediator, CancellationToken cancellationToken)
        {
            var query = new Get.PagedList.Query(Request: request);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Access permissions retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<PagedList<Get.SelectList.Result>>>> GetSelectListHandler([AsParameters] Get.SelectList.Request request, [FromServices] ISender mediator, CancellationToken cancellationToken)
        {
            var query = new Get.SelectList.Query(Request: request);
            var result = await mediator.Send(request: query, cancellationToken: cancellationToken);
            var apiResponse = result.ToApiResponse(message: "Access permissions retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }
    }
}
