using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Phone;

public static partial class PhoneModule
{
    public sealed class Endpoints : ICarterModule
    {
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            RouteGroupBuilder group = app.MapGroup(prefix: "api/account/phone")
                .UseGroupMeta(meta: Annotations.Group)
                .RequireAuthorization();

            group.MapPost(pattern: "change",
                    handler: ChangeHandler)
                .UseEndpointMeta(meta: Annotations.Change)
                .RequireAuthorization();

            group.MapPost(pattern: "confirm",
                    handler: ConfirmHandler)
                .UseEndpointMeta(meta: Annotations.Confirm)
                .RequireAuthorization();

            group.MapPost(pattern: "resend",
                    handler: ResendHandler)
                .UseEndpointMeta(meta: Annotations.ResendVerification)
                .RequireAuthorization();
        }

        private static async Task<Ok<ApiResponse<ResendVerification.Result>>> ResendHandler([FromBody] ResendVerification.Param param, [FromServices] ISender mediator, CancellationToken ct)
        {
            ResendVerification.Command command = new ResendVerification.Command(Param: param);
            ErrorOr<ResendVerification.Result> result = await mediator.Send(request: command, cancellationToken: ct);
            ApiResponse<ResendVerification.Result> apiResponse = result.ToApiResponse(message: "Phone verification code resent successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Updated>>> ConfirmHandler([FromBody] Confirm.Param param, [FromServices] ISender mediator, CancellationToken ct)
        {
            Confirm.Command command = new Confirm.Command(Param: param);
            ErrorOr<Updated> result = await mediator.Send(request: command, cancellationToken: ct);
            ApiResponse<Updated> apiResponse = result.ToApiResponse(message: "Phone number confirmed successfully");

            return TypedResults.Ok(value: apiResponse);
        }

        private static async Task<Ok<ApiResponse<Change.Result>>> ChangeHandler([FromBody] Change.Param param, [FromServices] ISender mediator, CancellationToken ct)
        {
            Change.Command command = new Change.Command(Param: param);
            ErrorOr<Change.Result> result = await mediator.Send(request: command, cancellationToken: ct);
            ApiResponse<Change.Result> apiResponse = result.ToApiResponse(message: "Phone change command sent successfully");

            return TypedResults.Ok(value: apiResponse);
        }
    }
}

