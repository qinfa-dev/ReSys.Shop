using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Auth.Externals;

public static partial class ExternalModule
{
    private const string Route = "api/account/auth/external";
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup(prefix: Route)
                .UseGroupMeta(meta: Annotations.Group)
                .DisableAntiforgery(); // Disable antiforgery for external API calls

            group.MapGet(
                    pattern: "/{provider}/configuration",
                    handler: GetOAuthConfigHandler)
                .UseEndpointMeta(meta: Annotations.GetOAuthConfig)
                .AllowAnonymous();

            group.MapGet(pattern: "/providers",
                    handler: GetExternalProvidersHandler)
                .UseEndpointMeta(meta: Annotations.GetExternalProviders)
                .AllowAnonymous();

            group.MapPost(
                    pattern: "/{provider}/exchange",
                    handler: ExchangeTokenHandler)
                .UseEndpointMeta(meta: Annotations.ExchangeToken)
                .AllowAnonymous();

            group.MapPost(
                    pattern: "/{provider}/verify",
                    handler: VerifyExternalTokenHandler)
                .UseEndpointMeta(meta: Annotations.VerifyExternalToken)
                .AllowAnonymous();
        }

        private static async Task<Ok<ApiResponse<GetOAuthConfig.Result>>> GetOAuthConfigHandler(
            [FromRoute] string provider,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetOAuthConfig.Query(Provider: provider);
            var result = await mediator.Send(request: query,
                cancellationToken: cancellationToken);

            var apiResponse = result.ToApiResponse(
                message: $"OAuth configuration retrieved successfully for {provider}");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<List<GetExternalProviders.Result>>>> GetExternalProvidersHandler(
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var query = new GetExternalProviders.Query();
            var result = await mediator.Send(request: query,
                cancellationToken: cancellationToken);

            var apiResponse = result.ToApiResponse(message: "External providers retrieved successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<ExchangeToken.Result>>> ExchangeTokenHandler(
            [FromRoute] string? provider,
            [FromBody] ExchangeToken.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new ExchangeToken.Command(Provider: provider,
                Param: param);
            ErrorOr<ExchangeToken.Result> result = await mediator.Send(request: command,
                cancellationToken: cancellationToken);

            var apiResponse = result.ToApiResponse(
                message: $"External token exchanged successfully for {provider}");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<VerifyExternalToken.Result>>> VerifyExternalTokenHandler(
            [FromRoute] string? provider,
            [FromBody] VerifyExternalToken.Param param,
            [FromServices] ISender mediator,
            CancellationToken cancellationToken)
        {
            var command = new VerifyExternalToken.Command(
                Provider: provider,
                Param: param
            );

            var result = await mediator.Send(request: command,
                cancellationToken: cancellationToken);

            var apiResponse = result.ToApiResponse(
                message: $"External token verified successfully for {provider}"
            );

            return TypedResults.Ok(value: apiResponse);
        }
    }
}