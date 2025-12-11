using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Profile;

public static partial class ProfileModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup(prefix: "api/account/profile")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapGet(pattern: string.Empty,
                    handler: GetHandler)
                .UseEndpointMeta(meta: Annotations.Get)
                .RequireAuthorization();

            group.MapPut(pattern: string.Empty,
                    handler: UpdateHandler)
                .UseEndpointMeta(meta: Annotations.Update)
                .RequireAuthorization();
        }

        private async Task<Ok<ApiResponse<Updated>>> UpdateHandler([FromBody] Update.Param param, [FromServices] ISender mediator, [FromServices] IUserContext userContext)
        {
            Update.Command command = new Update.Command(UserId: userContext.UserId, Param: param);
            ErrorOr<Updated> result = await mediator.Send(request: command);
            ApiResponse<Updated> apiResponse = result.ToApiResponse(message: "Profile updated successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private async Task<Ok<ApiResponse<Get.Result>>> GetHandler([FromServices] ISender mediator, [FromServices] IUserContext userContext)
        {
            Get.Query query = new Get.Query(UserId: userContext.UserId);
            ErrorOr<Get.Result> result = await mediator.Send(request: query);
            ApiResponse<Get.Result> apiResponse = result.ToApiResponse(message: "Profile retrieved successfully");

            return TypedResults.Ok(value: apiResponse);
        }
    }
}
