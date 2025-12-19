using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

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
            
            if (app.ServiceProvider
                .GetRequiredService<IHostEnvironment>()
                .IsDevelopment())
            {
                group.MapPost("login/dev", DevLoginHandler);
            }
        }
        
        private static async Task<Ok<ApiResponse<Login.Result>>> DevLoginHandler(
            [FromServices] ISender mediator)
        {
            var param = new Login.Param(
                Credential: "System.Admin",
                Password: "Seeder@123",
                RememberMe: true
            );

            var command = new Login.Command(param);
            var result = await mediator.Send(command);

            return TypedResults.Ok(
                result.ToApiResponse("Development auto login")
            );
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

