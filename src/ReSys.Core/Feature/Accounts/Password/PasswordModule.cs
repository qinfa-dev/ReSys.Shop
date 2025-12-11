using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Password;

public static partial class PasswordModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            // Apply group metadata
            RouteGroupBuilder group = app.MapGroup(prefix: "api/account/password")
                                         .UseGroupMeta(meta: Annotations.Group);

            group.MapPost(pattern: "change",
                    handler: ChangeHandler)
               .UseEndpointMeta(meta: Annotations.Change)
               .RequireAuthorization();

            group.MapPost(pattern: "forgot",
                    handler: ForgotHandler)
               .UseEndpointMeta(meta: Annotations.Forgot);

            group.MapPost(pattern: "reset",
                    handler: ResetHandler)
               .UseEndpointMeta(meta: Annotations.Reset);
        }

        private async Task<Ok<ApiResponse<Reset.Result>>> ResetHandler([FromBody] Reset.Param param, [FromServices] ISender mediator)
        {
            var command = new Reset.Command(Param: param);
            var result = await mediator.Send(request: command);
            var apiResponse = result.ToApiResponse(message: "Password reset successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private async Task<Ok<ApiResponse<Forgot.Result>>> ForgotHandler([FromBody] Forgot.Param param, [FromServices] ISender mediator)
        {
            var command = new Forgot.Command(Param: param);
            var result = await mediator.Send(request: command);
            var apiResponse = result.ToApiResponse(message: "Password reset email sent successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Updated>>> ChangeHandler([FromBody] Change.Param param, [FromServices] ISender mediator, [FromServices] IUserContext userContext)
        {
            var command = new Change.Command(Param: param);
            var result = await mediator.Send(request: command);
            var apiResponse = result.ToApiResponse(message: "Password changed successfully");
            return TypedResults.Ok(value: apiResponse);
        }
    }
}
