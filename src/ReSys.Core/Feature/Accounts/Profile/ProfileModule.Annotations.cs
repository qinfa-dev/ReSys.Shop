using Microsoft.AspNetCore.Http;

using ReSys.Core.Common.Models.Wrappers.Responses;

namespace ReSys.Core.Feature.Accounts.Profile;

public static partial class ProfileModule
{
    public static class Annotations
    {
        // ---------------- Group Metadata ----------------
        public static readonly ApiGroupMeta Group = new()
        {
            Name = "ProfileManagement",
            Tags = ["Profile"],
            Summary = "Profile API",
            Description = "Endpoints for getting and updating user profile."
        };

        // ---------------- Endpoint Metadata ----------------
        public static readonly ApiEndpointMeta Get = new()
        {
            Name = "Account.Profile.Get",
            Summary = "Get user profile",
            Description = "Retrieves the profile information for the current user.",
            ResponseType = typeof(ApiResponse<ProfileModule.Get.Result>),
            StatusCode = StatusCodes.Status200OK
        };

        public static readonly ApiEndpointMeta Update = new()
        {
            Name = "Account.Profile.Update",
            Summary = "Update user profile",
            Description = "Updates the profile information for the current user.",
            ResponseType = typeof(ApiResponse),
            StatusCode = StatusCodes.Status200OK
        };
    }
}