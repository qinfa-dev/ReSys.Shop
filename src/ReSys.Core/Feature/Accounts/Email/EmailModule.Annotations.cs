using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Email;

public static partial class EmailModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "EmailManagement",
            Tags = ["Email"],
            Summary = "Email API",
            Description = "Endpoints for changing email, confirming email, and resending email confirmation."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta Change = new()
        {
            Name = "Account.Email.Change",
            Summary = "Change email",
            Description = "Changes a user's email address.",
            ResponseType = typeof(ApiResponse<global::ReSys.Core.Feature.Accounts.Email.EmailModule.Change.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta Confirm = new()
        {
            Name = "Account.Email.Confirm",
            Summary = "Confirm email",
            Description = "Confirms a user's email address.",
            ResponseType = typeof(ApiResponse<global::ReSys.Core.Feature.Accounts.Email.EmailModule.Confirm.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta ResendConfirmation = new()
        {
            Name = "Account.Email.ResendConfirmation",
            Summary = "Resend confirmation email",
            Description = "Resends a user's email confirmation link.",
            ResponseType = typeof(ApiResponse<global::ReSys.Core.Feature.Accounts.Email.EmailModule.ResendConfirmation.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}