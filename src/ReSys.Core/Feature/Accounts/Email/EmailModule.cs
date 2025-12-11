using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Email;

public static partial class EmailModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            // Apply group metadata
            RouteGroupBuilder group = app.MapGroup(prefix: "api/account/email")
                                         .UseGroupMeta(meta: Annotations.Group);

            group.MapPost(pattern: "change",
                    handler: ChangeHandler)
                .UseEndpointMeta(meta: Annotations.Change)
                .RequireAuthorization();

            group.MapPost(pattern: "confirm",
                    handler: ConfirmHandler)
                .UseEndpointMeta(meta: Annotations.Confirm);

            group.MapPost(pattern: "resend-confirmation",
                    handler: ResendHandler)
                .UseEndpointMeta(meta: Annotations.ResendConfirmation);
        }

        private static async Task<Ok<ApiResponse<ResendConfirmation.Result>>> ResendHandler([FromBody] ResendConfirmation.Param param, [FromServices] ISender mediator)
        {
            var command = new ResendConfirmation.Command(Param: param);
            ErrorOr<ResendConfirmation.Result> result = await mediator.Send(request: command);
            var apiResponse = result.ToApiResponse(message: "Email confirmation resent successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Confirm.Result>>> ConfirmHandler([FromBody] Confirm.Param param, [FromServices] ISender mediator)
        {
            var command = new Confirm.Command(Param: param);
            ErrorOr<Confirm.Result> result = await mediator.Send(request: command);
            var apiResponse = result.ToApiResponse(message: "Email confirmed successfully");
            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Change.Result>>> ChangeHandler([FromBody] Change.Param param, [FromServices] ISender mediator)
        {
            var command = new Change.Command(Param: param);
            ErrorOr<Change.Result> result = await mediator.Send(request: command);
            var apiResponse = result.ToApiResponse(message: "Email change request sent successfully");
            return TypedResults.Ok(value: apiResponse);
        }
    }
}