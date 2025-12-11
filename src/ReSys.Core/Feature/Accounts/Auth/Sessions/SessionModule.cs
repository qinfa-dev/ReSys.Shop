using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class SessionModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            // Apply group metadata
            RouteGroupBuilder group = app.MapGroup(prefix: "api/account/auth/session")
                                         .UseGroupMeta(meta: Annotations.Group);

            group.MapGet(pattern: "/",
                    handler: GetHandler)
                .UseEndpointMeta(meta: Annotations.Get)
                .RequireAuthorization();

            group.MapPost(pattern: "/refresh",
                    handler: RefreshHandler)
                .UseEndpointMeta(meta: Annotations.Refresh);
        }

        private static async Task<Ok<ApiResponse<Get.Result>>> GetHandler([FromServices] ISender mediator)
        {
            Get.Query query = new Get.Query();
            ErrorOr<Get.Result> result = await mediator.Send(request: query);
            ApiResponse<Get.Result> apiResponse =
                result.ToApiResponse(message: "Session information retrieved successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Refresh.Result>>> RefreshHandler(
            [FromBody] Refresh.Param param,
            [FromServices] ISender mediator)
        {
            Refresh.Command command = new Refresh.Command(Param: param);
            ErrorOr<Refresh.Result> result = await mediator.Send(request: command);
            ApiResponse<Refresh.Result> apiResponse =
                result.ToApiResponse(message: "Token refreshed successfully");

            return TypedResults.Ok(value: apiResponse);
        }
    }
}