using Carter;

using MediatR;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;

using ReSys.Core.Common.Models.Wrappers.PagedLists;
using ReSys.Core.Common.Models.Wrappers.Responses;
using ReSys.Core.Feature.Common.Security.Authentication.Contexts.Interfaces;

namespace ReSys.Core.Feature.Accounts.Addresses;

public static partial class AddressModule
{
    public sealed class Endpoints : ICarterModule
    {
        private const string Route = "api/account/addresses";
        public void AddRoutes(IEndpointRouteBuilder app)
        {
            var group = app.MapGroup(Route)
                           .UseGroupMeta(Annotations.Group)
                           .RequireAuthorization();

            group.MapPost(string.Empty, CreateHandler)
            .UseEndpointMeta(Annotations.Create)
            .RequireAuthorization();

            group.MapDelete("{id:guid}", DeleteHandler)
            .UseEndpointMeta(Annotations.Delete)
            .RequireAuthorization();

            group.MapGet("{id:guid}", GetByIdHandler)
            .UseEndpointMeta(Annotations.GetById)
            .RequireAuthorization();

            group.MapGet(string.Empty, GetPagedListHandler)
            .UseEndpointMeta(Annotations.GetPagedList)
            .RequireAuthorization();

            group.MapGet("select", GetSelectListHandler)
            .UseEndpointMeta(Annotations.GetSelectList)
            .RequireAuthorization();

            group.MapPut("{id:guid}", UpdateHandler)
            .UseEndpointMeta(Annotations.Update)
            .RequireAuthorization();
        }

        private static async Task<Ok<ApiResponse<List<GetSelectList.Result>>>> GetSelectListHandler([AsParameters] GetSelectList.Param param, [FromServices] ISender mediator, [FromServices] IUserContext userContext, CancellationToken cancellationToken)
        {
            var query = new GetSelectList.Query(UserId: userContext.UserId, Param: param);
            ErrorOr<PagedList<GetSelectList.Result>> result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponsePaged("User addresses retrieved successfully");
            return TypedResults.Ok(apiResponse);
        }

        private static async Task<Ok<ApiResponse<List<GetPagedList.Result>>>> GetPagedListHandler([AsParameters] GetPagedList.Param param, [FromServices] ISender mediator, [FromServices] IUserContext userContext, CancellationToken cancellationToken)
        {
            var query = new GetPagedList.Query(UserId: userContext.UserId, Param: param);
            ErrorOr<PagedList<GetPagedList.Result>> result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponsePaged("User addresses retrieved successfully");
            return TypedResults.Ok(apiResponse);
        }

        private static async Task<Ok<ApiResponse<GetById.Result>>> GetByIdHandler([FromRoute] Guid id, [FromServices] ISender mediator, [FromServices] IUserContext userContext, CancellationToken cancellationToken)
        {
            var query = new GetById.Query(Id: id, UserId: userContext.UserId);
            ErrorOr<GetById.Result> result = await mediator.Send(query, cancellationToken);
            var apiResponse = result.ToApiResponse("User address details retrieved successfully");
            return TypedResults.Ok(apiResponse);
        }

        private static async Task<Ok<ApiResponse>> DeleteHandler([FromRoute] Guid id, [FromServices] ISender mediator, [FromServices] IUserContext userContext, CancellationToken cancellationToken)
        {
            var command = new Delete.Command(Id: id, UserId: userContext.UserId);
            ErrorOr<Deleted> result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseDeleted("User address deleted successfully");
            return TypedResults.Ok(apiResponse);
        }

        private static async Task<Created<ApiResponse<Create.Result>>> CreateHandler([FromBody] Create.Param param, [FromServices] ISender mediator, [FromServices] IUserContext userContext, CancellationToken cancellationToken)
        {
            var command = new Create.Command(UserId: userContext.UserId, Param: param);
            ErrorOr<Create.Result> result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponseCreated("User address created successfully");
            return TypedResults.Created($"{Route}/{apiResponse.Data?.Id}", apiResponse);
        }

        private static async Task<Ok<ApiResponse<Update.Result>>> UpdateHandler([FromRoute] Guid id, [FromBody] Update.Param param, [FromServices] ISender mediator, [FromServices] IUserContext userContext, CancellationToken cancellationToken)
        {
            var command = new Update.Command(Id: id, UserId: userContext.UserId, Param: param);
            ErrorOr<Update.Result> result = await mediator.Send(command, cancellationToken);
            var apiResponse = result.ToApiResponse("User address updated successfully");
            return TypedResults.Ok(apiResponse);
        }
    }
}
