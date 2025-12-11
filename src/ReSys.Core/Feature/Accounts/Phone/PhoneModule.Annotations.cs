using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Phone;

public static partial class PhoneModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "PhoneManagement",
            Tags = ["Phone"],
            Summary = "Phone API",
            Description = "Endpoints for changing phone, confirming phone change, and resending phone verification."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta Change = new()
        {
            Name = "Account.Phone.Change",
            Summary = "Change phone",
            Description = "Changes a user's phone number and sends a verification code.",
            ResponseType = typeof(ApiResponse<Change.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta Confirm = new()
        {
            Name = "Account.Phone.Confirm",
            Summary = "Confirm phone change",
            Description = "Confirms a user's phone change using the verification code.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta ResendVerification = new()
        {
            Name = "Account.Phone.ResendVerification",
            Summary = "Resend phone verification",
            Description = "Resends the phone verification SMS.",
            ResponseType = typeof(ApiResponse<ResendVerification.Result>),
            StatusCode = StatusCodes.Status200OK
        };
    }
}