using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class LogOutModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            // Apply group metadata
            RouteGroupBuilder group = app.MapGroup(prefix: "api/account/auth/session/logout")
                                         .UseGroupMeta(meta: Annotations.Group);

            group.MapPost(pattern: "/me",
                    handler: SingleHandler)
                .UseEndpointMeta(meta: Annotations.Single)
                .RequireAuthorization();

            group.MapPost(pattern: "/all",
                    handler: FromAllHandler)
                .UseEndpointMeta(meta: Annotations.FromAll)
                .RequireAuthorization();
        }

        private static async Task<IResult> SingleHandler(
            [FromBody] Single.Param param,
            [FromServices] ISender mediator)
        {
            Single.Command command = new Single.Command(Param: param);
            ErrorOr<Deleted> result = await mediator.Send(request: command);
            ApiResponse apiResponse = result.ToApiResponseDeleted(message: "Successfully logged out");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<IResult> FromAllHandler(
            [FromBody] FromAll.Param param,
            [FromServices] ISender mediator)
        {
            FromAll.Command command = new FromAll.Command(Param: param);
            ErrorOr<Deleted> result = await mediator.Send(request: command);
            ApiResponse apiResponse = result.ToApiResponseDeleted(message: "Successfully logged out from all devices");
            return TypedResults.Ok(value: apiResponse);
        }
    }
}