using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Password;

public static partial class PasswordModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "Password",
            Tags = ["Password"],
            Summary = "Password API",
            Description = "Endpoints for changing password, requesting password reset, and resetting password."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta Change = new()
        {
            Name = "Account.Password.Change",
            Summary = "Change password",
            Description = "Changes a user's password.",
            ResponseType = typeof(ApiResponse<Updated>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta Forgot = new()
        {
            Name = "Account.Password.Forgot",
            Summary = "Forgot password",
            Description = "Initiates the password reset process for a user's account.",
            ResponseType = typeof(ApiResponse<Forgot.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta Reset = new()
        {
            Name = "Account.Password.Reset",
            Summary = "Reset password",
            Description = "Resets a user's password using a valid reset code.",
            ResponseType = typeof(ApiResponse<Reset.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}
