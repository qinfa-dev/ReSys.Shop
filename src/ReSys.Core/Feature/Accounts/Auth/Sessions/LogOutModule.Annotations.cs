using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Auth.Sessions;

public static partial class LogOutModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "LogoutManagement",
            Tags = ["Logout"],
            Summary = "Account Logout API",
            Description = "Account logout API. Allows users to log out from current or all sessions."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta Single = new()
        {
            Name = "Account.Authentication.LogOut.Single",
            Summary = "Logout",
            Description = "Logs out the currently authenticated user.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta FromAll = new()
        {
            Name = "Account.Authentication.Logout.All",
            Summary = "Logout All",
            Description = "Logs out the currently authenticated user from all sessions.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };
    }
}