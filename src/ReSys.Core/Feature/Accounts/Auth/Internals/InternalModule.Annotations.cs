using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Auth.Internals;

public static partial class InternalModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "Account.Auth.Internal",
            Tags = ["Internal"],
            Summary = "Internal Authentication Management API",
            Description = "Endpoints for managing internal authentication flows."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta Register = new()
        {
            Name = "Account.Auth.Internal.Register",
            Summary = "Register new user",
            Description = "Registers a new user and sends email/phone confirmation.",
            ResponseType = typeof(ApiResponse<Register.Result>),
            StatusCode = StatusCodes.Status200OK
        };
        public static readonly ApiEndpointMeta Login = new()
        {
            Name = "Account.Auth.Internal.Login",
            Summary = "Login with password",
            Description = "Authenticates a user using credential + password.",
            ResponseType = typeof(ApiResponse<Login.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}