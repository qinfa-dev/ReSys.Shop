using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Auth.Internals;

public static partial class InternalModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(prefix: "api/account/auth/internal/")
                .UseGroupMeta(Annotations.Group)
                .DisableAntiforgery(); // Disable Anti-forgery if not needed

            group.MapPost(pattern: "register",
                    handler: RegisterHandler)
                .UseEndpointMeta(Annotations.Register);

            group.MapPost(pattern: "login",
                    handler: LoginHandler)
                .UseEndpointMeta(meta: Annotations.Login);

        }

        private static async Task<Ok<ApiResponse<Login.Result>>> LoginHandler([FromBody] Login.Param param, [FromServices] ISender mediator)
        {
            Login.Command command = new Login.Command(Param: param);
            ErrorOr<Login.Result> result = await mediator.Send(request: command);
            ApiResponse<Login.Result> apiResponse = result.ToApiResponse(message: "User logged in successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Register.Result>>> RegisterHandler([FromBody] Register.Command command, [FromServices] ISender mediator)
        {
            ErrorOr<Register.Result> result = await mediator.Send(request: command);
            ApiResponse<Register.Result> apiResponse = result.ToApiResponse(message: "User registered successfully");

            return TypedResults.Ok(value: apiResponse);
        }
    }
}

