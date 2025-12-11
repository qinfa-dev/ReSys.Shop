using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class SessionModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "SessionManagement",
            Tags = ["Session"],
            Summary = "Session API",
            Description = "Endpoints for managing user sessions, including getting session info and refreshing tokens."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta Get = new()
        {
            Name = "Account.Authentication.Session.Get",
            Summary = "Get user session",
            Description = "Retrieves the session information for the current user.",
            ResponseType = typeof(ApiResponse<global::ReSys.Core.Feature.Accounts.Auth.Sessions.SessionModule.Get.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta Refresh = new()
        {
            Name = "Account.Authentication.Session.Refresh",
            Summary = "Refresh Token",
            Description = "Refreshes the authentication token for the currently authenticated user.",
            ResponseType = typeof(ApiResponse<global::ReSys.Core.Feature.Accounts.Auth.Sessions.SessionModule.Refresh.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}