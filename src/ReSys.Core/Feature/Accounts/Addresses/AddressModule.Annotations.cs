using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Addresses;

public static partial class AddressModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static ApiGroupMeta Group => new()
        {
            Name = "Account.Address",
            Tags = ["User Addresses"],
            Summary = "User Address Management API",
            Description = "Endpoints for managing user addresses"
        };

        // ---------------- Endpoint Metadata ----------------
        public static ApiEndpointMeta Create => new()
        {
            Name = "Account.Address.Create",
            Summary = "Create a new user address",
            Description = "Creates a new user address with the specified details.",
            ResponseType = typeof(ApiResponse<Create.Result>),
            StatusCode = StatusCodes.Status201Created
        };

        public static ApiEndpointMeta Delete => new()
        {
            Name = "Account.Address.Delete",
            Summary = "Delete a user address",
            Description = "Deletes a user address by ID.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetById => new()
        {
            Name = "Account.Address.GetById",
            Summary = "Get user address details",
            Description = "Retrieves details of a specific user address by ID.",
            ResponseType = typeof(ApiResponse<GetById.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetPagedList => new()
        {
            Name = "Account.Address.GetPagedList",
            Summary = "Get paged list of user addresses",
            Description = "Retrieves a paginated list of user addresses.",
            ResponseType = typeof(ApiResponse<List<GetPagedList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta GetSelectList => new()
        {
            Name = "Account.Address.GetSelectList",
            Summary = "Get selectable list of user addresses",
            Description = "Retrieves a simplified list of user addresses for selection purposes.",
            ResponseType = typeof(ApiResponse<List<GetSelectList.Result>>),
            StatusCode = StatusCodes.Status200OK
        };

        public static ApiEndpointMeta Update => new()
        {
            Name = "Account.Address.Update",
            Summary = "Update a user address",
            Description = "Updates an existing user address by ID.",
            ResponseType = typeof(ApiResponse<Update.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}
